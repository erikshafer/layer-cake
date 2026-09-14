# Slice 005: Pay with Tendr

**Why this slice is in the talk:** every audience asked the same question in different words: where does a call to an outside service go in a slice, and what does it cost each twin? The talk's steelman (4.2) says "if you have real vendor churn, keep the port," and the follow-up ("my payments client is behind a port and it paid; where does it go in a slice?") had no code behind it. This slice is that code. Both twins call a third-party card vendor, **Tendr**, over plain HTTP and JSON, on the load leg of PlaceOrder, and the shared suite runs every payment scenario against a real Tendr host on a real socket. Decided by Erik 2026-09-14, the first talk change after KCDC (v1.1).

Tendr is a fake vendor that lives in this repo (`src/tendr/Tendr`), built on Wolverine.Http and Marten's event store, because a payment is a stream by nature. It is not a twin and not in the scorecard, and its internals are never on a slide.

This slice extends slice 003 the way 004 did. Everything 003 settled stands.

## Contract

### POST /orders

Request gains an optional `card`:

```json
{
  "lines": [ { "cakeId": "…", "quantity": 2 } ],
  "couponCode": "BDAY10",
  "card": { "number": "4242 4242 4242 4242" }
}
```

- **No card: nothing changes.** A bakery takes payment at pickup. The request without `card` behaves exactly as in slice 003 and the response body is identical: no `payment` member at all, the same way `couponCode` is absent when no coupon was sent. No call is made to Tendr, so an order without a card succeeds even while Tendr is down. In each twin's own storage the order's payment status is `atPickup`; that is not on the wire.
- **Approved card:** `201` as in slice 003, plus `"payment": { "authorizationId": "<uuid>", "status": "approved" }`. `authorizationId` equals the order `id`, because the order id is the idempotency key sent to Tendr.
- Amount sent to Tendr: `total` in cents, currency `usd`.

Guard order, extending 003's (first failure wins, each failure shape follows the slice 001 parity rule):

1. `400`: `lines` non-empty; every `quantity` >= 1
2. `422`: every `cakeId` exists
3. `422`: `couponCode`, if present, evaluates to `valid`
4. `402`: `card`, if present, is approved by Tendr. Problem detail `Card declined: <reason>.` where reason is Tendr's (`card_declined`, `insufficient_funds`, `unknown_card`).
5. Decide: persist the order with its payment, emit `NotifyBaker`

A request that fails guards 1 to 3 is never sent to Tendr. A declined card creates no order and no baker task.

**The vendor is down.** When Tendr cannot be reached or does not answer within two seconds, both twins answer `503` as `application/problem+json` with detail `The payment service did not answer.` No order, no baker task. No retry, no circuit breaker, no resilience package on either twin: the two-second client timeout is the whole story, and it is the same on both sides.

### GET /orders/{id}

Mirrors the POST body: `payment` present with the same values when the order was paid by card, absent when paid at pickup.

### No card data in the twins

Neither twin stores or logs a card number. The twins pass it to Tendr and keep only what Tendr returns (the authorization id and status). Tendr stores the last four digits and nothing else of the card.

## Tendr, the vendor

A very simplified Stripe. Port `42040` live. Its README (`src/tendr/Tendr/README.md`) documents the API and the test cards for anyone who clones the repo.

`POST /v1/authorizations`, header `Idempotency-Key: <uuid>` (required; `400` problem if missing or not a UUID), body `{ "amountCents": 6120, "currency": "usd", "card": { "number": "4242424242424242" } }`.

- `201 Created` with `Location: /v1/authorizations/{id}` on first sight of the key; `200 OK` with the identical body on any replay of the same key, regardless of what the replay sent.
- Body: `{ "id": "<the key>", "status": "approved" | "declined", "reason": null | "card_declined" | "insufficient_funds" | "unknown_card", "amountCents": 6120, "currency": "usd", "cardLast4": "4242" }`.
- `400` problem when `amountCents` <= 0, when `currency` is not `usd`, or when `card` is missing.
- Card numbers are accepted with or without spaces.

Test cards, the whole list: `4242 4242 4242 4242` approves; `4000 0000 0000 0002` declines `card_declined`; `4000 0000 0000 9995` declines `insufficient_funds`; any other number declines `unknown_card`.

`GET /v1/authorizations/{id}`: `200` with the same body, `404` problem when unknown. `GET /ping` like the twins.

Store: Marten, schema `tendr` in the same PostgreSQL the twins use (a demo convenience; Tendr never reads `before` or `after`, and its own server is one connection string away). One event stream per authorization, stream id = the idempotency key: `AuthorizationRequested(Id, AmountCents, Currency, CardLast4)` then `CardApproved(Id)` or `CardDeclined(Id, Reason)`. A single-stream `Authorization` aggregate as an inline snapshot, so the GET and the replay check are document loads. No seed data.

## Before twin: REQUIRED structure

Built the way the layers ask for it. Application owns the port and the outcome; Infrastructure owns the adapter, the options, and the vendor's wire shapes; the handler awaits the port; the filter maps the exceptions. The port is named for the role, the adapter for the vendor.

- `Application/Common/Interfaces/IPaymentGateway.cs`: `Task<PaymentAuthorization> AuthorizeAsync(Guid orderId, decimal total, string cardNumber, CancellationToken cancellationToken)`.
- `Application/Payments/PaymentAuthorization.cs`: the Application-layer outcome (`AuthorizationId`, `Approved`, `DeclineReason`), so the handler never sees a vendor DTO.
- `Application/Payments/CardRequest.cs`: the command's card shape (the before twin keeps its `Request`/`Dto` suffixes).
- `Application/Common/Exceptions/PaymentDeclinedException.cs` and `PaymentUnavailableException.cs`.
- `Application/Orders/PaymentDto.cs`: `AuthorizationId`, `Status`, beside `OrderLineDto.cs`.
- `Domain/Enums/PaymentStatus.cs`: `AtPickup`, `Approved`, beside `CouponStatus.cs`.
- `Infrastructure/Payments/TendrPaymentGateway.cs`: the typed `HttpClient` adapter. `Idempotency-Key` header, maps `HttpRequestException` and the client timeout to `PaymentUnavailableException`, maps the vendor response to `PaymentAuthorization`.
- `Infrastructure/Payments/TendrOptions.cs` (`BaseUrl`, `TimeoutSeconds`), bound by hand in `DependencyInjection.cs` the way `RabbitMqOptions` is.
- `Infrastructure/Payments/TendrAuthorizationRequest.cs` and `TendrAuthorizationResponse.cs`: the vendor's wire shapes, Infrastructure's business.
- Edits: `PlaceOrderCommand` gains `CardRequest? Card`; `PlaceOrderCommandHandler` injects `IPaymentGateway` (eight dependencies) and, after the decide block, awaits the gateway when a card was sent, throws `PaymentDeclinedException` when not approved, and sets the order's payment fields before the existing save and publish; `Order` + `OrderConfiguration` gain `PaymentStatus` and `PaymentAuthorizationId` with a generated migration `AddOrderPayment` (excluded from counts); `OrderDto` gains `PaymentDto? Payment` (null means absent) and `OrderMappingProfile` maps it; `ApiExceptionFilterAttribute` maps the two exceptions to `402` and `503` problems; `OrdersController` documents `402` and `503`; `Infrastructure/DependencyInjection.cs` binds the options and registers `AddHttpClient<IPaymentGateway, TendrPaymentGateway>` with the base address and the two-second timeout; `appsettings.json` gains `Tendr:BaseUrl`.

## After twin: expected shape

- `Payments/Tendr.cs`, one new file: `TendrClient` (a typed client, the one constructor-injected class in the twin), the wire records `AuthorizeCard`, `Card`, `CardAuthorization`, and the twin's own `Payment(AuthorizationId, Status, Reason)`. No card means no `Payment`.
- `Program.cs`: `AddHttpClient<TendrClient>` with the base address from `Tendr:BaseUrl` and a two-second timeout.
- `Orders/Order.cs`: `PaymentStatus` and `PaymentAuthorizationId` on the entity, mapped in the same file.
- `Orders/PlaceOrder.cs`: `PlaceOrder` gains `Card? Card`; `PlacedOrder` gains `Payment? Payment` (omitted when null). `LoadAsync`, `Validate(PlaceOrder, PlaceOrderData)` and `Decide` do not change. A new rung `AuthorizeAsync` on the load leg runs `Decide` and calls Tendr only when a card was sent; guard four is a `Validate(Payment?)` overload; `Post` stores what the rungs above returned and still returns `(PlacedOrder, NotifyBaker)`. The chain must run `LoadAsync`, `Validate`, `AuthorizeAsync`, `Validate`, `Post`, in that order; verify on the generated code.
- `NotifyBaker.cs` and the outbox line in `Program.cs`: untouched.

## Shared contract scenarios

The 27 existing scenarios per twin are unchanged, byte for byte. New, in `tests/LayerCake.ContractTests`, against a Tendr host per twin collection on a real Kestrel socket:

`PaymentScenarios` (both twins):

1. `place_order_with_approved_card_returns_201_with_payment` (the payment member, `authorizationId` equal to `id`, `GET /orders/{id}` carries the same payment)
2. `placing_order_with_approved_card_produces_exactly_one_baker_task`
3. `place_order_without_card_has_no_payment` (raw POST and GET bodies never contain `payment`)
4. `place_order_with_declined_card_returns_402_and_creates_no_order` (Tendr recorded the decline under the would-be order id; that id is `404` on the twin and never gets a baker task)
5. `place_order_with_insufficient_funds_returns_402_with_reason`
6. `place_order_with_bad_coupon_and_declining_card_returns_422_without_calling_tendr` (Tendr holds no new authorization afterwards: the ordering is the claim)

`PaymentOutageScenarios` (both twins, a host pointed at a closed port):

7. `place_order_with_card_while_tendr_is_down_returns_503_and_creates_no_order`
8. `place_order_without_card_while_tendr_is_down_returns_201`

`tests/Tendr.Tests` covers Tendr itself: approve, the declines, unknown card, replay `200` with the same body, missing or malformed `Idempotency-Key` `400`, the other `400`s, GET `200`/`404`.

## Out of scope

Capture, refund, void, webhooks, card validation beyond the test-card table, storing any card data in the twins, retries, circuit breakers, resilience packages, a payment status lifecycle, and any change to the no-card path of `POST /orders`.
