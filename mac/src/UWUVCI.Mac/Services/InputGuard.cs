namespace UWUVCI.Mac.Services;

/// <summary>Outcome of validating a user supplied path: a canonical path, or an error message.</summary>
/// <param name="Path">Canonical absolute path when valid.</param>
/// <param name="Error">Human readable reason when invalid.</param>
public readonly record struct Checked(string? Path, string? Error)
{
    /// <summary>True when <see cref="Error"/> is null.</summary>
    public bool IsValid => Error is null;
}

/// <summary>Validation and canonicalization of everything that comes from the UI. Files are untrusted input.</summary>
public static class InputGuard
{
    /// <summary>Largest ROM the app reads (128 MiB).</summary>
    public const long MaxRomBytes = 128L * 1024 * 1024;

    /// <summary>Largest image or ini file the app reads (16 MiB).</summary>
    public const long MaxSmallFileBytes = 16L * 1024 * 1024;

    /// <summary>Accepted ROM extensions.</summary>
    public static readonly string[] RomExtensions = [".z64", ".v64", ".n64"];

    /// <summary>Accepted image extensions.</summary>
    public static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".bmp"];

    /// <summary>True when <paramref name="value"/> is exactly <paramref name="length"/> hex digits.</summary>
    public static bool IsHex(string? value, int length) =>
        value is not null && value.Length == length && value.All(Uri.IsHexDigit);

    /// <summary>Checks an existing regular file: absolute path, allowed extension, size limit. Resolves symlinks.</summary>
    /// <param name="path">Path from the UI.</param>
    /// <param name="maxBytes">Size limit.</param>
    /// <param name="extensions">Allowed lower case extensions, or empty for any.</param>
    public static Checked CheckFile(string? path, long maxBytes, string[] extensions)
    {
        var abs = Absolute(path);
        if (abs is null) return new(null, "Choose a file with a full path.");
        try
        {
            var info = new FileInfo(abs);
            if (info.ResolveLinkTarget(true) is FileInfo target) info = target;
            if (!info.Exists) return new(null, "The file does not exist.");
            if (extensions.Length > 0 && !extensions.Contains(info.Extension.ToLowerInvariant()))
                return new(null, $"Unsupported file type. Expected {string.Join(", ", extensions)}.");
            if (info.Length > maxBytes)
                return new(null, $"The file is larger than the {maxBytes / (1024 * 1024)} MiB limit.");
            return new(info.FullName, null);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return new(null, "The file cannot be read.");
        }
    }

    /// <summary>Checks an image: extension, size limit and PNG, JPEG or BMP magic bytes.</summary>
    public static Checked CheckImage(string? path)
    {
        var r = CheckFile(path, MaxSmallFileBytes, ImageExtensions);
        if (!r.IsValid) return r;
        try
        {
            Span<byte> head = stackalloc byte[8];
            using var fs = File.OpenRead(r.Path!);
            int n = fs.Read(head);
            var h = head[..n];
            bool png = h.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47 });
            bool jpg = h.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF });
            bool bmp = h.StartsWith(new byte[] { 0x42, 0x4D });
            return png || jpg || bmp ? r : new(null, "This does not look like a PNG, JPEG or BMP image.");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return new(null, "The file cannot be read.");
        }
    }

    /// <summary>Checks a decrypted base: a folder with <c>code</c>, <c>content</c> and <c>meta</c> inside.</summary>
    public static Checked CheckBaseFolder(string? path)
    {
        var abs = Absolute(path);
        if (abs is null) return new(null, "Choose a folder with a full path.");
        if (!Directory.Exists(abs)) return new(null, "The folder does not exist.");
        var missing = new[] { "code", "content", "meta" }.Where(d => !Directory.Exists(Path.Combine(abs, d))).ToArray();
        return missing.Length == 0
            ? new(abs, null)
            : new(null, $"Not a decrypted base. Missing: {string.Join(", ", missing)}.");
    }

    /// <summary>Checks an output folder. It may not exist yet, but it must not be a file.</summary>
    public static Checked CheckOutputFolder(string? path)
    {
        var abs = Absolute(path);
        if (abs is null) return new(null, "Choose a folder with a full path.");
        return File.Exists(abs) ? new(null, "That path is a file, not a folder.") : new(abs, null);
    }

    /// <summary>Expands a leading <c>~</c> and returns a canonical absolute path, or null when unusable.</summary>
    public static string? Absolute(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Contains('\0')) return null;
        path = path.Trim();
        if (path == "~" || path.StartsWith("~/", StringComparison.Ordinal))
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), path.TrimStart('~').TrimStart('/'));
        if (!Path.IsPathRooted(path)) return null;
        return Path.GetFullPath(path);
    }
}
