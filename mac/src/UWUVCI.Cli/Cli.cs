using UWUVCI.Core.Injection;
using UWUVCI.Core.N64;

/// <summary>Headless entry point: inspect ROMs and build titles without the GUI.</summary>
public static class Cli
{
    private const string Usage = """
        uwuvci-cli detect <rom>
            Show the detected game and the VC ini that would be written.
        uwuvci-cli vc-layout <rom> <base-dir> <name> <out-dir> [--pak on|off] [--save-type auto|sram|flash|eeprom] [--widescreen] [--no-dark-filter] [--shadow-ellipses | --no-shadow-ellipses]
            Inject the ROM into a copy of an official N64 base (a decrypted folder with code, content, meta),
            unencrypted, in <out-dir>/base. Run <out-dir>/base/code/*.rpx in Cemu to test it. The base is not changed.
        uwuvci-cli vc-pack <rom> <base-dir> <name> <out-dir> --key <32 hex> [--pak on|off] [--save-type auto|sram|flash|eeprom] [--widescreen] [--no-dark-filter] [--shadow-ellipses | --no-shadow-ellipses]
            Same, then encrypt into an installable title with your Wii U common key.
        uwuvci-cli base-download <title-id> <dest-dir>
            Download and decrypt a base from the Nintendo CDN into <dest-dir>. The keys come from the environment so
            they never appear in a command line or a log: UWUVCI_TITLE_KEY and UWUVCI_COMMON_KEY (32 hex each).
        uwuvci-cli native-layout <rom> <name> <out-dir> [--template <dir>]
            Build the unpacked native (not64) title and the SD files. Loadiine layout, runnable in Cemu.
        uwuvci-cli native-pack <rom> <name> <out-dir> --key <32 hex> [--template <dir>]
            Same, then encrypt the title with your Wii U common key.
        """;

    /// <summary>Runs one command and returns the exit code (0 ok, 2 bad input or failure).</summary>
    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        try
        {
            return args switch
            {
                ["detect", var rom] => Detect(rom, output),
                ["base-download", var titleId, var dest] => BaseDownload(titleId, dest, output),
                ["vc-layout", var rom, var baseDir, var name, var outDir, ..var rest] => Vc(rom, baseDir, name, outDir, rest, false, output),
                ["vc-pack", var rom, var baseDir, var name, var outDir, ..var rest] => Vc(rom, baseDir, name, outDir, rest, true, output),
                ["native-layout", var rom, var name, var outDir, ..var rest] => Native(rom, name, outDir, rest, false, output, error),
                ["native-pack", var rom, var name, var outDir, ..var rest] => Native(rom, name, outDir, rest, true, output, error),
                _ => Fail(error, Usage),
            };
        }
        catch (Exception e) when (e is IOException or InvalidDataException or ArgumentException or HttpRequestException
                                      or UnauthorizedAccessException or NotSupportedException)
        {
            return Fail(error, $"error: {e.Message}");
        }
    }

    private static int Fail(TextWriter error, string message)
    {
        error.WriteLine(message);
        return 2;
    }

    private static int Detect(string rom, TextWriter output)
    {
        var bytes = File.ReadAllBytes(rom);
        var header = N64Rom.ReadHeader(bytes);
        var game = N64GameDatabase.Find(header.Crc1, header.Crc2);
        output.WriteLine($"Format : {N64Rom.Detect(bytes)}");
        output.WriteLine($"Title  : {header.Title} ({header.GameCode})");
        output.WriteLine($"CRC    : {header.Crc1:X8} {header.Crc2:X8}");
        output.WriteLine($"Known  : {game?.Name ?? "no, using safe defaults"}");
        output.WriteLine();
        output.Write(VcIni.Build(N64Profiles.For(game, header, null)));
        return 0;
    }

    private static int BaseDownload(string titleId, string dest, TextWriter output)
    {
        var titleKey = Environment.GetEnvironmentVariable("UWUVCI_TITLE_KEY")
            ?? throw new ArgumentException("Set UWUVCI_TITLE_KEY (32 hex characters).");
        var commonKey = Environment.GetEnvironmentVariable("UWUVCI_COMMON_KEY")
            ?? throw new ArgumentException("Set UWUVCI_COMMON_KEY (32 hex characters).");
        var known = N64Bases.Find(titleId) ?? throw new ArgumentException("That title id is not in the N64 base list.");
        if (!TitleKey.IsValid(known, titleKey))
            throw new ArgumentException("The title key does not match the selected base.");
        var last = -1;
        var progress = new Progress<InjectProgress>(p =>
        {
            if (p.Percent / 10 != last) { last = p.Percent / 10; output.WriteLine($"{p.Percent,3}% {p.Message}"); }
        });
        BaseProvider.DownloadAsync(titleId, titleKey, commonKey, dest, progress).GetAwaiter().GetResult();
        output.WriteLine($"Base: {dest}");
        return 0;
    }

    private static int Vc(string rom, string baseDir, string name, string outDir, string[] rest, bool pack, TextWriter output)
    {
        bool? pak = Option(rest, "--pak") switch { "on" => true, "off" => false, null => null, var v => throw new ArgumentException($"--pak must be on or off, not '{v}'.") };
        BackupType? save = Option(rest, "--save-type") switch
        {
            "auto" => BackupType.Auto, "sram" => BackupType.Sram, "flash" => BackupType.Flash, "eeprom" => BackupType.Eeprom,
            null => null,
            var v => throw new ArgumentException($"--save-type must be auto, sram, flash or eeprom, not '{v}'."),
        };
        var req = new InjectRequest(GameConsole.N64, rom, baseDir, name)
        {
            N64 = new N64Options { ExpansionPak = pak, SaveType = save, WideScreen = rest.Contains("--widescreen"), DarkFilter = rest.Contains("--no-dark-filter"), ShadowEllipses = rest.Contains("--no-shadow-ellipses") ? false : rest.Contains("--shadow-ellipses") ? true : null }
        };
        Directory.CreateDirectory(outDir);
        if (pack)
        {
            var key = Option(rest, "--key") ?? throw new ArgumentException("--key is required to pack (32 hex characters).");
            var title = WupInjector.Run(req, outDir, key);
            output.WriteLine($"Title: {title}");
            return 0;
        }
        var work = Path.Combine(outDir, "work");
        var prepared = WupInjector.Prepare(req, work);
        var target = Path.Combine(outDir, "base");
        if (Directory.Exists(target)) Directory.Delete(target, true);
        Directory.Move(prepared, target);
        Directory.Delete(work, true);
        output.WriteLine($"Base: {target}");
        output.WriteLine($"Run in Cemu: {Path.Combine(target, "code")}/<app>.rpx");
        return 0;
    }

    private static int Native(string rom, string name, string outDir, string[] rest, bool pack, TextWriter output, TextWriter error)
    {
        string? template = Option(rest, "--template");
        string? key = Option(rest, "--key");
        Directory.CreateDirectory(outDir);
        var cache = Path.Combine(outDir, ".template");
        template ??= Not64Template.FetchLatestAsync(cache).GetAwaiter().GetResult();

        if (pack)
        {
            if (key is null) return Fail(error, "--key is required to pack");
            var req = new InjectRequest(GameConsole.N64, rom, template, name);
            var layout = WupInjector.RunNative(req, template, outDir, key);
            output.WriteLine($"Title: {layout.TitleDir}\nSD   : {layout.SdDir}");
        }
        else
        {
            var work = Path.Combine(outDir, "native");
            if (Directory.Exists(work)) Directory.Delete(work, true);
            var layout = Not64Injector.Prepare(template, rom, name, work, Random.Shared);
            output.WriteLine($"Title: {layout.TitleDir}\nSD   : {layout.SdDir}");
        }
        return 0;
    }

    private static string? Option(string[] args, string name)
    {
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
