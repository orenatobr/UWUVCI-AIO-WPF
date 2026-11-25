[PifLog]
moduleMatches = 0xC0C21308
.origin = codecave

fmt2:
.string "PIFOUT %08x %08x %08x %08x %08x %08x %08x\n"

hook2:
mflr r0
stwu r1, -0x120(r1)
stw r0, 0x124(r1)
stmw r3, 0x10(r1)
lis r12, 0x1330
addi r12, r12, 0x2ac
lwz r4, 0(r12)
lwz r5, 4(r12)
lwz r6, 8(r12)
lwz r7, 0xc(r12)
lwz r8, 0x10(r12)
lwz r9, 0x14(r12)
lwz r10, 0x18(r12)
lis r3, fmt2@ha
addi r3, r3, fmt2@l
crxor 6, 6, 6
bla 0x0E05DB8
lmw r3, 0x10(r1)
lwz r0, 0x124(r1)
mtlr r0
addi r1, r1, 0x120
lmw r26, 0x20(r1)
blr

0x020616B0 = bla hook2
