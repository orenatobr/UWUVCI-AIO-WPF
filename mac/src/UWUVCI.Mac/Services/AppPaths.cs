using System.Diagnostics;

namespace UWUVCI.Mac.Services;

/// <summary>Well known folders and files of the app.</summary>
public static class AppPaths
{
    private static string Home => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    /// <summary>Folder for settings and downloaded bases.</summary>
    public static string DataDir => Path.Combine(Home, "Library", "Application Support", "UWUVCI");

    /// <summary>The settings JSON (mode 600).</summary>
    public static string SettingsFile => Path.Combine(DataDir, "settings.json");

    /// <summary>Where downloaded and decrypted bases are kept.</summary>
    public static string BasesDir => Path.Combine(DataDir, "bases");

    /// <summary>The diagnostic log.</summary>
    public static string LogFile => Path.Combine(Home, "Library", "Logs", "UWUVCI", "uwuvci.log");

    /// <summary>Default output folder.</summary>
    public static string DefaultOutputDir => Path.Combine(Home, "Documents", "UWUVCI");
}

/// <summary>Opens Finder on a result.</summary>
public static class Finder
{
    /// <summary>Selects <paramref name="path"/> in Finder (<c>open -R</c>). Arguments are passed as a list, never a shell string.</summary>
    public static void Reveal(string path)
    {
        var full = InputGuard.Absolute(path);
        if (full is null || !(Directory.Exists(full) || File.Exists(full))) return;
        var psi = new ProcessStartInfo("/usr/bin/open") { UseShellExecute = false };
        psi.ArgumentList.Add("-R");
        psi.ArgumentList.Add(full);
        using var p = Process.Start(psi);
    }
}
