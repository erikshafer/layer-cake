# coupons Specification

## Purpose

Validating a coupon code to a four-status discriminated answer over the shared HTTP contract. Covers both twins and the shared contract scenarios; the twins are one capability with two implementations. Contract source of truth: `docs/slices/002-validate-coupon.md`.

## Requirements

### Requirement: Validate a coupon code

Both twins SHALL expose `GET /coupons/{code}` returning ALWAYS `200` with a camelCase discriminated envelope `{ code, status }` where `status` is exactly one of `"invalid"`, `"notYetActive"`, `"expired"`, or `"valid"`. An unknown code SHALL return the envelope with status `invalid`, never `404` (validation is a question; an unknown code is a normal answer). The `percentOff` field SHALL be present ONLY when `status` is `valid`, and absent (not null) for every other status. The discount model is `percentOff` only; there SHALL be no coupon-definition endpoint (coupons enter via seed data only).

#### Scenario: unknown_coupon_returns_invalid

- **WHEN** a client GETs `/coupons/{code}` with a code that matches no coupon
- **THEN** the response is exactly `200` with body `{ "code": <canonical code>, "status": "invalid" }` and no `percentOff` field

#### Scenario: percent_off_absent_unless_valid

- **WHEN** a client GETs `/coupons/{code}` for codes whose statuses are `invalid`, `notYetActive`, and `expired`
- **THEN** each `200` envelope omits the `percentOff` field entirely

### Requirement: Coupon status from date mechanics

Coupon status SHALL be determined by date mechanics alone, evaluated in this order: exists → active window (`startsAt` in the future → `notYetActive`) → expiry (`expiresAt` in the past → `expired`) → `valid`. The three shared seed coupons (BDAY10 10% far past → far future, SUMMER25 25% far past → past, HOLIDAY30 30% future → far future) SHALL load identically into both twins, be idempotently re-seedable, and use relative offsets from now (e.g., ±1 year) so seeds never rot. There SHALL be no redemption caps or tracking ("exhausted" is not a status).

#### Scenario: valid_coupon_returns_valid_with_percent_off

- **WHEN** a client GETs `/coupons/BDAY10` against a freshly seeded host
- **THEN** the response is exactly `200` with body `{ "code": "BDAY10", "status": "valid", "percentOff": 10 }`

#### Scenario: not_yet_active_coupon_returns_notYetActive

- **WHEN** a client GETs `/coupons/HOLIDAY30` against a freshly seeded host
- **THEN** the response is exactly `200` with body `{ "code": "HOLIDAY30", "status": "notYetActive" }` and no `percentOff` field

#### Scenario: expired_coupon_returns_expired

- **WHEN** a client GETs `/coupons/SUMMER25` against a freshly seeded host
- **THEN** the response is exactly `200` with body `{ "code": "SUMMER25", "status": "expired" }` and no `percentOff` field

### Requirement: Case-insensitive code matching

Coupon lookup SHALL be case-insensitive, and the `code` echoed in the envelope SHALL be the canonical uppercase form regardless of the casing the client sent.

#### Scenario: coupon_lookup_is_case_insensitive

- **WHEN** a client GETs `/coupons/bday10` against a freshly seeded host
- **THEN** the response is exactly `200` with body `{ "code": "BDAY10", "status": "valid", "percentOff": 10 }`
