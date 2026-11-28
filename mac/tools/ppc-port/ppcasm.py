import re,struct
def reg(s): return int(re.fullmatch(r'[rf](\d+)',s.strip()).group(1))
def num(s): return int(s.strip(),0)
D={'lwz':32,'stw':36,'lfs':48,'addi':14,'mulli':7,'subfic':8,'stmw':47,'lmw':46,'stwu':37}
X={'add':266,'subf':40,'mullw':235,'divw':491}
BC={'beq':(12,2),'bne':(4,2),'blt':(12,0),'bge':(4,0),'bgt':(12,1),'ble':(4,1)}
def parse_mem(s):
    m=re.fullmatch(r'(-?\w+)\((r\d+)\)',s.strip()); return num(m.group(1)),reg(m.group(2))
def asm(lines,base,labels_out=None):
    items=[];labels={}
    for l in lines:
        l=l.split(';')[0].strip()
        if not l: continue
        if l.endswith(':'): labels[l[:-1]]=base+4*len(items); continue
        items.append(l)
    out=[]
    for i,l in enumerate(items):
        pc=base+4*i
        mn,_,rest=l.partition(' '); ops=[o.strip() for o in rest.split(',')] if rest else []
        def tgt(s): return labels[s] if s in labels else num(s)
        if mn=='.word': w=num(ops[0])
        elif mn=='blr': w=0x4e800020
        elif mn in D:
            if mn in('lwz','stw','lfs','stmw','lmw','stwu'):
                d,ra=parse_mem(ops[1]); w=D[mn]<<26|reg(ops[0])<<21|ra<<16|d&0xffff
            elif mn=='subfic' or mn=='addi' or mn=='mulli':
                w=D[mn]<<26|reg(ops[0])<<21|reg(ops[1])<<16|num(ops[2])&0xffff
        elif mn=='li': w=14<<26|reg(ops[0])<<21|num(ops[1])&0xffff
        elif mn=='lis': w=15<<26|reg(ops[0])<<21|num(ops[1])&0xffff
        elif mn=='ori': w=24<<26|reg(ops[1])<<21|reg(ops[0])<<16|num(ops[2])&0xffff
        elif mn=='cmpwi': w=11<<26|reg(ops[0])<<16|num(ops[1])&0xffff
        elif mn=='cmpw': w=31<<26|reg(ops[0])<<16|reg(ops[1])<<11
        elif mn in X: w=31<<26|reg(ops[0])<<21|reg(ops[1])<<16|reg(ops[2])<<11|X[mn]<<1
        elif mn=='stbx': w=31<<26|reg(ops[0])<<21|reg(ops[1])<<16|reg(ops[2])<<11|215<<1
        elif mn=='stfiwx': w=31<<26|reg(ops[0])<<21|num(ops[1])<<16|reg(ops[2])<<11|983<<1
        elif mn=='fctiwz': w=63<<26|reg(ops[0])<<21|reg(ops[1])<<11|15<<1
        elif mn=='fmr': w=63<<26|reg(ops[0])<<21|reg(ops[1])<<11|72<<1
        elif mn=='mflr': w=31<<26|reg(ops[0])<<21|8<<16|339<<1
        elif mn=='mtlr': w=31<<26|reg(ops[0])<<21|8<<16|467<<1
        elif mn=='mr': w=31<<26|reg(ops[1])<<21|reg(ops[0])<<16|reg(ops[1])<<11|444<<1
        elif mn=='slwi': n=num(ops[2]); w=21<<26|reg(ops[1])<<21|reg(ops[0])<<16|n<<11|0<<6|(31-n)<<1
        elif mn=='srwi': n=num(ops[2]); w=21<<26|reg(ops[1])<<21|reg(ops[0])<<16|(32-n)<<11|n<<6|31<<1
        elif mn in BC:
            bo,bi=BC[mn]; off=tgt(ops[0])-pc; w=16<<26|bo<<21|bi<<16|off&0xfffc
        elif mn=='b': w=18<<26|(tgt(ops[0])-pc)&0x3fffffc
        elif mn=='ba': w=18<<26|tgt(ops[0])&0x3fffffc|2
        elif mn=='bla': w=18<<26|tgt(ops[0])&0x3fffffc|3
        else: raise SystemExit('unknown '+l)
        out.append(w&0xffffffff)
    if labels_out is not None: labels_out.update(labels)
    return out
