using UWUVCI.Core.Injection;
using UWUVCI.Core.N64;

namespace UWUVCI.Core.Tests.Injection;

public class N64InjectorTests
{
    private static byte[] Z64(string title = "TEST", uint c1 = 0xC2E9AA9A, uint c2 = 0x475D70AA)
    {
        var rom = new byte[0x100];
        rom[0] = 0x80; rom[1] = 0x37; rom[2] = 0x12; rom[3] = 0x40;
        for (int i = 0; i < 4; i++) { rom[0x10 + i] = (byte)(c1 >> (24 - 8 * i)); rom[0x14 + i] = (byte)(c2 >> (24 - 8 * i)); }
        System.Text.Encoding.ASCII.GetBytes(title).CopyTo(rom, 0x20);
        System.Text.Encoding.ASCII.GetBytes("NB7E").CopyTo(rom, 0x3B);
        return rom;
    }

    [Fact]
    public void Inject_ReplacesRomAndWritesIniNamedAfterBaseRom()
    {
        using var b = new TestBase();
        var romFile = Path.Combine(b.Root.FullName, "game.v64");
        var z = Z64();
        var v = (byte[])z.Clone();
        for (int i = 0; i < v.Length; i += 2) (v[i], v[i + 1]) = (v[i + 1], v[i]);
        File.WriteAllBytes(romFile, v);

        N64Injector.Inject(b.Root.FullName, romFile, new N64Options());

        Assert.Equal(z, File.ReadAllBytes(Path.Combine(b.Content, "rom", "NAAE.z64")));
        var ini = File.ReadAllText(Path.Combine(b.Content, "config", "NAAE.z64.ini"));
        Assert.StartsWith(";", ini);
        Assert.Contains("[RomOption]", ini);
        Assert.Contains("BackupType = 3", ini);   // Banjo-Tooie: EEPROM
        Assert.Contains("BackupSize = 2048", ini);
        Assert.Contains("TrueBoot = 1", ini);      // community ini for NB7E
        Assert.Contains("StickModify = 2", ini);
        Assert.DoesNotContain("RamSize", ini);     // works with the default RAM, do not experiment
        Assert.Contains("Rumble = 1", ini);
    }

    [Fact]
    public void Inject_CustomIniOverridesAutomaticProfile()
    {
        using var b = new TestBase();
        var romFile = Path.Combine(b.Root.FullName, "game.z64");
        File.WriteAllBytes(romFile, Z64());
        var custom = Path.Combine(b.Root.FullName, "mine.ini");
        File.WriteAllText(custom, "[RomOption]\nBackupType = 1\n");

        N64Injector.Inject(b.Root.FullName, romFile, new N64Options { CustomIniPath = custom });

        Assert.Equal("[RomOption]\nBackupType = 1\n",
            File.ReadAllText(Path.Combine(b.Content, "config", "NAAE.z64.ini")));
    }

    [Fact]
    public void Inject_UnknownRom_StillWritesSafeDefaultIni()
    {
        using var b = new TestBase();
        var romFile = Path.Combine(b.Root.FullName, "x.z64");
        File.WriteAllBytes(romFile, Z64("HOMEBREW", 1, 2));

        N64Injector.Inject(b.Root.FullName, romFile, new N64Options());

        var ini = File.ReadAllText(Path.Combine(b.Content, "config", "NAAE.z64.ini"));
        Assert.Contains("BackupType = 0", ini);
        Assert.DoesNotContain("RamSize", ini);
    }

    [Fact]
    public void Inject_ForceExpansionPakOverride()
    {
        using var b = new TestBase();
        var romFile = Path.Combine(b.Root.FullName, "x.z64");
        File.WriteAllBytes(romFile, Z64("HOMEBREW", 1, 2));

        N64Injector.Inject(b.Root.FullName, romFile, new N64Options { ExpansionPak = true });

        Assert.Contains("RamSize = 0x800000",
            File.ReadAllText(Path.Combine(b.Content, "config", "NAAE.z64.ini")));
    }

    [Fact]
    public void Inject_BaseWithoutRom_Throws()
    {
        using var b = new TestBase();
        File.Delete(Path.Combine(b.Content, "rom", "NAAE.z64"));
        var romFile = Path.Combine(b.Root.FullName, "x.z64");
        File.WriteAllBytes(romFile, Z64());
        Assert.Throws<InvalidDataException>(() => N64Injector.Inject(b.Root.FullName, romFile, new N64Options()));
    }

    [Fact]
    public void Inject_InvalidRom_ThrowsAndLeavesBaseRomIntact()
    {
        using var b = new TestBase();
        var romFile = Path.Combine(b.Root.FullName, "bad.z64");
        File.WriteAllBytes(romFile, new byte[64]);
        Assert.Throws<InvalidDataException>(() => N64Injector.Inject(b.Root.FullName, romFile, new N64Options()));
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, File.ReadAllBytes(Path.Combine(b.Content, "rom", "NAAE.z64")));
    }
}
