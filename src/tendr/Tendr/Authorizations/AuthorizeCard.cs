using Marten;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace Tendr.Authorizations;

public record AuthorizeCard(int AmountCents, string? Currency, Card? Card);

public record Card(string? Number);

/// <summary>
/// Tendr's answer: the same body the first time, on every replay of the same
/// idempotency key, and on the GET.
/// </summary>
public record CardAuthorization(
    Guid Id,
    string Status,
    string? Reason,
    int AmountCents,
    string Currency,
    string CardLast4)
{
    public static CardAuthorization From(Authorization authorization) => new(
        authorization.Id,
        authorization.Status,
        authorization.Reason,
        authorization.AmountCents,
        authorization.Currency,
        authorization.CardLast4);
}

public static class AuthorizeCardEndpoint
{
    public const string IdempotencyKeyHeader = "Idempotency-Key";

    // A replay finds the authorization its first request made. A missing or
    // malformed key finds nothing, and Validate answers for it.
    public static async Task<Authorization?> LoadAsync(
        [FromHeader(Name = IdempotencyKeyHeader)] string? idempotencyKey,
        IDocumentSession session,
        CancellationToken ct)
    {
        return Guid.TryParse(idempotencyKey, out var id)
            ? await session.LoadAsync<Authorization>(id, ct)
            : null;
    }

    public static ProblemDetails Validate(
        AuthorizeCard command,
        [FromHeader(Name = IdempotencyKeyHeader)] string? idempotencyKey,
        Authorization? existing)
    {
        if (!Guid.TryParse(idempotencyKey, out _))
        {
            return new ProblemDetails { Detail = "The Idempotency-Key header is required and must be a UUID.", Status = 400 };
        }

        // A replay gets the first answer whatever it sent this time, the way
        // Stripe treats a reused key.
        if (existing is not null)
        {
            return WolverineContinue.NoProblems;
        }

        if (command.AmountCents <= 0)
        {
            return new ProblemDetails { Detail = "amountCents must be greater than zero.", Status = 400 };
        }

        if (command.Currency != "usd")
        {
            return new ProblemDetails { Detail = "currency must be usd.", Status = 400 };
        }

        if (command.Card is null)
        {
            return new ProblemDetails { Detail = "card is required.", Status = 400 };
        }

        return WolverineContinue.NoProblems;
    }

    // 201 on first sight of a key, 200 with the stored answer on a replay.
    // AutoApplyTransactions commits the new stream after this method returns.
    [WolverinePost("/v1/authorizations")]
    public static IResult Post(
        AuthorizeCard command,
        [FromHeader(Name = IdempotencyKeyHeader)] string idempotencyKey,
        Authorization? existing,
        IDocumentSession session)
    {
        if (existing is not null)
        {
            return Results.Ok(CardAuthorization.From(existing));
        }

        var id = Guid.Parse(idempotencyKey);
        var number = (command.Card!.Number ?? string.Empty).Replace(" ", string.Empty);
        var last4 = number.Length <= 4 ? number : number[^4..];
        var reason = Decide(number);

        object outcome = reason is null ? new CardApproved(id) : new CardDeclined(id, reason);

        // One stream per authorization, keyed by the idempotency key.
        session.Events.StartStream<Authorization>(id, new AuthorizationRequested(id, command.AmountCents, command.Currency!, last4), outcome);

        var authorization = new CardAuthorization(
            id,
            reason is null ? "approved" : "declined",
            reason,
            command.AmountCents,
            command.Currency!,
            last4);

        return Results.Created($"/v1/authorizations/{id}", authorization);
    }

    // The whole test-card table: why a card is declined, or null when it is
    // approved. Spaces are for people reading the number.
    public static string? Decide(string cardNumber) => cardNumber.Replace(" ", string.Empty) switch
    {
        "4242424242424242" => null,
        "4000000000000002" => "card_declined",
        "4000000000009995" => "insufficient_funds",
        _ => "unknown_card"
    };
}
