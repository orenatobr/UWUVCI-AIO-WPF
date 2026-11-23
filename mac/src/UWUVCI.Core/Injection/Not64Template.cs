using System.IO.Compression;
using System.Text.Json;

namespace UWUVCI.Core.Injection;

/// <summary>Fetches and unpacks the not64-wiiu native title that serves as base for the native engine.</summary>
public static class Not64Template
{
    private const string LatestReleaseApi = "https://api.github.com/repos/orenatobr/not64-wiiu/releases/latest";
    private const string AssetName = "not64-wiiu-wup.zip";
    private const long MaxZipBytes = 64L * 1024 * 1024;
    private const long MaxExtractedBytes = 256L * 1024 * 1024;

    /// <summary>Downloads the latest release asset and extracts it. Returns the folder with code, content, meta.</summary>
    /// <param name="destDir">Work folder for the download and extraction.</param>
    /// <param name="http">HTTP client; a default one is used when null.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async Task<string> FetchLatestAsync(string destDir, HttpClient? http = null, CancellationToken ct = default)
    {
        using var owned = http is null ? new HttpClient() : null;
        var client = http ?? owned!;
        client.DefaultRequestHeaders.UserAgent.ParseAdd("uwuvci-mac");

        using var meta = JsonDocument.Parse(await client.GetStringAsync(LatestReleaseApi, ct).ConfigureAwait(false));
        string? url = null;
        foreach (var a in meta.RootElement.GetProperty("assets").EnumerateArray())
            if (a.GetProperty("name").GetString() == AssetName)
                url = a.GetProperty("browser_download_url").GetString();
        if (url is null || !url.StartsWith("https://github.com/orenatobr/not64-wiiu/", StringComparison.Ordinal))
            throw new InvalidDataException($"Release asset {AssetName} was not found.");

        Directory.CreateDirectory(destDir);
        var zip = Path.Combine(destDir, AssetName);
        using (var resp = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false))
        {
            resp.EnsureSuccessStatusCode();
            if (resp.Content.Headers.ContentLength is > MaxZipBytes)
                throw new InvalidDataException("The template download is unexpectedly large.");
            await using var fs = File.Create(zip);
            await resp.Content.CopyToAsync(fs, ct).ConfigureAwait(false);
        }
        return ExtractZip(zip, Path.Combine(destDir, "extracted"));
    }

    /// <summary>Extracts a template zip safely and returns the folder holding code, content and meta.</summary>
    /// <exception cref="InvalidDataException">Path traversal, oversize content, or no title folders inside.</exception>
    public static string ExtractZip(string zipPath, string destDir)
    {
        var root = Path.GetFullPath(destDir) + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(destDir);
        long total = 0;
        using var zip = ZipFile.OpenRead(zipPath);
        foreach (var entry in zip.Entries)
        {
            var target = Path.GetFullPath(Path.Combine(destDir, entry.FullName));
            if (!target.StartsWith(root, StringComparison.Ordinal))
                throw new InvalidDataException("The archive contains an unsafe path.");
            total += entry.Length;
            if (total > MaxExtractedBytes)
                throw new InvalidDataException("The archive expands to an unexpected size.");
            if (entry.FullName.EndsWith('/')) { Directory.CreateDirectory(target); continue; }
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            entry.ExtractToFile(target, overwrite: true);
        }

        foreach (var dir in Directory.EnumerateDirectories(destDir, "*", SearchOption.AllDirectories).Prepend(destDir))
            if (Directory.Exists(Path.Combine(dir, "code")) && Directory.Exists(Path.Combine(dir, "content"))
                && Directory.Exists(Path.Combine(dir, "meta")))
                return dir;
        throw new InvalidDataException("The archive does not contain a Wii U title (code, content, meta).");
    }
}
