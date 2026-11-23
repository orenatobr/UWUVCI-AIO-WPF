using UWUVCI.Core.N64;

namespace UWUVCI.Core.Tests.N64;

public class N64BasesTests
{
    [Fact]
    public void All_LoadsTwentyThreeBases()
    {
        Assert.Equal(23, N64Bases.All.Count);
        Assert.All(N64Bases.All, b => Assert.Equal(16, b.TitleId.Length));
    }

    [Fact]
    public void Find_MarioGolfUs()
    {
        var b = N64Bases.Find("00050000101A5900");
        Assert.NotNull(b);
        Assert.Equal("Mario Golf", b!.Name);
        Assert.Equal("US", b.Region);
        Assert.Equal("00050000101a5900", b.TitleId);
        Assert.Null(N64Bases.Find("nope"));
    }

    // Values computed independently with a Python port of the original GetDeterministicHashCode.
    [Theory]
    [InlineData("", 757602046)]
    [InlineData("a", -842352707)]
    [InlineData("ab", 938310215)]
    [InlineData("0123456789abcdef0123456789abcdef", -1576894338)]
    [InlineData("00000000000000000000000000000000", -1245272322)]
    public void Hash_MatchesKnownValues(string text, int expected) =>
        Assert.Equal(expected, TitleKey.Hash(text));

    [Fact]
    public void IsValid_AcceptsMatchingKeyCaseInsensitively()
    {
        var b = new N64Base("Test", "US", "0005000010000000", -1576894338);
        Assert.True(TitleKey.IsValid(b, "0123456789abcdef0123456789abcdef"));
        Assert.True(TitleKey.IsValid(b, "0123456789ABCDEF0123456789ABCDEF"));
    }

    [Theory]
    [InlineData("00000000000000000000000000000000")]
    [InlineData("0123456789abcdef")]
    [InlineData("")]
    [InlineData("zz23456789abcdef0123456789abcdef")]
    public void IsValid_RejectsWrongOrMalformed(string key)
    {
        var b = new N64Base("Test", "US", "0005000010000000", -1576894338);
        Assert.False(TitleKey.IsValid(b, key));
    }

    [Fact]
    public void Base_ToString_HasNoKeyHash() =>
        Assert.DoesNotContain("1576894338", new N64Base("T", "US", "0005000010000000", -1576894338).DisplayName);
}
