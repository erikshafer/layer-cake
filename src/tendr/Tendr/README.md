# Tendr

*A very small card vendor that happens to live in this repo.*

Tendr is the third-party payment service both LayerCake twins call when an order comes with a card (slice 005, `docs/slices/005-pay-with-tendr.md`). It exists so that call is a real HTTP request over a real socket, in the live demo and in the contract suite, without anyone needing a Stripe account. It is not a twin, it is not in the scorecard, and nothing about how it is built is part of the talk's argument. If you are curious anyway: Wolverine.Http endpoints over Marten's event store, one stream per authorization.

## Run it

```bash
docker compose up -d
dotnet run --project src/tendr/Tendr
```

Tendr listens on `http://localhost:42040`, with Swagger UI at `/swagger`. The twins find it through `Tendr:BaseUrl`, which defaults to that address.

## The API

### POST /v1/authorizations

Header `Idempotency-Key: <uuid>`, required. Body:

```json
{ "amountCents": 6120, "currency": "usd", "card": { "number": "4242 4242 4242 4242" } }
```

The first request with a key answers `201 Created` with `Location: /v1/authorizations/{key}`. Any later request with the same key answers `200 OK` with the identical body, whatever that later request sent. That is how a client retries safely: the twins use the order id as the key, so one order can never be authorized twice.

```json
{ "id": "<the key>", "status": "approved", "reason": null, "amountCents": 6120, "currency": "usd", "cardLast4": "4242" }
```

`status` is `approved` or `declined`. A decline is still a `201`: the request was fine, the card was not. `reason` is `null` when approved, otherwise `card_declined`, `insufficient_funds`, or `unknown_card`.

`400` problem responses: the `Idempotency-Key` header is missing or not a UUID, `amountCents` is not greater than zero, `currency` is not `usd`, or `card` is missing.

### GET /v1/authorizations/{id}

`200` with the same body, or a `404` problem.

### GET /ping

`pong`.

### Wolverine's HTTP transport

Tendr also maps Wolverine's own HTTP transport endpoints (`/_wolverine/batch/{queue}` and `/_wolverine/invoke`), so a Wolverine application can send `AuthorizeCard` as a message and get the `CardAuthorization` back as the reply to `InvokeAsync<CardAuthorization>`. The envelope id stands in for the `Idempotency-Key` header, and the message runs the same rules as the REST call. A real vendor would not offer this and neither twin uses it; `tests/Tendr.Tests/HttpTransportFacts.cs` is the proof that it works. On the pinned Wolverine 6.30.0 the transport only resolves `https://` URLs (plain `http://` arrives in 6.34.0), which is why that test serves Tendr over HTTPS with a throwaway certificate.

## Test cards

The whole list. Spaces are optional.

| Card number | Result |
|---|---|
| `4242 4242 4242 4242` | approved |
| `4000 0000 0000 0002` | declined, `card_declined` |
| `4000 0000 0000 9995` | declined, `insufficient_funds` |
| anything else | declined, `unknown_card` |

## What it stores

Marten, in schema `tendr` of the same PostgreSQL database the twins use. Sharing the bakery's database is a demo convenience and nothing more: Tendr never reads the `before` or `after` schemas, and pointing it at a server of its own is one connection string (`ConnectionStrings:Postgres`).

Each authorization is an event stream keyed by the idempotency key: `AuthorizationRequested`, then `CardApproved` or `CardDeclined`. An `Authorization` snapshot is kept inline, so the GET and the replay check are single document loads. Of the card, Tendr keeps the last four digits and nothing else. There is no seed data; Tendr starts empty.

## Tests

```bash
dotnet test tests/Tendr.Tests
```

Tendr's own scenarios on a throwaway PostgreSQL container (Docker required). The twins' contract suite also starts a Tendr host per twin, on Kestrel against that twin's test database, and reaches it through PlaceOrder.
