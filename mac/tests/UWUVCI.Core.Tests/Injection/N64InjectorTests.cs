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

    // li r11,0x4000 / cmpwi r12,0x800 / stb r7,1(r26) / beq +8 / li r11,0 / ori r10,r11,0x8000: how the engine answers the EEPROM size query
    private static readonly uint[] EepromAnswer = [0x39604000, 0x2C0C0800, 0x98FA0001, 0x41820008, 0x39600000, 0x616A8000];

    internal static string WriteEngine(TestBase b, bool withSequence = true)
    {
        var code = SyntheticRpx.Code();
        if (withSequence)
            for (int i = 0; i < EepromAnswer.Length; i++)
                System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(code.AsSpan((300 + i) * 4), EepromAnswer[i]);
        var path = Path.Combine(b.Root.FullName, "code", "VESSEL.rpx");
        File.WriteAllBytes(path, SyntheticRpx.Build(code));
        return path;
    }

    private static string RomFile(TestBase b, byte[] z64)
    {
        var f = Path.Combine(b.Root.FullName, "game.z64");
        File.WriteAllBytes(f, z64);
        return f;
    }

    [Fact]
    public void Inject_BanjoTooie_MakesTheEngineAnswerA16KbitEeprom()
    {
        using var b = new TestBase();
        var rpx = WriteEngine(b);
        N64Injector.Inject(b.Root.FullName, RomFile(b, Z64()), new N64Options());
        var code = SyntheticRpx.Read(File.ReadAllBytes(rpx)).Code;
        Assert.Equal(0x60000000u, System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(code.AsSpan((300 + 4) * 4)));   // li r11,0 became a nop
    }

    [Fact]
    public void Inject_BanjoTooie_PatchesAlsoWhenEepromIsChosen()
    {
        using var b = new TestBase();
        var rpx = WriteEngine(b);
        N64Injector.Inject(b.Root.FullName, RomFile(b, Z64()), new N64Options { SaveType = BackupType.Eeprom });
        var code = SyntheticRpx.Read(File.ReadAllBytes(rpx)).Code;
        Assert.Equal(0x60000000u, System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(code.AsSpan((300 + 4) * 4)));
    }

    [Fact]
    public void Inject_OtherGames_LeaveTheEngineAlone()
    {
        using var b = new TestBase();
        var rpx = WriteEngine(b);
        var before = File.ReadAllBytes(rpx);
        N64Injector.Inject(b.Root.FullName, RomFile(b, Z64("SOME OTHER GAME", 0x11111111, 0x22222222)), new N64Options());
        Assert.Equal(before, File.ReadAllBytes(rpx));
    }

    [Fact]
    public void Inject_BanjoTooie_OnAnEngineWithoutTheSequence_FailsBeforeTouchingTheBase()
    {
        using var b = new TestBase();
        WriteEngine(b, withSequence: false);
        var romBefore = File.ReadAllBytes(Path.Combine(b.Content, "rom", "NAAE.z64"));
        var ex = Assert.Throws<InvalidDataException>(() => N64Injector.Inject(b.Root.FullName, RomFile(b, Z64()), new N64Options()));
        Assert.Contains("engine", ex.Message);
        Assert.Equal(romBefore, File.ReadAllBytes(Path.Combine(b.Content, "rom", "NAAE.z64")));
    }

    [Fact]
    public void Inject_ReplacesRomAndWritesIniNamedAfterBaseRom()
    {
        using var b = new TestBase();
        WriteEngine(b);
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
        Assert.Contains("BackupType = 1", ini);   // Banjo-Tooie: SRAM, the only save type that boots in Cemu (EEPROM stays on the first scene)
        Assert.DoesNotContain("BackupSize", ini);
        Assert.Contains("TrueBoot = 1", ini);      // community ini for NB7E
        Assert.Contains("StickModify = 2", ini);
        Assert.DoesNotContain("RamSize", ini);     // works with the default RAM, do not experiment
        Assert.Contains("Rumble = 1", ini);
    }

    [Fact]
    public void Inject_CustomIniOverridesAutomaticProfile()
    {
        using var b = new TestBase();
        WriteEngine(b);                     // the test ROM is Banjo-Tooie, whose engine patch applies with a custom ini too
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

public class N64InjectorTargetGameTests
{
    private static byte[] Rom(uint c1, uint c2)
    {
        var rom = new byte[0x100];
        rom[0] = 0x80; rom[1] = 0x37; rom[2] = 0x12; rom[3] = 0x40;
        for (int i = 0; i < 4; i++) { rom[0x10 + i] = (byte)(c1 >> (24 - 8 * i)); rom[0x14 + i] = (byte)(c2 >> (24 - 8 * i)); }
        System.Text.Encoding.ASCII.GetBytes("TEST").CopyTo(rom, 0x20);
        return rom;
    }

    private static string InjectAndReadIni(uint c1, uint c2)
    {
        using var b = new TestBase();
        N64InjectorTests.WriteEngine(b);     // Banjo-Tooie also gets its engine patched
        var romFile = Path.Combine(b.Root.FullName, "g.z64");
        File.WriteAllBytes(romFile, Rom(c1, c2));
        N64Injector.Inject(b.Root.FullName, romFile, new N64Options());
        return File.ReadAllText(Path.Combine(b.Content, "config", "NAAE.z64.ini"));
    }

    [Theory]
    [InlineData(0x30C7AC50u, 0x7704072Du, "Conker", "BackupType = 3")]
    [InlineData(0x8A6009B6u, 0x94ACE150u, "Jet Force Gemini", "BackupType = 2")]
    [InlineData(0xC2E9AA9Au, 0x475D70AAu, "Banjo-Tooie", "BackupType = 1")]
    [InlineData(0xDCBC50D1u, 0x09FD1AA3u, "GoldenEye", "BackupSize = 512")]
    [InlineData(0x2F70F10Du, 0x5C4187FFu, "Turok - Dinosaur Hunter", "MemPack = 1")]
    [InlineData(0x49088A11u, 0x6494957Eu, "Turok 2", "MemPack = 1")]
    [InlineData(0x89A579F1u, 0x667E97EFu, "Turok 3", "MemPack = 1")]
    [InlineData(0xADB9498Bu, 0xDAF28F55u, "Turok - Rage Wars", "MemPack = 1")]
    public void TargetGames_GetAnIniWithTheirSaveSetup(uint c1, uint c2, string game, string expected)
    {
        var ini = InjectAndReadIni(c1, c2);
        Assert.Contains(game, ini);
        Assert.Contains(expected, ini);
        Assert.DoesNotContain("RamSize", ini); // Expansion Pak stays off unless the user asks
    }
}
