using UWUVCI.Core.Injection;

namespace UWUVCI.Core.Tests.Injection;

public class N64FrameLayoutPatcherTests
{
    private static uint Float(byte[] a, int o) => (uint)a[o] << 24 | (uint)a[o + 1] << 16 | (uint)a[o + 2] << 8 | a[o + 3];

    [Fact]
    public void Widescreen_StretchesTheFramePaneToSixteenByNine()
    {
        var arc = FrameLayoutArc.Build();
        N64FrameLayoutPatcher.Apply(arc, widescreen: true, removeDarkFilter: false);
        Assert.Equal(0x44F00000u, Float(arc, FrameLayoutArc.FrameWidthOffset)); // 1920.0
        Assert.Equal(1, arc[FrameLayoutArc.MaskFlagOffset]);                    // dark filter kept
    }

    [Fact]
    public void WithoutWidescreen_TheFramePaneStaysFourByThree()
    {
        var arc = FrameLayoutArc.Build();
        N64FrameLayoutPatcher.Apply(arc, widescreen: false, removeDarkFilter: true);
        Assert.Equal(0x44B40000u, Float(arc, FrameLayoutArc.FrameWidthOffset)); // 1440.0
        Assert.Equal(0, arc[FrameLayoutArc.MaskFlagOffset]);                    // dark filter hidden
    }

    [Fact]
    public void NothingRequested_LeavesTheArchiveUntouched()
    {
        var arc = FrameLayoutArc.Build(); var copy = (byte[])arc.Clone();
        N64FrameLayoutPatcher.Apply(arc, false, false);
        Assert.Equal(copy, arc);
    }

    [Fact]
    public void UnsupportedArchive_IsRejected()
    {
        Assert.Throws<InvalidDataException>(() => N64FrameLayoutPatcher.Apply(new byte[0x80], true, false));
    }

    private static float Single(byte[] a, int o) => BitConverter.UInt32BitsToSingle(Float(a, o));

    [Fact]
    public void Crop_ShrinksTheFrameTextureRectToTheDrawnPartOfThePicture()
    {
        var arc = FrameLayoutArc.Build(0x88);
        N64FrameLayoutPatcher.Apply(arc, false, false, new FrameCrop(0.05f, 0.1f));
        int uv = FrameLayoutArc.FrameUvOffset;
        Assert.Equal(0f, Single(arc, uv));                 // TL s
        Assert.Equal(0f, Single(arc, uv + 4));             // TL t
        Assert.Equal(0.95f, Single(arc, uv + 8), 5);       // TR s
        Assert.Equal(0f, Single(arc, uv + 12));            // TR t
        Assert.Equal(0f, Single(arc, uv + 16));            // BL s
        Assert.Equal(0.9f, Single(arc, uv + 20), 5);       // BL t
        Assert.Equal(0.95f, Single(arc, uv + 24), 5);      // BR s
        Assert.Equal(0.9f, Single(arc, uv + 28), 5);       // BR t
    }

    [Fact]
    public void CropOnly_DoesNotTouchTheFrameSizeOrTheDarkFilter()
    {
        var arc = FrameLayoutArc.Build(0x88);
        var width = Float(arc, FrameLayoutArc.FrameWidthOffset); var mask = arc[FrameLayoutArc.MaskFlagOffset];
        N64FrameLayoutPatcher.Apply(arc, false, false, new FrameCrop(0.05f, 0.05f));
        Assert.Equal(width, Float(arc, FrameLayoutArc.FrameWidthOffset));
        Assert.Equal(mask, arc[FrameLayoutArc.MaskFlagOffset]);
    }

    [Fact]
    public void CropWithWidescreen_DoesBoth()
    {
        var arc = FrameLayoutArc.Build(0x88);
        N64FrameLayoutPatcher.Apply(arc, true, false, new FrameCrop(0.05f, 0.05f));
        Assert.Equal(0x44F00000u, Float(arc, FrameLayoutArc.FrameWidthOffset));
        Assert.Equal(0.95f, Single(arc, FrameLayoutArc.FrameUvOffset + 24), 5);
    }

    [Fact]
    public void Crop_OnAPaneWithoutTextureCoordinates_IsRejected()
    {
        var arc = FrameLayoutArc.Build(0x60);   // too short to hold the four coordinates
        Assert.Throws<InvalidDataException>(() => N64FrameLayoutPatcher.Apply(arc, false, false, new FrameCrop(0.05f, 0.05f)));
    }

    [Theory]
    [InlineData(-0.1f, 0f)]
    [InlineData(0f, 0.5f)]
    [InlineData(0.6f, 0f)]
    public void Crop_OutsideZeroToHalf_IsRejected(float right, float bottom)
    {
        var arc = FrameLayoutArc.Build(0x88);
        Assert.Throws<ArgumentOutOfRangeException>(() => N64FrameLayoutPatcher.Apply(arc, false, false, new FrameCrop(right, bottom)));
    }
}
