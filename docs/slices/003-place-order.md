# Slice 003 — PlaceOrder

**Why this slice is in the talk:** the A-Frame act, plus the reliable-side-effect lesson. The after twin's handler is guard → pure decide → return (state change + cascaded message); Wolverine's outbox carries the "notify the baker" side effect. The before twin performs the same side effect inline mid-transaction, narrated in the talk as "imagine this line is SendGrid." The baker to-do surface makes the async effect synchronously observable in a demo (a table you can query, not a log to tail).

CritterMart source design: Orders `PlaceOrder` (handler returns HTTP result + cascaded messages via outbox). Cut from the source: DCB, composite boundaries, payment timeout. Keep: guard → decide → return.

**Fallback fence:** if this slice crowds the schedule, slice 002 stands alone (coupon collapses to validate-only) and the talk compresses per plan. Do not let this slice's scope creep backward into 001/002.

## Contract (settled 2026-08-23)

### POST /orders

Request (`couponCode` optional):

```json
{
  "lines": [ { "cakeId": "…", "quantity": 2 } ],
  "couponCode": "BDAY10"
}
```

Success `201 Created`, `Location: /orders/{id}`:

```json
{
  "id": "…",
  "lines": [ { "cakeId": "…", "name": "Chocolate Stout", "unitPrice": 34.00, "quantity": 2, "lineTotal": 68.00 } ],
  "subtotal": 68.00,
  "discount": 6.80,
  "total": 61.20,
  "couponCode": "BDAY10",
  "placedAt": "2026-09-10T15:04:05Z"
}
```

- `discount` is `0` and `couponCode` is absent when no coupon was sent.
- Money math: `lineTotal = unitPrice * quantity`; `subtotal = Σ lineTotal`; `discount = round(subtotal * percentOff / 100, 2, MidpointRounding.ToEven)`; `total = subtotal - discount`. Identical on both twins; the suite asserts the numbers.
- Prices are captured from the cake at order time (`unitPrice` snapshot), not joined live afterward.

Guard order (ROP-flavored, exactly this sequence; each guard's failure shape follows the parity rule in slice 001):

1. `400`: `lines` non-empty; every `quantity` >= 1
2. `422`: every `cakeId` exists (problem detail lists the offending ids)
3. `422`: `couponCode`, if present, evaluates to `valid` via the SAME shared validation function from slice 002 (problem detail carries the failing status: `invalid`, `notYetActive`, or `expired`)
4. Decide: compute totals, persist order, emit `NotifyBaker`

**DECIDED posture: a bad coupon FAILS the order (422). Never silently dropped.** Checkout is authoritative; the validate endpoint is advisory.

### GET /orders/{id}

- `200` (same body shape as the POST response) / `404`. No slides.

### GET /baker/tasks

- `200`: `[ { orderId, summary, createdAt } ]`
- Optional `?orderId={id}` filter so the suite can probe for one order's task.
- **The side effect is contract behavior**: placing an order produces exactly one baker task for that order. The suite polls this endpoint with a short timeout and does not know or care which twin is async. Exactly-once per order is asserted (poll, then assert count == 1).

## Before twin — REQUIRED structure

Same earnestness rules as slice 001. Expected elements: `Domain/Entities/Order.cs` (+ `OrderLine`), `Domain/Entities/BakerTask.cs`; `Application/Orders/Commands/PlaceOrder/...` (command + handler + validator); order DTOs + mapping profile; `IOrderRepository`, `IBakerTaskRepository` (or an `IBakerNotifier` service interface; pick the idiomatic ceremony) + Infrastructure implementations + configurations; `OrdersController`, `BakerController`. The handler writes the baker task INLINE in the same transaction as the order (this is the antipattern on display; add the one "imagine this is SendGrid" comment). Coupon check calls the slice 002 shared service. Append the honest file list to `docs/file-inventory.md`.

## After twin — expected shape

- `Orders/PlaceOrder.cs`: `PlaceOrder` record + static endpoint. Guards as `Validate`/`ValidateAsync` returning `ProblemDetails` / `WolverineContinue.NoProblems`, in the contract's order, reusing `CouponValidation` from 002. The endpoint method stays pure-ish: compute totals (ideally a small pure decide function that slides can show alone), `Store` the `Order` document, and RETURN the `NotifyBaker` message as a cascaded value (tuple with the response). Never send via an injected bus; never `SaveChangesAsync`. The outbox (Marten integration + `AutoApplyTransactions`) makes store-and-send atomic; say so in one comment, because that is the A-Frame beat.
- `Orders/NotifyBaker.cs`: `NotifyBaker` record + handler that stores the `BakerTask` document.
- `Orders/GetOrder.cs`: `[WolverineGet("/orders/{id}")]` with `[Entity(Required = true)]`.
- `Orders/GetBakerTasks.cs`: `IQuerySession` read with the optional filter.
- Documents: `Orders/Order.cs` (+ line type), `Orders/BakerTask.cs`, mutable classes.

## Shared contract scenarios

1. `place_order_returns_201_with_totals`
2. `place_order_without_coupon_has_zero_discount`
3. `place_order_with_empty_lines_returns_400`
4. `place_order_with_zero_quantity_returns_400`
5. `place_order_with_unknown_cake_returns_422_listing_ids`
6. `place_order_with_valid_coupon_applies_discount_math`
7. `place_order_with_expired_coupon_returns_422_with_status`
8. `place_order_with_not_yet_active_coupon_returns_422_with_status`
9. `get_order_by_id_returns_200_with_same_shape`
10. `get_missing_order_returns_404`
11. `placing_order_produces_exactly_one_baker_task` (polled with timeout)

## Out of scope

Payment (not even stubbed), inventory/stock, order status lifecycle, cancellation, cart, email/SendGrid (the baker task IS the stand-in), event sourcing.
