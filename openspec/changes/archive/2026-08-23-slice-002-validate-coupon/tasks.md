# Tasks: slice-002-validate-coupon

Contract, structure, and scenarios: `docs/slices/002-validate-coupon.md`. Decisions: `design.md`.

## 1. Before twin (earnest Clean Architecture)

- [x] 1.1 Implement `Domain/Entities/Coupon.cs` (`Code` canonical uppercase, `PercentOff`, `StartsAt`, `ExpiresAt`) and the `CouponStatus` enum in Domain per design.md; verify the Domain project builds with no dependencies on other layers
- [x] 1.2 Implement the shared status evaluation as a coupon domain service behind an interface: the ordered exists → active window → expiry → valid pipeline as `(Coupon?, DateTimeOffset now) -> CouponStatus`, consuming `IDateTimeProvider` (new Application interface + Infrastructure implementation returning `DateTimeOffset.UtcNow`) per design.md; verify the check order is readable in one place (slice 003 will reuse this service)
- [x] 1.3 Implement `Application/Coupons/Queries/ValidateCoupon/` (query + handler + `CouponValidationDto` with `int? PercentOff` omit-when-null per design.md) plus `ICouponRepository` with case-insensitive lookup via uppercased input; verify the handler consults the shared service, not its own date logic
- [x] 1.4 Implement Infrastructure: `CouponConfiguration` (unique index on `Code`), `CouponRepository`, DbContext registration, and a new EF Core migration on schema `before`; verify `Database.Migrate()` applies cleanly against the compose database
- [x] 1.5 Implement `WebApi/Controllers/CouponsController.cs` (thin, delegates to MediatR, always 200, camelCase); verify via Swagger: valid, invalid, notYetActive, expired, lowercase-input lookups all return the correct envelopes with `percentOff` absent unless valid

## 2. After twin (Wolverine + Marten slices)

- [x] 2.1 Implement `Coupons/Coupon.cs` Marten document (plain mutable class, `Code` string identity canonical uppercase, `PercentOff`, `StartsAt`, `ExpiresAt`) and verify the after twin builds
- [x] 2.2 Implement `Features/Coupons/CouponValidation.cs`: the ONE pure static function `(Coupon?, DateTimeOffset now) -> CouponStatus` with the three ordered checks visible in its body (no Result types per charter non-negotiable 4); verify it is clock-parameterized with no session or IO dependency
- [x] 2.3 Implement `Features/ValidateCoupon.cs`: `[WolverineGet("/coupons/{code}")]`, `IQuerySession` load by uppercased code, passes `DateTimeOffset.UtcNow` to the shared function, returns the envelope record with `int? PercentOff` omit-when-null per design.md; verify via Swagger: all four statuses, case-insensitive lookup, `percentOff` absent unless valid

## 3. Shared seeds and contract scenarios

- [x] 3.1 Extend the idempotent seeding with the three shared coupons (BDAY10 10% −1y/+1y, SUMMER25 25% −1y/−1d, HOLIDAY30 30% +1d/+1y — relative offsets from now per the slice spec) through each twin's own persistence idiom, and extend the per-scenario-class reset to cover coupon tables/documents; verify seeding twice leaves exactly three coupons per twin
- [x] 3.2 Implement the six shared scenarios in an abstract base class with twin subclasses (existing fixture pattern), asserting exact `200`, camelCase envelope, and raw-body absence of `"percentOff"` where required: `valid_coupon_returns_valid_with_percent_off`, `unknown_coupon_returns_invalid`, `not_yet_active_coupon_returns_notYetActive`, `expired_coupon_returns_expired`, `coupon_lookup_is_case_insensitive`, `percent_off_absent_unless_valid`; verify each runs against both hosts (12 green results)

## 4. Verification and bookkeeping

- [x] 4.1 Run `docker compose up -d` then `dotnet test` from clean and verify the full shared suite (slices 001 + 002) is green on BOTH hosts in one run (definition of done)
- [x] 4.2 Append the before twin's honest per-slice file list and count to `docs/file-inventory.md` (a slide depends on the real number)
- [x] 4.3 Record mid-build decisions in `docs/build-log.md`, including the design.md calls (per-twin clock sourcing, omit-null `percentOff`, uppercase-identity lookup) as implemented
