# Proposal: slice-004-notify-baker-over-rabbitmq

## Why

Slice 003 left "tell the baker" as a cascaded outbox message on the after twin and an inline row write on the before twin, and the talk asks the room to imagine that inline line is a call to an external system. This slice stops asking: the baker notification crosses a real RabbitMQ broker on BOTH twins, so the talk can show what one message costs a layered codebase against what it costs a slice. Full rationale, settled decisions, and structure: [docs/slices/004-notify-baker-over-rabbitmq.md](../../../docs/slices/004-notify-baker-over-rabbitmq.md). Decided by Erik 2026-09-05; the slate's three-slice lock stands for the talk's Act 3 features, and 004 is an extension of PlaceOrder's side effect.

## What Changes

- Before twin: a messaging port in Application (`IMessagePublisher`), a `NotifyBakerMessage`, a `CreateBakerTask` command + handler as the consumer's re-dispatch target, a raw `RabbitMQ.Client` publisher (adapter) plus connection and options in Infrastructure, a `BackgroundService` consumer in WebApi, and `PlaceOrderCommandHandler` swapping the inline baker-task write for a publish AFTER the commit (no outbox; the dual-write gap is owned in one comment).
- After twin: `Program.cs` only. RabbitMQ transport wired unconditionally, `NotifyBaker` published to a RabbitMQ queue with the durable outbox, the same queue listened to in-process. `PlaceOrder.cs` and `NotifyBaker.cs` untouched.
- Shared contract suite: fixture changes only. One RabbitMQ container per twin collection through `Testcontainers.RabbitMq`, connection string passed to both hosts, the after fixture stops stubbing external transports. Scenarios and assertions unchanged.
- Two package ADDITIONS at Erik's explicit call, never bumps: `RabbitMQ.Client` (before twin) and `Testcontainers.RabbitMq` (suite). Existing pins untouched.
- Bookkeeping: slice 004 section in `docs/file-inventory.md` with the whole-twin recount, dated `docs/build-log.md` entry, README / CLAUDE.md / `openspec/config.yaml` / `docker-compose.yml` wording updated so nothing still claims the suite is broker-free.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `orders`: the "Placing an order produces exactly one baker task" requirement now states that the notification is delivered through a RabbitMQ queue in both twins before the task is written. Scenario list unchanged; HTTP surface unchanged.

## Impact

- `src/before/`: all four projects touched (Application port + message + command, Infrastructure adapter + DI + csproj, WebApi hosted consumer + Program + appsettings, Domain unchanged). `PlaceOrderCommandHandler` loses `IBakerTaskRepository`, gains `IMessagePublisher`.
- `src/after/LayerCake.Slices/Program.cs` and `appsettings.json` only.
- `tests/LayerCake.ContractTests/TwinHosts.cs` and its csproj; `Directory.Packages.props` gains two entries.
- `dotnet test` now starts PostgreSQL AND RabbitMQ per twin through Testcontainers; Docker remains the only prerequisite. The live `dotnet test` moment in the talk is timed, so the before/after wall-clock is recorded.
- Docs: README, CLAUDE.md, `openspec/config.yaml`, `docker-compose.yml` comment, file inventory, build log.

## Non-goals

- No outbox, retry loop, or dead-letter exchange in the before twin: save through the unit of work, then publish. The gap is a slide, not a fix.
- No poison handling beyond log-and-nack in the before twin's consumer.
- No second message on either twin; no MassTransit, Rebus, or NServiceBus.
- No change to routes, JSON shapes, status codes, seeds, the frontend page, the CritterWatch console, or `docs/slices/001-003`.
- No edit to `PlaceOrder.cs` or `NotifyBaker.cs` on the after twin, even for tidiness; that would be a talk-content decision for Erik.
- No second deployable per twin; each twin's consumer runs inside its own host process (charter non-negotiable 3).
