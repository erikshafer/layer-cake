# Proposal: slice-001-publish-browse-cakes

## Why

Slice 1 is the talk's Act 1 hook: one trivial write (PublishCake) traced through the before twin's full Clean Architecture ceremony, then the same feature as a single after-twin file, plus the BrowseCakes read beat. The full rationale and settled contract live in the slice spec: [docs/slices/001-publish-browse-cakes.md](../../../docs/slices/001-publish-browse-cakes.md). This is the first slice of the locked slate and nothing else can be built before it.

## What Changes

- Before twin: earnest Clean Architecture implementation of `POST /cakes`, `GET /cakes`, `GET /cakes/{id}` across Domain/Application/Infrastructure/WebApi, per the REQUIRED structure in the slice spec.
- After twin: the same three endpoints as Wolverine.Http + Marten feature files (`PublishCake.cs`, `BrowseCakes.cs`, `GetCake.cs`, `Cake` document).
- Shared contract suite: the slice's eight Alba scenarios, run identically against both hosts, with the shared error-shape assertion helper.
- Shared idempotent seed data (three cakes) loaded into both twins; test isolation resets and re-seeds both schemas.
- Bookkeeping: the before twin's honest file list appended to `docs/file-inventory.md` (a slide depends on it) and mid-build decisions recorded in `docs/build-log.md`.

## Capabilities

### New Capabilities

- `cakes`: publishing a cake and browsing/fetching cakes over the shared HTTP contract, covering both twins and the shared contract scenarios (the twins are one capability with two implementations).

### Modified Capabilities

None (first slice; no existing specs).

## Impact

- New projects/code in `src/before/` (four-project Clean Architecture solution) and `src/after/LayerCake.Slices`.
- New shared suite in `tests/LayerCake.ContractTests` referencing both hosts.
- PostgreSQL 17 via docker-compose; EF Core owns schema `before`, Marten owns schema `after`, one database.
- Version pins per `Directory.Packages.props` (MediatR 12.5.0, AutoMapper 14.0.0 deliberate; no Alba 9 beta).

## Non-goals

- No auth, no paging/filtering/sorting, no images, no cake update/delete, no soft deletes, no cart (slice spec out-of-scope list).
- No event sourcing (both twins state-stored; Marten as document store only), no frontend, one deployable per twin.
- No sabotage of the before twin and no collapsing of its required layers; no byte-identical error bodies (parity is status + content type + discoverable reason via the shared helper).
