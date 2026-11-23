"""Reads a UWUVCI .vcb base list (gzip + .NET BinaryFormatter of List<GameBases>) without .NET.

Only the record types that occur in these files are handled (MS-NRBF 2.x). Anything else raises, so a
changed format fails loudly instead of producing wrong data.

Usage: python3 -I read_vcb.py bases.vcbn64 > bases-n64.json
"""
import gzip
import json
import struct
import sys

REGIONS = {0: "EU", 1: "US", 2: "JP"}


class Reader:
    def __init__(self, data: bytes) -> None:
        self.d, self.p = data, 0
        self.classes = {}   # metadata id -> (name, [(member, bintype, extra)])
        self.objects = {}   # object id -> value

    def u8(self) -> int:
        v = self.d[self.p]; self.p += 1; return v

    def i32(self) -> int:
        v = struct.unpack_from("<i", self.d, self.p)[0]; self.p += 4; return v

    def lps(self) -> str:
        n, shift = 0, 0
        while True:
            b = self.u8(); n |= (b & 0x7F) << shift; shift += 7
            if not b & 0x80: break
        s = self.d[self.p:self.p + n].decode("utf-8"); self.p += n; return s

    def class_info(self):
        oid, name, count = self.i32(), self.lps(), self.i32()
        members = [self.lps() for _ in range(count)]
        types = [self.u8() for _ in range(count)]
        extra = []
        for t in types:
            if t in (0, 7): extra.append(self.u8())        # primitive type
            elif t == 3: extra.append(self.lps())           # system class name
            elif t == 4: extra.append((self.lps(), self.i32()))
            else: extra.append(None)
        return oid, name, members, list(zip(types, extra))

    def value(self, btype, extra):
        if btype in (0, 7):  # primitive
            if extra == 8: return self.i32()
            raise ValueError(f"primitive type {extra} not supported")
        return self.record()

    def record(self):
        t = self.u8()
        if t == 6:   # BinaryObjectString
            oid = self.i32(); s = self.lps(); self.objects[oid] = s; return s
        if t == 9:   # MemberReference
            return self.objects.get(self.i32())
        if t == 10:  # ObjectNull
            return None
        if t == 12:  # BinaryLibrary
            self.i32(); self.lps(); return self.record()
        if t == 5:   # ClassWithMembersAndTypes
            oid, name, members, meta = self.class_info(); self.i32()
            self.classes[oid] = (name, members, meta)
            return self.read_members(oid, name, members, meta)
        if t == 4:   # SystemClassWithMembersAndTypes (no library id)
            oid, name, members, meta = self.class_info()
            self.classes[oid] = (name, members, meta)
            return self.read_members(oid, name, members, meta)
        if t == 1:   # ClassWithId
            oid, mid = self.i32(), self.i32()
            name, members, meta = self.classes[mid]
            return self.read_members(oid, name, members, meta)
        raise ValueError(f"record type {t} at {self.p - 1} not supported")

    def read_members(self, oid, name, members, meta):
        obj = {"__class": name}
        for m, (bt, ex) in zip(members, meta):
            obj[m] = self.value(bt, ex)
        self.objects[oid] = obj
        return obj


def parse(path: str) -> list:
    r = Reader(gzip.decompress(open(path, "rb").read()))
    assert r.u8() == 0; r.p += 16                 # SerializedStreamHeader
    lst = r.record()                               # List<GameBases> (items, size, version)
    # The List object is followed by a BinaryArray of references; the objects themselves come after it.
    t = r.u8(); assert t == 7, f"expected BinaryArray, got {t}"
    r.i32(); assert r.u8() == 0; assert r.i32() == 1      # id, single dimension, rank 1
    length = r.i32(); assert r.u8() == 4                  # element type: class
    r.lps(); r.i32()                                      # class name, library id
    refs, seen = [], 0
    while seen < length:
        t = r.u8()
        if t == 9: refs.append(r.i32()); seen += 1
        elif t == 13: n = r.u8(); refs += [None] * n; seen += n   # ObjectNullMultiple256
        elif t == 10: refs.append(None); seen += 1
        else: raise ValueError(f"array element record {t} not supported")
    while r.d[r.p] != 11:                                 # until MessageEnd
        r.record()
    items = [r.objects.get(x) if x is not None else None for x in refs]
    out = []
    for it in items:
        if it is None: continue
        reg = it["<Region>k__BackingField"]
        out.append({
            "name": it["<Name>k__BackingField"],
            "region": REGIONS[reg["value__"] if isinstance(reg, dict) else reg],
            "titleId": it["<Tid>k__BackingField"],
            "keyHash": it["<KeyHash>k__BackingField"],
        })
    assert lst and len(out) > 0
    return out


if __name__ == "__main__":
    json.dump(parse(sys.argv[1]), sys.stdout, ensure_ascii=False, indent=2)
    print()
