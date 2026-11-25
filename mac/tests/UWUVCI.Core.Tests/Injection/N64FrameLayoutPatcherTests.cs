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
}
