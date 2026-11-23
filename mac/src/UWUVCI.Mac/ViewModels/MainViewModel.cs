using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UWUVCI.Mac.Services;

namespace UWUVCI.Mac.ViewModels;

/// <summary>Pages of the inject wizard, in order.</summary>
public enum WizardStep
{
    /// <summary>Pick the console.</summary>
    Console,
    /// <summary>Pick the ROM.</summary>
    Rom,
    /// <summary>Pick or download the base.</summary>
    Base,
    /// <summary>Name and images.</summary>
    Details,
    /// <summary>Console options.</summary>
    Options,
    /// <summary>Output folder and Inject.</summary>
    Output,
}

/// <summary>One entry in the sidebar.</summary>
public sealed partial class StepItem(WizardStep step, string title) : ObservableObject
{
    /// <summary>The page this entry opens.</summary>
    public WizardStep Step { get; } = step;

    /// <summary>Sidebar title.</summary>
    public string Title { get; } = title;

    /// <summary>1 based position.</summary>
    public int Number => (int)Step + 1;

    /// <summary>True for the open page.</summary>
    [ObservableProperty]
    private bool _isCurrent;

    /// <summary>True when the page has all it needs.</summary>
    [ObservableProperty]
    private bool _isComplete;

    /// <summary>True when every earlier page is complete, so the user may jump here.</summary>
    [ObservableProperty]
    private bool _isEnabled;
}

/// <summary>Shell state: wizard navigation, overlays and the finished screen.</summary>
public sealed partial class MainViewModel : ObservableObject
{
    private static readonly string[] Titles = ["Console", "ROM", "Base", "Details", "Options", "Output"];

    /// <summary>Creates the shell around its two main view models.</summary>
    public MainViewModel(SettingsViewModel settings, InjectViewModel inject)
    {
        Settings = settings;
        Inject = inject;
        Steps = [.. Enum.GetValues<WizardStep>().Select(s => new StepItem(s, Titles[(int)s]))];
        inject.PropertyChanged += (_, _) => Refresh();
        settings.PropertyChanged += (_, _) => Refresh();
        Refresh();
    }

    /// <summary>Settings sheet state.</summary>
    public SettingsViewModel Settings { get; }

    /// <summary>The N64 job.</summary>
    public InjectViewModel Inject { get; }

    /// <summary>Consoles for the first page.</summary>
    public IReadOnlyList<ConsoleItem> Consoles { get; } = ConsoleItem.All();

    /// <summary>Sidebar entries.</summary>
    public IReadOnlyList<StepItem> Steps { get; }

    /// <summary>Opens Finder on a path. Replaceable in tests.</summary>
    public Action<string> RevealAction { get; init; } = Finder.Reveal;

    /// <summary>The chosen console, or null.</summary>
    [ObservableProperty]
    private ConsoleItem? _selectedConsole;

    /// <summary>The open wizard page.</summary>
    [ObservableProperty]
    private WizardStep _step;

    /// <summary>True while the Settings sheet is open.</summary>
    [ObservableProperty]
    private bool _isSettingsOpen;

    /// <summary>True while the About sheet is open.</summary>
    [ObservableProperty]
    private bool _isAboutOpen;

    /// <summary>True when the form is showing.</summary>
    public bool IsWizard => Inject.Phase == InjectPhase.Editing;

    /// <summary>True while injecting.</summary>
    public bool IsRunning => Inject.Phase == InjectPhase.Running;

    /// <summary>True after a successful inject.</summary>
    public bool IsDone => Inject.Phase == InjectPhase.Done;

    /// <summary>True on the last wizard page.</summary>
    public bool IsLastStep => Step == WizardStep.Output;

    /// <summary>True on the first wizard page.</summary>
    public bool IsFirstStep => Step == WizardStep.Console;

    /// <summary>True when the open page has what it needs.</summary>
    public bool CanGoNext => !IsLastStep && IsComplete(Step);

    /// <summary>Picks a console. Consoles that are not available are ignored.</summary>
    [RelayCommand]
    private void SelectConsole(ConsoleItem? item)
    {
        if (item is { Available: true }) SelectedConsole = item;
    }

    /// <summary>Moves to the next page when the current one is complete.</summary>
    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void Next()
    {
        if (CanGoNext) Step++;
    }

    /// <summary>Moves to the previous page.</summary>
    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void Back()
    {
        if (CanGoBack) Step--;
    }

    private bool CanGoBack => !IsFirstStep;

    /// <summary>Jumps to a sidebar entry when it is enabled.</summary>
    [RelayCommand]
    private void GoTo(StepItem? item)
    {
        if (item is { IsEnabled: true }) Step = item.Step;
    }

    /// <summary>Opens the Settings sheet.</summary>
    [RelayCommand]
    private void OpenSettings()
    {
        IsAboutOpen = false;
        IsSettingsOpen = true;
    }

    /// <summary>Opens the About and FAQ sheet.</summary>
    [RelayCommand]
    private void OpenAbout()
    {
        IsSettingsOpen = false;
        IsAboutOpen = true;
    }

    /// <summary>Closes whichever sheet is open.</summary>
    [RelayCommand]
    private void CloseOverlay()
    {
        IsSettingsOpen = false;
        IsAboutOpen = false;
    }

    /// <summary>Reveals the finished title in Finder.</summary>
    [RelayCommand]
    private void ShowInFinder()
    {
        if (Inject.ResultPath is { } p) RevealAction(p);
    }

    /// <summary>Starts a new job on the ROM page.</summary>
    [RelayCommand]
    private void InjectAnother()
    {
        Inject.Reset();
        Step = WizardStep.Rom;
    }

    partial void OnStepChanged(WizardStep value) => Refresh();

    partial void OnSelectedConsoleChanged(ConsoleItem? value) => Refresh();

    private bool IsComplete(WizardStep s) => s switch
    {
        WizardStep.Console => SelectedConsole is not null,
        WizardStep.Rom => Inject.IsRomValid,
        WizardStep.Base => Inject.IsBaseValid,
        WizardStep.Details => Inject.IsDetailsValid,
        WizardStep.Options => Inject.IsOptionsValid,
        WizardStep.Output => Inject.CanInject,
        _ => false,
    };

    private void Refresh()
    {
        bool earlierDone = true;
        foreach (var item in Steps)
        {
            item.IsCurrent = item.Step == Step;
            item.IsComplete = IsComplete(item.Step);
            item.IsEnabled = earlierDone;
            earlierDone &= item.IsComplete;
        }
        foreach (var n in new[]
        {
            nameof(IsWizard), nameof(IsRunning), nameof(IsDone), nameof(IsLastStep), nameof(IsFirstStep), nameof(CanGoNext),
        })
            OnPropertyChanged(n);
        NextCommand.NotifyCanExecuteChanged();
        BackCommand.NotifyCanExecuteChanged();
    }
}
