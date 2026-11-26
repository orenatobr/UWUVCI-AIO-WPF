using System.Buffers.Binary;
using UWUVCI.Core.Injection;

namespace UWUVCI.Core.Tests.Injection;

public class RpxPatcherTests
{
    private const uint Li = 0x39600000, Nop = 0x60000000;

    private static (byte[] Rpx, int Word, uint Address) Sample()
    {
        var code = SyntheticRpx.Code();
        int word = 700;                       // 700 % 7 == 0, so it holds Li
        return (SyntheticRpx.Build(code), word, SyntheticRpx.CodeAddress + (uint)(word * 4));
    }

    [Fact]
    public void ReplacesTheWordAndKeepsTheRestOfTheCode()
    {
        var (rpx, word, addr) = Sample();
        var before = SyntheticRpx.Read(rpx).Code;
        RpxPatcher.PatchWord(rpx, addr, Li, Nop);
        var after = SyntheticRpx.Read(rpx).Code;
        Assert.Equal(Nop, BinaryPrimitives.ReadUInt32BigEndian(after.AsSpan(word * 4)));
        before[(word * 4)..(word * 4 + 4)].CopyTo(new byte[4], 0);
        for (int i = 0; i < before.Length; i++)
            if (i < word * 4 || i >= word * 4 + 4) Assert.Equal(before[i], after[i]);
    }

    [Fact]
    public void UpdatesTheCrcOfTheSection()
    {
        var (rpx, _, addr) = Sample();
        RpxPatcher.PatchWord(rpx, addr, Li, Nop);
        var (code, crc, _) = SyntheticRpx.Read(rpx);
        Assert.Equal(SyntheticRpx.Crc32(code), crc);
    }

    [Fact]
    public void WritesTheRealSizeOfTheNewStreamAndNoPadding()
    {
        var (rpx, _, addr) = Sample();
        var originalSize = SyntheticRpx.Read(rpx).SectionSize;
        RpxPatcher.PatchWord(rpx, addr, Li, Nop);
        var (code, _, size) = SyntheticRpx.Read(rpx);       // would throw on a trailing garbage stream if the size were wrong
        Assert.True(size <= originalSize);
        Assert.Equal(SyntheticRpx.Code().Length, code.Length);
    }

    [Fact]
    public void RefusesAWordThatIsNotWhatTheCallerExpects()
    {
        var (rpx, _, addr) = Sample();
        var copy = rpx.ToArray();
        var ex = Assert.Throws<InvalidDataException>(() => RpxPatcher.PatchWord(rpx, addr, 0x12345678, Nop));
        Assert.Contains("0x12345678", ex.Message);
        Assert.Equal(copy, rpx);                             // nothing was written
    }

    [Fact]
    public void RefusesAnAddressOutsideTheCode()
    {
        var (rpx, _, _) = Sample();
        Assert.Throws<InvalidDataException>(() => RpxPatcher.PatchWord(rpx, 0x30000000, Li, Nop));
    }

    [Fact]
    public void RefusesAFileThatIsNotAnElf() =>
        Assert.Throws<InvalidDataException>(() => RpxPatcher.PatchWord(new byte[256], SyntheticRpx.CodeAddress, Li, Nop));
}

public class RpxPatcherPatternTests
{
    // li r11,0x4000 / cmpwi r12,0x800 / stb (any registers) / beq +8 / li r11,0 / ori r10,r11,0x8000
    private static readonly uint[] Pattern = [0x39604000, 0x2C0C0800, 0x98000001, 0x41820008, 0x39600000, 0x616A8000];
    private static readonly uint[] Mask = [0xFFFFFFFF, 0xFFFFFFFF, 0xFC00FFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF];

    private static byte[] CodeWith(int copies, uint stb = 0x98FA0001)
    {
        var code = SyntheticRpx.Code();
        for (int c = 0; c < copies; c++)
            for (int i = 0; i < Pattern.Length; i++)
                BinaryPrimitives.WriteUInt32BigEndian(code.AsSpan((100 + c * 50 + i) * 4), i == 2 ? stb : Pattern[i]);
        return code;
    }

    [Fact]
    public void PatchesTheWordAtTheGivenIndexOfTheMatch()
    {
        var rpx = SyntheticRpx.Build(CodeWith(1));
        RpxPatcher.PatchMatching(rpx, Pattern, Mask, 4, 0x60000000);
        var code = SyntheticRpx.Read(rpx).Code;
        Assert.Equal(0x60000000u, BinaryPrimitives.ReadUInt32BigEndian(code.AsSpan((100 + 4) * 4)));
        Assert.Equal(0x39604000u, BinaryPrimitives.ReadUInt32BigEndian(code.AsSpan(100 * 4)));
        Assert.Equal(SyntheticRpx.Crc32(code), SyntheticRpx.Read(rpx).Crc);
    }

    [Fact]
    public void IgnoresTheRegistersOfTheMaskedWord()
    {
        var rpx = SyntheticRpx.Build(CodeWith(1, stb: 0x98C90001));   // stb r6,1(r9)
        RpxPatcher.PatchMatching(rpx, Pattern, Mask, 4, 0x60000000);
        Assert.Equal(0x60000000u, BinaryPrimitives.ReadUInt32BigEndian(SyntheticRpx.Read(rpx).Code.AsSpan((100 + 4) * 4)));
    }

    [Fact]
    public void RefusesWhenTheSequenceIsNotThere()
    {
        var rpx = SyntheticRpx.Build(SyntheticRpx.Code());
        var ex = Assert.Throws<InvalidDataException>(() => RpxPatcher.PatchMatching(rpx, Pattern, Mask, 4, 0x60000000));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void RefusesWhenTheSequenceIsThereMoreThanOnce()
    {
        var rpx = SyntheticRpx.Build(CodeWith(2));
        var copy = rpx.ToArray();
        var ex = Assert.Throws<InvalidDataException>(() => RpxPatcher.PatchMatching(rpx, Pattern, Mask, 4, 0x60000000));
        Assert.Contains("more than once", ex.Message);
        Assert.Equal(copy, rpx);
    }

    [Fact]
    public void ASecondPatchMayUseTheSpaceTheFirstOneFreed()
    {
        // A patch that compresses better shrinks the section's recorded size. A later patch that makes the stream
        // larger again must still fit in the space the file really has up to the next section.
        var code = new byte[4096 * 4];
        var rng = new Random(11);
        for (int i = 0; i < 4096; i++)
            BinaryPrimitives.WriteUInt32BigEndian(code.AsSpan(i * 4), i % 16 == 0 ? (uint)rng.Next() : 0x60000000u);   // mostly nops
        var rpx = SyntheticRpx.Build(code);
        int index = 16;                                          // one of the random words
        uint original = BinaryPrimitives.ReadUInt32BigEndian(code.AsSpan(index * 4));
        uint address = SyntheticRpx.CodeAddress + (uint)(index * 4);

        RpxPatcher.PatchWord(rpx, address, original, 0x60000000);   // compresses better
        RpxPatcher.PatchWord(rpx, address, 0x60000000, original);   // back to the larger stream

        Assert.Equal(code, SyntheticRpx.Read(rpx).Code);
    }
}
