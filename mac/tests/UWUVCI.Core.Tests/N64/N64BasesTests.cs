using UWUVCI.Core.N64;

namespace UWUVCI.Core.Tests.N64;

public class N64BasesTests
{
    [Fact]
    public void All_LoadsTheElevenCurrentBases()
    {
        Assert.Equal(11, N64Bases.All.Count);
        Assert.All(N64Bases.All, b => Assert.Equal(40, b.KeyHash.Length));
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

    // Computed independently with Python hashlib: sha1 of the lower case key text, upper case hex.
    [Theory]
    [InlineData("", "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709")]
    [InlineData("0123456789abcdef0123456789abcdef", "B1775A785F09A6EBAF2DC33D6EAEB98974D9CDB8")]
    [InlineData("00000000000000000000000000000000", "91D3D1F86024E49F0E35314258B357A8944068A1")]
    public void Sha1Hex_MatchesKnownValues(string text, string expected) =>
        Assert.Equal(expected, TitleKey.Sha1Hex(text));

    [Fact]
    public void IsValid_AcceptsMatchingKeyCaseInsensitively()
    {
        var b = new N64Base("Test", "US", "0005000010000000", "B1775A785F09A6EBAF2DC33D6EAEB98974D9CDB8");
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
        var b = new N64Base("Test", "US", "0005000010000000", "B1775A785F09A6EBAF2DC33D6EAEB98974D9CDB8");
        Assert.False(TitleKey.IsValid(b, key));
    }

    [Fact]
    public void Base_ToString_HasNoKeyHash() =>
        Assert.DoesNotContain("B1775A78", new N64Base("T", "US", "0005000010000000", "B1775A785F09A6EBAF2DC33D6EAEB98974D9CDB8").DisplayName);
}
