using System.Buffers.Binary;
using System.IO.Compression;

namespace UWUVCI.Core.Injection;

/// <summary>
/// Changes one instruction word in the compressed code section of a Wii U RPX, without moving any section.
/// The section is inflated, edited and deflated again (the new stream must fit in the space the file has up to the
/// next section, so spare bytes stay unused), and its entry in the CRC table is updated.
/// </summary>
public static class RpxPatcher
{
    private const uint Deflated = 0x08000000, ProgBits = 1, RplCrcs = 0x80000003;
    private const int SectionHeaderSize = 40;

    /// <summary>Replaces the 32 bit word at <paramref name="address"/>, which must currently be <paramref name="expected"/>.</summary>
    /// <exception cref="InvalidDataException">Not an RPX, the address is not in a code section, the word is not what was expected, or the new stream does not fit.</exception>
    public static void PatchWord(byte[] rpx, uint address, uint expected, uint value) =>
        Edit(rpx, (code, baseAddr) =>
        {
            if (address < baseAddr || address >= baseAddr + code.Length) return -1;
            int p = (int)(address - baseAddr);
            uint found = BinaryPrimitives.ReadUInt32BigEndian(code.AsSpan(p));
            if (found != expected)
                throw new InvalidDataException($"The code at {address:x8} is 0x{found:x8}, not the expected 0x{expected:x8}; this is not the VC engine the patch was made for.");
            return p;
        }, value, $"Address {address:x8} is not inside a compressed code section.");

    /// <summary>
    /// Finds the one place where the words match <paramref name="pattern"/> under <paramref name="mask"/> (a 0 bit is ignored)
    /// (<paramref name="pattern"/> must already have its ignored bits cleared) and replaces the word at <paramref name="index"/> of the match, so the patch does not depend on the engine build's addresses.
    /// </summary>
    /// <exception cref="InvalidDataException">No match or more than one match, or any of the errors of <see cref="PatchWord"/>.</exception>
    public static void PatchMatching(byte[] rpx, uint[] pattern, uint[] mask, int index, uint value) =>
        Edit(rpx, (code, _) =>
        {
            int found = -1;
            for (int p = 0; p + pattern.Length * 4 <= code.Length; p += 4)
            {
                bool ok = true;
                for (int k = 0; k < pattern.Length && ok; k++)
                    ok = (BinaryPrimitives.ReadUInt32BigEndian(code.AsSpan(p + k * 4)) & mask[k]) == pattern[k];
                if (!ok) continue;
                if (found >= 0) throw new InvalidDataException("The instruction sequence to patch is in the code more than once.");
                found = p;
            }
            return found < 0 ? -1 : found + index * 4;
        }, value, "The instruction sequence to patch was not found in the code.");

    private static void Edit(byte[] rpx, Func<byte[], uint, int> locate, uint value, string notFound)
    {
        if (rpx.Length < 0x40 || rpx[0] != 0x7F || rpx[1] != 'E' || rpx[2] != 'L' || rpx[3] != 'F')
            throw new InvalidDataException("The file is not an RPX (no ELF header).");

        int shoff = (int)BinaryPrimitives.ReadUInt32BigEndian(rpx.AsSpan(0x20));
        int entSize = BinaryPrimitives.ReadUInt16BigEndian(rpx.AsSpan(0x2E));
        int count = BinaryPrimitives.ReadUInt16BigEndian(rpx.AsSpan(0x30));
        if (entSize != SectionHeaderSize || shoff + count * entSize > rpx.Length)
            throw new InvalidDataException("The RPX section table is not valid.");

        uint U32(int at) => BinaryPrimitives.ReadUInt32BigEndian(rpx.AsSpan(at));
        int? crcTable = null;
        for (int i = 0; i < count; i++)
            if (U32(shoff + i * entSize + 4) == RplCrcs) crcTable = (int)U32(shoff + i * entSize + 16);
        if (crcTable is not { } crcs) throw new InvalidDataException("The RPX has no CRC table.");

        for (int t = 0; t < count; t++)
        {
            int sh = shoff + t * entSize;
            uint flags = U32(sh + 8);
            if (U32(sh + 4) != ProgBits || (flags & Deflated) == 0) continue;
            int secOff = (int)U32(sh + 16), secSize = (int)U32(sh + 20);
            if (secOff + 4 > rpx.Length || secSize < 5) continue;
            uint usize = U32(secOff);
            var code = new byte[usize];
            using (var z = new ZLibStream(new MemoryStream(rpx, secOff + 4, secSize - 4), CompressionMode.Decompress))
                z.ReadExactly(code);

            int p = locate(code, U32(sh + 12));
            if (p < 0) continue;

            // The recorded size shrinks after every patch, so the limit is the room the file really has: up to the
            // start of the next section's data (or the end of the file).
            int capacity = rpx.Length - secOff;
            for (int s = 0; s < count; s++)
            {
                int other = (int)U32(shoff + s * entSize + 16);
                if (other > secOff && other - secOff < capacity) capacity = other - secOff;
            }
            capacity = Math.Max(capacity, secSize);
            BinaryPrimitives.WriteUInt32BigEndian(code.AsSpan(p), value);

            byte[]? best = null;
            foreach (var level in new[] { CompressionLevel.Optimal, CompressionLevel.SmallestSize })
            {
                using var ms = new MemoryStream();
                using (var z = new ZLibStream(ms, level, leaveOpen: true)) z.Write(code);
                if (4 + ms.Length <= capacity && (best is null || ms.Length < best.Length)) best = ms.ToArray();
            }
            if (best is null) throw new InvalidDataException("The patched code section does not fit in its original space.");

            var section = rpx.AsSpan(secOff, Math.Max(secSize, 4 + best.Length));
            section.Clear();
            BinaryPrimitives.WriteUInt32BigEndian(section, usize);
            best.CopyTo(section[4..]);
            BinaryPrimitives.WriteUInt32BigEndian(rpx.AsSpan(sh + 20), (uint)(4 + best.Length));   // real size, no padding inside the section
            BinaryPrimitives.WriteUInt32BigEndian(rpx.AsSpan(crcs + 4 * t), Crc32(code));
            return;
        }
        throw new InvalidDataException(notFound);
    }

    private static readonly uint[] Table = Enumerable.Range(0, 256).Select(n =>
    {
        uint c = (uint)n;
        for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? (c >> 1) ^ 0xEDB88320 : c >> 1;
        return c;
    }).ToArray();

    private static uint Crc32(ReadOnlySpan<byte> data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (var b in data) crc = Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
        return ~crc;
    }
}
