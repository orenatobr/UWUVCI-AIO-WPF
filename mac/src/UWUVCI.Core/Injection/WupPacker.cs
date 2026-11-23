namespace UWUVCI.Core.Injection;

/// <summary>Packs an edited base folder into an installable, encrypted Wii U title (WUP).</summary>
public static class WupPacker
{
    private static readonly System.Text.RegularExpressions.Regex Hex32 = new("^[0-9A-Fa-f]{32}$");

    /// <summary>Encrypts and packs <paramref name="baseDir"/> into <paramref name="outDir"/>.</summary>
    /// <param name="baseDir">Folder with code, content and meta.</param>
    /// <param name="outDir">Destination folder, created by the packer.</param>
    /// <param name="commonKey">Wii U common key, 32 hex characters, supplied by the user.</param>
    /// <exception cref="ArgumentException">Key is malformed.</exception>
    /// <exception cref="DirectoryNotFoundException">Base folder is missing a required subfolder.</exception>
    public static void Pack(string baseDir, string outDir, string commonKey)
    {
        if (!Hex32.IsMatch(commonKey))
            throw new ArgumentException("The common key must be 32 hexadecimal characters.", nameof(commonKey));
        foreach (var sub in new[] { "code", "content", "meta" })
            if (!Directory.Exists(Path.Combine(baseDir, sub)))
                throw new DirectoryNotFoundException($"Base folder is missing '{sub}'.");

        // CNUSPACKER calls Environment.Exit on content it cannot map, which would kill the whole app.
        // Anything outside the three title folders is the known trigger, so refuse it up front.
        var stray = Directory.GetFileSystemEntries(baseDir)
            .Select(Path.GetFileName)
            .Where(n => n is not ("code" or "content" or "meta"))
            .ToArray();
        if (stray.Length > 0)
            throw new InvalidDataException($"The base folder contains unexpected entries: {string.Join(", ", stray)}.");

        // CNUSPACKER reads and writes relative to the working directory.
        var previous = Directory.GetCurrentDirectory();
        try
        {
            CNUSPACKER.Program.Main(["-in", baseDir, "-out", outDir, "-encryptKeyWith", commonKey]);
        }
        finally
        {
            Directory.SetCurrentDirectory(previous);
        }
    }
}
