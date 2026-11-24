"""Cemu GDB stub client (raw, robust): halt a running title, list threads, read regs and guest memory."""
import socket,time,re
class G:
    def __init__(s,port=1337):
        s.s=socket.create_connection(('127.0.0.1',port),timeout=30); s.b=b''
    def snd(s,d): s.s.sendall(f'${d}#{sum(d.encode())&255:02x}'.encode())
    def rcv(s,t=30):
        s.s.settimeout(t)
        while True:
            m=re.search(rb'\$([^#]*)#..',s.b)
            if m:
                s.b=s.b[m.end():]; s.s.sendall(b'+'); return m.group(1).decode(errors='replace')
            s.b+=s.s.recv(1<<20)
    def cmd(s,d): s.snd(d); return s.rcv()
    def halt(s): s.s.sendall(b'\x03'); return s.rcv(60)
    def mem(s,a,n):
        o=b''
        while n>0:
            k=min(n,0x800); r=s.cmd(f'm{a:x},{k:x}')
            if (len(r)==3 and r.startswith('E')) or not r: raise IOError(f'{a:x} {r}')
            o+=bytes.fromhex(r); a+=k; n-=k
        return o
    def regs(s):
        r=s.cmd('g'); w=[int(r[i:i+8],16) for i in range(0,len(r)-7,8)]; return w
