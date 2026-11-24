using System.Text.Json;
using System.Text.Json.Serialization;

namespace UWUVCI.Mac.Services;

/// <summary>Theme override chosen in Settings.</summary>
public enum ThemeChoice
{
    /// <summary>Follow the macOS appearance.</summary>
    System,
    /// <summary>Always light.</summary>
    Light,
    /// <summary>Always dark.</summary>
    Dark,
}

/// <summary>What the app remembers between launches.</summary>
public sealed record AppSettings
{
    /// <summary>Wii U common key, 32 hex characters, or empty. Never log this.</summary>
    public string CommonKey { get; init; } = "";

    /// <summary>Default output folder, or null for the built in default.</summary>
    public string? OutputDir { get; init; }

    /// <summary>Theme override.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ThemeChoice Theme { get; init; } = ThemeChoice.System;

    /// <summary>Title keys the user typed, by lower case title id. Secrets: never log them.</summary>
    public IReadOnlyDictionary<string, string> TitleKeys { get; init; } = new Dictionary<string, string>();

    /// <summary>Prints nothing from the fields, so a key can not reach a log or an error by accident.</summary>
    public override string ToString() => nameof(AppSettings);
}

/// <summary>Persistence for <see cref="AppSettings"/>.</summary>
public interface ISettingsStore
{
    /// <summary>Loads the settings, or defaults when nothing usable is stored.</summary>
    AppSettings Load();

    /// <summary>Stores the settings.</summary>
    void Save(AppSettings settings);
}

/// <summary>Stores settings as JSON, readable by the owner only (mode 600).</summary>
/// <param name="path">Full path of the settings file.</param>
public sealed class JsonSettingsStore(string path) : ISettingsStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    /// <inheritdoc />
    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(path)) return new AppSettings();
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), Options) ?? new AppSettings();
        }
        catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException)
        {
            return new AppSettings();
        }
    }

    /// <inheritdoc />
    public void Save(AppSettings settings)
    {
        var dir = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(dir);
        File.SetUnixFileMode(dir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

        // Create the temp file with mode 600 from the start so the key is never world readable, then swap.
        var tmp = path + ".tmp";
        File.Delete(tmp);
        using (var fs = new FileStream(tmp, new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.Write,
            UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite,
        }))
        {
            JsonSerializer.Serialize(fs, settings, Options);
        }
        File.Move(tmp, path, overwrite: true);
    }
}
