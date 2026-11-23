using System.Text;

namespace UWUVCI.Core.N64;

/// <summary>Byte order of an N64 ROM image.</summary>
public enum RomFormat
{
    /// <summary>Big endian (native), magic 80 37 12 40.</summary>
    Z64,
    /// <summary>Byte swapped pairs, magic 37 80 40 12.</summary>
    V64,
    /// <summary>Little endian words, magic 40 12 37 80.</summary>
    N64,
}

/// <summary>Fields read from the 0x40 byte N64 ROM header.</summary>
/// <param name="Crc1">First checksum (offset 0x10).</param>
/// <param name="Crc2">Second checksum (offset 0x14).</param>
/// <param name="Title">Internal title (offset 0x20, 20 chars, trimmed).</param>
/// <param name="GameCode">Four char game code (offset 0x3B).</param>
public sealed record RomHeader(uint Crc1, uint Crc2, string Title, string GameCode)
{
    /// <summary>Region letter, last char of the game code (E, P, J, ...).</summary>
    public char RegionCode => GameCode.Length == 4 ? GameCode[3] : '?';
}

/// <summary>Byte order detection and conversion for N64 ROMs (replacement for N64Converter.exe).</summary>
public static class N64Rom
{
    private const int HeaderSize = 0x40;

    /// <summary>Detects the byte order from the 4 byte magic.</summary>
    /// <exception cref="InvalidDataException">Data is too short or has an unknown magic.</exception>
    public static RomFormat Detect(ReadOnlySpan<byte> rom)
    {
        if (rom.Length < 4)
            throw new InvalidDataException("File is too small to be an N64 ROM.");
        return (rom[0], rom[1], rom[2], rom[3]) switch
        {
            (0x80, 0x37, 0x12, 0x40) => RomFormat.Z64,
            (0x37, 0x80, 0x40, 0x12) => RomFormat.V64,
            (0x40, 0x12, 0x37, 0x80) => RomFormat.N64,
            _ => throw new InvalidDataException("Unrecognized N64 ROM format."),
        };
    }

    /// <summary>Returns a new big endian (.z64) copy of the ROM.</summary>
    /// <exception cref="InvalidDataException">Unknown format or length not a multiple of the word size.</exception>
    public static byte[] ToZ64(ReadOnlySpan<byte> rom)
    {
        var format = Detect(rom);
        var result = rom.ToArray();
        switch (format)
        {
            case RomFormat.V64:
                if (result.Length % 2 != 0)
                    throw new InvalidDataException("A .v64 ROM must have an even length.");
                for (int i = 0; i < result.Length; i += 2)
                    (result[i], result[i + 1]) = (result[i + 1], result[i]);
                break;
            case RomFormat.N64:
                if (result.Length % 4 != 0)
                    throw new InvalidDataException("An .n64 ROM must have a length multiple of 4.");
                for (int i = 0; i < result.Length; i += 4)
                {
                    (result[i], result[i + 3]) = (result[i + 3], result[i]);
                    (result[i + 1], result[i + 2]) = (result[i + 2], result[i + 1]);
                }
                break;
        }
        return result;
    }

    /// <summary>Parses the header of a ROM in any byte order.</summary>
    public static RomHeader ReadHeader(ReadOnlySpan<byte> rom)
    {
        var z = ToZ64(rom);
        if (z.Length < HeaderSize)
            throw new InvalidDataException("ROM is smaller than its header.");
        uint U32(int o) => (uint)(z[o] << 24 | z[o + 1] << 16 | z[o + 2] << 8 | z[o + 3]);
        string Ascii(int o, int n) => Encoding.ASCII.GetString(z, o, n).TrimEnd('\0', ' ');
        return new RomHeader(U32(0x10), U32(0x14), Ascii(0x20, 20), Encoding.ASCII.GetString(z, 0x3B, 4));
    }

    /// <summary>Reads <paramref name="input"/> and writes a .z64 to <paramref name="output"/>.</summary>
    public static void ConvertFile(string input, string output) =>
        File.WriteAllBytes(output, ToZ64(File.ReadAllBytes(input)));
}
