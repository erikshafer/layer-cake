using Alba;
using Shouldly;

namespace LayerCake.ContractTests;

/// <summary>
/// The error-shape parity rule, in one place: exact status code,
/// application/problem+json content type, and the offending field or reason
/// discoverable in the body. Error BODIES are deliberately not byte-identical
/// across the twins; this helper is the contract they both answer.
/// </summary>
public static class ProblemDetailsAssertions
{
    public static async Task ShouldBeProblem(this IScenarioResult result, int expectedStatus, string expectedReason)
    {
        result.Context.Response.StatusCode.ShouldBe(expectedStatus);

        result.Context.Response.ContentType.ShouldNotBeNull();
        result.Context.Response.ContentType.ShouldStartWith("application/problem+json");

        var body = await result.ReadAsTextAsync();
        body.ShouldContain(expectedReason, Case.Insensitive);
    }
}
