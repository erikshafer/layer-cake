# Design: slice-001-publish-browse-cakes

## Context

First slice of the locked slate, so it carries the one-time bootstrap: solution file, `docker-compose.yml` (PostgreSQL 17), `Directory.Packages.props` pins, the before twin's four projects, the after twin's single project, and the shared contract-test project. Behavior and structure are settled in `docs/slices/001-publish-browse-cakes.md`; see proposal.md for motivation. This document only settles the open questions CLAUDE.md defers to this slice.

## Goals / Non-Goals

**Goals:**

- Settle the schema-management approach for the before twin's EF Core `before` schema.
- Settle Marten stored-JSON casing so `GET` reads can safely use camelCase responses.
- Fix the test-host and isolation approach for a suite that runs identical scenarios against two hosts.

**Non-Goals:**

- Anything re-litigating the settled contract, before-twin structure, or after-twin shape (those live in the slice spec).
- Production deployment concerns; both twins are demo hosts.

## Decisions

### EF Core migrations, not EnsureCreated (before twin)

Per CLAUDE.md's stated leaning: a sincere Clean Architecture team ships migrations, and the before twin's earnestness is non-negotiable. `dotnet ef migrations` output lives in `Infrastructure/Persistence/Migrations`; hosts and tests apply migrations on startup (`Database.Migrate()` from the WebApi in Development, and from the Alba fixture before the suite runs). Alternative considered: `EnsureCreated()` is less ceremony but is exactly the kind of demo shortcut the talk promises not to take, and it cannot coexist with migrations later. Record the decision in `docs/build-log.md` and clear the CLAUDE.md open item when the slice lands.

### Marten camelCase casing set explicitly upfront (after twin)

Configure `opts.UseSystemTextJsonForSerialization(casing: Casing.CamelCase)` in the after twin's Marten setup from the start rather than verifying default casing first. Rationale: deterministic camelCase stored JSON makes `Marten.AspNetCore` streamed results (`StreamOne<T>` and friends) safe for reads in this and later slices, and the contract suite pins camelCase on the wire either way. Alternative considered: rely on Marten defaults and verify; rejected because an explicit setting removes the verification burden and cannot silently drift.

### One Alba host fixture per twin, shared scenario base classes

Scenarios live in abstract base classes in `tests/LayerCake.ContractTests`; twin subclasses supply an `IAlbaHost` per host (xUnit class fixtures, one per twin). Test isolation resets BOTH sides before each scenario class run: delete rows in the EF `before` schema and `CleanAllMartenDataAsync()` on `after`, then re-seed the three shared cakes through each twin's own persistence idiom. Seeding is idempotent so the live-demo hosts can also call it on startup.

### Error-shape parity helper lives in the shared suite

One small assertion helper pins exact status code, `application/problem+json` content type, and that the offending field/reason appears in the body text. Bodies are deliberately not byte-identical; the README owns that as a laboratory decision (already settled 2026-08-23, restated here only because the helper is built in this slice).

## Risks / Trade-offs

- [Two hosts in one test process contend for one database] → Schema separation (`before`/`after`) means resets never cross twins; scenario classes within a twin run sequentially via xUnit collection fixtures if contention appears.
- [.NET 10 + pinned MediatR 12.5.0 / AutoMapper 14.0.0 interplay surprises] → Pins are deliberate finals; any incompatibility discovered at build time goes to the author, not to a version bump (freeze is before dry-run 1).
- [Migrations add demo-day friction (forgetting to apply)] → Hosts apply migrations on startup in Development; `docker compose up -d` plus `dotnet test` remains the whole ritual.

## Open Questions

None. The enterprise-parody display-name question in CLAUDE.md is cosmetic, does not affect this slice's artifacts, and stays an open item there.
