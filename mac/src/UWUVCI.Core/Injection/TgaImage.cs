using SkiaSharp;

namespace UWUVCI.Core.Injection;

/// <summary>Converts PNG, JPEG, BMP or similar images into the uncompressed TGA files the Wii U menu expects.</summary>
public static class TgaImage
{
    private static readonly byte[] Footer =
        [0, 0, 0, 0, 0, 0, 0, 0, .. "TRUEVISION-XFILE."u8, 0];

    /// <summary>Decodes, resizes and encodes an image as TGA.</summary>
    /// <param name="source">Encoded image bytes.</param>
    /// <param name="width">Target width.</param>
    /// <param name="height">Target height.</param>
    /// <param name="bitsPerPixel">24 (opaque) or 32 (with alpha).</param>
    /// <exception cref="InvalidDataException">The bytes are not a supported image.</exception>
    public static byte[] FromImage(byte[] source, int width, int height, int bitsPerPixel)
    {
        if (bitsPerPixel is not (24 or 32))
            throw new ArgumentOutOfRangeException(nameof(bitsPerPixel), "Only 24 and 32 bit TGA are supported.");

        using var decoded = Decode(source);
        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
        using var scaled = decoded.Resize(info, new SKSamplingOptions(SKCubicResampler.Mitchell))
            ?? throw new InvalidDataException("Could not resize the image.");

        int bytes = bitsPerPixel / 8;
        var tga = new byte[18 + width * height * bytes + Footer.Length];
        tga[2] = 2;
        tga[12] = (byte)width; tga[13] = (byte)(width >> 8);
        tga[14] = (byte)height; tga[15] = (byte)(height >> 8);
        tga[16] = (byte)bitsPerPixel;
        tga[17] = bitsPerPixel == 32 ? (byte)8 : (byte)0; // bottom-left origin like the stock UWUVCI files

        var pixels = scaled.Bytes; // BGRA
        int o = 18;
        for (int y = height - 1; y >= 0; y--)
            for (int x = 0; x < width; x++)
            {
                int i = (y * width + x) * 4;
                tga[o++] = pixels[i]; tga[o++] = pixels[i + 1]; tga[o++] = pixels[i + 2];
                if (bytes == 4) tga[o++] = pixels[i + 3];
            }
        Footer.CopyTo(tga, o);
        return tga;
    }

    private static SKBitmap Decode(byte[] source)
    {
        // SkiaSharp throws (null codec) for unknown formats instead of returning null.
        try
        {
            return SKBitmap.Decode(source) ?? throw new InvalidDataException("Unsupported image file.");
        }
        catch (ArgumentNullException)
        {
            throw new InvalidDataException("Unsupported image file.");
        }
    }
}
