using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace LayerCake.Slices.Payments;

/// <summary>
/// The body Tendr expects on POST /v1/authorizations, in Tendr's words.
/// </summary>
public record AuthorizeCard(int AmountCents, string Currency, Card Card);

public record Card(string Number);

/// <summary>
/// Tendr's answer. The card number never comes back; the last four digits do.
/// </summary>
public record CardAuthorization(
    Guid Id,
    string Status,
    string? Reason,
    int AmountCents,
    string Currency,
    string CardLast4);

/// <summary>
/// The bakery's own record of a card payment, kept apart from Tendr's shape.
/// No card means no Payment at all: the bakery takes payment at pickup.
/// </summary>
public record Payment(
    Guid AuthorizationId,
    string Status,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Reason);

/// <summary>
/// A typed HttpClient for the card vendor. The one constructor-injected class
/// in this twin, because IHttpClientFactory hands the configured HttpClient
/// to a constructor. Base address and timeout live in Program.cs.
/// </summary>
public sealed class TendrClient(HttpClient http)
{
    public async Task<CardAuthorization> AuthorizeAsync(
        Guid orderId,
        decimal total,
        string cardNumber,
        CancellationToken ct)
    {
        // Pricing already rounded the total to cents, so this is exact.
        var body = new AuthorizeCard(decimal.ToInt32(total * 100), "usd", new Card(cardNumber));

        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/authorizations")
        {
            Content = JsonContent.Create(body)
        };

        // The order id is the idempotency key: ask twice for the same order
        // and Tendr answers with the first authorization instead of a second.
        request.Headers.Add("Idempotency-Key", orderId.ToString());

        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<CardAuthorization>(ct))!;
    }
}
