import struct,zlib,glob
def sections(path):
    d=open(path,'rb').read()
    shoff=struct.unpack('>I',d[0x20:0x24])[0]; es=struct.unpack('>H',d[0x2e:0x30])[0]; n=struct.unpack('>H',d[0x30:0x32])[0]
    out=[]
    for i in range(n):
        sh=shoff+i*es
        name,typ,flags,addr,off,size=struct.unpack('>IIIIII',d[sh:sh+24])
        out.append((typ,flags,addr,off,size))
    return d,out
def code(path):
    d,secs=sections(path); res={}
    for typ,flags,addr,off,size in secs:
        if typ==1 and flags&0x08000000 and flags&4:  # progbits compressed exec
            u=struct.unpack('>I',d[off:off+4])[0]
            res[addr]=zlib.decompress(d[off+4:off+size])
            assert len(res[addr])==u
    return res
if __name__=='__main__':
    import sys
    for a,c in code(sys.argv[1]).items(): print(hex(a),hex(len(c)))
