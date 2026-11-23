using UWUVCI.Core.Injection;
using UWUVCI.Mac.ViewModels;

namespace UWUVCI.Mac.Tests;

public class MainViewModelTests
{
    private static MainViewModel Make(Fixture f, Action<string>? reveal = null) =>
        new(f.Settings, f.Vm) { RevealAction = reveal ?? (_ => { }) };

    [Fact]
    public void Consoles_ListEveryConsole_OnlyN64Enabled()
    {
        using var f = new Fixture();
        var vm = Make(f);
        Assert.Equal(Enum.GetValues<GameConsole>().Length, vm.Consoles.Count);
        Assert.Single(vm.Consoles, c => c.Available);
        var nes = vm.Consoles.Single(c => c.Console == GameConsole.Nes);
        Assert.False(nes.Available);
        Assert.Contains("not available in the macOS build yet", nes.Note, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SelectConsole_IgnoresUnavailable()
    {
        using var f = new Fixture();
        var vm = Make(f);
        vm.SelectConsoleCommand.Execute(vm.Consoles.Single(c => c.Console == GameConsole.Gba));
        Assert.Null(vm.SelectedConsole);
        Assert.False(vm.NextCommand.CanExecute(null));
        vm.SelectConsoleCommand.Execute(vm.Consoles.Single(c => c.Console == GameConsole.N64));
        Assert.NotNull(vm.SelectedConsole);
        Assert.True(vm.NextCommand.CanExecute(null));
    }

    [Fact]
    public void Next_WalksStepsOnlyWhenEachIsComplete()
    {
        using var f = new Fixture();
        var vm = Make(f);
        vm.SelectConsoleCommand.Execute(vm.Consoles.Single(c => c.Available));
        vm.NextCommand.Execute(null);
        Assert.Equal(WizardStep.Rom, vm.Step);
        Assert.False(vm.NextCommand.CanExecute(null));
        f.Vm.SetRom(f.Dir.File("g.z64", TestData.BanjoTooie()));
        Assert.True(vm.NextCommand.CanExecute(null));
        vm.NextCommand.Execute(null);
        Assert.Equal(WizardStep.Base, vm.Step);
        f.Vm.SetBase(f.Dir.Base());
        vm.NextCommand.Execute(null);
        f.Vm.GameName = "Name";
        vm.NextCommand.Execute(null);
        Assert.Equal(WizardStep.Options, vm.Step);
        vm.NextCommand.Execute(null);
        Assert.Equal(WizardStep.Output, vm.Step);
        Assert.True(vm.IsLastStep);
        Assert.False(vm.NextCommand.CanExecute(null));
        vm.BackCommand.Execute(null);
        Assert.Equal(WizardStep.Options, vm.Step);
    }

    [Fact]
    public void GoTo_BlockedUntilEarlierStepsAreDone()
    {
        using var f = new Fixture();
        var vm = Make(f);
        var details = vm.Steps.Single(s => s.Step == WizardStep.Details);
        Assert.False(details.IsEnabled);
        vm.GoToCommand.Execute(details);
        Assert.Equal(WizardStep.Console, vm.Step);

        vm.SelectConsoleCommand.Execute(vm.Consoles.Single(c => c.Available));
        f.MakeValid();
        Assert.True(details.IsEnabled);
        vm.GoToCommand.Execute(details);
        Assert.Equal(WizardStep.Details, vm.Step);
        Assert.True(vm.Steps.Single(s => s.Step == WizardStep.Rom).IsComplete);
        Assert.True(details.IsCurrent);
    }

    [Fact]
    public void BackFromFirstStep_IsDisabled()
    {
        using var f = new Fixture();
        Assert.False(Make(f).BackCommand.CanExecute(null));
    }

    [Fact]
    public async Task InjectAnother_ResetsToRomStep_AndRevealUsesResultPath()
    {
        using var f = new Fixture();
        string? revealed = null;
        var vm = Make(f, p => revealed = p);
        vm.SelectConsoleCommand.Execute(vm.Consoles.Single(c => c.Available));
        f.MakeValid();
        await f.Vm.InjectCommand.ExecuteAsync(null);
        Assert.True(vm.IsDone);
        vm.ShowInFinderCommand.Execute(null);
        Assert.Equal(f.Vm.ResultPath, revealed);
        vm.InjectAnotherCommand.Execute(null);
        Assert.False(vm.IsDone);
        Assert.Equal(WizardStep.Rom, vm.Step);
    }

    [Fact]
    public void Overlays_OpenAndClose_AreExclusive()
    {
        using var f = new Fixture();
        var vm = Make(f);
        vm.OpenSettingsCommand.Execute(null);
        Assert.True(vm.IsSettingsOpen);
        Assert.True(vm.IsOverlayOpen);
        vm.OpenAboutCommand.Execute(null);
        Assert.True(vm.IsAboutOpen);
        Assert.False(vm.IsSettingsOpen);
        vm.CloseOverlayCommand.Execute(null);
        Assert.False(vm.IsAboutOpen);
    }

    [Fact]
    public void Running_ReportsFlags()
    {
        using var f = new Fixture();
        var vm = Make(f);
        Assert.True(vm.IsWizard);
        Assert.False(vm.IsRunning);
    }

    [Fact]
    public void NextHint_ExplainsWhatIsMissing()
    {
        using var f = new Fixture(key: "");
        var vm = Make(f);
        Assert.Contains("Nintendo 64", vm.NextHint);
        vm.SelectConsoleCommand.Execute(vm.Consoles.Single(c => c.Available));
        Assert.Equal("", vm.NextHint);
        vm.Step = WizardStep.Rom;
        Assert.Contains("ROM", vm.NextHint);
        vm.Step = WizardStep.Base;
        Assert.Contains("base", vm.NextHint);
        vm.Step = WizardStep.Details;
        Assert.Contains("name", vm.NextHint);
        vm.Step = WizardStep.Options;
        Assert.Equal("", vm.NextHint);
        f.Vm.SetCustomIni("/nope.ini");
        Assert.Contains("ini", vm.NextHint);
        vm.Step = WizardStep.Output;
        Assert.Contains("common key", vm.NextHint);
        f.Settings.CommonKey = TestData.ValidKey;
        f.Vm.SetOutput("bad");
        Assert.Contains("output", vm.NextHint);
        f.Vm.SetOutput(Path.Combine(f.Dir.Path, "o"));
        Assert.Equal("", vm.NextHint);
    }

    [Fact]
    public void Native_BaseStepIsDoneAndNotNeeded_AndNavigationSkipsIt()
    {
        using var f = new Fixture();
        var vm = Make(f);
        vm.SelectConsoleCommand.Execute(vm.Consoles.Single(c => c.Available));
        f.MakeValidNative();
        var baseItem = vm.Steps.Single(s => s.Step == WizardStep.Base);
        Assert.True(baseItem.IsComplete);
        Assert.Equal("Not needed", baseItem.Note);
                vm.Step = WizardStep.Rom;
        vm.NextCommand.Execute(null);
        Assert.Equal(WizardStep.Details, vm.Step);
        vm.BackCommand.Execute(null);
        Assert.Equal(WizardStep.Rom, vm.Step);
        f.Vm.Engine = InjectEngine.NintendoVc;
        Assert.Equal("", baseItem.Note);
        Assert.False(baseItem.IsComplete);
    }

    [Fact]
    public async Task Native_Done_ShowsBothFolders()
    {
        using var f = new Fixture();
        var revealed = new List<string>();
        var vm = Make(f, revealed.Add);
        f.MakeValidNative();
        await f.Vm.InjectCommand.ExecuteAsync(null);
        Assert.True(vm.IsDone);
        Assert.Contains("root of your SD card", vm.SdInstruction);
        Assert.Contains("one game per SD card", vm.SdInstruction);
        vm.ShowInFinderCommand.Execute(null);
        vm.ShowSdInFinderCommand.Execute(null);
        Assert.Equal([f.Vm.ResultPath!, f.Vm.ResultSdPath!], revealed);
    }

    [Fact]
    public void ShowSdInFinder_WithoutResult_DoesNothing()
    {
        using var f = new Fixture();
        var revealed = new List<string>();
        Make(f, revealed.Add).ShowSdInFinderCommand.Execute(null);
        Assert.Empty(revealed);
    }

    [Fact]
    public void EveryConsole_HasAnIconKey()
    {
        var items = ConsoleItem.All();
        Assert.Equal(Enum.GetValues<UWUVCI.Core.Injection.GameConsole>().Length, items.Count);
        Assert.All(items, i => Assert.False(string.IsNullOrWhiteSpace(i.IconKey)));
        Assert.Equal(items.Count, items.Select(i => i.IconKey).Distinct().Count());
    }
}
