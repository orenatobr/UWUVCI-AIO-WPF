using System.Buffers.Binary;
using System.IO.Compression;

namespace UWUVCI.Core.Tests.Injection;

/// <summary>A minimal RPX: ELF header, one compressed code section at 0x02000020 and the CRC table.</summary>
internal static class SyntheticRpx
{
    public const uint CodeAddress = 0x02000020;
    public const int HeaderSize = 0x34, SectionHeaderSize = 40, SectionCount = 3;

    public static byte[] Code(int words = 2048)
    {
        var rng = new Random(7);
        var code = new byte[words * 4];
        for (int i = 0; i < words; i++)
            BinaryPrimitives.WriteUInt32BigEndian(code.AsSpan(i * 4), i % 7 == 0 ? 0x39600000u : (uint)rng.Next());
        return code;
    }

    public static uint Crc32(ReadOnlySpan<byte> data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (var b in data)
        {
            crc ^= b;
            for (int k = 0; k < 8; k++) crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
        }
        return ~crc;
    }

    /// <summary>Compressed with the fastest level, so the patcher's stronger levels have room, as with the real file.</summary>
    public static byte[] Build(byte[] code)
    {
        using var ms = new MemoryStream();
        using (var z = new ZLibStream(ms, CompressionLevel.Fastest, leaveOpen: true))
            z.Write(code);
        var blob = new byte[4 + ms.Length];
        BinaryPrimitives.WriteUInt32BigEndian(blob, (uint)code.Length);
        ms.ToArray().CopyTo(blob, 4);

        int textOff = 0x100, crcOff = textOff + ((blob.Length + 0x3F) & ~0x3F);
        var file = new byte[crcOff + 4 * SectionCount];
        file[0] = 0x7F; file[1] = (byte)'E'; file[2] = (byte)'L'; file[3] = (byte)'F';
        BinaryPrimitives.WriteUInt32BigEndian(file.AsSpan(0x20), 0x40);                       // e_shoff
        BinaryPrimitives.WriteUInt16BigEndian(file.AsSpan(0x2E), SectionHeaderSize);          // e_shentsize
        BinaryPrimitives.WriteUInt16BigEndian(file.AsSpan(0x30), SectionCount);               // e_shnum

        void Section(int i, uint type, uint flags, uint addr, int off, int size)
        {
            var h = file.AsSpan(0x40 + i * SectionHeaderSize, SectionHeaderSize);
            BinaryPrimitives.WriteUInt32BigEndian(h[4..], type);
            BinaryPrimitives.WriteUInt32BigEndian(h[8..], flags);
            BinaryPrimitives.WriteUInt32BigEndian(h[12..], addr);
            BinaryPrimitives.WriteUInt32BigEndian(h[16..], (uint)off);
            BinaryPrimitives.WriteUInt32BigEndian(h[20..], (uint)size);
        }
        Section(1, 1, 0x08000006, CodeAddress, textOff, blob.Length);
        Section(2, 0x80000003, 0, 0, crcOff, 4 * SectionCount);
        blob.CopyTo(file, textOff);
        BinaryPrimitives.WriteUInt32BigEndian(file.AsSpan(crcOff + 4), Crc32(code));
        return file;
    }

    /// <summary>Reads the code section back: (code bytes, stored CRC, section size in its header).</summary>
    public static (byte[] Code, uint Crc, int SectionSize) Read(byte[] file)
    {
        var h = file.AsSpan(0x40 + SectionHeaderSize);
        int off = (int)BinaryPrimitives.ReadUInt32BigEndian(h[16..]), size = (int)BinaryPrimitives.ReadUInt32BigEndian(h[20..]);
        uint usize = BinaryPrimitives.ReadUInt32BigEndian(file.AsSpan(off));
        using var z = new ZLibStream(new MemoryStream(file, off + 4, size - 4), CompressionMode.Decompress);
        var code = new byte[usize]; z.ReadExactly(code);
        var crcOff = (int)BinaryPrimitives.ReadUInt32BigEndian(file.AsSpan(0x40 + 2 * SectionHeaderSize + 16));
        return (code, BinaryPrimitives.ReadUInt32BigEndian(file.AsSpan(crcOff + 4)), size);
    }
}
