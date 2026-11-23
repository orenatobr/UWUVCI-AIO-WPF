using Avalonia.Controls;
using Avalonia.Interactivity;
using UWUVCI.Mac.ViewModels;

namespace UWUVCI.Mac.Views;

/// <summary>Console picker page.</summary>
public sealed partial class ConsolePage : UserControl
{
    /// <summary>Creates the page.</summary>
    public ConsolePage() => InitializeComponent();

    private void OnTileClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ConsoleItem item } && DataContext is MainViewModel vm)
            vm.SelectConsoleCommand.Execute(item);
    }
}
