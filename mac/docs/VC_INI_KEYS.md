# Keys the Wii U N64 Virtual Console engine reads from the game ini

Extracted from the strings of the engine (VESSEL.rpx) loaded in Cemu, so these are names the engine knows, not documented behavior. Meanings marked "?" are guesses from the name. Values are numbers or 0/1 unless stated.

* `[RomOption]`: BackupType, BackupSize, EEROMInitValue, RamSize, RomType, Rumble, TPak, MemPak, NoCntPak, PlayerNum, UseTimer, RetraceByVsync, TrueBoot, BootPCChange, TLBMissEnable, TicksPerFrame, TimeIntDelay, AIUseTimer, AIIntPerFrame, RSPMultiCore, RSPMultiCoreInt, RSPAMultiCoreWait, FrameTickHack, Hack0, Hack1, Hack2, Hack3, Hack32, BufFull, BufHalf, BufHave, rxBufFull, RomHack, SpecialInst, FGISPDFURL, PDFURL.
* `[RSPG]` (graphics task): RIntAfterGTask, GTaskDelay, RDPDelay, RDPInt, Skip, WaitOnlyFirst, WaitDelay, SIDelay, RSPGDCFlush.
* `[Render]`: FilterHack, ClipTop, ClipBottom, ClipLeft, ClipRight, ZClip, CopyDepthBuffer, CopyColorBuffer, CopyMiddleBuffer, CopyColorAfterTask, CopyAlphaForceOne, DoubleFillCheck, InitPerspectiveMode, ConstValue0..2, useViewportZScale, NeedTileSizeCheck, TileSizeCheckSpecial, PolygonOffset, PreparseTMEMBlock, NeedPreParse, TLUTCheck, CheckTlutValid, bCutClip, ClearVertexBuf, FlushMemEachTask, ForceFilterPoint, ForceRectFilterPoint, CalculateLOD, FirstFrameAt, TexEdgeAlpha, UseColorDither, DepthCompareLess, FogVertexAlpha, bForce720P.
* Other sections: `[Sound]`, `[Input]`, `[Idle]`, `[BreakBlockInst]` (Address, Inst, JmpPC, Type, Count), `[Cheat]` (Addr, Value, Bytes), `[Cmp]`, `[Cheat]`.

The engine contains a real RSP emulator and display list parsers for F3DEX1, S2DEX1 and S2DEX2 (strings such as `Unhandled F3DEX1 command`, `undefined microcode`, `ParseGBI [F3DEX1]`). Those messages never appeared in the Cemu log for Banjo-Tooie.

## What was tried on Banjo-Tooie (Cemu, F-Zero X US base)

* Same picture as with no extra key: WaitOnlyFirst, WaitDelay, SIDelay, TimeIntDelay, TLBMissEnable, BootPCChange, FrameTickHack, AIIntPerFrame, NoCntPak, RSPAMultiCoreWait (value 1).
* `RIntAfterGTask`, `GTaskDelay`, `RDPDelay`, `RDPInt` and `Skip` (value 1) change the behavior: the loading puzzle piece appears, but Cemu then presents about 0 frames per second and the picture does not progress in 5 minutes. These keys likely take other values than 1; the right values are not known.
