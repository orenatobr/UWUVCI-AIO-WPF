using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Data.Converters;
using Avalonia.Platform.Storage;

namespace UWUVCI.Mac.Views;

/// <summary>Wrappers over the native open panels.</summary>
internal static class Pickers
{
    public static readonly FilePickerFileType Roms = new("N64 ROM") { Patterns = ["*.z64", "*.v64", "*.n64"] };
    public static readonly FilePickerFileType Images = new("Image") { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.bmp"] };
    public static readonly FilePickerFileType Inis = new("Ini file") { Patterns = ["*.ini", "*.txt"] };

    /// <summary>Opens a file panel and returns the local path, or null when cancelled.</summary>
    public static async Task<string?> OpenFileAsync(Control from, string title, FilePickerFileType type)
    {
        if (TopLevel.GetTopLevel(from) is not { } top) return null;
        var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = [type],
        });
        return files.Count == 1 ? files[0].TryGetLocalPath() : null;
    }

    /// <summary>Opens a folder panel and returns the local path, or null when cancelled.</summary>
    public static async Task<string?> OpenFolderAsync(Control from, string title)
    {
        if (TopLevel.GetTopLevel(from) is not { } top) return null;
        var folders = await top.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
        });
        return folders.Count == 1 ? folders[0].TryGetLocalPath() : null;
    }
}

/// <summary>XAML converters.</summary>
public static class Converters
{
    /// <summary>Turns an image path into a small bitmap, or null when it cannot be decoded.</summary>
    public static readonly IValueConverter PathToBitmap = new FuncValueConverter<string?, Bitmap?>(path =>
    {
        if (string.IsNullOrEmpty(path)) return null;
        try
        {
            using var fs = File.OpenRead(path);
            return Bitmap.DecodeToWidth(fs, 256);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return null;
        }
    });

    /// <summary>Semi bold for true, regular for false.</summary>
    public static readonly IValueConverter BoldIf =
        new FuncValueConverter<bool, FontWeight>(b => b ? FontWeight.SemiBold : FontWeight.Normal);

    /// <summary>Formats a percentage for display.</summary>
    public static readonly IValueConverter Percent = new FuncValueConverter<int, string>(p => $"{p}%");
}
