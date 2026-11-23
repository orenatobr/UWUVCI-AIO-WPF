namespace UWUVCI.Core.Injection;

/// <summary>Everything the user chose for one inject.</summary>
public sealed record InjectRequest(GameConsole Console, string RomPath, string BaseDir, string GameName)
{
    /// <summary>N64 specific options.</summary>
    public N64Options N64 { get; init; } = new();
    /// <summary>Menu icon (any image), 128x128 output.</summary>
    public string? IconPath { get; init; }
    /// <summary>TV boot screen (any image), 1280x720 output.</summary>
    public string? TvBootPath { get; init; }
    /// <summary>GamePad boot screen, 854x480 output. Falls back to the TV image.</summary>
    public string? DrcBootPath { get; init; }
    /// <summary>Boot logo, 170x42 output.</summary>
    public string? LogoPath { get; init; }
}

/// <summary>Runs the inject steps on a temporary copy of the base and packs the result.</summary>
public static class WupInjector
{
    /// <summary>Copies the base to a work folder and applies ROM, meta and images. Returns the work folder.</summary>
    public static string Prepare(InjectRequest req, string workRoot, Random? rng = null, IProgress<InjectProgress>? progress = null)
    {
        if (req.Console != GameConsole.N64)
            throw new NotSupportedException($"{req.Console} injection is not available in the macOS build yet.");
        if (!File.Exists(req.RomPath)) throw new FileNotFoundException("ROM not found.", req.RomPath);
        if (!Directory.Exists(req.BaseDir)) throw new DirectoryNotFoundException(req.BaseDir);

        var work = Path.Combine(workRoot, "base");
        if (Directory.Exists(work)) Directory.Delete(work, true);

        progress?.Report(new InjectProgress(5, "Copying base..."));
        CopyDirectory(req.BaseDir, work);

        progress?.Report(new InjectProgress(25, "Injecting ROM..."));
        N64Injector.Inject(work, req.RomPath, req.N64);

        progress?.Report(new InjectProgress(60, "Editing meta.xml..."));
        MetaXml.Apply(Path.Combine(work, "meta", "meta.xml"), Path.Combine(work, "code", "app.xml"),
            req.GameName, rng ?? Random.Shared);

        progress?.Report(new InjectProgress(70, "Converting images..."));
        WriteImages(req, Path.Combine(work, "meta"));
        return work;
    }

    /// <summary>Full inject: prepare, pack and clean up. Returns the output title folder.</summary>
    public static string Run(InjectRequest req, string outputRoot, string commonKey, IProgress<InjectProgress>? progress = null)
    {
        var temp = Directory.CreateTempSubdirectory("uwuvci-work-");
        try
        {
            var work = Prepare(req, temp.FullName, null, progress);
            var output = UniqueOutput(outputRoot, req.Console, req.GameName);
            progress?.Report(new InjectProgress(85, "Packing..."));
            WupPacker.Pack(work, output, commonKey);
            progress?.Report(new InjectProgress(100, "Done"));
            return output;
        }
        finally
        {
            temp.Delete(true);
        }
    }

    internal static string UniqueOutput(string root, GameConsole console, string name)
    {
        var clean = System.Text.RegularExpressions.Regex.Replace(name, "[^a-zA-Z0-9 -]", "").Trim();
        if (clean.Length == 0) clean = "NoName";
        var path = Path.Combine(root, $"[WUP][{console.ToString().ToUpperInvariant()}] {clean}");
        for (int i = 0; Directory.Exists(path); i++)
            path = Path.Combine(root, $"[WUP][{console.ToString().ToUpperInvariant()}] {clean}_{i}");
        return path;
    }

    private static void WriteImages(InjectRequest r, string metaDir)
    {
        void Write(string? src, string file, int w, int h, int bpp)
        {
            if (src is null) return;
            File.WriteAllBytes(Path.Combine(metaDir, file), TgaImage.FromImage(File.ReadAllBytes(src), w, h, bpp));
        }
        Write(r.IconPath, "iconTex.tga", 128, 128, 32);
        Write(r.TvBootPath, "bootTvTex.tga", 1280, 720, 24);
        Write(r.DrcBootPath ?? r.TvBootPath, "bootDrcTex.tga", 854, 480, 24);
        Write(r.LogoPath, "bootLogoTex.tga", 170, 42, 32);
    }

    private static void CopyDirectory(string src, string dst)
    {
        Directory.CreateDirectory(dst);
        foreach (var f in Directory.GetFiles(src))
            File.Copy(f, Path.Combine(dst, Path.GetFileName(f)), true);
        foreach (var d in Directory.GetDirectories(src))
            CopyDirectory(d, Path.Combine(dst, Path.GetFileName(d)));
    }
}
