using UWUVCI.Core.N64;

namespace UWUVCI.Core.Tests.N64;

public class N64ProfilesTests
{
    private static VcIniOptions ProfileFor(uint c1, uint c2, bool? pak = null)
    {
        var g = N64GameDatabase.Find(c1, c2) ?? throw new InvalidOperationException("not in db");
        return N64Profiles.For(g, new RomHeader(c1, c2, "T", "NXXE"), pak);
    }

    [Fact]
    public void SaveOverride_Sram_ReplacesTheProfileAndDropsTheSize()
    {
        var g = N64GameDatabase.Find(0x30C7AC50, 0x7704072D)!;   // Conker: EEPROM 16K by default
        var o = N64Profiles.For(g, new RomHeader(0x30C7AC50, 0x7704072D, "T", "NCKE"), null, BackupType.Sram);
        Assert.Equal(BackupType.Sram, o.BackupType);
        Assert.Null(o.BackupSize);
        Assert.True(o.RetraceByVsync);   // the rest of the profile is kept
    }

    [Fact]
    public void SaveOverride_Eeprom_OnAFlashGame_GetsASize()
    {
        var g = N64GameDatabase.Find(0x8A6009B6, 0x94ACE150)!;   // Jet Force Gemini: Flash by default
        var o = N64Profiles.For(g, new RomHeader(0x8A6009B6, 0x94ACE150, "T", "NJFE"), null, BackupType.Eeprom);
        Assert.Equal(BackupType.Eeprom, o.BackupType);
        Assert.Equal("512", o.BackupSize);
    }

    [Fact]
    public void SaveOverride_Eeprom_OnBanjoTooie_KeepsTheSixteenKilobitSize()
    {
        var g = N64GameDatabase.Find(0xC2E9AA9A, 0x475D70AA)!;   // Banjo-Tooie: its profile is SRAM, its EEPROM is 16 Kbit (2048 bytes)
        var o = N64Profiles.For(g, new RomHeader(0xC2E9AA9A, 0x475D70AA, "T", "NB7E"), null, BackupType.Eeprom);
        Assert.Equal(BackupType.Eeprom, o.BackupType);
        Assert.Equal("2048", o.BackupSize);
    }

    [Fact]
    public void BanjoTooie_DefaultsToSram_WithoutASize()
    {
        var g = N64GameDatabase.Find(0xC2E9AA9A, 0x475D70AA)!;
        var o = N64Profiles.For(g, new RomHeader(0xC2E9AA9A, 0x475D70AA, "T", "NB7E"), null);
        Assert.Equal(BackupType.Sram, o.BackupType);
        Assert.Null(o.BackupSize);
    }

    [Fact]
    public void NoSaveOverride_FollowsTheProfile()
    {
        var g = N64GameDatabase.Find(0x30C7AC50, 0x7704072D)!;
        var o = N64Profiles.For(g, new RomHeader(0x30C7AC50, 0x7704072D, "T", "NCKE"), null, null);
        Assert.Equal(BackupType.Eeprom, o.BackupType);
    }

    [Fact]
    public void BanjoTooieEu_ClipsTheRightEdgeSoThePictureFillsTheFrame()
    {
        var o = N64Profiles.For(null, new RomHeader(1, 2, "Banjo-Tooie (E)", "NB7P"), null);
        Assert.Contains("[Render]\nClipRight = 33", o.ExtraSections);   // the right 5% of the EU picture is black; 55 also cut real content
        Assert.Contains("StickModify = 2", o.ExtraSections);   // the existing section is kept
    }

    [Fact]
    public void BanjoTooieUs_DoesNotGetTheEuClip()
    {
        var o = N64Profiles.For(null, new RomHeader(1, 2, "Banjo-Tooie (U)", "NB7E"), null);
        Assert.DoesNotContain("ClipRight", o.ExtraSections);
    }

    [Fact]
    public void Conker_UsesEepromTimerVsync_PakOffByDefault()
    {
        var o = ProfileFor(0x30C7AC50, 0x7704072D);
        Assert.Equal(BackupType.Eeprom, o.BackupType);
        Assert.Equal("2048", o.BackupSize);
        Assert.True(o.RetraceByVsync);
        Assert.True(o.UseTimer);
        Assert.False(o.ExpansionPak);
        Assert.Equal("US", o.Region);
    }

    [Fact]
    public void JetForceGemini_UsesFlashWithoutBackupSize()
    {
        var o = ProfileFor(0x8A6009B6, 0x94ACE150);
        Assert.Equal(BackupType.Flash, o.BackupType);
        Assert.Null(o.BackupSize);
        Assert.True(o.UseTimer);
        Assert.False(o.ExpansionPak);
    }

    [Fact]
    public void GoldenEyeUs_Eeprom4K_NoJpHack_NoPak()
    {
        var o = ProfileFor(0xDCBC50D1, 0x09FD1AA3);
        Assert.Equal("512", o.BackupSize);
        Assert.True(o.UseTimer);
        Assert.Null(o.ExtraSections);
        Assert.False(o.ExpansionPak);
    }

    [Fact]
    public void GoldenEyeJp_GetsVersionSpecificHack()
    {
        var o = ProfileFor(0xA24F4CF1, 0xA82327BA);
        Assert.Contains("0x7f0d0eeC", o.ExtraSections);
        Assert.Equal("JP", o.Region);
    }

    [Fact]
    public void Turok2_SavesOnControllerPak_NotRumble()
    {
        var o = ProfileFor(0x49088A11, 0x6494957E);
        Assert.True(o.MemPack);
        Assert.False(o.Rumble);
        Assert.False(o.ExpansionPak);
    }

    [Fact]
    public void TurokDinosaurHunter_NoPak_ControllerPak()
    {
        var o = ProfileFor(0x2F70F10D, 0x5C4187FF);
        Assert.True(o.MemPack);
        Assert.False(o.ExpansionPak);
    }

    [Fact]
    public void PakOverride_Wins_BothWays()
    {
        Assert.False(ProfileFor(0x30C7AC50, 0x7704072D, pak: false).ExpansionPak);
        Assert.True(ProfileFor(0x30C7AC50, 0x7704072D, pak: true).ExpansionPak);
    }

    [Fact]
    public void UnknownRom_FallsBackToHeaderAndSafeDefaults()
    {
        var o = N64Profiles.For(null, new RomHeader(1, 2, "MY HOMEBREW", "NHBE"), null);
        Assert.Equal("MY HOMEBREW", o.GameName);
        Assert.Equal("US", o.Region);
        Assert.Equal(BackupType.Auto, o.BackupType);
        Assert.False(o.ExpansionPak);
    }
}
