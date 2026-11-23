using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using UWUVCI.Core.Injection;
using UWUVCI.Core.N64;
using UWUVCI.Mac.Services;

namespace UWUVCI.Mac.ViewModels;

/// <summary>Where the inject job is.</summary>
public enum InjectPhase
{
    /// <summary>The user is filling in the form.</summary>
    Editing,
    /// <summary>An inject is running.</summary>
    Running,
    /// <summary>The inject finished.</summary>
    Done,
}

/// <summary>Expansion Pak choice: follow the game profile, force on or force off.</summary>
public enum PakChoice
{
    /// <summary>Follow the game profile.</summary>
    Auto,
    /// <summary>Force the Expansion Pak on.</summary>
    On,
    /// <summary>Force the Expansion Pak off.</summary>
    Off,
}

/// <summary>Which emulator runs the game on the Wii U.</summary>
public enum InjectEngine
{
    /// <summary>The official Virtual Console emulator inside a base title the user supplies.</summary>
    NintendoVc,
    /// <summary>The not64 emulator packed as its own title, with the ROM on the SD card.</summary>
    Native,
}

/// <summary>One entry of the base dropdown. A null base is the "Choose a base..." placeholder.</summary>
/// <param name="Base">The base, or null for the placeholder.</param>
public sealed record BaseOption(N64Base? Base)
{
    /// <summary>Text shown in the dropdown.</summary>
    public string Label => Base?.DisplayName ?? "Choose a base...";
}

/// <summary>Everything the user decides for one N64 inject, plus the run itself.</summary>
public sealed partial class InjectViewModel : ObservableObject
{
    private const int MaxNameLength = 100;
    private const int MaxIniPreviewChars = 8000;

    private readonly IInjectService _service;
    private readonly SettingsViewModel _settings;
    private readonly string _basesRoot;
    private readonly BaseOption[] _options;
    private readonly ILogger _log;
    private CancellationTokenSource? _cts;
    private RomDetection? _detection;
    private string? _customIniText;

    /// <summary>Creates the view model.</summary>
    /// <param name="service">Runs injects and downloads.</param>
    /// <param name="settings">Source of the common key and the remembered output folder.</param>
    /// <param name="basesRoot">Folder that receives downloaded bases.</param>
    /// <param name="log">Optional logger.</param>
    /// <param name="bases">The bases offered in the dropdown, the embedded N64 list by default.</param>
    public InjectViewModel(IInjectService service, SettingsViewModel settings, string basesRoot, ILogger? log = null,
        IReadOnlyList<N64Base>? bases = null)
    {
        _options =
        [
            new BaseOption(null),
            .. (bases ?? N64Bases.All)
                .OrderBy(b => b.Name, StringComparer.OrdinalIgnoreCase).ThenBy(b => b.Region, StringComparer.Ordinal)
                .Select(b => new BaseOption(b)),
        ];
        _selectedOption = _options[0];
        _service = service;
        _settings = settings;
        _basesRoot = basesRoot;
        _log = log ?? NullLogger.Instance;
        Icon = new ImageSlot("Icon", "Shown in the Wii U menu, 128 x 128");
        TvBoot = new ImageSlot("TV boot screen", "1280 x 720");
        DrcBoot = new ImageSlot("GamePad boot screen", "854 x 480, defaults to the TV image");
        Logo = new ImageSlot("Logo", "170 x 42");
        foreach (var slot in ImageSlots) slot.Changed += (_, _) => Revalidate();
        _outputDir = settings.OutputDir;
        settings.PropertyChanged += OnSettingsChanged;
    }

    // ---- ROM ----

    /// <summary>Why the chosen ROM was refused, or null.</summary>
    [ObservableProperty]
    private string? _romError;

    /// <summary>True while the ini preview panel is expanded.</summary>
    [ObservableProperty]
    private bool _isIniPreviewOpen;

    /// <summary>Expansion Pak choice.</summary>
    [ObservableProperty]
    private PakChoice _pakChoice;

    /// <summary>Choices for the Expansion Pak control.</summary>
    public PakChoice[] PakChoices { get; } = Enum.GetValues<PakChoice>();

    /// <summary>The detected ROM, or null.</summary>
    public RomDetection? Detection => _detection;

    /// <summary>True when a valid ROM is loaded.</summary>
    public bool IsRomValid => _detection is not null;

    /// <summary>Alias of <see cref="IsRomValid"/> for bindings that read better as "has".</summary>
    public bool HasRom => IsRomValid;

    /// <summary>File name of the ROM.</summary>
    public string RomFileName => _detection is null ? "" : Path.GetFileName(_detection.Path);

    /// <summary>Game name from the database or ROM header.</summary>
    public string RomName => _detection?.Name ?? "";

    /// <summary>True when the ROM was found in the game database.</summary>
    public bool RomInDatabase => _detection?.InDatabase ?? false;

    /// <summary>Region in words.</summary>
    public string RomRegion => _detection is null ? "" : _detection.Profile(null).Region switch
    {
        "US" => "United States",
        "JP" => "Japan",
        _ => "Europe",
    };

    /// <summary>Save hardware in words.</summary>
    public string RomSave => _detection?.Game?.Save switch
    {
        null => _detection is null ? "" : "Unknown, chosen automatically",
        SaveKind.None => "No save memory",
        SaveKind.Eeprom4K => "EEPROM 4 Kbit",
        SaveKind.Eeprom16K => "EEPROM 16 Kbit",
        SaveKind.Sram => "SRAM",
        SaveKind.Flash => "Flash RAM",
        _ => "",
    };

    /// <summary>Which controller accessory the ini plugs in.</summary>
    public string RomControllers
    {
        get
        {
            if (_detection is null) return "";
            var p = _detection.Profile(PakValue);
            return p.Rumble ? "Rumble Pak" : p.MemPack ? "Controller Pak" : "None";
        }
    }

    /// <summary>Whether the ini enables the Expansion Pak.</summary>
    public string RomExpansionPak => _detection is null ? ""
        : _customIniText is not null ? "Defined by your ini"
        : _detection.Profile(PakValue).ExpansionPak ? "Enabled (experimental)" : "Not enabled";

    /// <summary>The ini that will be written, read only.</summary>
    public string IniPreview => _customIniText ?? _detection?.BuildIni(PakValue) ?? "";

    private bool? PakValue => PakChoice switch { PakChoice.On => true, PakChoice.Off => false, _ => null };

    /// <summary>Detects an N64 ROM and fills the detection card.</summary>
    public void SetRom(string? path)
    {
        _detection = RomDetector.Detect(path, out var error);
        RomError = _detection is null ? error ?? "Choose a ROM." : null;
        Revalidate();
    }

    partial void OnPakChoiceChanged(PakChoice value) => Revalidate();

    // ---- Engine ----

    /// <summary>Which engine runs the game.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNative), nameof(IsNintendoVc), nameof(IsBaseRequired), nameof(ShowVcOptions))]
    private InjectEngine _engine;

    /// <summary>True for the native (not64) engine. Setting true selects it; false is ignored (radio group).</summary>
    public bool IsNative
    {
        get => Engine == InjectEngine.Native;
        set { if (value) Engine = InjectEngine.Native; }
    }

    /// <summary>True for the Nintendo Virtual Console engine. Setting true selects it; false is ignored.</summary>
    public bool IsNintendoVc
    {
        get => Engine == InjectEngine.NintendoVc;
        set { if (value) Engine = InjectEngine.NintendoVc; }
    }

    /// <summary>The Virtual Console engine needs a base title, the native one does not.</summary>
    public bool IsBaseRequired => !IsNative;

    /// <summary>Widescreen, dark filter, Expansion Pak and custom ini only exist in the VC engine.</summary>
    public bool ShowVcOptions => !IsNative;

    partial void OnEngineChanged(InjectEngine value) => Revalidate();

    // ---- Base ----

    /// <summary>Canonical path of the decrypted base, or empty.</summary>
    [ObservableProperty]
    private string _baseDir = "";

    /// <summary>Why the base was refused or the download failed.</summary>
    [ObservableProperty]
    private string? _baseError;

    /// <summary>The dropdown entries: a placeholder, then the known bases sorted by name and region.</summary>
    public IReadOnlyList<BaseOption> BaseOptions => _options;

    /// <summary>The dropdown selection. The placeholder when none or when a custom folder is used.</summary>
    [ObservableProperty]
    private BaseOption _selectedOption;

    /// <summary>The title key typed for the selected base. A secret: never logged.</summary>
    [ObservableProperty]
    private string _baseKeyInput = "";

    /// <summary>True while a download runs.</summary>
    [ObservableProperty]
    private bool _isDownloading;

    /// <summary>True when the base folder is usable.</summary>
    public bool IsBaseValid => BaseDir.Length > 0;

    /// <summary>True when the base step is satisfied: a valid base, or none needed.</summary>
    public bool IsBaseSatisfied => IsNative || IsBaseValid;

    /// <summary>The selected known base, or null.</summary>
    public N64Base? SelectedBase => SelectedOption.Base;

    /// <summary>True when the ready base is a folder the user picked rather than a dropdown entry.</summary>
    public bool IsCustomBase => IsBaseValid && SelectedBase is null;

    private N64Base? Pending => SelectedBase is not null && !IsBaseValid && !IsDownloading ? SelectedBase : null;

    /// <summary>The saved key for the selected base when it still matches the hash, otherwise null.</summary>
    private string? SavedKey()
    {
        var b = SelectedBase;
        var key = b is null ? null : _settings.TitleKeyFor(b.TitleId);
        return b is not null && TitleKey.IsValid(b, key) ? key : null;
    }

    private string? TypedKey => SelectedBase is { } b && TitleKey.IsValid(b, BaseKeyInput.Trim()) ? BaseKeyInput.Trim() : null;

    /// <summary>True when the inline title key field should show: a base is chosen, not ready, and no key is saved.</summary>
    public bool ShowKeyField => Pending is not null && SavedKey() is null;

    /// <summary>True when the Download button should show.</summary>
    public bool ShowDownload => Pending is not null;

    /// <summary>"Try again" after a failed or cancelled download, otherwise "Download".</summary>
    public string DownloadButtonText => BaseError is null ? "Download" : "Try again";

    /// <summary>True when the selected base cannot download because the common key is missing.</summary>
    public bool ShowCommonKeyHint => Pending is not null && !HasCommonKey;

    /// <summary>Live validation of the typed title key, or null when empty or valid. Never repeats the input.</summary>
    public string? BaseKeyError
    {
        get
        {
            var typed = BaseKeyInput.Trim();
            if (typed.Length == 0 || SelectedBase is null || TypedKey is not null) return null;
            return InputGuard.IsHex(typed, 32)
                ? "This key does not match the selected base."
                : "The title key is 32 hexadecimal characters.";
        }
    }

    partial void OnSelectedOptionChanged(BaseOption value) => Revalidate();

    partial void OnBaseKeyInputChanged(string value) => Revalidate();

    partial void OnIsDownloadingChanged(bool value) => Revalidate();

    /// <summary>Checks a decrypted base folder (code, content, meta) the user picked. Resets the dropdown.</summary>
    public void SetBase(string? path)
    {
        SelectedOption = _options[0];
        BaseKeyInput = "";
        UseBase(path);
    }

    private void UseBase(string? path)
    {
        var r = InputGuard.CheckBaseFolder(path);
        BaseDir = r.Path ?? "";
        BaseError = r.Error;
        Revalidate();
    }

    /// <summary>
    /// Handles a pick in the dropdown: uses a copy already on disk, downloads when a matching key is saved,
    /// otherwise waits for the user to type the key.
    /// </summary>
    public async Task ChooseBaseAsync(BaseOption? option)
    {
        if (option is null || ReferenceEquals(option, SelectedOption) || IsDownloading) return;
        SelectedOption = option;
        BaseKeyInput = "";
        BaseDir = "";
        BaseError = null;
        Revalidate();
        if (option.Base is not { } b) return;

        var cached = Path.Combine(_basesRoot, b.FolderName);
        if (Directory.Exists(cached) && InputGuard.CheckBaseFolder(cached).IsValid)
        {
            UseBase(cached);
            return;
        }
        if (SavedKey() is { } key && HasCommonKey) await DownloadAsync(b, key).ConfigureAwait(true);
    }

    private bool CanDownloadSelected() =>
        Pending is not null && HasCommonKey && (SavedKey() ?? TypedKey) is not null;

    /// <summary>Downloads the selected base with the saved or typed key, saving a typed key first.</summary>
    [RelayCommand(CanExecute = nameof(CanDownloadSelected))]
    private async Task DownloadSelectedBaseAsync()
    {
        var b = SelectedBase!;
        var key = SavedKey();
        if (key is null)
        {
            key = TypedKey!;
            _settings.SaveTitleKey(b.TitleId, key);
            BaseKeyInput = "";
        }
        await DownloadAsync(b, key).ConfigureAwait(true);
    }

    private async Task DownloadAsync(N64Base b, string key)
    {
        var commonKey = _settings.ValidKeyOrEmpty;
        var final = Path.Combine(_basesRoot, b.FolderName);
        // Download beside the final folder and move it in only when complete, so a failed or
        // cancelled attempt is never mistaken for a ready base.
        var partial = final + ".partial";
        IsDownloading = true;
        BaseError = null;
        Percent = 0;
        ProgressMessage = "Starting download...";
        var cts = _cts = new CancellationTokenSource();
        try
        {
            _log.LogInformation("Downloading base {TitleId}", b.TitleId);
            if (Directory.Exists(partial)) Directory.Delete(partial, true);
            await _service.DownloadBaseAsync(b.TitleId.ToUpperInvariant(), key, commonKey, partial, NewProgress(), cts.Token)
                .ConfigureAwait(true);
            if (Directory.Exists(final)) Directory.Delete(final, true);
            Directory.Move(partial, final);
            UseBase(final);
            _log.LogInformation("Base {TitleId} ready", b.TitleId);
        }
        catch (OperationCanceledException)
        {
            BaseError = "Download cancelled.";
        }
        catch (Exception e)
        {
            BaseError = Describe(e, "download", key, commonKey);
        }
        finally
        {
            IsDownloading = false;
            cts.Dispose();
            _cts = null;
            TryDeleteDirectory(partial);
            Revalidate();
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Best effort: the next download clears it first.
        }
    }

    // ---- Details ----

    /// <summary>Game name; use | for a second line.</summary>
    [ObservableProperty]
    private string _gameName = "";

    /// <summary>Menu icon.</summary>
    public ImageSlot Icon { get; }

    /// <summary>TV boot screen.</summary>
    public ImageSlot TvBoot { get; }

    /// <summary>GamePad boot screen.</summary>
    public ImageSlot DrcBoot { get; }

    /// <summary>Boot logo.</summary>
    public ImageSlot Logo { get; }

    /// <summary>All image slots in display order.</summary>
    public ImageSlot[] ImageSlots => [Icon, TvBoot, DrcBoot, Logo];

    /// <summary>Validation message for the name, or null (also null while empty).</summary>
    public string? GameNameError => GameName.Any(char.IsControl)
        ? "The name cannot contain line breaks. Use | for a second line."
        : GameName.Count(c => c == '|') > 1 ? "Use | at most once, for a second line."
        : GameName.Length > MaxNameLength ? $"Keep the name under {MaxNameLength} characters."
        : null;

    /// <summary>True when the name and the images are acceptable.</summary>
    public bool IsDetailsValid =>
        GameName.Trim().Length > 0 && GameNameError is null && ImageSlots.All(s => s.Error is null);

    partial void OnGameNameChanged(string value) => Revalidate();

    // ---- Options ----

    /// <summary>Stretch to 16:9.</summary>
    [ObservableProperty]
    private bool _wideScreen;

    /// <summary>Remove the dark filter overlay.</summary>
    [ObservableProperty]
    private bool _darkFilter;

    /// <summary>Chosen custom ini, or null.</summary>
    [ObservableProperty]
    private string? _customIniPath;

    /// <summary>Why the custom ini was refused, or null.</summary>
    [ObservableProperty]
    private string? _customIniError;

    /// <summary>True when the options are acceptable.</summary>
    public bool IsOptionsValid => IsNative || CustomIniError is null;

    /// <summary>Validates a custom ini (text, up to 16 MiB). Null removes it.</summary>
    public void SetCustomIni(string? path)
    {
        _customIniText = null;
        CustomIniPath = null;
        CustomIniError = null;
        if (path is not null)
        {
            var r = InputGuard.CheckFile(path, InputGuard.MaxSmallFileBytes, [".ini", ".txt"]);
            if (!r.IsValid)
            {
                CustomIniError = r.Error;
            }
            else
            {
                try
                {
                    var text = File.ReadAllText(r.Path!);
                    if (text.Contains('\0')) throw new InvalidDataException();
                    _customIniText = text.Length > MaxIniPreviewChars ? text[..MaxIniPreviewChars] + "\n..." : text;
                    CustomIniPath = r.Path;
                }
                catch (Exception e) when (e is IOException or InvalidDataException or UnauthorizedAccessException)
                {
                    CustomIniError = "That file is not a readable text ini.";
                }
            }
        }
        Revalidate();
    }

    // ---- Output ----

    /// <summary>Folder that receives the finished title.</summary>
    [ObservableProperty]
    private string _outputDir;

    /// <summary>Why the output folder is refused, or null.</summary>
    public string? OutputError => InputGuard.CheckOutputFolder(OutputDir).Error;

    /// <summary>True when the output folder is acceptable.</summary>
    public bool IsOutputValid => OutputError is null;

    /// <summary>Sets and remembers the output folder.</summary>
    public void SetOutput(string? path)
    {
        OutputDir = path ?? "";
    }

    partial void OnOutputDirChanged(string value)
    {
        if (IsOutputValid) _settings.RememberOutputDir(InputGuard.Absolute(value)!);
        Revalidate();
    }

    // ---- Run ----

    /// <summary>Current phase.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRunning))]
    private InjectPhase _phase;

    /// <summary>0 to 100.</summary>
    [ObservableProperty]
    private int _percent;

    /// <summary>Latest step message from the injector.</summary>
    [ObservableProperty]
    private string _progressMessage = "";

    /// <summary>Last failure, shown above the Inject button.</summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>The finished title folder.</summary>
    [ObservableProperty]
    private string? _resultPath;

    /// <summary>The SD card files folder of a native inject, or null.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSdResult))]
    private string? _resultSdPath;

    /// <summary>True when the finished job has an SD folder to copy.</summary>
    public bool HasSdResult => ResultSdPath is not null;

    /// <summary>True while injecting.</summary>
    public bool IsRunning => Phase == InjectPhase.Running;

    /// <summary>True when a common key is saved in Settings.</summary>
    public bool HasCommonKey => _settings.IsKeyValid;

    /// <summary>True when every input is valid and nothing is running.</summary>
    public bool CanInject =>
        Phase == InjectPhase.Editing && IsRomValid && IsBaseSatisfied && IsDetailsValid && IsOptionsValid
        && IsOutputValid && HasCommonKey && !IsDownloading;

    partial void OnPhaseChanged(InjectPhase value) => Revalidate();

    /// <summary>Runs the inject on a background task.</summary>
    [RelayCommand(CanExecute = nameof(CanInject))]
    private async Task InjectAsync()
    {
        var commonKey = _settings.ValidKeyOrEmpty;
        var output = InputGuard.Absolute(OutputDir)!;
        var cts = _cts = new CancellationTokenSource();
        Phase = InjectPhase.Running;
        ErrorMessage = null;
        Percent = 0;
        ProgressMessage = "Starting...";
        try
        {
            _log.LogInformation("Inject started: {Game}", GameName);
            if (IsNative)
            {
                var layout = await _service.InjectNativeAsync(BuildRequest(), output, commonKey, NewProgress(), cts.Token)
                    .ConfigureAwait(true);
                ResultPath = layout.TitleDir;
                ResultSdPath = layout.SdDir;
            }
            else
            {
                ResultPath = await _service.InjectAsync(BuildRequest(), output, commonKey, NewProgress(), cts.Token)
                    .ConfigureAwait(true);
                ResultSdPath = null;
            }
            Percent = 100;
            ProgressMessage = "Done";
            Phase = InjectPhase.Done;
            _log.LogInformation("Inject finished");
        }
        catch (OperationCanceledException)
        {
            ErrorMessage = "Cancelled.";
            Phase = InjectPhase.Editing;
            _log.LogInformation("Inject cancelled");
        }
        catch (Exception e)
        {
            ErrorMessage = Describe(e, "inject", commonKey);
            Phase = InjectPhase.Editing;
        }
        finally
        {
            cts.Dispose();
            _cts = null;
        }
    }

    /// <summary>Cancels a running inject or download at the next step boundary.</summary>
    [RelayCommand]
    private void Cancel() => _cts?.Cancel();

    /// <summary>Clears the job for "Inject another". The base and output folder stay.</summary>
    public void Reset()
    {
        _detection = null;
        RomError = null;
        GameName = "";
        foreach (var s in ImageSlots) s.Clear();
        WideScreen = false;
        DarkFilter = false;
        PakChoice = PakChoice.Auto;
        SetCustomIni(null);
        ResultPath = null;
        ResultSdPath = null;
        ErrorMessage = null;
        Percent = 0;
        ProgressMessage = "";
        Phase = InjectPhase.Editing;
        Revalidate();
    }

    private InjectRequest BuildRequest() => new(GameConsole.N64, _detection!.Path, IsNative ? "" : BaseDir, GameName.Trim())
    {
        // The VC-only options patch the frame layout and ini; the native engine ignores them.
        N64 = IsNative ? new N64Options() : new N64Options
        {
            CustomIniPath = CustomIniPath,
            ExpansionPak = PakValue,
            WideScreen = WideScreen,
            DarkFilter = DarkFilter,
        },
        IconPath = Icon.Path,
        TvBootPath = TvBoot.Path,
        DrcBootPath = DrcBoot.Path,
        LogoPath = Logo.Path,
    };

    private Progress<InjectProgress> NewProgress() => new(p =>
    {
        Percent = Math.Clamp(p.Percent, 0, 100);
        ProgressMessage = p.Message;
    });

    /// <summary>Turns an exception into a user message. Known failure types keep their text, others are generic.</summary>
    private string Describe(Exception e, string what, params string[] secrets)
    {
        _log.LogError("{What} failed: {Type}", what, e.GetType().Name);
        if (e is not (InvalidDataException or IOException or ArgumentException or NotSupportedException
            or UnauthorizedAccessException))
            return $"The {what} failed unexpectedly. See ~/Library/Logs/UWUVCI/uwuvci.log.";
        var msg = e.Message;
        foreach (var s in secrets.Where(s => s.Length > 0)) msg = msg.Replace(s, "***", StringComparison.OrdinalIgnoreCase);
        return msg;
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SettingsViewModel.CommonKey) or nameof(SettingsViewModel.IsKeyValid))
        {
            Revalidate();
        }
    }

    /// <summary>Notifies every derived property after any input changed.</summary>
    private void Revalidate()
    {
        foreach (var n in new[]
        {
            nameof(Detection), nameof(IsRomValid), nameof(HasRom), nameof(RomFileName), nameof(RomName),
            nameof(RomInDatabase), nameof(RomRegion), nameof(RomSave), nameof(RomControllers),
            nameof(RomExpansionPak), nameof(IniPreview), nameof(IsBaseValid), nameof(IsBaseSatisfied), nameof(GameNameError),
            nameof(IsDetailsValid), nameof(IsOptionsValid), nameof(OutputError), nameof(IsOutputValid),
            nameof(HasCommonKey), nameof(CanInject), nameof(SelectedBase), nameof(IsCustomBase), nameof(ShowKeyField),
            nameof(ShowDownload), nameof(DownloadButtonText), nameof(ShowCommonKeyHint), nameof(BaseKeyError),
        })
            OnPropertyChanged(n);
        InjectCommand.NotifyCanExecuteChanged();
        DownloadSelectedBaseCommand.NotifyCanExecuteChanged();
    }
}
