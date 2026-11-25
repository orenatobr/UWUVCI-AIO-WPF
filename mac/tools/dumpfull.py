"""lldb command: read the whole 8 MB RDRAM of the emulated N64 once. usage: dumpfull <host base> <out>"""
import lldb,struct
def dump(debugger, command, result, internal_dict):
    a=command.split(); base=int(a[0],16); out=a[1]
    proc=debugger.GetSelectedTarget().GetProcess(); err=lldb.SBError()
    gp=struct.unpack('>I',proc.ReadMemory(base+0x10054490,4,err))[0]; rdram=base+gp
    blk=b''
    for off in range(0,0x800000,0x10000): blk+=proc.ReadMemory(rdram+off,0x10000,err)
    open(out,'wb').write(blk); result.write('OK %d\n'%len(blk))
def __lldb_init_module(debugger, internal_dict):
    debugger.HandleCommand('command script add -f dumpfull.dump dumpfull')
