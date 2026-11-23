"""Generate n64-games.tsv from mupen64plus.ini (GPL game database, data only).

Usage: python3 -I gen_n64_db.py mupen64plus.ini out.tsv
Columns: crc1, crc2, name, savetype, players, rumble, mempak
Entries that only reference another via RefMD5 (hacks, bad dumps, translations)
share the CRC of the verified entry, so they are skipped to keep one row per CRC.
"""
import re
import sys


def _rank(entry: dict) -> tuple:
    """Prefer verified dumps ([!]), then entries that carry real metadata."""
    return ("[!]" in entry["GoodName"], "SaveType" in entry, "Players" in entry)


def main(src: str, dst: str) -> None:
    rows = {}
    cur = None
    for line in open(src, encoding="utf-8", errors="replace"):
        line = line.strip()
        if line.startswith("["):
            cur = {}
            continue
        if cur is None or "=" not in line:
            continue
        key, _, val = line.partition("=")
        cur[key.strip()] = val.strip()
        if key.strip() == "CRC" and "RefMD5" not in cur:
            pass
        # Entries are finalized lazily below.
        if "GoodName" in cur and "CRC" in cur:
            rows[id(cur)] = cur
    seen = {}
    for entry in rows.values():
        if "RefMD5" in entry:
            continue
        m = re.fullmatch(r"([0-9A-Fa-f]{8})\s+([0-9A-Fa-f]{8})", entry["CRC"])
        if not m:
            continue
        key = (m.group(1).upper(), m.group(2).upper())
        prev = seen.get(key)
        if prev is None or _rank(entry) > _rank(prev):
            seen[key] = entry
    with open(dst, "w", encoding="utf-8", newline="\n") as out:
        for (c1, c2), e in sorted(seen.items()):
            name = e["GoodName"].replace("\t", " ")
            out.write("\t".join([
                c1, c2, name, e.get("SaveType", "None"), e.get("Players", "0"),
                e.get("Rumble", "No"), e.get("Mempak", "No"),
            ]) + "\n")


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
