# Slice 002 — ValidateCoupon

**Why this slice is in the talk:** the Railway Oriented Programming act. Three ordered checks (exists → active window → expiry) producing a four-status discriminated answer, on date mechanics alone. The after twin shows ROP as Wolverine idioms (ordered guards, declarative flow), NOT explicit Result types (no `IResult` mystery meat, no `OneOf<>`; see CLAUDE.md non-negotiable 4). The validation logic here is also the shared function PlaceOrder reuses (slice 003), which is the repo's answer to "how do slices share logic?"

CritterMart source design: Orders `ValidateCoupon` (four-status discriminated answer, ordered checks).

**Fallback fence:** if PlaceOrder crowds the schedule, this slice stands alone as validate-only. Nothing here may depend on slice 003.

## Contract (settled 2026-08-23)

### GET /coupons/{code}

ALWAYS `200` with the discriminated envelope (DECIDED: envelope over status codes; validation is a question, and "unknown code" is a normal answer to a question, not a missing resource):

```json
{ "code": "SUMMER25", "status": "expired" }
{ "code": "BDAY10", "status": "valid", "percentOff": 10 }
```

- `status`: `"invalid" | "notYetActive" | "expired" | "valid"` (exactly these four; "exhausted" is out of scope forever: no caps, no redemption tracking)
- `percentOff` present ONLY when `status` is `valid`
- Check order is the ROP pipeline and must be observable in code on both sides: exists → active window (`startsAt` in the future → `notYetActive`) → expiry (`expiresAt` in the past → `expired`) → `valid`
- Discount model: `percentOff` only. No fixed-amount coupons, no polymorphic shapes.
- Code matching: case-insensitive lookup, canonical uppercase in responses.

No coupon-definition endpoint. Coupons enter the system via seed data only.

## The shared validation function (load-bearing for the talk)

ONE function evaluates a coupon to its status. Two call sites: this endpoint and PlaceOrder's guard chain (003). It must be deliberately, visibly shared, not duplicated and not buried:

- After twin: e.g., `Coupons/CouponValidation.cs`, a small pure static function `(Coupon?, DateTimeOffset now) -> CouponStatus`. The endpoint and the PlaceOrder guard both call it. Pure and clock-parameterized so tests need no time mocking.
- Before twin: the idiomatic equivalent, e.g., a `CouponService`/domain service consulted by both the query handler and (later) the order command handler, behind its interface, with the same status enum.

## Before twin — REQUIRED structure

Same earnestness rules as slice 001 (see its anti-shortcut list). Expected elements: `Domain/Entities/Coupon.cs`; `Application/Coupons/Queries/ValidateCoupon/...` (query + handler + DTO); the shared status evaluation in its service/domain home behind an interface; `ICouponRepository` + `Infrastructure` implementation + `CouponConfiguration`; `CouponsController`. Append the honest file list to `docs/file-inventory.md`.

## After twin — expected shape

- `Coupons/ValidateCoupon.cs`: `[WolverineGet("/coupons/{code}")]`, `IQuerySession`, calls the shared function, returns the envelope record.
- `Coupons/Coupon.cs`: Marten document, mutable class: `Code` (identity, uppercase), `PercentOff`, `StartsAt`, `ExpiresAt`.

## Shared contract scenarios

1. `valid_coupon_returns_valid_with_percent_off`
2. `unknown_coupon_returns_invalid` (still 200)
3. `not_yet_active_coupon_returns_notYetActive`
4. `expired_coupon_returns_expired`
5. `coupon_lookup_is_case_insensitive`
6. `percent_off_absent_unless_valid`

## Seed data (shared, idempotent)

| code | percentOff | startsAt | expiresAt | seeded status |
|---|---|---|---|---|
| BDAY10 | 10 | far past | far future | valid |
| SUMMER25 | 25 | far past | in the past | expired |
| HOLIDAY30 | 30 | in the future | far future | notYetActive |

All statuses achieved with date mechanics alone. Use relative offsets from now in the seed script (e.g., ±1 year) so seeds never rot.

## Out of scope

Coupon CRUD endpoints, redemption caps/tracking ("exhausted"), fixed-amount discounts, stacking, per-customer coupons.
