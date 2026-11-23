using WiiUDownloaderLibrary;
using WiiUDownloaderLibrary.Models;

namespace UWUVCI.Core.Injection;

/// <summary>Downloads a base title from the Nintendo CDN and decrypts it into a plain folder.</summary>
public static class BaseProvider
{
    /// <summary>Downloads and decrypts a title. The user supplies the title id, title key and common key.</summary>
    /// <param name="titleId">16 hex characters.</param>
    /// <param name="titleKey">32 hex characters.</param>
    /// <param name="commonKey">32 hex characters.</param>
    /// <param name="destDir">Folder that receives code, content and meta.</param>
    /// <param name="progress">Optional progress sink.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async Task DownloadAsync(string titleId, string titleKey, string commonKey, string destDir,
        IProgress<InjectProgress>? progress = null, CancellationToken ct = default)
    {
        Validate(titleId, 16, nameof(titleId));
        Validate(titleKey, 32, nameof(titleKey));
        Validate(commonKey, 32, nameof(commonKey));

        var work = Directory.CreateTempSubdirectory("uwuvci-dl-");
        try
        {
            var inner = new Progress<(double percent, string message)>(p =>
                progress?.Report(new InjectProgress((int)(p.percent * 0.8), p.message)));
            var result = await new Downloader(null, null)
                .DownloadAsync(new TitleData(titleId, titleKey), work.FullName, inner, ct).ConfigureAwait(false);
            if (!result.Success)
                throw new IOException($"Download failed: {result.Error}");

            progress?.Report(new InjectProgress(85, "Decrypting base..."));
            DecryptTitle(Path.Combine(work.FullName, titleId), destDir, commonKey);
            progress?.Report(new InjectProgress(100, "Base ready"));
        }
        finally
        {
            work.Delete(true);
        }
    }

    /// <summary>Decrypts a downloaded or dumped title folder (tmd, cetk and .app files) into <paramref name="destDir"/>.</summary>
    public static void DecryptTitle(string encryptedDir, string destDir, string commonKey)
    {
        Validate(commonKey, 32, nameof(commonKey));
        if (!Directory.Exists(encryptedDir))
            throw new DirectoryNotFoundException(encryptedDir);
        var previous = Directory.GetCurrentDirectory();
        try
        {
            CSharpDecrypt.CSharpDecrypt.Decrypt([commonKey, encryptedDir, destDir]);
        }
        finally
        {
            Directory.SetCurrentDirectory(previous);
        }
    }

    private static void Validate(string value, int length, string name)
    {
        if (value.Length != length || !value.All(Uri.IsHexDigit))
            throw new ArgumentException($"{name} must be {length} hexadecimal characters.", name);
    }
}
