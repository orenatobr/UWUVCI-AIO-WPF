using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UWUVCI.Mac.Services;

namespace UWUVCI.Mac.ViewModels;

/// <summary>One optional image (icon, boot screen or logo) with validation.</summary>
/// <param name="label">Name shown to the user.</param>
/// <param name="hint">Output size, shown as help text.</param>
public sealed partial class ImageSlot(string label, string hint) : ObservableObject
{
    /// <summary>Name shown to the user.</summary>
    public string Label { get; } = label;

    /// <summary>Output size hint.</summary>
    public string Hint { get; } = hint;

    /// <summary>Canonical path of the chosen image, or null.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasImage))]
    private string? _path;

    /// <summary>Why the last choice was refused, or null.</summary>
    [ObservableProperty]
    private string? _error;

    /// <summary>True when a valid image is set.</summary>
    public bool HasImage => Path is not null;

    /// <summary>Raised after the slot changes so the owner can revalidate.</summary>
    public event EventHandler? Changed;

    /// <summary>Validates and stores an image path. An invalid file clears the slot and sets <see cref="Error"/>.</summary>
    public void Set(string? file)
    {
        var r = InputGuard.CheckImage(file);
        Path = r.Path;
        Error = r.Error;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Removes the image and any error.</summary>
    [RelayCommand]
    public void Clear()
    {
        Path = null;
        Error = null;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
