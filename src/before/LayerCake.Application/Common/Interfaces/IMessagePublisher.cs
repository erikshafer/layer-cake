namespace LayerCake.Application.Common.Interfaces;

/// <summary>
/// The port for putting a message on the wire. The Application layer knows
/// that a message leaves the process; which broker, which queue, and which
/// client library are Infrastructure's business.
/// </summary>
public interface IMessagePublisher
{
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken);
}
