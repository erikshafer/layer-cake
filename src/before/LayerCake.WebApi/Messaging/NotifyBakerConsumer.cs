using System.Text.Json;
using LayerCake.Application.Baker.Commands.CreateBakerTask;
using LayerCake.Application.Orders.Messages;
using LayerCake.Infrastructure.Messaging;
using MediatR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

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

    private readonly RabbitMqConnection _connection;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotifyBakerConsumer> _logger;
    private IChannel? _channel;

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
        consumer.ReceivedAsync += (_, args) => HandleAsync(args, stoppingToken);

        await _channel.BasicConsumeAsync(
            queue: _connection.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);
    }

    private async Task HandleAsync(BasicDeliverEventArgs args, CancellationToken cancellationToken)
    {
        try
        {
            var message = JsonSerializer.Deserialize<NotifyBakerMessage>(args.Body.Span, SerializerOptions)
                ?? throw new JsonException("Empty NotifyBakerMessage body.");

            // Handlers and repositories are scoped; a broker callback has no
            // request scope of its own, so the consumer opens one per message.
            using var scope = _scopeFactory.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();

            await sender.Send(new CreateBakerTaskCommand(message.OrderId, message.Summary), cancellationToken);

            await _channel!.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to handle NotifyBakerMessage; dropping delivery {DeliveryTag}.", args.DeliveryTag);

            // Nack without requeue: a real system would dead-letter here.
            await _channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false, cancellationToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }
    }
}
