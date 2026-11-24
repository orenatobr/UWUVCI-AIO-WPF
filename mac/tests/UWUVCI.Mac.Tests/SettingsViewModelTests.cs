using UWUVCI.Mac.Services;
using UWUVCI.Mac.ViewModels;

namespace UWUVCI.Mac.Tests;

public class SettingsViewModelTests
{
    [Fact]
    public void Ctor_LoadsStoredValues_AndDefaultsOutput()
    {
        var store = new MemoryStore { Current = new AppSettings { CommonKey = TestData.ValidKey, Theme = ThemeChoice.Dark } };
        var vm = new SettingsViewModel(store, "/tmp/default");
        Assert.Equal(TestData.ValidKey, vm.CommonKey);
        Assert.True(vm.IsKeyValid);
        Assert.False(vm.RevealKey);
        Assert.Equal(ThemeChoice.Dark, vm.Theme);
        Assert.Equal("/tmp/default", vm.OutputDir);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz")]
    public void InvalidKey_ShowsErrorWithoutEchoingIt_AndIsNotSaved(string bad)
    {
        var store = new MemoryStore();
        var vm = new SettingsViewModel(store, "/tmp/d") { CommonKey = bad };
        Assert.False(vm.IsKeyValid);
        Assert.NotNull(vm.CommonKeyError);
        Assert.DoesNotContain(bad, vm.CommonKeyError);
        Assert.Equal("", store.Current.CommonKey);
    }

    [Fact]
    public void EmptyKey_HasNoErrorButIsNotValid()
    {
        var vm = new SettingsViewModel(new MemoryStore(), "/tmp/d");
        Assert.Null(vm.CommonKeyError);
        Assert.False(vm.IsKeyValid);
    }

    [Fact]
    public void ValidKey_IsTrimmedAndSaved()
    {
        var store = new MemoryStore();
        var vm = new SettingsViewModel(store, "/tmp/d") { CommonKey = "  " + TestData.ValidKey + " " };
        Assert.True(vm.IsKeyValid);
        Assert.Equal(TestData.ValidKey, store.Current.CommonKey);
        Assert.Equal(TestData.ValidKey, vm.ValidKeyOrEmpty);
    }

    [Fact]
    public void Theme_RaisesEventAndSaves()
    {
        var store = new MemoryStore();
        var vm = new SettingsViewModel(store, "/tmp/d");
        ThemeChoice? seen = null;
        vm.ThemeChanged += (_, t) => seen = t;
        vm.Theme = ThemeChoice.Light;
        Assert.Equal(ThemeChoice.Light, seen);
        Assert.Equal(ThemeChoice.Light, store.Current.Theme);
    }

    [Fact]
    public void OutputDir_InvalidIsReportedAndNotSaved()
    {
        var store = new MemoryStore();
        var vm = new SettingsViewModel(store, "/tmp/d") { OutputDir = "relative" };
        Assert.NotNull(vm.OutputDirError);
        Assert.Null(store.Current.OutputDir);
        vm.OutputDir = "/tmp/ok";
        Assert.Null(vm.OutputDirError);
        Assert.Equal("/tmp/ok", store.Current.OutputDir);
    }

    [Fact]
    public void ToggleReveal_FlipsFlag()
    {
        var vm = new SettingsViewModel(new MemoryStore(), "/tmp/d");
        vm.ToggleRevealCommand.Execute(null);
        Assert.True(vm.RevealKey);
    }

    [Fact]
    public void TitleKeys_AreSavedPerTitleId_AndReloaded()
    {
        var store = new MemoryStore();
        var vm = new SettingsViewModel(store, "/tmp/d");
        Assert.Null(vm.TitleKeyFor("0005000010000001"));
        vm.SaveTitleKey("0005000010000001", TestData.ValidKey);
        Assert.Equal(TestData.ValidKey, vm.TitleKeyFor("0005000010000001"));
        Assert.Equal(TestData.ValidKey, vm.TitleKeyFor("0005000010000001".ToUpperInvariant()));
        Assert.Equal(TestData.ValidKey, store.Current.TitleKeys["0005000010000001"]);
        Assert.Equal(TestData.ValidKey, new SettingsViewModel(store, "/tmp/d").TitleKeyFor("0005000010000001"));
        vm.Theme = ThemeChoice.Dark;
        Assert.Equal(TestData.ValidKey, store.Current.TitleKeys["0005000010000001"]);
    }
}
