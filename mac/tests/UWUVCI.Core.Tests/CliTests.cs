using UWUVCI.Core.Tests.Injection;

namespace UWUVCI.Core.Tests;

public class CliTests
{
    private static string Rom(string dir)
    {
        var rom = new byte[0x100];
        rom[0] = 0x80; rom[1] = 0x37; rom[2] = 0x12; rom[3] = 0x40;
        var p = Path.Combine(dir, "g.z64");
        File.WriteAllBytes(p, rom);
        return p;
    }

    private static (int Code, string Out, string Err) Run(params string[] args)
    {
        var o = new StringWriter(); var e = new StringWriter();
        int code = Cli.Run(args, o, e);
        return (code, o.ToString(), e.ToString());
    }

    [Fact]
    public void VcLayout_InjectsIntoACopyAndPrintsTheCemuPath()
    {
        using var b = new TestBase();
        var work = Directory.CreateTempSubdirectory();
        try
        {
            var rom = Rom(work.FullName);
            var (code, o, _) = Run("vc-layout", rom, b.Root.FullName, "My Game", Path.Combine(work.FullName, "out"));
            Assert.Equal(0, code);
            var baseDir = Path.Combine(work.FullName, "out", "base");
            Assert.Contains(baseDir, o);
            Assert.Equal(0x100, new FileInfo(Path.Combine(baseDir, "content", "rom", "NAAE.z64")).Length);
            Assert.Contains("[RomOption]", File.ReadAllText(Path.Combine(baseDir, "content", "config", "NAAE.z64.ini")));
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, File.ReadAllBytes(Path.Combine(b.Content, "rom", "NAAE.z64"))); // original untouched
        }
        finally { work.Delete(true); }
    }

    [Theory]
    [InlineData("on", true)]
    [InlineData("off", false)]
    public void VcLayout_PakFlag_SetsRamSize(string value, bool expected)
    {
        using var b = new TestBase();
        var work = Directory.CreateTempSubdirectory();
        try
        {
            var (code, _, _) = Run("vc-layout", Rom(work.FullName), b.Root.FullName, "G", Path.Combine(work.FullName, "o"), "--pak", value);
            Assert.Equal(0, code);
            var ini = File.ReadAllText(Path.Combine(work.FullName, "o", "base", "content", "config", "NAAE.z64.ini"));
            Assert.Equal(expected, ini.Contains("RamSize = 0x800000"));
        }
        finally { work.Delete(true); }
    }

    [Fact]
    public void VcLayout_MissingBase_FailsWithAMessage()
    {
        var (code, _, err) = Run("vc-layout", "/nope.z64", "/nope-base", "G", "/tmp/uwuvci-never");
        Assert.Equal(2, code);
        Assert.False(string.IsNullOrWhiteSpace(err));
    }

    [Fact]
    public void VcPack_RequiresAValidKey()
    {
        using var b = new TestBase();
        var work = Directory.CreateTempSubdirectory();
        try
        {
            var (code, _, err) = Run("vc-pack", Rom(work.FullName), b.Root.FullName, "G", work.FullName, "--key", "zz");
            Assert.Equal(2, code);
            Assert.Contains("key", err, StringComparison.OrdinalIgnoreCase);
        }
        finally { work.Delete(true); }
    }

    [Fact]
    public void UnknownCommand_PrintsUsage()
    {
        var (code, _, err) = Run("bogus");
        Assert.Equal(2, code);
        Assert.Contains("vc-layout", err);
    }
}
