import os,re,shutil,struct,subprocess,sys,time
W=os.path.expanduser('~/Workspace/uwuvci/work'); CEMU=W+'/cemu-vc/Cemu.app/Contents/MacOS/Cemu'
env=dict(os.environ,DYLD_INSERT_LIBRARIES=W+'/cemu-vc/libbigstack.dylib')
def ini_path(t): 
    d=t+'/content/config'; return d+'/'+[f for f in os.listdir(d) if f.endswith('.ini') and not f.startswith('blank')][0] if False else None
def run(name,src,edit):
    dst=f'{W}/vc-runs/p1-{name}'; shutil.rmtree(dst,ignore_errors=True); shutil.copytree(src,dst)
    cfg=dst+'/content/config'; ini=[f for f in os.listdir(cfg) if f.endswith('.ini')]
    # the title's own ini is the one whose rom id matches the rom file name
    rom=os.listdir(dst+'/content/rom')[0]; p=cfg+'/'+rom+'.ini'
    t=open(p).read(); t=edit(t); open(p,'w').write(t)
    rpx=dst+'/code/VESSEL.rpx'
    pr=subprocess.Popen([CEMU,'-g',rpx],env=env,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
    time.sleep(68)
    base=[l for l in open(os.path.expanduser('~/Library/Application Support/Cemu/log.txt'),errors='replace') if 'Init Wii U memory space' in l][-1].split('base: ')[1].split(')')[0]
    out=f'{W}/vc-runs/p1-{name}.bin'
    subprocess.run(['lldb','-p',str(pr.pid),'--batch','-o',f'command script import {W}/dumpfull.py','-o',f'dumpfull {base} {out}','-o','detach'],capture_output=True,text=True,timeout=150)
    pr.terminate(); time.sleep(3)
    d=open(out,'rb').read(); res=[]
    for i in range(0,len(d)-16,4):
        e=[struct.unpack('>HBB',d[i+4*k:i+4*k+4]) for k in range(4)]
        if e[0][0]==5 and all(x[0]==5 and x[1] in (0,1,2,3) and x[2] in (0,8,4) for x in e[:2]):
            res.append((hex(0x80000000+i),d[i:i+16].hex(' ')))
    print(name,res[:3],flush=True)
src=W+'/vc-titles/ab-43/base'
V={'base':lambda t:t,
   'alwayshave':lambda t:re.sub(r'StickModify\s*=\s*2','StickModify = 2\nAlwaysHave = 1',t),
   'norumble':lambda t:re.sub(r'Rumble\s*=\s*1','Rumble = 0',t),
   'delrumble':lambda t:re.sub(r'Rumble\s*=\s*1\n','',t),
   'tpak':lambda t:re.sub(r'Rumble\s*=\s*1','Rumble = 0\nTPak = 1',t)}
for n in sys.argv[1:]: run(n,src,V[n])
