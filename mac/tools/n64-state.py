"""Read the emulated N64 state of a Virtual Console title from a lldump.py file.
usage: n64-state.py <dump file>
Finds the RDRAM base in the engine global 0x10054490 (VESSEL.rpx of the Nintendo base used here), then lists the
libultra threads (found by their OSThread layout) with what each waits on, the event table and the OSTask structs.
Addresses 0x80040bd0 (empty wait list sentinel) and the layout offsets are libultra 2.0 as used by Banjo-Tooie."""
import struct,sys
raw=open(sys.argv[1],'rb').read(); idx={}; i=0
while i<len(raw):
    a=int.from_bytes(raw[i:i+4],'big'); n=int.from_bytes(raw[i+4:i+8],'big'); idx[a]=(i+8,n); i+=8+n
def g(addr,n):
    blk=addr&~0xFFFF; off,ln=idx[blk]; o=off+(addr-blk); return raw[o:o+n]
B=struct.unpack('>I',g(0x10054490,4))[0]; SIZE=struct.unpack('>I',g(0x100541a4,4))[0]
ram=bytearray(SIZE); blk=B&~0xFFFF
while blk<B+SIZE:
    if blk in idx:
        off,ln=idx[blk]; lo=max(blk,B); hi=min(blk+0x10000,B+SIZE); ram[lo-B:hi-B]=raw[off+(lo-blk):off+(hi-blk)]
    blk+=0x10000
u32=lambda a: struct.unpack('>I',ram[a-0x80000000:a-0x80000000+4])[0]
u16=lambda a: struct.unpack('>H',ram[a-0x80000000:a-0x80000000+2])[0]
u64=lambda a: struct.unpack('>Q',ram[a-0x80000000:a-0x80000000+8])[0]
print(f'RDRAM host base {B:#x} size {SIZE:#x}')
def isq(q): return 0x80000400<=q<0x807FFF00 and q%4==0 and u32(q+4)==0x80040bd0
for o in range(0x80000000,0x80000000+SIZE-0x130,8):
    st=u16(o+0x10)
    if st not in (1,2,4,8): continue
    nxt,pri,que,tln=u32(o),u32(o+4),u32(o+8),u32(o+12); tid=u32(o+0x14)
    if nxt==0 or nxt<0x80000000 or pri>255 or tid>=64 or u16(o+0x12)>7 or not (que==0 or 0x80000000<=que<0x80800000): continue
    print(f'thread {o:#x} id={tid} pri={pri} state={ {1:"STOPPED",2:"RUNNABLE",4:"RUNNING",8:"WAITING"}[st] } queue={que:#x} pc={u32(o+0x11c):#x} sp={u64(o+0xf0)&0xFFFFFFFF:#x}')
