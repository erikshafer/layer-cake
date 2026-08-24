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
- src/after/LayerCake.Slices/Cakes/SeedData.cs
- src/after/LayerCake.Slices/Features/PublishCake.cs
- src/after/LayerCake.Slices/Features/BrowseCakes.cs
- src/after/LayerCake.Slices/Features/GetCake.cs

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
- src/after/LayerCake.Slices/Features/Coupons/CouponValidation.cs
- src/after/LayerCake.Slices/Features/ValidateCoupon.cs

After twin, files edited (2):
- src/after/LayerCake.Slices/SeedData.cs (moved from Cakes/ to project root and extended: it now seeds two modules)
- src/after/LayerCake.Slices/Program.cs (using statement for the SeedData move)

Shared contract suite (outside both counts): CouponScenarios.cs created; TwinHosts.cs edited to reset coupons alongside cakes.
