using UWUVCI.Core.Injection;

namespace UWUVCI.Mac.ViewModels;

/// <summary>A console card on the first screen.</summary>
/// <param name="Console">Core console id.</param>
/// <param name="Name">Display name.</param>
/// <param name="Available">Whether the macOS build can inject for it today.</param>
/// <param name="Note">Short line under the name.</param>
public sealed record ConsoleItem(GameConsole Console, string Name, bool Available, string Note)
{
    private const string NotYet = "Not available in the macOS build yet.";

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
