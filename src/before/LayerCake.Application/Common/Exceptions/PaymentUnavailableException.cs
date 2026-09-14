namespace LayerCake.Application.Common.Exceptions;

/// <summary>
/// Thrown when the payment gateway cannot be reached or does not answer in
/// time. Mapped to a 503 in the WebApi layer. The order is not placed and is
/// not retried.
/// </summary>
public sealed class PaymentUnavailableException : Exception
{
    public PaymentUnavailableException(Exception innerException)
        : base("The payment service did not answer.", innerException)
    {
    }
}
