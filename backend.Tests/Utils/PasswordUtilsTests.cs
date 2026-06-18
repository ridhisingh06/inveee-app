using invmgmt.web.Utils;
using Xunit;

namespace invmgmt.web.Tests.Utils;

public class PasswordUtilsTests
{
    [Theory]
    [InlineData("$2a$10$abcdefghijklmnopqrstuv", true)]
    [InlineData("$2b$10$abcdefghijklmnopqrstuv", true)]
    [InlineData("$2y$10$abcdefghijklmnopqrstuv", true)]
    [InlineData("plaintext", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void LooksLikeBcryptHash_Works(string? value, bool expected)
    {
        Assert.Equal(expected, PasswordUtils.LooksLikeBcryptHash(value));
    }

    [Fact]
    public void FixedTimeEquals_ReturnsTrueForSame()
    {
        Assert.True(PasswordUtils.FixedTimeEquals("abc", "abc"));
    }

    [Fact]
    public void FixedTimeEquals_ReturnsFalseForDifferent()
    {
        Assert.False(PasswordUtils.FixedTimeEquals("abc", "abd"));
    }

    [Fact]
    public void FixedTimeEquals_ReturnsFalseForDifferentLength()
    {
        Assert.False(PasswordUtils.FixedTimeEquals("abc", "ab"));
    }
}

