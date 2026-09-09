# Per-slice file inventory

The abstract claims a trivial feature scatters across "a dozen files" in the before twin. Whatever the REAL number is, that is the number the slide says. This file is the evidence: after finishing each slice, append the actual files touched/created per twin, honestly counted (source files only; exclude csproj edits and generated code; note the convention if a judgment call comes up).

Format per slice:

```
## NNN SliceName (recorded YYYY-MM-DD)
Before twin (N files):
- path
- ...
After twin (N files):
- path
- ...
```

<!-- Append below. Do not backfill estimates; only record what was actually built. -->

## 001 PublishCake + BrowseCakes (recorded 2026-08-23)

Convention notes (judgment calls, applied consistently going forward): "created" counts source files created for the slice; pre-existing scaffold files edited to wire the slice in are listed separately; EF Core migration output (3 generated files under `Infrastructure/Persistence/Migrations/`) is excluded as generated code; the shared contract suite belongs to neither twin's count.

Before twin (19 files created):
- src/before/LayerCake.Domain/Entities/Cake.cs
- src/before/LayerCake.Application/Cakes/CakeDto.cs
- src/before/LayerCake.Application/Cakes/Commands/PublishCake/PublishCakeCommand.cs
- src/before/LayerCake.Application/Cakes/Commands/PublishCake/PublishCakeCommandHandler.cs
- src/before/LayerCake.Application/Cakes/Commands/PublishCake/PublishCakeCommandValidator.cs
- src/before/LayerCake.Application/Cakes/Queries/BrowseCakes/BrowseCakesQuery.cs
- src/before/LayerCake.Application/Cakes/Queries/BrowseCakes/BrowseCakesQueryHandler.cs
- src/before/LayerCake.Application/Cakes/Queries/GetCakeById/GetCakeByIdQuery.cs
- src/before/LayerCake.Application/Cakes/Queries/GetCakeById/GetCakeByIdQueryHandler.cs
- src/before/LayerCake.Application/Common/Interfaces/ICakeRepository.cs
- src/before/LayerCake.Application/Common/Mappings/CakeMappingProfile.cs
- src/before/LayerCake.Application/Common/Exceptions/ValidationException.cs
- src/before/LayerCake.Application/Common/Exceptions/NotFoundException.cs
- src/before/LayerCake.Application/Common/Exceptions/DuplicateCakeNameException.cs
- src/before/LayerCake.Infrastructure/Persistence/Configurations/CakeConfiguration.cs
- src/before/LayerCake.Infrastructure/Persistence/LayerCakeDbContextSeeder.cs
- src/before/LayerCake.Infrastructure/Repositories/CakeRepository.cs
- src/before/LayerCake.WebApi/Controllers/CakesController.cs
- src/before/LayerCake.WebApi/Filters/ApiExceptionFilterAttribute.cs

Before twin, scaffold files edited (4):
- src/before/LayerCake.Application/Common/Behaviors/ValidationBehavior.cs (throw the custom ValidationException)
- src/before/LayerCake.Infrastructure/Persistence/LayerCakeDbContext.cs (DbSet, audit stamping)
- src/before/LayerCake.Infrastructure/DependencyInjection.cs (repository registration)
- src/before/LayerCake.WebApi/Program.cs (exception filter, migrate + seed on startup)

After twin (5 files created):
- src/after/LayerCake.Slices/Cakes/Cake.cs
- src/after/LayerCake.Slices/Cakes/SeedData.cs (moved to the project root in slice 002; see below)
- src/after/LayerCake.Slices/Cakes/PublishCake.cs
- src/after/LayerCake.Slices/Cakes/BrowseCakes.cs
- src/after/LayerCake.Slices/Cakes/GetCake.cs

After twin, scaffold files edited (1):
- src/after/LayerCake.Slices/Program.cs (explicit camelCase serialization, seed on startup)

Shared contract suite (outside both counts): CakeScenarios.cs and ProblemDetailsAssertions.cs created; TwinHosts.cs and PingScenarios.cs edited for reset-and-reseed isolation and per-twin collections.

## 002 ValidateCoupon (recorded 2026-08-23)

Same conventions as 001. EF Core migration output (`AddCoupons` files) excluded as generated code.

Before twin (13 files created):
- src/before/LayerCake.Domain/Entities/Coupon.cs
- src/before/LayerCake.Domain/Enums/CouponStatus.cs
- src/before/LayerCake.Application/Common/Interfaces/ICouponRepository.cs
- src/before/LayerCake.Application/Common/Interfaces/ICouponValidationService.cs
- src/before/LayerCake.Application/Common/Interfaces/IDateTimeProvider.cs
- src/before/LayerCake.Application/Coupons/CouponValidationDto.cs
- src/before/LayerCake.Application/Coupons/CouponValidationService.cs
- src/before/LayerCake.Application/Coupons/Queries/ValidateCoupon/ValidateCouponQuery.cs
- src/before/LayerCake.Application/Coupons/Queries/ValidateCoupon/ValidateCouponQueryHandler.cs
- src/before/LayerCake.Infrastructure/Persistence/Configurations/CouponConfiguration.cs
- src/before/LayerCake.Infrastructure/Repositories/CouponRepository.cs
- src/before/LayerCake.Infrastructure/Services/DateTimeProvider.cs
- src/before/LayerCake.WebApi/Controllers/CouponsController.cs

Before twin, scaffold files edited (4):
- src/before/LayerCake.Application/DependencyInjection.cs (validation-service registration)
- src/before/LayerCake.Infrastructure/DependencyInjection.cs (repository + clock registrations)
- src/before/LayerCake.Infrastructure/Persistence/LayerCakeDbContext.cs (DbSet)
- src/before/LayerCake.Infrastructure/Persistence/LayerCakeDbContextSeeder.cs (coupon seeds)

After twin (3 files created):
- src/after/LayerCake.Slices/Coupons/Coupon.cs
- src/after/LayerCake.Slices/Coupons/CouponValidation.cs
- src/after/LayerCake.Slices/Coupons/ValidateCoupon.cs

After twin, files edited (2):
- src/after/LayerCake.Slices/SeedData.cs (moved from Cakes/ to project root and extended: it now seeds two modules)
- src/after/LayerCake.Slices/Program.cs (using statement for the SeedData move)

Shared contract suite (outside both counts): CouponScenarios.cs created; TwinHosts.cs edited to reset coupons alongside cakes.

## 003 PlaceOrder (recorded 2026-08-25)

Same conventions as 001. EF Core migration output (`AddOrders` files) excluded as generated code. `CouponStatusWire.cs` is counted here (created this slice) even though it extracts logic slice 002 wrote inline: the extraction exists because PlaceOrder became the second consumer.

Before twin (26 files created):
- src/before/LayerCake.Domain/Entities/Order.cs
- src/before/LayerCake.Domain/Entities/OrderLine.cs
- src/before/LayerCake.Domain/Entities/BakerTask.cs
- src/before/LayerCake.Application/Common/Interfaces/IOrderRepository.cs
- src/before/LayerCake.Application/Common/Interfaces/IBakerTaskRepository.cs
- src/before/LayerCake.Application/Common/Interfaces/IUnitOfWork.cs
- src/before/LayerCake.Application/Common/Exceptions/UnknownCakesException.cs
- src/before/LayerCake.Application/Common/Exceptions/InvalidCouponException.cs
- src/before/LayerCake.Application/Common/Mappings/OrderMappingProfile.cs
- src/before/LayerCake.Application/Coupons/CouponStatusWire.cs
- src/before/LayerCake.Application/Orders/OrderDto.cs
- src/before/LayerCake.Application/Orders/OrderLineDto.cs
- src/before/LayerCake.Application/Orders/Commands/PlaceOrder/PlaceOrderCommand.cs
- src/before/LayerCake.Application/Orders/Commands/PlaceOrder/PlaceOrderCommandValidator.cs
- src/before/LayerCake.Application/Orders/Commands/PlaceOrder/PlaceOrderCommandHandler.cs
- src/before/LayerCake.Application/Orders/Queries/GetOrderById/GetOrderByIdQuery.cs
- src/before/LayerCake.Application/Orders/Queries/GetOrderById/GetOrderByIdQueryHandler.cs
- src/before/LayerCake.Application/Baker/BakerTaskDto.cs
- src/before/LayerCake.Application/Baker/Queries/GetBakerTasks/GetBakerTasksQuery.cs
- src/before/LayerCake.Application/Baker/Queries/GetBakerTasks/GetBakerTasksQueryHandler.cs
- src/before/LayerCake.Infrastructure/Persistence/Configurations/OrderConfiguration.cs
- src/before/LayerCake.Infrastructure/Persistence/Configurations/BakerTaskConfiguration.cs
- src/before/LayerCake.Infrastructure/Repositories/OrderRepository.cs
- src/before/LayerCake.Infrastructure/Repositories/BakerTaskRepository.cs
- src/before/LayerCake.WebApi/Controllers/OrdersController.cs
- src/before/LayerCake.WebApi/Controllers/BakerController.cs

Before twin, scaffold/earlier-slice files edited (6):
- src/before/LayerCake.Application/Common/Interfaces/ICakeRepository.cs (GetByIdsAsync for the existence guard)
- src/before/LayerCake.Infrastructure/Repositories/CakeRepository.cs (GetByIdsAsync implementation)
- src/before/LayerCake.Application/Coupons/Queries/ValidateCoupon/ValidateCouponQueryHandler.cs (uses the extracted CouponStatusWire)
- src/before/LayerCake.Infrastructure/Persistence/LayerCakeDbContext.cs (DbSets, implements IUnitOfWork)
- src/before/LayerCake.Infrastructure/DependencyInjection.cs (repository + unit-of-work registrations)
- src/before/LayerCake.WebApi/Filters/ApiExceptionFilterAttribute.cs (422 mappings for unknown cakes and invalid coupon)

After twin (6 files created):
- src/after/LayerCake.Slices/Orders/Order.cs
- src/after/LayerCake.Slices/Orders/BakerTask.cs
- src/after/LayerCake.Slices/Orders/PlaceOrder.cs
- src/after/LayerCake.Slices/Orders/NotifyBaker.cs
- src/after/LayerCake.Slices/Orders/GetOrder.cs
- src/after/LayerCake.Slices/Orders/GetBakerTasks.cs

After twin, files edited: none.

Shared contract suite (outside both counts): OrderScenarios.cs created; TwinHosts.cs edited to reset orders and baker tasks alongside cakes and coupons.

## Frontend (recorded 2026-08-25, outside both counts)

Not a slice; session spec `docs/frontend.md`. The page belongs to neither twin's count.

Created (1 file):
- src/frontend/index.html (300 lines: one static page, inline CSS + JS)

Twins edited (Development-only CORS for the page, nothing else):
- src/before/LayerCake.WebApi/Program.cs (+5 lines)
- src/after/LayerCake.Slices/Program.cs (+5 lines)

Whole-twin line counts re-run (all `.cs` under `src/before` excluding `obj/` and `Persistence/Migrations/`; all `.cs` under `src/after` excluding `obj/`; raw count including blanks): before twin 1,701 (was 1,696 as of 2026-08-25), after twin 692 (was 687). The +5 on each side is exactly the CORS lines.

Whole-twin line counts re-run 2026-09-04 (same method): before twin 1,733 (was 1,701), after twin 699 (was 692). Before +32: the seeder's coupon-window refresh (+12), the exception filter's logged 500 fallback (+19), and a reworded clock comment (+1). After +7: the Marten unique index on cake name and its comment.

Whole-twin line counts re-run 2026-09-05 (same method): before twin 1,733 (unchanged), after twin 706 (was 699). After +7: `opts.Policies.UseDurableLocalQueues()` and its comment in `Program.cs`, so the cascaded `NotifyBaker` is a real outbox message (see `docs/critter-stack-audit.md`, A1). Non-blank: before 1,432 (unchanged), after 585 (was 579). Test-fixture change (`TwinHosts.cs`) is outside both counts.

Whole-twin line counts re-run 2026-09-05 after Audit Tier B first pass (same method): after twin 706 raw / 585 non-blank (unchanged; the `NotifyBakerHandler` rewrite to a pure `Store<BakerTask>` return is net zero), before twin 1,733 / 1,432 (unchanged). New project `tests/LayerCake.Slices.Tests` (4 files) is a test project and belongs to neither twin's count.

Whole-twin line counts re-run 2026-09-05 after Audit Tier B second pass (same method): after twin 695 raw / 574 non-blank (was 706 / 585). The -11 is exactly the `using` lines made redundant when the slices moved from `Features/` into `Cakes/`, `Coupons/`, `Orders/` and took those namespaces. The per-slice paths above were rewritten to the new locations in the same change; file count per slice is unchanged. Before twin 1,733 / 1,432 (unchanged).

## 004 NotifyBaker over RabbitMQ (recorded 2026-09-05)

Same conventions as 001. Non-`.cs` wiring edits (`.csproj`, `appsettings.json`, `Directory.Packages.props`) are listed separately and stay OUTSIDE the count, so the count remains comparable with slices 001 to 003. This slice adds no endpoint and changes no contract; the numbers are what it costs each twin to put one existing side effect on a real broker.

Before twin (8 files created):
- src/before/LayerCake.Application/Common/Interfaces/IMessagePublisher.cs
- src/before/LayerCake.Application/Orders/Messages/NotifyBakerMessage.cs
- src/before/LayerCake.Application/Baker/Commands/CreateBakerTask/CreateBakerTaskCommand.cs
- src/before/LayerCake.Application/Baker/Commands/CreateBakerTask/CreateBakerTaskCommandHandler.cs
- src/before/LayerCake.Infrastructure/Messaging/RabbitMqOptions.cs
- src/before/LayerCake.Infrastructure/Messaging/RabbitMqConnection.cs
- src/before/LayerCake.Infrastructure/Messaging/RabbitMqMessagePublisher.cs
- src/before/LayerCake.WebApi/Messaging/NotifyBakerConsumer.cs

Before twin, earlier-slice `.cs` files edited (6):
- src/before/LayerCake.Application/Orders/Commands/PlaceOrder/PlaceOrderCommandHandler.cs (`IBakerTaskRepository` out, `IMessagePublisher` in; publish after the commit)
- src/before/LayerCake.Application/Common/Interfaces/IBakerTaskRepository.cs (`ExistsForOrderAsync`)
- src/before/LayerCake.Application/Common/Interfaces/IUnitOfWork.cs (summary no longer claims the order and the task share a transaction)
- src/before/LayerCake.Infrastructure/Repositories/BakerTaskRepository.cs (`ExistsForOrderAsync` implementation)
- src/before/LayerCake.Infrastructure/DependencyInjection.cs (options, connection singleton, publisher registration)
- src/before/LayerCake.WebApi/Program.cs (`AddHostedService<NotifyBakerConsumer>`)

Before twin, non-`.cs` wiring edits (outside the count, 3):
- src/before/LayerCake.Infrastructure/LayerCake.Infrastructure.csproj (`RabbitMQ.Client` reference)
- src/before/LayerCake.WebApi/appsettings.json (`ConnectionStrings:RabbitMq`, `RabbitMq:QueueName`)
- Directory.Packages.props (`RabbitMQ.Client` 7.2.2 pin; shared file)

After twin (0 files created).

After twin, `.cs` files edited (1):
- src/after/LayerCake.Slices/Program.cs (`UseRabbitMq` unconditional, `PublishMessage<NotifyBaker>().ToRabbitQueue(...).UseDurableOutbox()`, `ListenToRabbitQueue`; `UseDurableLocalQueues` and its comment removed)

After twin, non-`.cs` wiring edits (outside the count, 2):
- src/after/LayerCake.Slices/appsettings.json (`ConnectionStrings:rabbitmq`, for discoverability; the code already had the fallback)
- src/after/LayerCake.Slices/LayerCake.Slices.csproj (comment only: it claimed the contract tests never need RabbitMQ)

Shared contract suite (outside both counts): TwinHosts.cs edited (`RabbitMqContainerFixture`, second collection fixture on both twins, broker connection string into both hosts, `DisableAllExternalWolverineTransports` removed from the after fixture); the suite csproj gains `Testcontainers.RabbitMq`; Directory.Packages.props gains the `Testcontainers.RabbitMq` 4.14.0 pin. No scenario file changed.

Whole-twin line counts re-run 2026-09-05 after slice 004 (same method): before twin 2,061 raw / 1,705 non-blank (was 1,733 / 1,432; +328 / +273, the eight new files plus the six edits above). After twin 703 raw / 581 non-blank (was 695 / 574; +8 / +7: the RabbitMQ block in `Program.cs` replaced `UseDurableLocalQueues()` and its comment).
Whole-twin line counts re-run 2026-09-05 after the before twin's consumer shutdown fix (same method; see `docs/build-log.md`): before twin 2,140 raw / 1,775 non-blank (was 2,061 / 1,705; +79 / +70). Two slice 004 files edited, none created: `src/before/LayerCake.WebApi/Messaging/NotifyBakerConsumer.cs` (idempotent `StopAsync`, in-flight gate, bounded consumer cancel, abort on dispose) and `src/before/LayerCake.Infrastructure/Messaging/RabbitMqConnection.cs` (bounded abort before dispose). After twin unchanged at 703 / 581.

## Experiment: slice 001 on the Clean Architecture Solution Template (2026-09-06, branch experiment/before-clean-template, not part of the scorecard)

`Clean.Architecture.Solution.Template` 10.8.0, generated with `dotnet new ca-sln -n LayerCake.CleanTemplate -cf None -db postgresql -o experiments/before-clean-template` (.NET 10 target), not in `LayerCake.slnx`, never under `src/`. Same conventions as slice 001 above: "created" counts `.cs` files created for the feature; pre-existing scaffold files edited to wire the feature in are listed separately; the test host belongs to no count. The template uses no migrations (`EnsureDeletedAsync` + `EnsureCreatedAsync` in its initialiser), so there is no generated code to exclude. Nothing above this section changes and the totals above stand. Full narrative in `docs/build-log.md` (2026-09-06).

Template (9 files created):
- experiments/before-clean-template/src/Domain/Entities/Cake.cs
- experiments/before-clean-template/src/Application/Cakes/CakeDto.cs (nested AutoMapper profile, the template's idiom)
- experiments/before-clean-template/src/Application/Cakes/Commands/PublishCake/PublishCake.cs (command + handler in one file, the template's idiom)
- experiments/before-clean-template/src/Application/Cakes/Commands/PublishCake/PublishCakeCommandValidator.cs
- experiments/before-clean-template/src/Application/Cakes/Queries/BrowseCakes/BrowseCakes.cs (query + handler, `ProjectTo`)
- experiments/before-clean-template/src/Application/Cakes/Queries/GetCake/GetCake.cs (query + handler, `ProjectTo` + `Guard.Against.NotFound`)
- experiments/before-clean-template/src/Application/Common/Exceptions/DuplicateCakeNameException.cs
- experiments/before-clean-template/src/Infrastructure/Data/Configurations/CakeConfiguration.cs (unique index on Name)
- experiments/before-clean-template/src/Web/Endpoints/Cakes.cs (one `IEndpointGroup`, all three endpoints, `RoutePrefix` overridden to `/cakes`)

Template, scaffold `.cs` files edited for the feature (8):
- experiments/before-clean-template/src/Domain/Common/BaseEntity.cs (the template's `int`-keyed base made generic, the path its own comment suggests; behaviour stays on the non-generic root so both `SaveChanges` interceptors are untouched)
- experiments/before-clean-template/src/Domain/Common/BaseAuditableEntity.cs (same split: audit fields on the non-generic root, `Id` on `BaseAuditableEntity<TId>`)
- experiments/before-clean-template/src/Domain/Entities/TodoList.cs (`: BaseAuditableEntity<int>`, ripple of the above)
- experiments/before-clean-template/src/Domain/Entities/TodoItem.cs (same ripple)
- experiments/before-clean-template/src/Application/Common/Interfaces/IApplicationDbContext.cs (`DbSet<Cake>`)
- experiments/before-clean-template/src/Infrastructure/Data/ApplicationDbContext.cs (`DbSet<Cake>`)
- experiments/before-clean-template/src/Infrastructure/Data/ApplicationDbContextInitialiser.cs (the same three seed cakes as both twins)
- experiments/before-clean-template/src/Web/Infrastructure/ProblemDetailsExceptionHandler.cs (409 mapping for the new exception)

Side by side, the same three endpoints: before twin 19 created / 4 edited (23 touched); template 9 created / 8 edited (17 touched; 13 if the four Guid-key ripple edits are set aside as a one-time cost); after twin 5 created / 1 edited (6 touched).

Outside the feature count (1):
- experiments/before-clean-template/src/Web/Endpoints/Ping.cs (the shared ping scenario; the twins' ping endpoints predate slice 001 and sit outside its count too)

Scaffold edits the feature or this machine forced (outside the count, 2; neither is C#):
- experiments/before-clean-template/global.json (SDK floor lowered from 10.0.201 to 10.0.100: only the 10.0.1xx band is installed here and `rollForward: latestFeature` does not cross feature bands)
- experiments/before-clean-template/Directory.Build.props (`NU1902;NU1903` appended to the template's own `WarningsNotAsErrors` list: the generated package graph carries current NuGet advisories, and the template's `TreatWarningsAsErrors` turned 162 audit warnings into restore errors, so `dotnet new ca-sln` + `dotnet build` fails as generated on 2026-09-06)

Not needed, checked: isolation props (the template ships its own `Directory.Build.props` and `Directory.Packages.props`, which stop the repo's chain), a provider swap (`--database postgresql` exists), `public partial class Program` (the SDK already emits a public `Program`), removing `RequireAuthorization()` (the new group simply never calls it).

Test host (outside every count): `tests/LayerCake.ContractTests.CleanTemplate/` (csproj + `CleanTemplateHost.cs`: collection, fixture, `CleanTemplateCakes`, `CleanTemplatePing`), its own project and not in `LayerCake.slnx` because the template's MediatR 14.1.0 / AutoMapper 16.1.1 cannot share a test process with the before twin's 12.5.0 / 14.0.0 (see the build log). `tests/LayerCake.ContractTests/` is unchanged on the branch.

Scaffold snapshot as generated, before any feature code (`.cs` per project, excluding bin/obj): src/AppHost 2, src/Application 33, src/Domain 12, src/Infrastructure 11, src/ServiceDefaults 1, src/Shared 1, src/Web 16 (src total 76); tests/Application.FunctionalTests 14, tests/Application.UnitTests 3, tests/Domain.UnitTests 1, tests/Infrastructure.IntegrationTests 1, tests/TestAppHost 1 (total 96).

Line counts (this file's method applied to `experiments/before-clean-template/src`, all `.cs` excluding obj/bin, no migrations exist): as generated 2,340 raw / 1,882 non-blank in 76 files; with slice 001 and ping 2,635 / 2,116 in 86 files. The nine feature files alone are 236 raw / 185 non-blank; the eight edits add 39 raw / 33 non-blank; ping is 20 / 16.

## After twin on EF Core (2026-09-09, Erik's call): the rows above describe the Marten twin

The core after twin moved from Marten documents to EF Core through Wolverine's EF Core integration, so both twins share the ORM and the database engine and only the architecture and the mediator differ. The Marten twin moved intact to `experiments/after-marten/LayerCake.Slices.Marten` and still runs the same scenarios (`tests/LayerCake.ContractTests.Marten`, 27/27) and the same unit facts (`tests/LayerCake.Slices.Marten.Tests`, 15/15); both are outside `LayerCake.slnx`, like the template experiment. Narrative in `docs/build-log.md` (2026-09-09).

**Every after-twin per-slice file list above still holds path for path, and so does every per-slice count.** The same feature files, in the same folders, with the same names; only what is inside them changed. That is the number a slide quotes:

| Feature | Before twin | After twin (Marten) | After twin (EF Core) |
|---|---|---|---|
| 001 Publish and browse cakes | 19 created, 4 edited | 5 created, 1 edited | 5 created, 1 edited |
| 002 Validate coupon | 13 created, 4 edited | 3 created, 2 edited | 3 created, 2 edited |
| 003 Place order | 26 created, 6 edited | 6 created, none edited | 6 created, none edited |
| 004 Notify the baker over RabbitMQ | 8 created, 6 edited | none created, 1 edited | none created, 1 edited |

**"Edited zero existing files" on the order slice survives the move, by design.** `src/after/LayerCake.Slices/LayerCakeDbContext.cs` has no `DbSet` properties and no per-entity mapping in it: it sets the `after` default schema and calls `ApplyConfigurationsFromAssembly`. Each table configures itself in its own feature file next to the type it maps (`Cakes/Cake.cs` carries `Cake` and `CakeTable`, and so on), and endpoints reach for `db.Set<T>()`. Adding a feature therefore adds files and edits nothing, which is exactly what the Marten document store gave for free. The before twin's `LayerCakeDbContext.cs` is edited by three of the four slices; the after twin's by none.

After twin, scaffold `.cs` file created by the move (outside every slice count, the same way `Program.cs`, `AfterTwin.cs` and `Ping.cs` are):
- src/after/LayerCake.Slices/LayerCakeDbContext.cs (23 raw / 20 non-blank)

There is no migrations folder in the after twin and so nothing to exclude as generated code: `opts.UseEntityFrameworkCoreWolverineManagedMigrations()` plus `builder.Services.AddResourceSetupOnStartup()` means Weasel builds the tables this DbContext describes, and Wolverine's own envelope tables, when the host starts. The before twin keeps its EF Core migrations, and those 4 generated files stay excluded as before.

Whole-twin line counts re-run 2026-09-09 (same method: all `.cs` under `src/before` excluding `obj/` and `Persistence/Migrations/`; all `.cs` under `src/after` excluding `obj/`):

| | raw | non-blank | `.cs` files |
|---|---|---|---|
| Before twin | 2,140 | 1,775 | unchanged |
| After twin, EF Core (current) | **844** | **707** | 18 |
| After twin, Marten (now the experiment) | 703 | 581 | 17 |

Before twin unchanged; nothing under `src/before` was touched. After twin +141 raw / +126 non-blank, measured file by file against the Marten twin:

| file | Marten | EF Core | delta |
|---|---|---|---|
| LayerCakeDbContext.cs | (none) | 23 / 20 | +23 / +20 |
| Cakes/Cake.cs | 18 / 13 | 41 / 33 | +23 / +20 |
| Coupons/Coupon.cs | 21 / 16 | 29 / 23 | +8 / +7 |
| Orders/Order.cs | 44 / 31 | 70 / 55 | +26 / +24 |
| Orders/BakerTask.cs | 17 / 13 | 30 / 24 | +13 / +11 |
| Orders/NotifyBaker.cs | 22 / 19 | 45 / 40 | +23 / +21 |
| SeedData.cs | 48 / 40 | 66 / 56 | +18 / +16 |
| Program.cs | 99 / 80 | 98 / 80 | -1 / +0 |
| the four read paths | 260 / 222 | 268 / 229 | +8 / +7 |
| unchanged (AfterTwin, Ping, GetCake, PublishCake, CouponValidation, GetOrder) | | | 0 / 0 |

What that says, since a slide may want it in one sentence: the bootstrap is a wash (`Program.cs` is one line SHORTER on EF Core; four registration calls replace `AddMarten(...).IntegrateWithWolverine().UseLightweightSessions()`), and essentially the whole +141 is the price of telling a relational store what a document store infers. The four `IEntityTypeConfiguration<T>` blocks that now sit in their feature files (`CakeTable`, `CouponTable`, `OrderTable`, `BakerTaskTable`) are +70 / +62 of it. `NotifyBaker.cs` is +23 / +21: the `AddBakerTask` side effect replaces a one-line `Storage.Store<BakerTask>` return and has to check for redelivery, where identity-keyed upsert made that free. `SeedData.cs` is +18 / +16 for the same reason on the coupon seeds. The endpoints themselves barely moved: `PublishCake.cs`, `GetCake.cs`, `GetOrder.cs` and `CouponValidation.cs` are byte-identical, and the four read paths cost +8 raw between them for `db.Set<T>().AsNoTracking()`.

One behaviour note that belongs with the numbers, not buried in the build log: Wolverine's `Storage.Store<T>` is an upsert against Marten but generates `// No explicit update necessary with EF Core without a Version property` against EF Core, so it silently does nothing for a new row. That is why `NotifyBakerHandler` returns an `ISideEffect` rather than a storage action, and why it carries `[Transactional]` (the handler takes no DbContext, so `AutoApplyTransactions` has nothing to notice). The handler stays a pure function returning a value, which is what the unit test asserts on.

The ratio the scorecard slide quotes moves from about 3.0x to about **2.5x** (raw 2,140 vs 844; non-blank 1,775 vs 707). The Marten twin's 703 / 581 is now an experiment number and does not belong on the scorecard.

DbContext callers, for the "how many places touch persistence" slide: **9 methods in 7 files** take `LayerCakeDbContext` as a parameter, or **8 in 6** counting feature code only (that is, excluding `SeedData.ApplyAsync`).

- src/after/LayerCake.Slices/Cakes/BrowseCakes.cs:13 `Get`
- src/after/LayerCake.Slices/Cakes/PublishCake.cs:36 `ValidateAsync`, :57 `Post`
- src/after/LayerCake.Slices/Coupons/ValidateCoupon.cs:25 `Get`
- src/after/LayerCake.Slices/Orders/GetBakerTasks.cs:21 `Get`
- src/after/LayerCake.Slices/Orders/NotifyBaker.cs:16 `AddBakerTask.ExecuteAsync`
- src/after/LayerCake.Slices/Orders/PlaceOrder.cs:57 `LoadAsync`, :128 `Post`
- src/after/LayerCake.Slices/SeedData.cs:14 `ApplyAsync`

Two further references are not parameters and are not methods: the registration at `Program.cs:38` and the scope resolve at `SeedData.cs:62`. `GetCake` and `GetOrder` take no DbContext at all; `[Entity(Required = true, OnMissing = OnMissing.ProblemDetailsWith404)]` resolves them against it.

The Marten twin, for reference, is unchanged at `experiments/after-marten/LayerCake.Slices.Marten` (17 `.cs` files, 703 raw / 581 non-blank) and belongs to no count above. Its test projects (`tests/LayerCake.ContractTests.Marten`, 2 files; `tests/LayerCake.Slices.Marten.Tests`, 4 files) belong to no count either.
