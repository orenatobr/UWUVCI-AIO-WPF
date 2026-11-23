using UWUVCI.Core.N64;

namespace UWUVCI.Core.Tests.N64;

public class VcIniTests
{
    [Fact]
    public void Build_Defaults_MatchesIniCreatorLayout()
    {
        var ini = VcIni.Build(new VcIniOptions("Test Game", "US"));
        Assert.Equal(";Test Game US\n[RomOption]\nBackupType = 0\nBackupSize = 512\n", ini);
    }

    [Fact]
    public void Build_WritesOptionalKeysInCreatorOrder()
    {
        var ini = VcIni.Build(new VcIniOptions("G", "EU")
        {
            BackupType = BackupType.Eeprom,
            BackupSize = "2048",
            RetraceByVsync = true,
            UseTimer = true,
            TrueBoot = true,
            ExpansionPak = true,
            Rumble = true,
        });
        Assert.Equal(
            ";G EU\n[RomOption]\nBackupType = 3\nBackupSize = 2048\nRetraceByVsync = 1\nUseTimer = 1\nTrueBoot = 1\nRamSize = 0x800000\nRumble = 1\n",
            ini);
    }

    [Fact]
    public void Build_RumbleWinsOverMemPack()
    {
        var ini = VcIni.Build(new VcIniOptions("G", "US") { Rumble = true, MemPack = true });
        Assert.Contains("Rumble = 1", ini);
        Assert.DoesNotContain("MemPack", ini);
    }

    [Fact]
    public void Build_MemPackAlone()
    {
        var ini = VcIni.Build(new VcIniOptions("G", "US") { MemPack = true });
        Assert.EndsWith("MemPack = 1\n", ini);
    }

    [Theory]
    [InlineData("1024")]
    [InlineData("")]
    [InlineData("512\nRamSize = 1")]
    public void Build_RejectsUnknownBackupSize(string size) =>
        Assert.Throws<ArgumentException>(() => VcIni.Build(new VcIniOptions("G", "US") { BackupSize = size }));

    [Theory]
    [InlineData("Evil\n[RomOption]")]
    [InlineData("a\rb")]
    public void Build_RejectsNewlinesInName(string name) =>
        Assert.Throws<ArgumentException>(() => VcIni.Build(new VcIniOptions(name, "US")));
}
