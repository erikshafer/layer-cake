# Handlers, HTTP Endpoints, and Marten Idioms

Rule strength: MUST / SHOULD / OBSERVED, as defined in `naming-and-types.md`. Source flavor tags [APP] / [DOC] / [TXPT] as defined there.

## 4. Handler style

**MUST: Wolverine.HTTP endpoint holder classes are `static`, with `static` methods.** Every endpoint class in every [APP] sample is `public static class XEndpoint`, including `PaymentsMonolith/Wallets/AddFundsEndpoint` (confirmed during the verification pass). Citations: `DepositFundsEndpoint`, `OpenAccountEndpoint`, `EnrollClientEndpoint`, `UpdateClientEndpoint`, `CreateContributorEndpoint`, `UpdateContributorEndpoint`, `DeleteContributorEndpoint`, `GetContributorsEndpoint`, `StudentEndpoints`, `CheckoutBasketEndpoint`, `StoreBasketEndpoint`, `CreateOrderEndpoint`, `SubmitRegistrationEndpoint`, `CreateTodoItemEndpoint`, `RequestMentorshipEndpoint`, `AddFundsEndpoint`. This is SHOULD rather than MUST only because of one counter-example: `TeleHealth.WebApi/ProviderShiftEndpoint.cs` and `BoardViewEndpoint.cs` [DOC] use non-static instance classes with instance methods. Default to static; treat instance endpoint classes as a legacy/alternate shape seen in one older sample family.

**MUST: sad-path validation is a separate `Validate` or `ValidateAsync` static method on the same endpoint class, returning `ProblemDetails` on failure or `WolverineContinue.NoProblems` (or `WolverineContinue.Result()`) to continue.** Wolverine calls this automatically as "before" middleware; the sample code itself calls this "Railway Programming" (`IncidentService/CategoriseIncident.cs`). Citations: `WithdrawFundsEndpoint.Validate`, `CheckoutBasketEndpoint.ValidateAsync`, `CreateContributorEndpoint.ValidateAsync`, `SubmitRegistrationEndpoint.ValidateAsync`, `CategoriseIncidentEndpoint.Validate`, `RequestMentorshipEndpoint.Validate`, `StartProviderShiftEndpoint.Validate`.
```csharp
public static ProblemDetails Validate(WithdrawFunds command, Account account)
{
    if (account.Balance < command.Amount)
        return new ProblemDetails { Detail = "Insufficient funds", Status = 400 };
    return WolverineContinue.NoProblems;
}
```

**MUST: side effects and follow-on messages are returned, not sent imperatively.** A handler or endpoint method returns the event(s)/response(s) as a single value, a tuple, or (rarely) an `IEnumerable<object>`, and Wolverine appends/publishes them after the transaction commits. This is the dominant shape across every project in both repos; representative citations: `DepositFundsEndpoint.Post` returns `(IResult, FundsDeposited)`; `SubmitRegistrationEndpoint.Post` returns `(IResult, Registration, RegistrationSubmitted)`; `CheckoutBasketEndpoint.Post` returns `(bool, BasketCheckoutEvent)`; `AddFundsEndpoint.Post` returns `(Wallet, FundsAdded)`; `Quickstart/CreateIssueHandler.Handle` returns `IssueCreated` directly; `CloseIncidentEndpoint.Handle` returns `(UpdatedAggregate, Events, OutgoingMessages)`.

**MUST: do not call `session.SaveChangesAsync()` inside a handler or endpoint method.** Rely on Wolverine's transactional middleware (`opts.Policies.AutoApplyTransactions()`, or `[Transactional]`). This is stated directly in the samples themselves ("you should almost never need to directly call `IDocumentSession.SaveChangesAsync()` in your handlers or endpoint methods", `CqrsMinimalApi/StudentEndpoints.cs`) and confirmed behaviorally: `session.Store(...)` / `session.Delete(...)` are called without a following `SaveChangesAsync()` in every [APP] endpoint (dozens of citations, including `AddFundsEndpoint` in the verification pass).

**SHOULD: prefer method-parameter injection of services (`IDocumentSession`, `IQuerySession`, `ILogger<T>`) over constructor injection.** Static classes can only do this anyway, and it is what nearly every handler and endpoint in the samples does. Wolverine's own docs sample explicitly frames this as the preferred style versus constructor injection (`DocumentationSamples/HandlerExamples.cs`, region `sample_handlerusingmethodinjection` versus `sample_handlerbuiltbyconstructorinjection`).

**SHOULD: `[AggregateHandler]` on a message handler, paired with the current aggregate injected as a typed parameter and a tuple return of `(response, event)`, is the idiom for mutating an event-sourced aggregate from a message handler.**
```csharp
[WolverinePost("/api/accounts/{accountId}/deposits")]
[AggregateHandler]
public static (IResult, FundsDeposited) Post(DepositFunds command, Account account)
{
    var newBalance = account.Balance + command.Amount;
    return (Results.NoContent(), new FundsDeposited(command.AccountId, command.Amount, newBalance));
}
```
Citations: `BankAccountES` (three endpoints), `TeleHealth.WebApi/ProviderShiftEndpoint.CompleteCharting` and `CompleteChartingHandler` (`[AggregateHandler(ConcurrencyStyle.Exclusive)]`).

Distinct from the above: `IncidentService` uses `[Aggregate("propertyName")]` (from `Wolverine.Http.Marten`) as a *parameter* attribute to load an event-sourced aggregate specifically for an HTTP endpoint, rather than `[AggregateHandler]` on the method. Both mechanisms appear in the samples; they are not interchangeable syntax for the same thing, they are two different features (message-handler aggregate loading vs. HTTP-endpoint aggregate loading). When writing a Wolverine.HTTP endpoint against an event-sourced aggregate, prefer `[Aggregate]` on the parameter; when writing a plain message handler, prefer `[AggregateHandler]` on the method.

**SHOULD: `OutgoingMessages` as a tuple element is the idiom for "append or start this event stream and separately send these outgoing bus messages" from a single handler or endpoint.** Originally logged as a single, unconfirmed citation; the verification pass turned up a second, independent project, so this is now a confirmed SHOULD rather than an OBSERVED single citation. Confirmed in `IncidentService/CloseIncident.cs` (`(UpdatedAggregate, Events, OutgoingMessages)`) and `ProcessManagerViaHandlers/OrderFulfillment/Handlers/StartOrderFulfillmentHandler.cs`, which builds the instance explicitly to schedule a delayed message alongside starting a new stream:
```csharp
public static (IStartStream, OutgoingMessages) Handle(StartOrderFulfillment command)
{
    var started = new OrderFulfillmentStarted(command.OrderFulfillmentStateId, command.CustomerId, command.TotalAmount);

    var outgoing = new OutgoingMessages();
    outgoing.Delay(new PaymentTimeout(command.OrderFulfillmentStateId), command.PaymentTimeoutWindow ?? DefaultPaymentTimeoutWindow);

    return (MartenOps.StartStream<OrderFulfillmentState>(command.OrderFulfillmentStateId, started), outgoing);
}
```

## 5. HTTP endpoint style (Wolverine.HTTP)

**MUST: `[WolverineGet]`, `[WolverinePost]`, `[WolverinePut]`, `[WolverineDelete]` attributes with a literal route string.** Universal, zero exceptions, in every [APP] sample and most [DOC] samples.

**MUST: return the strongly-typed result directly (`Student`, `Account`, `Task<IReadOnlyList<Contributor>>`, ...) instead of wrapping in `IResult`, unless the method needs a genuine conditional early return.** This is stated directly in the sample code itself:
> "Remove the usage of `IResult`, that's 'mystery meat' and Wolverine can better derive the OpenAPI metadata by a more expressive signature" (`CqrsMinimalApi/StudentEndpoints.cs`)

and confirmed behaviorally in `Contributor`, `Account`, `Client`, `Mentorship`, `Order`, `TodoItem`, and the `Get*` query endpoints throughout. `IResult` is reserved for cases with a genuine either/or outcome (`Results.NoContent()` inside a cascading tuple; `Results.BadRequest()` from a `LoadAsync` short-circuit).

**MUST: use `[Entity]` for declarative entity loading with automatic 404 (or configurable) handling instead of manually querying and null-checking.**
```csharp
[WolverineDelete("/api/contributors/{id}")]
public static void Delete(int id, [Entity(Required = true)] Contributor contributor, IDocumentSession session)
    => session.Delete(contributor);
```
Citations: `ContributorApi` (three endpoints), `CqrsMinimalApi/StudentEndpoints` (three endpoints), `CleanArchitectureTodos/CreateTodoItemEndpoint`, `MoreSpeakers/RequestMentorshipEndpoint` (two `[Entity]` parameters batch-loaded in one round trip), `BankAccountES/OpenAccountEndpoint`, `PaymentsMonolith/AddFundsEndpoint` (verification pass). This is also called out directly as a "common pattern" in the CritterStackSamples README.

**SHOULD: routes are REST-ish, noun-based collections with the HTTP verb carrying the semantics** (`POST /api/accounts`, `PUT /api/clients/{clientId}`, `DELETE /api/contributors/{id}`), not verb-in-path RPC style. The one counter-example, `CqrsMinimalApi` (`/student/create`, `/student/get-all`, `/student/update/{id}`), is explicitly a preserved port of an existing MediatR-controller demo's routes, not a fresh design; do not imitate it for new endpoints.

**SHOULD: `Results.NoContent()` as the first element of a cascading tuple is the preferred way to return 204 with side-effect events,** over the `[EmptyResponse]` attribute. The CritterStackSamples README states this directly as a common pattern. `[EmptyResponse]` does appear once in a Wolverine-repo sample (`IncidentService/CategoriseIncident.cs` in the Wolverine documentation samples, not CritterStackSamples), so it is a real, valid alternative mechanism, just not the preferred default.

**SHOULD: use a `CreationResponse`-derived record for 201-style "here's the new resource's URL" responses.** `TodoCreationResponse(Guid ListId) : CreationResponse(...)`, `ShiftStartingResponse(Guid ShiftId) : CreationResponse(...)`, `CreationResponse<Guid>` in `IncidentService/LogIncident.cs`, and `CreationResponse.For(new TodoCreated(todo.Id), $"/todoitems/{tenant}/{todo.Id}")` in `MultiTenantedTodoService` (verification pass). Four independent citations.

## 6. Marten idioms in application code

**MUST: aggregates rebuild state via overloaded `Apply(EventType e)` instance methods on the aggregate class itself.** Universal: `Account`, `Client` (BankAccountES); `Board`, `ProviderShift`, `Incident`, `Appointment` (Wolverine samples). Nearly every `Apply` method is a one-liner assigning fields from the event.

**SHOULD: a `Create` factory (static or, on a projection class, instance) constructs the aggregate/view from its first event, optionally async and injecting `IQuerySession` to enrich from related documents.**
```csharp
public static async Task<ProviderShift> Create(ProviderJoined joined, IQuerySession session)
{
    var provider = await session.LoadAsync<Provider>(joined.ProviderId);
    return new ProviderShift { Name = $"{provider!.FirstName} {provider.LastName}", ... };
}
```
Citations: `TeleHealth.Common/ProviderShift.Create`, `TeleHealth.Common/AppointmentProjection.Create`.

**MUST: `SingleStreamProjection<TView, TId>` is the base class for building a separate read-model document from an aggregate's event stream**, using `CreateEvent<T>` (or `Create`) plus `Apply` overloads. Citations: `BankAccountES/AccountTransactionsProjection`, `TeleHealth.Common/AppointmentProjection`.

**OBSERVED, single citation: `EventProjection` subclass with `Options.DeleteDataInTableOnTeardown` and `ops.QueueSqlCommand(...)` for custom-table projections** (`TeleHealth.Common/AppointmentDurationProjection`). Real and valid, but only seen once; do not treat as the default projection base class.

**MUST: starting a new event stream uses `session.Events.StartStream<T>(id, firstEvent)` (imperative) or `MartenOps.StartStream<T>(id, firstEvent)` returned as an `IStartStream` cascading value (functional/pure).**
Imperative, inline with other session work: `BankAccountES/EnrollClientEndpoint.Post`, `BankAccountES/OpenAccountEndpoint.Post`.
Functional, returned as a side effect from a pure endpoint method: `TodoWebService/TodoListEndpoint.CreateTodoList`, `IncidentService/LogIncidentEndpoint.Post`, `TeleHealth.WebApi/StartProviderShiftEndpoint.Create`, `ProcessManagerViaHandlers/StartOrderFulfillmentHandler.Handle` (verification pass).
Prefer the functional `MartenOps.StartStream` return-value form for new Wolverine.HTTP endpoints; it keeps the method a pure function of its inputs, which is also what the `IncidentService` sample calls out explicitly in its own unit test ("Pure function FTW!"). The imperative form remains valid, especially when the same method also needs to do other `IDocumentSession` work.

**MUST: read-side queries are written with `IQuerySession`, not `IDocumentSession`, as the injected parameter, and use `session.Query<T>().Where(...).ToListAsync(ct)` / `AnyAsync(...)` / `FirstOrDefaultAsync(...)` LINQ.** Citations: `GetContributorsEndpoint`, `StudentEndpoints.GetAll`, `StudentEndpoints.GetByName`, `GetClientAccountsEndpoint`, `CreateContributorEndpoint.ValidateAsync`, `SubmitRegistrationEndpoint.ValidateAsync`.

Known variation: event-stream reads sometimes inject `IDocumentSession` even for a pure read, to reach `session.Events.FetchLatest<T>(...)` (`IncidentService/GetIncidentEndpoint.Get`), while others inject `IQuerySession` for the equivalent `session.Events.AggregateStreamAsync<T>(...)` (`BankAccountES/GetAccountEndpoint.Get`). Both compile and both work because `IDocumentSession` derives from `IQuerySession`; prefer `IQuerySession` for anything that is purely a read.

**OBSERVED, single citation, explicitly framed as the older/manual way in its own sample: `session.Events.FetchForExclusiveWriting<T>(id)`** inside an MVC controller shown for contrast against the `[Aggregate]`/`[AggregateHandler]` attribute-driven approach (`TeleHealth.WebApi/CompleteChartingController`). Prefer `[Aggregate]` / `[AggregateHandler]` for new code; know this exists for cases that genuinely need manual stream control.

**SHOULD: register one Marten schema per module/bounded context via `opts.Schema.For<T>().DatabaseSchemaName("x")`, or one `opts.DatabaseSchemaName` per single-purpose service.** Ten-plus citations across `EcommerceModularMonolith` (four calls), `BankAccountES`, `ContributorApi`, `IncidentService`, `OutboxDemo`, `TodoWebService`, `CQRSWithMarten`.
