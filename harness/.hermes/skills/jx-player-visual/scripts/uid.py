#!/usr/bin/env python3
r"""JX path UID helper for staged player/NPC SPRs.

This mirrors the project runtime `SprRuntimeService.ComputePathUid`, whose default
is the PC signed-byte FileNameHash (`g_FileName2Id`, exported from engine.dll).

There are TWO byte treatments; the ONLY difference is how a path byte >= 0x80 is read:

  * SIGNED   (default, PC-accurate): high bytes are treated as signed char
             (`b - 256`). REQUIRED for any path containing Chinese/GBK folders
             such as `\spr\Ui\技能图标\...`. This is what `g_FileName2Id` does and
             what `SprRuntimeService.ComputePathUid(..., signedBytes:true)` does.
  * UNSIGNED (legacy): high bytes kept as 0..255. Only agrees with SIGNED for
             pure-ASCII paths; it MISSES real `unknown/<uid>.spr` assets for CJK
             paths and is the historical "fake missing asset" bug.

Most player/NPC part paths are pure ASCII (`spr\npcres\man\MA_BD_019_ST01.spr`),
so signed and unsigned agree there. Always default to SIGNED so CJK paths resolve.

Usage:
    python3 uid.py 'spr\npcres\man\MA_BD_019_ST01.spr'
    -> 45488ea8
    python3 uid.py --unsigned 'spr\npcres\man\MA_BD_019_ST01.spr'   # legacy variant
Evidence: '\spr\Ui\技能图标\icon_sk_ty_at.spr' (GB2312) -> signed c4454165, unsigned bedc5b69.
"""
import sys


def normalize(path: str) -> str:
    p = path.strip().rstrip("\0").replace("/", "\\")
    if not p:
        return ""
    if not p.startswith("\\"):
        p = "\\" + p
    return p


def compute_uid(path: str, encoding: str = "gb2312", signed_bytes: bool = True) -> int:
    norm = normalize(path)
    if not norm:
        return 0
    try:
        data = norm.encode(encoding, "replace")
    except LookupError:
        data = norm.encode("utf-8", "replace")
    value = 0
    for i, b in enumerate(data):
        c = (b - 256 if b >= 128 else b) if signed_bytes else b
        if 65 <= c <= 90:        # 'A'..'Z' -> lowercase ASCII only
            c += 32
        idx = i + 1
        value = ((value + idx * c) % 0x8000000B) * 0xFFFFFFEF & 0xFFFFFFFF
    return (value ^ 0x12345678) & 0xFFFFFFFF


def uid_hex(path: str, encoding: str = "gb2312", signed_bytes: bool = True) -> str:
    u = compute_uid(path, encoding, signed_bytes)
    return None if u == 0 else format(u, "08x")


if __name__ == "__main__":
    args = sys.argv[1:]
    signed = True
    if args and args[0] in ("--unsigned", "-u"):
        signed = False
        args = args[1:]
    if not args:
        print(__doc__)
        sys.exit(1)
    for arg in args:
        print(uid_hex(arg, signed_bytes=signed))
