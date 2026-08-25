# Design: slice-003-place-order

## Context

Slices 001 and 002 left everything bootstrapped: hosts, schemas, migrations, Alba fixtures, per-twin collections, reset/seed pipeline, the error-shape helper (`ProblemDetailsAssertions`), and the shared coupon validation on both sides (`CouponValidation.Evaluate` on the after twin, the coupon domain service on the before twin). Behavior and structure are settled in `docs/slices/003-place-order.md`; see proposal.md for motivation. This document settles only what the slice spec leaves open: the before twin's baker ceremony (the spec offers an either/or), exactly-once semantics for the after twin's asynchronous baker task, how the after twin's guards share loaded data with the decide step, whether order totals are stored or recomputed, and the contract suite's polling mechanics.

## Goals / Non-Goals

**Goals:**

- Settle the before twin's baker abstraction so the inventory is honest (no padding) and the "imagine this is SendGrid" beat still lands.
- Settle exactly-once for the after twin's baker task despite at-least-once outbox delivery.
- Settle the after twin's guard-to-decide data flow so cakes and the coupon load once and the decide function stays pure.
- Settle totals persistence so `GET /orders/{id}` returns the placement values byte-for-byte without recompute drift.
- Settle the suite's poll-with-timeout idiom for the side-effect scenario.

**Non-Goals:**

- Anything re-litigating the settled contract, guard order, 422-on-bad-coupon posture, before-twin structure, or after-twin shape (slice spec owns those).
- Payment, inventory, lifecycle, cancellation, cart, email, event sourcing (proposal non-goals).

## Decisions

### Before twin: one `IBakerTaskRepository`, no separate notifier

The slice spec offers `IBakerTaskRepository` OR `IBakerNotifier`; pick the repository. `GET /baker/tasks` needs a read path, and the before twin's established idiom is a repository per aggregate (`ICakeRepository`, `ICouponRepository`), so the repository is needed regardless; adding a notifier interface on top would be padding, and the charter forbids padding as explicitly as it forbids collapsing layers. The PlaceOrder handler calls `bakerTaskRepository` inline, in the same transaction as the order write, and the one "imagine this line is SendGrid" comment sits on that call site. The antipattern on display is the in-transaction side effect itself, not the shape of the abstraction. Alternative considered: `IBakerNotifier` for the write plus a repository for the read; rejected as two abstractions over one tiny table.

### Before twin: one transaction, one save

The order and its baker task commit atomically in a single `SaveChangesAsync`, whatever unit-of-work shape slices 001/002 established (if the existing repositories self-save, the handler composes them so exactly one save covers both writes; adjust to the established idiom rather than inventing a new one). "Same transaction" is the talk claim, so it must be literally true.

### After twin: `BakerTask` identity = order id, making the handler idempotent

Wolverine's durable outbox is at-least-once: a retried `NotifyBaker` delivery must not create a second task, because the contract asserts exactly one per order. The `BakerTask` Marten document uses the order's id as its own identity, so `session.Store` is an upsert and redelivery is naturally idempotent — no dedupe table, no distributed lock, one line of design. The before twin has no duplication risk (inline same-transaction write) and keeps a conventional `Id` + `OrderId` entity per earnest EF idiom; the contract only asserts count, not key shape. Alternative considered: querying for an existing task before storing; rejected as a read-modify-write race that the identity upsert avoids for free.

### After twin: guards load once, decide stays pure

`ValidateAsync` loads the referenced cakes and (when present) the coupon a single time and returns them alongside the `ProblemDetails` verdict as a compound return, so Wolverine passes the loaded documents into the endpoint method and the decide step needs no session. The decide function itself is a small pure static (`lines + cakes + percentOff -> totals`) that a slide can show alone. If the compound Validate-with-pass-through idiom fights Wolverine's middleware conventions during the build, the fallback is a second `LoadManyAsync` in the endpoint method — a demo-sized cost, recorded in `docs/build-log.md` if taken, never a reason to inject the session into the decide function. Coupon guard reuses `CouponValidation.Evaluate(coupon, DateTimeOffset.UtcNow)` verbatim; the failing status lands in the `ProblemDetails.Detail` so the suite's reason probe finds `expired` / `notYetActive` / `invalid`.

### Totals are stored at placement, never recomputed

The order document/entity persists everything the response shows: snapshot lines (`cakeId`, `name`, `unitPrice`, `quantity`, `lineTotal`) plus `subtotal`, `discount`, `total`, optional `couponCode`, and `placedAt`. `GET /orders/{id}` reads and returns stored values; nothing is rejoined or recomputed, so later cake price edits cannot drift the read model and the get-matches-post scenario holds by construction. Money is `decimal` end to end; the single rounding point is the discount (`MidpointRounding.ToEven`, per contract). `couponCode` absence-not-null reuses slice 002's omit-when-null attribute pattern on the response shape.

### Contract suite: poll until present, then assert exactly one

The baker-task scenario polls `GET /baker/tasks?orderId={id}` on a short interval (~250 ms) with a hard timeout (~5 s), succeeds the wait as soon as the array is non-empty, then asserts the count is exactly 1 with the matching `orderId`. The helper lives in the contract suite and is twin-agnostic: it does not know the before twin will pass on the first poll and the after twin after the outbox relay. Timing out is a test failure with the last body in the message. Alternative considered: asserting quiescence (poll again to prove no second task ever arrives); rejected as sleep-based flakiness — the upsert-identity decision already makes a duplicate impossible on the only async path.

## Risks / Trade-offs

- [After twin's async task makes the suite timing-sensitive] → Local durable queue delivery is fast (well under the 5 s timeout) and the poll helper's failure message carries the last response body for diagnosis; the interval/timeout are constants in one place if CI needs tuning.
- [Compound Validate-with-pass-through may not bind the way the design assumes] → Explicit fallback decided above (re-load in the endpoint method), recorded in the build log if taken; the contract cannot tell the difference.
- [Guard order regressions hide behind combined-invalid requests] → The suite's guard scenarios each isolate one failure; the 422 coupon scenarios use valid lines so a 400 can never mask them, pinning the short-circuit order.
- [Storing totals denormalizes money math into two code paths (compute at POST, trust at GET)] → That is the point (price snapshot posture, settled in the slice spec); the get-matches-post scenario pins it on both twins.

## Open Questions

None. Anything smaller than these decisions gets recorded in `docs/build-log.md` as it is made.
