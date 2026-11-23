using Avalonia.Controls;
using Avalonia.Interactivity;
using UWUVCI.Mac.ViewModels;

namespace UWUVCI.Mac.Views;

/// <summary>Game name and optional images.</summary>
public sealed partial class DetailsPage : UserControl
{
    /// <summary>Creates the page.</summary>
    public DetailsPage() => InitializeComponent();

    private async void OnChoose(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: ImageSlot slot }) return;
        var path = await Pickers.OpenFileAsync(this, $"Choose {slot.Label}", Pickers.Images);
        if (path is not null) slot.Set(path);
    }
}
