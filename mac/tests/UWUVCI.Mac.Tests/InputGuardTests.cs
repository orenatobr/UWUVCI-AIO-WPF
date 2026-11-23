using UWUVCI.Mac.Services;

namespace UWUVCI.Mac.Tests;

public class InputGuardTests
{
    [Theory]
    [InlineData("0123456789abcdef0123456789ABCDEF", true)]
    [InlineData("0123456789abcdef0123456789ABCDE", false)]
    [InlineData("0123456789abcdef0123456789ABCDEG", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsHex32_Validates(string? value, bool expected) =>
        Assert.Equal(expected, InputGuard.IsHex(value, 32));

    [Fact]
    public void File_RejectsMissingWrongExtensionAndOversize()
    {
        using var t = new TempDir();
        var ok = t.File("a.z64", TestData.Rom(1, 2));
        Assert.Null(InputGuard.CheckFile(ok, 1 << 20, [".z64"]).Error);
        Assert.NotNull(InputGuard.CheckFile(Path.Combine(t.Path, "none.z64"), 1 << 20, [".z64"]).Error);
        Assert.NotNull(InputGuard.CheckFile(ok, 1 << 20, [".png"]).Error);
        Assert.NotNull(InputGuard.CheckFile(ok, 10, [".z64"]).Error);
        Assert.NotNull(InputGuard.CheckFile("", 10, [".z64"]).Error);
        Assert.NotNull(InputGuard.CheckFile("rel/x.z64", 10, [".z64"]).Error);
    }

    [Fact]
    public void File_CanonicalizesDotDotAndSymlinks()
    {
        using var t = new TempDir();
        var real = t.File("real.z64", TestData.Rom(1, 2));
        var link = Path.Combine(t.Path, "link.z64");
        File.CreateSymbolicLink(link, real);
        var viaDots = Path.Combine(t.Path, "x", "..", "link.z64");
        Directory.CreateDirectory(Path.Combine(t.Path, "x"));
        var r = InputGuard.CheckFile(viaDots, 1 << 20, [".z64"]);
        Assert.Equal(Path.GetFullPath(real), Path.GetFullPath(r.Path!));
        Assert.DoesNotContain("..", r.Path);
    }

    [Fact]
    public void Image_ChecksMagicBytes()
    {
        using var t = new TempDir();
        Assert.Null(InputGuard.CheckImage(t.File("a.png", TestData.Png())).Error);
        Assert.NotNull(InputGuard.CheckImage(t.File("b.png", [1, 2, 3, 4, 5, 6, 7, 8, 9, 10])).Error);
        Assert.Null(InputGuard.CheckImage(t.File("c.jpg", [0xFF, 0xD8, 0xFF, 0xE0, 0, 0])).Error);
        Assert.Null(InputGuard.CheckImage(t.File("d.bmp", [0x42, 0x4D, 0, 0, 0, 0])).Error);
    }

    [Fact]
    public void BaseFolder_NeedsCodeContentMeta()
    {
        using var t = new TempDir();
        Assert.Null(InputGuard.CheckBaseFolder(t.Base()).Error);
        var partial = Path.Combine(t.Path, "partial");
        Directory.CreateDirectory(Path.Combine(partial, "code"));
        Assert.NotNull(InputGuard.CheckBaseFolder(partial).Error);
        Assert.NotNull(InputGuard.CheckBaseFolder("").Error);
    }

    [Fact]
    public void OutputFolder_ExpandsTildeAndRejectsFiles()
    {
        using var t = new TempDir();
        Assert.Null(InputGuard.CheckOutputFolder(Path.Combine(t.Path, "new")).Error);
        var f = t.File("f.txt", [1]);
        Assert.NotNull(InputGuard.CheckOutputFolder(f).Error);
        Assert.NotNull(InputGuard.CheckOutputFolder("relative/dir").Error);
        var home = InputGuard.CheckOutputFolder("~/UWUVCI").Path;
        Assert.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), home);
    }
}
