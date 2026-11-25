using UWUVCI.Core.Injection;

namespace UWUVCI.Core.N64;

/// <summary>
/// Chooses the VC ini for a ROM. Save hardware, rumble and controller pak come from the game
/// database. Title specific tuning mirrors the community inis published in UWUVCI-IMAGES
/// (Banjo-Tooie NB7E, Conker NCKE, Jet Force Gemini NJFE, GoldenEye NGEJ).
/// </summary>
public static class N64Profiles
{
    private const string BanjoInput = "[Input]\nStickModify = 2\n";

    // The PAL picture of the VC engine is drawn 5% short of the frame (a black bar on the right). The community
    // fixes this for Banjo-Kazooie with ClipRight = 55, and the same value fills the frame exactly for Banjo-Tooie (EU).
    private const string BanjoEuRender = "\n[Render]\nClipRight = 55\n";

    private const string GoldenEyeJp =
        "[BreakBlockInst]\nCount = 1\nAddress0 = 0x7f0d0eeC\nInst0 = 0x24420004\nJmpPC0= 0x7f0d0ee8\nType0 = 1\n\n" +
        "[Render]\nFogVertexAlpha = 0\n";

    /// <summary>Builds the ini options for a ROM.</summary>
    /// <param name="game">Database entry, or null for an unknown ROM.</param>
    /// <param name="header">Parsed ROM header.</param>
    /// <param name="pakOverride">Explicit Expansion Pak choice, null to follow the profile.</param>
    /// <param name="saveOverride">Explicit save hardware, null to follow the profile.</param>
    public static VcIniOptions For(N64Game? game, RomHeader header, bool? pakOverride, BackupType? saveOverride = null)
    {
        string name = game?.Name ?? header.Title;
        var o = new VcIniOptions(CleanName(name), RegionLabel(name, header)) { BackupSize = "512" };

        if (game is not null)
        {
            o = game.Save switch
            {
                SaveKind.Eeprom4K => o with { BackupType = BackupType.Eeprom, BackupSize = "512" },
                SaveKind.Eeprom16K => o with { BackupType = BackupType.Eeprom, BackupSize = "2048" },
                SaveKind.Sram => o with { BackupType = BackupType.Sram, BackupSize = null },
                SaveKind.Flash => o with { BackupType = BackupType.Flash, BackupSize = null },
                _ => o,
            };
            // Games without cartridge saves keep progress on a Controller Pak, so it has to be plugged in.
            bool saveOnPak = game.Save == SaveKind.None && game.MemPak;
            o = o with { Rumble = game.Rumble && !saveOnPak, MemPack = saveOnPak };
        }

        o = Tune(name, o);
        if (saveOverride is { } save)
            o = o with { BackupType = save, BackupSize = save == BackupType.Eeprom ? o.BackupSize ?? (game?.Save == SaveKind.Eeprom16K ? "2048" : "512") : null };
        // No published ini sets RamSize, so the Expansion Pak stays off unless the user asks for it.
        return o with { ExpansionPak = pakOverride ?? false };
    }

    private static VcIniOptions Tune(string name, VcIniOptions o)
    {
        if (IsBanjoTooie(name))
            // SRAM: with the EEPROM save the first scene never advances in Cemu. The EEPROM size answer is fixed in the engine (VcEnginePatches).
            return o with { BackupType = BackupType.Sram, BackupSize = null, RetraceByVsync = true, TrueBoot = true, ExtraSections = o.Region == "EU" ? BanjoInput + BanjoEuRender : BanjoInput };
        if (name.StartsWith("Conker's Bad Fur Day", StringComparison.OrdinalIgnoreCase))
            return o with { RetraceByVsync = true, UseTimer = true };
        if (name.StartsWith("Jet Force Gemini", StringComparison.OrdinalIgnoreCase))
            return o with { UseTimer = true };
        if (name.StartsWith("GoldenEye 007", StringComparison.OrdinalIgnoreCase))
            return o with { UseTimer = true, ExtraSections = name.Contains("(J)") ? GoldenEyeJp : null };
        return o;
    }

    /// <summary>True for Banjo-Tooie, which needs the engine to answer a 16 Kbit EEPROM (see <c>VcEnginePatches</c>).</summary>
    public static bool NeedsEeprom16KAnswer(N64Game? game, RomHeader header) => IsBanjoTooie(game?.Name ?? header.Title);

    private static bool IsBanjoTooie(string name) => name.StartsWith("Banjo-Tooie", StringComparison.OrdinalIgnoreCase);

    private static string CleanName(string name) =>
        name.Replace('\r', ' ').Replace('\n', ' ').Trim();

    private static string RegionLabel(string name, RomHeader h)
    {
        if (name.Contains("(U)") || name.Contains("(USA)")) return "US";
        if (name.Contains("(J)")) return "JP";
        if (name.Contains("(E)") || name.Contains("(G)") || name.Contains("(F)")) return "EU";
        return h.RegionCode switch { 'E' => "US", 'J' => "JP", _ => "EU" };
    }
}
