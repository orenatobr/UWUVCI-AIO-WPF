using Avalonia.Controls;
using Avalonia.Interactivity;
using UWUVCI.Mac.ViewModels;

namespace UWUVCI.Mac.Views;

/// <summary>Output folder and summary page.</summary>
public sealed partial class OutputPage : UserControl
{
    /// <summary>Creates the page.</summary>
    public OutputPage() => InitializeComponent();

    private async void OnChoose(object? sender, RoutedEventArgs e)
    {
        var path = await Pickers.OpenFolderAsync(this, "Choose the output folder");
        if (path is not null && DataContext is MainViewModel vm) vm.Inject.SetOutput(path);
    }
}
