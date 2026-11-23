using UWUVCI.Mac.ViewModels;

namespace UWUVCI.Mac.Tests;

public class InjectViewModelTests
{
    [Fact]
    public void NewViewModel_CannotInject()
    {
        using var f = new Fixture();
        Assert.False(f.Vm.CanInject);
        Assert.False(f.Vm.InjectCommand.CanExecute(null));
        Assert.Equal(PakChoice.Auto, f.Vm.PakChoice);
        Assert.Equal(InjectPhase.Editing, f.Vm.Phase);
    }

    [Fact]
    public void SetRom_KnownGame_FillsDetectionCard()
    {
        using var f = new Fixture();
        f.Vm.SetRom(f.Dir.File("bt.z64", TestData.BanjoTooie()));
        Assert.True(f.Vm.IsRomValid);
        Assert.Null(f.Vm.RomError);
        Assert.Contains("Banjo-Tooie", f.Vm.RomName);
        Assert.Equal("Not enabled", f.Vm.RomExpansionPak);
        Assert.Contains("Rumble", f.Vm.RomControllers);
        Assert.Contains("EEPROM", f.Vm.RomSave);
        Assert.Contains("[RomOption]", f.Vm.IniPreview);
        Assert.Equal("United States", f.Vm.RomRegion);
    }

    [Fact]
    public void SetRom_UnknownGame_SaysSo()
    {
        using var f = new Fixture();
        f.Vm.SetRom(f.Dir.File("x.z64", TestData.Rom(1, 2, "HOMEBREW")));
        Assert.True(f.Vm.IsRomValid);
        Assert.Equal("HOMEBREW", f.Vm.RomName);
        Assert.False(f.Vm.RomInDatabase);
    }

    [Fact]
    public void SetRom_Invalid_ClearsDetectionAndShowsError()
    {
        using var f = new Fixture();
        f.Vm.SetRom(f.Dir.File("ok.z64", TestData.BanjoTooie()));
        f.Vm.SetRom(f.Dir.File("bad.z64", new byte[64]));
        Assert.False(f.Vm.IsRomValid);
        Assert.NotNull(f.Vm.RomError);
        Assert.Equal("", f.Vm.IniPreview);
    }

    [Fact]
    public void PakChoice_On_UpdatesPreviewAndStatus()
    {
        using var f = new Fixture();
        f.Vm.SetRom(f.Dir.File("bt.z64", TestData.BanjoTooie()));
        f.Vm.PakChoice = PakChoice.On;
        Assert.Contains("RamSize = 0x800000", f.Vm.IniPreview);
        Assert.Equal("Enabled (experimental)", f.Vm.RomExpansionPak);
    }

    [Fact]
    public void SetBase_ValidatesFolder()
    {
        using var f = new Fixture();
        f.Vm.SetBase(f.Dir.Path);
        Assert.False(f.Vm.IsBaseValid);
        Assert.NotNull(f.Vm.BaseError);
        f.Vm.SetBase(f.Dir.Base());
        Assert.True(f.Vm.IsBaseValid);
        Assert.Null(f.Vm.BaseError);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("Banjo|Tooie", true)]
    [InlineData("A|B|C", false)]
    [InlineData("Line\nBreak", false)]
    public void GameName_Validation(string name, bool ok)
    {
        using var f = new Fixture();
        f.Vm.GameName = name;
        Assert.Equal(ok, f.Vm.IsDetailsValid);
    }

    [Fact]
    public void GameName_TooLong_IsRejected()
    {
        using var f = new Fixture();
        f.Vm.GameName = new string('a', 101);
        Assert.NotNull(f.Vm.GameNameError);
    }

    [Fact]
    public void ImageSlot_ValidInvalidAndClear()
    {
        using var f = new Fixture();
        f.Vm.GameName = "x";
        f.Vm.Icon.Set(f.Dir.File("i.png", TestData.Png()));
        Assert.True(f.Vm.Icon.HasImage);
        Assert.True(f.Vm.IsDetailsValid);
        f.Vm.Logo.Set(f.Dir.File("l.png", [1, 2, 3, 4, 5, 6, 7, 8]));
        Assert.NotNull(f.Vm.Logo.Error);
        Assert.False(f.Vm.IsDetailsValid);
        f.Vm.Logo.Clear();
        Assert.True(f.Vm.IsDetailsValid);
        f.Vm.Icon.ClearCommand.Execute(null);
        Assert.False(f.Vm.Icon.HasImage);
    }

    [Fact]
    public void CustomIni_ValidatedAndShownInPreview()
    {
        using var f = new Fixture();
        f.Vm.SetRom(f.Dir.File("bt.z64", TestData.BanjoTooie()));
        f.Vm.SetCustomIni(f.Dir.File("my.ini", "[RomOption]\nBackupType = 1\n"u8.ToArray()));
        Assert.True(f.Vm.IsOptionsValid);
        Assert.Contains("BackupType = 1", f.Vm.IniPreview);
        f.Vm.SetCustomIni(Path.Combine(f.Dir.Path, "missing.ini"));
        Assert.False(f.Vm.IsOptionsValid);
        f.Vm.SetCustomIni(null);
        Assert.True(f.Vm.IsOptionsValid);
        Assert.Contains("TrueBoot", f.Vm.IniPreview);
    }

    [Fact]
    public void SetOutput_RejectsRelativePath()
    {
        using var f = new Fixture();
        f.Vm.SetOutput("relative/path");
        Assert.False(f.Vm.IsOutputValid);
        Assert.NotNull(f.Vm.OutputError);
    }

    [Fact]
    public void Output_DefaultsFromSettings()
    {
        using var f = new Fixture();
        Assert.Equal(f.Settings.OutputDir, f.Vm.OutputDir);
        Assert.True(f.Vm.IsOutputValid);
    }

    [Fact]
    public void CanInject_RequiresEverything()
    {
        using var f = new Fixture();
        f.MakeValid();
        Assert.True(f.Vm.CanInject);
        Assert.True(f.Vm.InjectCommand.CanExecute(null));

        f.Vm.GameName = "";
        Assert.False(f.Vm.CanInject);
        f.Vm.GameName = "Ok";
        Assert.True(f.Vm.CanInject);

        f.Vm.SetBase(f.Dir.Path);
        Assert.False(f.Vm.CanInject);
        f.Vm.SetBase(f.Dir.Base());

        f.Vm.SetRom(null);
        Assert.False(f.Vm.CanInject);
        f.Vm.SetRom(f.Dir.File("game.z64", TestData.BanjoTooie()));

        f.Vm.SetOutput("nope");
        Assert.False(f.Vm.CanInject);
        f.Vm.SetOutput(Path.Combine(f.Dir.Path, "out"));
        Assert.True(f.Vm.CanInject);

        f.Settings.CommonKey = "bad";
        Assert.False(f.Vm.CanInject);
        Assert.False(f.Vm.HasCommonKey);
    }

    [Fact]
    public async Task Inject_SendsRequestAndFinishes()
    {
        using var f = new Fixture();
        f.MakeValid();
        f.Vm.WideScreen = true;
        f.Vm.DarkFilter = true;
        f.Vm.PakChoice = PakChoice.Off;
        f.Vm.Icon.Set(f.Dir.File("i.png", TestData.Png()));

        await f.Vm.InjectCommand.ExecuteAsync(null);

        var r = f.Service.Request!;
        Assert.Equal(UWUVCI.Core.Injection.GameConsole.N64, r.Console);
        Assert.Equal("Banjo|Tooie", r.GameName);
        Assert.True(r.N64.WideScreen);
        Assert.True(r.N64.DarkFilter);
        Assert.False(r.N64.ExpansionPak);
        Assert.NotNull(r.IconPath);
        Assert.Null(r.LogoPath);
        Assert.Equal(TestData.ValidKey, f.Service.CommonKeyUsed);
        Assert.Equal(InjectPhase.Done, f.Vm.Phase);
        Assert.EndsWith("[WUP][N64] Result", f.Vm.ResultPath);
        Assert.Equal(100, f.Vm.Percent);
        Assert.Equal("Done", f.Vm.ProgressMessage);
    }

    [Theory]
    [InlineData(PakChoice.Auto, null)]
    [InlineData(PakChoice.On, true)]
    [InlineData(PakChoice.Off, false)]
    public async Task Inject_MapsPakChoice(PakChoice choice, bool? expected)
    {
        using var f = new Fixture();
        f.MakeValid();
        f.Vm.PakChoice = choice;
        await f.Vm.InjectCommand.ExecuteAsync(null);
        Assert.Equal(expected, f.Service.Request!.N64.ExpansionPak);
    }

    [Fact]
    public async Task Inject_Failure_ReturnsToEditingWithoutKeyInMessage()
    {
        using var f = new Fixture();
        f.MakeValid();
        f.Service.Fail = new InvalidDataException("The base has no ROM in content/rom.");
        await f.Vm.InjectCommand.ExecuteAsync(null);
        Assert.Equal(InjectPhase.Editing, f.Vm.Phase);
        Assert.Contains("no ROM", f.Vm.ErrorMessage);
        Assert.DoesNotContain(TestData.ValidKey, f.Vm.ErrorMessage);
    }

    [Fact]
    public async Task Inject_UnexpectedFailure_ShowsGenericMessage()
    {
        using var f = new Fixture();
        f.MakeValid();
        f.Service.Fail = new InvalidOperationException("boom " + TestData.ValidKey);
        await f.Vm.InjectCommand.ExecuteAsync(null);
        Assert.DoesNotContain(TestData.ValidKey, f.Vm.ErrorMessage);
        Assert.Equal(InjectPhase.Editing, f.Vm.Phase);
    }

    [Fact]
    public async Task Cancel_StopsRunningInject()
    {
        using var f = new Fixture();
        f.MakeValid();
        f.Service.Gate = new TaskCompletionSource();
        var run = f.Vm.InjectCommand.ExecuteAsync(null);
        Assert.Equal(InjectPhase.Running, f.Vm.Phase);
        Assert.Equal(40, f.Vm.Percent);
        Assert.Equal("Halfway", f.Vm.ProgressMessage);
        f.Vm.CancelCommand.Execute(null);
        await run;
        Assert.Equal(InjectPhase.Editing, f.Vm.Phase);
        Assert.Equal("Cancelled.", f.Vm.ErrorMessage);
    }

    [Fact]
    public async Task Reset_ClearsJobButKeepsBaseAndOutput()
    {
        using var f = new Fixture();
        f.MakeValid();
        await f.Vm.InjectCommand.ExecuteAsync(null);
        f.Vm.Reset();
        Assert.Equal(InjectPhase.Editing, f.Vm.Phase);
        Assert.False(f.Vm.IsRomValid);
        Assert.Equal("", f.Vm.GameName);
        Assert.True(f.Vm.IsBaseValid);
        Assert.True(f.Vm.IsOutputValid);
        Assert.Null(f.Vm.ResultPath);
    }

    [Fact]
    public void Engine_DefaultsToNintendoVc_AndNeedsBase()
    {
        using var f = new Fixture();
        Assert.Equal(InjectEngine.NintendoVc, f.Vm.Engine);
        Assert.True(f.Vm.ShowVcOptions);
        f.MakeValid();
        f.Vm.SetBase(null);
        Assert.False(f.Vm.CanInject);
    }

    [Fact]
    public void Native_CanInjectWithoutBase()
    {
        using var f = new Fixture();
        f.MakeValidNative();
        Assert.True(f.Vm.IsNative);
        Assert.False(f.Vm.IsBaseRequired);
        Assert.False(f.Vm.ShowVcOptions);
        Assert.True(f.Vm.CanInject);
    }

    [Fact]
    public void Native_IgnoresBadCustomIni()
    {
        using var f = new Fixture();
        f.MakeValidNative();
        f.Vm.SetCustomIni(f.Dir.File("bad.bin", new byte[] { 0, 1, 2 }));
        Assert.True(f.Vm.IsOptionsValid);
        f.Vm.Engine = InjectEngine.NintendoVc;
        Assert.False(f.Vm.IsOptionsValid);
    }

    [Fact]
    public void EngineRadioProxies_SwitchEngine()
    {
        using var f = new Fixture();
        f.Vm.IsNative = true;
        Assert.Equal(InjectEngine.Native, f.Vm.Engine);
        Assert.False(f.Vm.IsNintendoVc);
        f.Vm.IsNintendoVc = true;
        Assert.Equal(InjectEngine.NintendoVc, f.Vm.Engine);
        f.Vm.IsNative = false;
        Assert.True(f.Vm.IsNintendoVc);
    }

    [Fact]
    public async Task Native_Inject_CallsNativeService_WithDefaultN64Options()
    {
        using var f = new Fixture();
        f.MakeValidNative();
        f.Vm.WideScreen = true;
        f.Vm.DarkFilter = true;
        f.Vm.PakChoice = PakChoice.On;
        await f.Vm.InjectCommand.ExecuteAsync(null);
        Assert.Null(f.Service.Request);
        Assert.NotNull(f.Service.NativeRequest);
        Assert.False(f.Service.NativeRequest!.N64.WideScreen);
        Assert.False(f.Service.NativeRequest.N64.DarkFilter);
        Assert.Null(f.Service.NativeRequest.N64.ExpansionPak);
        Assert.Equal(InjectPhase.Done, f.Vm.Phase);
        Assert.EndsWith("Result Native", f.Vm.ResultPath);
        Assert.EndsWith("Result Native SD", f.Vm.ResultSdPath);
        Assert.True(f.Vm.HasSdResult);
    }

    [Fact]
    public async Task Vc_Inject_HasNoSdResult()
    {
        using var f = new Fixture();
        f.MakeValid();
        await f.Vm.InjectCommand.ExecuteAsync(null);
        Assert.NotNull(f.Service.Request);
        Assert.Null(f.Service.NativeRequest);
        Assert.Null(f.Vm.ResultSdPath);
        Assert.False(f.Vm.HasSdResult);
    }

    [Fact]
    public async Task Native_Failure_ReturnsToEditing()
    {
        using var f = new Fixture();
        f.MakeValidNative();
        f.Service.Fail = new InvalidDataException("template broke");
        await f.Vm.InjectCommand.ExecuteAsync(null);
        Assert.Equal(InjectPhase.Editing, f.Vm.Phase);
        Assert.Equal("template broke", f.Vm.ErrorMessage);
        Assert.Null(f.Vm.ResultSdPath);
    }

    [Fact]
    public async Task Reset_ClearsSdResult_KeepsEngine()
    {
        using var f = new Fixture();
        f.MakeValidNative();
        await f.Vm.InjectCommand.ExecuteAsync(null);
        f.Vm.Reset();
        Assert.Null(f.Vm.ResultSdPath);
        Assert.Equal(InjectEngine.Native, f.Vm.Engine);
    }
}
