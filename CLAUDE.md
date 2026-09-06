# LayerCake — AI Development Guidelines

LayerCake is a tiny bakery selling layer cakes: built in layers, served in slices. It is the demo system for the KCDC 2026 talk **"How I Gave Up Clean Architecture, and Why My Code Got Simpler"** (Sept 10-11, 2026). The repo is a **laboratory**: one solution, two implementations of the identical HTTP API, one shared contract-test suite proving they behave the same.

- **Before twin** (`src/before/`, four projects): earnest .NET Clean Architecture. EF Core + Npgsql, MediatR, FluentValidation pipeline behavior, AutoMapper, DTOs, repository/service layering. Built sincerely, never as a strawman.
- **After twin** (`src/after/LayerCake.Slices`, one project): feature-folder vertical slices on Wolverine.Http + Marten documents.
- **The spine** (`tests/LayerCake.ContractTests`): one Alba scenario suite referencing both hosts. Scenarios live in abstract base classes; twin subclasses run the identical set against each host. `dotnet test` green twice is the talk's most credible artifact.

This file is the routing layer for AI sessions: the non-negotiables, the build order, and where detail lives. **The talk is the deadline. When ceremony conflicts with shipping the talk, shipping wins — explicitly, never silently.**

**Mode since 2026-09-05: demo readiness, not build.** All four slices are merged and archived (`main` at `74034cd`, PR #13; suite 54/54 on both hosts, unit tests 15/15, 0 warnings). No new slices, no package bumps (security patches excepted), no re-planning. Sessions now exist for dry-run support, bug fixes that keep the suite green twice, and doc accuracy. A fix that changes built behaviour updates `openspec/specs/`, `docs/file-inventory.md` (if a count moved), and `docs/build-log.md` in the same PR, so every document keeps saying what shipped.

**Experiments (since 2026-09-06).** `experiments/` holds hosts that test a claim rather than ship a slice. Today that is `experiments/before-clean-template/` (the Clean Architecture Solution Template, `dotnet new ca-sln` 10.8.0, generated as-is with slice 001 built its way) and its test project `tests/LayerCake.ContractTests.CleanTemplate/` (PR #15, merged 2026-09-06). Both are outside `LayerCake.slnx`, outside the scorecard, and outside the version freeze (the template's package graph is its own, deliberately unpinned by the repo). Experiments do not run the OpenSpec loop and never touch `src/`, `openspec/`, `docs/slices/`, or the shared scenario classes. Name experiment folders and namespaces after the artifact (`before-clean-template`, `LayerCake.CleanTemplate`; a variant on the `Ardalis.CleanArchitecture.Template` package would be `before-clean-arch`), never after a person.

---

## Commands

```
dotnet build              # one solution, both twins + the CritterWatch console
dotnet test               # the money shot: identical scenarios, green twice (Docker running is the only prerequisite; Testcontainers starts PostgreSQL AND RabbitMQ per twin); also runs the after-twin-only pure-function unit tests in tests/LayerCake.Slices.Tests
dotnet test tests/LayerCake.ContractTests.CleanTemplate/LayerCake.ContractTests.CleanTemplate.csproj
                          # the template experiment only (Docker, PostgreSQL via Testcontainers, no broker); root dotnet test excludes it; 6/10 as committed is the template as shipped
docker compose up -d      # PostgreSQL 17 + RabbitMQ, for running the twins LIVE only (both twins put the baker notification on the broker; CritterWatch rides it too)
```

`dotnet test` starts its own PostgreSQL 17 and RabbitMQ 4 per twin via Testcontainers (`tests/LayerCake.ContractTests/TwinHosts.cs`); it never touches the compose database, the compose broker, or the console. Root `dotnet test` excludes `experiments/`: the template host's test project is outside the slnx and runs only through the explicit command above.

Frontend demo page: run both twins, then open `src/frontend/index.html` straight from disk (no build step, no server).

Ports (live demo only; Alba self-hosts in tests): before twin `42010`, after twin `42020`, CritterWatch console `42030` (`src/monitor/`). Swagger UI at `/swagger` on both twins in Development. The after twin publishes CritterWatch telemetry only when `CritterWatch:Enabled` is true (set via launchSettings env var; the contract tests never set it).

---

## Tech stack

| Concern | Before twin | After twin |
|---|---|---|
| Runtime | .NET 10, C# 14 | .NET 10, C# 14 |
| HTTP | ASP.NET Core controllers | Wolverine.Http endpoints |
| Mediation | MediatR 12.5.0 (final OSS release, deliberate) | Wolverine handlers |
| Persistence | EF Core 10 + Npgsql, schema `before` | Marten documents, schema `after` |
| Validation | FluentValidation via MediatR pipeline behavior | Wolverine `Validate()` / ProblemDetails guards |
| Mapping | AutoMapper 14.0.0 (final OSS release, deliberate) | none (that is the point) |
| Messaging | `RabbitMQ.Client` publisher behind an Application port, `BackgroundService` consumer in WebApi re-dispatching through MediatR, publish after commit, no outbox | Wolverine RabbitMQ transport: `PublishMessage<NotifyBaker>().ToRabbitQueue(...).UseDurableOutbox()` + `ListenToRabbitQueue`, all in `Program.cs` |
| Database | PostgreSQL 17 (docker-compose live; Testcontainers under test) | same database, different schema |
| Tests | shared Alba + xUnit + Shouldly contract suite | the same suite, same scenarios |

**Version freeze:** pins live in `Directory.Packages.props` with the rationale. Frozen before dry-run 1 (week of Aug 31). Only security patches justify a bump after the freeze. Do not adopt Alba 9 (beta) before the talk.

---

## Architectural non-negotiables

1. **The before twin is built earnestly.** It represents real Clean Architecture .NET codebases and the talk says so. No sabotage, no strawman shortcuts, no deliberately bad code. If it would embarrass a competent 2019 architecture review, it does not ship.
2. **Same HTTP contract, byte-honest.** Both twins expose the identical surface, verified by the shared suite. Exact status-code assertions (never a 2xx range). Explicit `Content-Type` on request bodies. 404 for missing resources on both sides (do not use Wolverine's `OnMissing.EmptyContentWith204`). camelCase JSON on both sides.
3. **One deployable per twin, monolith.** No auth. No event sourcing (both twins are state-stored; Marten is used as a document store only). No frontend on the critical path.
4. **After-twin idioms are Wolverine's, not explicit Result types.** No `IResult` mystery meat, no `OneOf<>`. Sad paths via `Validate`/`ValidateAsync` static methods returning `ProblemDetails` or `WolverineContinue.NoProblems`. Side effects and follow-on messages as return values (cascading), never an injected bus. Never call `SaveChangesAsync` in a handler; `AutoApplyTransactions` commits.
5. **Shared logic between slices is a deliberate, visible choice.** Coupon validation is ONE shared function used by both ValidateCoupon and PlaceOrder. It is the in-repo answer to "how do slices share logic?"
6. **Schema separation, one database.** EF Core owns `before`, Marten owns `after`, docker-compose owns PostgreSQL for the live demo (Testcontainers under test). The database engine never changes between twins; only the access idiom does.

## C# style (both twins where applicable; after twin especially)

Write C# a JasperFx maintainer would recognize. Distilled from the author's `csharp-critter-style` skill (mmo-reconnect) and CritterStackSamples:

- One command + its validator + its endpoint per file, named after the command (`PublishCake.cs`).
- Commands verb-first imperative records (`PublishCake`, `NotifyBaker`); events, if any ever exist here, past-tense (`CakePublished`); HTTP response bodies are noun phrases (`PublishedCake`, `PlacedOrder`) so a tuple like `(PlacedOrder, NotifyBaker)` reads as response-plus-command at a glance. **Never suffix a type with a role word such as `Response`, `Request`, `Event`, or `Message`** when it can be avoided; name the type for what it is (Erik's call, 2026-09-05: without the layers, the labels are not needed). Endpoint classes `<VerbNoun>Endpoint`, static, with static methods.
- Marten documents: plain mutable classes with `{ get; set; }`, never records. Commands/queries: positional records.
- Inject `IDocumentSession`/`IQuerySession` as method parameters, not constructors. `IQuerySession` for pure reads.
- File-scoped namespaces, top-level `Program.cs`, Allman braces, `var` when apparent, collection expressions for empty defaults, no `#region`, no primary constructors on handler classes.
- REST-ish noun routes (`POST /cakes`), literal route strings in `[WolverineGet]`/`[WolverinePost]`.
- Comments explain Wolverine/Marten mechanics (cascading, outbox timing, `[Entity]`), never restate code. Short conversational `/// <summary>` on documents/aggregates.
- Tests: xUnit `[Fact]` + Shouldly + Alba scenarios, snake_case test method names.

The before twin follows conventional Clean Architecture idioms instead where they differ (constructor injection, repository interfaces, DTO mappers). That asymmetry is the exhibit, not an inconsistency.

## Talk-content rules

- **No em dashes** in anything that could land on a slide, in the abstract, or in talk prose. Em dashes are fine in repo markdown like this file and the README.
- **The file inventory is complete** (`docs/file-inventory.md`: per-slice files created and edited, counted honestly, plus whole-twin line counts). Slides quote it, never a fresh ad-hoc count. Final numbers as of 2026-09-05 (slice 004 plus the consumer shutdown fix): before twin 2,140 raw / 1,775 non-blank lines of C#, after twin 703 / 581. The abstract claims "a dozen files"; the inventory's number is the number the slide says. If twin code changes, re-run the count by the inventory's stated method and update the inventory first. The inventory's final section (the 2026-09-06 template experiment) sits outside those totals, which are unchanged; the experiment's own line counts live there (2,340 raw / 1,882 non-blank as generated, 2,635 / 2,116 with slice 001 and ping) and never join the twins' numbers.
- The repo is public and attendees will clone it. The README speaks to them; no purist hedging.

---

## Session workflow (lightweight on purpose)

1. Read this file, then the slice spec in `docs/slices/` for whatever you are touching. The spec owns the contract clauses, the before twin's REQUIRED structure (do not collapse its layers; do not pad it either), the after twin's expected shape, and the scenario list. The `csharp-critter-style` skill (`.claude/skills/`) auto-activates for after-twin and test code; it does NOT apply to the before twin.
2. Stay scoped to the task at hand (one slice, one fix, one doc pass); no opportunistic edits elsewhere. Surfaced out-of-scope work becomes a `docs/build-log.md` line, not a change.
3. On finishing any change that touches twin code: run the suite (Docker running is enough), update `docs/file-inventory.md` if a file or line count moved, and record any decisions made along the way in `docs/build-log.md`. Doc-only changes get a build-log line and nothing else. A change under `experiments/` runs the CleanTemplate project instead (the explicit `dotnet test` command under Commands) and must leave the root suite untouched; re-run root `dotnet test` to show it still is.
4. There is no prompt/retro pipeline here (deliberate; the talk is the deadline). The build log is the memory between sessions.
5. Commit messages and PR bodies are plain: no AI co-author trailers, no "generated with" footers, no tool attribution of any kind.

### OpenSpec (adopted 2026-08-24)

Build-week work runs through OpenSpec (`openspec/`, spec-driven schema, CLI 1.10.0). Adopted thin: task checklists, on-rails sessions, and an archive trail — never re-planning.

- **One change per slice**, matching the build order: `slice-001-publish-browse-cakes`, `slice-002-validate-coupon`, `slice-003-place-order`, and (added 2026-09-05) `slice-004-notify-baker-over-rabbitmq`. Each change spans before twin + after twin + shared contract scenarios end to end.
- During the build, each slice started with `/opsx:propose`, was implemented with `/opsx:apply`, and was archived with `/opsx:archive` only once the shared suite was green on both hosts and the bookkeeping (file inventory, build log) was done. **All four changes are archived (last one 2026-09-05).** Post-build work (doc refreshes, demo fixes, bug fixes) does not go through OpenSpec; a behaviour fix keeps `openspec/specs/` in step with what shipped.
- **Change artifacts derive from `docs/slices/`.** The settled slice specs are the source of truth: proposals link to them instead of restating them, delta-spec requirements and scenario names mirror them, and nothing settled gets re-litigated in a proposal. Ambiguity that survives the slice spec goes to the author, not into an assumption.
- **`openspec/specs/` accretes the built truth**: capabilities `cakes`, `coupons`, and `orders` materialize as slices archive. `docs/slices/` remains the design record; if the two diverge, the archived spec reflects what shipped and the divergence gets reconciled immediately.
- Authoring rules and operation guidance live in `openspec/config.yaml`.

## Slice slate (LOCKED, all four BUILT) and build order

Built **per-slice, end to end** (before + after + contract scenarios), in this order; the last one merged 2026-09-05. **Each slice has a full spec in `docs/slices/` — read it before touching that slice's code:**

| # | Slice | Teaches | Contract surface |
|---|---|---|---|
| 1 | **PublishCake** (+ BrowseCakes read beat) | Act 1 hook: trivial write through a dozen layered files | `POST /cakes`, `GET /cakes`, `GET /cakes/{id}` |
| 2 | **ValidateCoupon** | Railway Oriented Programming | `GET /coupons/{code}` always-200 envelope (statuses: invalid, notYetActive, expired, valid); coupons enter via seed data only |
| 3 | **PlaceOrder** | A-Frame, side effects via outbox | `POST /orders` (optional `couponCode`), `GET /orders/{id}`, baker to-do read |
| 4 | **NotifyBaker over RabbitMQ** (extends 3; slate amended by Erik 2026-09-05) | What one message costs a layered codebase vs. a slice | No new endpoint. The baker notification crosses a RabbitMQ queue on BOTH twins (`layercake-before-baker-tasks`, `layercake-after-baker-tasks`); the exactly-one-baker-task scenario is the proof |

The three-slice lock stands for the talk's Act 3 features; slice 004 is an extension of PlaceOrder's side effect, not a fourth feature. Slice designs are lifted-and-simplified from CritterMart (`PublishProduct`, `ValidateCoupon`, `PlaceOrder`), state-stored here instead of event-sourced. Coupon statuses come from date mechanics only: exists → active window → valid. "Exhausted" is out of scope (no redemption caps). The baker notification must be synchronously observable (bakers' to-do table with a read endpoint, not log tailing).

**Schedule insurance, never needed (history):** the pre-agreed fallbacks were coupon collapsing to validate-only if PlaceOrder crowded the schedule, and a single-feature deep dive as the emergency compression. All four slices shipped, so neither applies. The static single-page frontend exists in `src/frontend/` (built 2026-08-25, spec `docs/frontend.md`); it is a demo surface only, not part of the proof, and the deck must never depend on it.

## Where detail lives

- **In this repo (agent-facing, canonical for BUILD):** `docs/slices/001-004` (per-slice specs: contract, required structure, scenarios, seeds), `docs/frontend.md` (the static demo page's design record), `docs/critter-stack-audit.md` (the after twin, suite, and console audited against the JasperFx skills; closed 2026-09-05, accepted divergences and their reasons recorded there), `docs/file-inventory.md` (the honest per-slice file counts; a slide depends on it), `docs/build-log.md` (decisions made mid-build), `openspec/` (change workflow per slice; `openspec/specs/` is canonical for what is BUILT so far).
- **The template experiment (2026-09-06):** the final section of `docs/file-inventory.md` ("Experiment: slice 001 on the Clean Architecture Solution Template": file lists, the 9 / 8 number against 19 / 4 and 5 / 1, scaffold edits, line counts) and the 2026-09-06 entry of `docs/build-log.md` (package graph, idiom differences, the hop trace, why the suite is 6/10, the live run, the open calls). PR #15's body is the short version. Link to these; do not restate them.
- **Talk planning (canonical for the TALK, not mirrored here on purpose — narrative and slide beats stay out of the public repo):** the `presentations` repo, `how-i-gave-up-clean-architecture/` (plan.md with all locked decisions, slice-slate-gate.md, api-contract.md, jasperfx-research.md). Mirrored in the author's "Presentations & Talks" Claude project.
- **Slice design sources:** CritterMart (`C:\Code\crittermart`), the quarry, not the vehicle.
- **Generic Critter Stack mechanics:** the JasperFx ai-skills library (user-level, license required) and Context7 (`/jasperfx/wolverine`, `/jasperfx/marten`). This repo documents only what diverges.

## Open items (decide during build, record here)

- Whether the enterprise-parody display name ("ShopSphere Commerce Platform" energy) appears anywhere (slides only vs. before solution-folder display name). Still open 2026-09-05, the only build-era item left: nothing in `LayerCake.slnx` carries it today, so the repo's de facto answer is "slides only" unless Erik decides otherwise before the deck locks.
- Template experiment (PR #15, 2026-09-06), three calls for Erik, detail under "For Erik" in the build-log entry: (a) keep `tests/LayerCake.ContractTests.CleanTemplate` at the honest 6/10, or apply the one-line `Results.Problem` fix in the template's exception handler as a listed scaffold edit and report 10/10; (b) whether the four Guid-key ripple edits count as feature edits (17 touched) or a one-time scaffold cost (13 touched), which decides the reading a slide quotes; (c) whether a second variant on the `Ardalis.CleanArchitecture.Template` package (`before-clean-arch`, about an hour) is worth building.

RESOLVED 2026-08-23 (details in `docs/slices/` and `docs/build-log.md`): error-shape parity (status + content type + reason discoverable, one shared assertion helper); two-schemas-one-database; PublishCake keeps the 409 duplicate-name guard; coupon validation is an always-200 envelope; `GET /cakes/{id}` stays.

RESOLVED 2026-08-25, upgrade pass (details in `docs/build-log.md`): JasperFx pins bumped to Wolverine 6.30.0 / Marten 9.29.0 before the freeze; CritterWatch 1.0.1 ADDED at Erik's explicit call — console host in `src/monitor/`, RabbitMQ in docker-compose, monitoring opt-in so the test suite never needs the console (since slice 004 the suite does start its own broker, for the baker notification, not for CritterWatch).

RESOLVED 2026-09-02 (details in `docs/build-log.md`): the contract suite runs on **Testcontainers** (`Testcontainers.PostgreSql` 4.14.0 ADDED at Erik's call, an addition rather than a bump): one `postgres:17` container per twin collection, so `dotnet test` needs only Docker. docker-compose remains for the live demo (twins on their ports, the frontend page, CritterWatch).

RESOLVED 2026-09-05, slice 004 (details in `docs/slices/004-notify-baker-over-rabbitmq.md` and `docs/build-log.md`): the baker notification crosses RabbitMQ on BOTH twins. Before twin: raw `RabbitMQ.Client` 7.2.2 (ADDED at Erik's call), port in Application, publisher in Infrastructure, `BackgroundService` consumer in WebApi, publish after commit, NO outbox (the gap is owned on a slide). After twin: `Program.cs` only, `UseDurableOutbox` on the publish rule. Suite: `Testcontainers.RabbitMq` 4.14.0 (ADDED), one `rabbitmq:4` container per twin collection; the after fixture no longer stubs external transports.

RESOLVED 2026-08-23, slice 001 build (details in `docs/build-log.md`): EF Core **migrations**, not `EnsureCreated`, for the before twin (applied at startup in Development); Marten stored-JSON casing is **explicit camelCase** via `opts.UseSystemTextJsonForSerialization(casing: Casing.CamelCase)` (verified in `after.mt_doc_cake`).
