using UWUVCI.Core.N64;

namespace UWUVCI.Core.Tests.N64;

public class VcIniRenderKeysTests
{
    [Fact]
    public void AddsARenderSectionWithABlankLineAfterEveryKeyButTheLast()
    {
        var ini = VcIni.WithRenderKeys("[RomOption]\nBackupType = 3\n", [("CanvasWidth", "864"), ("CanvasHeight", "648")]);
        Assert.EndsWith("[Render]\nCanvasWidth = 864\n\nCanvasHeight = 648\n", ini);
    }

    [Fact]
    public void PutsTheKeysInFrontOfAnExistingRenderSection()
    {
        var ini = VcIni.WithRenderKeys("[RomOption]\nA = 1\n\n[Render]\nClipRight = 33\n", [("CanvasWidth", "864")]);
        Assert.Contains("[Render]\nCanvasWidth = 864\n\nClipRight = 33\n", ini);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(ini, @"\[Render\]"));
    }

    [Fact]
    public void LeavesTheIniAloneWithNoKeys() =>
        Assert.Equal("[RomOption]\nA = 1\n", VcIni.WithRenderKeys("[RomOption]\nA = 1\n", []));
}
