using System.Text.Json;
using LayerCake.Application.Baker.Commands.CreateBakerTask;
using LayerCake.Application.Orders.Messages;
using LayerCake.Infrastructure.Messaging;
using MediatR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace LayerCake.WebApi.Messaging;

/// <summary>
/// Pulls NotifyBakerMessage off the baker-task queue and re-dispatches it as a
/// CreateBakerTaskCommand. A consumer is a delivery mechanism like a
/// controller, so it lives in the outer ring and stays thin the same way:
/// deserialize, open a scope, send a command, acknowledge.
/// </summary>
public sealed class NotifyBakerConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    // The graceful part of shutdown gets this long. A broker or database that
    // does not answer in time must not hold the host for its full shutdown
    // timeout; the channel is aborted instead and the broker redelivers.
    private static readonly TimeSpan GracefulStopTimeout = TimeSpan.FromSeconds(5);

    private readonly RabbitMqConnection _connection;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotifyBakerConsumer> _logger;

    // Prefetch is 1, so at most one delivery is ever being handled; this gate
    // is how the shutdown waits for it before the channel goes away.
    private readonly SemaphoreSlim _inFlight = new(1, 1);
    private readonly object _shutdownLock = new();
    private Task? _shutdown;
    private IChannel? _channel;
    private string? _consumerTag;

    public NotifyBakerConsumer(
        RabbitMqConnection connection,
        IServiceScopeFactory scopeFactory,
        ILogger<NotifyBakerConsumer> logger)
    {
        _connection = connection;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connection = await _connection.GetConnectionAsync(stoppingToken);

        // One long-lived channel for the life of the host. The same declare as
        // the publisher, so start order between the two does not matter and a
        // message published before this subscription waits in the queue.
        _channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await _connection.DeclareQueueAsync(_channel, stoppingToken);

        // One unacknowledged message at a time keeps the handler's work serial.
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += (_, args) => HandleAsync(args);

        _consumerTag = await _channel.BasicConsumeAsync(
            queue: _connection.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);
    }

    // The host's stopping token is deliberately not passed down here. A delivery
    // that is already being handled when shutdown begins runs to completion and
    // is acknowledged, so stopping the host never drops a baker task; the
    // shutdown waits on the in-flight gate for exactly that reason.
    private async Task HandleAsync(BasicDeliverEventArgs args)
    {
        await _inFlight.WaitAsync();
        try
        {
            var message = JsonSerializer.Deserialize<NotifyBakerMessage>(args.Body.Span, SerializerOptions)
                ?? throw new JsonException("Empty NotifyBakerMessage body.");

            // Handlers and repositories are scoped; a broker callback has no
            // request scope of its own, so the consumer opens one per message.
            using var scope = _scopeFactory.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();

            await sender.Send(new CreateBakerTaskCommand(message.OrderId, message.Summary), CancellationToken.None);

            await _channel!.BasicAckAsync(args.DeliveryTag, multiple: false);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to handle NotifyBakerMessage; dropping delivery {DeliveryTag}.", args.DeliveryTag);

            // Nack without requeue: a real system would dead-letter here.
            await _channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false);
        }
        finally
        {
            _inFlight.Release();
        }
    }

    // A host can ask a hosted service to stop more than once, concurrently:
    // WebApplicationFactory (which the contract suite reaches through Alba)
    // stops a host whose own Run() is already stopping. The teardown below
    // tears down a channel, so it must run exactly once; every caller awaits
    // the same task.
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        lock (_shutdownLock)
        {
            _shutdown ??= ShutdownAsync(cancellationToken);
        }

        return _shutdown;
    }

    private async Task ShutdownAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        if (_channel is null)
        {
            return;
        }

        using var graceful = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        graceful.CancelAfter(GracefulStopTimeout);

        try
        {
            // Graceful shutdown in two steps: stop taking deliveries, then let the
            // one in flight finish and acknowledge. Disposing the channel under a
            // running handler makes its ack land on a closed object, and the
            // unacknowledged message would come straight back to whichever
            // consumer subscribes next.
            if (_consumerTag is not null && _channel.IsOpen)
            {
                await _channel.BasicCancelAsync(_consumerTag, noWait: false, graceful.Token);
            }

            await _inFlight.WaitAsync(graceful.Token);
            _inFlight.Release();
        }
        catch (Exception exception) when (exception is OperationCanceledException or RabbitMQClientException)
        {
            _logger.LogWarning(
                exception,
                "The baker-task consumer did not stop cleanly within {Timeout}; aborting its channel. An unacknowledged delivery is redelivered.",
                GracefulStopTimeout);
        }

        // Dispose aborts the channel: one bounded round trip, never an exception.
        // A graceful channel close is deliberately not attempted; with this
        // client version it can leave the connection's main loop unfinished, and
        // the connection then stalls on its own timeouts while shutting down.
        await _channel.DisposeAsync();
    }
}
