# Banjo-Tooie (US) shadows for Cemu

In Cemu the Virtual Console engine draws the character shadows of Banjo-Tooie as dark translucent rectangles. This
graphic pack draws them as translucent ellipses. It is a Cemu workaround only: it patches the running engine in
Cemu and changes no file of the title, so nothing changes on a real Wii U.

## Use
1. Copy this folder into Cemu's `graphicPacks` folder.
2. In `rules.txt`, set `titleIds` to the title id of your Banjo-Tooie title (Cemu prints it in the window title).
3. In `patch_btshadows.asm`, set `moduleMatches` to the checksum Cemu prints in `log.txt` for the title
   (`Loaded module 'vessel' with checksum 0x...`). It differs between engines that were patched and engines that were not.
4. Enable the pack in Cemu's graphic pack list and start the title.

If the log does not show `Applying patch group 'BTShadows'`, the checksum or the assembler is wrong.

## What it does and does not do
* Shadows are translucent ellipses with a hard edge; the original ones have a soft edge. The ellipse height is
  estimated from the previous shadow of similar width.
* The pause menu background is not fixed by this pack.
* Checked in Cemu 2.6 with the US ROM only.

See `mac/docs/COMPATIBILITY.md` for the cause.
