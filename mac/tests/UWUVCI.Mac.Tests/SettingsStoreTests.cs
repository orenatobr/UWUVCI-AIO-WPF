using UWUVCI.Mac.Services;

namespace UWUVCI.Mac.Tests;

public class SettingsStoreTests
{
    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        using var t = new TempDir();
        var s = new JsonSettingsStore(Path.Combine(t.Path, "settings.json")).Load();
        Assert.Equal("", s.CommonKey);
        Assert.Equal(ThemeChoice.System, s.Theme);
    }

    [Fact]
    public void SaveThenLoad_RoundTrips()
    {
        using var t = new TempDir();
        var store = new JsonSettingsStore(Path.Combine(t.Path, "sub", "settings.json"));
        store.Save(new AppSettings { CommonKey = TestData.ValidKey, OutputDir = "/tmp/out", Theme = ThemeChoice.Dark });
        var back = store.Load();
        Assert.Equal(TestData.ValidKey, back.CommonKey);
        Assert.Equal("/tmp/out", back.OutputDir);
        Assert.Equal(ThemeChoice.Dark, back.Theme);
    }

    [Fact]
    public void Save_RestrictsFileToOwner()
    {
        using var t = new TempDir();
        var path = Path.Combine(t.Path, "settings.json");
        var store = new JsonSettingsStore(path);
        store.Save(new AppSettings());
        store.Save(new AppSettings { Theme = ThemeChoice.Light });
        Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(path));
        Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute,
            File.GetUnixFileMode(Path.GetDirectoryName(path)!));
    }

    [Fact]
    public void Load_CorruptFile_ReturnsDefaults()
    {
        using var t = new TempDir();
        var path = Path.Combine(t.Path, "settings.json");
        File.WriteAllText(path, "{ not json");
        Assert.Equal("", new JsonSettingsStore(path).Load().CommonKey);
    }
}
