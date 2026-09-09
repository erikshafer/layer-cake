# LayerCake 🍰

*A tiny bakery selling layer cakes. Built in layers, served in slices.*

[![CI](https://github.com/erikshafer/layer-cake/actions/workflows/ci.yml/badge.svg)](https://github.com/erikshafer/layer-cake/actions/workflows/ci.yml)

LayerCake is the same small bakery API written twice, side by side, in one .NET solution:

| | Where | Shape |
|---|---|---|
| **Before** | `src/before/` (4 projects) | Clean Architecture: controllers, MediatR, FluentValidation, AutoMapper, DTOs, repositories over EF Core |
| **After** | `src/after/` (1 project) | Vertical slices: one feature per file on Wolverine.Http endpoints over the same EF Core |

One shared contract-test suite (`tests/LayerCake.ContractTests`) runs the exact same scenarios against both. If the suite is green twice, the two implementations behave identically. Everything else in the repo exists to make that comparison honest and easy to see for yourself. Both twins use EF Core against PostgreSQL, so the only things that change between them are the architecture and the mediator. Two further hosts sit outside the solution in `experiments/`, each running the same scenarios to test a claim rather than ship a feature. See [The proof](#the-proof-one-suite-two-hosts).

It is the companion repo for the KCDC 2026 talk **"How I Gave Up Clean Architecture, and Why My Code Got Simpler"** by Erik Shafer (Kansas City, September 10-11, 2026). You do not need to have seen the talk to use it. Slides and a recording will be linked here when they exist.

## Contents

- [Quick start](#quick-start)
- [What you are looking at](#what-you-are-looking-at)
- [Three features and one message](#three-features-and-one-message)
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

`dotnet test` is the whole point of the repo. The suite starts a throwaway PostgreSQL 17 container per twin, migrates and seeds it, runs every scenario against the Clean Architecture host, runs the identical scenarios against the vertical-slice host, and tears both down. Since the baker notification crosses RabbitMQ on both twins, the suite starts a throwaway RabbitMQ 4 container per twin the same way. No compose step, no leftover state. The same command runs in CI on every push.

Want to poke at the APIs in a browser instead? See [Running it live](#running-it-live).

## What you are looking at

The talk makes a claim: the ceremony that Clean Architecture asks of a .NET codebase (the layers, the interfaces over interfaces, the mediator, the mappers, the DTOs) buys less than it costs for most systems, and a feature-folder vertical-slice shape ends up simpler without giving up the things the layers were supposed to protect. A claim like that is easy to make with a strawman. So the rules for this repo were:

1. **The before twin is built earnestly.** It is written the way a disciplined .NET team following the standard Clean Architecture template actually builds things. If it would embarrass a competent architecture review, it does not ship. It is not a caricature and it is not padded to inflate a file count. That sentence was tested rather than asserted: slice 001 was rebuilt on the Clean Architecture Solution Template (`dotnet new ca-sln`) as generated and counted against both twins. The template differs in specifics (no repository, reads projected straight to DTOs, Minimal API endpoint groups instead of controllers) and matches in layering and ceremony (five MediatR behaviours on every request, a validator, a DTO with a mapping profile, an exception-to-ProblemDetails mapping, an EF configuration, a seed). The number and the file list are in [the experiment section of the inventory](docs/file-inventory.md#experiment-slice-001-on-the-clean-architecture-solution-template-2026-09-06-branch-experimentbefore-clean-template-not-part-of-the-scorecard); the idiom-by-idiom comparison is the 2026-09-06 entry in `docs/build-log.md`.
2. **The after twin uses its stack's own idioms**, not a translation of the before twin. Wolverine's compound handlers and `ProblemDetails` guards, `[Entity]` loading, its durable outbox, side effects as return values. It does not smuggle in a Result type or a hand-rolled mediator.
3. **Both expose a byte-honest identical HTTP contract**, and one suite proves it. Exact status codes, explicit content types, camelCase JSON, 404 for missing resources, the same problem-details shape on failures.
4. **Same database engine and the same ORM.** Both twins talk to PostgreSQL 17 through EF Core 10 and Npgsql, in the same `layercake` database when run live: the layered twin owns the `before` schema, the slice twin the `after` schema. The comparison is about code shape, not about swapping databases or data-access libraries.

The after twin was originally written on Marten documents, and that version still exists and still passes the same scenarios (`experiments/after-marten/`). It was moved out of the solution the day before the talk for one reason: with a document store on one side, the honest answer to "what changed?" included the persistence library, and that is not what the talk is arguing about. What moves between the twins now is the architecture and the mediator. What is left confounded, and the talk says so, is the web framework: controllers plus MediatR on one side, Wolverine.Http on the other.

## Three features and one message

A bakery publishes cakes, shoppers browse them, check a coupon, and place an order; the baker gets a to-do entry for each order. Three features, each chosen because it showcases one claim, plus one extension that puts the third feature's side effect on a real broker.

| # | Feature | What it demonstrates |
|---|---|---|
| 1 | **Publish and browse cakes** | The hook. One trivial write, traced through every layer of the before twin, then the same feature as a single file in the after twin. |
| 2 | **Validate a coupon** | Railway-oriented flow. A coupon is `invalid`, `notYetActive`, `expired`, or `valid`, always as a 200 envelope. The evaluation is one shared function that the next feature reuses, which is the in-repo answer to "how do slices share logic?" |
| 3 | **Place an order** | The A-Frame shape (load, decide purely, persist) and a reliable side effect: placing an order creates exactly one baker task. The after twin sends that through Wolverine's transactional outbox, written in the same EF Core transaction as the order, so no order without a task and no task without an order. |
| 4 | **Notify the baker over RabbitMQ** (extends 3) | What one message costs each shape. The baker notification crosses a real RabbitMQ queue in both twins. The before twin adds a port, an adapter, a hosted consumer, and a re-dispatch through MediatR, and publishes after its commit with no outbox. The after twin changes `Program.cs` and nothing else. |

Step 4 adds no endpoint and changes no contract; the scenario that already proved exactly one baker task now proves it across the broker. Same broker on both sides; only the idiom changes. It is recorded as slice 004 in `docs/slices/`.

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
| Notify the baker over RabbitMQ | 8 files created, 6 edited | none created, 1 edited |
| Whole twin, all C# source | 2,140 lines | 844 lines |

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

And in the after twin it is `src/after/LayerCake.Slices/Cakes/PublishCake.cs`, shown here in full apart from the usings and the 201 response record:

```csharp
public record PublishCake(string? Name, string? Description, decimal Price);

public static class PublishCakeEndpoint
{
    // Wolverine runs ValidateAsync before Post and short-circuits with the
    // ProblemDetails, so the endpoint method only ever sees the happy path.
    public static async Task<ProblemDetails> ValidateAsync(
        PublishCake command, LayerCakeDbContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            return new ProblemDetails { Detail = "Name is required", Status = 400 };

        if (command.Price <= 0)
            return new ProblemDetails { Detail = "Price must be greater than zero", Status = 400 };

        var nameTaken = await db.Set<Cake>().AnyAsync(c => c.Name == command.Name, ct);

        return nameTaken
            ? new ProblemDetails { Detail = $"A cake named \"{command.Name}\" has already been published", Status = 409 }
            : WolverineContinue.NoProblems;
    }

    [WolverinePost("/cakes")]
    public static PublishedCake Post(PublishCake command, LayerCakeDbContext db)
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
        db.Add(cake);

        return new PublishedCake(cake.Id, cake.Name, cake.Description, cake.Price, cake.PublishedAt);
    }
}
```

Both twins are talking to the same EF Core, against the same PostgreSQL, with the same unique index on the cake name backing that 409. Neither side is hiding anything. The before twin does the same validation, the same duplicate check, the same persistence; it just does them across a domain entity, a command, a handler, a validator, a pipeline behavior, a repository interface, a repository, a mapping profile, a DTO, an EF configuration, a controller, and an exception filter. Both are worth reading. The talk is about what all of that indirection is buying.

## The proof: one suite, two hosts

`tests/LayerCake.ContractTests` is a single [Alba](https://jasperfx.github.io/alba/) + xUnit + Shouldly project. Every scenario is written once in an abstract class (`CakeScenarios`, `CouponScenarios`, `OrderScenarios`), and two sealed subclasses at the bottom of each file bind it to a host: one boots the Clean Architecture twin, one boots the vertical-slice twin. The test runner sees 27 scenarios twice, 54 runs, and every one of them must pass.

A second, smaller project, `tests/LayerCake.Slices.Tests`, exists for the after twin only. It has no host, no database, and no mocks: 15 facts call the pricing function, the guard chain, the coupon rule, and the baker handler directly and inspect what they return. It is not part of the parity proof. It is the exhibit for why the vertical-slice code is cheap to test, and `dotnet test` runs it alongside the contract suite.

### The experimental hosts

`experiments/` holds hosts that test a claim rather than ship a feature. Neither is in the solution, so `dotnet test` at the root is exactly the two-host proof above.

`experiments/after-marten/` is the after twin as it stood on Marten documents, kept intact and still green: same slices, same scenarios, a document store instead of an ORM. It is what the after twin looked like before the twins were put on one ORM so that only the architecture moved between them.

```bash
dotnet test tests/LayerCake.ContractTests.Marten/LayerCake.ContractTests.Marten.csproj   # 27/27
dotnet test tests/LayerCake.Slices.Marten.Tests/LayerCake.Slices.Marten.Tests.csproj     # 15/15
```

`experiments/before-clean-template/` is the Clean Architecture Solution Template (`dotnet new ca-sln`, version 10.8.0) generated as-is, with slice 001 built on it the template's way, and `tests/LayerCake.ContractTests.CleanTemplate/` runs the unchanged cake and ping scenarios against it. The test project is separate because one process can load one MediatR: the template ships MediatR 14, the before twin pins 12.5.0, and sharing a test bin broke every before-twin scenario before it ran. Run the experiment on its own (Docker is the only prerequisite):

```bash
dotnet test tests/LayerCake.ContractTests.CleanTemplate/LayerCake.ContractTests.CleanTemplate.csproj
```

As committed it is 6 of 10 green, and that is the template as shipped, not a bug in the scenarios: the four error scenarios get the right status codes but the template's exception handler writes `application/json` instead of `application/problem+json`, and its 400 bodies drop the `errors` dictionary that names the failing field. One line in the template's handler makes it 10 of 10; the committed state leaves it as generated. For the same three endpoints the template touches 9 files created / 8 edited, against 19 / 4 for the before twin and 5 / 1 for the after twin. The file list is [the experiment section of the inventory](docs/file-inventory.md#experiment-slice-001-on-the-clean-architecture-solution-template-2026-09-06-branch-experimentbefore-clean-template-not-part-of-the-scorecard), and the narrative (package graph, the hop trace, why the four are red, the one-line fix) is the 2026-09-06 entry in `docs/build-log.md`. The experiment is outside the scorecard and outside the proof.

The scenarios assert what the contract says, not what is convenient: exact status codes rather than "any 2xx", the `Location` header on creates, the raw body never containing `percentOff` unless the coupon is valid, the discount math to the cent, and that placing an order produces exactly one baker task. For that last one the suite polls the baker endpoint with a short timeout and does not know or care which twin does the work asynchronously.

Each twin gets its own PostgreSQL 17 and RabbitMQ 4 containers from Testcontainers, has its schema applied (EF Core migrations on the before twin, Wolverine-managed schema creation on the after twin), and is reset and reseeded per scenario class. The two twins run in parallel, and every scenario starts from the same three cakes and three coupons.

## Running it live

The suite needs nothing but Docker, but to click around you want the twins running against a persistent database.

```bash
docker compose up -d                                    # PostgreSQL 17 + RabbitMQ 4 (both twins use the broker)
dotnet run --project src/before/LayerCake.WebApi        # http://localhost:42010
dotnet run --project src/after/LayerCake.Slices         # http://localhost:42020
```

Both twins apply their schema and seed data on startup in Development. Swagger UI is at `/swagger` on each. Both write to the one `layercake` database through EF Core: the before twin into the `before` schema via migrations, the after twin into the `after` schema via Wolverine's managed schema creation. `docker compose down -v` wipes everything.

Both twins expect RabbitMQ to be up (compose starts it): placing an order publishes the baker notification to a queue, and each twin consumes its own queue inside its own process. The after twin's default launch profile additionally turns on CritterWatch telemetry; set `CritterWatch__Enabled=false` or pass `--no-launch-profile` to run it without the console's queues. The broker's management UI is at http://localhost:15672 (guest/guest) if you want to watch the two `layercake-*-baker-tasks` queues.

The experimental template host runs live too. It drops and recreates whatever database its connection string names, so give it its own database name and never the compose `layercake` one:

```bash
dotnet run --project experiments/before-clean-template/src/Web -- --urls http://localhost:5113 "--ConnectionStrings:LayerCake.CleanTemplateDb=Server=127.0.0.1;Port=5432;Database=layercake_clean_template;Username=postgres;Password=postgres;"
```

Point it at `42010` instead and the demo page's Before switch drives it for cakes; the page reports `Location: null` because the template's CORS policy does not expose the header (the raw response carries it), and the baker's board shows a 404 because the template has no such endpoint. Both are expected.

### The demo page

`src/frontend/index.html` is one static HTML file: inline CSS and JS, no build step, no server. With both twins running, open it straight from disk. It walks the whole journey (browse, publish, coupon, order, baker's board) against either twin through a Before/After switch, with a wire pane showing every request and response so the identical contract is visible to a human. It is a demo surface, not part of the proof; the contract tests are the proof.

### The monitor (optional)

`src/monitor/LayerCake.CritterWatch` is a third host: a [CritterWatch](https://jasperfx.net/news/announcing-critterwatch-1-0-rc-1) console for Wolverine at http://localhost:42030. It exists to watch the after twin's messaging (the `NotifyBaker` cascade from placing an order, its handler execution, the outbox) during prep, Q&A, and debugging.

```bash
docker compose up -d
dotnet run --project src/monitor/LayerCake.CritterWatch
dotnet run --project src/after/LayerCake.Slices
```

Then exercise the after twin (the demo page is the easy way) and open the console. It keeps its own event store in a dedicated `critterwatch` PostgreSQL database; the compose file creates that database on a fresh volume, and an existing volume needs a one-off `CREATE DATABASE critterwatch;`. RabbitMQ carries the telemetry between the twin and the console, on top of the baker notification it already carries for both twins. In Development the console runs without a license key; outside Development it reads `JasperFx:LicenseKey` from user secrets (id `layercake-critterwatch`). `dotnet test` never touches the console or the compose broker; it starts its own.

## Repo tour

```
src/
  before/                            the Clean Architecture twin
    LayerCake.Domain/                entities, enums, base classes
    LayerCake.Application/           commands, queries, handlers, validators, DTOs, mapping profiles, interfaces (including the messaging port)
    LayerCake.Infrastructure/        DbContext, EF configurations, migrations, repositories, services, the RabbitMQ publisher
    LayerCake.WebApi/                controllers, exception filter, the RabbitMQ consumer, Program.cs (port 42010)
  after/
    LayerCake.Slices/                the vertical-slice twin (port 42020)
      Cakes/                         Cake.cs (the entity and its table mapping) plus one file per feature: PublishCake.cs, BrowseCakes.cs, GetCake.cs
      Coupons/                       Coupon.cs, CouponValidation.cs (THE shared rule), ValidateCoupon.cs
      Orders/                        Order.cs, BakerTask.cs, PlaceOrder.cs, NotifyBaker.cs, GetOrder.cs, GetBakerTasks.cs
      Ping.cs, SeedData.cs, LayerCakeDbContext.cs, Program.cs   (the DbContext has no DbSets and no mapping: every table configures itself in its feature file)
  frontend/index.html                the static demo page
  monitor/LayerCake.CritterWatch/    the optional monitoring console (port 42030)
experiments/
  after-marten/                      the after twin as it stood on Marten documents; same slices, same scenarios, outside the solution and the scorecard
  before-clean-template/             the Clean Architecture Solution Template (dotnet new ca-sln) with slice 001 built its way; outside the solution and the scorecard
tests/
  LayerCake.ContractTests/           one Alba suite, both hosts, 27 scenarios x 2
  LayerCake.Slices.Tests/            15 pure-function facts against the after twin only: no host, no database, no mocks
  LayerCake.ContractTests.Marten/    the same 27 scenarios against the Marten experiment; its own project, outside the solution
  LayerCake.Slices.Marten.Tests/    the Marten experiment's 15 pure-function facts; outside the solution
  LayerCake.ContractTests.CleanTemplate/  the cake and ping scenarios against the template host; its own project, outside the solution
docs/
  slices/                            design record per feature: contract, required structure, scenarios
  file-inventory.md                  honest per-feature file counts (the numbers above come from here)
  build-log.md                       dated decisions made while building, for anyone wondering "why is it like this?"
  frontend.md                        the demo page's design record
  critter-stack-audit.md             the after twin checked against JasperFx's own guidance, with the divergences it kept and why
openspec/                            the change workflow used during the build; specs/ is what actually shipped
Directory.Packages.props             every package version, pinned, with the reason for each pin
docker-compose.yml                   PostgreSQL 17 + RabbitMQ 4 for running the twins live
```

## Tech stack

| Concern | Before twin | After twin |
|---|---|---|
| Runtime | .NET 10, C# 14 | .NET 10, C# 14 |
| HTTP | ASP.NET Core controllers | [Wolverine.Http](https://wolverinefx.net/guide/http/) endpoints |
| Mediation | MediatR 12.5.0 | Wolverine handlers |
| Persistence | EF Core 10 + Npgsql, schema `before` | EF Core 10 + Npgsql, schema `after`, through [Wolverine's EF Core integration](https://wolverinefx.net/guide/durability/efcore/) |
| Validation | FluentValidation via a MediatR pipeline behavior | Wolverine `Validate` methods returning `ProblemDetails` |
| Mapping | AutoMapper 14.0.0 | none |
| Messaging | `RabbitMQ.Client` publisher behind a port, `BackgroundService` consumer, no outbox | Wolverine's RabbitMQ transport with the durable outbox, configured in `Program.cs` |
| Database | PostgreSQL 17 | PostgreSQL 17 |
| Broker | RabbitMQ 4 | RabbitMQ 4 |
| Tests | the shared Alba + xUnit + Shouldly suite | the same suite |

MediatR and AutoMapper are deliberately pinned at their final open-source releases, which is exactly where a lot of real layered codebases sit today. All versions were frozen before the talk's dry runs; the rationale for each pin is a comment in `Directory.Packages.props`.

## What this repo is not

It is a laboratory built for a talk, not a starter template and not a production system. Deliberately out of scope, because each one is a different talk:

- Authentication and authorization.
- Event sourcing. Both twins are state-stored. If the after twin makes you curious, that refactor is one step away: see [CritterMart](https://github.com/erikshafer/crittermart), which these features were lifted and simplified from.
- Microservices, messaging between services, or anything beyond one deployable per twin. The one RabbitMQ message here leaves and re-enters the same process on each side.
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
