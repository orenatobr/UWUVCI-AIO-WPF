using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UWUVCI.Mac.Services;

namespace UWUVCI.Mac.ViewModels;

/// <summary>State of the Settings sheet. Valid changes are saved immediately.</summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsStore _store;
    private readonly string _defaultOutputDir;
    private readonly bool _loading;

    /// <summary>Loads the stored settings.</summary>
    /// <param name="store">Where settings live.</param>
    /// <param name="defaultOutputDir">Output folder used when none was stored.</param>
    public SettingsViewModel(ISettingsStore store, string defaultOutputDir)
    {
        _store = store;
        _defaultOutputDir = defaultOutputDir;
        _loading = true;
        var s = store.Load();
        CommonKey = s.CommonKey;
        OutputDir = s.OutputDir ?? defaultOutputDir;
        Theme = s.Theme;
        _loading = false;
    }

    /// <summary>Raised when the theme override changes.</summary>
    public event EventHandler<ThemeChoice>? ThemeChanged;

    /// <summary>The Wii U common key as typed. Never logged.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CommonKeyError), nameof(IsKeyValid), nameof(ValidKeyOrEmpty))]
    private string _commonKey = "";

    /// <summary>Whether the key field shows the characters instead of dots.</summary>
    [ObservableProperty]
    private bool _revealKey;

    /// <summary>Default output folder.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OutputDirError))]
    private string _outputDir = "";

    /// <summary>Theme override.</summary>
    [ObservableProperty]
    private ThemeChoice _theme;

    /// <summary>Theme choices for the picker.</summary>
    public ThemeChoice[] Themes { get; } = Enum.GetValues<ThemeChoice>();

    /// <summary>True when the key is exactly 32 hex characters.</summary>
    public bool IsKeyValid => InputGuard.IsHex(CommonKey.Trim(), 32);

    /// <summary>The trimmed key when valid, otherwise an empty string.</summary>
    public string ValidKeyOrEmpty => IsKeyValid ? CommonKey.Trim() : "";

    /// <summary>Validation message, null when the field is empty or valid. Does not repeat the input.</summary>
    public string? CommonKeyError => CommonKey.Length == 0 || IsKeyValid
        ? null
        : "The common key is 32 hexadecimal characters (0 to 9, a to f).";

    /// <summary>Validation message for the output folder.</summary>
    public string? OutputDirError => InputGuard.CheckOutputFolder(OutputDir).Error;

    partial void OnCommonKeyChanged(string value) => Persist();

    partial void OnOutputDirChanged(string value) => Persist();

    partial void OnThemeChanged(ThemeChoice value)
    {
        if (_loading) return;
        ThemeChanged?.Invoke(this, value);
        Persist();
    }

    /// <summary>Stores a new default output folder if it is valid (used when the user picks one while injecting).</summary>
    public void RememberOutputDir(string dir)
    {
        if (InputGuard.CheckOutputFolder(dir).IsValid) OutputDir = dir;
    }

    [RelayCommand]
    private void ToggleReveal() => RevealKey = !RevealKey;

    private void Persist()
    {
        if (_loading) return;
        // Invalid values stay in the form but are never written, so a half typed key is not stored.
        _store.Save(new AppSettings
        {
            CommonKey = CommonKey.Length == 0 ? "" : IsKeyValid ? CommonKey.Trim() : _store.Load().CommonKey,
            OutputDir = OutputDirError is null ? OutputDir : _store.Load().OutputDir,
            Theme = Theme,
        });
    }
}
