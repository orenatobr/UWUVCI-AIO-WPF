"""Run one title N times alone and report when the title screen (red TOOIE letters) first appears."""
import os,subprocess,sys,time
from PIL import Image
W=os.path.expanduser('~/Workspace/uwuvci/work'); CEMU=W+'/cemu-vc/Cemu.app/Contents/MacOS/Cemu'
tdir,name,n,dur=sys.argv[1],sys.argv[2],int(sys.argv[3]),int(sys.argv[4])
def red(path):
    im=Image.open(path).convert('RGB'); w,h=im.size; px=im.load(); c=0
    for y in range(int(h*0.25),int(h*0.75),4):
        for x in range(int(w*0.2),int(w*0.8),4):
            r,g,b=px[x,y]
            if r>170 and g<60 and b<60: c+=1
    return c
res=[]
for run in range(n):
    out=f'{W}/vc-runs/rep-{name}-{run}'; os.makedirs(out,exist_ok=True)
    for f in os.listdir(out): os.remove(f'{out}/{f}')
    p=subprocess.Popen([CEMU,'-g',tdir+'/code/VESSEL.rpx'],env=dict(os.environ,DYLD_INSERT_LIBRARIES=W+'/cemu-vc/libbigstack.dylib'),stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
    t0=time.time(); title=None; fronted=False
    while time.time()-t0<dur:
        time.sleep(4)
        if os.environ.get('FRONT') and not fronted and time.time()-t0>15:
            subprocess.run(['osascript','-e',f'tell application "System Events" to set frontmost of (first process whose unix id is {p.pid}) to true']); fronted=True
        try:
            wid=subprocess.check_output([W+'/vc-runs/control5/.winid',str(p.pid)]).decode().strip()
            f=f'{out}/t{int(time.time()-t0):04d}.png'; subprocess.run(['screencapture','-x','-o','-l',wid,f])
            pass
        except Exception: pass
    p.terminate(); time.sleep(3)
    res.append(title); print(f'run {run}: title screen first seen at {title}s (None = not within {dur}s)',flush=True)
print('summary',res,flush=True); print('done',flush=True)
