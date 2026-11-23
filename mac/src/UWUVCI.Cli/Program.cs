using UWUVCI.Core.Injection;
using UWUVCI.Core.N64;

return Cli.Run(args);

/// <summary>Headless entry point: inspect ROMs and build titles without the GUI.</summary>
internal static class Cli
{
    private const string Usage = """
        uwuvci-cli detect <rom>
            Show the detected game and the VC ini that would be written.
        uwuvci-cli native-layout <rom> <name> <out-dir> [--template <dir>]
            Build the unpacked native (not64) title and the SD files. Loadiine layout, runnable in Cemu.
        uwuvci-cli native-pack <rom> <name> <out-dir> --key <32 hex> [--template <dir>]
            Same, then encrypt the title with your Wii U common key.
        """;

    public static int Run(string[] args)
    {
        try
        {
            return args switch
            {
                ["detect", var rom] => Detect(rom),
                ["native-layout", var rom, var name, var outDir, ..var rest] => Native(rom, name, outDir, rest, pack: false),
                ["native-pack", var rom, var name, var outDir, ..var rest] => Native(rom, name, outDir, rest, pack: true),
                _ => Fail(Usage),
            };
        }
        catch (Exception e) when (e is IOException or InvalidDataException or ArgumentException or HttpRequestException)
        {
            return Fail($"error: {e.Message}");
        }
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 2;
    }

    private static int Detect(string rom)
    {
        var bytes = File.ReadAllBytes(rom);
        var header = N64Rom.ReadHeader(bytes);
        var game = N64GameDatabase.Find(header.Crc1, header.Crc2);
        Console.WriteLine($"Format : {N64Rom.Detect(bytes)}");
        Console.WriteLine($"Title  : {header.Title} ({header.GameCode})");
        Console.WriteLine($"CRC    : {header.Crc1:X8} {header.Crc2:X8}");
        Console.WriteLine($"Known  : {game?.Name ?? "no, using safe defaults"}");
        Console.WriteLine();
        Console.Write(VcIni.Build(N64Profiles.For(game, header, null)));
        return 0;
    }

    private static int Native(string rom, string name, string outDir, string[] rest, bool pack)
    {
        string? template = Option(rest, "--template");
        string? key = Option(rest, "--key");
        Directory.CreateDirectory(outDir);
        var cache = Path.Combine(outDir, ".template");
        template ??= Not64Template.FetchLatestAsync(cache).GetAwaiter().GetResult();

        if (pack)
        {
            if (key is null) return Fail("--key is required to pack");
            var req = new InjectRequest(GameConsole.N64, rom, template, name);
            var layout = WupInjector.RunNative(req, template, outDir, key);
            Console.WriteLine($"Title: {layout.TitleDir}\nSD   : {layout.SdDir}");
        }
        else
        {
            var work = Path.Combine(outDir, "native");
            if (Directory.Exists(work)) Directory.Delete(work, true);
            var layout = Not64Injector.Prepare(template, rom, name, work, Random.Shared);
            Console.WriteLine($"Title: {layout.TitleDir}\nSD   : {layout.SdDir}");
        }
        return 0;
    }

    private static string? Option(string[] args, string name)
    {
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
