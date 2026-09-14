using LayerCake.Application.Payments;

namespace LayerCake.Application.Common.Interfaces;

/// <summary>
/// The port for taking a card payment. The Application layer knows that an
/// order's total is authorized against a card; which vendor, which protocol,
/// and which wire shapes are Infrastructure's business.
/// </summary>
public interface IPaymentGateway
{
    Task<PaymentAuthorization> AuthorizeAsync(
        Guid orderId,
        decimal total,
        string cardNumber,
        CancellationToken cancellationToken);
}
