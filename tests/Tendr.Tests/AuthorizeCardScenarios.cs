using System.Text.Json;
using Alba;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Tendr.Authorizations;
using Xunit;

namespace Tendr.Tests;

/// <summary>
/// Tendr's contract as a card vendor: POST /v1/authorizations with an
/// Idempotency-Key, the test-card table, replays, and GET by id.
/// </summary>
[Collection(TendrCollection.Name)]
public sealed class AuthorizeCardScenarios
{
    private static readonly JsonSerializerOptions Json = JsonSerializerOptions.Web;

    private readonly TendrFixture _fixture;

    public AuthorizeCardScenarios(TendrFixture fixture)
    {
        _fixture = fixture;
    }

    private IAlbaHost Host => _fixture.Host;

    private async Task<(CardAuthorization Authorization, string RawBody, IScenarioResult Result)> AuthorizeAsync(
        Guid key,
        string cardNumber,
        int amountCents = 6120,
        int expectedStatus = 201)
    {
        var result = await Host.Scenario(s =>
        {
            s.Post
                .Json(new { amountCents, currency = "usd", card = new { number = cardNumber } })
                .ToUrl("/v1/authorizations");
            s.WithRequestHeader(AuthorizeCardEndpoint.IdempotencyKeyHeader, key.ToString());
            s.StatusCodeShouldBe(expectedStatus);
        });

        var body = await result.ReadAsTextAsync();

        return (JsonSerializer.Deserialize<CardAuthorization>(body, Json)!, body, result);
    }

    private static async Task ShouldBeProblem(IScenarioResult result, string expectedReason)
    {
        result.Context.Response.ContentType.ShouldNotBeNull().ShouldStartWith("application/problem+json");

        var body = await result.ReadAsTextAsync();
        body.ShouldContain(expectedReason, Case.Insensitive);
    }

    [Fact]
    public async Task approved_card_returns_201_with_location_and_body()
    {
        var key = Guid.NewGuid();

        var (authorization, body, result) = await AuthorizeAsync(key, "4242 4242 4242 4242");

        result.Context.Response.Headers.Location.ToString().ShouldEndWith($"/v1/authorizations/{key}");

        authorization.Id.ShouldBe(key);
        authorization.Status.ShouldBe("approved");
        authorization.Reason.ShouldBeNull();
        authorization.AmountCents.ShouldBe(6120);
        authorization.Currency.ShouldBe("usd");
        authorization.CardLast4.ShouldBe("4242");

        // Null is on the wire as null, not left out.
        body.ShouldContain("\"reason\":null");
    }

    [Fact]
    public async Task card_ending_0002_is_declined_as_card_declined()
    {
        var (authorization, _, _) = await AuthorizeAsync(Guid.NewGuid(), "4000000000000002");

        authorization.Status.ShouldBe("declined");
        authorization.Reason.ShouldBe("card_declined");
        authorization.CardLast4.ShouldBe("0002");
    }

    [Fact]
    public async Task card_ending_9995_is_declined_as_insufficient_funds()
    {
        var (authorization, _, _) = await AuthorizeAsync(Guid.NewGuid(), "4000 0000 0000 9995");

        authorization.Status.ShouldBe("declined");
        authorization.Reason.ShouldBe("insufficient_funds");
    }

    [Fact]
    public async Task any_other_card_is_declined_as_unknown_card()
    {
        var (authorization, _, _) = await AuthorizeAsync(Guid.NewGuid(), "5555 5555 5555 4444");

        authorization.Status.ShouldBe("declined");
        authorization.Reason.ShouldBe("unknown_card");
        authorization.CardLast4.ShouldBe("4444");
    }

    [Fact]
    public async Task replay_of_a_key_returns_200_with_the_first_body_whatever_it_sends()
    {
        var key = Guid.NewGuid();

        var (_, firstBody, _) = await AuthorizeAsync(key, "4242 4242 4242 4242");

        // Different card, different amount, same key: the first answer stands.
        var (_, replayBody, replay) = await AuthorizeAsync(key, "4000 0000 0000 0002", amountCents: 99, expectedStatus: 200);

        replayBody.ShouldBe(firstBody);
        replay.Context.Response.Headers.Location.ToString().ShouldBeEmpty();
    }

    [Fact]
    public async Task missing_idempotency_key_returns_400()
    {
        var result = await Host.Scenario(s =>
        {
            s.Post
                .Json(new { amountCents = 100, currency = "usd", card = new { number = "4242424242424242" } })
                .ToUrl("/v1/authorizations");
            s.StatusCodeShouldBe(400);
        });

        await ShouldBeProblem(result, "Idempotency-Key");
    }

    [Fact]
    public async Task malformed_idempotency_key_returns_400()
    {
        var result = await Host.Scenario(s =>
        {
            s.Post
                .Json(new { amountCents = 100, currency = "usd", card = new { number = "4242424242424242" } })
                .ToUrl("/v1/authorizations");
            s.WithRequestHeader(AuthorizeCardEndpoint.IdempotencyKeyHeader, "order-42");
            s.StatusCodeShouldBe(400);
        });

        await ShouldBeProblem(result, "Idempotency-Key");
    }

    [Fact]
    public async Task nonpositive_amount_returns_400()
    {
        var result = await Host.Scenario(s =>
        {
            s.Post
                .Json(new { amountCents = 0, currency = "usd", card = new { number = "4242424242424242" } })
                .ToUrl("/v1/authorizations");
            s.WithRequestHeader(AuthorizeCardEndpoint.IdempotencyKeyHeader, Guid.NewGuid().ToString());
            s.StatusCodeShouldBe(400);
        });

        await ShouldBeProblem(result, "amountCents");
    }

    [Fact]
    public async Task currency_other_than_usd_returns_400()
    {
        var result = await Host.Scenario(s =>
        {
            s.Post
                .Json(new { amountCents = 100, currency = "eur", card = new { number = "4242424242424242" } })
                .ToUrl("/v1/authorizations");
            s.WithRequestHeader(AuthorizeCardEndpoint.IdempotencyKeyHeader, Guid.NewGuid().ToString());
            s.StatusCodeShouldBe(400);
        });

        await ShouldBeProblem(result, "currency");
    }

    [Fact]
    public async Task missing_card_returns_400()
    {
        var result = await Host.Scenario(s =>
        {
            s.Post.Json(new { amountCents = 100, currency = "usd" }).ToUrl("/v1/authorizations");
            s.WithRequestHeader(AuthorizeCardEndpoint.IdempotencyKeyHeader, Guid.NewGuid().ToString());
            s.StatusCodeShouldBe(400);
        });

        await ShouldBeProblem(result, "card");
    }

    [Fact]
    public async Task get_authorization_returns_200_with_the_same_body()
    {
        var key = Guid.NewGuid();
        var (_, postedBody, _) = await AuthorizeAsync(key, "4000 0000 0000 9995");

        var result = await Host.Scenario(s =>
        {
            s.Get.Url($"/v1/authorizations/{key}");
            s.StatusCodeShouldBe(200);
        });

        (await result.ReadAsTextAsync()).ShouldBe(postedBody);
    }

    [Fact]
    public async Task get_unknown_authorization_returns_404()
    {
        var result = await Host.Scenario(s =>
        {
            s.Get.Url($"/v1/authorizations/{Guid.NewGuid()}");
            s.StatusCodeShouldBe(404);
        });

        await ShouldBeProblem(result, "not found");
    }

    [Fact]
    public async Task an_authorization_is_a_stream_of_two_events_that_keeps_only_the_last_four()
    {
        var key = Guid.NewGuid();
        await AuthorizeAsync(key, "4242 4242 4242 4242");

        await using var session = Host.Services.GetRequiredService<IDocumentStore>().QuerySession();
        var events = await session.Events.FetchStreamAsync(key);

        events.Select(e => e.Data.GetType()).ShouldBe([typeof(AuthorizationRequested), typeof(CardApproved)]);

        // The stored events are the whole record, and the card number is not in it.
        foreach (var e in events)
        {
            JsonSerializer.Serialize(e.Data, e.EventType).ShouldNotContain("4242424242424242");
        }
    }
}
