# Tasks: slice-004-notify-baker-over-rabbitmq

Contract, structure, and scenarios: `docs/slices/004-notify-baker-over-rabbitmq.md`. Decisions: `design.md` (queue-per-twin topology, publish-after-commit with no outbox, consumer in WebApi re-dispatching through MediatR, `ExistsForOrderAsync` idempotency, connection and channel lifecycle, `UseDurableOutbox` on the after twin's publish rule, one RabbitMQ container per twin collection, identical queue-declare arguments).

## 1. Package pins

- [x] 1.1 Add `RabbitMQ.Client` 7.2.2 and `Testcontainers.RabbitMq` 4.14.0 to `Directory.Packages.props` with a dated "addition, not a bump" comment in the Testcontainers.PostgreSql style; verify `git diff` touches no existing pin and `dotnet restore` succeeds

## 2. Before twin (earnest Clean Architecture)

- [x] 2.1 Add `Application/Common/Interfaces/IMessagePublisher.cs` (the port) and `Application/Orders/Messages/NotifyBakerMessage.cs`; verify the Application project builds with no new package references
- [x] 2.2 Add `ExistsForOrderAsync(Guid orderId, CancellationToken)` to `IBakerTaskRepository` and `BakerTaskRepository`; verify the Infrastructure project builds
- [x] 2.3 Add `Application/Baker/Commands/CreateBakerTask/CreateBakerTaskCommand.cs` and `CreateBakerTaskCommandHandler.cs` (exists-check, stage, commit through `IUnitOfWork`); no validator (record the choice in the build log); verify by reading that the handler never saves twice and skips the add when a task exists
- [x] 2.4 Add `Infrastructure/Messaging/RabbitMqOptions.cs`, `RabbitMqConnection.cs` (lazy singleton `IConnection`, disposed with the host), and `RabbitMqMessagePublisher.cs : IMessagePublisher` (channel per publish, durable queue declare, camelCase System.Text.Json body, persistent delivery, default exchange, routing key = queue name); add `RabbitMQ.Client` to `Infrastructure.csproj`; register options, connection, and publisher in `Infrastructure/DependencyInjection.cs`; verify the Infrastructure project builds with 0 warnings
- [x] 2.5 Edit `PlaceOrderCommandHandler`: `IBakerTaskRepository` out, `IMessagePublisher` in, publish `NotifyBakerMessage` after `SaveChangesAsync` with the em-dash-free comment that owns the gap; verify the constructor still lists seven dependencies and the baker-task write is gone from the handler
- [x] 2.6 Add `WebApi/Messaging/NotifyBakerConsumer.cs : BackgroundService` (own channel, identical queue declare, `AsyncEventingBasicConsumer`, manual ack, DI scope + `ISender.Send(new CreateBakerTaskCommand(...))`, log-and-nack-without-requeue on exception with the one dead-letter comment); register with `AddHostedService` in `Program.cs`; add `ConnectionStrings:RabbitMq` and `RabbitMq:QueueName` to `appsettings.json`; verify the WebApi project builds with 0 warnings and every comment in the consumer and the handler is em-dash-free

## 3. After twin (Wolverine + Marten slices)

- [x] 3.1 Edit `Program.cs` only: `UseRabbitMq(...).AutoProvision()` unconditional, `PublishMessage<NotifyBaker>().ToRabbitQueue("layercake-after-baker-tasks").UseDurableOutbox()`, `ListenToRabbitQueue("layercake-after-baker-tasks")`, `AddCritterWatchMonitoring` still gated, `UseDurableLocalQueues()` kept or removed with an honest em-dash-free comment; add `ConnectionStrings:rabbitmq` to `appsettings.json`; verify `git status` shows no other `.cs` file in `src/after` changed
- [x] 3.2 Run `dotnet run --project src/after/LayerCake.Slices -- describe` with the compose stack up and verify the `layercake-after-baker-tasks` sending endpoint reports Durable mode; paste the relevant lines into the build log

## 4. Shared contract suite (fixtures only)

- [x] 4.1 Add `Testcontainers.RabbitMq` to the suite's csproj; add `RabbitMqContainerFixture` (`rabbitmq:4`) and register it as a second `ICollectionFixture<>` on both twin collections; verify the suite project builds
- [x] 4.2 Pass the container's connection string to both host fixtures via `UseSetting("ConnectionStrings:RabbitMq", ...)`; remove `DisableAllExternalWolverineTransports()` from `AfterHostFixture`, keep `RunWolverineInSoloMode()`, update the comment; verify no scenario file changed

## 5. Verification

- [x] 5.1 Run `dotnet test` twice with Docker running and verify 54/54 on both hosts plus 15/15 unit facts, 0 build warnings, both times; record the wall-clock seconds against the 13 s baseline measured before this slice
- [x] 5.2 With the compose stack up, run both twins live, place one order on each through `src/frontend/index.html`, and verify in the RabbitMQ management UI that both `layercake-*-baker-tasks` queues exist with one consumer and zero backlog, that the baker's board shows the task on each twin, and that the CritterWatch queues are untouched
- [x] 5.3 Negative check, once, then revert: stop the broker mid-run or comment out the after twin's `ListenToRabbitQueue` line and verify `placing_order_produces_exactly_one_baker_task` fails on that twin; confirm `git diff` is clean of the experiment afterwards

## 6. Bookkeeping and docs

- [x] 6.1 Append a "Slice 004" section to `docs/file-inventory.md` (before twin `.cs` created and edited, after twin `.cs` created and edited, non-`.cs` wiring edits listed separately) and the whole-twin recount by the inventory's rule; verify the numbers by re-running the count command
- [x] 6.2 Append a dated `docs/build-log.md` entry: decisions as applied, surprises, the `describe` output, the suite timings, the negative-check outcome, the no-validator choice
- [x] 6.3 Update README (tour, run-it, monitor wording), CLAUDE.md (commands, tech-stack Messaging row, OpenSpec section and slate, CritterWatch RESOLVED line), `openspec/config.yaml` context, and the `docker-compose.yml` RabbitMQ comment; verify no file still claims the suite or `dotnet test` is broker-free
