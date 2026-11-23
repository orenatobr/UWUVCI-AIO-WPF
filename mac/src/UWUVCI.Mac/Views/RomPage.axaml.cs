using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using UWUVCI.Mac.ViewModels;

namespace UWUVCI.Mac.Views;

/// <summary>ROM picker with drag and drop and the detection card.</summary>
public sealed partial class RomPage : UserControl
{
    /// <summary>Creates the page.</summary>
    public RomPage()
    {
        InitializeComponent();
        DropZone.AddHandler(DragDrop.DragOverEvent, OnDragOver);
        DropZone.AddHandler(DragDrop.DragEnterEvent, (_, _) => DropZone.Classes.Add("over"));
        DropZone.AddHandler(DragDrop.DragLeaveEvent, (_, _) => DropZone.Classes.Remove("over"));
        DropZone.AddHandler(DragDrop.DropEvent, OnDrop);
    }

    private InjectViewModel? Vm => (DataContext as MainViewModel)?.Inject;

    private async void OnChoose(object? sender, RoutedEventArgs e)
    {
        var path = await Pickers.OpenFileAsync(this, "Choose an N64 ROM", Pickers.Roms);
        if (path is not null) Vm?.SetRom(path);
    }

    private static void OnDragOver(object? sender, DragEventArgs e) =>
        e.DragEffects = e.DataTransfer.TryGetFiles() is { Length: 1 } ? DragDropEffects.Copy : DragDropEffects.None;

    private void OnDrop(object? sender, DragEventArgs e)
    {
        DropZone.Classes.Remove("over");
        if (e.DataTransfer.TryGetFiles() is { Length: 1 } files)
            Vm?.SetRom(files[0].TryGetLocalPath());
    }
}
