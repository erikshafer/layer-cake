# Design: slice-004-notify-baker-over-rabbitmq

## Context

Slice 003 is archived and green. The after twin's `PlaceOrderEndpoint.Post` returns `(PlacedOrder, NotifyBaker)`; Wolverine routes `NotifyBaker` to a durable local queue and `NotifyBakerHandler` stores the `BakerTask`. The before twin's `PlaceOrderCommandHandler` writes the `BakerTask` inline through `IBakerTaskRepository` and commits once through `IUnitOfWork`. docker-compose already runs RabbitMQ 4 for CritterWatch, and the after twin already references `WolverineFx.RabbitMQ` (gated behind `CritterWatch:Enabled`). The contract suite runs both hosts concurrently in one process, each on its own Testcontainers PostgreSQL. Behavior and structure are settled in `docs/slices/004-notify-baker-over-rabbitmq.md`; see proposal.md for motivation. This document records the decisions Erik took on 2026-09-05 with the trade-offs in view, so no later session re-litigates them, and settles the mechanics the slice spec leaves to the build.

## Goals / Non-Goals

**Goals:**

- Fix the broker topology so two hosts in one test process against real brokers can never consume each other's messages.
- Fix the before twin's publish timing and consumer placement so the layered cost is honest (no padding, no shortcut) and the dual-write gap is visible on purpose.
- Fix the after twin's routing so the outbox claim stays literally true after the message leaves the process.
- Fix the test fixture pattern so `dotnet test` still needs only Docker.

**Non-Goals:**

- Anything re-litigating the settled contract, the before twin's required structure, or the after twin's `Program.cs`-only shape (slice spec owns those).
- Outbox, retry, dead-lettering, poison handling, second messages, second deployables (proposal non-goals).

## Decisions

### Same broker on both sides, one queue per twin

RabbitMQ on both twins, the broker compose already runs. This is the Postgres move again: the engine never changes between twins, only the idiom for reaching it. Queue names `layercake-before-baker-tasks` and `layercake-after-baker-tasks`, default exchange, routing key equals queue name, durable queues. Two queues because the two hosts run concurrently in one test process against real brokers; a shared name would let one twin's consumer drain the other twin's notification and fail the exactly-one assertion on the wrong side. Alternative considered: one queue with a per-twin header filter; rejected as invisible topology that a slide cannot show.

### Before twin: raw `RabbitMQ.Client`, no framework

Hand-written publisher and consumer, not MassTransit, Rebus, or NServiceBus. That is exactly what the port-and-adapter split leaves a team writing when no framework is doing it, and the point of the slide is the cost of that writing. `RabbitMQ.Client` 7.x is async-first; the code uses `CreateConnectionAsync`, `CreateChannelAsync`, `BasicPublishAsync`, `BasicAckAsync`, `BasicNackAsync`, and `AsyncEventingBasicConsumer` with no sync-over-async. Alternative considered: MassTransit, which would hide the port, the adapter, and the hosted service behind one registration and make the before twin's cost look smaller than a raw-client team pays; rejected because the exhibit is the hand-written seam.

### Before twin: publish after commit, no outbox

`PlaceOrderCommandHandler` saves through `IUnitOfWork`, then calls `IMessagePublisher.PublishAsync`. If the publish throws, the order exists and the baker never hears. The gap is real, owned on a slide in one sentence, and carried in the code by one em-dash-free comment. No outbox, no retry loop, no dead-letter exchange: most real layered codebases do not have one, and adding one here would be padding the before twin to look worse or better than it is. Alternative considered: publish before commit; rejected because that inverts the gap (a baker task for an order that never committed) and is the less common real-world shape.

### Before twin: consumer is a `BackgroundService` in WebApi, re-dispatching through MediatR

A consumer is a delivery mechanism like a controller, so it belongs in the outer ring, and it stays thin the same way a controller does: deserialize, open a DI scope, `ISender.Send(new CreateBakerTaskCommand(...))`, ack. The publisher (adapter) lives in Infrastructure behind `IMessagePublisher` in Application. On an exception the consumer logs and nacks without requeue, with one comment saying a real system would dead-letter here. Alternative considered: the consumer in Infrastructure; rejected because Infrastructure has no reason to know MediatR is the dispatch mechanism, and a hosted service is host wiring.

### Before twin: idempotency via `ExistsForOrderAsync`

The broker delivers at least once and the suite asserts exactly one, so `CreateBakerTaskCommandHandler` checks `IBakerTaskRepository.ExistsForOrderAsync` before staging the task. The before twin keeps its conventional `Id` + `OrderId` entity (slice 003 design), so an identity upsert is not available the way it is on the after twin; a read-before-write is the earnest EF-idiom answer, and its race window is acceptable for a demo that redelivers only on consumer failure. Alternative considered: a unique index on `OrderId` plus catching the violation; rejected as a new migration and an exception-as-flow path the talk does not need.

### Before twin: message name keeps the `Message` suffix

`NotifyBakerMessage(Guid OrderId, string Summary)` carries the same information as the after twin's `NotifyBaker`. The `Message` suffix is the before twin's idiom and stays; CLAUDE.md's no-role-suffix rule applies to the after twin only.

### Before twin: connection and channel lifecycle

One `IConnection` per host, created lazily on first use, disposed with the host (singleton `RabbitMqConnection` in Infrastructure). Publisher opens a channel per publish and disposes it. The consumer owns one long-lived channel for the life of the hosted service. Both sides declare the queue with identical arguments (durable, non-exclusive, non-auto-delete, no arguments) so whichever starts first wins and the other side's declare is a no-op; RabbitMQ refuses an inequivalent redeclaration with a channel error, which is why the arguments are spelled out once in the options and used twice. A message published before the consumer subscribes waits in the queue; that is the normal case in the Alba fixture, where the first `POST` can beat the hosted service's subscription.

### After twin: `UseDurableOutbox` on the RabbitMQ publish rule, listen in-process

`opts.PublishMessage<NotifyBaker>().ToRabbitQueue("layercake-after-baker-tasks").UseDurableOutbox()` keeps the envelope in the same Marten transaction as the order (the claim on the outbox slide) and sends after commit; `opts.ListenToRabbitQueue("layercake-after-baker-tasks")` delivers it back to `NotifyBakerHandler` in the same process. `UseRabbitMq(...).AutoProvision()` moves out of the `CritterWatch:Enabled` block and runs unconditionally; `AddCritterWatchMonitoring` stays gated. `UseDurableLocalQueues()` is revisited at build time: kept only if any local message can still exist, otherwise removed, and either way the comment stops describing `NotifyBaker` as local. Verification: `dotnet run -- describe` must report the sending endpoint in Durable mode.

### Suite: one RabbitMQ container per twin collection

`Testcontainers.RabbitMq` at its latest stable, image `rabbitmq:4` (same major as compose). A `RabbitMqContainerFixture` per twin collection as a second `ICollectionFixture<>`, mirroring the 2026-09-02 Postgres pattern, so `dotnet test` still needs only Docker and the two twins never share a broker either. Both host fixtures pass the container's connection string as `ConnectionStrings:RabbitMq`; configuration keys are case-insensitive, so the after twin's lowercase read matches. `AfterHostFixture` drops `DisableAllExternalWolverineTransports()` (it would swap the RabbitMQ sender for a null sender and make the proof vacuous) and keeps `RunWolverineInSoloMode()`. Alternative considered: one shared container for both collections with the two queue names keeping them apart; rejected because per-collection containers are the established pattern and the second container is cheap.

### Package additions, never bumps

`RabbitMQ.Client` pinned at 7.2.2, which is the exact version `WolverineFx.RabbitMQ` 6.30.0 already depends on, so the solution carries one copy. `Testcontainers.RabbitMq` pinned at 4.14.0, matching `Testcontainers.PostgreSql`. Both recorded with a dated comment in `Directory.Packages.props`; no existing pin moves.

## Risks / Trade-offs

- [Container startup adds seconds to the timed live `dotnet test` moment] → Record wall-clock before and after; the RabbitMQ containers start in parallel with Postgres per collection. Poll constants stay 250 ms / 5 s unless a cold run proves slower, in which case only the timeout rises and the build log says why.
- [The before twin's first `POST` can beat the consumer's subscription] → Durable queue, persistent delivery, message waits; both sides declare, so the queue exists before the first publish regardless of start order.
- [Inequivalent queue redeclaration fails the channel] → Arguments defined once in `RabbitMqOptions`, used by publisher and consumer; on the after twin Wolverine owns both sides of its queue.
- [`DisableAllExternalWolverineTransports` removed, so the after host now needs a reachable broker in every test class] → That is the point: with no broker the scenario fails, and the negative check in verification proves it can.
- [Dual-write gap in the before twin] → Owned, not mitigated. One comment in code, one sentence on a slide.

## Open Questions

None. The negative check (stop the broker or comment out the listener, confirm the scenario fails, revert) runs once during verification and is recorded in the build log.
