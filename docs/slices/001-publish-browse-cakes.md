# Slice 001 — PublishCake + BrowseCakes

**Why this slice is in the talk:** the Act 1 hook. One trivial write traced through the before twin's full layer ceremony ("I did everything right"), then the same feature as a single after-twin file. Browse is a named 2-3 minute beat with its own read-stack contrast. This slice's honest file count IS a slide number: record it in `docs/file-inventory.md`.

CritterMart source design: Catalog `PublishProduct` (one file, ProblemDetails guard, ~60 lines).

## Contract (settled 2026-08-23; see also error-parity rules at the bottom)

### POST /cakes

Request:

```json
{ "name": "Chocolate Stout", "description": "Six layers, no mercy", "price": 34.00 }
```

- `201 Created`, `Location: /cakes/{id}`, body `{ id, name, description, price, publishedAt }`
- `400` input validation: `name` required and non-empty; `price` > 0
- `409` duplicate cake name (the slice's one real business guard; DECIDED, keep it)
- `id`: server-generated Guid. `publishedAt`: ISO 8601 UTC. camelCase everywhere.

### GET /cakes

- `200`: `[ { id, name, description, price } ]`
- No paging, no filtering, no sorting parameters. A bakery has a dozen cakes.

### GET /cakes/{id}

- `200` (same item shape as the browse array) / `404`
- Exists because POST's `Location` points here. No slides; keep it tiny.

## Before twin — REQUIRED structure

Build it earnestly and idiomatically, the way a disciplined 2019-2023 Clean Architecture .NET team would. The layering below is a requirement, not a suggestion; an agent that "helpfully" collapses layers destroys the exhibit. Do NOT pad artificially either: every file must be one a sincere CA practitioner would write. The honest count is whatever it is.

Structural elements (typical paths; adjust names to idiom, not to brevity):

- `Domain/Entities/Cake.cs` (extends `BaseAuditableEntity<Guid>`)
- `Application/Cakes/Commands/PublishCake/PublishCakeCommand.cs` (MediatR `IRequest<CakeDto>`)
- `Application/Cakes/Commands/PublishCake/PublishCakeCommandHandler.cs`
- `Application/Cakes/Commands/PublishCake/PublishCakeCommandValidator.cs` (FluentValidation, runs via the pipeline behavior)
- `Application/Cakes/Queries/BrowseCakes/BrowseCakesQuery.cs` + `BrowseCakesQueryHandler.cs`
- `Application/Cakes/Queries/GetCakeById/GetCakeByIdQuery.cs` + `GetCakeByIdQueryHandler.cs`
- `Application/Cakes/CakeDto.cs`
- `Application/Common/Mappings/CakeMappingProfile.cs` (AutoMapper)
- `Application/Common/Interfaces/ICakeRepository.cs` (repository interface OVER EF Core; yes, really; that is the exhibit)
- `Application/Common/Exceptions/` as idiomatic (e.g., duplicate-name → exception or result the controller maps to 409)
- `Infrastructure/Persistence/Configurations/CakeConfiguration.cs` (`IEntityTypeConfiguration<Cake>`)
- `Infrastructure/Repositories/CakeRepository.cs`
- `WebApi/Controllers/CakesController.cs` (thin; delegates to MediatR; maps results to ActionResults)
- `WebApi/Contracts/` request/response records if idiom calls for them beyond the DTO

Anti-shortcut rules: no `DbContext` outside Infrastructure; controllers never see entities; commands return DTOs, never entities; the duplicate-name check goes through the repository interface.

When done, append the actual file list and count to `docs/file-inventory.md`.

## After twin — expected shape

- `Features/PublishCake.cs`: `PublishCake` positional record command + static `PublishCakeEndpoint` with `ValidateAsync` (duplicate name → 409 `ProblemDetails`, else `WolverineContinue.NoProblems`) and `[WolverinePost("/cakes")]` returning `CreationResponse`. Marten `IDocumentSession.Store`; `AutoApplyTransactions` commits (never call `SaveChangesAsync`).
- `Features/BrowseCakes.cs`: `[WolverineGet("/cakes")]`, expression-bodied, `IQuerySession`.
- `Features/GetCake.cs`: `[WolverineGet("/cakes/{id}")]` with `[Entity(Required = true)]` for the automatic 404.
- `Cakes/Cake.cs` (or beside the features): Marten document, plain mutable class with `{ get; set; }`, never a record.

Follow the `csharp-critter-style` skill (auto-activates). Show-the-pure-part-first matters for slides: keep the endpoint body free of incidental noise.

## Shared contract scenarios (write once, run against both hosts)

1. `publish_cake_returns_201_with_location_and_body`
2. `publish_cake_with_missing_name_returns_400`
3. `publish_cake_with_nonpositive_price_returns_400`
4. `publish_cake_with_duplicate_name_returns_409`
5. `browse_returns_seeded_cakes`
6. `browse_includes_newly_published_cake`
7. `get_cake_by_id_returns_200`
8. `get_missing_cake_returns_404`

Discipline: exact status codes; explicit `Content-Type: application/json` on requests with bodies; failures asserted per the parity rule below. Test isolation must reset BOTH sides (EF `before` schema and `CleanAllMartenDataAsync()` on `after`) and re-seed.

## Seed data (shared, idempotent; part of this slice's build)

| name | description | price |
|---|---|---|
| Classic Yellow | Three layers, vanilla buttercream | 24.00 |
| Chocolate Stout | Six layers, no mercy | 34.00 |
| Lemon Chiffon | Light, tart, dangerously easy | 28.00 |

Seeds load identically into both twins (same names/prices; ids may differ per twin, tests discover ids via browse).

## Error-shape parity (applies to every slice; DECIDED: middle option)

Both twins return `application/problem+json`. The suite pins: exact status code, content type, and that the offending field/reason is discoverable in the body, via ONE small custom assertion helper in the shared suite. Bodies are NOT byte-identical and the README owns that as a deliberate laboratory decision. Status split: 400 input shape, 404 missing resource on GET, 409 conflict, 422 domain rejection.

## Out of scope

No auth, no paging, no images, no cake update/delete, no soft deletes, no cart.
