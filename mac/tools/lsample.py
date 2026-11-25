"""lldb command: sample words of the emulated N64 RDRAM of a running Cemu several times per second.
Usage: lldb -p <pid> --batch -o "command script import lsample.py" -o "lsample <host base> <samples> <seconds between>" -o detach
Edit WORDS (N64 address -> name) for the title being studied. The addresses below are those of Banjo-Tooie (EU)."""
import lldb,time,struct
WORDS={'p3550':0x80083550,'p3554':0x80083554,'last3b1c':0x80083b1c,'b18':0x80083b18,'vid1':0x80083470+0xC,'sch1':0x80082800+0xC,'viC_rc':0x80040c40,'viN_rc':0x80040c10,'viC_fb':0x80040c44,'viN_fb':0x80040c14,'game_q1':0x80080e10+8,'tmo':0x800830c8}
def _wait_stopped(proc,limit=5.0):
    t=time.time()
    while proc.GetState()!=lldb.eStateStopped and time.time()-t<limit: time.sleep(0.002)
def sample(debugger, command, result, internal_dict):
    a=command.split(); base=int(a[0],16); n=int(a[1]); dt=float(a[2])
    debugger.SetAsync(True)
    proc=debugger.GetSelectedTarget().GetProcess(); err=lldb.SBError()
    _wait_stopped(proc)
    gp=struct.unpack('>I',proc.ReadMemory(base+0x10054490,4,err))[0]; rdram=base+gp
    result.write('rdram guest %x\n'%gp)
    rows=[]; t0=time.time()
    for k in range(n):
        row={}
        for name,addr in WORDS.items():
            d=proc.ReadMemory(rdram+(addr-0x80000000),4,err); row[name]=struct.unpack('>I',d)[0] if err.Success() else None
        rows.append((time.time()-t0,row))
        proc.Continue(); time.sleep(dt); proc.Stop(); _wait_stopped(proc)
    for t,r in rows: result.write('T%6.2f '%t+' '.join('%s=%s'%(k,('%x'%v if v is not None else 'NA')) for k,v in r.items())+'\n')
    proc.Continue()
def __lldb_init_module(debugger, internal_dict):
    debugger.HandleCommand('command script add -f lsample.sample lsample')
