# Evidence Log

Audit trail for the `csharp-critter-style` skill. Records which files were read, from which of the two authorized source locations, and what was concluded per style dimension. Source flavor tags: [APP] = CritterStackSamples application-style sample, [DOC] = Wolverine repo `DocumentationSamples` or another heavily-commented teaching sample, [TXPT] = Wolverine repo transport-demo sample.

## Survey pass

Directory trees listed in full for both authorized locations:
- `C:\Code\JasperFx\CritterStackSamples` (16 sample projects plus root `README.md` and `sample-projects.md`)
- `C:\Code\JasperFx\wolverine\src\Samples` (roughly 35 sample projects)

Projects selected for the extraction pass, spanning small, medium, and large, per the requested method:

| Project | Repo | Size | Why chosen |
|---|---|---|---|
| BankAccountES | CritterStackSamples | Medium | Pure Marten event sourcing from scratch, `[AggregateHandler]`, inline snapshot projections |
| ContributorApi | CritterStackSamples | Small | Clean CRUD-over-documents baseline |
| CqrsMinimalApi | CritterStackSamples | Small/Medium | Explicit "converted vs desired" commentary on endpoint style; production codegen `Program.cs` |
| EcommerceModularMonolith | CritterStackSamples | Large | Vertical-slice modular monolith, schema-per-module, durable local queues |
| OutboxDemo | CritterStackSamples | Small/Medium | Saga + outbox + cascading messages, canonical |
| CleanArchitectureTodos | CritterStackSamples | Small | One-file-per-request layout, MediatR-unraveling port |
| MoreSpeakers | CritterStackSamples | Medium | Multiple `[Entity]` batch loading |
| PaymentsMonolith | CritterStackSamples | Medium | **Verification pass only, not in original survey** |
| Quickstart | wolverine/Samples | Small | Canonical intro sample, mixes static and instance handlers |
| TodoWebService | wolverine/Samples | Small | Wolverine.HTTP fundamentals, `IStartStream` cascading |
| WebApiWithMarten | wolverine/Samples | Small/Medium | Side-by-side comparison of controller / shorthand / longhand handler styles |
| CQRSWithMarten (TeleHealth) | wolverine/Samples | Medium/Large | Aggregate `Create`/`Apply`, `[Aggregate]` vs `[AggregateHandler]`, MVC-vs-Wolverine.HTTP comparison |
| IncidentService | wolverine/Samples | Medium | Event-sourced aggregate over HTTP, scheduled cascading messages, "Railway Programming" comment |
| OrderSagaSample | wolverine/Samples | Small | Saga fundamentals, timeout messages |
| DocumentationSamples | wolverine/Samples | N/A (teaching) | Required by task; heavily commented, catalogues handler/middleware variations explicitly |
| PingPong | wolverine/Samples | Small | Transport-demo baseline, contrasted against [APP] message-naming conventions |
| MultiTenantedTodoService | wolverine/Samples | Small | **Verification pass only, not in original survey** |
| ProcessManagerViaHandlers | wolverine/Samples | Medium | **Verification pass only, not in original survey** |

## Extraction pass, by dimension

**1. File and folder shape.** Read: `BankAccountES/*.cs`, `ContributorApi/*.cs`, `CqrsMinimalApi/*.cs`, `EcommerceModularMonolith/{Basket,Ordering}/*.cs` + `IntegrationEvents.cs`, `OutboxDemo/*.cs`, `CleanArchitectureTodos/{TodoList,CreateTodoItemRequest,Dtos}.cs`, `MoreSpeakers/Mentorships/*.cs`, `PingPong/Messages/Messages.cs`, `PingPong/{Pinger,Ponger}/*Handler.cs`. Concluded: one-file-per-command with nested `Validator` is the dominant [APP] shape; vertical-slice module folders are named after the bounded context; `Messages.cs` grouping is a [TXPT] pattern, not an [APP] one.

**2. Type choices.** Same file set as above, plus `BankAccountES.csproj`, `ContributorApi.csproj`, `Quickstart.csproj` (for `Nullable`/`ImplicitUsings`). Concluded: records for commands/events (MUST), mutable classes for aggregates/documents (MUST), `Nullable`/`ImplicitUsings` enabled (MUST, direct `.csproj` citation). `sealed`, `struct`, primary constructors on handlers, and target-typed `new()` were explicitly checked for and not found with sufficient repetition; left uncodified rather than invented.

**3. Message naming.** Same file set as dimensions 1 to 2, plus `IncidentService/Incident.cs`, `Quickstart/{CreateIssue,AssignIssue,IssueCreated,IssueAssigned}.cs`, `TeleHealth.Common/{Appointments,Boards,Providers}.cs`, `OrderSagaSample/OrderSaga.cs`. Concluded: verb-first commands and past-tense events are exceptionless across everything read; `BasketCheckoutEvent` is a single-citation outlier on the event-naming rule and is flagged as such, not folded into the rule.

**4. Handler style.** Read: `BankAccountES/{DepositFunds,WithdrawFunds,UpdateClient}.cs`, `ContributorApi/*.cs`, `EcommerceModularMonolith/Basket/CheckoutBasket.cs`, `OutboxDemo/{Registration,SubmitRegistration}.cs`, `CqrsMinimalApi/StudentEndpoints.cs`, `DocumentationSamples/HandlerExamples.cs`, `TeleHealth.WebApi/ProviderShiftEndpoint.cs`, `IncidentService/{CategoriseIncident,CloseIncident}.cs`. Verification pass added `PaymentsMonolith/Wallets/AddFunds.cs` and `ProcessManagerViaHandlers/OrderFulfillment/Handlers/StartOrderFulfillmentHandler.cs`. The `OutgoingMessages`-as-tuple-element rule was upgraded from a single, unconfirmed citation to a confirmed SHOULD as a direct result of the verification pass finding the `ProcessManagerViaHandlers` citation; see "Verification pass" below.

**5. HTTP endpoint style.** Same file set as dimension 4, plus `MultiTenantedTodoService/Endpoints.cs` (verification pass) and CritterStackSamples' root `README.md` ("Common Patterns Across Samples" section, which independently states several of these rules in the maintainers' own words: `[Entity]`, `ValidateAsync`/`Validate`, `Results.NoContent()` preferred over `[EmptyResponse]`). Treating that README section as corroborating, maintainer-authored evidence rather than a citation-worthy code sample in its own right.

**6. Marten idioms.** Read: `BankAccountES/{Account,Client,TransactionHistory,Program}.cs`, `TeleHealth.Common/{AppointmentProjection,ProviderShift,Appointments}.cs`, `TeleHealth.WebApi/ProviderShiftEndpoint.cs`, `TodoWebService/TodoListEndpoint.cs`, `IncidentService/{LogIncident,GetIncidentEndpoint}.cs`, `EcommerceModularMonolith/Program.cs`. Verification pass confirmed `MartenOps.StartStream` usage in a third independent project (`ProcessManagerViaHandlers`).

**7 to 9. C# language level, Program.cs shape, formatting.** Read every `Program.cs` in the extraction-pass project list (10 files) plus `BankAccountES.csproj`, `ContributorApi.csproj`, `Quickstart.csproj`, and the target repo's own `C:\Code\mmo-reconnect\.editorconfig` (used as a cross-check for formatting rules, not as one of the two authorized citation sources; every formatting rule stated as MUST or SHOULD in `bootstrapping-and-formatting.md` is independently backed by sample citations, and the `.editorconfig` is noted only where it corroborates, e.g. brace style, `var` usage, expression-bodied members).

**10. Comment tone.** Read all of the above plus `DocumentationSamples/{HandlerExamples,CascadingSamples,Middleware,BootstrappingSamples,ExceptionHandling}.cs`, `Quickstart/*.cs`, `OrderSagaSample/OrderSaga.cs`. Verification pass added the `<see cref>`-style formal comment in `ProcessManagerViaHandlers/StartOrderFulfillmentHandler.cs`, which is noted as a real but rarer variant rather than folded into the dominant-voice rule.

**11. Testing style.** Read: `BankAccountES/Tests/BankAccountTests.cs`, `ContributorApi/Tests/ContributorTests.cs`, `CqrsMinimalApi/Tests/StudentEndpointTests.cs`, `IncidentService.Tests/when_logging_an_incident.cs`. Cross-checked against CritterStackSamples' root `README.md`, which states the Alba/Shouldly/`CleanAllMartenDataAsync` pattern directly.

## Verification pass

Per the requested method, three files were read from projects entirely outside the original survey set, after the reference files and a first draft of `SKILL.md` were written, specifically to stress-test the stated rules against unseen code:

1. `C:\Code\JasperFx\CritterStackSamples\PaymentsMonolith\Wallets\AddFunds.cs` [APP, untouched project]
2. `C:\Code\JasperFx\wolverine\src\Samples\MultiTenantedTodoService\MultiTenantedTodoService\Endpoints.cs` [DOC, untouched project]
3. `C:\Code\JasperFx\wolverine\src\Samples\ProcessManagerViaHandlers\ProcessManagerViaHandlers\OrderFulfillment\Handlers\StartOrderFulfillmentHandler.cs` [APP-flavored but lives in the Wolverine repo, untouched project]

**Result: no MUST or SHOULD rule was contradicted.** Every rule checked against these three files held:
- `AddFunds.cs` confirmed, in a single file: one-file-per-command shape, record command with nested `Validator`, static `Endpoint` class, `[Entity]` declarative loading, verb-first command name (`AddFunds`) and past-tense event name (`FundsAdded`), cascading tuple return, no explicit `SaveChangesAsync()` call, file-scoped namespace, trailing comma in the object initializer.
- `MultiTenantedTodoService/Endpoints.cs` confirmed the plural, multi-method `Endpoints` static class shape already flagged as a known variation (it is, in fact, the same sample family as `TodoWebService`, extended for multi-tenancy), and added a fourth citation for the `CreationResponse` idiom via its `CreationResponse.For(...)` static-factory call style.
- `StartOrderFulfillmentHandler.cs` upgraded the `OutgoingMessages`-tuple rule from a single, unconfirmed citation to a two-project-confirmed SHOULD, and surfaced the more formal `<see cref>`-style doc comment as a real but narrower variant of the comment-tone guidance. Both reference files were updated to reflect this (`handlers-and-endpoints.md`, `comments-and-docs-style.md`). No downgrade or removal of any existing rule was necessary.

One incidental anomaly noted but not used as evidence for any rule: `MultiTenantedTodoService/Endpoints.cs` defines a class named `TodoCreatedHandler` whose `Handle` method actually takes a `DeleteTodo` command, not a `TodoCreated` event; this reads as a copy-paste artifact in the sample itself and was excluded from the message-naming and handler-naming citations.

## Dimensions where evidence was thinner than the others

- **Comment tone (dimension 10)** required the most editorial judgment to separate documentation-pipeline density (`#region`-driven) from a transferable production style. Confidence is high on placement and the mechanics-explaining MUST rule; confidence is medium on exactly how much of the casual voice should carry over, since that is inherently a judgment call once the jokes and first-person asides are stripped out.
- **`EventProjection` (vs. `SingleStreamProjection`) usage** and **`session.Events.FetchForExclusiveWriting`** each have exactly one citation in the entire surveyed set (see `handlers-and-endpoints.md`, dimension 6). Both are documented as OBSERVED rather than upgraded to SHOULD, and both are explicitly framed in their own source files as either a specialized case (`EventProjection`, custom SQL table) or an older/manual approach being contrasted against the newer attribute-driven one (`FetchForExclusiveWriting`, shown inside an MVC controller for comparison).
- **Primary constructors, `sealed`, and `struct`** were checked for deliberately and not found with enough repetition (or, in the primary-constructor case, found exactly once, in a file that is itself cataloguing valid variations rather than recommending one) to codify as rules. This is a deliberate absence, not an oversight; see the "Not codified" note in `naming-and-types.md` section 2.
