import os,struct,subprocess,time
W=os.path.expanduser('~/Workspace/uwuvci/work'); CEMU=W+'/cemu-vc/Cemu.app/Contents/MacOS/Cemu'
env=dict(os.environ,DYLD_INSERT_LIBRARIES=W+'/cemu-vc/libbigstack.dylib')
pr=subprocess.Popen([CEMU,'-g',W+'/vc-runs/p1-base/code/VESSEL.rpx'],env=env,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
def arr(path):
    d=open(path,'rb').read(); o=0x83fa0
    return d[o:o+16].hex(' ')
def dump(tag):
    base=[l for l in open(os.path.expanduser('~/Library/Application Support/Cemu/log.txt'),errors='replace') if 'Init Wii U memory space' in l][-1].split('base: ')[1].split(')')[0]
    out=f'{W}/vc-runs/el-{tag}.bin'
    subprocess.run(['lldb','-p',str(pr.pid),'--batch','-o',f'command script import {W}/dumpfull.py','-o',f'dumpfull {base} {out}','-o','detach'],capture_output=True,text=True,timeout=150)
    return out
time.sleep(60); a=dump('t60'); print('t~60s',arr(a),flush=True)
time.sleep(25); b=dump('t140'); print('late ',arr(b),flush=True)
pr.terminate()
