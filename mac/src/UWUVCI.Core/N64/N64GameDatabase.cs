using System.Reflection;

namespace UWUVCI.Core.N64;

/// <summary>Save hardware as catalogued by the community ROM database.</summary>
public enum SaveKind
{
    /// <summary>No save memory.</summary>
    None,
    /// <summary>4 Kbit EEPROM (512 bytes).</summary>
    Eeprom4K,
    /// <summary>16 Kbit EEPROM (2048 bytes).</summary>
    Eeprom16K,
    /// <summary>256 Kbit battery SRAM.</summary>
    Sram,
    /// <summary>1 Mbit Flash RAM.</summary>
    Flash,
}

/// <summary>A verified ROM dump entry.</summary>
/// <param name="Crc1">ROM CRC1.</param>
/// <param name="Crc2">ROM CRC2.</param>
/// <param name="Name">Good name, for example "Banjo-Tooie (U) [!]".</param>
/// <param name="Save">Save hardware.</param>
/// <param name="Players">Supported players.</param>
/// <param name="Rumble">Supports the Rumble Pak.</param>
/// <param name="MemPak">Supports the Controller Pak.</param>
public sealed record N64Game(uint Crc1, uint Crc2, string Name, SaveKind Save, int Players, bool Rumble, bool MemPak);

/// <summary>Lookup of ROM metadata by header CRC, generated from the mupen64plus game database.</summary>
public static class N64GameDatabase
{
    // Titles whose engine uses the 4 MB Expansion Pak (8 MB RDRAM) and fail or corrupt without it.
    private static readonly string[] ExpansionPakTitles =
    [
        "Banjo-Tooie", "Conker's Bad Fur Day", "Jet Force Gemini",
        "Turok 2", "Turok 3", "Perfect Dark", "Donkey Kong 64",
        "Zelda no Densetsu - Mujura no Kamen", "Legend of Zelda, The - Majora's Mask",
    ];

    private static readonly Lazy<Dictionary<(uint, uint), N64Game>> Table = new(Load);

    /// <summary>Number of catalogued games.</summary>
    public static int Count => Table.Value.Count;

    /// <summary>Finds a game by its ROM header checksums, or null.</summary>
    public static N64Game? Find(uint crc1, uint crc2) =>
        Table.Value.TryGetValue((crc1, crc2), out var g) ? g : null;

    /// <summary>True for titles that need the Expansion Pak to run correctly.</summary>
    public static bool NeedsExpansionPak(string name) =>
        ExpansionPakTitles.Any(t => name.StartsWith(t, StringComparison.OrdinalIgnoreCase));

    private static SaveKind ParseSave(string s) => s switch
    {
        "Eeprom 4KB" => SaveKind.Eeprom4K,
        "Eeprom 16KB" => SaveKind.Eeprom16K,
        "SRAM" => SaveKind.Sram,
        "Flash RAM" => SaveKind.Flash,
        _ => SaveKind.None,
    };

    private static Dictionary<(uint, uint), N64Game> Load()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("n64-games.tsv")
            ?? throw new InvalidOperationException("Embedded n64-games.tsv is missing.");
        using var reader = new StreamReader(stream);
        var map = new Dictionary<(uint, uint), N64Game>();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var f = line.Split('\t');
            if (f.Length < 7) continue;
            uint c1 = Convert.ToUInt32(f[0], 16), c2 = Convert.ToUInt32(f[1], 16);
            map[(c1, c2)] = new N64Game(c1, c2, f[2], ParseSave(f[3]),
                int.TryParse(f[4], out var p) ? p : 0, f[5] == "Yes", f[6] == "Yes");
        }
        return map;
    }
}
