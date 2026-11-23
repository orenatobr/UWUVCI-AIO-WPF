using System.IO.Compression;
using UWUVCI.Core.Injection;

namespace UWUVCI.Core.Tests.Injection;

public class Not64Tests
{
    private static string TemplateDir(TestBase b)
    {
        // The not64 template has no rom slot: code, content (not64.txt), meta only.
        File.Delete(Path.Combine(b.Content, "rom", "NAAE.z64"));
        Directory.Delete(Path.Combine(b.Content, "rom"));
        Directory.Delete(Path.Combine(b.Content, "config"), true);
        File.WriteAllText(Path.Combine(b.Content, "not64.txt"), "not64");
        return b.Root.FullName;
    }

    private static string Rom(string dir)
    {
        var rom = new byte[0x100];
        rom[0] = 0x37; rom[1] = 0x80; rom[2] = 0x40; rom[3] = 0x12; // .v64 magic
        var p = Path.Combine(dir, "Conker's Bad Fur Day.v64");
        File.WriteAllBytes(p, rom);
        return p;
    }

    [Fact]
    public void Prepare_WritesZ64ToSdAndAutobootAndEditsTitle()
    {
        using var b = new TestBase();
        var template = TemplateDir(b);
        var work = Directory.CreateTempSubdirectory();
        var romDir = Directory.CreateTempSubdirectory();
        try
        {
            var r = Not64Injector.Prepare(template, Rom(romDir.FullName), "Conker", work.FullName, new Random(3));

            Assert.True(File.Exists(Path.Combine(r.TitleDir, "code", "app.xml")));
            Assert.Contains("Conker", File.ReadAllText(Path.Combine(r.TitleDir, "meta", "meta.xml")));
            var autoboot = File.ReadAllText(Path.Combine(r.SdDir, "not64", "autoboot.txt"));
            Assert.Equal("roms/Conker's-Bad-Fur-Day.z64\n", autoboot);
            var rom = File.ReadAllBytes(Path.Combine(r.SdDir, "not64", "roms", "Conker's-Bad-Fur-Day.z64"));
            Assert.Equal(new byte[] { 0x80, 0x37, 0x12, 0x40 }, rom[..4]); // converted to z64
            Assert.False(Directory.GetFiles(r.TitleDir, "*.z64", SearchOption.AllDirectories).Length > 0,
                "the ROM must never be packed into the title");
        }
        finally { work.Delete(true); romDir.Delete(true); }
    }

    [Theory]
    [InlineData("../../evil name.z64", "evil-name.z64")]
    [InlineData("Banjo Tooie (U) [!].v64", "Banjo-Tooie.z64")]
    [InlineData("???.n64", "game.z64")]
    public void SafeRomName_StripsPathAndOddCharacters(string input, string expected) =>
        Assert.Equal(expected, Not64Injector.SafeRomName(input));

    [Fact]
    public void ExtractZip_RejectsPathTraversal()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            var zip = Path.Combine(dir.FullName, "bad.zip");
            using (var z = ZipFile.Open(zip, ZipArchiveMode.Create))
            {
                var e = z.CreateEntry("../escape.txt");
                using var w = new StreamWriter(e.Open());
                w.Write("x");
            }
            Assert.Throws<InvalidDataException>(() => Not64Template.ExtractZip(zip, Path.Combine(dir.FullName, "out")));
            Assert.False(File.Exists(Path.Combine(dir.FullName, "escape.txt")));
        }
        finally { dir.Delete(true); }
    }

    [Fact]
    public void ExtractZip_ReturnsFolderHoldingCodeContentMeta()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            var zip = Path.Combine(dir.FullName, "t.zip");
            using (var z = ZipFile.Open(zip, ZipArchiveMode.Create))
                foreach (var name in new[] { "not64/code/a.rpx", "not64/content/x", "not64/meta/meta.xml" })
                {
                    using var w = new StreamWriter(z.CreateEntry(name).Open());
                    w.Write("x");
                }
            var title = Not64Template.ExtractZip(zip, Path.Combine(dir.FullName, "out"));
            Assert.EndsWith("not64", title);
            Assert.True(Directory.Exists(Path.Combine(title, "meta")));
        }
        finally { dir.Delete(true); }
    }

    [Fact]
    public void ExtractZip_WithoutTitleFolders_Throws()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            var zip = Path.Combine(dir.FullName, "t.zip");
            using (var z = ZipFile.Open(zip, ZipArchiveMode.Create))
            {
                using var w = new StreamWriter(z.CreateEntry("readme.txt").Open());
                w.Write("x");
            }
            Assert.Throws<InvalidDataException>(() => Not64Template.ExtractZip(zip, Path.Combine(dir.FullName, "out")));
        }
        finally { dir.Delete(true); }
    }
}
