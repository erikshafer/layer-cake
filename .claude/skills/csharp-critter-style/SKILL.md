---
name: csharp-critter-style
description: Use this skill whenever writing or reviewing C# code in a Wolverine or Marten based project, or any time the user asks for code to match "the Critter Stack way" or house style. Triggers on Wolverine message handlers, Wolverine.HTTP endpoints, Marten aggregates or projections, event-sourced command handlers, sagas, Program.cs bootstrapping for a Wolverine/Marten service, or general C# style/formatting questions in such a project. This is a style and idiom guide (naming, file shape, handler shape, comment tone), not an architecture or testing-strategy guide.
---

# Critter Stack C# Style

> **LayerCake scope note:** this skill governs the AFTER twin (`src/after/LayerCake.Slices`) and the shared contract suite. The BEFORE twin (`src/before/`) deliberately follows conventional Clean Architecture idioms instead (constructor injection, repository interfaces, MediatR handlers, DTO mappers); do not apply these rules there. That asymmetry is the exhibit. See `CLAUDE.md` and `docs/slices/`.

Write C# so that a JasperFx maintainer would recognize it as "one of ours" at a glance. This skill is about surface style: naming, file shape, handler shape, formatting, comment tone. It is not about architecture, saga design theory, transport selection, or testing strategy, those are out of scope.

Every rule below is backed by citations from real Wolverine and Marten sample code. Full detail, code snippets, exact file citations, and rule-strength reasoning live in `references/`. This file is the distilled checklist; consult a reference file before making a judgment call it doesn't obviously cover.

Rule tags: **MUST** (universal or near-universal in the samples), **SHOULD** (dominant, known exceptions exist), **OBSERVED** (real but inconsistent, use your judgment).

## File and type shape

1. **MUST** Put one command (or query), its `Validator`, and its static endpoint class in a single file named after the command: `DepositFunds.cs` holds `DepositFunds`, `DepositFunds.Validator`, and `DepositFundsEndpoint`. See `references/naming-and-types.md` #1.
2. **MUST** Write commands and events as positional `record` types. Write aggregates and Marten documents as plain mutable classes with `{ get; set; }` auto-properties, never records. See `references/naming-and-types.md` #2.
3. **SHOULD** Put an aggregate/document class and the events that mutate it in one file named after the aggregate (`Account.cs` holds `AccountOpened`, `FundsDeposited`, and `Account`). See `references/naming-and-types.md` #1.
4. **SHOULD** Name vertical-slice folders after the bounded context or module (`Basket/`, `Ordering/`, `Catalog/`), never after technical layers (`Controllers/`, `Services/`, `Repositories/`). See `references/naming-and-types.md` #1.
5. Do not use `sealed`, `struct`, or primary constructors on handler classes. Not observed with any consistency in the samples; do not invent this convention. See `references/naming-and-types.md` #2.

## Naming

6. **MUST** Name commands verb-first and imperative: `CreateContributor`, `DepositFunds`, `CloseIncident`, `RequestMentorship`.
7. **MUST** Name events past-tense: `AccountOpened`, `FundsDeposited`, `IncidentClosed`, `IssueAssigned`.
8. **SHOULD** Name the endpoint holder class `<VerbNoun>Endpoint`, singular, one per command. Don't group unrelated operations into one plural `XEndpoints` class for new code; that shape exists in a couple of samples as a legacy port artifact.
9. **MUST** Name non-HTTP message handler classes `<MessageName>Handler`: `ArchiveIncidentHandler`, `SendRegistrationEmailHandler`.
10. **MUST** Name projection classes `<View>Projection`: `AccountTransactionsProjection`, `AppointmentProjection`.

See `references/naming-and-types.md` #3 for full citations and the outlier cases (`BasketCheckoutEvent`'s `Event` suffix, `StudentEndpoints`' plural grouping).

## Handlers and endpoints

11. **MUST** Make Wolverine.HTTP endpoint holder classes `static` with `static` methods.
12. **MUST** Put sad-path validation in a separate `Validate`/`ValidateAsync` static method returning `ProblemDetails` or `WolverineContinue.NoProblems`:
    ```csharp
    public static ProblemDetails Validate(WithdrawFunds command, Account account)
        => account.Balance < command.Amount
            ? new ProblemDetails { Detail = "Insufficient funds", Status = 400 }
            : WolverineContinue.NoProblems;
    ```
13. **MUST** Return side effects and follow-on messages as the method's return value (single value, tuple, or `IEnumerable<object>`), never send them imperatively via an injected bus inside the method body.
14. **MUST** Never call `session.SaveChangesAsync()` inside a handler or endpoint method. Let Wolverine's transactional middleware (`AutoApplyTransactions()`, `[Transactional]`) commit for you.
15. **SHOULD** Inject services (`IDocumentSession`, `IQuerySession`, `ILogger<T>`) as method parameters, not constructor parameters.
16. **SHOULD** Use `[AggregateHandler]` on a message handler (with the aggregate as a typed parameter and a `(response, event)` tuple return) to mutate an event-sourced aggregate from a plain handler. Use `[Aggregate("propName")]` as a parameter attribute for the equivalent on a Wolverine.HTTP endpoint. These are two different mechanisms; don't conflate them.
17. **SHOULD** Use `OutgoingMessages` as a tuple element when a handler both starts/appends an event stream and needs to separately send outgoing bus messages (e.g. scheduling a timeout).

Full detail, including the `[Entity]` batching pattern, `ProblemDetails` shapes, and `CreationResponse`: `references/handlers-and-endpoints.md` #4 to #5.

## Wolverine.HTTP specifics

18. **MUST** Use `[WolverineGet]` / `[WolverinePost]` / `[WolverinePut]` / `[WolverineDelete]` with a literal route string.
19. **MUST** Return the strongly-typed result directly (`Student`, `Task<IReadOnlyList<Contributor>>`), not wrapped in `IResult`, unless the method genuinely needs a conditional early return. Quote from the samples themselves: "Remove the usage of `IResult`, that's 'mystery meat'."
20. **MUST** Use `[Entity]` for declarative entity loading and automatic 404 handling instead of manual query-and-null-check:
    ```csharp
    [WolverineDelete("/api/contributors/{id}")]
    public static void Delete(int id, [Entity(Required = true)] Contributor contributor, IDocumentSession session)
        => session.Delete(contributor);
    ```
21. **SHOULD** Write REST-ish, noun-based routes (`POST /api/accounts`), not verb-in-path RPC routes (`POST /student/create`). The one RPC-style sample is an explicit legacy port, not a pattern to imitate.
22. **SHOULD** Return `Results.NoContent()` as the first element of a cascading tuple for 204 responses, in preference to `[EmptyResponse]`.

## Marten idioms

23. **MUST** Rebuild aggregate state via overloaded `Apply(EventType e)` instance methods on the aggregate class itself.
24. **SHOULD** Use a `Create` factory (static, optionally async, optionally injecting `IQuerySession`) to construct the aggregate/view from its first event.
25. **MUST** Base read-model projections on `SingleStreamProjection<TView, TId>` with `CreateEvent<T>`/`Create` plus `Apply` overloads.
26. **MUST** Start a new event stream with `session.Events.StartStream<T>(id, firstEvent)` (imperative) or `MartenOps.StartStream<T>(id, firstEvent)` returned as an `IStartStream` cascading value (functional). Prefer the functional form for new Wolverine.HTTP endpoints, it keeps the method a pure function.
27. **MUST** Inject `IQuerySession` (not `IDocumentSession`) for pure reads, and query with `session.Query<T>().Where(...).ToListAsync(ct)`.
28. **SHOULD** Register one Marten schema per module or service via `opts.Schema.For<T>().DatabaseSchemaName("x")`.

Full detail: `references/handlers-and-endpoints.md` #6.

## Program.cs / bootstrapping

29. **MUST** Follow the standard nine-step skeleton (builder, Swagger/OpenAPI services, `AddWolverineHttp()`, `AddMarten` with a local-default connection string fallback, `IntegrateWithWolverine().UseLightweightSessions()`, `UseWolverine` with `Discovery.IncludeAssembly` + `AutoApplyTransactions()`, `builder.Build()`, dev-only Swagger UI, `MapWolverineEndpoints()`, then `RunAsync()` or `RunJasperFxCommands(args)`). Full skeleton with real code: `references/bootstrapping-and-formatting.md` #8.
30. **SHOULD** Prefer `return await app.RunJasperFxCommands(args);` over plain `await app.RunAsync();` for any service expected to grow CLI tooling (codegen, migrations); it's a strict superset.

## C# language level and formatting

31. **MUST** File-scoped namespaces, top-level-statement `Program.cs`, `Nullable` and `ImplicitUsings` both enabled.
32. **MUST** Allman brace style (opening brace on its own line).
33. **SHOULD** `var` for locals when the type is apparent; collection expressions (`= [];`) for empty list defaults; expression-bodied members for one-line pass-through methods and simple properties/accessors (not for multi-statement handler bodies).
34. **Do not use `#region`/`#endregion` markers in application code.** They're pervasive in Wolverine's own doc-snippet samples because the docs site pulls named regions into published pages. They appear in zero CritterStackSamples application files. See `references/bootstrapping-and-formatting.md` #9.

## Comments

35. **MUST** Place inline `//` comments directly above the line they explain, and use them to explain Wolverine/Marten mechanics (cascading, outbox timing, `[Entity]` behavior), never to restate what the code already says.
36. **SHOULD** Give aggregate/document/saga classes a short (one to three sentence) `/// <summary>`. Keep the tone plain and conversational, not formal enterprise Javadoc, and skip line-by-line commentary of straightforward CRUD bodies.

Full guidance, including how much of the samples' comment density is a documentation-pipeline artifact versus a transferable production style: `references/comments-and-docs-style.md`.

## Testing (brief; not a testing strategy guide)

37. **MUST** xUnit `[Fact]` + Shouldly assertions + Alba (`IAlbaHost`, `.Scenario(...)`) for HTTP integration tests, with `IAsyncLifetime` bootstrapping `AlbaHost.For<Program>()` and calling `CleanAllMartenDataAsync()` for isolation.

Full guidance and the two observed test-naming casings: `references/testing-style.md`.

## When something isn't covered here

Check `references/evidence-log.md` for what was and wasn't confirmed, and why. Dimensions with thin evidence (custom-table `EventProjection`, `FetchForExclusiveWriting`, exact test-method casing) are flagged there rather than forced into a rule. When a request needs something genuinely not covered by any reference file, follow the target repo's `.editorconfig` and the nearest existing code in the same project over guessing.
