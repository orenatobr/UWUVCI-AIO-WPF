namespace UWUVCI.Core.Injection;

/// <summary>Consoles the injector knows about.</summary>
public enum GameConsole
{
    /// <summary>Nintendo Entertainment System.</summary>
    Nes,
    /// <summary>Super Nintendo.</summary>
    Snes,
    /// <summary>Nintendo 64.</summary>
    N64,
    /// <summary>Game Boy Advance.</summary>
    Gba,
    /// <summary>Nintendo DS.</summary>
    Nds,
    /// <summary>TurboGrafx-16 / PC Engine.</summary>
    Tg16,
    /// <summary>MSX.</summary>
    Msx,
    /// <summary>Wii.</summary>
    Wii,
    /// <summary>GameCube.</summary>
    GameCube,
}

/// <summary>User choices for an N64 inject.</summary>
public sealed record N64Options
{
    /// <summary>Path to a hand written ini that replaces the automatic profile.</summary>
    public string? CustomIniPath { get; init; }
    /// <summary>Force the Expansion Pak on (true), off (false) or follow the game profile (null).</summary>
    public bool? ExpansionPak { get; init; }
    /// <summary>Force the save hardware (the Cemu freeze of EEPROM titles can be avoided with SRAM or Flash), null follows the game profile.</summary>
    public UWUVCI.Core.N64.BackupType? SaveType { get; init; }
    /// <summary>Stretch the game picture to 16:9 in the frame layout.</summary>
    public bool WideScreen { get; init; }
    /// <summary>Remove the darkening overlay of the VC frame.</summary>
    public bool DarkFilter { get; init; }
    /// <summary>
    /// Banjo-Tooie (US) only: draw each character shadow as a translucent ellipse instead of the dark rectangle that comes
    /// from the engine's frame buffer read-back (see <see cref="VcEnginePatches.ShadowEllipses"/>). Off by default.
    /// </summary>
    public bool ShadowEllipses { get; init; }
}

/// <summary>Progress notification from a running inject.</summary>
/// <param name="Percent">0 to 100.</param>
/// <param name="Message">Human readable step.</param>
public readonly record struct InjectProgress(int Percent, string Message);
