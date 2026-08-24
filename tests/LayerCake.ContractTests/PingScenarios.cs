using Alba;
using Shouldly;
using Xunit;

namespace LayerCake.ContractTests;

/// <summary>
/// The pattern every contract scenario in this repo follows: scenarios are
/// written ONCE in an abstract base class, and the two sealed subclasses at
/// the bottom run the identical set against each twin. When `dotnet test`
/// is green, both architectures have answered the same contract.
///
/// Contract discipline: assert EXACT status codes (never a 2xx range) and
/// always send explicit Content-Type headers on requests with bodies.
/// </summary>
public abstract class PingScenarios
{
    protected abstract IAlbaHost Host { get; }

    [Fact]
    public async Task ping_returns_200_pong()
    {
        var result = await Host.Scenario(s =>
        {
            s.Get.Url("/ping");
            s.StatusCodeShouldBe(200);
        });

        var body = await result.ReadAsTextAsync();
        body.ShouldBe("pong");
    }
}

[Collection(BeforeTwinCollection.Name)]
public sealed class BeforeTwinPing : PingScenarios, IClassFixture<BeforeHostFixture>
{
    private readonly BeforeHostFixture _fixture;

    public BeforeTwinPing(BeforeHostFixture fixture)
    {
        _fixture = fixture;
    }

    protected override IAlbaHost Host => _fixture.Host;
}

[Collection(AfterTwinCollection.Name)]
public sealed class AfterTwinPing : PingScenarios, IClassFixture<AfterHostFixture>
{
    private readonly AfterHostFixture _fixture;

    public AfterTwinPing(AfterHostFixture fixture)
    {
        _fixture = fixture;
    }

    protected override IAlbaHost Host => _fixture.Host;
}
