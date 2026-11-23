using SkiaSharp;
using UWUVCI.Core.Injection;

namespace UWUVCI.Core.Tests.Injection;

public class TgaImageTests
{
    private static byte[] SolidPng(int w, int h, SKColor top, SKColor bottom)
    {
        using var bmp = new SKBitmap(w, h);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                bmp.SetPixel(x, y, y < h / 2 ? top : bottom);
        using var img = SKImage.FromBitmap(bmp);
        return img.Encode(SKEncodedImageFormat.Png, 100).ToArray();
    }

    [Fact]
    public void Convert_24Bit_HasReferenceHeaderAndFooter()
    {
        var tga = TgaImage.FromImage(SolidPng(8, 4, SKColors.Red, SKColors.Blue), 16, 8, 24);
        Assert.Equal(new byte[] { 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 0, 8, 0, 24, 0 }, tga[..18]);
        Assert.Equal(18 + 16 * 8 * 3 + 26, tga.Length);
        Assert.EndsWith("TRUEVISION-XFILE.\0", System.Text.Encoding.ASCII.GetString(tga[^18..]));
    }

    [Fact]
    public void Convert_32Bit_UsesAlphaDescriptor()
    {
        var tga = TgaImage.FromImage(SolidPng(2, 2, SKColors.Red, SKColors.Blue), 4, 4, 32);
        Assert.Equal(32, tga[16]);
        Assert.Equal(8, tga[17]);
        Assert.Equal(18 + 4 * 4 * 4 + 26, tga.Length);
    }

    [Fact]
    public void Convert_StoresRowsBottomUpInBgrOrder()
    {
        var tga = TgaImage.FromImage(SolidPng(2, 2, SKColors.Red, SKColors.Blue), 2, 2, 24);
        // First stored pixel is bottom-left: blue -> B=255,G=0,R=0
        Assert.Equal(new byte[] { 255, 0, 0 }, tga[18..21]);
        // Last stored pixel is top-right: red -> B=0,G=0,R=255
        int last = 18 + 3 * 3;
        Assert.Equal(new byte[] { 0, 0, 255 }, tga[last..(last + 3)]);
    }

    [Fact]
    public void Convert_InvalidDepth_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TgaImage.FromImage(SolidPng(2, 2, SKColors.Red, SKColors.Blue), 2, 2, 16));

    [Fact]
    public void Convert_NotAnImage_Throws() =>
        Assert.Throws<InvalidDataException>(() => TgaImage.FromImage(new byte[] { 1, 2, 3 }, 2, 2, 24));
}
