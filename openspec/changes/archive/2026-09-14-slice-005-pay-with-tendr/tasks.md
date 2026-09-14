# Tasks: slice-005-pay-with-tendr

Contract, structure, and scenarios: `docs/slices/005-pay-with-tendr.md`. Decisions: `design.md` (the `[WolverineBefore]` rung, `PlaceOrder` kept on `Post`, the service-location allow-list, the 503 via guard four, Tendr on Kestrel under test, `PaymentScenarios` as its own class).

## 1. Tendr, the vendor

- [x] 1.1 Create `src/tendr/Tendr` (Wolverine.Http + Marten, schema `tendr`, port 42040, Swagger in Development, `RunJasperFxCommands`, `AddResourceSetupOnStartup`) with the `TendrApi` marker and `GET /ping`; add it to `LayerCake.slnx` under `/tendr/`; verify `dotnet build` has 0 warnings and no pin in `Directory.Packages.props` changed
- [x] 1.2 Add `Authorizations/Authorization.cs` (the three events and the inline-snapshot aggregate), `Authorizations/AuthorizeCard.cs` (command, response, endpoint with `LoadAsync`, `Validate`, a pure `Decide(string cardNumber)`, 201 or 200 replay), and `Authorizations/GetAuthorization.cs`; verify with `codegen preview` that both chains generate
- [x] 1.3 Create `tests/Tendr.Tests` (in the slnx) with Alba scenarios on Testcontainers PostgreSQL: approve, the two declines, unknown card, replay 200 with the same body, missing and malformed `Idempotency-Key` 400, amount and currency 400, GET 200 and 404; plus pure facts for `Decide`; verify all green
- [x] 1.4 Write `src/tendr/Tendr/README.md` (API, test cards, the shared-database convenience) and `Properties/launchSettings.json`; verify `grep -c "[—–]"` prints 0 on the README

## 2. Before twin (earnest Clean Architecture)

- [x] 2.1 Add `Domain/Enums/PaymentStatus.cs`, the two `Order` properties, the `OrderConfiguration` mapping, and generate migration `AddOrderPayment`; verify the migration adds two columns and nothing else
- [x] 2.2 Add `IPaymentGateway`, `PaymentAuthorization`, `CardRequest`, `PaymentDeclinedException`, `PaymentUnavailableException`, `PaymentDto`; edit `PlaceOrderCommand`, `OrderDto`, `OrderMappingProfile`; verify the Application project builds with no new package reference
- [x] 2.3 Add `Infrastructure/Payments/TendrPaymentGateway.cs`, `TendrOptions.cs`, `TendrAuthorizationRequest.cs`, `TendrAuthorizationResponse.cs`; the `FrameworkReference`; the options binding and `AddHttpClient<IPaymentGateway, TendrPaymentGateway>` in `DependencyInjection.cs`; verify Infrastructure builds with 0 warnings
- [x] 2.4 Edit `PlaceOrderCommandHandler` (inject the gateway, authorize after decide, throw on decline, set payment fields before the existing save and publish), `ApiExceptionFilterAttribute` (402, 503), `OrdersController` (documented 402, 503), `appsettings.json` (`Tendr:BaseUrl`); verify the constructor lists eight dependencies and no card number reaches a log call or a column

## 3. After twin (Wolverine + EF Core slices)

- [x] 3.1 `Payments/Tendr.cs`, `Program.cs` (`AddHttpClient<TendrClient>`, the allow-list line), `appsettings.json`; verify `git diff` shows `NotifyBaker.cs` and the outbox line untouched
- [x] 3.2 `Orders/Order.cs` (two stored columns, computed `Payment`) and `Orders/PlaceOrder.cs` (`Card`, `Payment`, `[WolverineBefore] AuthorizeAsync`, `Validate(Payment?)`, `Post` storing what the rungs returned); verify with `codegen preview` that `POST_orders` runs `LoadAsync`, `Validate`, `AuthorizeAsync`, `Validate`, `Post`, and that `LoadAsync`, the three-guard `Validate`, and `Decide` have no diff
- [x] 3.3 Add `Validate(Payment?)` facts to `tests/LayerCake.Slices.Tests` (null passes, approved passes, declined is 402 with the reason, unavailable is 503); verify `PlaceOrderTests` has no diff and the project is green

## 4. Shared contract suite

- [x] 4.1 `TendrHostFixture` (collection fixture, Kestrel on `127.0.0.1:0`, lazy start against the collection's PostgreSQL, authorization-id listing), wired into both twin collections and both host fixtures through `Tendr:BaseUrl`; outage host fixtures per twin; verify the suite builds and every existing scenario file has no diff
- [x] 4.2 `PaymentScenarios.cs` (six scenarios, both twins) and `PaymentOutageScenarios.cs` (two scenarios, both twins); run the outage class once before the after twin's catch exists and record the status it returns; verify all green on both hosts

## 5. Verification

- [x] 5.1 Run root `dotnet test` twice (warm) and once after `dotnet clean` (cold); record totals per project and wall-clock; 0 build warnings
- [x] 5.2 Re-run `tests/LayerCake.ContractTests.Marten` (27/27), `tests/LayerCake.Slices.Marten.Tests` (15/15), `tests/LayerCake.ContractTests.CleanTemplate` (6/10 as committed); verify none changed
- [x] 5.3 Negative checks, each reverted: swap the declaration order of `AuthorizeAsync` and the three-guard `Validate` and watch the guard-order scenario go red on the after twin; remove the decline throw from the before twin's handler and watch the 402 scenarios go red; verify `git diff` clean of both afterwards
- [x] 5.4 Live: compose up, run both twins and Tendr, place one card order and one declined card on each twin with curl, stop Tendr and see the 503; verify the compose database's existing orders still read back

## 6. Frontend, docs, bookkeeping

- [x] 6.1 Demo page: a "Pay now" select with the test cards and "Pay at pickup" as the default; `docs/frontend.md` updated; verify a 402 shows by hand
- [x] 6.2 `README.md` and `CLAUDE.md` (mode paragraph, slate row 5, tech-stack row, non-negotiable 3 note, ports, commands, schema list); `.gitignore` gains the after twin's `Internal/Generated/`; `openspec/config.yaml` context mentions slice 005; verify `grep -c "[—–]"` prints 0 on every new or rewritten passage meant for slides
- [x] 6.3 `docs/file-inventory.md`: the 005 row (before created and edited, after created and edited, Tendr outside both counts with line counts, suite changes) and the whole-twin recount by the inventory's method; verify the numbers by re-running the count command
- [x] 6.4 `docs/build-log.md`: dated entry with the decisions above, the ordering evidence, the 503 mechanism, Tendr hosting under test, the migration name, timings, and the "For Erik" calls
