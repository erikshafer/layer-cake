# Proposal: slice-005-pay-with-tendr

## Why

Every KCDC audience asked where a call to an outside service goes in a slice and what it costs each twin, and the talk's "keep the port if you have real vendor churn" steelman had no code behind it. This slice puts a card payment on PlaceOrder's load leg in both twins, against a real vendor host on a real socket. Full rationale, contract, required structure, and scenario list: [docs/slices/005-pay-with-tendr.md](../../../docs/slices/005-pay-with-tendr.md). Decided by Erik 2026-09-14 for the talk's v1.1; the demo-readiness freeze in `CLAUDE.md` is lifted for this slice only.

## What Changes

- New host `src/tendr/Tendr` (in `LayerCake.slnx` under `/tendr/`, port 42040): a fake card vendor on Wolverine.Http with Marten's event store in schema `tendr`. Idempotent `POST /v1/authorizations`, `GET /v1/authorizations/{id}`, `GET /ping`, a four-entry test-card table. Not a twin and not in the scorecard.
- Before twin: an `IPaymentGateway` port and a `PaymentAuthorization` outcome in Application, a `TendrPaymentGateway` typed-client adapter with options and wire shapes in Infrastructure, two exceptions mapped to `402` and `503` in the WebApi filter, payment columns on `Order` with a generated migration, and the handler awaiting the port after its decide block.
- After twin: one new file `Payments/Tendr.cs` (typed client, wire records, `Payment`), `AddHttpClient<TendrClient>` in `Program.cs`, payment columns on `Order`, and in `PlaceOrder.cs` a new `AuthorizeAsync` rung plus a `Validate(Payment?)` guard between the existing guards and `Post`. `Decide`, the three existing guards, `NotifyBaker.cs`, and the outbox line do not change.
- Shared contract suite: a Tendr host per twin collection on Kestrel against the collection's PostgreSQL container (no new container); new `PaymentScenarios` and `PaymentOutageScenarios` classes bound to both twins. Every existing scenario is unchanged.
- New test project `tests/Tendr.Tests` (in the slnx) covering Tendr's own API; new unit facts for guard four in `tests/LayerCake.Slices.Tests`.
- No package additions or bumps. The before twin's Infrastructure project gains a `FrameworkReference` to the ASP.NET Core shared framework for `Microsoft.Extensions.Http`, which is not a package.
- Bookkeeping: slice 005 row and whole-twin recount in `docs/file-inventory.md`, a dated `docs/build-log.md` entry, README and CLAUDE.md gain Tendr, `docs/frontend.md` and the demo page gain a card select, `.gitignore` ignores the after twin's codegen output.

## Capabilities

### New Capabilities

None. Tendr is test infrastructure and a demo prop, not a twin capability; its contract lives in the slice spec and its README and is proven by `tests/Tendr.Tests`.

### Modified Capabilities

- `orders`: `POST /orders` accepts an optional `card` and returns a `payment` member only when one was sent; the ordered guard chain gains a fourth guard (`402`, card declined); a new requirement covers the vendor being unreachable (`503`, no order); `GET /orders/{id}` mirrors the payment.

## Impact

- `src/before/`: Domain (enum, entity), Application (port, outcome, request shape, two exceptions, DTO, mapping, command, handler), Infrastructure (adapter, options, wire shapes, configuration, DI, migration, csproj), WebApi (filter, controller, appsettings). The handler goes from seven injected dependencies to eight.
- `src/after/LayerCake.Slices/`: `Payments/Tendr.cs` created; `Orders/PlaceOrder.cs`, `Orders/Order.cs`, `Program.cs`, `appsettings.json` edited.
- `src/tendr/Tendr/`: new project, README, launch settings.
- `tests/`: `LayerCake.ContractTests` (fixtures and two new scenario files, project reference to Tendr), `LayerCake.Slices.Tests` (guard-four facts), `Tendr.Tests` (new). The Marten and template experiments keep their bindings and their counts.
- `dotnet test` on the slnx gains a Tendr host per twin collection, the payment scenarios, one extra host boot per twin for the outage class, and the Tendr test project.
- Docs: slice spec, OpenSpec `orders` spec on archive, file inventory, build log, README, CLAUDE.md, frontend spec.

## Non-goals

- Capture, refund, void, webhooks, a payment status lifecycle, or card validation beyond Tendr's test-card table.
- Storing or logging card data in either twin.
- Retries, circuit breakers, Polly, or any resilience package; the two-second timeout is the whole story on both twins.
- Any change to the no-card path of `POST /orders`, to `Decide`, to the three existing guards, to `NotifyBaker.cs`, or to the outbox wiring.
- A docker-compose service for Tendr; it is a .NET host, run with `dotnet run`.
- Event sourcing in either twin (charter non-negotiable 3); only Tendr is event-sourced.
- The Wolverine HTTP transport in either twin. It is an optional, fenced, Tendr-only follow-up commit, attempted only after everything else is green.
