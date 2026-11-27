; Banjo-Tooie (US) character shadows for Cemu: draws each shadow as a translucent ellipse instead of a dark rectangle.
;
; Why: the engine draws a shadow as a stack of strips (112 to 224 wide) that read the picture as an I8 texture. In Cemu
; that texture arrives empty (the engine's GX2CopySurface read-back converts RGBA8 to RGBA4/R8, which Cemu skips when the
; bits per pixel differ), so every strip is a flat dark quad. This pack (1) records the unit 0 R8 texture at bind time,
; (2) for each shadow strip fills it with the matching slice of one ellipse (strips of the same shadow are found by
; their contiguous rows), (3) draws the strip with a constant 50% blend and restores the blend control afterwards.
; Cemu only. It does not change the title or any file; the draw is unchanged on a real Wii U.
;
; Use: see README.md. moduleMatches is the checksum Cemu prints for the title's VESSEL.rpx ("Loaded module 'vessel'
; with checksum 0x..."); it is different for an engine that RpxPatcher has patched. The addresses are those of the
; F-Zero X base engine (the one the app uses for Banjo-Tooie).
[BTShadows]
moduleMatches = 0x209a50c6
.origin = codecave

dummy:
blr
blr

hookbind:
cmpwi r4, 0
bne bpass
lis r11, 0x1018
lwz r5, 0x14(r3)
cmpwi r5, 1
beq bfmt
li r6, 0
stw r6, 0x130(r11)
b bpass
bfmt:
lwz r6, 0x24(r3)
stw r6, 0x120(r11)
lwz r6, 0x3c(r3)
stw r6, 0x124(r11)
lwz r6, 4(r3)
stw r6, 0x128(r11)
lwz r6, 8(r3)
stw r6, 0x12c(r11)
lwz r6, 0x20(r3)
stw r6, 0x134(r11)
li r6, 1
stw r6, 0x130(r11)
bpass:
ba 0x0E01684

hookv:
lwz r12, 0xa4(r30)
lis r11, 0x50
ori r11, r11, 0x4241
cmpw r12, r11
bne vpass
mflr r0
stwu r1, -0x140(r1)
stw r0, 0x144(r1)
stmw r3, 0x10(r1)
lis r28, 0x1018
li r31, 0
lwz r12, 0xc(r25)
mulli r11, r12, 0x18
lwz r10, 0(r25)
add r10, r10, r11
lfs f0, 0(r10)
fctiwz f4, f0
addi r9, r1, 0x100
stfiwx f4, 0, r9
lwz r14, 0x100(r1)
lfs f0, 4(r10)
fctiwz f4, f0
addi r9, r1, 0x100
stfiwx f4, 0, r9
lwz r15, 0x100(r1)
lfs f0, 48(r10)
fctiwz f4, f0
addi r9, r1, 0x100
stfiwx f4, 0, r9
lwz r16, 0x100(r1)
lfs f0, 52(r10)
fctiwz f4, f0
addi r9, r1, 0x100
stfiwx f4, 0, r9
lwz r17, 0x100(r1)
subf r18, r14, r16
subf r19, r15, r17
cmpwi r18, 64
blt vrel
cmpwi r18, 224
bgt vrel
cmpwi r19, 1
blt vrel
lwz r9, 0x130(r28)
cmpwi r9, 0
beq vrel
lwz r9, 0x128(r28)
cmpw r9, r18
bne vrel
lwz r9, 0x12c(r28)
cmpw r9, r19
bne vrel
lwz r20, 0x100(r28)
lwz r21, 0x104(r28)
cmpw r15, r20
bne newgrp
cmpw r14, r21
bne newgrp
b samegrp
newgrp:
lwz r22, 0x108(r28)
subf r23, r22, r20
cmpwi r23, 8
blt nofin
stw r23, 0x114(r28)
lwz r24, 0x10c(r28)
stw r24, 0x118(r28)
nofin:
stw r15, 0x108(r28)
stw r18, 0x10c(r28)
lwz r23, 0x114(r28)
lwz r24, 0x118(r28)
subf r25, r18, r24
cmpwi r25, -24
blt guess
cmpwi r25, 24
bgt guess
cmpwi r23, 16
blt guess
mr r26, r23
b seth
guess:
mulli r26, r18, 3
li r25, 10
divw r26, r26, r25
seth:
stw r26, 0x110(r28)
samegrp:
stw r17, 0x100(r28)
stw r14, 0x104(r28)
lwz r22, 0x108(r28)
subf r27, r22, r15
lwz r26, 0x110(r28)
cmpwi r26, 1
blt vrel
mullw r11, r18, r18
mullw r12, r26, r26
lwz r9, 0x120(r28)
lwz r10, 0x124(r28)
li r29, 0
fyl:
add r5, r29, r27
slwi r5, r5, 1
addi r5, r5, 1
subf r5, r26, r5
mullw r5, r5, r5
slwi r5, r5, 8
divw r5, r5, r12
li r30, 0
mullw r6, r29, r10
add r6, r9, r6
fxl:
slwi r7, r30, 1
addi r7, r7, 1
subf r7, r18, r7
mullw r7, r7, r7
slwi r7, r7, 8
divw r7, r7, r11
add r7, r7, r5
li r8, 0
cmpwi r7, 256
bge fst
subfic r8, r7, 256
cmpwi r8, 255
ble fst
li r8, 255
fst:
mulli r8, r8, 3
srwi r8, r8, 8
subfic r8, r8, 255
stbx r8, r6, r30
addi r30, r30, 1
cmpw r30, r18
blt fxl
addi r29, r29, 1
cmpw r29, r19
blt fyl
lwz r4, 0x120(r28)
lwz r5, 0x134(r28)
li r3, 0x42
bla 0x0E00F10
li r31, 1
vrel:
cmpwi r31, 1
bne vnorm
lis r9, 16128
stw r9, 0x100(r1)
lfs f1, 0x100(r1)
fmr f2, f1
fmr f3, f1
fmr f4, f1
bla 0xE01144
li r3, 0
li r4, 0xD
li r5, 0xE
li r6, 0
li r7, 0
li r8, 4
li r9, 5
li r10, 0
bla 0x0E01148
lmw r3, 0x10(r1)
bla 0x0E00DC8
li r3, 0
li r4, 4
li r5, 5
li r6, 0
li r7, 0
li r8, 4
li r9, 5
li r10, 0
bla 0x0E01148
lwz r0, 0x144(r1)
mtlr r0
addi r1, r1, 0x140
blr
vnorm:
lmw r3, 0x10(r1)
lwz r0, 0x144(r1)
mtlr r0
addi r1, r1, 0x140
vpass:
ba 0x0E00DC8

0x0202C09C = bla hookv
0x020229A0 = bla hookbind
0x02022A94 = bla hookbind
0x02022BC0 = bla hookbind
0x02029BE0 = bla hookbind
0x0202CBA8 = bla hookbind
0x0202CC38 = bla hookbind
0x0202CCF4 = bla hookbind
0x0202CDC8 = bla hookbind
0x0202CF20 = bla hookbind
0x0202D058 = bla hookbind
0x0202D0D8 = bla hookbind
0x0202D15C = bla hookbind
0x0202D1E4 = bla hookbind
0x0202D298 = bla hookbind
0x0202D324 = bla hookbind
0x0202D3B0 = bla hookbind
0x020313D4 = bla hookbind
0x02038248 = bla hookbind
0x02038A5C = bla hookbind
0x02038A74 = bla hookbind
0x02038A8C = bla hookbind
0x02038AAC = bla hookbind
0x02038AC4 = bla hookbind
0x02039310 = bla hookbind
0x020397D8 = bla hookbind
0x020397F4 = bla hookbind
0x0203A060 = bla hookbind
0x020A3E24 = bla hookbind
0x022022C8 = bla hookbind
0x0228C4B0 = bla hookbind
0x0228C520 = bla hookbind
0x022B2798 = bla hookbind
0x022B283C = bla hookbind
