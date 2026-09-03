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
docker compose up -d   # PostgreSQL 17, plus RabbitMQ for the optional monitor (below)
dotnet build
dotnet test            # identical scenarios, green against both twins
```

Run a twin live: `dotnet run --project src/before/LayerCake.WebApi` (port 42010) or `dotnet run --project src/after/LayerCake.Slices` (port 42020). Swagger UI at `/swagger` on both.

Both twins share one PostgreSQL database (`layercake`): EF Core writes to the `before` schema, Marten to the `after` schema. The database engine never changes between the two; only the access idiom does.

### The frontend

`src/frontend/index.html` is a single static page: plain HTML, inline CSS and JS, no build step. Start Postgres, run both twins, and open the file straight from disk. It walks the whole journey (browse, publish, coupon, order, baker's board) against either twin via a Before/After switch, with a wire pane showing every request and response. It exists to show the identical HTTP contract to a human in a browser; it is **not** part of the proof. The contract tests are the proof.

### The monitor (optional)

`src/monitor/LayerCake.CritterWatch` is a third host: a [CritterWatch](https://jasperfx.net/news/announcing-critterwatch-1-0-rc-1) console, a monitoring console for Wolverine, run at http://localhost:42030. It is opt-in and exists for two reasons: watching the after twin's messages (the `NotifyBaker` cascade from PlaceOrder, its handler execution, the outbox) during prep and Q&A, and debugging. It is not part of the proof either.

It has its own event store, a dedicated `critterwatch` PostgreSQL database rather than a schema inside `layercake` (the compose file creates it on a fresh volume; an existing volume needs a one-off `CREATE DATABASE critterwatch;`). RabbitMQ is the telemetry channel between the after twin and the console, which is why `docker compose up` brings up a broker alongside Postgres.

The after twin publishes telemetry only when `CritterWatch:Enabled` is true. Its `http` launch profile sets that flag, so `dotnet run --project src/after/LayerCake.Slices` expects RabbitMQ to be up; to run the twin without the broker, set `CritterWatch__Enabled=false` or run with `--no-launch-profile`. The contract tests boot the twin without the flag, so **`dotnet test` never needs RabbitMQ or the console**. To watch a run: `docker compose up -d`, then `dotnet run --project src/monitor/LayerCake.CritterWatch`, then the after twin, then exercise it (the frontend or the suite's scenarios) and open the console.

In Development the console runs without a license key. Outside Development it reads `JasperFx:LicenseKey` from user secrets (id `layercake-critterwatch`).

## What this repo is (and is not)

This repo is a **laboratory built for a talk**, not a production system or a template. The before twin is written earnestly, the way many real .NET codebases are actually structured, because the comparison is worthless if it's a strawman. The after twin changes architecture *and* library stack at the same time; the talk owns that confounding honestly rather than pretending one variable moved.

Deliberately out of scope: authentication, event sourcing, microservices, and any frontend framework or SPA. Those are different talks. The two extras, the static demo page described in [The frontend](#the-frontend) and the console described in [The monitor](#the-monitor-optional), stay off the critical path and are not part of the proof; the contract tests are. If the Marten "after" twin makes you curious about event sourcing, that refactor is one step away; see [CritterMart](https://github.com/erikshafer/crittermart) and the Critter Stack docs.

## The domain

A bakery publishes cakes, shoppers browse them, apply coupons, and place orders; the baker gets a to-do entry for each order. Three features, chosen because each one showcases a claim the talk makes:

1. **Publish / browse cakes**: a trivial write, and how many files it touches in each twin.
2. **Validate coupon**: railway-oriented flow (invalid, not-yet-active, expired, valid).
3. **Place order**: the A-Frame shape, with a reliable "notify the baker" side effect through the outbox.

## License

MIT
