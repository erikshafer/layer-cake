# Design: slice-005-pay-with-tendr

## Context

The hand-off (Erik, 2026-09-14) settled the contract and most of the structure; `docs/slices/005-pay-with-tendr.md` records it. This file holds only the questions the hand-off left to the build, what was verified before any task list was written, and the calls made inside its fences. See proposal.md for why.

Verified against the pinned binaries and the generated code before writing tasks (Wolverine 6.30.0, JasperFx 2.55.0, Microsoft.AspNetCore.Mvc.Testing 10.0.0 via Alba 8.5.3):

- Wolverine discovers middleware by the names `Before`, `BeforeAsync`, `Load`, `LoadAsync`, `Validate`, `ValidateAsync` (and the `After`/`Finally` pairs) or by `[WolverineBefore]`. `AuthorizeAsync` is not a convention name.
- Wolverine.Http takes the request body's type from the endpoint method's own parameters only, not from its middleware.
- A typed `HttpClient` is an opaque factory registration, and Wolverine 6's `ServiceLocationPolicy.NotAllowed` refuses it at codegen.
- `WebApplicationFactory<T>.UseKestrel(...)` and `StartServer()` exist in the Mvc.Testing version already in the suite's graph.

## Goals / Non-Goals

**Goals:** the chain order on the after twin is provable from generated code; the suite crosses a real socket to Tendr without a new container; the existing scenario files and the experiments' bindings stay byte-identical.

**Non-Goals:** redesigning either twin's error handling beyond the two new status codes; making Tendr a Critter Stack tutorial.

## Decisions

1. **`[WolverineBefore]` on `AuthorizeAsync`.** Needed for discovery, not ordering. With it, the generated `POST_orders` runs `LoadAsync`, `Validate(command, data)`, `AuthorizeAsync`, `Validate(payment)`, `Post` in declaration order. Alternative considered: renaming the rung `LoadAsync` overload (discovered by name, but the slide wants the word "Authorize"), or folding the 402 into one rung returning `(ProblemDetails, Order, Payment?)` (loses guard four as its own `Validate`). The `Validate(Payment?)` overload is discovered by name with no attribute.
2. **`Post` keeps `PlaceOrder command` as its first parameter**, unused in the body, with a comment. Without it Wolverine.Http reads no body and codegen fails with `UnResolvableVariableException` for `PlaceOrder`. This is the one deviation from the hand-off's `Post(Order, Payment?, LayerCakeDbContext)` signature.
3. **`opts.CodeGeneration.AlwaysUseServiceLocationFor<TendrClient>()`** inside `UseWolverine`, one line with a comment. The typed client is a factory registration by design; allow-listing that one type is the documented escape hatch, where loosening the global policy is not.
4. **503 on the after twin comes from guard four, not from Wolverine.** An exception escaping a Wolverine.Http endpoint rolls the transaction back and rethrows to ASP.NET Core, which answers a bare 500. So `AuthorizeAsync` catches `HttpRequestException` and `TaskCanceledException` and returns a `Payment` with status `unavailable`, and `Validate(Payment?)` maps it to the 503 problem. The story stays in one file. To be confirmed once by running the outage scenario before the catch exists.
5. **The before twin's 503 comes from the adapter and the filter**, the textbook: `TendrPaymentGateway` translates the transport exceptions to `PaymentUnavailableException`, `ApiExceptionFilterAttribute` maps it.
6. **`GET /orders/{id}` on the after twin stays untouched.** It returns the `Order` entity, so `Order` carries the two stored columns as `[JsonIgnore]` and a computed, unmapped `Payment` property that is null when there was no card. `PlacedOrder.From(order)` reads the same property, so it keeps its one-argument signature.
7. **Before twin additions beyond the hand-off's file list, both on in-repo precedent:** `Domain/Enums/PaymentStatus.cs` (a status in the Domain is an enum, like `CouponStatus`; stored as an integer by EF convention so existing rows default to `AtPickup` = 0 without a column default) and `Application/Orders/PaymentDto.cs` (a nested DTO gets its own file, like `OrderLineDto`). `OrdersController` also gains `ProducesResponseType` for 402 and 503, as it already documents 400 and 422.
8. **`FrameworkReference Microsoft.AspNetCore.App` on the before twin's Infrastructure project.** `AddHttpClient` lives in `Microsoft.Extensions.Http`; a class library reaches the shared framework through a framework reference, which is not a package and has no version to pin. The existing comment claiming the configuration binder is out of reach becomes false and is corrected; options stay bound by hand for consistency with `RabbitMqOptions`.
9. **Tendr under the contract suite: `WebApplicationFactory<TendrApi>` on Kestrel**, bound to `127.0.0.1:0`, address read from `IServerAddressesFeature`. Tendr's `Program.cs` stays an ordinary top-level program with no test hook. xUnit 2 collection fixtures cannot take other collection fixtures, so `TendrHostFixture` is a collection fixture started lazily by the first host fixture with the collection's PostgreSQL connection string; classes in one collection run sequentially, so there is no race. Tendr's data is never reset: every order id is fresh.
10. **New scenarios live in `PaymentScenarios.cs`, not `OrderScenarios.cs`.** `tests/LayerCake.ContractTests.Marten` binds `OrderScenarios` to the Marten twin, which has no card path and no Tendr; adding scenarios there would redden an experiment and force it to grow a fixture. A separate class keeps `OrderScenarios.cs` byte-identical and leaves both experiments' bindings alone.
11. **Finding the would-be order id of a declined card**: Tendr stored the decline under the idempotency key, which is the order id. The Tendr fixture lists authorization ids before and after the request through Tendr's own document store; the single new id is the order that never was. The guard-order scenario asserts the set did not grow.
12. **Outage fixtures** subclass the twin host fixtures and override only the Tendr base URL (`http://127.0.0.1:1`, a closed port that refuses immediately). The no-card outage scenario waits for its baker task before it ends, so the 503 scenario's "no new baker task" window cannot see a straggler.

## Risks / Trade-offs

- [The EF Core transaction is open across the vendor call on the after twin] Wolverine's EF Core middleware begins the transaction before the first rung, so the connection and transaction are held for up to the two-second timeout. The before twin opens its transaction only inside `SaveChangesAsync`, after the call. Recorded for Erik, not changed: the lightweight transaction mode would be a talk-content decision.
- [`tests/Tendr.Tests` in the slnx starts its own PostgreSQL container] The hand-off asks both for the project in the slnx with Testcontainers and for `dotnet test` not to grow by a container. The project goes in the slnx as §6 names it; the wall-clock with and without it is measured and the one-line alternative (drop it from the slnx) goes to Erik.
- [`GetMethods()` order is not a documented CLR guarantee] Declaration order holds on CoreCLR and the ordering scenario would catch a change; the generated code is the evidence.
- [A 4xx from Tendr would read as 503 on both twins] Only reachable by a twin bug (both twins always send a positive amount and `usd`); a blank card number is sent through and Tendr declines it `unknown_card`, so a customer typo is a 402, not a 503.
