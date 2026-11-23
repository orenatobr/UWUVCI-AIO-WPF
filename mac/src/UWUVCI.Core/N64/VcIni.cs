namespace UWUVCI.Core.N64;

/// <summary>Save hardware selector understood by the Wii U N64 Virtual Console emulator.</summary>
public enum BackupType
{
    /// <summary>Let the emulator decide.</summary>
    Auto = 0,
    /// <summary>Battery backed SRAM.</summary>
    Sram = 1,
    /// <summary>Flash RAM.</summary>
    Flash = 2,
    /// <summary>Serial EEPROM.</summary>
    Eeprom = 3,
}

/// <summary>Settings written to the per-game <c>[RomOption]</c> ini.</summary>
/// <param name="GameName">Free text written in the comment header.</param>
/// <param name="Region">Region label written in the comment header (EU, US, JP).</param>
public sealed record VcIniOptions(string GameName, string Region)
{
    /// <summary>Save hardware.</summary>
    public BackupType BackupType { get; init; } = BackupType.Auto;
    /// <summary>One of 512, 2048, 4K, 16K; null omits the key (SRAM and Flash do not use it).</summary>
    public string? BackupSize { get; init; } = "512";
    /// <summary>Sync frame pacing to vertical retrace.</summary>
    public bool RetraceByVsync { get; init; }
    /// <summary>Use the timer based pacing mode.</summary>
    public bool UseTimer { get; init; }
    /// <summary>Boot through the real IPL path.</summary>
    public bool TrueBoot { get; init; }
    /// <summary>Emulate the 4 MB Expansion Pak (8 MB total RDRAM).</summary>
    public bool ExpansionPak { get; init; }
    /// <summary>Plug a Rumble Pak.</summary>
    public bool Rumble { get; init; }
    /// <summary>Plug a Controller Pak (ignored when <see cref="Rumble"/> is set).</summary>
    public bool MemPack { get; init; }
    /// <summary>Raw ini sections appended verbatim, for example <c>[Input]\nStickModify = 2\n</c>.</summary>
    public string? ExtraSections { get; init; }
}

/// <summary>Builds the per-game ini consumed by the Wii U N64 VC emulator (same layout as INICreator.exe).</summary>
public static class VcIni
{
    private static readonly string[] ValidSizes = ["512", "2048", "4K", "16K"];

    /// <summary>Renders the ini text with LF line endings.</summary>
    /// <exception cref="ArgumentException">Name or region contain line breaks, or the size is unknown.</exception>
    public static string Build(VcIniOptions o)
    {
        RejectLineBreaks(o.GameName, nameof(o.GameName));
        RejectLineBreaks(o.Region, nameof(o.Region));
        if (o.BackupSize is not null && Array.IndexOf(ValidSizes, o.BackupSize) < 0)
            throw new ArgumentException($"BackupSize must be one of {string.Join(", ", ValidSizes)}.", nameof(o));

        var sb = new System.Text.StringBuilder();
        sb.Append(';').Append(o.GameName).Append(' ').Append(o.Region).Append('\n');
        sb.Append("[RomOption]\n");
        sb.Append("BackupType = ").Append((int)o.BackupType).Append('\n');
        if (o.BackupSize is not null) sb.Append("BackupSize = ").Append(o.BackupSize).Append('\n');
        if (o.RetraceByVsync) sb.Append("RetraceByVsync = 1\n");
        if (o.UseTimer) sb.Append("UseTimer = 1\n");
        if (o.TrueBoot) sb.Append("TrueBoot = 1\n");
        if (o.ExpansionPak) sb.Append("RamSize = 0x800000\n");
        if (o.Rumble) sb.Append("Rumble = 1\n");
        else if (o.MemPack) sb.Append("MemPack = 1\n");
        if (!string.IsNullOrEmpty(o.ExtraSections))
        {
            sb.Append('\n').Append(o.ExtraSections.Replace("\r\n", "\n"));
            if (!o.ExtraSections.EndsWith('\n')) sb.Append('\n');
        }
        return sb.ToString();
    }

    private static void RejectLineBreaks(string value, string name)
    {
        if (value.AsSpan().IndexOfAny('\r', '\n') >= 0)
            throw new ArgumentException("Line breaks are not allowed.", name);
    }
}
