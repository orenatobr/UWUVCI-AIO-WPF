using UWUVCI.Core.N64;
using UWUVCI.Mac.Services;
using UWUVCI.Mac.ViewModels;

namespace UWUVCI.Mac.Tests;

public class BaseSelectionTests
{
    // BaseKey hashes to this SHA-1 (computed independently, see N64BasesTests).
    private static readonly N64Base Game = new("Test Game", "US", "0005000010000001", "B1775A785F09A6EBAF2DC33D6EAEB98974D9CDB8");
    private static readonly N64Base Other = new("Another Game", "EU", "0005000010000002", "1C7313922EA106D22DBE3C9C3E4E880C1DE83501");
    private static readonly N64Base[] Bases = [Game, Other];

    private const string BaseKey = "0123456789abcdef0123456789abcdef";

    private static Fixture NewFixture(string commonKey = TestData.ValidKey) => new(commonKey, Bases);

    private static BaseOption Option(Fixture f, N64Base b) => f.Vm.BaseOptions.Single(o => o.Base == b);

    private static void FakeDownloadWritesBase(Fixture f) => f.Service.OnDownload = dest =>
    {
        foreach (var d in new[] { "code", "content", "meta" }) Directory.CreateDirectory(Path.Combine(dest, d));
    };

    [Fact]
    public void Options_StartWithPlaceholderThenSortedByNameThenRegion()
    {
        using var f = new Fixture();
        var o = f.Vm.BaseOptions;
        Assert.Equal(12, o.Count);
        Assert.Null(o[0].Base);
        Assert.Equal("Choose a base...", o[0].Label);
        Assert.Same(o[0], f.Vm.SelectedOption);
        var names = o.Skip(1).Select(x => (x.Base!.Name, x.Base.Region)).ToList();
        Assert.Equal(names.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Region, StringComparer.Ordinal).ToList(), names);
        Assert.Contains(o, x => x.Label == "Mario Golf (US)");
    }

    [Fact]
    public async Task Choose_CachedBase_IsUsedAtOnce()
    {
        using var f = NewFixture();
        var dir = f.Dir.Base(Path.Combine("bases", Game.FolderName));
        await f.Vm.ChooseBaseAsync(Option(f, Game));
        Assert.True(f.Vm.IsBaseValid);
        Assert.Equal(Path.GetFullPath(dir), f.Vm.BaseDir);
        Assert.Equal(0, f.Service.DownloadCount);
        Assert.False(f.Vm.ShowDownload);
        Assert.False(f.Vm.ShowKeyField);
    }

    [Fact]
    public async Task Choose_WithSavedKey_DownloadsAutomatically()
    {
        using var f = NewFixture();
        f.Settings.SaveTitleKey(Game.TitleId, BaseKey);
        FakeDownloadWritesBase(f);
        await f.Vm.ChooseBaseAsync(Option(f, Game));
        var d = f.Service.Download!.Value;
        Assert.Equal(Game.TitleId.ToUpperInvariant(), d.Id);
        Assert.Equal(BaseKey, d.Key);
        Assert.StartsWith(Path.Combine(f.Dir.Path, "bases"), d.Dest);
        Assert.Equal(TestData.ValidKey, f.Service.CommonKeyUsed);
        Assert.True(f.Vm.IsBaseValid);
        Assert.Equal(Path.GetFullPath(Path.Combine(f.Dir.Path, "bases", Game.FolderName)), f.Vm.BaseDir);
        Assert.False(f.Vm.IsDownloading);
        Assert.Null(f.Vm.BaseError);
    }

    [Fact]
    public async Task Choose_WithoutKey_AsksForTheKey()
    {
        using var f = NewFixture();
        await f.Vm.ChooseBaseAsync(Option(f, Game));
        Assert.Equal(0, f.Service.DownloadCount);
        Assert.True(f.Vm.ShowKeyField);
        Assert.True(f.Vm.ShowDownload);
        Assert.False(f.Vm.DownloadSelectedBaseCommand.CanExecute(null));
        Assert.False(f.Vm.IsBaseValid);
        Assert.False(f.Vm.CanInject);
    }

    [Theory]
    [InlineData("00000000000000000000000000000000", "This key does not match the selected base.")]
    [InlineData("0123", "The title key is 32 hexadecimal characters.")]
    public async Task WrongKey_IsRejected_WithoutEchoingIt(string typed, string message)
    {
        using var f = NewFixture();
        await f.Vm.ChooseBaseAsync(Option(f, Game));
        f.Vm.BaseKeyInput = typed;
        Assert.Equal(message, f.Vm.BaseKeyError);
        Assert.False(f.Vm.DownloadSelectedBaseCommand.CanExecute(null));
        Assert.Equal("", f.Settings.TitleKeyFor(Game.TitleId) ?? "");
    }

    [Fact]
    public async Task ValidKey_IsSaved_AndDownloadStarts()
    {
        using var f = NewFixture();
        FakeDownloadWritesBase(f);
        await f.Vm.ChooseBaseAsync(Option(f, Game));
        f.Vm.BaseKeyInput = BaseKey.ToUpperInvariant();
        Assert.Null(f.Vm.BaseKeyError);
        Assert.True(f.Vm.DownloadSelectedBaseCommand.CanExecute(null));
        await f.Vm.DownloadSelectedBaseCommand.ExecuteAsync(null);
        Assert.Equal(BaseKey.ToUpperInvariant(), f.Service.Download!.Value.Key);
        Assert.Equal(BaseKey.ToUpperInvariant(), f.Store.Current.TitleKeys[Game.TitleId]);
        Assert.True(f.Vm.IsBaseValid);
        Assert.Equal("", f.Vm.BaseKeyInput);
    }

    [Fact]
    public async Task MissingCommonKey_BlocksAndExplains()
    {
        using var f = NewFixture(commonKey: "");
        f.Settings.SaveTitleKey(Game.TitleId, BaseKey);
        await f.Vm.ChooseBaseAsync(Option(f, Game));
        Assert.Equal(0, f.Service.DownloadCount);
        Assert.True(f.Vm.ShowCommonKeyHint);
        Assert.True(f.Vm.ShowDownload);
        Assert.False(f.Vm.DownloadSelectedBaseCommand.CanExecute(null));

        // Adding the common key in Settings unblocks the button without reselecting.
        f.Settings.CommonKey = TestData.ValidKey;
        Assert.False(f.Vm.ShowCommonKeyHint);
        Assert.True(f.Vm.DownloadSelectedBaseCommand.CanExecute(null));
    }

    [Fact]
    public async Task Cancel_StopsTheDownload_AndAllowsRetry()
    {
        using var f = NewFixture();
        f.Settings.SaveTitleKey(Game.TitleId, BaseKey);
        f.Service.DownloadGate = new TaskCompletionSource();
        var run = f.Vm.ChooseBaseAsync(Option(f, Game));
        Assert.True(f.Vm.IsDownloading);
        Assert.Equal(30, f.Vm.Percent);
        f.Vm.CancelCommand.Execute(null);
        await run;
        Assert.False(f.Vm.IsDownloading);
        Assert.Equal("Download cancelled.", f.Vm.BaseError);
        Assert.False(f.Vm.IsBaseValid);
        Assert.True(f.Vm.ShowDownload);
        Assert.Equal("Try again", f.Vm.DownloadButtonText);
    }

    [Fact]
    public async Task DownloadFailure_ShowsMessageWithoutSecrets_AndLeavesNoBase()
    {
        using var f = NewFixture();
        f.Settings.SaveTitleKey(Game.TitleId, BaseKey);
        f.Service.Fail = new IOException($"HTTP 404 for key {BaseKey} and {TestData.ValidKey}");
        await f.Vm.ChooseBaseAsync(Option(f, Game));
        Assert.Contains("404", f.Vm.BaseError);
        Assert.DoesNotContain(BaseKey, f.Vm.BaseError!, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(TestData.ValidKey, f.Vm.BaseError!, StringComparison.OrdinalIgnoreCase);
        Assert.False(f.Vm.IsBaseValid);

        // The failed attempt must not look like a cached base on the next selection.
        Assert.False(Directory.Exists(Path.Combine(f.Dir.Path, "bases", Game.FolderName)));
    }

    [Fact]
    public async Task SavedKeyThatDoesNotMatch_IsIgnored()
    {
        using var f = NewFixture();
        f.Settings.SaveTitleKey(Other.TitleId, BaseKey);
        await f.Vm.ChooseBaseAsync(Option(f, Other));
        Assert.Equal(0, f.Service.DownloadCount);
        Assert.True(f.Vm.ShowKeyField);
    }

    [Fact]
    public async Task ChoosingAnotherBase_ClearsTheReadyBase_AndPlaceholderResets()
    {
        using var f = NewFixture();
        f.Dir.Base(Path.Combine("bases", Game.FolderName));
        await f.Vm.ChooseBaseAsync(Option(f, Game));
        Assert.True(f.Vm.IsBaseValid);
        await f.Vm.ChooseBaseAsync(Option(f, Other));
        Assert.False(f.Vm.IsBaseValid);
        Assert.True(f.Vm.ShowKeyField);
        await f.Vm.ChooseBaseAsync(f.Vm.BaseOptions[0]);
        Assert.False(f.Vm.ShowKeyField);
        Assert.False(f.Vm.ShowDownload);
    }

    [Fact]
    public async Task CustomFolder_ResetsTheDropdown()
    {
        using var f = NewFixture();
        await f.Vm.ChooseBaseAsync(Option(f, Game));
        f.Vm.SetBase(f.Dir.Base());
        Assert.True(f.Vm.IsBaseValid);
        Assert.Same(f.Vm.BaseOptions[0], f.Vm.SelectedOption);
        Assert.False(f.Vm.ShowKeyField);
        Assert.True(f.Vm.IsCustomBase);
    }

    [Fact]
    public void Native_StillSkipsTheBaseStep()
    {
        using var f = NewFixture();
        f.Vm.Engine = InjectEngine.Native;
        Assert.True(f.Vm.IsBaseSatisfied);
        Assert.False(f.Vm.IsBaseRequired);
    }
}
