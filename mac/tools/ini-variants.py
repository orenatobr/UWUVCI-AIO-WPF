"""Variants of the pak accessories: measure whether NO CONTROLLER (golden text) shows on the title screen, and keep frames."""
import os,re,shutil,subprocess,sys,time,glob
from PIL import Image
W=os.path.expanduser('~/Workspace/uwuvci/work'); CEMU=W+'/cemu-vc/Cemu.app/Contents/MacOS/Cemu'
src=W+'/vc-titles/ab-43/base'; INI='content/config/Ucfze0.242.ini'
variants=[('si1',['+SIDelay = 1']),('si10',['+SIDelay = 10']),('si100',['+SIDelay = 100']),('si1000',['+SIDelay = 1000']),('si0',['+SIDelay = 0'])]
def golden(path):
    im=Image.open(path).convert('RGB'); w,h=im.size; px=im.load(); n=0
    for y in range(int(h*0.72),int(h*0.92),3):
        for x in range(int(w*0.20),int(w*0.80),3):
            r,g,b=px[x,y]
            if r>170 and 110<g<190 and b<80: n+=1
    return n
for name,ops in variants:
    d=f'{W}/vc-runs/nc-{name}'; shutil.rmtree(d,ignore_errors=True); shutil.copytree(src,d)
    p_=d+'/'+INI; t=open(p_).read()
    for o in ops:
        if o[0]=='-': t=re.sub(r'^'+re.escape(o[1:])+r'\b.*\n','',t,flags=re.M)
        elif o[0]=='^': t=t.replace('ClipRight = 55\n','ClipRight = 55\n'+o[1:]+'\n',1)
        elif o[0]=='@': t=t.rstrip('\n')+'\n\n'+o[1:]+'\n'
        elif o[0]=='^': t=t.replace('ClipRight = 55\n','ClipRight = 55\n'+o[1:]+'\n',1)
        elif o[0]=='@': t=t.rstrip('\n')+'\n\n'+o[1:].replace('|','\n')+'\n'
        else: t=t.replace('[RomOption]\n','[RomOption]\n'+o[1:]+'\n',1)
    open(p_,'w').write(t)
    out=f'{W}/vc-runs/ncshots-{name}'; shutil.rmtree(out,ignore_errors=True); os.makedirs(out)
    pr=subprocess.Popen([CEMU,'-g',d+'/code/VESSEL.rpx'],env=dict(os.environ,DYLD_INSERT_LIBRARIES=W+'/cemu-vc/libbigstack.dylib'),stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
    time.sleep(28); res=[]
    for i in range(9):
        wid=subprocess.check_output([W+'/vc-runs/control5/.winid',str(pr.pid)]).decode().strip()
        f=f'{out}/s{i}.png'; subprocess.run(['screencapture','-x','-o','-l',wid,f]); res.append(golden(f)); time.sleep(6)
    pr.terminate(); time.sleep(2); shutil.rmtree(d,ignore_errors=True)
    print(f'{name:14s} golden-pixel count per frame (every 5 s from 33 s):',res,flush=True)
print('done',flush=True)
