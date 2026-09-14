using System.Net.Http.Json;
using LayerCake.Application.Common.Exceptions;
using LayerCake.Application.Common.Interfaces;
using LayerCake.Application.Payments;

namespace LayerCake.Infrastructure.Payments;

/// <summary>
/// The adapter behind IPaymentGateway: authorizes an order's total with Tendr
/// over HTTP. The HttpClient arrives configured (base address, timeout) from
/// the typed-client registration in DependencyInjection.
/// </summary>
public sealed class TendrPaymentGateway : IPaymentGateway
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    private readonly HttpClient _httpClient;

    public TendrPaymentGateway(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PaymentAuthorization> AuthorizeAsync(
        Guid orderId,
        decimal total,
        string cardNumber,
        CancellationToken cancellationToken)
    {
        // The total is already rounded to cents by the pricing, so this is exact.
        var body = new TendrAuthorizationRequest(decimal.ToInt32(total * 100), "usd", new TendrCardRequest(cardNumber));

        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/authorizations")
        {
            Content = JsonContent.Create(body)
        };

        // The order id is the idempotency key, so a retried placement can
        // never authorize the same order twice.
        request.Headers.Add(IdempotencyKeyHeader, orderId.ToString());

        TendrAuthorizationResponse? response;

        try
        {
            using var httpResponse = await _httpClient.SendAsync(request, cancellationToken);
            httpResponse.EnsureSuccessStatusCode();

            response = await httpResponse.Content.ReadFromJsonAsync<TendrAuthorizationResponse>(cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new PaymentUnavailableException(exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient reports its own timeout as a cancellation.
            throw new PaymentUnavailableException(exception);
        }

        return new PaymentAuthorization(response!.Id, response.Status == "approved", response.Reason);
    }
}
