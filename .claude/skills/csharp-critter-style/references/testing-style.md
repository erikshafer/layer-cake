# Testing Style (Reference Page, Not a Testing Strategy Guide)

This is a brief style reference only, per scope. It covers naming and shape, not test strategy, coverage, or what to test. Rule strength: MUST / SHOULD / OBSERVED, as defined in `naming-and-types.md`.

## 11. Naming of tests and test style

**MUST: xUnit `[Fact]` + Shouldly assertions (`.ShouldBe`, `.ShouldNotBeNull`, `.ShouldBeTrue`) + Alba (`IAlbaHost`, `.Scenario(...)`) for HTTP integration tests.** This is the standard stack in every CritterStackSamples project with tests (`BankAccountES`, `ContributorApi`, `CqrsMinimalApi`, `EcommerceModularMonolith`), and is called out directly as a common pattern in the CritterStackSamples README ("Alba + Shouldly, integration tests with `CleanAllMartenDataAsync()` for test isolation").

**MUST: test classes implement `IAsyncLifetime`, spin up `AlbaHost.For<Program>()` in `InitializeAsync`, and call `await _host.CleanAllMartenDataAsync();` for test isolation before each test class runs.**
```csharp
public class ContributorTests : IAsyncLifetime
{
    private IAlbaHost _host = null!;

    public async Task InitializeAsync()
    {
        _host = await AlbaHost.For<Program>();
        await _host.CleanAllMartenDataAsync();
    }

    public async Task DisposeAsync() => await _host.DisposeAsync();
}
```
Citations: `BankAccountES.Tests`, `ContributorApi.Tests`, `CqrsMinimalApi.Tests`.

**SHOULD: exercise the API through `_host.Scenario(x => { x.Post.Json(...).ToUrl(...); x.StatusCodeShouldBe(...); })`, not through direct handler invocation**, so the test also covers routing, validation middleware, and serialization.

**OBSERVED, distinct family from the above, not to be mixed into the same project: the Wolverine repo's own samples (`IncidentService.Tests`, `ProcessManagerViaHandlers.Tests`) use a shared `IntegrationContext` / `AppFixture` base class plus xUnit collection fixtures, with lowercase snake_case test classes named `when_<scenario>` and lowercase snake_case (or terse) test method names:**
```csharp
public class when_logging_an_incident : IntegrationContext
{
    [Fact]
    public void unit_test() { ... }

    [Fact]
    public async Task happy_path_end_to_end() { ... }
}
```
This pattern adds more shared fixture machinery than the CritterStackSamples `IAsyncLifetime`-per-class approach. Both are valid; for a new repo, default to the simpler CritterStackSamples shape (`IAsyncLifetime` + `AlbaHost.For<Program>()` per test class) unless a shared, expensive-to-build fixture is genuinely needed across many test classes, in which case the `IntegrationContext`/fixture pattern is the fallback.

**OBSERVED, two casings both appear even within CritterStackSamples itself:**
- pure lower snake_case: `BankAccountES.Tests` (`enroll_client`, `open_account_with_invalid_client_returns_400`, `withdraw_insufficient_funds_returns_400`)
- PascalCase with underscores: `ContributorApi.Tests` (`Can_create_contributor`, `Get_by_id_returns_404_for_missing`)

Both read as full sentences describing the scenario and its expected outcome. Pick one casing per repository and hold it consistently; pure lower snake_case is slightly more common across the combined evidence (also matches the Wolverine repo's own `when_...` test classes) and is the marginally safer default when starting a new repo from scratch.
