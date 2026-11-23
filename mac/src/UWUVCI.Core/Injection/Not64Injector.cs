using System.Text.RegularExpressions;
using UWUVCI.Core.N64;

namespace UWUVCI.Core.Injection;

/// <summary>Where a native inject put its two outputs.</summary>
/// <param name="TitleDir">Edited title (code, content, meta), ready to pack. Never contains the ROM.</param>
/// <param name="SdDir">Files to copy to the SD card root: not64/roms/ and not64/autoboot.txt.</param>
public sealed record NativeLayout(string TitleDir, string SdDir);

/// <summary>Builds a title around the native not64 emulator, with the ROM kept on the SD card.</summary>
public static class Not64Injector
{
    /// <summary>Copies the template, edits its identity and writes the SD files.</summary>
    /// <param name="templateDir">Folder with code, content and meta of the not64 title.</param>
    /// <param name="romPath">Any N64 ROM (.z64, .v64, .n64).</param>
    /// <param name="gameName">Name shown in the Wii U menu.</param>
    /// <param name="workRoot">Empty work folder.</param>
    /// <param name="rng">Random source for the title id.</param>
    public static NativeLayout Prepare(string templateDir, string romPath, string gameName, string workRoot, Random rng)
    {
        var z64 = N64Rom.ToZ64(File.ReadAllBytes(romPath)); // validates before anything is written
        var title = Path.Combine(workRoot, "title");
        var sd = Path.Combine(workRoot, "sd");
        CopyDirectory(templateDir, title);

        MetaXml.Apply(Path.Combine(title, "meta", "meta.xml"), Path.Combine(title, "code", "app.xml"), gameName, rng);

        var romName = SafeRomName(Path.GetFileName(romPath));
        var romDir = Path.Combine(sd, "not64", "roms");
        Directory.CreateDirectory(romDir);
        File.WriteAllBytes(Path.Combine(romDir, romName), z64);
        File.WriteAllText(Path.Combine(sd, "not64", "autoboot.txt"), $"roms/{romName}\n");
        return new NativeLayout(title, sd);
    }

    /// <summary>Makes a file name safe for the FAT SD card and the autoboot line, always ending in .z64.</summary>
    public static string SafeRomName(string fileName)
    {
        var stem = Path.GetFileNameWithoutExtension(Path.GetFileName(fileName.Replace('\\', '/')));
        stem = Regex.Replace(stem, @"[\[\(][^\]\)]*[\]\)]", " ");      // drop (U) [!] tags
        stem = Regex.Replace(stem, @"[^A-Za-z0-9._'-]+", " ").Trim().Replace(' ', '-');
        stem = Regex.Replace(stem, "-{2,}", "-").Trim('-', '.');
        return (stem.Length == 0 ? "game" : stem) + ".z64";
    }

    private static void CopyDirectory(string src, string dst)
    {
        Directory.CreateDirectory(dst);
        foreach (var f in Directory.GetFiles(src)) File.Copy(f, Path.Combine(dst, Path.GetFileName(f)), true);
        foreach (var d in Directory.GetDirectories(src)) CopyDirectory(d, Path.Combine(dst, Path.GetFileName(d)));
    }
}
