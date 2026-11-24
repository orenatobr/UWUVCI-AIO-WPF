"""Sample where each guest thread of a running Cemu title is blocked: boot, then halt/read/continue N times."""
import sys,time,subprocess,os,collections
sys.path.insert(0,os.path.dirname(os.path.abspath(__file__)))
from gdb2 import G
W=os.path.expanduser('~/Workspace/uwuvci/work')
rpx,boot,n=sys.argv[1],int(sys.argv[2]),int(sys.argv[3])
p=subprocess.Popen([W+'/cemu-vc/Cemu.app/Contents/MacOS/Cemu','--enable-gdbstub','-g',rpx],env=dict(os.environ,DYLD_INSERT_LIBRARIES=W+'/cemu-vc/libbigstack.dylib'),stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
time.sleep(20); g=G(); g.snd('c'); time.sleep(boot)
hist=collections.Counter(); main=collections.Counter()
for i in range(n):
    g.halt()
    for t in g.cmd('qfThreadInfo').lstrip('m').split(','):
        g.cmd(f'Hg{t}'); pc=g.cmd('p40'); lr=g.cmd('p43')
        hist[(t,pc,lr)]+=1
    g.snd('c'); time.sleep(1.3)
for (t,pc,lr),c in sorted(hist.items()): print(t,'pc',pc,'lr',lr,'x',c)
p.terminate()
