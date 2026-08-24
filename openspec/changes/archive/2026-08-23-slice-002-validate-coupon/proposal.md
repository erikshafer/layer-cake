# Proposal: slice-002-validate-coupon

## Why

Slice 2 is the talk's Railway Oriented Programming act: three ordered checks (exists → active window → expiry) producing a four-status discriminated answer, shown as Wolverine idioms rather than explicit Result types. Its validation logic is also the ONE shared function PlaceOrder (slice 003) reuses, the repo's answer to "how do slices share logic?" The full rationale and settled contract live in the slice spec: [docs/slices/002-validate-coupon.md](../../../docs/slices/002-validate-coupon.md). Slice 001 is archived and green; this is next in the locked build order.

## What Changes

- Before twin: earnest Clean Architecture implementation of `GET /coupons/{code}` (Domain `Coupon` entity, Application query + handler + DTO, shared status evaluation behind a service interface, `ICouponRepository` + Infrastructure implementation + `CouponConfiguration`, `CouponsController`).
- After twin: `Features/ValidateCoupon.cs` Wolverine.Http endpoint plus `Coupons/Coupon.cs` Marten document, calling the shared pure validation function in `Features/Coupons/CouponValidation.cs`.
- The shared validation function on each side is deliberately visible and single-sourced; slice 003 will call it from the order guard chain.
- Shared contract suite: the slice's six Alba scenarios, run identically against both hosts.
- Shared idempotent seed data: three coupons (BDAY10 valid, SUMMER25 expired, HOLIDAY30 notYetActive) via relative date offsets so seeds never rot.
- Bookkeeping: before-twin file list appended to `docs/file-inventory.md`, decisions to `docs/build-log.md`.

## Capabilities

### New Capabilities

- `coupons`: validating a coupon code to a four-status discriminated envelope over the shared HTTP contract, covering both twins and the shared contract scenarios (the twins are one capability with two implementations).

### Modified Capabilities

None (`cakes` is untouched).

## Impact

- New code in `src/before/` (all four projects) and `src/after/LayerCake.Slices` (`Features/`, `Coupons/`); no changes to existing cake code paths.
- New scenario base class + twin subclasses and coupon seeding in `tests/LayerCake.ContractTests`; existing isolation/reset infrastructure extended to the new tables/documents.
- No new packages, no version bumps (freeze intact).

## Non-goals

- No coupon CRUD endpoints: coupons enter via seed data only.
- No redemption caps or tracking ("exhausted" is out of scope forever), no fixed-amount discounts, no stacking, no per-customer coupons (slice spec out-of-scope list).
- Nothing here may depend on slice 003 (fallback fence: this slice must stand alone as validate-only).
- No explicit Result types in the after twin (`IResult` mystery meat, `OneOf<>`); ROP is expressed as Wolverine idioms per charter non-negotiable 4.
