using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Microsoft.Extensions.Logging;
using UWUVCI.Mac.Services;
using UWUVCI.Mac.ViewModels;
using UWUVCI.Mac.Views;

namespace UWUVCI.Mac;

/// <summary>Application root: wires services, view models and the theme.</summary>
public sealed class App : Application
{
    /// <summary>Replaces the real services, used by the headless screenshot test.</summary>
    public static Func<MainViewModel>? ViewModelFactory { get; set; }

    /// <inheritdoc />
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = (ViewModelFactory ?? CreateViewModel)();
            ApplyTheme(vm.Settings.Theme);
            vm.Settings.ThemeChanged += (_, t) => ApplyTheme(t);
            desktop.MainWindow = new MainWindow { DataContext = vm };
        }
        base.OnFrameworkInitializationCompleted();
    }

    private void ApplyTheme(ThemeChoice t) => RequestedThemeVariant = t switch
    {
        ThemeChoice.Light => ThemeVariant.Light,
        ThemeChoice.Dark => ThemeVariant.Dark,
        _ => ThemeVariant.Default,
    };

    private static MainViewModel CreateViewModel()
    {
        var logs = LoggerFactory.Create(b => b.AddProvider(new FileLoggerProvider(AppPaths.LogFile)));
        var settings = new SettingsViewModel(new JsonSettingsStore(AppPaths.SettingsFile), AppPaths.DefaultOutputDir);
        var inject = new InjectViewModel(new InjectService(), settings, AppPaths.BasesDir, logs.CreateLogger<InjectViewModel>());
        logs.CreateLogger<App>().LogInformation("UWUVCI for macOS started");
        return new MainViewModel(settings, inject);
    }
}
