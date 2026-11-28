using UWUVCI.Core.Injection;

namespace UWUVCI.Core.Tests.Injection;

public class BanjoShadowEllipseCodeTests
{
    [Fact]
    public void EveryCallSiteBecomesABlIntoTheNewCode()
    {
        uint start = BanjoShadowEllipseCode.CaveAddress, end = start + (uint)BanjoShadowEllipseCode.Code.Length * 4;
        Assert.Equal(34, BanjoShadowEllipseCode.Sites.Length);
        foreach (var (address, original, patched) in BanjoShadowEllipseCode.Sites)
        {
            Assert.Equal(0x48000001u, patched & 0xFC000003);               // bl, relative
            int off = (int)(patched & 0x03FFFFFC); if ((off & 0x02000000) != 0) off -= 0x04000000;
            uint target = (uint)(address + off);
            Assert.InRange(target, start, end - 4);
            Assert.NotEqual(original, patched);
            Assert.NotEqual(0x48000000u, original & 0xFC000000);             // the moved instruction is not a branch
        }
    }

    [Fact]
    public void TheEngineIsRefusedWhenItIsNotTheOneThePatchWasMadeFor() =>
        Assert.Throws<InvalidDataException>(() => VcEnginePatches.ShadowEllipses(new byte[256]));

    [Fact]
    public void TheKeepEngineDrawBranchIsABne()
    {
        var word = BanjoShadowEllipseCode.Code[BanjoShadowEllipseCode.KeepEngineDrawBranchIndex];
        Assert.Equal(0x40820000u, word & 0xFFFF0003);        // bc 4,2 (bne), relative, no link
    }
}
