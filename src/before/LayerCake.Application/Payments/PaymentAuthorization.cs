namespace LayerCake.Application.Payments;

/// <summary>
/// The outcome of a card authorization as the Application layer sees it, so
/// the handler never depends on a vendor's response shape.
/// </summary>
public sealed record PaymentAuthorization(Guid AuthorizationId, bool Approved, string? DeclineReason);
