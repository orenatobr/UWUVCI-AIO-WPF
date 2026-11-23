using UWUVCI.Core.N64;

namespace UWUVCI.Mac.Services;

/// <summary>What was recognised in a ROM file, plus what the ini would contain.</summary>
public sealed class RomDetection
{
    private readonly N64Game? _game;
    private readonly RomHeader _header;

    internal RomDetection(string path, RomHeader header, N64Game? game)
    {
        Path = path;
        _header = header;
        _game = game;
    }

    /// <summary>Canonical path of the ROM.</summary>
    public string Path { get; }

    /// <summary>Game name from the database, or the header title for unknown ROMs.</summary>
    public string Name => _game?.Name ?? _header.Title;

    /// <summary>True when the ROM checksums were found in the game database.</summary>
    public bool InDatabase => _game is not null;

    /// <summary>The database entry, if any.</summary>
    public N64Game? Game => _game;

    /// <summary>Ini options for the given Expansion Pak choice (null follows the game profile).</summary>
    public VcIniOptions Profile(bool? expansionPak) => N64Profiles.For(_game, _header, expansionPak);

    /// <summary>The ini text that would be written for the given Expansion Pak choice.</summary>
    public string BuildIni(bool? expansionPak) => VcIni.Build(Profile(expansionPak));
}

/// <summary>Reads an N64 ROM header and looks the game up.</summary>
public static class RomDetector
{
    /// <summary>Detects a ROM. Returns null and sets <paramref name="error"/> when the file is not a usable N64 ROM.</summary>
    public static RomDetection? Detect(string? path, out string? error)
    {
        var check = InputGuard.CheckFile(path, InputGuard.MaxRomBytes, InputGuard.RomExtensions);
        if (!check.IsValid)
        {
            error = check.Error;
            return null;
        }
        try
        {
            var header = N64Rom.ReadHeader(File.ReadAllBytes(check.Path!));
            error = null;
            return new RomDetection(check.Path!, header, N64GameDatabase.Find(header.Crc1, header.Crc2));
        }
        catch (Exception e) when (e is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            error = e is InvalidDataException ? e.Message : "The file cannot be read.";
            return null;
        }
    }
}
