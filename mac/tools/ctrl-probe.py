"""Hold candidate keys one at a time and dump the N64 RDRAM during each hold, plus a screenshot, to find the pad structure and the stick key ids."""
import os,subprocess,sys,time
W=os.path.expanduser('~/Workspace/uwuvci/work'); CEMU=W+'/cemu-vc/Cemu.app/Contents/MacOS/Cemu'
S='/private/tmp/claude-501/-Users-renato-pereira-Workspace-uwuvci/716fa725-9bbb-429f-afe0-caf408e2d784/scratchpad'
tdir=sys.argv[1]; keys=sys.argv[2].split(',')
out=W+'/vc-runs/ctrl'; os.makedirs(out,exist_ok=True)
for f in os.listdir(out): os.remove(f'{out}/{f}')
p=subprocess.Popen([CEMU,'-g',tdir+'/code/VESSEL.rpx'],env=dict(os.environ,DYLD_INSERT_LIBRARIES=W+'/cemu-vc/libbigstack.dylib'),stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
time.sleep(65)
base=[l for l in open(os.path.expanduser('~/Library/Application Support/Cemu/log.txt'),errors='replace') if 'Init Wii U memory space' in l][-1].split('base: ')[1].split(')')[0]
print('base',base,flush=True)
def wid(): return subprocess.check_output([W+'/vc-runs/control5/.winid',str(p.pid)]).decode().strip()
def shot(tag): subprocess.run(['screencapture','-x','-o','-l',wid(),f'{out}/{tag}.png'])
def dump(tag):
    r=subprocess.run(['lldb','-p',str(p.pid),'--batch','-o',f'command script import {S}/dumpfull.py','-o',f'dumpfull {base} {out}/{tag}.bin','-o','detach'],capture_output=True,text=True,timeout=150)
    return 'OK ' in r.stdout
subprocess.run(['osascript','-e',f'tell application "System Events" to set frontmost of (first process whose unix id is {p.pid}) to true']); time.sleep(1)
print('idle dump',dump('idle'),flush=True); shot('idle')
time.sleep(3)
for k in keys:
    h=subprocess.Popen([W+'/cemu-vc/sendkey',str(p.pid),k,'75000'])
    time.sleep(2)
    ok=dump(f'key_{k}'); print('key',k,'dump',ok,flush=True)
    shot(f'key_{k}'); h.wait(); time.sleep(3)
p.terminate(); print('done',flush=True)
