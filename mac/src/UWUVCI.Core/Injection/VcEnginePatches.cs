namespace UWUVCI.Core.Injection;

/// <summary>Changes to the Wii U N64 Virtual Console engine (<c>code/*.rpx</c>) that a game needs.</summary>
public static class VcEnginePatches
{
    // How the engine answers the EEPROM size query on the PIF: li r11,0x4000 / cmpwi r12,0x800 / stb (any) / beq +8 /
    // li r11,0 / ori r10,r11,0x8000. It adds the 16 Kbit flag (0x4000) only when its backup size variable is exactly 0x800,
    // and with the ini keys of the engine that variable never is, so every game is told it has a 4 Kbit EEPROM.
    private static readonly uint[] EepromAnswer = [0x39604000, 0x2C0C0800, 0x98000001, 0x41820008, 0x39600000, 0x616A8000];
    private static readonly uint[] EepromAnswerMask = [0xFFFFFFFF, 0xFFFFFFFF, 0xFC00FFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF];
    private const int LiR11Zero = 4;
    private const uint Nop = 0x60000000;

    // Where the engine stores the mask of textures a draw samples, which the pixel shader reads as uf_remappedPS[0].z (bit 0
    // TEXEL0, bit 1 TEXEL1; below 1 it skips the first texture, below 2 the second). The mask comes from the combiner color
    // inputs it scans, and it is 0 for many draws that need a texture (Banjo-Tooie HUD digits and dust puffs came out
    // missing). The two places that copy it into the uniform are: lwz r10,0(r26) / stw r10,0xc8(r26) and
    // lwz r10,0(r26) / stfs f1,0xe8(r26) / stw r10,0xc8(r26). Both are changed to li r10,3.
    private static readonly uint[] MaskStoreA = [0x815A0000, 0x915A00C8];
    private static readonly uint[] MaskStoreB = [0x815A0000, 0xD03A00E8, 0x915A00C8];
    private const uint LiR10Three = 0x39400003;

    /// <summary>
    /// Returns the engine with the EEPROM size answer fixed at 16 Kbit. Banjo-Tooie asks <c>osEepromProbe</c> and, when it
    /// is not told 16 Kbit, stores an error that makes it show NO CONTROLLER and ignore the pad.
    /// </summary>
    /// <exception cref="InvalidDataException">The engine is not an RPX or does not contain the instruction sequence.</exception>
    public static byte[] AnswerEeprom16K(byte[] engine)
    {
        var copy = (byte[])engine.Clone();
        try { RpxPatcher.PatchMatching(copy, EepromAnswer, EepromAnswerMask, LiR11Zero, Nop); }
        catch (InvalidDataException e)
        {
            throw new InvalidDataException("This base's VC engine cannot be patched for the 16 Kbit EEPROM answer (" + e.Message + "). Choose another base.", e);
        }
        return copy;
    }

    /// <summary>
    /// Returns the engine with every draw sampling both textures (mask 3), so draws whose texture the engine decided not to
    /// sample still get it. Verified in Cemu with Banjo-Tooie: the counter digits of the pause screen and the dust puffs
    /// under Banjo appear.
    /// </summary>
    /// <exception cref="InvalidDataException">The engine is not an RPX or does not contain the instruction sequences.</exception>
    public static byte[] SampleBothTextures(byte[] engine)
    {
        var copy = (byte[])engine.Clone();
        try
        {
            RpxPatcher.PatchMatching(copy, MaskStoreA, Exact(MaskStoreA), 0, LiR10Three);
            RpxPatcher.PatchMatching(copy, MaskStoreB, Exact(MaskStoreB), 0, LiR10Three);
        }
        catch (InvalidDataException e)
        {
            throw new InvalidDataException("This base's VC engine cannot be patched to sample the draw texture (" + e.Message + "). Choose another base.", e);
        }
        return copy;
    }

    /// <summary>
    /// Returns the engine with Banjo-Tooie's character shadows drawn as translucent ellipses. The engine draws a shadow as a
    /// stack of strips that sample a texture built by reading the frame buffer back (GX2CopySurface RGBA8 to R8); when that
    /// texture is empty each strip is a flat dark quad. The new code (the Cemu pack in tools/graphicpack-bt-shadows, assembled
    /// by tools/ppc-port) is written over the start of a function nothing calls and the 34 calls that bind a texture or draw
    /// a rectangle are redirected to it. With <paramref name="always"/> the ellipse is drawn even where the texture has data. It is made for the F-Zero X (US) base engine and refuses any other.
    /// </summary>
    /// <exception cref="InvalidDataException">The engine is not that engine.</exception>
    public static byte[] ShadowEllipses(byte[] engine, bool always = false)
    {
        var copy = (byte[])engine.Clone();
        var code = (uint[])BanjoShadowEllipseCode.Code.Clone();
        // always: draw the ellipse even when the strip's texture has data (a canvas other than 640 x 480 makes the engine's frame buffer
        // read-back come out wrong on a console, so its texture holds data that is not the silhouette).
        if (always) code[BanjoShadowEllipseCode.KeepEngineDrawBranchIndex] = Nop;
        var regions = new List<RpxPatcher.Region> { new(BanjoShadowEllipseCode.CaveAddress, null, code) };
        foreach (var (address, original, patched) in BanjoShadowEllipseCode.Sites)
            regions.Add(new(address, [original], [patched]));
        try { RpxPatcher.PatchRegions(copy, regions); }
        catch (InvalidDataException e)
        {
            throw new InvalidDataException("This base's VC engine cannot take the shadow patch (" + e.Message + "). It needs the F-Zero X (US) base.", e);
        }
        return copy;
    }

    private static uint[] Exact(uint[] words) => words.Select(_ => 0xFFFFFFFFu).ToArray();
}
