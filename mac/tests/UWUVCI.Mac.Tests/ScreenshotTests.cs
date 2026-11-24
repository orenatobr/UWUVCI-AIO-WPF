using Avalonia;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using UWUVCI.Core.Injection;
using UWUVCI.Core.N64;
using UWUVCI.Mac;
using UWUVCI.Mac.Services;
using UWUVCI.Mac.ViewModels;
using UWUVCI.Mac.Views;

namespace UWUVCI.Mac.Tests;

/// <summary>Headless Skia app builder used by the render tests.</summary>
public static class HeadlessApp
{
    /// <summary>Builds the app without a platform window system.</summary>
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}

/// <summary>
/// Renders the real window with the real XAML and checks that every page loads and paints.
/// Set UWUVCI_SCREENSHOT_DIR to keep the PNGs.
/// </summary>
public class ScreenshotTests
{
    private static readonly Lazy<HeadlessUnitTestSession> Session =
        new(() => HeadlessUnitTestSession.StartNew(typeof(HeadlessApp)));

    private static string OutDir()
    {
        var dir = Environment.GetEnvironmentVariable("UWUVCI_SCREENSHOT_DIR")
            ?? Path.Combine(Path.GetTempPath(), "uwuvci-shots");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static Task Render(string name, ThemeVariant theme, Action<Fixture, MainViewModel> arrange,
        IReadOnlyList<N64Base>? bases = null) =>
        Session.Value.Dispatch(() =>
        {
            using var f = new Fixture(bases: bases);
            var vm = new MainViewModel(f.Settings, f.Vm) { RevealAction = _ => { } };
            arrange(f, vm);
            Application.Current!.RequestedThemeVariant = theme;
            var window = new MainWindow { DataContext = vm };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            using WriteableBitmap frame = window.CaptureRenderedFrame()!;
            var path = Path.Combine(OutDir(), name + ".png");
            frame.Save(path, new PngBitmapEncoderOptions());
            window.Close();
            Assert.True(new FileInfo(path).Length > 5_000, "rendered image looks empty");
        }, CancellationToken.None);

    private static void SelectN64(MainViewModel vm) =>
        vm.SelectConsoleCommand.Execute(vm.Consoles.Single(c => c.Available));

    [Fact]
    public Task ConsolePage_Light() => Render("01-console-light", ThemeVariant.Light, (_, vm) => SelectN64(vm));

    [Fact]
    public Task RomDetected_Dark() => Render("02-rom-detected-dark", ThemeVariant.Dark, (f, vm) =>
    {
        SelectN64(vm);
        f.Vm.SetRom(f.Dir.File("bt.z64", TestData.BanjoTooie()));
        vm.Step = WizardStep.Rom;
        f.Vm.IsIniPreviewOpen = true;
    });

    [Fact]
    public Task OutputReady_Light() => Render("03-output-light", ThemeVariant.Light, (f, vm) =>
    {
        SelectN64(vm);
        f.MakeValid();
        vm.Step = WizardStep.Output;
    });

    [Fact]
    public Task Details_Dark() => Render("04-details-dark", ThemeVariant.Dark, (f, vm) =>
    {
        SelectN64(vm);
        f.MakeValid();
        vm.Step = WizardStep.Details;
    });

    [Fact]
    public Task Settings_Light() => Render("05-settings-light", ThemeVariant.Light, (f, vm) =>
    {
        SelectN64(vm);
        vm.OpenSettingsCommand.Execute(null);
    });

    [Fact]
    public Task Running_Dark() => Render("06-running-dark", ThemeVariant.Dark, (f, vm) =>
    {
        f.MakeValid();
        f.Service.Gate = new TaskCompletionSource();
        _ = f.Vm.InjectCommand.ExecuteAsync(null);
    });

    [Fact]
    public Task Done_Light() => Render("07-done-light", ThemeVariant.Light, (f, vm) =>
    {
        f.MakeValid();
        f.Vm.InjectCommand.ExecuteAsync(null).GetAwaiter().GetResult();
    });

    [Fact]
    public Task OptionsNative_Light() => Render("08-options-native-light", ThemeVariant.Light, (f, vm) =>
    {
        SelectN64(vm);
        f.MakeValidNative();
        vm.Step = WizardStep.Options;
    });

    [Fact]
    public Task DoneNative_Dark() => Render("09-done-native-dark", ThemeVariant.Dark, (f, vm) =>
    {
        f.MakeValidNative();
        f.Vm.InjectCommand.ExecuteAsync(null).GetAwaiter().GetResult();
    });

    private static readonly N64Base Demo = new("Mario Golf", "US", "00050000101a5900", "B1775A785F09A6EBAF2DC33D6EAEB98974D9CDB8");

    [Fact]
    public Task BaseKeyPrompt_Light() => Render("10-base-key-light", ThemeVariant.Light, (f, vm) =>
    {
        SelectN64(vm);
        f.Vm.SetRom(f.Dir.File("bt.z64", TestData.BanjoTooie()));
        vm.Step = WizardStep.Base;
        f.Vm.ChooseBaseAsync(f.Vm.BaseOptions.Single(o => o.Base == Demo)).GetAwaiter().GetResult();
        f.Vm.BaseKeyInput = "00000000000000000000000000000000";
    }, [Demo]);

    [Fact]
    public Task BaseDownloading_Dark() => Render("11-base-downloading-dark", ThemeVariant.Dark, (f, vm) =>
    {
        SelectN64(vm);
        f.Vm.SetRom(f.Dir.File("bt.z64", TestData.BanjoTooie()));
        vm.Step = WizardStep.Base;
        f.Settings.SaveTitleKey(Demo.TitleId, "0123456789abcdef0123456789abcdef");
        f.Service.DownloadGate = new TaskCompletionSource();
        _ = f.Vm.ChooseBaseAsync(f.Vm.BaseOptions.Single(o => o.Base == Demo));
    }, [Demo]);

    [Fact]
    public Task BaseReady_Light() => Render("12-base-ready-light", ThemeVariant.Light, (f, vm) =>
    {
        SelectN64(vm);
        f.Vm.SetRom(f.Dir.File("bt.z64", TestData.BanjoTooie()));
        vm.Step = WizardStep.Base;
        f.Dir.Base(Path.Combine("bases", Demo.FolderName));
        f.Vm.ChooseBaseAsync(f.Vm.BaseOptions.Single(o => o.Base == Demo)).GetAwaiter().GetResult();
    }, [Demo]);

    [Fact]
    public Task BaseNoCommonKey_Dark() => Render("13-base-no-common-key-dark", ThemeVariant.Dark, (f, vm) =>
    {
        SelectN64(vm);
        f.Vm.SetRom(f.Dir.File("bt.z64", TestData.BanjoTooie()));
        vm.Step = WizardStep.Base;
        f.Settings.CommonKey = "";
        f.Vm.ChooseBaseAsync(f.Vm.BaseOptions.Single(o => o.Base == Demo)).GetAwaiter().GetResult();
    }, [Demo]);

    [Fact]
    public Task ConsoleIcons_HaveGeometryAndAccentInBothThemes() => Session.Value.Dispatch(() =>
    {
        var app = Application.Current!;
        foreach (var item in ConsoleItem.All())
        {
            Assert.True(app.TryGetResource("Icon." + item.IconKey, null, out var geo), item.IconKey);
            Assert.IsAssignableFrom<Avalonia.Media.Geometry>(geo);
            foreach (var theme in new[] { ThemeVariant.Light, ThemeVariant.Dark })
                Assert.True(app.TryGetResource("Console." + item.IconKey, theme, out _), item.IconKey + " " + theme);
        }
    }, CancellationToken.None);

    [Fact]
    public Task ConsolePage_Dark() => Render("14-console-dark", ThemeVariant.Dark, (_, vm) => SelectN64(vm));

    [Fact]
    public Task NavigationButtons_HaveMacShortcuts() => Session.Value.Dispatch(() =>
    {
        using var f = new Fixture();
        var vm = new MainViewModel(f.Settings, f.Vm) { RevealAction = _ => { } };
        var window = new MainWindow { DataContext = vm };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var buttons = Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(window).OfType<Avalonia.Controls.Button>().ToList();
        var wanted = new Dictionary<string, string> { ["Back"] = "Cmd+Left", ["Continue"] = "Cmd+Return", ["Inject"] = "Cmd+Return" };
        foreach (var (label, gesture) in wanted)
        {
            var b = buttons.Single(x => x.Content as string == label);
            Assert.Equal(Avalonia.Input.KeyGesture.Parse(gesture), b.HotKey);
        }
        Assert.Contains(buttons, x => x.HotKey == Avalonia.Input.KeyGesture.Parse("Cmd+OemComma"));
        window.Close();
    }, CancellationToken.None);
}
