namespace LayerCake.Infrastructure.Payments;

/// <summary>
/// Tendr's authorization response. Stays inside Infrastructure; the gateway
/// maps it to the Application layer's PaymentAuthorization.
/// </summary>
public sealed record TendrAuthorizationResponse(
    Guid Id,
    string Status,
    string? Reason,
    int AmountCents,
    string Currency,
    string CardLast4);
