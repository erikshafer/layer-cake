namespace LayerCake.Infrastructure.Payments;

/// <summary>
/// The body of Tendr's POST /v1/authorizations, in Tendr's own terms.
/// </summary>
public sealed record TendrAuthorizationRequest(int AmountCents, string Currency, TendrCardRequest Card);

/// <summary>
/// The card as Tendr expects it inside an authorization request.
/// </summary>
public sealed record TendrCardRequest(string Number);
