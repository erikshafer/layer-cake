# C# Language Level, Program.cs Shape, and Formatting

Rule strength: MUST / SHOULD / OBSERVED, as defined in `naming-and-types.md`. Source flavor tags [APP] / [DOC] / [TXPT] as defined there.

## 7. C# language level and syntax

**MUST: file-scoped namespaces (`namespace Foo;`).** Zero curly-brace namespace blocks found in any application code file across either repository.

**MUST: `Program.cs` is top-level statements.** No `class Program { static void Main() }` boilerplate anywhere. (`IncidentService/Program.cs` does add `public partial class Program {}` at the very end, purely to give integration tests a type to reference; this does not reintroduce a `Main` method.)

**MUST: `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>`.** Confirmed directly in `BankAccountES.csproj` and `ContributorApi.csproj`; no `.cs` file anywhere opens with `using System;`, `using System.Collections.Generic;`, etc.

**MUST: `var` for locals whenever the type is apparent from the right-hand side.** `var builder = WebApplication.CreateBuilder(args);`, `var connectionString = ...`, `var order = new Order { ... };`. Universal across every file read; also matches the target repo's own `.editorconfig` (`csharp_style_var_when_type_is_apparent = true:warning`).

**SHOULD: collection expressions (`[]`) for empty list-typed property defaults**, not `new List<T>()`. `public List<ShoppingCartItem> Items { get; set; } = [];`, `public List<OrderItem> Items { get; set; } = [];`, `public List<TodoItem> Items { get; set; } = [];`, `public List<Transaction> Transactions { get; set; } = [];`.

**SHOULD: expression-bodied members for one-line pass-through methods**, especially query/projection endpoint methods:
```csharp
[WolverineGet("/api/contributors")]
public static Task<IReadOnlyList<Contributor>> Get(IQuerySession session, CancellationToken ct)
    => session.Query<Contributor>().OrderBy(c => c.Name).ToListAsync(ct);
```
Citations: `GetContributorsEndpoint`, `TodoEndpoints.Get`, `TodoEndpoints.GetTodo`, `PhoneNumber.ToString()`, `TodoColours.IsValid`. This also matches the target repo's `.editorconfig` (`csharp_style_expression_bodied_properties/indexers/accessors = true`; note methods and constructors are left `false:silent` there, so keep expression bodies to properties, single-expression query methods, and simple accessors, not to multi-statement handler logic).

**MUST: nullable annotations (`string?`, `Guid?`, `DateTime?`) on genuinely optional data**, paired with `= string.Empty` (or another concrete default) on data that is not optional. See `naming-and-types.md` section 2 for the full rule and the `= null!` variation.

**Not codified:** target-typed `new()` is not a consistent pattern; object initializers consistently spell out the type name even on a `var`-declared local.

## 8. Program.cs / bootstrapping shape

**MUST: the same nine-step skeleton appears, in the same order, in essentially every HTTP-hosting sample across both repositories** (9+ direct citations: `BankAccountES`, `ContributorApi`, `CqrsMinimalApi`, `EcommerceModularMonolith`, `OutboxDemo`, `IncidentService`, `TodoWebService`, `Quickstart`, `OrderSagaSample`). Write new `Program.cs` files in this shape:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddWolverineHttp();

builder.Services.AddMarten(opts =>
{
    var connectionString = builder.Configuration.GetConnectionString("Marten")
        ?? "Host=localhost;Port=5433;Database=my_service;Username=postgres;Password=postgres";

    opts.Connection(connectionString);
    opts.DatabaseSchemaName = "my_service";
})
.IntegrateWithWolverine()
.UseLightweightSessions();

builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(Program).Assembly);
    opts.Policies.AutoApplyTransactions();
    opts.ServiceName = "MyService";
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapWolverineEndpoints();

await app.RunAsync();
```

**SHOULD: the `AddMarten` connection-string lambda falls back to a hardcoded local default** with `builder.Configuration.GetConnectionString("Marten") ?? "Host=localhost;Port=5433;..."`, using port 5433 (not the Postgres default 5432, since the samples' shared `docker-compose.yml` remaps it). Citations: `BankAccountES`, `ContributorApi`, `CqrsMinimalApi`, `OutboxDemo`, all with this exact literal fallback pattern.

**SHOULD: `opts.Discovery.IncludeAssembly(typeof(Program).Assembly);` is stated explicitly inside `UseWolverine`** even in single-project apps where discovery would likely find handlers anyway. Citations: `BankAccountES`, `ContributorApi`, `CqrsMinimalApi`, `EcommerceModularMonolith`.

**OBSERVED, real split, pick one per project rather than mixing: the terminal call is either `await app.RunAsync();` or `return await app.RunJasperFxCommands(args);`.** Roughly half the samples use each. `RunJasperFxCommands` is a strict superset (it still runs the web host with no arguments, and additionally unlocks `codegen write`, database migration, and diagnostic CLI commands), so prefer it as the default for any service expected to grow command-line tooling; fall back to plain `RunAsync` only for the simplest demo-style hosts. Citations for `RunJasperFxCommands`: `Quickstart`, `TodoWebService`, `IncidentService`, `OrderSagaSample`, `CqrsMinimalApi`. Citations for plain `RunAsync`: `BankAccountES`, `ContributorApi`, `OutboxDemo`, `EcommerceModularMonolith`.

**SHOULD: `IntegrateWithWolverine()` immediately followed by `.UseLightweightSessions()` is the canonical Marten-plus-Wolverine wiring**, called out directly in the CritterStackSamples README as a common pattern across every sample there.

## 9. Formatting and layout

**MUST: Allman brace style (opening brace on its own line).** Universal, zero exceptions, and matches the target repo's `.editorconfig` (`csharp_new_line_before_open_brace = all`).

**MUST: within a "one file per command" file, the command record (with its nested `Validator`) comes first, the static `...Endpoint` class comes second.** Confirmed in every file cited in `naming-and-types.md` section 1.

**SHOULD: trailing comma after the last member in a multi-line object initializer.**
```csharp
var contributor = new Contributor
{
    Name = command.Name,
    CreatedAt = DateTimeOffset.UtcNow,
};
```
Citations: `Contributor`, `Order` (Ordering), `TodoList.Created`-style initializers, `Registration`.

**Do not use `#region` / `#endregion` markers in application code.** They appear pervasively in the Wolverine repo's own `DocumentationSamples`, `Quickstart`, `TodoWebService`, `IncidentService`, `OrderSagaSample`, and `WebApiWithMarten` [DOC], where they exist specifically so the Wolverine documentation site can pull named code snippets into published docs pages. They appear in zero files across the entire CritterStackSamples [APP] set. Treat region markers as a docs-tooling convention, not a code style to imitate.

**General blank-line convention (not independently rule-worthy, but consistent throughout): one blank line separates the `using` block from the `namespace` line, and one blank line separates the namespace declaration from the first type**, with no blank line at the very top or bottom of the file.
