using CommunityToolkit.Mvvm.ComponentModel;
using UWUVCI.Core.Injection;

namespace UWUVCI.Mac.ViewModels;

/// <summary>A console card on the first page.</summary>
/// <param name="console">Core console id.</param>
/// <param name="name">Display name.</param>
/// <param name="available">Whether the macOS build can inject for it today.</param>
/// <param name="note">Short line under the name.</param>
public sealed partial class ConsoleItem(GameConsole console, string name, bool available, string note) : ObservableObject
{
    private const string NotYet = "Not available in the macOS build yet.";

    /// <summary>Core console id.</summary>
    public GameConsole Console { get; } = console;

    /// <summary>Display name.</summary>
    public string Name { get; } = name;

    /// <summary>True when the macOS build can inject for this console.</summary>
    public bool Available { get; } = available;

    /// <summary>Short line under the name.</summary>
    public string Note { get; } = note;

    /// <summary>True for the chosen console.</summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>Builds the list from <see cref="GameConsole"/>. Only N64 is implemented in Core.</summary>
    public static IReadOnlyList<ConsoleItem> All() =>
        [.. Enum.GetValues<GameConsole>().Select(c => c == GameConsole.N64
            ? new ConsoleItem(c, "Nintendo 64", true, "ROM conversion, ini profile and packing are supported.")
            : new ConsoleItem(c, DisplayName(c), false, NotYet))];

    private static string DisplayName(GameConsole c) => c switch
    {
        GameConsole.Nes => "NES",
        GameConsole.Snes => "Super Nintendo",
        GameConsole.N64 => "Nintendo 64",
        GameConsole.Gba => "Game Boy Advance",
        GameConsole.Nds => "Nintendo DS",
        GameConsole.Tg16 => "TurboGrafx-16",
        GameConsole.Msx => "MSX",
        GameConsole.Wii => "Wii",
        GameConsole.GameCube => "GameCube",
        _ => c.ToString(),
    };
}
