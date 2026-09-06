# Slice 004 — NotifyBaker over RabbitMQ

**Why this slice is in the talk:** slice 003 made "tell the baker" a cascaded message in the after twin and an inline row write in the before twin. This slice puts that one side effect on a real broker in BOTH twins, so the talk can show what a message costs a layered codebase (a port, an adapter, a hosted consumer, a re-dispatch, wiring in every ring) against what it costs a slice (configuration in `Program.cs`; the feature files do not change). Same broker both sides; only the idiom changes. Decided 2026-09-05.

## Contract (unchanged from slice 003)

`POST /orders`, `GET /orders/{id}`, `GET /baker/tasks` are byte-for-byte the slice 003 contract. No new endpoint. The observable side effect is unchanged: placing an order produces exactly one baker task for that order, visible on `GET /baker/tasks?orderId={id}` within the suite's polling window. What changes is the path the notification takes: it now crosses a RabbitMQ queue in both twins before the task is written.

## Before twin — REQUIRED structure

Earnest Clean Architecture, no padding, no collapsing. Expected elements:

- `Application/Common/Interfaces/IMessagePublisher.cs`: the port. `Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken)`.
- `Application/Orders/Messages/NotifyBakerMessage.cs`: `record NotifyBakerMessage(Guid OrderId, string Summary)`.
- `Application/Baker/Commands/CreateBakerTask/CreateBakerTaskCommand.cs` and `CreateBakerTaskCommandHandler.cs`: the consumer's re-dispatch target. The handler checks `IBakerTaskRepository.ExistsForOrderAsync` before adding (the broker delivers at least once; the suite asserts exactly one), stages the task, and commits through `IUnitOfWork`.
- `Infrastructure/Messaging/RabbitMqOptions.cs` (connection URI, queue name), `Infrastructure/Messaging/RabbitMqConnection.cs` (one `IConnection` per host, created lazily, disposed with the host), `Infrastructure/Messaging/RabbitMqMessagePublisher.cs : IMessagePublisher` (channel per publish, declares the queue durable, System.Text.Json camelCase body, persistent delivery mode, default exchange, routing key = queue name).
- `WebApi/Messaging/NotifyBakerConsumer.cs : BackgroundService`: own channel, declares the same queue with identical arguments, `AsyncEventingBasicConsumer`, manual ack. On a message: deserialize `NotifyBakerMessage`, create a DI scope, `ISender.Send(new CreateBakerTaskCommand(...))`, ack. On an exception: log and nack without requeue, with one comment saying a real system would dead-letter here.
- `PlaceOrderCommandHandler` edits: `IBakerTaskRepository` out, `IMessagePublisher` in (the constructor stays at seven dependencies; one of them is now a bus). After `SaveChangesAsync`, publish `NotifyBakerMessage`. Replace the "Imagine this line is SendGrid" comment with one that owns the gap in plain words, em-dash-free, for example: `// Published after the commit. If this line throws, the order exists and the baker never hears. No outbox, because most real ones do not have one.`
- `IBakerTaskRepository` and `BakerTaskRepository` edits: add `ExistsForOrderAsync(Guid orderId, CancellationToken)`.
- `Infrastructure/DependencyInjection.cs`: bind options from the `RabbitMq` section plus `ConnectionStrings:RabbitMq`, register the connection as a singleton and the publisher, and `Infrastructure.csproj` gains `RabbitMQ.Client`.
- `WebApi/Program.cs`: `AddHostedService<NotifyBakerConsumer>()`. `appsettings.json`: `ConnectionStrings:RabbitMq` (`amqp://localhost`) and `RabbitMq:QueueName` (`layercake-before-baker-tasks`).
- No validator for `CreateBakerTaskCommand` unless the pipeline demands one (it does not; the shape was fixed by deserialization). Record the choice in the build log either way.

## After twin — expected shape

`Program.cs` only:

- `opts.UseRabbitMq(...)` moves OUT of the `CritterWatch:Enabled` block and runs unconditionally, keeping `.AutoProvision()`. The CritterWatch `AddCritterWatchMonitoring` call stays inside the block.
- `opts.PublishMessage<NotifyBaker>().ToRabbitQueue("layercake-after-baker-tasks").UseDurableOutbox();` so the envelope is still written in the same Marten transaction as the order (the claim on the 3.15 slide) and sent after commit.
- `opts.ListenToRabbitQueue("layercake-after-baker-tasks");` so `NotifyBakerHandler` receives it in-process.
- Revisit the `UseDurableLocalQueues()` comment (it describes `NotifyBaker` as a local message, which stops being true). Keep the line if any local message could still exist, otherwise remove it; keep the comment honest and em-dash-free. Add `ConnectionStrings:rabbitmq` to `appsettings.json` for discoverability (the code already reads that key with an `amqp://localhost` fallback).
- `PlaceOrder.cs`, `NotifyBaker.cs`, and every other file in the project: untouched.

## Shared contract scenarios

Unchanged: the eleven order scenarios from slice 003, including `placing_order_produces_exactly_one_baker_task`, which is now the proof that a message crossed the broker on each twin (with no broker, neither consumer runs and the scenario fails). Fixture changes only:

- `Testcontainers.RabbitMq` added. A `RabbitMqContainerFixture` (`rabbitmq:4`, same major as docker-compose) per twin collection, added as a second `ICollectionFixture<>` on `BeforeTwinCollection` and `AfterTwinCollection`, mirroring the Postgres pattern.
- Both host fixtures pass the container's connection string via `UseSetting("ConnectionStrings:RabbitMq", ...)` (configuration keys are case-insensitive, so the after twin's lowercase read matches).
- `AfterHostFixture` REMOVES `services.DisableAllExternalWolverineTransports()`; `RunWolverineInSoloMode()` stays. Update the comment.
- Poll constants stay 250 ms / 5 s unless the real broker proves slower on a cold run; if so, raise the timeout constant only and record why.

## Out of scope

Outbox in the before twin. Retry, dead-lettering, or poison handling beyond log-and-nack. Any second message. Any change to routes, JSON shapes, status codes, seeds, the frontend page, the CritterWatch console, or `docs/slices/001-003`.
