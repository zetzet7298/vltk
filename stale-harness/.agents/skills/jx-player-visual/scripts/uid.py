#!/usr/bin/env python3
"""Unsigned JX runtime path-UID hash.

Matches SprRuntimeService.ComputePathUid (C#). This is the RUNTIME file-naming
hash, NOT the signed g_FileName2Id pak-lookup hash (see jx-map-port for that one).
Player part paths are ASCII-only so signed/unsigned agree, but the staged file
names must be produced by THIS function to line up with what the C# runtime
re-derives at load time.

Usage:
    python3 uid.py 'spr\\npcres\\man\\MA_BD_019_ST01.spr'
    -> 45488ea8
"""
import sys


def normalize(path: str) -> str:
    p = path.strip().rstrip("\0").replace("/", "\\")
    if not p:
        return ""
    if not p.startswith("\\"):
        p = "\\" + p
    return p


def compute_uid(path: str, encoding: str = "gb2312") -> int:
    norm = normalize(path)
    if not norm:
        return 0
    try:
        data = norm.encode(encoding, "replace")
    except LookupError:
        data = norm.encode("utf-8", "replace")
    value = 0
    for i, b in enumerate(data):
        if 65 <= b <= 90:        # 'A'..'Z' -> lowercase
            b += 32
        idx = i + 1
        value = ((value + idx * b) % 0x8000000B) * 0xFFFFFFEF & 0xFFFFFFFF
    return (value ^ 0x12345678) & 0xFFFFFFFF


def uid_hex(path: str, encoding: str = "gb2312") -> str:
    u = compute_uid(path, encoding)
    return None if u == 0 else format(u, "08x")


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(1)
    for arg in sys.argv[1:]:
        print(uid_hex(arg))
