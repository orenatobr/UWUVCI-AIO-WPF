import re,struct,sys,glob
sys.path.insert(0,'.')
import ppcasm,ondisk
CAVE=int(sys.argv[1],16)
A='/Users/renato.pereira/Workspace/uwuvci/repo/mac/tools/graphicpack-bt-shadows/patch_btshadows.asm'
rpx=glob.glob('/Users/renato.pereira/Workspace/uwuvci/work/bases/fzerox-us/code/*.rpx')[0]
disk=ondisk.code(rpx)[0x2000020]
mem=open('/Users/renato.pereira/Workspace/uwuvci/work/dumps/text.bin','rb').read()
dw=lambda a: struct.unpack('>I',disk[a-0x2000020:a-0x2000020+4])[0]
mw=lambda a: struct.unpack('>I',mem[a-0x2000000:a-0x2000000+4])[0]
raw=[];sites=[]
for l in open(A):
    s=l.split(';')[0].strip()
    m=re.match(r'(0x[0-9A-Fa-f]+)\s*=\s*bla (\w+)',s)
    if m: sites.append((int(m.group(1),16),m.group(2))); continue
    if not s or s.startswith('[') or s.startswith('.') or s.startswith('moduleM'): continue
    raw.append(s)
# call-through transformation: the hook runs BEFORE the original (relocated) call and returns to it.
out=[];i=0;first_ret=True
for s in raw:
    if s in('ba 0x0E01684','ba 0x0E00DC8'): out.append('blr'); continue
    out.append(s)
# handled path of hookv: the 'lwz r0, 0x144(r1) / mtlr r0' just before the first 'blr' after 'bla 0xE01148' second call must skip the original call (LR+4)
idx=[k for k,s in enumerate(out) if s=='bla 0x0E01148'][-1]
assert out[idx+1]=='lwz r0, 0x144(r1)' and out[idx+2]=='mtlr r0'
out[idx+1:idx+3]=['lwz r12, 0x144(r1)','addi r12, r12, 4','mtlr r12']
# Keep the original draw when the strip's texture already has data (the engine's frame buffer read-back worked): the
# ellipse is only a stand-in for an empty (all zero) texture. r9 = texture, r10 = pitch, r18 = width, r19 = rows.
k=[i for i,s in enumerate(out) if s=='lwz r10, 0x124(r28)'][0]
assert out[k+1]=='li r29, 0'
out[k+1:k+1]=['li r29, 0','zy:','mullw r6, r29, r10','add r6, r9, r6','li r30, 0','zx:','lbzx r7, r6, r30','cmpwi r7, 0','bne vrel',
 'addi r30, r30, 1','cmpw r30, r18','blt zx','addi r29, r29, 1','cmpw r29, r19','blt zy']
body=len(out)
for n,(a,h) in enumerate(sites):
    out.append('t%d:'%n); out.append('.word 0x%08x'%dw(a-4)); out.append('b '+h)
labs={}
code=ppcasm.asm(out,CAVE,labs)
cave_end=CAVE+4*len(code)
assert all(dw(a)==mw(a) for a in range(CAVE,cave_end,4)), 'cave has relocated words'
def bl(frm,to): return (18<<26|(to-frm)&0x3fffffc|1)&0xffffffff
rows=[(a-4,dw(a-4),bl(a-4,labs['t%d'%n])) for n,(a,h) in enumerate(sites)]
cs=['namespace UWUVCI.Core.Injection;','','// Generated from tools/graphicpack-bt-shadows/patch_btshadows.asm by tools/ppc-port/gen.py; do not edit by hand.',
'public static class BanjoShadowEllipseCode','{',f'    public const uint CaveAddress = 0x{CAVE:08X};','','    /// <summary>The new code: the hooks plus one small stub per call site (the instruction moved from before the call, then a jump to the hook).</summary>','    public static readonly uint[] Code =','    [']
for k in range(0,len(code),6): cs.append('        '+', '.join('0x%08X'%w for w in code[k:k+6])+',')
cs+=['    ];','','    /// <summary>Per call site: the address of the instruction before the call, its word in the base engine file, and the bl to its stub.</summary>','    public static readonly (uint Address, uint Original, uint Patched)[] Sites =','    [']
for a,o,n in rows: cs.append(f'        (0x{a:08X}, 0x{o:08X}, 0x{n:08X}),')
cs+=['    ];','}','']
open('BanjoShadowEllipseCode.cs','w').write('\n'.join(cs))
print(len(code)*4,'bytes, ends',hex(cave_end),'sites',len(rows))
