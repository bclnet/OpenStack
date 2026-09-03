#!/usr/bin/env python3
"""Stamp real C# hashes into ported .rs headers and sync PORT_MAP.tsv statuses."""
import csv, hashlib, re
from pathlib import Path

SRC = Path("/home/claude/src/dotnet")
OUT = Path("/home/claude/rust")
MAP = OUT / "PORT_MAP.tsv"

rows = list(csv.DictReader(MAP.open(), delimiter="\t"))
stamped = done = 0

for r in rows:
    rs = OUT / r["rs_path"]
    cs = SRC / r["cs_path"]
    if not rs.exists() or not cs.exists():
        continue
    text = rs.read_text()
    if "// PORT-STATUS: done" not in text:
        continue
    sha = hashlib.sha256(cs.read_bytes()).hexdigest()[:16]
    new = re.sub(r"^// PORT-SHA: .*$", f"// PORT-SHA: {sha}", text, count=1, flags=re.M)
    if new != text:
        rs.write_text(new)
        stamped += 1
    r["status"] = "done"
    r["cs_sha256_16"] = sha
    done += 1

with MAP.open("w", newline="") as fh:
    w = csv.DictWriter(fh, fieldnames=rows[0].keys(), delimiter="\t")
    w.writeheader()
    w.writerows(rows)

print(f"stamped {stamped} headers; {done} files marked done of {len(rows)}")
