using UWUVCI.Core.N64;

namespace UWUVCI.Core.Injection;

/// <summary>Puts an N64 ROM and its ini into an unpacked Wii U N64 VC base.</summary>
public static class N64Injector
{
    /// <summary>Injects the ROM into <paramref name="baseDir"/> (the folder holding code, content, meta).</summary>
    /// <exception cref="InvalidDataException">ROM is not a valid N64 image, or the base has no ROM slot.</exception>
    public static void Inject(string baseDir, string romPath, N64Options options)
    {
        // Validate and convert first so a bad ROM never leaves a half edited base behind.
        var raw = File.ReadAllBytes(romPath);
        var z64 = N64Rom.ToZ64(raw);
        var header = N64Rom.ReadHeader(z64);

        var game = N64GameDatabase.Find(header.Crc1, header.Crc2);

        // Prepare the engine patch in memory too, so an engine that cannot take it fails before anything is written.
        string? enginePath = null; byte[]? engine = null;
        bool eeprom16K = N64Profiles.NeedsEeprom16KAnswer(game, header);
        bool shadows = options.ShadowEllipses ?? N64Profiles.WantsShadowEllipses(game, header);
        if (eeprom16K || shadows)
        {
            enginePath = Directory.Exists(Path.Combine(baseDir, "code")) ? Directory.GetFiles(Path.Combine(baseDir, "code"), "*.rpx").FirstOrDefault() : null;
            if (enginePath is null) throw new InvalidDataException("The base has no VC engine (.rpx) in code.");
            engine = File.ReadAllBytes(enginePath);
            if (eeprom16K) engine = VcEnginePatches.SampleBothTextures(VcEnginePatches.AnswerEeprom16K(engine));
            if (shadows)
            {
                // Chosen by the game (null): an engine that cannot take it keeps the stock draw. Asked for (true): it is an error.
                try { engine = VcEnginePatches.ShadowEllipses(engine); }
                catch (InvalidDataException) when (options.ShadowEllipses is null) { }
            }
        }

        var romDir = Path.Combine(baseDir, "content", "rom");
        var slots = Directory.Exists(romDir) ? Directory.GetFiles(romDir) : [];
        if (slots.Length == 0)
            throw new InvalidDataException("The base has no ROM in content/rom.");
        var slot = slots[0];

        File.WriteAllBytes(slot, z64);

        var configDir = Path.Combine(baseDir, "content", "config");
        Directory.CreateDirectory(configDir);
        var iniPath = Path.Combine(configDir, Path.GetFileName(slot) + ".ini");
        if (options.CustomIniPath is { } custom)
        {
            File.Copy(custom, iniPath, overwrite: true);
        }
        else
        {
            File.WriteAllText(iniPath, VcIni.Build(N64Profiles.For(game, header, options.ExpansionPak, options.SaveType)));
        }

        if (engine is not null) File.WriteAllBytes(enginePath!, engine);

        var crop = N64Profiles.FrameCrop(game, header);
        if (options.WideScreen || options.DarkFilter || crop is not null)
        {
            var layout = Path.Combine(baseDir, "content", "FrameLayout.arc");
            var arc = File.ReadAllBytes(layout);
            N64FrameLayoutPatcher.Apply(arc, options.WideScreen, options.DarkFilter, crop);
            File.WriteAllBytes(layout, arc);
        }
    }
}
