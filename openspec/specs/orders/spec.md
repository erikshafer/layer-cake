# orders Specification

## Purpose

Placing a guarded, coupon-aware order with price-snapshot totals, reading it back, and the observable baker-notification side effect surfaced as baker tasks. Covers both twins and the shared contract scenarios; the twins are one capability with two implementations. Contract source of truth: `docs/slices/003-place-order.md`.

## Requirements

### Requirement: Place an order with computed totals

Both twins SHALL expose `POST /orders` accepting a camelCase body `{ lines: [ { cakeId, quantity } ], couponCode? }` and, on success, return exactly `201 Created` with a `Location: /orders/{id}` header and a camelCase body `{ id, lines, subtotal, discount, total, couponCode?, placedAt }` where each response line is `{ cakeId, name, unitPrice, quantity, lineTotal }`. Money math SHALL be identical on both twins: `lineTotal = unitPrice * quantity`, `subtotal = Σ lineTotal`, `discount = round(subtotal * percentOff / 100, 2, MidpointRounding.ToEven)`, `total = subtotal - discount`. `unitPrice` and `name` SHALL be captured from the cake at order time (snapshot), not joined live afterward. When no coupon was sent, `discount` SHALL be `0` and `couponCode` SHALL be absent from the response body (not null).

#### Scenario: place_order_returns_201_with_totals

- **WHEN** a client POSTs `/orders` with lines referencing seeded cakes (e.g., quantity 2 of a known cake)
- **THEN** the response is exactly `201` with a `Location: /orders/{id}` header and a body whose `lines` carry the snapshot `name` and `unitPrice`, and whose `lineTotal`, `subtotal`, `discount`, and `total` match the money math exactly

#### Scenario: place_order_without_coupon_has_zero_discount

- **WHEN** a client POSTs `/orders` with valid lines and no `couponCode`
- **THEN** the `201` body has `discount` of `0`, `total` equal to `subtotal`, and no `couponCode` field in the raw body

### Requirement: Ordered request guards

`POST /orders` SHALL apply guards in exactly this order, short-circuiting at the first failure, with each failure shape following the slice 001 error-parity rule (status + content type + reason discoverable, asserted by the shared helper): (1) `400` when `lines` is empty or any `quantity` is less than 1; (2) `422` when any `cakeId` does not exist, with the problem detail listing the offending ids; (3) `422` when a present `couponCode` does not evaluate to `valid`. Failing requests SHALL NOT create an order or a baker task.

#### Scenario: place_order_with_empty_lines_returns_400

- **WHEN** a client POSTs `/orders` with an empty `lines` array
- **THEN** the response is exactly `400` with the shared error shape, and no order is created

#### Scenario: place_order_with_zero_quantity_returns_400

- **WHEN** a client POSTs `/orders` with a line whose `quantity` is `0`
- **THEN** the response is exactly `400` with the shared error shape, and no order is created

#### Scenario: place_order_with_unknown_cake_returns_422_listing_ids

- **WHEN** a client POSTs `/orders` with a line referencing a `cakeId` that does not exist
- **THEN** the response is exactly `422` with the shared error shape and the problem detail lists the offending cake id(s)

### Requirement: Coupon at checkout is authoritative

When `couponCode` is present, `POST /orders` SHALL evaluate it with the SAME shared validation logic as `GET /coupons/{code}` (slice 002): a coupon evaluating to `valid` applies its `percentOff` to the discount math and echoes the canonical code in the response; any other status FAILS the order with exactly `422`, the problem detail carrying the failing status (`invalid`, `notYetActive`, or `expired`). A bad coupon SHALL never be silently dropped: checkout is authoritative, the validate endpoint is advisory.

#### Scenario: place_order_with_valid_coupon_applies_discount_math

- **WHEN** a client POSTs `/orders` with valid lines and `couponCode` `"BDAY10"` (seeded valid, 10%)
- **THEN** the response is exactly `201` with `discount` equal to `round(subtotal * 10 / 100, 2, ToEven)`, `total = subtotal - discount`, and `couponCode` `"BDAY10"` echoed in the body

#### Scenario: place_order_with_expired_coupon_returns_422_with_status

- **WHEN** a client POSTs `/orders` with valid lines and `couponCode` `"SUMMER25"` (seeded expired)
- **THEN** the response is exactly `422` with the shared error shape, the failing status `expired` discoverable in the problem detail, and no order is created

#### Scenario: place_order_with_not_yet_active_coupon_returns_422_with_status

- **WHEN** a client POSTs `/orders` with valid lines and `couponCode` `"HOLIDAY30"` (seeded not yet active)
- **THEN** the response is exactly `422` with the shared error shape, the failing status `notYetActive` discoverable in the problem detail, and no order is created

### Requirement: Read an order back

Both twins SHALL expose `GET /orders/{id}` returning exactly `200` with the same camelCase body shape as the `POST /orders` success response for an existing order, and exactly `404` (shared error-parity shape) for an unknown id.

#### Scenario: get_order_by_id_returns_200_with_same_shape

- **WHEN** a client places an order and then GETs `/orders/{id}` with the returned id
- **THEN** the response is exactly `200` and the body matches the shape and values of the placement response

#### Scenario: get_missing_order_returns_404

- **WHEN** a client GETs `/orders/{id}` with an id that matches no order
- **THEN** the response is exactly `404` following the shared error-parity rule

### Requirement: Placing an order produces exactly one baker task

Placing an order SHALL produce exactly one baker task for that order, observable via `GET /baker/tasks` returning exactly `200` with a camelCase array `[ { orderId, summary, createdAt } ]`, filterable with an optional `?orderId={id}` query parameter. The task MAY appear asynchronously after the `201`, so observers SHALL poll with a short timeout; once present, the count for that order SHALL be exactly one (never zero after settling, never duplicated). The contract does not reveal which twin produces the task synchronously.

#### Scenario: placing_order_produces_exactly_one_baker_task

- **WHEN** a client places an order successfully and polls `GET /baker/tasks?orderId={id}` with a short timeout
- **THEN** exactly one task for that order appears, with its `orderId` matching the placed order and a non-empty `summary` and `createdAt`
