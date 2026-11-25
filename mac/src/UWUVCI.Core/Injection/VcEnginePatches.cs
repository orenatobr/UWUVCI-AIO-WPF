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
}
