#!/usr/bin/env python3
"""Crude delimiter balance check on ported .rs files, ignoring comments and strings."""
import re, subprocess, sys
from pathlib import Path

files = subprocess.run(
    ["grep", "-rl", "^// PORT-STATUS: done", "--include=*.rs", "."],
    capture_output=True, text=True).stdout.split()

bad = 0
for f in sorted(files):
    src = Path(f).read_text()
    # strip line comments, block comments, string and char literals
    s = re.sub(r'//[^\n]*', '', src)
    s = re.sub(r'/\*.*?\*/', '', s, flags=re.S)
    # DOTALL matters: Rust's `"\` line-continuation escape puts a newline
    # after the backslash, and without it the escape class stops matching and
    # the whole string-stripping regex desyncs — reporting a phantom imbalance
    # in any file containing a multi-line string literal.
    s = re.sub(r'"(\\.|[^"\\])*"', '""', s, flags=re.S)
    s = re.sub(r"'(\\.|[^'\\])'", "''", s, flags=re.S)
    for o, c in [('{', '}'), ('(', ')'), ('[', ']')]:
        if s.count(o) != s.count(c):
            print(f"UNBALANCED {f} {o}{c} {s.count(o)}/{s.count(c)}")
            bad += 1
print(f"checked {len(files)} ported files; {bad} imbalanced")
sys.exit(1 if bad else 0)
