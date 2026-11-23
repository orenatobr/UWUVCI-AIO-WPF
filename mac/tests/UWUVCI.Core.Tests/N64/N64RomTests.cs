using UWUVCI.Core.N64;

namespace UWUVCI.Core.Tests.N64;

public class N64RomTests
{
    private static byte[] BuildZ64(int length = 0x1000)
    {
        var rom = new byte[length];
        rom[0] = 0x80; rom[1] = 0x37; rom[2] = 0x12; rom[3] = 0x40;
        // CRC1 at 0x10, CRC2 at 0x14 (big endian)
        new byte[] { 0xC2, 0xE9, 0xAA, 0x9A }.CopyTo(rom, 0x10);
        new byte[] { 0x47, 0x5D, 0x70, 0xAA }.CopyTo(rom, 0x14);
        System.Text.Encoding.ASCII.GetBytes("BANJO TOOIE").CopyTo(rom, 0x20);
        System.Text.Encoding.ASCII.GetBytes("NB7E").CopyTo(rom, 0x3B);
        for (int i = 0x40; i < length; i++) rom[i] = (byte)(i * 31);
        return rom;
    }

    private static byte[] ToV64(byte[] z64)
    {
        var v = (byte[])z64.Clone();
        for (int i = 0; i < v.Length; i += 2) (v[i], v[i + 1]) = (v[i + 1], v[i]);
        return v;
    }

    private static byte[] ToN64(byte[] z64)
    {
        var n = (byte[])z64.Clone();
        for (int i = 0; i < n.Length; i += 4)
        {
            (n[i], n[i + 3]) = (n[i + 3], n[i]);
            (n[i + 1], n[i + 2]) = (n[i + 2], n[i + 1]);
        }
        return n;
    }

    [Fact]
    public void Detect_Z64() => Assert.Equal(RomFormat.Z64, N64Rom.Detect(BuildZ64()));

    [Fact]
    public void Detect_V64() => Assert.Equal(RomFormat.V64, N64Rom.Detect(ToV64(BuildZ64())));

    [Fact]
    public void Detect_N64() => Assert.Equal(RomFormat.N64, N64Rom.Detect(ToN64(BuildZ64())));

    [Fact]
    public void Detect_Unknown_Throws() =>
        Assert.Throws<InvalidDataException>(() => N64Rom.Detect(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }));

    [Fact]
    public void Detect_TooShort_Throws() =>
        Assert.Throws<InvalidDataException>(() => N64Rom.Detect(new byte[3]));

    [Fact]
    public void ToZ64_FromV64_RestoresOriginal()
    {
        var z = BuildZ64();
        Assert.Equal(z, N64Rom.ToZ64(ToV64(z)));
    }

    [Fact]
    public void ToZ64_FromN64_RestoresOriginal()
    {
        var z = BuildZ64();
        Assert.Equal(z, N64Rom.ToZ64(ToN64(z)));
    }

    [Fact]
    public void ToZ64_FromZ64_IsIdentityCopy()
    {
        var z = BuildZ64();
        var result = N64Rom.ToZ64(z);
        Assert.Equal(z, result);
        Assert.NotSame(z, result);
    }

    [Fact]
    public void ToZ64_OddLengthV64_Throws() =>
        Assert.Throws<InvalidDataException>(() =>
            N64Rom.ToZ64(new byte[] { 0x37, 0x80, 0x40, 0x12, 0x00 }));

    [Fact]
    public void ReadHeader_ParsesCrcTitleAndCode()
    {
        var h = N64Rom.ReadHeader(BuildZ64());
        Assert.Equal(0xC2E9AA9Au, h.Crc1);
        Assert.Equal(0x475D70AAu, h.Crc2);
        Assert.Equal("BANJO TOOIE", h.Title);
        Assert.Equal("NB7E", h.GameCode);
        Assert.Equal('E', h.RegionCode);
    }

    [Fact]
    public void ReadHeader_AcceptsV64Input()
    {
        var h = N64Rom.ReadHeader(ToV64(BuildZ64()));
        Assert.Equal(0xC2E9AA9Au, h.Crc1);
    }

    [Fact]
    public void ConvertFile_WritesZ64(){
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            var src = Path.Combine(dir.FullName, "in.v64");
            var dst = Path.Combine(dir.FullName, "out.z64");
            var z = BuildZ64();
            File.WriteAllBytes(src, ToV64(z));
            N64Rom.ConvertFile(src, dst);
            Assert.Equal(z, File.ReadAllBytes(dst));
        }
        finally { dir.Delete(true); }
    }
}
