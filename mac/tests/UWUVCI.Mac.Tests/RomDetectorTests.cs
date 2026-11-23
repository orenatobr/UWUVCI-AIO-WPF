using UWUVCI.Mac.Services;

namespace UWUVCI.Mac.Tests;

public class RomDetectorTests
{
    [Fact]
    public void Detect_KnownGame_UsesDatabaseName()
    {
        using var t = new TempDir();
        var d = RomDetector.Detect(t.File("bt.z64", TestData.BanjoTooie()), out var error);
        Assert.Null(error);
        Assert.NotNull(d);
        Assert.Contains("Banjo-Tooie", d!.Name);
        Assert.True(d.InDatabase);
        Assert.Contains("TrueBoot = 1", d.BuildIni(null));
    }

    [Fact]
    public void Detect_UnknownGame_FallsBackToHeaderTitle()
    {
        using var t = new TempDir();
        var d = RomDetector.Detect(t.File("x.z64", TestData.Rom(7, 8, "MY HOMEBREW")), out _);
        Assert.Equal("MY HOMEBREW", d!.Name);
        Assert.False(d.InDatabase);
    }

    [Fact]
    public void Detect_ExpansionPakOverride_ChangesIni()
    {
        using var t = new TempDir();
        var d = RomDetector.Detect(t.File("x.z64", TestData.Rom(7, 8)), out _)!;
        Assert.DoesNotContain("RamSize", d.BuildIni(null));
        Assert.Contains("RamSize", d.BuildIni(true));
        Assert.True(d.Profile(true).ExpansionPak);
    }

    [Fact]
    public void Detect_NotARom_ReportsError()
    {
        using var t = new TempDir();
        var d = RomDetector.Detect(t.File("x.z64", new byte[0x100]), out var error);
        Assert.Null(d);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void Detect_ByteSwappedRom_IsAccepted()
    {
        using var t = new TempDir();
        var z = TestData.BanjoTooie();
        for (int i = 0; i < z.Length; i += 2) (z[i], z[i + 1]) = (z[i + 1], z[i]);
        var d = RomDetector.Detect(t.File("x.v64", z), out _);
        Assert.Contains("Banjo-Tooie", d!.Name);
    }

    [Fact]
    public void Detect_WrongExtension_ReportsError()
    {
        using var t = new TempDir();
        Assert.Null(RomDetector.Detect(t.File("x.zip", TestData.BanjoTooie()), out var error));
        Assert.NotNull(error);
    }
}
