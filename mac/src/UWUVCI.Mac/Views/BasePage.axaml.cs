using Avalonia.Controls;
using Avalonia.Interactivity;
using UWUVCI.Mac.ViewModels;

namespace UWUVCI.Mac.Views;

/// <summary>Base folder picker and downloader.</summary>
public sealed partial class BasePage : UserControl
{
    /// <summary>Creates the page.</summary>
    public BasePage() => InitializeComponent();

    private async void OnBaseSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainViewModel vm && BaseCombo.SelectedItem is BaseOption option)
            await vm.Inject.ChooseBaseAsync(option);
    }

    private async void OnChooseFolder(object? sender, RoutedEventArgs e)
    {
        var path = await Pickers.OpenFolderAsync(this, "Choose the decrypted base folder");
        if (path is not null && DataContext is MainViewModel vm) vm.Inject.SetBase(path);
    }
}
