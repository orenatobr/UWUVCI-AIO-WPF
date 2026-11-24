"""Boot a VC title in Cemu, press buttons on a schedule, and capture the Cemu window only (no shadow).
usage: vc-play.py <name> <title-dir> <boot-wait-s> <total-s> <capture-every-s> <keys comma list, cycled every 5 s>"""
import os,subprocess,sys,time
W=os.environ.get('UWUVCI_WORK',os.path.expanduser('~/Workspace/uwuvci/work'))  # folder with cemu-vc/ (Cemu copy, libbigstack.dylib, sendkey) and vc-runs/
CEMU=W+'/cemu-vc/Cemu.app/Contents/MacOS/Cemu'; DYLIB=W+'/cemu-vc/libbigstack.dylib'
WINID=W+'/vc-runs/control5/.winid'; SENDKEY=W+'/cemu-vc/sendkey'
name,tdir,boot,total,every,keys=sys.argv[1],sys.argv[2],int(sys.argv[3]),int(sys.argv[4]),int(sys.argv[5]),sys.argv[6].split(',')
out=f'{W}/vc-runs/play-{name}'; os.makedirs(out,exist_ok=True)
for f in os.listdir(out): os.remove(f'{out}/{f}')
rpx=[f for f in os.listdir(tdir+'/code') if f.endswith('.rpx')][0]
subprocess.run(['caffeinate','-u','-t','2'])
p=subprocess.Popen([CEMU,'-g',f'{tdir}/code/{rpx}'],env=dict(os.environ,DYLD_INSERT_LIBRARIES=DYLIB),stdout=open(out+'/cemu.log','w'),stderr=subprocess.STDOUT)
def front():
    subprocess.run(['osascript','-e',f'tell application "System Events" to set frontmost of (first process whose unix id is {p.pid}) to true'],capture_output=True)
def shot(tag):
    try:
        wid=subprocess.check_output([WINID,str(p.pid)]).decode().strip()
        subprocess.run(['screencapture','-x','-o','-l',wid,f'{out}/{tag}.png'])
        t=subprocess.run(['osascript','-e',f'tell application "System Events" to get name of window 1 of (first process whose unix id is {p.pid})'],capture_output=True,text=True).stdout.strip()
        print(tag,t[:95],flush=True)
    except Exception as e: print(tag,'shot failed',e,flush=True)
t0=time.time(); time.sleep(boot); front(); i=0; nextshot=0
while time.time()-t0<total and p.poll() is None:
    el=int(time.time()-t0)
    if el>=nextshot: shot(f't{el:04d}'); nextshot+=every
    k=keys[i%len(keys)]; i+=1
    if k!='-': subprocess.run([SENDKEY,str(p.pid),k,'200'])
    time.sleep(5)
shot('final'); print('exited early' if p.poll() is not None else 'ok'); p.terminate()
