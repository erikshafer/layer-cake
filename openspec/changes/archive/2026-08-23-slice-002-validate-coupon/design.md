# Design: slice-002-validate-coupon

## Context

Slice 001's infrastructure is all in place (hosts, schemas, migrations, Alba fixtures, per-twin collections, reset/seed pipeline, error-shape helper), so this slice adds no bootstrap. Behavior and structure are settled in `docs/slices/002-validate-coupon.md`; see proposal.md for motivation. This document settles only the small mechanics the slice spec leaves open: who supplies the clock on each side, how `percentOff` stays absent (not null), and how case-insensitive lookup meets canonical-uppercase identity.

## Goals / Non-Goals

**Goals:**

- Settle clock sourcing on both twins so the shared validation function stays pure and tests need no time mocking.
- Settle the envelope serialization so `percentOff` is absent (never `null`) unless `valid`, identically on both twins.
- Settle coupon identity and lookup so case-insensitivity is achieved without collation tricks.

**Non-Goals:**

- Anything re-litigating the settled contract, before-twin structure, or after-twin shape (those live in the slice spec).
- Anticipating slice 003's reuse beyond keeping the validation function pure and visible (fallback fence: this slice stands alone).

## Decisions

### Clock: pure function takes `now`; each twin supplies it idiomatically

The shared evaluation is `(Coupon?, DateTimeOffset now) -> CouponStatus` on both sides. The after twin's endpoint passes `DateTimeOffset.UtcNow` inline — no clock abstraction, which is exactly the ceremony contrast the talk wants. The before twin injects an `IDateTimeProvider` (Application-layer interface, Infrastructure implementation returning `DateTimeOffset.UtcNow`) into the coupon service, because that is what an earnest 2019 Clean Architecture review expects and it honestly adds two files to the inventory. Tests need no time control on either side: seed dates sit ±1 year from now, so real UtcNow always lands inside the intended status windows. Alternative considered: .NET `TimeProvider` on both sides; rejected because the after twin doesn't need any abstraction and the before twin's period idiom is the hand-rolled interface.

### `percentOff` absent via nullable property + omit-null serialization

One envelope shape per twin with `int? PercentOff`, set only when `valid`, serialized with omit-when-null (`[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]` or the twin's equivalent serializer setting scoped to the DTO). The contract suite asserts the raw body does NOT contain `"percentOff"` for non-valid statuses, which pins absence rather than null on both twins. `percentOff` is an `int` (whole percentages; seed values 10/25/30, matching the slice spec's JSON examples). Alternative considered: distinct response types per status; rejected as polymorphic-shape complexity the slice spec explicitly fences out.

### Canonical uppercase identity; lookup uppercases the input

Coupon codes are stored uppercase on both sides and treated as the identity: the Marten document uses `Code` (string) as its identity, and the EF entity carries a unique index on its uppercase `Code`. Case-insensitive lookup is `input.ToUpperInvariant()` before the fetch — one normalization at the edge, no `ILIKE`, no `citext`, no custom collations. The envelope echoes the stored (canonical) code. This also gives PlaceOrder (slice 003) a single obvious lookup rule to reuse.

### Ordered guards are the visible ROP pipeline, not a Result type

The shared function's body reads as the three ordered checks (exists → not yet active → expired → valid) returning a `CouponStatus` enum; the after twin exposes it from `Features/Coupons/CouponValidation.cs`, the before twin from the coupon service behind its interface. No `OneOf<>`, no `IResult`, per charter non-negotiable 4. The enum lives in Domain on the before twin (it is domain vocabulary the service interface returns) and next to the shared function on the after twin.

## Risks / Trade-offs

- [Seed dates drift relative to test-run time] → Offsets are ±1 year from `DateTimeOffset.UtcNow` at seed time, re-seeded per scenario-class run by the existing fixtures; no boundary sits near now.
- [Marten string-identity casing mismatch hides documents] → Codes are uppercased once at seed/lookup edges; the contract scenario `coupon_lookup_is_case_insensitive` pins the behavior on both twins.
- [Serializer-level omit-null accidentally leaks into other endpoints] → Scope the omit-null to the envelope DTO (attribute), not the host-wide serializer options; slice 001's cake bodies have no nullable fields today, but the scoped attribute keeps it that way by construction.

## Open Questions

None. Anything not settled here or in the slice spec is minor enough to record in `docs/build-log.md` as it is decided.
