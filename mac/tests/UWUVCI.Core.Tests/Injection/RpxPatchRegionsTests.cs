using System.Buffers.Binary;
using UWUVCI.Core.Injection;

namespace UWUVCI.Core.Tests.Injection;

public class RpxPatchRegionsTests
{
    private const uint Li = 0x39600000, Nop = 0x60000000;
    private const int Word = 700, CaveWord = 1500;                       // 700 % 7 == 0, so it holds Li

    private static (byte[] Rpx, uint Address, uint Cave) Sample() =>
        (SyntheticRpx.Build(SyntheticRpx.Code()), SyntheticRpx.CodeAddress + Word * 4, SyntheticRpx.CodeAddress + CaveWord * 4);

    [Fact]
    public void WritesABlockAndARedirectInOneEdit()
    {
        var (rpx, addr, cave) = Sample();
        uint[] block = [0x4E800020, 0x11111111, 0x22222222];
        RpxPatcher.PatchRegions(rpx, [new(cave, null, block), new(addr, [Li], [Nop])]);
        var (code, crc, _) = SyntheticRpx.Read(rpx);
        for (int k = 0; k < block.Length; k++)
            Assert.Equal(block[k], BinaryPrimitives.ReadUInt32BigEndian(code.AsSpan(CaveWord * 4 + k * 4)));
        Assert.Equal(Nop, BinaryPrimitives.ReadUInt32BigEndian(code.AsSpan(Word * 4)));
        Assert.Equal(SyntheticRpx.Crc32(code), crc);
    }

    [Fact]
    public void WritesNothingWhenOneExpectedWordDiffers()
    {
        var (rpx, addr, cave) = Sample();
        var copy = rpx.ToArray();
        var ex = Assert.Throws<InvalidDataException>(() =>
            RpxPatcher.PatchRegions(rpx, [new(cave, null, [1, 2]), new(addr, [0x12345678], [Nop])]));
        Assert.Contains("0x12345678", ex.Message);
        Assert.Equal(copy, rpx);
    }

    [Fact]
    public void RefusesARegionThatRunsPastTheSection()
    {
        var (rpx, _, _) = Sample();
        var size = (uint)SyntheticRpx.Code().Length;
        Assert.Throws<InvalidDataException>(() =>
            RpxPatcher.PatchRegions(rpx, [new(SyntheticRpx.CodeAddress + size - 4, null, [1, 2])]));
    }
}
