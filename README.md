# LayerCake 🍰

*A tiny bakery selling layer cakes. Built in layers, served in slices.*

[![CI](https://github.com/erikshafer/layer-cake/actions/workflows/ci.yml/badge.svg)](https://github.com/erikshafer/layer-cake/actions/workflows/ci.yml)

LayerCake is the same small bakery API written twice, side by side, in one .NET solution:

| | Where | Shape |
|---|---|---|
| **Before** | `src/before/` (4 projects) | Clean Architecture: controllers, MediatR, FluentValidation, AutoMapper, DTOs, repositories over EF Core |
| **After** | `src/after/` (1 project) | Vertical slices: one feature per file on Wolverine.Http endpoints and Marten documents |

One shared contract-test suite (`tests/LayerCake.ContractTests`) runs the exact same scenarios against both. If the suite is green twice, the two implementations behave identically. Everything else in the repo exists to make that comparison honest and easy to see for yourself.

It is the companion repo for the KCDC 2026 talk **"How I Gave Up Clean Architecture, and Why My Code Got Simpler"** by Erik Shafer (Kansas City, September 10-11, 2026). You do not need to have seen the talk to use it. Slides and a recording will be linked here when they exist.

## Contents

- [Quick start](#quick-start)
- [What you are looking at](#what-you-are-looking-at)
- [The three features](#the-three-features)
- [The HTTP contract](#the-http-contract)
- [Same feature, two shapes](#same-feature-two-shapes)
- [The proof: one suite, two hosts](#the-proof-one-suite-two-hosts)
- [Running it live](#running-it-live)
- [Repo tour](#repo-tour)
- [Tech stack](#tech-stack)
- [What this repo is not](#what-this-repo-is-not)
- [Further reading](#further-reading)
- [Contributing](#contributing)
- [License](#license)

## Quick start

Prerequisites: the [.NET 10 SDK](https://dotnet.microsoft.com/download) and Docker (Docker Desktop or any engine [Testcontainers](https://dotnet.testcontainers.org/) can reach). Nothing else.

```bash
git clone https://github.com/erikshafer/layer-cake.git
cd layer-cake
dotnet build
dotnet test
```

`dotnet test` is the whole point of the repo. The suite starts a throwaway PostgreSQL 17 container per twin, migrates and seeds it, runs every scenario against the Clean Architecture host, runs the identical scenarios against the vertical-slice host, and tears both down. No compose step, no leftover state, no broker. The same command runs in CI on every push.

Want to poke at the APIs in a browser instead? See [Running it live](#running-it-live).

## What you are looking at

The talk makes a claim: the ceremony that Clean Architecture asks of a .NET codebase (the layers, the interfaces over interfaces, the mediator, the mappers, the DTOs) buys less than it costs for most systems, and a feature-folder vertical-slice shape ends up simpler without giving up the things the layers were supposed to protect. A claim like that is easy to make with a strawman. So the rules for this repo were:

1. **The before twin is built earnestly.** It is written the way a disciplined .NET team following the standard Clean Architecture template actually builds things. If it would embarrass a competent architecture review, it does not ship. It is not a caricature and it is not padded to inflate a file count.
2. **The after twin uses its stack's own idioms**, not a translation of the before twin. Wolverine's compound handlers and `ProblemDetails` guards, Marten's document sessions and outbox, side effects as return values. It does not smuggle in a Result type or a hand-rolled mediator.
3. **Both expose a byte-honest identical HTTP contract**, and one suite proves it. Exact status codes, explicit content types, camelCase JSON, 404 for missing resources, the same problem-details shape on failures.
4. **Same database engine, different access idiom.** Both twins talk to PostgreSQL 17. EF Core owns the `before` schema, Marten owns the `after` schema, in the same `layercake` database when run live. The comparison is about code shape, not about swapping databases.

The after twin changes the architecture *and* the library stack at the same time. That is a confounded experiment, and the talk owns it rather than pretending only one variable moved.

## The three features

A bakery publishes cakes, shoppers browse them, check a coupon, and place an order; the baker gets a to-do entry for each order. Three features, each chosen because it showcases one claim.

| # | Feature | What it demonstrates |
|---|---|---|
| 1 | **Publish and browse cakes** | The hook. One trivial write, traced through every layer of the before twin, then the same feature as a single file in the after twin. |
| 2 | **Validate a coupon** | Railway-oriented flow. A coupon is `invalid`, `notYetActive`, `expired`, or `valid`, always as a 200 envelope. The evaluation is one shared function that the next feature reuses, which is the in-repo answer to "how do slices share logic?" |
| 3 | **Place an order** | The A-Frame shape (load, decide purely, persist) and a reliable side effect: placing an order creates exactly one baker task. The after twin sends that through Marten's transactional outbox, so no order without a task and no task without an order. |

Design notes for each feature, including the contract clauses, the required structure of each twin, and the scenario list, live in `docs/slices/`.

## The HTTP contract

Both twins serve exactly this. Request and response bodies are camelCase JSON; failures are `application/problem+json` with the reason in the body.

| Endpoint | Success | Failures |
|---|---|---|
| `POST /cakes` | `201` + `Location: /cakes/{id}`, body `{ id, name, description, price, publishedAt }` | `400` missing name or non-positive price, `409` duplicate name |
| `GET /cakes` | `200` array of cakes | |
| `GET /cakes/{id}` | `200` cake | `404` |
| `GET /coupons/{code}` | `200` always: `{ code, status }`, plus `percentOff` only when `status` is `valid` | never fails; an unknown code is `status: "invalid"` |
| `POST /orders` | `201` + `Location: /orders/{id}`, body with priced lines, `subtotal`, `discount`, `total`, optional `couponCode`, `placedAt` | `400` empty lines or quantity below 1, `422` unknown cake ids (listed), `422` coupon not valid (status named) |
| `GET /orders/{id}` | `200` same shape as the POST body | `404` |
| `GET /baker/tasks` | `200` array of `{ orderId, summary, createdAt }`, optional `?orderId=` filter | |

Seed data is identical on both sides: three cakes (Classic Yellow, Chocolate Stout, Lemon Chiffon) and three coupons whose windows are relative to today so they never rot:

| Code | Status today |
|---|---|
| `BDAY10` | valid, 10% off |
| `SUMMER25` | expired |
| `HOLIDAY30` | not yet active |

## Same feature, two shapes

`docs/file-inventory.md` records the actual files created for each feature as it was built, counted honestly (generated EF migrations excluded, files edited to wire a feature in listed separately). The numbers a slide can quote are the numbers in that file.

| Feature | Before twin | After twin |
|---|---|---|
| Publish and browse cakes | 19 files created, 4 edited | 5 files created, 1 edited |
| Validate coupon | 13 files created, 4 edited | 3 files created, 2 edited |
| Place order | 26 files created, 6 edited | 6 files created, none edited |
| Whole twin, all C# source | 1,701 lines | 692 lines |

The shortest way to feel the difference is to read one feature in both. Publishing a cake in the before twin touches:

```
Domain/Entities/Cake.cs
Application/Cakes/CakeDto.cs
Application/Cakes/Commands/PublishCake/PublishCakeCommand.cs
Application/Cakes/Commands/PublishCake/PublishCakeCommandHandler.cs
Application/Cakes/Commands/PublishCake/PublishCakeCommandValidator.cs
Application/Common/Behaviors/ValidationBehavior.cs
Application/Common/Interfaces/ICakeRepository.cs
Application/Common/Mappings/CakeMappingProfile.cs
Application/Common/Exceptions/DuplicateCakeNameException.cs
Infrastructure/Persistence/Configurations/CakeConfiguration.cs
Infrastructure/Repositories/CakeRepository.cs
WebApi/Controllers/CakesController.cs
WebApi/Filters/ApiExceptionFilterAttribute.cs
```

And in the after twin it is `src/after/LayerCake.Slices/Features/PublishCake.cs`, shown here in full apart from the usings and the 201 response record:

```csharp
public record PublishCake(string? Name, string? Description, decimal Price);

public static class PublishCakeEndpoint
{
    // Wolverine runs ValidateAsync before Post and short-circuits with the
    // ProblemDetails, so the endpoint method only ever sees the happy path.
    public static async Task<ProblemDetails> ValidateAsync(
        PublishCake command, IQuerySession session, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            return new ProblemDetails { Detail = "Name is required", Status = 400 };

        if (command.Price <= 0)
            return new ProblemDetails { Detail = "Price must be greater than zero", Status = 400 };

        var nameTaken = await session.Query<Cake>().AnyAsync(c => c.Name == command.Name, ct);

        return nameTaken
            ? new ProblemDetails { Detail = $"A cake named \"{command.Name}\" has already been published", Status = 409 }
            : WolverineContinue.NoProblems;
    }

    [WolverinePost("/cakes")]
    public static CakePublished Post(PublishCake command, IDocumentSession session)
    {
        var cake = new Cake
        {
            Id = Guid.NewGuid(),
            Name = command.Name!,
            Description = command.Description ?? string.Empty,
            Price = command.Price,
            PublishedAt = DateTimeOffset.UtcNow,
        };

        // AutoApplyTransactions commits this; no SaveChangesAsync in handlers.
        session.Store(cake);

        return new CakePublished(cake.Id, cake.Name, cake.Description, cake.Price, cake.PublishedAt);
    }
}
```

Neither side is hiding anything. The before twin does the same validation, the same duplicate check, the same persistence; it just does them across a domain entity, a command, a handler, a validator, a pipeline behavior, a repository interface, a repository, a mapping profile, a DTO, an EF configuration, a controller, and an exception filter. Both are worth reading. The talk is about what all of that indirection is buying.

## The proof: one suite, two hosts

`tests/LayerCake.ContractTests` is a single [Alba](https://jasperfx.github.io/alba/) + xUnit + Shouldly project. Every scenario is written once in an abstract class (`CakeScenarios`, `CouponScenarios`, `OrderScenarios`), and two sealed subclasses at the bottom of each file bind it to a host: one boots the Clean Architecture twin, one boots the vertical-slice twin. The test runner sees 26 scenarios twice, 52 runs, and every one of them must pass.

The scenarios assert what the contract says, not what is convenient: exact status codes rather than "any 2xx", the `Location` header on creates, the raw body never containing `percentOff` unless the coupon is valid, the discount math to the cent, and that placing an order produces exactly one baker task. For that last one the suite polls the baker endpoint with a short timeout and does not know or care which twin does the work asynchronously.

Each twin gets its own PostgreSQL 17 container from Testcontainers, has its schema applied (EF Core migrations on one side, Marten on the other), and is reset and reseeded per scenario class. The two twins run in parallel, and every scenario starts from the same three cakes and three coupons.

## Running it live

The suite needs nothing but Docker, but to click around you want the twins running against a persistent database.

```bash
docker compose up -d                                    # PostgreSQL 17 (+ RabbitMQ, only for the optional monitor)
dotnet run --project src/before/LayerCake.WebApi        # http://localhost:42010
dotnet run --project src/after/LayerCake.Slices         # http://localhost:42020
```

Both twins apply their schema and seed data on startup in Development. Swagger UI is at `/swagger` on each. Both write to the one `layercake` database: EF Core into the `before` schema, Marten into the `after` schema. `docker compose down -v` wipes everything.

The after twin's default launch profile turns on CritterWatch telemetry and therefore expects RabbitMQ to be up (compose starts it). To run the after twin with only PostgreSQL, set `CritterWatch__Enabled=false` or pass `--no-launch-profile`.

### The demo page

`src/frontend/index.html` is one static HTML file: inline CSS and JS, no build step, no server. With both twins running, open it straight from disk. It walks the whole journey (browse, publish, coupon, order, baker's board) against either twin through a Before/After switch, with a wire pane showing every request and response so the identical contract is visible to a human. It is a demo surface, not part of the proof; the contract tests are the proof.

### The monitor (optional)

`src/monitor/LayerCake.CritterWatch` is a third host: a [CritterWatch](https://jasperfx.net/news/announcing-critterwatch-1-0-rc-1) console for Wolverine at http://localhost:42030. It exists to watch the after twin's messaging (the `NotifyBaker` cascade from placing an order, its handler execution, the outbox) during prep, Q&A, and debugging.

```bash
docker compose up -d
dotnet run --project src/monitor/LayerCake.CritterWatch
dotnet run --project src/after/LayerCake.Slices
```

Then exercise the after twin (the demo page is the easy way) and open the console. It keeps its own event store in a dedicated `critterwatch` PostgreSQL database; the compose file creates that database on a fresh volume, and an existing volume needs a one-off `CREATE DATABASE critterwatch;`. RabbitMQ is the telemetry channel between the twin and the console, which is why compose brings up a broker. In Development the console runs without a license key; outside Development it reads `JasperFx:LicenseKey` from user secrets (id `layercake-critterwatch`). `dotnet test` never touches any of this.

## Repo tour

```
src/
  before/                            the Clean Architecture twin
    LayerCake.Domain/                entities, enums, base classes
    LayerCake.Application/           commands, queries, handlers, validators, DTOs, mapping profiles, interfaces
    LayerCake.Infrastructure/        DbContext, EF configurations, migrations, repositories, services
    LayerCake.WebApi/                controllers, exception filter, Program.cs (port 42010)
  after/
    LayerCake.Slices/                the vertical-slice twin (port 42020)
      Features/                      one file per feature: PublishCake.cs, ValidateCoupon.cs, PlaceOrder.cs, ...
      Cakes/ Coupons/ Orders/        Marten documents
      SeedData.cs, Program.cs
  frontend/index.html                the static demo page
  monitor/LayerCake.CritterWatch/    the optional monitoring console (port 42030)
tests/
  LayerCake.ContractTests/           one Alba suite, both hosts, 26 scenarios x 2
docs/
  slices/                            design record per feature: contract, required structure, scenarios
  file-inventory.md                  honest per-feature file counts (the numbers above come from here)
  build-log.md                       dated decisions made while building, for anyone wondering "why is it like this?"
  frontend.md                        the demo page's design record
openspec/                            the change workflow used during the build; specs/ is what actually shipped
Directory.Packages.props             every package version, pinned, with the reason for each pin
docker-compose.yml                   PostgreSQL 17 + RabbitMQ for running the twins live
```

## Tech stack

| Concern | Before twin | After twin |
|---|---|---|
| Runtime | .NET 10, C# 14 | .NET 10, C# 14 |
| HTTP | ASP.NET Core controllers | [Wolverine.Http](https://wolverinefx.net/guide/http/) endpoints |
| Mediation | MediatR 12.5.0 | Wolverine handlers |
| Persistence | EF Core 10 + Npgsql, schema `before` | [Marten](https://martendb.io/) 9 documents, schema `after` |
| Validation | FluentValidation via a MediatR pipeline behavior | Wolverine `Validate` methods returning `ProblemDetails` |
| Mapping | AutoMapper 14.0.0 | none |
| Database | PostgreSQL 17 | PostgreSQL 17 |
| Tests | the shared Alba + xUnit + Shouldly suite | the same suite |

MediatR and AutoMapper are deliberately pinned at their final open-source releases, which is exactly where a lot of real layered codebases sit today. All versions were frozen before the talk's dry runs; the rationale for each pin is a comment in `Directory.Packages.props`.

## What this repo is not

It is a laboratory built for a talk, not a starter template and not a production system. Deliberately out of scope, because each one is a different talk:

- Authentication and authorization.
- Event sourcing. Both twins are state-stored; Marten is used purely as a document store here. If the after twin makes you curious, that refactor is one step away: see [CritterMart](https://github.com/erikshafer/crittermart), which these features were lifted and simplified from.
- Microservices, messaging between services, or anything beyond one deployable per twin.
- A frontend framework or SPA. The demo page is a single static file and the talk never depends on it.

It is also not a claim that Clean Architecture is never the right call, or that the Critter Stack is the only way to write slices. It is one honest before-and-after you can clone, run, and argue with.

## Further reading

- `docs/slices/` for the design of each feature, and `docs/build-log.md` for every non-obvious decision made along the way.
- [Wolverine](https://wolverinefx.net/), [Marten](https://martendb.io/), and [Alba](https://jasperfx.github.io/alba/) documentation.
- [CritterMart](https://github.com/erikshafer/crittermart), the larger event-sourced system these slices come from.
- The talk's slides and recording, to be linked here after KCDC.

## Contributing

Issues and pull requests are welcome, especially from anyone who thinks the before twin is not being given a fair shake: that is the one critique the repo most wants to hear. Two rules keep the comparison meaningful:

1. Both twins must keep passing the shared suite. `dotnet test` green twice is the bar for any change.
2. A change to one twin that alters the HTTP contract needs the matching change in the other twin and in the scenarios.

The before twin follows conventional Clean Architecture idioms (constructor injection, repository interfaces, DTO mapping). The after twin follows Critter Stack idioms (static endpoints, compound handlers, session as a method parameter, side effects as return values). That asymmetry is the exhibit, so please keep each side in its own style.

## License

[MIT](LICENSE)
