using System.Text.Json;
using Alba;
using Shouldly;
using Xunit;

namespace LayerCake.ContractTests;

/// <summary>
/// Slice 002 contract: GET /coupons/{code}, always 200 with the four-status
/// discriminated envelope. Written once, run against both twins by the
/// sealed subclasses at the bottom. Each class run starts from the freshly
/// seeded coupon book: BDAY10 (valid), SUMMER25 (expired), HOLIDAY30
/// (notYetActive); see TwinHosts.cs.
/// </summary>
public abstract class CouponScenarios
{
    protected abstract IAlbaHost Host { get; }

    private static readonly JsonSerializerOptions Json = JsonSerializerOptions.Web;

    private sealed record CouponEnvelope(string Code, string Status, int? PercentOff);

    private async Task<(CouponEnvelope Envelope, string RawBody)> ValidateAsync(string code)
    {
        var result = await Host.Scenario(s =>
        {
            s.Get.Url($"/coupons/{code}");
            s.StatusCodeShouldBe(200);
        });

        var body = await result.ReadAsTextAsync();

        // Deserialization with Web options is case-insensitive, so pin the
        // camelCase keys against the raw body too.
        body.ShouldContain("\"code\"");
        body.ShouldContain("\"status\"");

        return (JsonSerializer.Deserialize<CouponEnvelope>(body, Json)!, body);
    }

    [Fact]
    public async Task valid_coupon_returns_valid_with_percent_off()
    {
        var (envelope, body) = await ValidateAsync("BDAY10");

        envelope.Code.ShouldBe("BDAY10");
        envelope.Status.ShouldBe("valid");
        envelope.PercentOff.ShouldBe(10);
        body.ShouldContain("\"percentOff\"");
    }

    [Fact]
    public async Task unknown_coupon_returns_invalid()
    {
        var (envelope, body) = await ValidateAsync("DOESNOTEXIST");

        envelope.Code.ShouldBe("DOESNOTEXIST");
        envelope.Status.ShouldBe("invalid");
        body.ShouldNotContain("percentOff");
    }

    [Fact]
    public async Task not_yet_active_coupon_returns_notYetActive()
    {
        var (envelope, body) = await ValidateAsync("HOLIDAY30");

        envelope.Code.ShouldBe("HOLIDAY30");
        envelope.Status.ShouldBe("notYetActive");
        body.ShouldNotContain("percentOff");
    }

    [Fact]
    public async Task expired_coupon_returns_expired()
    {
        var (envelope, body) = await ValidateAsync("SUMMER25");

        envelope.Code.ShouldBe("SUMMER25");
        envelope.Status.ShouldBe("expired");
        body.ShouldNotContain("percentOff");
    }

    [Fact]
    public async Task coupon_lookup_is_case_insensitive()
    {
        // Lowercase in, canonical uppercase out.
        var (envelope, _) = await ValidateAsync("bday10");

        envelope.Code.ShouldBe("BDAY10");
        envelope.Status.ShouldBe("valid");
        envelope.PercentOff.ShouldBe(10);
    }

    [Fact]
    public async Task percent_off_absent_unless_valid()
    {
        // Absent means the key never appears, not "percentOff": null.
        foreach (var code in new[] { "DOESNOTEXIST", "HOLIDAY30", "SUMMER25" })
        {
            var (_, body) = await ValidateAsync(code);

            body.ShouldNotContain("percentOff", customMessage: $"envelope for {code} should omit percentOff");
        }
    }
}

[Collection(BeforeTwinCollection.Name)]
public sealed class BeforeTwinCoupons : CouponScenarios, IClassFixture<BeforeHostFixture>
{
    private readonly BeforeHostFixture _fixture;

    public BeforeTwinCoupons(BeforeHostFixture fixture)
    {
        _fixture = fixture;
    }

    protected override IAlbaHost Host => _fixture.Host;
}

[Collection(AfterTwinCollection.Name)]
public sealed class AfterTwinCoupons : CouponScenarios, IClassFixture<AfterHostFixture>
{
    private readonly AfterHostFixture _fixture;

    public AfterTwinCoupons(AfterHostFixture fixture)
    {
        _fixture = fixture;
    }

    protected override IAlbaHost Host => _fixture.Host;
}
