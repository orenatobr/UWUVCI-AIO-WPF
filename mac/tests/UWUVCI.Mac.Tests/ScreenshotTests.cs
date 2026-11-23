using Avalonia;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using UWUVCI.Core.Injection;
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

    private static Task Render(string name, ThemeVariant theme, Action<Fixture, MainViewModel> arrange) =>
        Session.Value.Dispatch(() =>
        {
            using var f = new Fixture();
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
}
