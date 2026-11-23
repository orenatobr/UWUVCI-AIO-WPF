using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UWUVCI.Core.N64;

/// <summary>One N64 Virtual Console base title the CDN serves.</summary>
/// <param name="Name">Game name.</param>
/// <param name="Region">EU, US or JP.</param>
/// <param name="TitleId">16 hex characters.</param>
/// <param name="KeyHash">Hash of the lower case title key, used to check a typed key (see <see cref="TitleKey"/>).</param>
public sealed record N64Base(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("region")] string Region,
    [property: JsonPropertyName("titleId")] string TitleId,
    [property: JsonPropertyName("keyHash")] int KeyHash)
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
    /// <summary>True when <paramref name="key"/> is 32 hex characters whose hash matches the base. Case insensitive.</summary>
    public static bool IsValid(N64Base b, string? key) =>
        key is { Length: 32 } && key.All(Uri.IsHexDigit) && Hash(key.ToLowerInvariant()) == b.KeyHash;

    /// <summary>The deterministic string hash of the original UWUVCI (the classic .NET Framework algorithm).</summary>
    public static int Hash(string str)
    {
        unchecked
        {
            int hash1 = (5381 << 16) + 5381;
            int hash2 = hash1;
            for (int i = 0; i < str.Length; i += 2)
            {
                hash1 = ((hash1 << 5) + hash1) ^ str[i];
                if (i == str.Length - 1) break;
                hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
            }
            return hash1 + (hash2 * 1566083941);
        }
    }
}
