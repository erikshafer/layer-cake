# Critter Stack practices audit (2026-09-05)

An audit of the after twin (`src/after/LayerCake.Slices`), the shared contract suite (`tests/LayerCake.ContractTests`), and the CritterWatch console (`src/monitor/`) against the current JasperFx AI skills (`~/.claude/skills/wolverine-*`, `marten-*`, `critterstack-*`, `wolverine-integrations-critterwatch-setup`) plus the repo's own `csharp-critter-style` skill. The before twin is out of scope by design (CLAUDE.md non-negotiable 1).

Skills consulted: wolverine-http-fundamentals, wolverine-handlers-fundamentals, wolverine-handlers-a-frame-architecture, wolverine-handlers-railway-programming, wolverine-handlers-declarative-persistence, critterstack-arch-new-project-wolverine-marten, critterstack-arch-vertical-slice-fundamentals, wolverine-testing-alba, wolverine-testing-integration, wolverine-testing-integration-marten, wolverine-testing-with-testcontainers, wolverine-observability-code-generation, wolverine-integrations-critterwatch-setup, csharp-critter-style. The outbox finding was additionally checked against the Wolverine docs (`docs/guide/durability/marten/inbox.md`, `docs/tutorials/modular-monolith.md` via Context7).

Verdict in one line: the after twin is idiomatic Critter Stack code and would pass a JasperFx maintainer's glance. One claim the talk makes about it is not currently true (Tier A1). Everything else is either a two-line hardening or a judgment call worth a conversation.

---

## Tier A: act before the talk

**Status 2026-09-05: both items shipped.** A1 added `opts.Policies.UseDurableLocalQueues()` to the after twin's `Program.cs`; `dotnet run -- describe` now lists the local queues as `Durable`. A2 added solo mode and stubbed external transports to `AfterHostFixture`. Suite 54/54 on both twins, 0 warnings. Details in `docs/build-log.md`; line counts in `docs/file-inventory.md`. The text below is preserved as the finding record.

### A1. The baker-task side effect is NOT on a durable outbox (the talk says it is)

**What the repo claims.** README slice table ("sends that through Marten's transactional outbox, so no order without a task and no task without an order"), `docs/slices/003-place-order.md` ("Wolverine's outbox carries the notify-the-baker side effect"), CLAUDE.md slice slate ("side effects via outbox"), the comment in `PlaceOrder.Post` ("rides Marten's outbox in the SAME transaction"), and the build-log line about `BakerTask.Id` upsert being idempotent "under the outbox's at-least-once redelivery".

**What the code does.** `NotifyBaker` is a cascaded message with a local handler, so Wolverine routes it to a local queue. Nothing in `Program.cs` marks that queue durable: no `opts.Policies.UseDurableLocalQueues()`, no `opts.LocalQueue(...).UseDurableInbox()`. Per the Wolverine docs, a local queue is buffered in memory unless marked durable; `IntegrateWithWolverine()` creates the envelope tables but only durable endpoints use them.

**What that means today.** `IntegrateWithWolverine()` + `AutoApplyTransactions()` does give you the "don't send before commit" half: the message is held until `SaveChangesAsync` succeeds, so there is never a baker task for an order that failed to commit. It does not give you the other half: if the process dies after the order commits but before `NotifyBakerHandler` runs, the message is gone and the order has no task. "No order without a baker task" is therefore not guaranteed. The at-least-once redelivery the build log mentions also does not apply to an in-memory queue, so the identity-upsert idempotency in `NotifyBakerHandler` is currently defending against a path that cannot happen.

**Recommendation: one line in `Program.cs`, inside `UseWolverine`.**

```csharp
// Local queues are in-memory by default. Durable makes the cascaded NotifyBaker
// an actual outbox message: written to wolverine_incoming_envelopes in the same
// Marten transaction as the order, replayed after a crash, at-least-once.
opts.Policies.UseDurableLocalQueues();
```

This makes every existing sentence about the outbox true, makes the idempotency comment in `NotifyBakerHandler` earn its keep, and gives CritterWatch something real to show in the durability views. The contract suite should stay green unchanged (the after collection's container already gets the Wolverine tables from `IntegrateWithWolverine()`; the fixture's `ApplyAllConfiguredChangesToDatabaseAsync` covers them). Verify by running the suite and, on the compose database, watching a row appear and disappear in `after.wolverine_incoming_envelopes` when placing an order live.

The alternative is to soften the wording everywhere to "deferred until commit", which is weaker on stage. Not recommended.

**Talk angle.** This is a genuinely good one-liner for the deck: the difference between "send after commit" and "outbox" is one policy line, and the audience will have been bitten by exactly this distinction in MassTransit/NServiceBus/hand-rolled code.

### A2. Test fixture: run the after twin in solo mode

`AfterHostFixture` boots the host with only a connection-string override. Every JasperFx testing skill's canonical fixture also calls:

```csharp
x.ConfigureServices(services =>
{
    services.DisableAllExternalWolverineTransports();
    services.RunWolverineInSoloMode();
});
```

Solo mode skips node registration and leader election, so each of the five sequential after-twin hosts boots faster and leaves no stale `wolverine_nodes` rows behind. `DisableAllExternalWolverineTransports()` is belt-and-braces here (RabbitMQ is already gated behind `CritterWatch:Enabled`, which the tests never set) and is safe because the after twin uses conventional local routing only, so nothing gets dropped into a `NullSender`. Two lines, no behavior change, and it matches what an attendee will read in the skills.

If A1 lands, A2 matters slightly more: durable local queues engage the durability agent, and solo mode is the documented way to run that in a test host.

---

## Tier B: discuss (it depends)

Each item is a defensible divergence or a judgment call. Listed roughly by how much it would change what the audience sees.

**Status 2026-09-05, first pass:** B1 done (`NotifyBakerHandler` returns `Store<BakerTask>`). B5 done (`tests/LayerCake.Slices.Tests`, 15 facts, no host). B9 checked: the console really does run a 6.29.1/6.30.0 Wolverine mix, but pinning down to 6.29.1 does not compile because the official quickstart's `ProcessInParallelWithNativeAcks()` is a 6.30 API; the mix is what JasperFx's own sample expects, re-verified live, reasoning on the console csproj. B2, B3, B4, B6, B7, B8, B10, B11 remain open. Details in `docs/build-log.md`.

**Status 2026-09-05, second pass:** B3 and B4 done. `Features/` is gone; slices live in `Cakes/`, `Coupons/`, `Orders/` beside their documents with matching namespaces, `Ping.cs` at the project root, `NotifyBakerHandler.cs` renamed `NotifyBaker.cs`. Eleven redundant usings dropped (after twin 695 raw / 574 non-blank). B2, B6, B7, B8, B10, B11 remain open.

**Status 2026-09-05, third pass:** B2 done. The two HTTP response records are now noun phrases, `PublishedCake` and `PlacedOrder`, so the past-tense event convention is reserved for events (of which this repo has none; `NotifyBaker` is the only message and it is a command). `Response`/`Request` suffixes were considered and rejected as a standing rule, now in CLAUDE.md. B6, B7, B8, B10, B11 remain open.

### B1. `NotifyBakerHandler` could be a pure function

Current shape injects `IDocumentSession` and calls `session.Store(...)`. The declarative-persistence skill's stated preference for a simple write is a storage-action return:

```csharp
public static Store<BakerTask> Handle(NotifyBaker message)
    => Storage.Store(new BakerTask { Id = message.OrderId, ... });
```

No session, no async, unit-testable with no infrastructure, and the identity-upsert comment still applies (`Store` is upsert). Against: `session.Store` is the shape the Clean Architecture audience already understands, and the "look, a document write" beat is clearer with it. The skill lists both as acceptable ("need explicit session control → inject `IDocumentSession` directly"). Talk decision.

The same option exists for `PublishCake.Post` (`(CakePublished, Insert<Cake>)`) and `PlaceOrder.Post` (`(OrderPlaced, NotifyBaker, Store<Order>)`). I would not touch those: the endpoints already read as the A-Frame beat and adding a third tuple element costs slide real estate.

### B2. Response types named like events

`CakePublished` and `OrderPlaced` are HTTP response records (they implement `IHttpAware`), but they carry past-tense event names. In the samples, past tense means "cascaded event". A reader skimming `(OrderPlaced, NotifyBaker)` may assume both elements are messages. Options: rename to `CakePublishedResponse` / `OrderPlacedResponse`, or leave as is because the names read well on a slide and the `IHttpAware` implementation is right there. Cosmetic; discuss.

The underlying divergence from `CreationResponse` is recorded and sound: `CreationResponse` serializes its `Url` into the body, which would break byte-honest parity with the before twin. `IHttpAware` is the skill-sanctioned fallback. Keep.

### B3. Folder layout: `Features/` flat vs bounded-context folders

Style rule 4 says name slice folders after the context (`Cakes/`, `Orders/`), not a technical grouping. The after twin already has `Cakes/`, `Coupons/`, `Orders/` for documents but puts every slice under `Features/` (with `Features/Coupons/` as an odd nested exception). The skill layout would be `Cakes/PublishCake.cs`, `Cakes/Cake.cs`, `Orders/PlaceOrder.cs`, and so on. File count is unchanged, so the slide is unaffected, but `docs/file-inventory.md` paths and any screenshots would need refreshing. Cheap to do, cheap to skip. Depends on whether the deck shows the tree.

### B4. `NotifyBakerHandler.cs` file name

Style rule 1 names the file after the message (`NotifyBaker.cs`). The message record already lives in that file, so this is a rename only. Trivial; batch it with B3 if B3 happens.

### B5. No pure-function unit tests exist

`PlaceOrderEndpoint.Decide`, `PlaceOrderEndpoint.Validate`, and `CouponValidation.Evaluate` are exactly the "test the handler with no mocks" exhibit every Wolverine skill leads with, and the repo has no test that calls them directly. The reason is deliberate: the spine is one shared suite run twice, and after-only tests break that symmetry. But a small `tests/LayerCake.Slices.Tests` project with three or four `[Fact]`s would be the strongest evidence for the talk's "my code got simpler" claim (compare with what it takes to unit test the before twin's `PlaceOrderCommandHandler`). It also changes nothing about the twin line counts. Talk decision; if yes, it is an hour of work.

### B6. Clock sourcing

`DateTimeOffset.UtcNow` appears inline in `PlaceOrder.Validate`, `PlaceOrder.Post`, and `ValidateCoupon.Get`. The build log records this asymmetry (before twin injects `IDateTimeProvider`) as the exhibit, and the seed windows make time control unnecessary. Keep. For the record, the handler-fundamentals skill lists a `DateTimeOffset now` method parameter as an injectable in message handlers; whether that binds as an injection or as a query-string value on an HTTP endpoint should be checked with `codegen-preview` before anyone reaches for it.

### B7. `PlaceOrder.LoadAsync` makes two round trips

`LoadManyAsync<Cake>` then `LoadAsync<Coupon>`. The A-Frame skill says not to make N awaits when one batch would do (Marten `CreateBatchQuery`, or a tuple of query specs). Two round trips on a demo is nothing, and the sequential version is easier to read on a slide. Keep, but know it is the thing a maintainer would nudge.

### B8. Seed data: explicit `SeedData.ApplyAsync` vs Marten `IInitialData`

The skills' idiom for baseline data is `IInitialData` registered with `.InitializeWith<T>()`. The repo calls `SeedData.ApplyAsync` explicitly in `Program.cs` (Development only) and in the test fixture (after `CleanAllMartenDataAsync`). The fixture would still need the explicit call after wiping, so `IInitialData` saves one line in `Program.cs` and costs a type rename. Marginal; skip unless someone asks.

### B9. CritterWatch: the skill's console sample disagrees with what the repo verified

The skill's single-node sample is `opts.ListenToRabbitQueue("critterwatch").Sequential()` with `.DisableDeadLetterQueueing()`. The build log records that Wolverine 6.30's listener-mode validation refused Inline/Sequential and that the official 1.0 quickstart's `.ProcessInParallelWithNativeAcks().UseCritterWatchSerializer()` is what boots. The repo's shape was verified end to end; the skill sample looks stale for 6.30. Worth a note (or a PR) to the skills repo.

Two things the skill flags that are fine here but worth a pre-talk re-check:

- **Version coupling.** The skill's number one CritterWatch crash is a `TypeLoadException` when the console process mixes Wolverine versions. The console pins WolverineFx.RabbitMQ and RuntimeCompilation at 6.30.0 alongside CritterWatch 1.0.1 (compiled against 6.29.1 per the skill's own check). It ran live, so NuGet unified cleanly, but run `dotnet list package --include-transitive` on the console project once before dry-run 1 and confirm every `WolverineFx.*` resolves to the same version.
- **DLQ arguments on the shared `critterwatch` queue.** Both sides leave DLQ at the Wolverine default. That matches the skill's "simplest safe choice". The build log already records the rerun gotcha (`rabbitmqctl delete_queue critterwatch`).

Config-flag gating (`CritterWatch:Enabled`, default off) is exactly the skill's recommended rollout shape and correctly avoids branching on `IHostEnvironment`. The metrics-storage default (`Hybrid`) is fine at demo volume.

### B10. `WolverineFx.RuntimeCompilation` referenced unconditionally

The skills want a Debug-only reference so Release ships without Roslyn. The csproj comment and build log record the decision to skip that (repo only runs in Development; the regen-and-commit ritual would muddy the file-count slide). Documented divergence, keep. The csproj comment already pre-answers the "what about prod?" question.

### B11. The polling loop in `OrderScenarios`

The integration-testing skill's rule is "never sleep to wait", and its escape hatch is "genuinely no signal exists: poll a condition with a deadline, not a fixed sleep, and leave a comment saying why". `WaitForBakerTasksAsync` is a deadline poll with the reason commented (the before twin has no Wolverine, so `ExecuteAndWaitAsync` cannot be the shared tool). Compliant. If B5 happens, an after-only test could use `Host.ExecuteAndWaitAsync` around the POST and assert `tracked.Executed.SingleMessage<NotifyBaker>()`, which is the idiomatic Wolverine form and a nice contrast slide.

---

## Tier C: confirmed compliant (no action)

- `AddWolverineHttp()` before `MapWolverineEndpoints()`; `IntegrateWithWolverine().UseLightweightSessions()`; `AutoApplyTransactions()`; `RunJasperFxCommands(args)`. Matches the nine-step skeleton.
- No `SaveChangesAsync` in any handler or endpoint. No injected `IMessageBus`. Cascades via tuple return.
- `IQuerySession` for every pure read (`BrowseCakes`, `GetBakerTasks`, `ValidateCoupon`, both `ValidateAsync`/`LoadAsync` methods). Reads stay non-transactional.
- `[Entity(Required = true, OnMissing = OnMissing.ProblemDetailsWith404)]` on `GetCake` and `GetOrder`; `EmptyContentWith204` correctly avoided per CLAUDE.md.
- `ValidateCoupon` deliberately not `[Entity]`, with the reason in a comment. Correct: the route value needs normalizing and a miss is a 200.
- A-Frame in `PlaceOrder`: `LoadAsync` returns a record, `Validate(PlaceOrder command, PlaceOrderData data)` keeps the command in its signature (the skill's body-type-discovery gotcha), `Post` is synchronous, `Decide` is a public pure function.
- Railway: every sad path is `ProblemDetails` from `Validate`/`ValidateAsync` returning `WolverineContinue.NoProblems`; no exceptions for expected failures; no `IResult`.
- Static endpoint classes, static methods, method-parameter DI, positional-record commands, mutable-class documents, file-scoped namespaces, Allman braces, `[Identity]` from the `JasperFx` namespace, comments that explain mechanics rather than restate code.
- `Coupon.Code` as document identity with an uppercase-then-`LoadAsync` lookup; `BakerTask.Id = OrderId` for idempotent upsert (and it becomes meaningful once A1 lands).
- Marten: explicit camelCase STJ, `DatabaseSchemaName = "after"`, computed unique index on `Cake.Name` backing the 409 guard, verified by the new `database_refuses_duplicate_cake_name_behind_the_api` scenario.
- Tests: Alba scenarios with exact status codes, xUnit v2 `IAsyncLifetime` returning `Task`, Shouldly, `CleanAllMartenDataAsync()`, one Testcontainers container per twin collection, snake_case test names, hosts shared via fixtures (never per test).
- `Discovery.IncludeAssembly(typeof(AfterTwin).Assembly)` is redundant for the application assembly but harmless and appears in the style skill's skeleton.

---

## Suggested pre-dry-run commands

Both come straight from the skills and need no database:

```
dotnet run --project src/after/LayerCake.Slices -- codegen test
dotnet run --project src/after/LayerCake.Slices -- wolverine-diagnostics codegen-preview --route "POST /orders"
```

The first compiles every generated chain in seconds and is the fastest whole-application check that every attribute resolves. The second prints the actual generated code for the A-Frame endpoint, which is also a strong candidate for a slide: it shows the load, the guard, the transaction, and the cascade with nothing hidden.
