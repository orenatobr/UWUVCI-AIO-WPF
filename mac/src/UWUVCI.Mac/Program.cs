using Avalonia;

namespace UWUVCI.Mac;

/// <summary>Entry point.</summary>
public static class Program
{
    /// <summary>Starts the desktop app.</summary>
    [STAThread]
    public static int Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    /// <summary>Builds the Avalonia app. Also used by the headless screenshot test.</summary>
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect();
}
