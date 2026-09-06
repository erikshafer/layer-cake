using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace LayerCake.Infrastructure.Messaging;

/// <summary>
/// One broker connection per host process, opened on first use and closed
/// when the host disposes it. Channels are cheap and short-lived; the
/// connection is the expensive thing, so the publisher and the consumer
/// share this one.
/// </summary>
public sealed class RabbitMqConnection : IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;

    public RabbitMqConnection(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
    }

    public string QueueName => _options.QueueName;

    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync();
            }

            var factory = new ConnectionFactory { Uri = new Uri(_options.ConnectionUri) };
            _connection = await factory.CreateConnectionAsync(cancellationToken);

            return _connection;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Declares the baker-task queue. Both the publisher and the consumer call
    /// this, so whichever starts first creates the queue and the other side's
    /// declare is a no-op. The arguments live here once because RabbitMQ
    /// refuses a redeclaration with different arguments.
    /// </summary>
    public Task DeclareQueueAsync(IChannel channel, CancellationToken cancellationToken)
    {
        return channel.QueueDeclareAsync(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        // Close before dispose, as the client documents: disposing an open
        // connection takes the abort path, which can wait out the client's
        // internal timeouts before giving up quietly. A closed one disposes at
        // once. The close itself is bounded so a silent broker cannot hold the
        // host either; on expiry the client closes the socket and moves on.
        if (_connection is not null)
        {
            if (_connection.IsOpen)
            {
                await _connection.CloseAsync(TimeSpan.FromSeconds(5));
            }

            await _connection.DisposeAsync();
        }

        _gate.Dispose();
    }
}
