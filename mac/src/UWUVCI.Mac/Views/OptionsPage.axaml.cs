using Avalonia.Controls;
using Avalonia.Interactivity;
using UWUVCI.Mac.ViewModels;

namespace UWUVCI.Mac.Views;

/// <summary>N64 options page.</summary>
public sealed partial class OptionsPage : UserControl
{
    /// <summary>Creates the page.</summary>
    public OptionsPage() => InitializeComponent();

    private async void OnChoose(object? sender, RoutedEventArgs e)
    {
        var path = await Pickers.OpenFileAsync(this, "Choose a custom ini", Pickers.Inis);
        if (path is not null && DataContext is MainViewModel vm) vm.Inject.SetCustomIni(path);
    }

    private void OnRemove(object? sender, RoutedEventArgs e) => (DataContext as MainViewModel)?.Inject.SetCustomIni(null);
}
