using System.Security.Claims;
using invmgmt.web.Utils;
using Xunit;

namespace invmgmt.web.Tests.Utils;

public class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void GetUserId_ReturnsId_WhenClaimExists()
    {
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(new[] { new Claim("UserId", "123") }, "test"));

        Assert.Equal(123, principal.GetUserId());
    }

    [Fact]
    public void GetUserId_Throws_WhenClaimMissing()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        Assert.Throws<InvalidOperationException>(() => principal.GetUserId());
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-int")]
    public void GetUserId_Throws_WhenClaimInvalid(string raw)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("UserId", raw) }, "test"));
        Assert.Throws<InvalidOperationException>(() => principal.GetUserId());
    }
}

