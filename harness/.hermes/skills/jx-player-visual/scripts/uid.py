#!/usr/bin/env python3
"""JX path UID helper for staged player SPRs.

Current project runtime SprRuntimeService.ComputePathUid defaults to PC signed-byte
FileNameHash and also supports the legacy unsigned variant with signedBytes:false.
Most player part paths are ASCII-only, so signed/unsigned agree. For any future
Chinese/GBK player/NPC part path, use the signed-byte PC hash or the runtime will
miss real unknown/<uid>.spr assets.

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
