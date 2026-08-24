# Naming and Types

Covers file/folder shape, type choices, and message naming. Rule strength: MUST (universal in the projects surveyed), SHOULD (dominant, exceptions exist), OBSERVED (common but inconsistent, not a firm rule).

Source flavor key: [APP] = CritterStackSamples (application-style samples, treated as primary). [DOC] = Wolverine repo `DocumentationSamples` or other heavily-commented teaching samples. [TXPT] = Wolverine repo transport-demo samples (PingPong, AspireWithKafka, etc.), which follow looser conventions than real app code.

## 1. File and folder shape

**MUST: one command (or query) plus its validator plus its endpoint class live in a single file named after the command.**
The file is named `VerbNoun.cs` and contains, top to bottom: the command record, a nested `Validator` class if FluentValidation applies, then the static `VerbNounEndpoint` class.
- `BankAccountES/DepositFunds.cs`, `BankAccountES/OpenAccount.cs`, `BankAccountES/WithdrawFunds.cs` [APP]
- `ContributorApi/CreateContributor.cs`, `ContributorApi/UpdateContributor.cs` [APP]
- `CleanArchitectureTodos/CreateTodoItemRequest.cs` [APP]
- `MoreSpeakers/Mentorships/RequestMentorship.cs` [APP]

```csharp
public record DepositFunds(Guid AccountId, decimal Amount)
{
    public class Validator : AbstractValidator<DepositFunds>
    {
        public Validator() => RuleFor(x => x.Amount).GreaterThan(0);
    }
}

public static class DepositFundsEndpoint
{
    [WolverinePost("/api/accounts/{accountId}/deposits")]
    public static IResult Post(DepositFunds command) => Results.NoContent();
}
```

**MUST: `Program.cs` is a single top-level-statement file at the project root.** Universal across every project read (12+ projects, both repos).

**SHOULD: an aggregate/document class and the domain events that mutate it live together in one file named after the aggregate.**
- `BankAccountES/Account.cs` (holds `AccountOpened`, `FundsDeposited`, `FundsWithdrawn`, and `Account`)
- `BankAccountES/Client.cs` (holds `ClientEnrolled`, `ClientUpdated`, and `Client`)
- `wolverine/.../TeleHealth.Common/Boards.cs` (holds `BoardOpened`, `BoardFinished`, `BoardClosed`, and `Board`)
- `wolverine/.../TeleHealth.Common/Providers.cs`

**SHOULD: vertical-slice folders are named after the bounded context or module, not after technical layers.** No `Controllers/`, `Services/`, `Repositories/` folders appear anywhere in the samples.
- `EcommerceModularMonolith/{Basket,Catalog,Ordering,Discount}/`
- `MeetingGroupMonolith/{Administration,Meetings,Payments,Registrations,UserAccess}/`
- `PaymentsMonolith/{Customers,Payments,Users,Wallets}/`
- `MoreSpeakers/{Expertise,Mentorships,Speakers}/`

**SHOULD: tests live in a sibling `Tests/` folder (or `<Project>.Tests` project) next to the app project it exercises**, excluded from the main project's compile via `<Compile Remove="Tests/**" />`.
- Confirmed in every CritterStackSamples project with tests, and in `IncidentService/IncidentService.Tests`.

**OBSERVED, known variation: grouping unrelated message contracts into a single `Messages.cs` file, separate from any handler.** This shows up in transport-demo [TXPT] samples (`PingPong/Messages/Messages.cs`, `AspireWithKafka/Consumer/Messages.cs`) where the messages are simple plumbing payloads, not domain commands/events. It does not appear in [APP] samples, where each command gets its own file. Do not use `Messages.cs` grouping for domain commands or events; it is acceptable only for a small, genuinely shared contracts library consumed by multiple processes.

## 2. Type choices

**MUST: commands and events are positional `record` types.** This is the single most consistent convention across both repositories. Two-plus citations per message shown throughout this document; representative sample:
```csharp
public record DepositFunds(Guid AccountId, decimal Amount);
public record FundsDeposited(Guid AccountId, decimal Amount, decimal NewBalance);
```
Cited in `BankAccountES`, `ContributorApi`, `Quickstart` (`CreateIssue`, `AssignIssue`, `IssueCreated`, `IssueAssigned`), `IncidentService` (`IncidentLogged`, `IncidentCategorised`, ...), `OutboxDemo`, `EcommerceModularMonolith`.

Known variation: `PingPong/Messages/Messages.cs` [TXPT] uses plain mutable classes (`public class Ping { public int Number { get; set; } }`) instead of records. This is isolated to bare transport-demo payloads and should not be treated as precedent for domain messages.

**MUST: aggregates and Marten documents are plain mutable classes with `{ get; set; }` auto-properties, never records.**
```csharp
public class Account
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public decimal Balance { get; set; }
}
```
Cited in `Account`, `Client` (BankAccountES), `Order` (EcommerceModularMonolith), `Incident`, `Appointment`, `ProviderShift`, `Board` (Wolverine samples), `Contributor`, `Student`, `TodoList`.

**MUST: `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>` are set at the project level**, and code is written accordingly: string properties default to `= string.Empty;` or are explicitly nullable with `?`.
- `BankAccountES.csproj`, `ContributorApi.csproj` both set these explicitly.
- Body evidence: `public string Name { get; set; } = string.Empty;` appears in `Client`, `Contributor`, `TodoList`, `Order`, `ShoppingCart`, and dozens of others.

**OBSERVED: an older `= null!;` idiom for properties that are always populated post-construction** appears alongside `= string.Empty` in Wolverine-repo samples (`Quickstart/Issue.cs`: `public string Title { get; set; } = null!;`; `TeleHealth.Common/Board.cs`: `public string Name { get; } = null!;`). Prefer `= string.Empty` for strings that logically default to empty; reserve `= null!` for properties truly guaranteed non-null by construction logic that the compiler cannot see.

**Not codified (insufficient repeated evidence, do not treat as rules):**
- `sealed` on classes: not observed once in any sample read.
- `struct` for value-ish types: not observed.
- Primary constructors on handler/service classes: one occurrence only (`DocumentationSamples/HandlerExamples.cs`, `CreateProjectHandler(IProjectRepository Repository)`), and that file is explicitly a teaching sample cataloguing valid variations rather than recommending one. Do not default to primary constructors for handler classes.
- Target-typed `new()`: not a consistent pattern; object initializers are written with the explicit type name (`new Order { ... }`) even when assigned to a `var`.

## 3. Message naming

**MUST: commands are verb-first, imperative, PascalCase.** `CreateContributor`, `DepositFunds`, `WithdrawFunds`, `EnrollClient`, `UpdateClient`, `OpenAccount`, `SubmitRegistration`, `StartOrder`, `CompleteOrder`, `LogIncident`, `CategoriseIncident`, `CloseIncident`, `ArchiveIncident`, `RequestMentorship`, `PlaceOrder`, `CreateOrder`, `CheckoutBasket`, `StoreBasket`. This pattern holds in every project surveyed without exception.

**MUST: events are past tense.** `AccountOpened`, `FundsDeposited`, `FundsWithdrawn`, `ClientEnrolled`, `IssueCreated`, `IssueAssigned`, `RegistrationSubmitted`, `RegistrationValidated`, `IncidentLogged`, `IncidentCategorised`, `IncidentClosed`, `OrderCreated`, `OrderApproved`, `AppointmentRequested`, `ProviderJoined`, `BoardOpened`, `ChartingFinished`.

Known variation: `EcommerceModularMonolith`'s `BasketCheckoutEvent` uses an explicit `Event` suffix instead of pure past tense. Treat this as an outlier, not a pattern to copy; plain past tense is the dominant, preferred form.

**SHOULD: endpoint holder classes are named `<VerbNoun>Endpoint` (singular), static, one per command or query.** `DepositFundsEndpoint`, `OpenAccountEndpoint`, `EnrollClientEndpoint`, `CreateContributorEndpoint`, `GetContributorsEndpoint`, `CheckoutBasketEndpoint`, `SubmitRegistrationEndpoint`, `LogIncidentEndpoint`, `CategoriseIncidentEndpoint`, `RequestMentorshipEndpoint`. This is by far the dominant shape (15+ citations).

Known variation: `CqrsMinimalApi/StudentEndpoints.cs` groups every Student operation (`Create`, `GetAll`, `GetById`, `GetByName`, `Update`, `Delete`) as static methods on one plural `StudentEndpoints` class. This is an explicit, intentional artifact of that sample being a direct MediatR-controller port (its own comments walk through "converted" vs "desired" shapes); it is not the pattern to imitate for new code. Default to one file/one endpoint class per command.

**MUST: message handler classes for non-HTTP handling are named `<MessageName>Handler`.** `BasketCheckoutEventHandler`, `ArchiveIncidentHandler`, `SendRegistrationEmailHandler`, `AddEventAttendeeHandler`, `RegistrationValidatedHandler`, `TodoCreatedHandler`, `OrderCreatedHandler`, `CreateIssueHandler`, `IssueCreatedHandler`.

**MUST: projection classes are named `<View>Projection`.** `AccountTransactionsProjection`, `AppointmentProjection`, `AppointmentDurationProjection`, `BoardViewProjection`, `DayProjection`, `DistanceProjection`, `TripProjection`. Seven independent citations across both repos, zero exceptions.

**SHOULD: query/read endpoints are named `Get<Thing>Endpoint`, or a plain `Get`/`GetById`/`GetAll` static method on such a class.** `GetContributorsEndpoint`, `GetContributorByIdEndpoint`, `GetIncidentEndpoint`, `GetTransactionsEndpoint`, `GetAccountEndpoint`, `GetClientEndpoint`, `GetClientAccountsEndpoint`, `BoardViewEndpoint`.

**OBSERVED: saga classes are named as plain domain nouns (`Order`, `Registration`), not suffixed `Saga`,** with the `Saga`-ness expressed only via `: Saga` inheritance and the containing sample's project name. Two citations: `OutboxDemo/Registration.cs` and `OrderSagaSample/OrderSaga.cs` (which, confusingly, defines `class Order : Saga` inside a file called `OrderSaga.cs`, i.e. the file is suffixed but the type is not). Name the saga type after the domain concept it tracks; suffix the file name with `Saga` if that helps discoverability.
