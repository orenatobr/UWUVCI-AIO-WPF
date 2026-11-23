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

/// <summary>Everything the user decides for one N64 inject, plus the run itself.</summary>
public sealed partial class InjectViewModel : ObservableObject
{
    private const int MaxNameLength = 100;
    private const int MaxIniPreviewChars = 8000;

    private readonly IInjectService _service;
    private readonly SettingsViewModel _settings;
    private readonly string _basesRoot;
    private readonly ILogger _log;
    private CancellationTokenSource? _cts;
    private RomDetection? _detection;
    private string? _customIniText;

    /// <summary>Creates the view model.</summary>
    /// <param name="service">Runs injects and downloads.</param>
    /// <param name="settings">Source of the common key and the remembered output folder.</param>
    /// <param name="basesRoot">Folder that receives downloaded bases.</param>
    /// <param name="log">Optional logger.</param>
    public InjectViewModel(IInjectService service, SettingsViewModel settings, string basesRoot, ILogger? log = null)
    {
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

    // ---- Base ----

    /// <summary>Canonical path of the decrypted base, or empty.</summary>
    [ObservableProperty]
    private string _baseDir = "";

    /// <summary>Why the base was refused or the download failed.</summary>
    [ObservableProperty]
    private string? _baseError;

    /// <summary>Title id to download (16 hex).</summary>
    [ObservableProperty]
    private string _titleId = "";

    /// <summary>Title key to download (32 hex). Never logged.</summary>
    [ObservableProperty]
    private string _titleKey = "";

    /// <summary>True while a download runs.</summary>
    [ObservableProperty]
    private bool _isDownloading;

    /// <summary>True when the base folder is usable.</summary>
    public bool IsBaseValid => BaseDir.Length > 0;

    partial void OnTitleIdChanged(string value) => DownloadBaseCommand.NotifyCanExecuteChanged();

    partial void OnTitleKeyChanged(string value) => DownloadBaseCommand.NotifyCanExecuteChanged();

    partial void OnIsDownloadingChanged(bool value) => DownloadBaseCommand.NotifyCanExecuteChanged();

    /// <summary>Checks a decrypted base folder (code, content, meta).</summary>
    public void SetBase(string? path)
    {
        var r = InputGuard.CheckBaseFolder(path);
        BaseDir = r.Path ?? "";
        BaseError = r.Error;
        Revalidate();
    }

    private bool CanDownload() =>
        !IsDownloading && InputGuard.IsHex(TitleId.Trim(), 16) && InputGuard.IsHex(TitleKey.Trim(), 32) && _settings.IsKeyValid;

    /// <summary>Downloads the base from the Nintendo CDN into the app folder and selects it.</summary>
    [RelayCommand(CanExecute = nameof(CanDownload))]
    private async Task DownloadBaseAsync()
    {
        var id = TitleId.Trim().ToUpperInvariant();
        var key = TitleKey.Trim();
        var commonKey = _settings.ValidKeyOrEmpty;
        var dest = Path.Combine(_basesRoot, id);
        IsDownloading = true;
        BaseError = null;
        Percent = 0;
        ProgressMessage = "Starting download...";
        var cts = _cts = new CancellationTokenSource();
        try
        {
            _log.LogInformation("Downloading base {TitleId}", id);
            await _service.DownloadBaseAsync(id, key, commonKey, dest, NewProgress(), cts.Token).ConfigureAwait(true);
            SetBase(dest);
            _log.LogInformation("Base {TitleId} ready", id);
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
    public bool IsOptionsValid => CustomIniError is null;

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
        if (IsOutputValid) _settings.RememberOutputDir(InputGuard.Absolute(OutputDir)!);
    }

    partial void OnOutputDirChanged(string value) => Revalidate();

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

    /// <summary>True while injecting.</summary>
    public bool IsRunning => Phase == InjectPhase.Running;

    /// <summary>True when a common key is saved in Settings.</summary>
    public bool HasCommonKey => _settings.IsKeyValid;

    /// <summary>True when every input is valid and nothing is running.</summary>
    public bool CanInject =>
        Phase == InjectPhase.Editing && IsRomValid && IsBaseValid && IsDetailsValid && IsOptionsValid
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
            var result = await _service.InjectAsync(BuildRequest(), output, commonKey, NewProgress(), cts.Token)
                .ConfigureAwait(true);
            ResultPath = result;
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

    [RelayCommand]
    private void ToggleIniPreview() => IsIniPreviewOpen = !IsIniPreviewOpen;

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
        ErrorMessage = null;
        Percent = 0;
        ProgressMessage = "";
        Phase = InjectPhase.Editing;
        Revalidate();
    }

    private InjectRequest BuildRequest() => new(GameConsole.N64, _detection!.Path, BaseDir, GameName.Trim())
    {
        N64 = new N64Options
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
            DownloadBaseCommand.NotifyCanExecuteChanged();
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
            nameof(RomExpansionPak), nameof(IniPreview), nameof(IsBaseValid), nameof(GameNameError),
            nameof(IsDetailsValid), nameof(IsOptionsValid), nameof(OutputError), nameof(IsOutputValid),
            nameof(HasCommonKey), nameof(CanInject),
        })
            OnPropertyChanged(n);
        InjectCommand.NotifyCanExecuteChanged();
    }
}
