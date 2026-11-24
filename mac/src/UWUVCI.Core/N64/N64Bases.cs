using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UWUVCI.Core.N64;

/// <summary>One N64 Virtual Console base title the CDN serves.</summary>
/// <param name="Name">Game name.</param>
/// <param name="Region">EU, US or JP.</param>
/// <param name="TitleId">16 hex characters.</param>
/// <param name="KeyHash">SHA-1 (40 hex characters) of the lower case title key text, used to check a typed key (see <see cref="TitleKey"/>).</param>
public sealed record N64Base(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("region")] string Region,
    [property: JsonPropertyName("titleId")] string TitleId,
    [property: JsonPropertyName("keyHash")] string KeyHash)
{
    /// <summary>Text for lists, such as "Mario Golf (US)".</summary>
    [JsonIgnore]
    public string DisplayName => $"{Name} ({Region})";

    /// <summary>Folder name for a downloaded copy, such as "Mario Golf [US]".</summary>
    [JsonIgnore]
    public string FolderName => $"{Name} [{Region}]";
}

/// <summary>The embedded list of N64 bases.</summary>
public static class N64Bases
{
    private static readonly Lazy<IReadOnlyList<N64Base>> Table = new(Load);

    /// <summary>All bases in file order.</summary>
    public static IReadOnlyList<N64Base> All => Table.Value;

    /// <summary>Finds a base by title id (case insensitive), or null.</summary>
    public static N64Base? Find(string? titleId) =>
        All.FirstOrDefault(b => string.Equals(b.TitleId, titleId, StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyList<N64Base> Load()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("n64-bases.json")
            ?? throw new InvalidOperationException("Embedded n64-bases.json is missing.");
        return JsonSerializer.Deserialize<N64Base[]>(stream)
            ?? throw new InvalidDataException("n64-bases.json is empty.");
    }
}

/// <summary>Checks a typed title key against the hash stored for a base. No key is ever stored or shipped.</summary>
public static class TitleKey
{
    /// <summary>True when <paramref name="key"/> is 32 hex characters whose SHA-1 matches the base. Case insensitive.</summary>
    public static bool IsValid(N64Base b, string? key) =>
        key is { Length: 32 } && key.All(Uri.IsHexDigit)
        && string.Equals(Sha1Hex(key.ToLowerInvariant()), b.KeyHash, StringComparison.OrdinalIgnoreCase);

    /// <summary>Upper case hex SHA-1 of the UTF-8 text, the format the current UWUVCI base list uses.</summary>
    public static string Sha1Hex(string text) =>
        Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(text)));
}
