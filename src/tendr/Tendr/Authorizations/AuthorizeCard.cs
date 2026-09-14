using Marten;
using Microsoft.AspNetCore.Mvc;
using Wolverine;
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

        return Invalid(command) is { } detail
            ? new ProblemDetails { Detail = detail, Status = 400 }
            : WolverineContinue.NoProblems;
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

        var authorization = Start(Guid.Parse(idempotencyKey), command, session);

        return Results.Created($"/v1/authorizations/{authorization.Id}", authorization);
    }

    // The request rules, shared with AuthorizeCardHandler: why the request
    // cannot be authorized, or null when it can.
    public static string? Invalid(AuthorizeCard command)
    {
        if (command.AmountCents <= 0)
        {
            return "amountCents must be greater than zero.";
        }

        if (command.Currency != "usd")
        {
            return "currency must be usd.";
        }

        return command.Card is null ? "card is required." : null;
    }

    // Decides the card and starts its stream, keyed by the idempotency key.
    // Also shared with AuthorizeCardHandler, so the REST call and the message
    // run the same rules.
    public static CardAuthorization Start(Guid id, AuthorizeCard command, IDocumentSession session)
    {
        var number = (command.Card!.Number ?? string.Empty).Replace(" ", string.Empty);
        var last4 = number.Length <= 4 ? number : number[^4..];
        var reason = Decide(number);

        object outcome = reason is null ? new CardApproved(id) : new CardDeclined(id, reason);

        session.Events.StartStream<Authorization>(id, new AuthorizationRequested(id, command.AmountCents, command.Currency!, last4), outcome);

        return new CardAuthorization(id, reason is null ? "approved" : "declined", reason, command.AmountCents, command.Currency!, last4);
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

/// <summary>
/// The same authorization as a Wolverine message, for a caller that is itself
/// a Wolverine app and sends over Wolverine's HTTP transport instead of
/// calling the REST endpoint. A real vendor would not offer this; it answers
/// "and if the other side were mine?".
/// </summary>
public static class AuthorizeCardHandler
{
    // The envelope id stands in for the Idempotency-Key header: a message has
    // no headers of its own, and a redelivered envelope keeps its id.
    // Returning the CardAuthorization makes it the reply to InvokeAsync<T>.
    public static async Task<CardAuthorization> Handle(
        AuthorizeCard command,
        Envelope envelope,
        IDocumentSession session,
        CancellationToken ct)
    {
        var existing = await session.LoadAsync<Authorization>(envelope.Id, ct);
        if (existing is not null)
        {
            return CardAuthorization.From(existing);
        }

        if (AuthorizeCardEndpoint.Invalid(command) is { } detail)
        {
            throw new ArgumentException(detail, nameof(command));
        }

        return AuthorizeCardEndpoint.Start(envelope.Id, command, session);
    }
}
