using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using UWUVCI.Mac.ViewModels;

namespace UWUVCI.Mac.Views;

/// <summary>The single app window.</summary>
public sealed partial class MainWindow : Window
{
    /// <summary>Creates the window.</summary>
    public MainWindow() => InitializeComponent();

    private void OnStepClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: StepItem item } && DataContext is MainViewModel vm)
            vm.GoToCommand.Execute(item);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is MainViewModel vm && (vm.IsSettingsOpen || vm.IsAboutOpen))
        {
            vm.CloseOverlayCommand.Execute(null);
            e.Handled = true;
        }
    }
}
