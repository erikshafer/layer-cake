# Comment Tone and Placement

Rule strength: MUST / SHOULD / OBSERVED, as defined in `naming-and-types.md`. Source flavor tags [APP] / [DOC] / [TXPT] as defined there.

This dimension needed the most care to separate signal from noise. `DocumentationSamples`, `Quickstart`, `OrderSagaSample`, and the `TeleHealth` (`CQRSWithMarten`) samples are comment-dense because the comments are the documentation; that density is a property of the docs pipeline (see the `#region` note in `bootstrapping-and-formatting.md`), not a production code style. What follows extracts the recurring *tone and placement* from both flavors, and states a lighter, production-appropriate version.

## 10. Comment tone and placement

**SHOULD: a short `/// <summary>` on aggregate/document/saga classes explaining the type's role**, most often one to three sentences, placed directly above the class. Citations: `Account`, `TransactionHistory.cs` (`AccountTransactions`, `AccountTransactionsProjection`), `Contributor`, `TodoList`, `BasketCheckoutEvent`, `BasketCheckoutEventHandler`, `Order` (Ordering).

```csharp
/// <summary>
/// Contributor document stored in Marten.
/// </summary>
public class Contributor
{
    ...
}
```

Context-specific pattern, do not carry forward verbatim: several of these summaries in CritterStackSamples also state what the type *replaces* from an original ported codebase ("Replaces MassTransit's `IConsumer` + MediatR round trip...", "Replaces: Contributor aggregate + Vogen code generation..."). That framing exists because those samples are explicitly before/after migration ports (see the CritterStackSamples README's stated purpose). New, non-port application code should keep the "what this type is for" sentence and drop the "replaces X" framing, since there is no "before" to reference.

**OBSERVED variant, worth knowing: a more formal `/// <summary>` with `<see cref="..."/>` cross-references, used specifically to explain a subtle framework interaction decision.** Found once during the verification pass, in `ProcessManagerViaHandlers/OrderFulfillment/Handlers/StartOrderFulfillmentHandler.cs`, explaining why that handler is a plain handler rather than `[AggregateHandler]` (because `[AggregateHandler]`'s default `OnMissing` behavior short-circuits on a not-yet-existing stream). This is heavier and more technical than the dominant casual voice described below; it is appropriate specifically when documenting a "why did we deliberately not use the usual attribute here" decision, not as a general-purpose comment style.

**MUST: inline `//` comments are placed directly above the line they explain, and explain Wolverine- or Marten-specific mechanics (why this shape, what happens next, what's non-obvious), never a restatement of what the code already says.**
```csharp
// Cascade: first element is HTTP response (bool), second is the integration event
// published via Wolverine outbox to be consumed by the Ordering service.
// After publishing, delete the basket.
[WolverinePost("/basket/checkout")]
public static async Task<(bool, BasketCheckoutEvent)> Post(...)
```
```csharp
// [Entity] loads the TodoList by the "ListId" property on the request
[WolverinePost("/api/todoitems")]
public static TodoItem Post(...)
```
```csharp
// Sad path: insufficient funds check using the loaded aggregate
public static ProblemDetails Validate(WithdrawFunds command, Account account)
```
This is extremely consistent (10+ citations) across nearly every file surveyed and is the single most transferable convention in this dimension.

**Voice: plain, second-person-adjacent, conversational, not formal enterprise Javadoc**, even inside `///` summaries. Representative phrasing from the samples: "Watch this syntax.", "it's all in memory, but just let this go...", "No soup for you!", "Just going to code this one pretty crudely". For production code, keep the plain, unpretentious register (short sentences, contractions are fine, explain the mechanic like you're telling a teammate) but drop first-person asides and jokes; aim for the tone of the `//` mechanics comments above, not the jokier teaching-sample asides. Reserve the more formal `<see cref>` style above for the rarer case of explaining a deliberate deviation from the usual attribute/idiom.

**Teaching-sample density (do not copy): near-line-by-line comments explaining every parameter and every branch.** This is appropriate in `DocumentationSamples`, `Quickstart`, `OrderSagaSample`, and `CQRSWithMarten` because those files are consumed as prose by documentation readers, not just as code. For production application code, comment only:
- non-obvious Wolverine/Marten mechanics (cascading messages, outbox timing, `[Entity]`/`[Aggregate]` behavior, middleware ordering),
- the "why" behind a business rule, not the "what" the code visibly does,
- the one-to-three-sentence type-level summary described above.

Leave straightforward CRUD bodies (`session.Store(x); return x;`) uncommented, exactly as the [APP] samples do; not one of `ContributorApi`'s or `BankAccountES`'s straightforward create/update/delete bodies carries an inline comment.
