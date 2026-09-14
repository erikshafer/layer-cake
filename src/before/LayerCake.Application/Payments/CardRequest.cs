namespace LayerCake.Application.Payments;

/// <summary>
/// The card a customer pays with. Passed through to the payment gateway and
/// never stored.
/// </summary>
public sealed record CardRequest(string Number);
