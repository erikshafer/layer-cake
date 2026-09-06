using System.Text.Json;
using LayerCake.Application.Common.Interfaces;
using RabbitMQ.Client;

namespace LayerCake.Infrastructure.Messaging;

/// <summary>
/// The adapter behind IMessagePublisher: serializes the message as camelCase
/// JSON and publishes it to the baker-task queue through the default
/// exchange, routing key equal to the queue name, persistent delivery so
/// the message survives a broker restart alongside the durable queue.
/// </summary>
public sealed class RabbitMqMessagePublisher : IMessagePublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly RabbitMqConnection _connection;

    public RabbitMqMessagePublisher(RabbitMqConnection connection)
    {
        _connection = connection;
    }

    public async Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken)
    {
        var connection = await _connection.GetConnectionAsync(cancellationToken);

        // A channel per publish: channels are not thread-safe, and the handler
        // that calls this runs per request.
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await _connection.DeclareQueueAsync(channel, cancellationToken);

        var body = JsonSerializer.SerializeToUtf8Bytes(message, SerializerOptions);
        var properties = new BasicProperties
        {
            ContentType = "application/json",
            Persistent = true
        };

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: _connection.QueueName,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }
}
