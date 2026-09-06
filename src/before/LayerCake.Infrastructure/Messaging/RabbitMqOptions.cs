namespace LayerCake.Infrastructure.Messaging;

/// <summary>
/// Broker settings for the baker notification. The connection URI comes from
/// ConnectionStrings:RabbitMq and the rest from the RabbitMq section.
/// </summary>
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string ConnectionUri { get; set; } = "amqp://localhost";

    public string QueueName { get; set; } = "layercake-before-baker-tasks";
}
