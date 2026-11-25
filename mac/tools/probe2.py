"""Boot a title; every <period> s take a window capture and a one-shot lldb memory read, so frames and memory can be cross-read.
usage: probe2.py <title dir> <name> <first at s> <period s> <count>"""
import os,subprocess,sys,time
W=os.path.expanduser('~/Workspace/uwuvci/work'); CEMU=W+'/cemu-vc/Cemu.app/Contents/MacOS/Cemu'
S='/private/tmp/claude-501/-Users-renato-pereira-Workspace-uwuvci/716fa725-9bbb-429f-afe0-caf408e2d784/scratchpad'
tdir,name,first,period,count=sys.argv[1],sys.argv[2],int(sys.argv[3]),int(sys.argv[4]),int(sys.argv[5])
out=f'{W}/vc-runs/probe2-{name}'; os.makedirs(out,exist_ok=True)
for f in os.listdir(out): os.remove(f'{out}/{f}')
p=subprocess.Popen([CEMU,'-g',tdir+'/code/VESSEL.rpx'],env=dict(os.environ,DYLD_INSERT_LIBRARIES=W+'/cemu-vc/libbigstack.dylib'),stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
t0=time.time()
time.sleep(5)
base=[l for l in open(os.path.expanduser('~/Library/Application Support/Cemu/log.txt'),errors='replace') if 'Init Wii U memory space' in l][-1].split('base: ')[1].split(')')[0]
def wid(): return subprocess.check_output([W+'/vc-runs/control5/.winid',str(p.pid)]).decode().strip()
time.sleep(max(0,first-(time.time()-t0)))
for k in range(count):
    t=int(time.time()-t0)
    try: subprocess.run(['screencapture','-x','-o','-l',wid(),f'{out}/s{k:02d}_{t:04d}.png'])
    except Exception as e: print('shot failed',e)
    r=subprocess.run(['lldb','-p',str(p.pid),'--batch','-o',f'command script import {S}/dump1.py','-o',f'dump1 {base} {out}/m{k:02d}_{t:04d}.bin','-o','detach'],capture_output=True,text=True,timeout=120)
    print(k,t,'OK' if 'OK ' in r.stdout else 'FAILED',flush=True)
    time.sleep(max(0,period-(time.time()-t0-t)))
p.terminate(); print('done',flush=True)
