# Proposal: slice-003-place-order

## Why

Slice 3 is the talk's A-Frame act and its reliable-side-effect lesson: the after twin's handler is guard → pure decide → return (state change + cascaded `NotifyBaker` message carried by Wolverine's outbox), while the before twin performs the same side effect inline mid-transaction ("imagine this line is SendGrid"). It also cashes in slice 002's promise: the ONE shared coupon validation function is reused in the order guard chain. Full rationale and settled contract: [docs/slices/003-place-order.md](../../../docs/slices/003-place-order.md). Slices 001 and 002 are archived and green; this is the last slice in the locked build order.

## What Changes

- Before twin: earnest Clean Architecture implementation of `POST /orders`, `GET /orders/{id}`, and `GET /baker/tasks` (Domain `Order`/`OrderLine`/`BakerTask` entities, Application command + handler + validator + query handlers, DTOs + mapping, repositories + Infrastructure implementations + configurations + migration, `OrdersController` + `BakerController`). The handler writes the baker task inline in the same transaction as the order (the antipattern on display, with the one "imagine this is SendGrid" comment).
- After twin: `Features/PlaceOrder.cs` (ordered guards as `Validate`/`ValidateAsync`, pure totals decide, `Store` + cascaded `NotifyBaker` return), `Features/NotifyBakerHandler.cs` (stores the `BakerTask` document), `Features/GetOrder.cs` (`[Entity(Required = true)]`), `Features/GetBakerTasks.cs`, plus `Orders/Order.cs` and `Orders/BakerTask.cs` Marten documents.
- Both twins reuse slice 002's shared coupon validation in guard 3: a bad coupon FAILS the order with 422 (checkout is authoritative; the validate endpoint is advisory).
- Shared contract suite: the slice's eleven Alba scenarios, run identically against both hosts, including the polled exactly-one-baker-task-per-order assertion that does not know or care which twin is async.
- Bookkeeping: before-twin file list appended to `docs/file-inventory.md`, decisions to `docs/build-log.md`.

## Capabilities

### New Capabilities

- `orders`: placing an order (guarded, coupon-aware totals math, price snapshot at order time), reading an order back, and the observable baker-notification side effect surfaced as baker tasks, over the shared HTTP contract. Covers both twins and the shared contract scenarios (the twins are one capability with two implementations).

### Modified Capabilities

None. `cakes` and `coupons` requirements are untouched: PlaceOrder consumes cakes and the shared coupon validation function, but `POST /cakes`, `GET /cakes*`, and `GET /coupons/{code}` behavior does not change.

## Impact

- New code in `src/before/` (all four projects, one new EF Core migration) and `src/after/LayerCake.Slices` (`Features/`, `Orders/`); slice 001/002 code paths are read, not modified (cake lookup for price snapshots, coupon validation for guard 3).
- New scenario base class + twin subclasses in `tests/LayerCake.ContractTests`; existing isolation/reset infrastructure extended to order and baker-task tables/documents.
- No new packages, no version bumps (freeze intact); Wolverine's Marten integration + `AutoApplyTransactions` already provide the outbox the after twin cascades through.

## Non-goals

- No payment (not even stubbed), no inventory/stock, no order status lifecycle, no cancellation, no cart (slice spec out-of-scope list).
- No email/SendGrid integration: the baker task IS the stand-in, chosen so the side effect is synchronously observable in a demo.
- No event sourcing; both twins stay state-stored per charter non-negotiable 3.
- No silent coupon drop: the decided posture (422 on a bad coupon) is settled in the slice spec and not revisited here.
- No backward scope creep into slices 001/002 (fallback fence: 002 stands alone if this slice is cut).
