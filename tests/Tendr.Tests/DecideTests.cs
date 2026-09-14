using Shouldly;
using Tendr.Authorizations;
using Xunit;

namespace Tendr.Tests;

/// <summary>
/// The test-card table is a pure function, so it is tested as one.
/// </summary>
public class DecideTests
{
    [Theory]
    [InlineData("4242424242424242", null)]
    [InlineData("4242 4242 4242 4242", null)]
    [InlineData("4000 0000 0000 0002", "card_declined")]
    [InlineData("4000000000009995", "insufficient_funds")]
    [InlineData("4111 1111 1111 1111", "unknown_card")]
    [InlineData("", "unknown_card")]
    public void decide_reads_the_test_card_table(string cardNumber, string? expectedReason)
    {
        AuthorizeCardEndpoint.Decide(cardNumber).ShouldBe(expectedReason);
    }
}
