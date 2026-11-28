using SkiaSharp;
using UWUVCI.Core.Injection;

namespace UWUVCI.Core.Tests.Injection;

public class WupInjectorTests
{
    private static string MakeRom(string dir)
    {
        var rom = new byte[0x100];
        rom[0] = 0x80; rom[1] = 0x37; rom[2] = 0x12; rom[3] = 0x40;
        var p = Path.Combine(dir, "g.z64");
        File.WriteAllBytes(p, rom);
        return p;
    }

    private static string MakePng(string dir)
    {
        using var bmp = new SKBitmap(10, 10);
        bmp.Erase(SKColors.Green);
        var p = Path.Combine(dir, "a.png");
        File.WriteAllBytes(p, SKImage.FromBitmap(bmp).Encode(SKEncodedImageFormat.Png, 100).ToArray());
        return p;
    }

    [Fact]
    public void Prepare_WorksOnACopy_AndLeavesOriginalBaseUntouched()
    {
        using var b = new TestBase();
        var work = Directory.CreateTempSubdirectory();
        try
        {
            var req = new InjectRequest(GameConsole.N64, MakeRom(b.Root.FullName), b.Root.FullName, "My Game")
            { TvBootPath = MakePng(b.Root.FullName), IconPath = MakePng(b.Root.FullName) };
            var result = WupInjector.Prepare(req, work.FullName, new Random(7));

            Assert.True(File.Exists(Path.Combine(result, "meta", "bootTvTex.tga")));
            Assert.True(File.Exists(Path.Combine(result, "meta", "bootDrcTex.tga"))); // falls back to the TV image
            Assert.True(File.Exists(Path.Combine(result, "meta", "iconTex.tga")));
            Assert.False(File.Exists(Path.Combine(result, "meta", "bootLogoTex.tga")));
            Assert.Contains("My Game", File.ReadAllText(Path.Combine(result, "meta", "meta.xml")));
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, File.ReadAllBytes(Path.Combine(b.Content, "rom", "NAAE.z64")));
            Assert.Equal(0x100, new FileInfo(Path.Combine(result, "content", "rom", "NAAE.z64")).Length);
        }
        finally { work.Delete(true); }
    }

    [Fact]
    public void Prepare_NonN64_IsReportedAsUnsupported()
    {
        using var b = new TestBase();
        var req = new InjectRequest(GameConsole.Gba, MakeRom(b.Root.FullName), b.Root.FullName, "x");
        Assert.Throws<NotSupportedException>(() => WupInjector.Prepare(req, b.Root.FullName));
    }

    [Fact]
    public void Prepare_MissingRom_Throws()
    {
        using var b = new TestBase();
        var req = new InjectRequest(GameConsole.N64, "/nope.z64", b.Root.FullName, "x");
        Assert.Throws<FileNotFoundException>(() => WupInjector.Prepare(req, b.Root.FullName));
    }

    [Fact]
    public void UniqueOutput_SanitizesNameAndAvoidsCollisions()
    {
        var root = Directory.CreateTempSubdirectory();
        try
        {
            var first = WupInjector.UniqueOutput(root.FullName, GameConsole.N64, "Banjo: Tooie!");
            Assert.EndsWith("[WUP][N64] Banjo Tooie", first);
            Directory.CreateDirectory(first);
            Assert.EndsWith("[WUP][N64] Banjo Tooie_0", WupInjector.UniqueOutput(root.FullName, GameConsole.N64, "Banjo: Tooie!"));
            Assert.EndsWith("[WUP][N64] NoName", WupInjector.UniqueOutput(root.FullName, GameConsole.N64, "???"));
        }
        finally { root.Delete(true); }
    }

    [Theory]
    [InlineData("zz")]
    [InlineData("")]
    public void Pack_RejectsBadKey(string key) =>
        Assert.Throws<ArgumentException>(() => WupPacker.Pack("/x", "/y", key));

    [Fact]
    public void Pack_RejectsIncompleteBase()
    {
        using var b = new TestBase();
        Directory.Delete(Path.Combine(b.Root.FullName, "meta"), true);
        Assert.Throws<DirectoryNotFoundException>(() =>
            WupPacker.Pack(b.Root.FullName, "/tmp/out", new string('A', 32)));
    }

    [Fact]
    public void Run_N64_ProducesInstallableTitleFiles()
    {
        using var b = new TestBase();
        var outRoot = Directory.CreateTempSubdirectory();
        try
        {
            var req = new InjectRequest(GameConsole.N64, MakeRom(outRoot.FullName), b.Root.FullName, "Pack Test");
            var output = WupInjector.Run(req, outRoot.FullName, new string('0', 32));
            Assert.EndsWith("[WUP][N64] Pack Test", output);
            foreach (var f in new[] { "title.tmd", "title.tik", "title.cert", "00000000.app" })
                Assert.True(File.Exists(Path.Combine(output, f)), f);
        }
        finally { outRoot.Delete(true); }
    }

    [Fact]
    public void Run_N64_PacksTheTitleIdOfTheBaseAppXml()
    {
        // CNUSPACKER looks for app.xml under "<in>\\code\\app.xml"; without the shim it found nothing on macOS and packed title id 0.
        using var b = new TestBase();
        var outRoot = Directory.CreateTempSubdirectory();
        try
        {
            var req = new InjectRequest(GameConsole.N64, MakeRom(outRoot.FullName), b.Root.FullName, "Pack Test");
            var output = WupInjector.Run(req, outRoot.FullName, new string('0', 32));
            var tmd = File.ReadAllBytes(Path.Combine(output, "title.tmd"));
            var id = Convert.ToHexString(tmd.AsSpan(0x18C, 8));                      // title id in the TMD header
            Assert.NotEqual("0000000000000000", id);
            Assert.StartsWith("00050002", id);                                        // MetaXml.Apply writes 00050002 + a random id
        }
        finally { outRoot.Delete(true); }
    }

    [Fact]
    public void Pack_RefusesStrayFilesInBaseRoot()
    {
        using var b = new TestBase();
        File.WriteAllText(Path.Combine(b.Root.FullName, "stray.z64"), "x");
        Assert.Throws<InvalidDataException>(() =>
            WupPacker.Pack(b.Root.FullName, Path.Combine(b.Root.FullName, "..", "o"), new string('0', 32)));
    }
}
