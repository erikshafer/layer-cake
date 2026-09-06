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
