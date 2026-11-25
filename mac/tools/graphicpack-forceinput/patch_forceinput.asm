; Presses START on pad 1 from inside the VC engine, so a run in Cemu can be driven without a keyboard.
; (Cemu on macOS does not read synthetic key events, so the buttons are put in the PIF data the game reads.)
;
; Use: copy this folder into Cemu's graphicPacks, set titleIds in rules.txt to the title under test and
; moduleMatches below to the checksum Cemu prints in log.txt ("Loaded module 'vessel' with checksum 0x...");
; it is different for an engine that RpxPatcher has patched. Enable the pack in Cemu's graphic pack list.
;
; Every read of the buttons flips the START bit against the value of the previous read (kept in the game's own
; buffer), so the game sees a press and a release on alternate frames. 0x10 in the first data byte is START.
[ForceStart]
moduleMatches = 0xb4bf2291
.origin = codecave

hookstart:
lbz r0, 4(r30)
andi. r0, r0, 0x10
xori r0, r0, 0x10
stb r0, 4(r29)
li r5, 0x40
blr

; the engine's PIF processor, at the end of the "read the buttons" command (was: li r5,0x40)
0x0206134C = bla hookstart
