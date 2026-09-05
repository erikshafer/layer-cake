# LayerCake — AI Development Guidelines

LayerCake is a tiny bakery selling layer cakes: built in layers, served in slices. It is the demo system for the KCDC 2026 talk **"How I Gave Up Clean Architecture, and Why My Code Got Simpler"** (Sept 10-11, 2026). The repo is a **laboratory**: one solution, two implementations of the identical HTTP API, one shared contract-test suite proving they behave the same.

- **Before twin** (`src/before/`, four projects): earnest .NET Clean Architecture. EF Core + Npgsql, MediatR, FluentValidation pipeline behavior, AutoMapper, DTOs, repository/service layering. Built sincerely, never as a strawman.
- **After twin** (`src/after/LayerCake.Slices`, one project): feature-folder vertical slices on Wolverine.Http + Marten documents.
- **The spine** (`tests/LayerCake.ContractTests`): one Alba scenario suite referencing both hosts. Scenarios live in abstract base classes; twin subclasses run the identical set against each host. `dotnet test` green twice is the talk's most credible artifact.

This file is the routing layer for AI sessions: the non-negotiables, the build order, and where detail lives. **The talk is the deadline. When ceremony conflicts with shipping the talk, shipping wins — explicitly, never silently.**

---

## Commands

```
dotnet build              # one solution, both twins + the CritterWatch console
dotnet test               # the money shot: identical scenarios, green twice (Docker running is the only prerequisite); also runs the after-twin-only pure-function unit tests in tests/LayerCake.Slices.Tests
docker compose up -d      # PostgreSQL 17 + RabbitMQ, for running the twins LIVE only (RabbitMQ only feeds CritterWatch)
```

`dotnet test` starts its own PostgreSQL 17 per twin via Testcontainers (`tests/LayerCake.ContractTests/TwinHosts.cs`); it never touches the compose database, RabbitMQ, or the console.

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
- **Record the before twin's per-slice file inventory as it is built** (append to `docs/file-inventory.md`). The abstract claims "a dozen files"; whatever the real number is, that is the number the slide says.
- The repo is public and attendees will clone it. The README speaks to them; no purist hedging.

---

## Session workflow (lightweight on purpose)

1. Read this file, then the slice spec in `docs/slices/` for whatever you are building. The spec owns the contract clauses, the before twin's REQUIRED structure (do not collapse its layers; do not pad it either), the after twin's expected shape, and the scenario list. The `csharp-critter-style` skill (`.claude/skills/`) auto-activates for after-twin and test code; it does NOT apply to the before twin.
2. Stay scoped to the slice being built; no opportunistic edits elsewhere. Surfaced out-of-scope work becomes a `docs/build-log.md` line, not a change.
3. On finishing a slice: run the suite (Docker running is enough), append the honest file list to `docs/file-inventory.md`, and record any decisions made along the way in `docs/build-log.md`.
4. There is no prompt/retro pipeline here (deliberate; the talk is the deadline). The build log is the memory between sessions.
5. Commit messages and PR bodies are plain: no AI co-author trailers, no "generated with" footers, no tool attribution of any kind.

### OpenSpec (adopted 2026-08-24)

Build-week work runs through OpenSpec (`openspec/`, spec-driven schema, CLI 1.10.0). Adopted thin: task checklists, on-rails sessions, and an archive trail — never re-planning.

- **One change per slice**, matching the build order: `slice-001-publish-browse-cakes`, `slice-002-validate-coupon`, `slice-003-place-order`. Each change spans before twin + after twin + shared contract scenarios end to end.
- Start a slice with `/opsx:propose`, implement with `/opsx:apply`, and `/opsx:archive` only when the shared suite is green on both hosts and the bookkeeping (file inventory, build log) is done.
- **Change artifacts derive from `docs/slices/`.** The settled slice specs are the source of truth: proposals link to them instead of restating them, delta-spec requirements and scenario names mirror them, and nothing settled gets re-litigated in a proposal. Ambiguity that survives the slice spec goes to the author, not into an assumption.
- **`openspec/specs/` accretes the built truth**: capabilities `cakes`, `coupons`, and `orders` materialize as slices archive. `docs/slices/` remains the design record; if the two diverge, the archived spec reflects what shipped and the divergence gets reconciled immediately.
- Authoring rules and operation guidance live in `openspec/config.yaml`.

## Slice slate (LOCKED) and build order

Build **per-slice, end to end** (before + after + contract scenarios), in this order. **Each slice has a full spec in `docs/slices/` — read it before writing code:**

| # | Slice | Teaches | Contract surface |
|---|---|---|---|
| 1 | **PublishCake** (+ BrowseCakes read beat) | Act 1 hook: trivial write through a dozen layered files | `POST /cakes`, `GET /cakes`, `GET /cakes/{id}` |
| 2 | **ValidateCoupon** | Railway Oriented Programming | `GET /coupons/{code}` always-200 envelope (statuses: invalid, notYetActive, expired, valid); coupons enter via seed data only |
| 3 | **PlaceOrder** | A-Frame, side effects via outbox | `POST /orders` (optional `couponCode`), `GET /orders/{id}`, baker to-do read |

Slice designs are lifted-and-simplified from CritterMart (`PublishProduct`, `ValidateCoupon`, `PlaceOrder`), state-stored here instead of event-sourced. Coupon statuses come from date mechanics only: exists → active window → valid. "Exhausted" is out of scope (no redemption caps). The baker notification must be synchronously observable (bakers' to-do table with a read endpoint, not log tailing).

**Pre-agreed fallbacks:** coupon collapses to validate-only if PlaceOrder crowds the schedule; single-feature deep dive is the emergency compression. The static single-page frontend exists in `src/frontend/` (built 2026-08-25, spec `docs/frontend.md`); it is a demo surface only, not part of the proof, and the deck must never depend on it.

## Where detail lives

- **In this repo (agent-facing, canonical for BUILD):** `docs/slices/001-003` (per-slice specs: contract, required structure, scenarios, seeds), `docs/frontend.md` (the static demo page's design record), `docs/file-inventory.md` (the honest per-slice file counts; a slide depends on it), `docs/build-log.md` (decisions made mid-build), `openspec/` (change workflow per slice; `openspec/specs/` is canonical for what is BUILT so far).
- **Talk planning (canonical for the TALK, not mirrored here on purpose — narrative and slide beats stay out of the public repo):** the `presentations` repo, `how-i-gave-up-clean-architecture/` (plan.md with all locked decisions, slice-slate-gate.md, api-contract.md, jasperfx-research.md). Mirrored in the author's "Presentations & Talks" Claude project.
- **Slice design sources:** CritterMart (`C:\Code\crittermart`), the quarry, not the vehicle.
- **Generic Critter Stack mechanics:** the JasperFx ai-skills library (user-level, license required) and Context7 (`/jasperfx/wolverine`, `/jasperfx/marten`). This repo documents only what diverges.

## Open items (decide during build, record here)

- Whether the enterprise-parody display name ("ShopSphere Commerce Platform" energy) appears anywhere (slides only vs. before solution-folder display name).

RESOLVED 2026-08-23 (details in `docs/slices/` and `docs/build-log.md`): error-shape parity (status + content type + reason discoverable, one shared assertion helper); two-schemas-one-database; PublishCake keeps the 409 duplicate-name guard; coupon validation is an always-200 envelope; `GET /cakes/{id}` stays.

RESOLVED 2026-08-25, upgrade pass (details in `docs/build-log.md`): JasperFx pins bumped to Wolverine 6.30.0 / Marten 9.29.0 before the freeze; CritterWatch 1.0.1 ADDED at Erik's explicit call — console host in `src/monitor/`, RabbitMQ in docker-compose, monitoring opt-in so the test suite stays broker-free.

RESOLVED 2026-09-02 (details in `docs/build-log.md`): the contract suite runs on **Testcontainers** (`Testcontainers.PostgreSql` 4.14.0 ADDED at Erik's call, an addition rather than a bump): one `postgres:17` container per twin collection, so `dotnet test` needs only Docker. docker-compose remains for the live demo (twins on their ports, the frontend page, CritterWatch).

RESOLVED 2026-08-23, slice 001 build (details in `docs/build-log.md`): EF Core **migrations**, not `EnsureCreated`, for the before twin (applied at startup in Development); Marten stored-JSON casing is **explicit camelCase** via `opts.UseSystemTextJsonForSerialization(casing: Casing.CamelCase)` (verified in `after.mt_doc_cake`).
