using Avalonia.Controls;
using Avalonia.Interactivity;
using UWUVCI.Mac.ViewModels;

namespace UWUVCI.Mac.Views;

/// <summary>Settings sheet: common key, output folder, theme.</summary>
public sealed partial class SettingsSheet : UserControl
{
    /// <summary>Creates the sheet.</summary>
    public SettingsSheet() => InitializeComponent();

    private async void OnChooseOutput(object? sender, RoutedEventArgs e)
    {
        var path = await Pickers.OpenFolderAsync(this, "Choose the default output folder");
        if (path is not null && DataContext is MainViewModel vm) vm.Settings.OutputDir = path;
    }
}
