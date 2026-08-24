# LayerCake 🍰

*A tiny bakery selling layer cakes. Built in layers, served in slices.*

LayerCake is the companion system for the KCDC 2026 talk **"How I Gave Up Clean Architecture, and Why My Code Got Simpler."** It implements the same small bakery API twice, in one solution:

| | Projects | Style |
|---|---|---|
| **Before** | `LayerCake.Domain`, `LayerCake.Application`, `LayerCake.Infrastructure`, `LayerCake.WebApi` | Clean Architecture: controllers, MediatR, EF Core, repositories, DTOs, mappers |
| **After** | `LayerCake.Slices` | Vertical slices: Wolverine.Http endpoints, Marten documents, feature folders |

Both twins expose the **identical HTTP contract**, and one shared Alba test suite (`LayerCake.ContractTests`) runs the exact same scenarios against each. Same behavior, same tests, two architectures.

## Running it

```bash
docker compose up -d   # PostgreSQL 17
dotnet build
dotnet test            # identical scenarios, green against both twins
```

Run a twin live: `dotnet run --project src/before/LayerCake.WebApi` (port 42010) or `dotnet run --project src/after/LayerCake.Slices` (port 42020). Swagger UI at `/swagger` on both.

Both twins share one PostgreSQL database (`layercake`): EF Core writes to the `before` schema, Marten to the `after` schema. The database engine never changes between the two; only the access idiom does.

## What this repo is (and is not)

This repo is a **laboratory built for a talk**, not a production system or a template. The before twin is written earnestly, the way many real .NET codebases are actually structured, because the comparison is worthless if it's a strawman. The after twin changes architecture *and* library stack at the same time; the talk owns that confounding honestly rather than pretending one variable moved.

Deliberately out of scope: authentication, event sourcing, microservices, a frontend. Those are different talks. If the Marten "after" twin makes you curious about event sourcing, that refactor is one step away; see [CritterMart](https://github.com/erikshafer/crittermart) and the Critter Stack docs.

## The domain

A bakery publishes cakes, shoppers browse them, apply coupons, and place orders; the baker gets a to-do entry for each order. Three features, chosen because each one showcases a claim the talk makes:

1. **Publish / browse cakes**: a trivial write, and how many files it touches in each twin.
2. **Validate coupon**: railway-oriented flow (invalid, not-yet-active, expired, valid).
3. **Place order**: the A-Frame shape, with a reliable "notify the baker" side effect through the outbox.

## License

MIT
