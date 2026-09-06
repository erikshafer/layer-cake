namespace LayerCake.Application.Orders.Messages;

/// <summary>
/// The wire message that tells the bakers an order needs making. Published
/// by PlaceOrder after its commit and consumed by the WebApi hosted service,
/// which re-dispatches it as a CreateBakerTaskCommand.
/// </summary>
public sealed record NotifyBakerMessage(Guid OrderId, string Summary);
