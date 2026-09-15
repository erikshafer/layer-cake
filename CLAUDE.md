# LayerCake: AI Development Guidelines

LayerCake is a tiny bakery selling layer cakes: built in layers, served in slices. It is the demo system for the talk **"How I Gave Up Clean Architecture, and Why My Code Got Simpler"**, first given at KCDC 2026 (Kansas City, 2026-09-10) and headed for more user groups and conferences. The repo is a **laboratory**: one solution, two implementations of the identical HTTP API, one shared contract-test suite proving they behave the same.

- **Before twin** (`src/before/`, four projects): earnest .NET Clean Architecture. EF Core + Npgsql, MediatR, FluentValidation pipeline behavior, AutoMapper, DTOs, repository/service layering. Built sincerely, never as a strawman.
- **After twin** (`src/after/LayerCake.Slices`, one project): feature-folder vertical slices on Wolverine.Http + EF Core through Wolverine's EF Core integration. Same ORM and same database engine as the before twin (Erik's call, 2026-09-09), so only the architecture and the mediator move between the twins. The Marten version of this twin lives on as an experiment; see below.
- **The spine** (`tests/LayerCake.ContractTests`): one Alba scenario suite referencing both hosts. Scenarios live in abstract base classes; twin subclasses run the identical set against each host. `dotnet test` green twice is the talk's most credible artifact.
- **The vendor** (`src/tendr/Tendr`, since slice 005): Tendr, a fake third-party card vendor both twins call over HTTP from PlaceOrder. Wolverine.Http on Marten's event store, schema `tendr`. Not a twin, not in the scorecard, never on a slide; it exists so the outside-service call crosses a real socket, live and under test. Its README documents the API and the test cards.

This file is the routing layer for AI sessions: the non-negotiables, the slice slate, and where detail lives. **The next delivery of the talk is the deadline. When ceremony conflicts with shipping the talk, shipping wins, explicitly, never silently.**

**Mode: a living talk (since 2026-09-14).** KCDC is over: the talk was delivered there on 2026-09-10. It will be given again at user groups and conferences (the next run is at the Improving office, date TBD), and the repo keeps being updated and polished between runs. Slice 005 (pay with Tendr, `docs/slices/005-pay-with-tendr.md`, PR #19) was the first such item and made the talk v1.1. Two kinds of session follow from that:

- **A build item** is Erik's call. It arrives as a hand-off from the talk side (the `presentations` repo's `handoff-*.md` files), gets its spec in `docs/slices/` first, runs the OpenSpec loop on both twins and the shared suite end to end, and lands with its bookkeeping, exactly as slices 004 and 005 did.
- **Everything else** is dry-run support, bug fixes that keep the suite green twice, and doc accuracy: no slice nobody asked for, no package bumps without Erik's call (security patches excepted), no re-planning. A fix that changes built behaviour updates `openspec/specs/`, `docs/file-inventory.md` (if a count moved), and `docs/build-log.md` in the same PR, so every document keeps saying what shipped.

History: demo-readiness mode began 2026-09-05 with all four original slices archived (`main` at `74034cd`, PR #13); the after twin moved from Marten to EF Core on 2026-09-09 (Erik's call, the day before KCDC), which changed no HTTP behaviour and no scenario; the Marten baseline is tagged `kcdc-marten-baseline`; slice 005 merged 2026-09-14 (`main` at `46f453e`).

**Experiments (since 2026-09-06).** `experiments/` holds hosts that test a claim rather than ship a slice. Today that is `experiments/after-marten/LayerCake.Slices.Marten` (the after twin as it stood on Marten documents, moved out of the solution 2026-09-09 when the twins were put on one ORM; its test projects are `tests/LayerCake.ContractTests.Marten`, 27/27, and `tests/LayerCake.Slices.Marten.Tests`, 15/15; namespaces stay `LayerCake.Slices` and the contract test project aliases the assembly) and `experiments/before-clean-template/` (the Clean Architecture Solution Template, `dotnet new ca-sln` 10.8.0, generated as-is with slice 001 built its way) and its test project `tests/LayerCake.ContractTests.CleanTemplate/` (PR #15, merged 2026-09-06). All of them are outside `LayerCake.slnx` and outside the scorecard; the template experiment is also outside the version freeze (its package graph is its own, deliberately unpinned by the repo), while the Marten experiment keeps the repo's pins. Experiments do not run the OpenSpec loop and never touch `src/`, `openspec/`, `docs/slices/`, or the shared scenario classes. Name experiment folders and namespaces after the artifact (`before-clean-template`, `LayerCake.CleanTemplate`; a variant on the `Ardalis.CleanArchitecture.Template` package would be `before-clean-arch`), never after a person.

---

## Commands

```
dotnet build              # one solution: both twins, Tendr, the CritterWatch console, and the three test projects
dotnet test               # the money shot: identical scenarios, green twice (Docker running is the only prerequisite; Testcontainers starts PostgreSQL AND RabbitMQ per twin, and a Tendr host per twin runs on Kestrel against that twin's PostgreSQL); also runs the after-twin-only pure-function unit tests in tests/LayerCake.Slices.Tests and Tendr's own tests in tests/Tendr.Tests (one more PostgreSQL container)
dotnet test tests/Tendr.Tests/Tendr.Tests.csproj
                          # Tendr, the card vendor, on its own: its API scenarios, the test-card table, and one InvokeAsync fact over Wolverine's HTTP transport
dotnet run --project src/tendr/Tendr
                          # Tendr live on 42040; both twins call it for any order that carries a card
dotnet test tests/LayerCake.ContractTests.Marten/LayerCake.ContractTests.Marten.csproj
                          # the Marten experiment only (Docker: PostgreSQL + RabbitMQ via Testcontainers); the same 27 scenarios against the document-store after twin; root dotnet test excludes it
dotnet test tests/LayerCake.Slices.Marten.Tests/LayerCake.Slices.Marten.Tests.csproj
                          # the Marten experiment's 15 pure-function facts; no host, no database
dotnet test tests/LayerCake.ContractTests.CleanTemplate/LayerCake.ContractTests.CleanTemplate.csproj
                          # the template experiment only (Docker, PostgreSQL via Testcontainers, no broker); root dotnet test excludes it; 6/10 as committed is the template as shipped
docker compose up -d      # PostgreSQL 17 + RabbitMQ, for running the twins LIVE only (both twins put the baker notification on the broker; CritterWatch rides it too)
```

`dotnet test` starts its own PostgreSQL 17 and RabbitMQ 4 per twin via Testcontainers (`tests/LayerCake.ContractTests/TwinHosts.cs`) and its own Tendr per twin (`TendrHost.cs`); it never touches the compose database, the compose broker, a live Tendr, or the console. Every Wolverine host the contract suite builds (the after twin and Tendr) goes through `WolverineHostGate.cs`, one at a time with its own application assembly, because Wolverine and JasperFx remember the first host's assembly process-wide; a new Wolverine host in that project must use the gate too. Root `dotnet test` excludes `experiments/`: the experiment test projects are outside the slnx and run only through the explicit commands above.

Frontend demo page: run both twins, then open `src/frontend/index.html` straight from disk (no build step, no server).

Ports (live demo only; Alba self-hosts in tests): before twin `42010`, after twin `42020`, CritterWatch console `42030` (`src/monitor/`), Tendr `42040` (`src/tendr/`). Swagger UI at `/swagger` on both twins in Development. The after twin publishes CritterWatch telemetry only when `CritterWatch:Enabled` is true (set via launchSettings env var; the contract tests never set it).

Before a live run (dry run or delivery): `docker compose down -v` then `up -d` restores the clean seed (a demo walk leaves cakes and orders behind); pre-pull `postgres:17` and `rabbitmq:4` on the demo machine; make sure nothing stale holds 42010 to 42040 (a pre-slice-005 after twin was once found still running on 42020). Slide captures from `codegen write` live outside the project folder: the after twin compiles every `.cs` under `src/after/LayerCake.Slices`, which is why `Internal/Generated/` is gitignored.

---

## Tech stack

| Concern | Before twin | After twin |
|---|---|---|
| Runtime | .NET 10, C# 14 | .NET 10, C# 14 |
| HTTP | ASP.NET Core controllers | Wolverine.Http endpoints |
| Mediation | MediatR 12.5.0 (final OSS release, deliberate) | Wolverine handlers |
| Persistence | EF Core 10 + Npgsql, schema `before` | EF Core 10 + Npgsql, schema `after`, via `AddDbContextWithWolverineIntegration` (no migrations folder: `UseEntityFrameworkCoreWolverineManagedMigrations` + `AddResourceSetupOnStartup` build the schema at host start) |
| Validation | FluentValidation via MediatR pipeline behavior | Wolverine `Validate()` / ProblemDetails guards |
| Mapping | AutoMapper 14.0.0 (final OSS release, deliberate) | none (that is the point) |
| ORM registration | `AddDbContext` + repositories over it | the `LayerCakeDbContext` injected straight into endpoint methods, no repository and no interface over it |
| Messaging | `RabbitMQ.Client` publisher behind an Application port, `BackgroundService` consumer in WebApi re-dispatching through MediatR, publish after commit, no outbox | Wolverine RabbitMQ transport: `PublishMessage<NotifyBaker>().ToRabbitQueue(...).UseDurableOutbox()` + `ListenToRabbitQueue`, all in `Program.cs`; the envelope commits in the same EF Core transaction as the order |
| Outside service (slice 005) | typed `HttpClient` behind `IPaymentGateway` (port in Application), `TendrPaymentGateway` adapter with options and wire DTOs in Infrastructure, exceptions mapped to 402/503 in the filter | typed `HttpClient` (`TendrClient`) as a method parameter on the load leg of `PlaceOrder`; 402/503 as a `Validate` guard |
| Database | PostgreSQL 17 (docker-compose live; Testcontainers under test) | same database, different schema |
| Tests | shared Alba + xUnit + Shouldly contract suite | the same suite, same scenarios |

**Version freeze:** pins live in `Directory.Packages.props` with the rationale. Frozen before KCDC's dry-run 1 (week of Aug 31) and still frozen between runs of the talk, so the comparison never drifts under a package update. A security patch justifies a bump; anything else is Erik's explicit call, recorded in that file and in the build log. One documented exception, 2026-09-09 (Erik's call): `WolverineFx.EntityFrameworkCore` and `WolverineFx.Postgresql` ADDED at 6.30.0 for the ORM move, both tracking the existing Wolverine pin, with EF Core and Npgsql reusing the before twin's pins. `Marten` and `WolverineFx.Marten` stay pinned for the experiment and for Tendr. Slice 005 added no package and bumped none: Tendr uses pins the twins and the Marten experiment already carry, and the before twin's Infrastructure project reaches `Microsoft.Extensions.Http` through a `FrameworkReference` to the ASP.NET Core shared framework, which is not a package. Known cost of the pin: on WolverineFx.Http 6.30.0 the HTTP transport resolves only `https://` endpoints (plain `http://` arrives in 6.34.0), which is why `tests/Tendr.Tests/HttpTransportFacts.cs` serves Tendr over HTTPS. Do not adopt Alba 9 (beta) without Erik's call.

---

## Architectural non-negotiables

1. **The before twin is built earnestly.** It represents real Clean Architecture .NET codebases and the talk says so. No sabotage, no strawman shortcuts, no deliberately bad code. If it would embarrass a competent 2019 architecture review, it does not ship.
2. **Same HTTP contract, byte-honest.** Both twins expose the identical surface, verified by the shared suite. Exact status-code assertions (never a 2xx range). Explicit `Content-Type` on request bodies. 404 for missing resources on both sides (do not use Wolverine's `OnMissing.EmptyContentWith204`). camelCase JSON on both sides.
3. **One deployable per twin, monolith.** No auth. No event sourcing (both twins are state-stored). No frontend on the critical path. Tendr, the vendor, is event-sourced on Marten on purpose; it is not a twin and not in the scorecard.
4. **After-twin idioms are Wolverine's, not explicit Result types.** No `IResult` mystery meat, no `OneOf<>`. Sad paths via `Validate`/`ValidateAsync` static methods returning `ProblemDetails` or `WolverineContinue.NoProblems`. Side effects and follow-on messages as return values (cascading), never an injected bus. Never call `SaveChangesAsync` in a handler; `AutoApplyTransactions` commits.
5. **Shared logic between slices is a deliberate, visible choice.** Coupon validation is ONE shared function used by both ValidateCoupon and PlaceOrder. It is the in-repo answer to "how do slices share logic?"
6. **Schema separation, one database, one ORM.** EF Core owns `before` and `after`, docker-compose owns PostgreSQL for the live demo (Testcontainers under test). Neither the database engine nor the ORM changes between twins; the architecture does. Tendr's Marten store owns a third schema, `tendr`, in the same database as a demo convenience; it never reads the twins' schemas.
7. **The after twin's DbContext is never edited per feature.** No `DbSet` properties, no per-entity mapping in it: `HasDefaultSchema("after")` plus `ApplyConfigurationsFromAssembly`. Each table's `IEntityTypeConfiguration<T>` lives in its own feature file next to the type it maps (`Cakes/Cake.cs` carries `Cake` and `CakeTable`), and endpoints reach for `db.Set<T>()`. This is what keeps the scorecard's "edited zero existing files" honest, and it is deliberate; do not add DbSets.
8. **`Storage.Store<T>` is a no-op against EF Core.** Wolverine's storage actions are upserts on Marten but generate `// No explicit update necessary with EF Core without a Version property` here, so a new row silently never lands. `NotifyBakerHandler` therefore returns an `ISideEffect` (`AddBakerTask`) and carries `[Transactional]`, because a handler with no DbContext parameter is invisible to `AutoApplyTransactions`. The handler stays a pure function; see `docs/build-log.md` (2026-09-09).
9. **A new non-nullable column on an after-twin table carries a database default.** The after twin has no migrations folder: Weasel diffs the model when the host starts, and a `NOT NULL` column with no default makes it drop and recreate the table (`DROP TABLE IF EXISTS after.orders CASCADE`), which silently empties a populated live database. `HasDefaultValue(...)` in the feature file's table configuration turns that into `ALTER TABLE ... ADD COLUMN`; `Order.PaymentStatus` (`atPickup`) is the example. The before twin's generated migrations do not have the problem. See `docs/build-log.md` (2026-09-14).

## C# style (both twins where applicable; after twin especially)

Write C# a JasperFx maintainer would recognize. Distilled from the author's `csharp-critter-style` skill (mmo-reconnect) and CritterStackSamples:

- One command + its validator + its endpoint per file, named after the command (`PublishCake.cs`).
- Commands verb-first imperative records (`PublishCake`, `NotifyBaker`); events, if any ever exist here, past-tense (`CakePublished`); HTTP response bodies are noun phrases (`PublishedCake`, `PlacedOrder`) so a tuple like `(PlacedOrder, NotifyBaker)` reads as response-plus-command at a glance. **Never suffix a type with a role word such as `Response`, `Request`, `Event`, or `Message`** when it can be avoided; name the type for what it is (Erik's call, 2026-09-05: without the layers, the labels are not needed). Endpoint classes `<VerbNoun>Endpoint`, static, with static methods.
- EF Core entities: plain mutable classes with `{ get; set; }`, never records, each with its `IEntityTypeConfiguration<T>` in the same file. Commands/queries: positional records.
- Inject `LayerCakeDbContext` as a method parameter, not a constructor. Pure reads say so: `AsNoTracking()` and `[NonTransactional]`.
- File-scoped namespaces, top-level `Program.cs`, Allman braces, `var` when apparent, collection expressions for empty defaults, no `#region`, no primary constructors on handler classes.
- REST-ish noun routes (`POST /cakes`), literal route strings in `[WolverineGet]`/`[WolverinePost]`.
- Comments explain Wolverine mechanics (cascading, outbox timing, `[Entity]`, why a read is `[NonTransactional]`), never restate code. Short conversational `/// <summary>` on entities and side effects.
- Tests: xUnit `[Fact]` + Shouldly + Alba scenarios, snake_case test method names.

The before twin follows conventional Clean Architecture idioms instead where they differ (constructor injection, repository interfaces, DTO mappers). That asymmetry is the exhibit, not an inconsistency.

## Talk-content rules

- **No em dashes or en dashes** in anything that could land on a slide, in the abstract, or in talk prose, and none in new repo text either (README, this file, `docs/`, commit messages, PR bodies): use a period, comma, colon, or parenthetical, and a hyphen or "to" for ranges. Older build-log entries keep theirs as history.
- **The file inventory is complete** (`docs/file-inventory.md`: per-slice files created and edited, counted honestly, plus whole-twin line counts). Slides quote it, never a fresh ad-hoc count. Numbers as of 2026-09-14 (slice 005, the v1.1 scorecard): before twin 2,405 raw / 2,002 non-blank lines of C#, after twin 1,000 / 841, about 2.4x (KCDC's scorecard quoted 2,140 / 1,775 against 844 / 707; before the 2026-09-09 ORM move the after twin was 703 / 581). Tendr's lines (305 / 251) belong to neither twin. The abstract claims "a dozen files"; the inventory's number is the number the slide says. If twin code changes, re-run the count by the inventory's stated method and update the inventory first. The inventory's template-experiment section (2026-09-06) sits outside those totals; the experiment's own line counts live there (2,340 raw / 1,882 non-blank as generated, 2,635 / 2,116 with slice 001 and ping) and never join the twins' numbers.
- The repo is public and attendees will clone it. The README speaks to them; no purist hedging.

---

## Session workflow (lightweight on purpose)

1. Read this file, then the slice spec in `docs/slices/` for whatever you are touching. The spec owns the contract clauses, the before twin's REQUIRED structure (do not collapse its layers; do not pad it either), the after twin's expected shape, and the scenario list. The `csharp-critter-style` skill (`.claude/skills/`) auto-activates for after-twin and test code; it does NOT apply to the before twin.
2. Stay scoped to the task at hand (one slice, one fix, one doc pass); no opportunistic edits elsewhere. Surfaced out-of-scope work becomes a `docs/build-log.md` line, not a change.
3. On finishing any change that touches twin code: run the suite (Docker running is enough), update `docs/file-inventory.md` if a file or line count moved, and record any decisions made along the way in `docs/build-log.md`. If the change moved the contract, a count, a scenario or fact total, a command, a port, or a package, bring `README.md` and this file along in the same PR; they are what a new session and an attendee read first. Doc-only changes get a build-log line and nothing else. A change under `experiments/` runs that experiment's own test project instead (the explicit `dotnet test` commands under Commands) and must leave the root suite untouched; re-run root `dotnet test` to show it still is.
4. There is no prompt/retro pipeline here (deliberate; the next delivery is the deadline). The build log is the memory between sessions.
5. Commit messages and PR bodies are plain: no AI co-author trailers, no "generated with" footers, no tool attribution of any kind.

### OpenSpec (adopted 2026-08-24)

Slice work runs through OpenSpec (`openspec/`, spec-driven schema, CLI 1.10.0), the original build and every build item since. Adopted thin: task checklists, on-rails sessions, and an archive trail, never re-planning.

- **One change per slice**, matching the build order: `slice-001-publish-browse-cakes`, `slice-002-validate-coupon`, `slice-003-place-order`, (added 2026-09-05) `slice-004-notify-baker-over-rabbitmq`, and (v1.1, 2026-09-14) `slice-005-pay-with-tendr`. The next slice takes the next number (`slice-006-...`). Each change spans before twin + after twin + shared contract scenarios end to end.
- Each slice starts with `/opsx:propose`, is implemented with `/opsx:apply`, and is archived with `/opsx:archive` only once the shared suite is green on both hosts and the bookkeeping (file inventory, build log, README and this file where they moved) is done. **All five changes are archived (slices 001 to 004 by 2026-09-05; slice 005, the first post-KCDC item, on 2026-09-14); none is open.** Work that is not a slice (doc refreshes, demo fixes, bug fixes) does not go through OpenSpec; a behaviour fix keeps `openspec/specs/` in step with what shipped.
- **Change artifacts derive from `docs/slices/`.** The settled slice specs are the source of truth: proposals link to them instead of restating them, delta-spec requirements and scenario names mirror them, and nothing settled gets re-litigated in a proposal. Ambiguity that survives the slice spec goes to the author, not into an assumption.
- **`openspec/specs/` accretes the built truth**: capabilities `cakes`, `coupons`, and `orders` materialize as slices archive (slices 004 and 005 extended `orders` rather than adding a capability). `docs/slices/` remains the design record; if the two diverge, the archived spec reflects what shipped and the divergence gets reconciled immediately.
- Authoring rules and operation guidance live in `openspec/config.yaml`.

## Slice slate (all five BUILT) and build order

Built **per-slice, end to end** (before + after + contract scenarios), in this order; slices 1 to 4 merged by 2026-09-05, slice 5 merged 2026-09-14 for v1.1. **Each slice has a full spec in `docs/slices/`; read it before touching that slice's code:**

| # | Slice | Teaches | Contract surface |
|---|---|---|---|
| 1 | **PublishCake** (+ BrowseCakes read beat) | Act 1 hook: trivial write through a dozen layered files | `POST /cakes`, `GET /cakes`, `GET /cakes/{id}` |
| 2 | **ValidateCoupon** | Railway Oriented Programming | `GET /coupons/{code}` always-200 envelope (statuses: invalid, notYetActive, expired, valid); coupons enter via seed data only |
| 3 | **PlaceOrder** | A-Frame, side effects via outbox | `POST /orders` (optional `couponCode`), `GET /orders/{id}`, baker to-do read |
| 4 | **NotifyBaker over RabbitMQ** (extends 3; slate amended by Erik 2026-09-05) | What one message costs a layered codebase vs. a slice | No new endpoint. The baker notification crosses a RabbitMQ queue on BOTH twins (`layercake-before-baker-tasks`, `layercake-after-baker-tasks`); the exactly-one-baker-task scenario is the proof |
| 5 | **Pay with Tendr** (extends 3; v1.1, Erik 2026-09-14) | What one call to an outside service costs a layered codebase vs. a slice; the seam you keep | Optional `card` on `POST /orders`; 402 (declined) / 503 (vendor down); `payment` on the order |

The three-slice lock stands for the talk's Act 3 features; slices 004 and 005 extend PlaceOrder (its side effect onto a broker, its load leg out to a vendor), not a fourth or fifth feature. The slate grows the same way between runs of the talk: a new row only by Erik's call, with its `docs/slices/` spec written before any code. Slice designs are lifted-and-simplified from CritterMart (`PublishProduct`, `ValidateCoupon`, `PlaceOrder`), state-stored here instead of event-sourced. Coupon statuses come from date mechanics only: exists → active window → valid. "Exhausted" is out of scope (no redemption caps). The baker notification must be synchronously observable (bakers' to-do table with a read endpoint, not log tailing).

**Schedule insurance, never needed (history):** the pre-agreed fallbacks were coupon collapsing to validate-only if PlaceOrder crowded the schedule, and a single-feature deep dive as the emergency compression. All four original slices shipped for KCDC, so neither applies. The static single-page frontend exists in `src/frontend/` (built 2026-08-25, spec `docs/frontend.md`); it is a demo surface only, not part of the proof, and the deck must never depend on it.

## Where detail lives

- **In this repo (agent-facing, canonical for BUILD):** `docs/slices/001-005` (per-slice specs: contract, required structure, scenarios, seeds), `src/tendr/Tendr/README.md` (the vendor's API and test cards), `docs/frontend.md` (the static demo page's design record), `docs/critter-stack-audit.md` (the after twin, suite, and console audited against the JasperFx skills; closed 2026-09-05, accepted divergences and their reasons recorded there), `docs/file-inventory.md` (the honest per-slice file counts; a slide depends on it), `docs/build-log.md` (decisions made mid-build), `openspec/` (change workflow per slice; `openspec/specs/` is canonical for what is BUILT so far).
- **The Marten experiment (2026-09-09):** the 2026-09-09 entry of `docs/build-log.md` (why the twins were put on one ORM, the `Storage.Store<T>` trap, the schema-creation decision, the results) and the 2026-09-09 section of `docs/file-inventory.md` (the file-by-file delta, the DbContext-caller list, the counts). Link to these; do not restate them.
- **Slice 005 and Tendr (2026-09-14):** the two 2026-09-14 entries of `docs/build-log.md` (the rung ordering verified on generated code with `[WolverineBefore]`, `AlwaysUseServiceLocationFor<TendrClient>()`, why the after twin's 503 comes from guard four, the table-drop migration trap, the Tendr test host and `WolverineHostGate`, the HTTP transport on 6.30.0, the calls for Erik) and the 005 section of `docs/file-inventory.md` (files, counts, the DbContext-caller list). Link to these; do not restate them.
- **The template experiment (2026-09-06):** the section of `docs/file-inventory.md` titled "Experiment: slice 001 on the Clean Architecture Solution Template" (file lists, the 9 / 8 number against 19 / 4 and 5 / 1, scaffold edits, line counts) and the 2026-09-06 entry of `docs/build-log.md` (package graph, idiom differences, the hop trace, why the suite is 6/10, the live run, the open calls). PR #15's body is the short version. Link to these; do not restate them.
- **Talk planning (canonical for the TALK, not mirrored here on purpose; narrative and slide beats stay out of the public repo):** the `presentations` repo, `how-i-gave-up-clean-architecture/` (plan.md with all locked decisions, slice-slate-gate.md, api-contract.md, the `handoff-*.md` files that start build items, `deck/`, `feedback-2026-09-10.md` from KCDC, and `research/`). Mirrored in the author's "Presentations & Talks" Claude project.
- **Slice design sources:** CritterMart (https://github.com/erikshafer/crittermart; locally a sibling checkout, `../crittermart`), the quarry, not the vehicle.
- **Generic Critter Stack mechanics:** the JasperFx ai-skills library (user-level, license required) and Context7 (`/jasperfx/wolverine`, `/jasperfx/marten`). This repo documents only what diverges.

## Open items (Erik's calls, recorded here until decided)

- Whether the enterprise-parody display name ("ShopSphere Commerce Platform" energy) appears anywhere (slides only vs. before solution-folder display name). Open since the build, the only build-era item left: nothing in `LayerCake.slnx` carries it, so the repo's de facto answer stays "slides only" unless Erik decides otherwise for a later run.
- Slice 005 (PR #19, 2026-09-14), three calls for Erik, detail under "For Erik" in the first 2026-09-14 build-log entry (its other letters are slide notes, not decisions): (c) whether the after twin should stop holding its EF Core transaction open across the Tendr call (up to the two-second timeout; the before twin opens its transaction only inside `SaveChangesAsync`), a talk-content call that was not tried; (d) whether `tests/Tendr.Tests` stays in the slnx (one more PostgreSQL container, no measurable wall-clock; dropping it is one line); (e) which before-twin count a slide quotes, 11 created / 8 edited with the `PaymentStatus` enum and `PaymentDto`, or 9 / 8 without (the inventory and README say 11 / 8).
- Template experiment (PR #15, 2026-09-06), three calls for Erik, detail under "For Erik" in the build-log entry: (a) keep `tests/LayerCake.ContractTests.CleanTemplate` at the honest 6/10, or apply the one-line `Results.Problem` fix in the template's exception handler as a listed scaffold edit and report 10/10; (b) whether the four Guid-key ripple edits count as feature edits (17 touched) or a one-time scaffold cost (13 touched), which decides the reading a slide quotes; (c) whether a second variant on the `Ardalis.CleanArchitecture.Template` package (`before-clean-arch`, about an hour) is worth building.

RESOLVED 2026-08-23 (details in `docs/slices/` and `docs/build-log.md`): error-shape parity (status + content type + reason discoverable, one shared assertion helper); two-schemas-one-database; PublishCake keeps the 409 duplicate-name guard; coupon validation is an always-200 envelope; `GET /cakes/{id}` stays.

RESOLVED 2026-08-25, upgrade pass (details in `docs/build-log.md`): JasperFx pins bumped to Wolverine 6.30.0 / Marten 9.29.0 before the freeze; CritterWatch 1.0.1 ADDED at Erik's explicit call: console host in `src/monitor/`, RabbitMQ in docker-compose, monitoring opt-in so the test suite never needs the console (since slice 004 the suite does start its own broker, for the baker notification, not for CritterWatch).

RESOLVED 2026-09-02 (details in `docs/build-log.md`): the contract suite runs on **Testcontainers** (`Testcontainers.PostgreSql` 4.14.0 ADDED at Erik's call, an addition rather than a bump): one `postgres:17` container per twin collection, so `dotnet test` needs only Docker. docker-compose remains for the live demo (twins on their ports, the frontend page, CritterWatch).

RESOLVED 2026-09-05, slice 004 (details in `docs/slices/004-notify-baker-over-rabbitmq.md` and `docs/build-log.md`): the baker notification crosses RabbitMQ on BOTH twins. Before twin: raw `RabbitMQ.Client` 7.2.2 (ADDED at Erik's call), port in Application, publisher in Infrastructure, `BackgroundService` consumer in WebApi, publish after commit, NO outbox (the gap is owned on a slide). After twin: `Program.cs` only, `UseDurableOutbox` on the publish rule. Suite: `Testcontainers.RabbitMq` 4.14.0 (ADDED), one `rabbitmq:4` container per twin collection; the after fixture no longer stubs external transports.

RESOLVED 2026-08-23, slice 001 build (details in `docs/build-log.md`): EF Core **migrations**, not `EnsureCreated`, for the before twin (applied at startup in Development). The camelCase-stored-JSON decision was a Marten one and now applies only to the experiment; the after twin's camelCase is the wire contract, which ASP.NET Core gives by default.

RESOLVED 2026-09-09, the ORM move (details in `docs/build-log.md` and the 2026-09-09 section of `docs/file-inventory.md`): the after twin runs on EF Core through Wolverine; the Marten twin moved to `experiments/after-marten/`. No migrations folder in the after twin (`UseEntityFrameworkCoreWolverineManagedMigrations` + `AddResourceSetupOnStartup`; `EnsureCreated` was rejected because it no-ops when the database already exists, and the twins share one). `Order.Lines` is an owned collection as jsonb (`OwnsMany(...).ToJson()`), one column, no child table. Baker notification is `AddBakerTask : ISideEffect` returned by a pure `[Transactional]` handler that checks for redelivery, because EF Core inserts where the document store upserted on identity. Scorecard: before twin 2,140 raw / 1,775 non-blank unchanged, after twin 844 / 707 (was 703 / 581); per-slice file counts unchanged path for path.

RESOLVED 2026-09-14, slice 005 (details in `docs/slices/005-pay-with-tendr.md`, `docs/build-log.md`, and the 005 section of `docs/file-inventory.md`): both twins authorize an optional `card` on `POST /orders` with Tendr over HTTP on the load leg, after the three existing guards; declined is `402`, a refused connection or the two-second timeout is `503`, no card behaves exactly as slice 003. Tendr (`src/tendr/Tendr`, port 42040, schema `tendr`) is in the slnx with `tests/Tendr.Tests`, and also answers `AuthorizeCard` over Wolverine's HTTP transport (neither twin uses that). Before twin: `IPaymentGateway` port in Application, `TendrPaymentGateway` typed-client adapter with options and wire DTOs in Infrastructure, two exceptions mapped in the filter, migration `AddOrderPayment`; 11 created / 8 edited. After twin: `Payments/Tendr.cs`, plus a `[WolverineBefore]` `AuthorizeAsync` rung and a `Validate(Payment?)` guard in `PlaceOrder.cs` and `AlwaysUseServiceLocationFor<TendrClient>()` in `Program.cs`; 1 created / 3 edited. No package added or bumped. Root `dotnet test`: contract 70/70 (35 scenarios x 2; the 27 earlier scenarios unchanged), after-twin facts 19/19, Tendr 20/20. Scorecard: before twin 2,405 / 2,002, after twin 1,000 / 841.
