using MediatR;

namespace LayerCake.Application.Baker.Commands.CreateBakerTask;

/// <summary>
/// Creates the baker's to-do entry for an order. Dispatched by the RabbitMQ
/// consumer in WebApi once a NotifyBakerMessage arrives.
/// </summary>
public sealed record CreateBakerTaskCommand(Guid OrderId, string Summary) : IRequest;
