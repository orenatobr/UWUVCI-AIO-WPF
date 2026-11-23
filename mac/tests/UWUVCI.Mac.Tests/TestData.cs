using UWUVCI.Mac.Services;

namespace UWUVCI.Mac.Tests;

/// <summary>Builds throwaway files and folders for tests.</summary>
internal sealed class TempDir : IDisposable
{
    public string Path { get; } = Directory.CreateTempSubdirectory("uwuvci-mac-tests-").FullName;

    public string File(string name, byte[] bytes)
    {
        var p = System.IO.Path.Combine(Path, name);
        System.IO.File.WriteAllBytes(p, bytes);
        return p;
    }

    public string Base(string name = "base")
    {
        var p = System.IO.Path.Combine(Path, name);
        foreach (var d in new[] { "code", "content", "meta" })
            Directory.CreateDirectory(System.IO.Path.Combine(p, d));
        return p;
    }

    public void Dispose() => Directory.Delete(Path, true);
}

internal static class TestData
{
    public const string ValidKey = "0123456789abcdef0123456789ABCDEF";

    /// <summary>A minimal big endian N64 image with the given header fields.</summary>
    public static byte[] Rom(uint crc1, uint crc2, string title = "TEST GAME", string code = "NTSE")
    {
        var b = new byte[0x1000];
        b[0] = 0x80; b[1] = 0x37; b[2] = 0x12; b[3] = 0x40;
        void U32(int o, uint v) { b[o] = (byte)(v >> 24); b[o + 1] = (byte)(v >> 16); b[o + 2] = (byte)(v >> 8); b[o + 3] = (byte)v; }
        U32(0x10, crc1);
        U32(0x14, crc2);
        System.Text.Encoding.ASCII.GetBytes(title).CopyTo(b, 0x20);
        System.Text.Encoding.ASCII.GetBytes(code).CopyTo(b, 0x3B);
        return b;
    }

    public static byte[] BanjoTooie() => Rom(0xC2E9AA9A, 0x475D70AA, "BANJO TOOIE", "NB7E");

    public static byte[] Png() => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0];
}
