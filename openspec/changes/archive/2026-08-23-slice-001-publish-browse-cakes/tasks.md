# Tasks: slice-001-publish-browse-cakes

Contract, structure, and scenarios: `docs/slices/001-publish-browse-cakes.md`. Decisions: `design.md`.

## 1. Repo bootstrap (first slice only)

- [x] 1.1 Create `docker-compose.yml` (PostgreSQL 17, database `layercake`) and verify `docker compose up -d` starts and accepts connections
- [x] 1.2 Create the solution, `Directory.Packages.props` with pinned versions and rationale comments (MediatR 12.5.0, AutoMapper 14.0.0 finals; no Alba 9 beta), and empty project shells: four before-twin projects (`Domain`, `Application`, `Infrastructure`, `WebApi` under `src/before/`), `src/after/LayerCake.Slices`, `tests/LayerCake.ContractTests`; verify `dotnet build` succeeds
- [x] 1.3 Wire host bootstraps: before twin on port 42010 (controllers, Swagger in Development), after twin on port 42020 (Wolverine.Http, Marten with schema `after` and explicit `UseSystemTextJsonForSerialization(casing: Casing.CamelCase)` per design.md, `AutoApplyTransactions`, Swagger in Development); verify both hosts start and serve `/swagger`

## 2. Before twin (earnest Clean Architecture)

- [x] 2.1 Implement `Domain/Entities/Cake.cs` extending `BaseAuditableEntity<Guid>` (create the base type if this is its first use) and verify the Domain project builds with no dependencies on other layers
- [x] 2.2 Implement the Application layer per the slice spec's REQUIRED structure: `PublishCakeCommand` + handler + FluentValidation validator (name required, price > 0), `BrowseCakesQuery` + handler, `GetCakeByIdQuery` + handler, `CakeDto`, `CakeMappingProfile`, `ICakeRepository`, duplicate-name exception path; verify the validation pipeline behavior runs validators automatically
- [x] 2.3 Implement Infrastructure: `CakeConfiguration` (`IEntityTypeConfiguration<Cake>`), `CakeRepository`, DbContext registered with Npgsql on schema `before`; add the initial EF Core migration per design.md (migrations, not `EnsureCreated`) and verify `Database.Migrate()` creates the schema against the compose database
- [x] 2.4 Implement `WebApi/Controllers/CakesController.cs` (thin, delegates to MediatR, maps duplicate-name to 409 and missing cake to 404, `application/problem+json` failures, camelCase JSON) plus any idiomatic request/response contracts; verify manually via Swagger: POST 201 + Location, 400s, 409, GET list, GET by id, 404

## 3. After twin (Wolverine + Marten slices)

- [x] 3.1 Implement `Cakes/Cake.cs` Marten document (plain mutable class, `{ get; set; }`) and verify the after twin builds
- [x] 3.2 Implement `Features/PublishCake.cs`: positional record command, static endpoint with `ValidateAsync` (missing name / non-positive price → 400 ProblemDetails, duplicate name → 409 ProblemDetails, else `WolverineContinue.NoProblems`), `[WolverinePost("/cakes")]` returning `CreationResponse`, `IDocumentSession.Store` with no `SaveChangesAsync`; verify via Swagger: 201 + Location, 400s, 409
- [x] 3.3 Implement `Features/BrowseCakes.cs` (`[WolverineGet("/cakes")]`, expression-bodied, `IQuerySession`) and `Features/GetCake.cs` (`[WolverineGet("/cakes/{id}")]`, `[Entity(Required = true)]` for the automatic 404 — confirm it yields 404, not 204); verify via Swagger: 200 list, 200 by id, 404 missing, camelCase bodies

## 4. Shared seeds and contract suite

- [x] 4.1 Implement idempotent seeding of the three shared cakes (Classic Yellow 24.00, Chocolate Stout 34.00, Lemon Chiffon 28.00) through each twin's own persistence idiom, callable from tests and host startup; verify seeding twice leaves exactly three cakes per twin
- [x] 4.2 Build the test skeleton per design.md: one Alba host fixture per twin, abstract scenario base classes with twin subclasses, per-run isolation that resets BOTH sides (EF `before` schema delete + `CleanAllMartenDataAsync()` on `after`) and re-seeds; verify a trivial smoke scenario passes against both hosts
- [x] 4.3 Implement the shared error-shape assertion helper (exact status code, `application/problem+json` content type, offending field/reason discoverable in body) and verify it is used by every failure scenario
- [x] 4.4 Implement the eight shared scenarios with exact status codes and explicit `Content-Type: application/json` on request bodies: `publish_cake_returns_201_with_location_and_body`, `publish_cake_with_missing_name_returns_400`, `publish_cake_with_nonpositive_price_returns_400`, `publish_cake_with_duplicate_name_returns_409`, `browse_returns_seeded_cakes`, `browse_includes_newly_published_cake`, `get_cake_by_id_returns_200`, `get_missing_cake_returns_404`; verify each runs against both hosts (16 green results)

## 5. Verification and bookkeeping

- [x] 5.1 Run `docker compose up -d` then `dotnet test` from clean and verify the shared suite is green on BOTH hosts in one run (definition of done)
- [x] 5.2 Append the before twin's honest per-slice file list and count to `docs/file-inventory.md` (a slide depends on the real number)
- [x] 5.3 Record mid-build decisions in `docs/build-log.md`, including the migrations-over-EnsureCreated and Marten camelCase decisions from design.md, and update CLAUDE.md's open-items section to mark those resolved
