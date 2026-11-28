# ppc-port

Turns the Cemu graphic pack `tools/graphicpack-bt-shadows/patch_btshadows.asm` into code inside the engine file, for the
`--shadow-ellipses` option of `vc-layout` and `vc-pack`.

* `ppcasm.py` is a small PowerPC assembler for the instructions the pack uses (no external assembler loads on Apple Silicon).
* `ondisk.py` reads the compressed code section of an RPX.
* `gen2.py <cave address in hex>` writes `src/UWUVCI.Core/Injection/BanjoShadowEllipseCode.cs`. It needs the F-Zero X (US) base
  engine and a memory dump of its code (`text.bin`, taken from Cemu) to compare the words the loader changes.

How it works: the calls to `GX2SetPixelTexture` and `GX2DrawEx` that the pack hooked are relocated by the loader, so patching
the call word itself would be overwritten. Each hook site instead gets the instruction before the call replaced with a `bl`
to a small stub (the moved instruction, then a jump to the hook). The hook runs, returns to the real call, and for a shadow
strip it returns past the call so the original draw is skipped. The new code lives at the start of a function that nothing
calls (checked in Cemu by filling candidates with an illegal instruction and playing to gameplay).
