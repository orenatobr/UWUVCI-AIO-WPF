using UWUVCI.Core.N64;

namespace UWUVCI.Core.Tests.N64;

public class N64GameDatabaseTests
{
    [Theory]
    [InlineData(0xC2E9AA9Au, 0x475D70AAu, "Banjo-Tooie", SaveKind.Eeprom16K)]
    [InlineData(0x30C7AC50u, 0x7704072Du, "Conker's Bad Fur Day", SaveKind.Eeprom16K)]
    [InlineData(0x8A6009B6u, 0x94ACE150u, "Jet Force Gemini", SaveKind.Flash)]
    [InlineData(0xDCBC50D1u, 0x09FD1AA3u, "GoldenEye 007", SaveKind.Eeprom4K)]
    [InlineData(0x49088A11u, 0x6494957Eu, "Turok 2", SaveKind.None)]
    public void Find_KnownGames(uint c1, uint c2, string namePart, SaveKind save)
    {
        var g = N64GameDatabase.Find(c1, c2);
        Assert.NotNull(g);
        Assert.Contains(namePart, g!.Name);
        Assert.Equal(save, g.Save);
    }

    [Fact]
    public void Find_Unknown_ReturnsNull() => Assert.Null(N64GameDatabase.Find(1, 2));

    [Fact]
    public void Count_IsPlausible() => Assert.InRange(N64GameDatabase.Count, 1000, 5000);
}
