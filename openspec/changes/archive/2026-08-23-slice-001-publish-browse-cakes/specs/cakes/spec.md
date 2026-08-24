# Delta: cakes (slice-001-publish-browse-cakes)

## Purpose

Publishing a cake to the bakery's catalog and browsing/fetching cakes over the shared HTTP contract. Covers both twins and the shared contract scenarios; the twins are one capability with two implementations. Contract source of truth: `docs/slices/001-publish-browse-cakes.md`.

## ADDED Requirements

### Requirement: Publish a cake

Both twins SHALL expose `POST /cakes` accepting a camelCase JSON body `{ name, description, price }`. On success the response SHALL be `201 Created` with a `Location: /cakes/{id}` header and body `{ id, name, description, price, publishedAt }`, where `id` is a server-generated Guid and `publishedAt` is ISO 8601 UTC. The endpoint SHALL reject a missing or empty `name` and a non-positive `price` with `400`, and a duplicate cake name with `409`. Failure responses SHALL be `application/problem+json` with the offending field or reason discoverable in the body (error-shape parity rule; bodies are not byte-identical across twins).

#### Scenario: publish_cake_returns_201_with_location_and_body

- **WHEN** a client POSTs `{ "name": "Chocolate Stout", "description": "Six layers, no mercy", "price": 34.00 }` to `/cakes` with `Content-Type: application/json`
- **THEN** the response is exactly `201` with a `Location` header pointing at `/cakes/{id}` and a camelCase body containing `id`, `name`, `description`, `price`, and `publishedAt`

#### Scenario: publish_cake_with_missing_name_returns_400

- **WHEN** a client POSTs a cake body whose `name` is missing or empty
- **THEN** the response is exactly `400` with `application/problem+json` and the `name` field discoverable as the reason

#### Scenario: publish_cake_with_nonpositive_price_returns_400

- **WHEN** a client POSTs a cake body whose `price` is zero or negative
- **THEN** the response is exactly `400` with `application/problem+json` and the `price` field discoverable as the reason

#### Scenario: publish_cake_with_duplicate_name_returns_409

- **WHEN** a client POSTs a cake whose `name` matches an already-published cake
- **THEN** the response is exactly `409` with `application/problem+json` and the duplicate-name reason discoverable in the body

### Requirement: Browse cakes

Both twins SHALL expose `GET /cakes` returning `200` with a camelCase JSON array `[ { id, name, description, price } ]`. There SHALL be no paging, filtering, or sorting parameters. The three shared seed cakes (Classic Yellow 24.00, Chocolate Stout 34.00, Lemon Chiffon 28.00) SHALL load identically into both twins (same names, descriptions, and prices; ids may differ per twin) and be idempotently re-seedable.

#### Scenario: browse_returns_seeded_cakes

- **WHEN** a client GETs `/cakes` against a freshly seeded host
- **THEN** the response is exactly `200` with an array containing the three seed cakes by name, description, and price

#### Scenario: browse_includes_newly_published_cake

- **WHEN** a client publishes a new cake and then GETs `/cakes`
- **THEN** the response is exactly `200` and the array includes the newly published cake

### Requirement: Get cake by id

Both twins SHALL expose `GET /cakes/{id}` returning `200` with the same item shape as the browse array for an existing cake, and `404` for a missing cake (never an empty `204`). The `404` SHALL be `application/problem+json` per the error-shape parity rule.

#### Scenario: get_cake_by_id_returns_200

- **WHEN** a client GETs `/cakes/{id}` for a cake discovered via browse
- **THEN** the response is exactly `200` with body `{ id, name, description, price }`

#### Scenario: get_missing_cake_returns_404

- **WHEN** a client GETs `/cakes/{id}` with a Guid that matches no cake
- **THEN** the response is exactly `404`
