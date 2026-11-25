"""lldb command: dump the guest memory of a running Cemu (the readable Wii U ranges) to a file.
Usage: lldb -p <cemu pid> --batch -o "command script import lldump.py" -o "lldump <host base> <out file>" -o detach
The host base is on the "Init Wii U memory space (base: ...)" line of Cemu's log. The file is a list of
[guest address u32][length u32][bytes] records, 64 KB each. Needs the macOS Developer Tools approval."""
import lldb
def dump(debugger, command, result, internal_dict):
    base=int(command.split()[0],16); out=command.split()[1]
    proc=debugger.GetSelectedTarget().GetProcess(); err=lldb.SBError()
    ranges=[(0x02000000,0x50000000),(0xE0000000,0xE4000000),(0xF4000000,0xFA000000)]
    tot=0
    with open(out,'wb') as f:
        for lo,hi in ranges:
            a=lo
            while a<hi:
                n=0x10000
                data=proc.ReadMemory(base+a,n,err)
                if err.Success() and data:
                    f.write(a.to_bytes(4,'big')+len(data).to_bytes(4,'big')+data); tot+=len(data)
                a+=n
    result.write('done %d MB\n'%(tot>>20))
def __lldb_init_module(debugger, internal_dict):
    debugger.HandleCommand('command script add -f lldump.dump lldump')
