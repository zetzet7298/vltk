# -*- coding: utf-8 -*-
import os, sys

def normalize(p):
    p = p.replace("\\", "/")
    while p.startswith("/"):
        p = p[1:]
    return p

def path_uid(path, signed=True, encoding="gb2312"):
    norm = normalize(path)
    if not norm:
        return 0
    try:
        data = norm.encode(encoding)
    except Exception:
        data = norm.encode("utf-8")
    value = 0
    for i in range(len(data)):
        b = data[i]
        signed_b = b - 256 if b >= 128 else b
        c = signed_b if signed else b
        if 65 <= c <= 90:
            c += 32
        index = i + 1
        value = ((value + index * c) % 0x8000000B) * 0xFFFFFFEF & 0xFFFFFFFF
    return (value ^ 0x12345678) & 0xFFFFFFFF

root = r"Assets/StreamingAssets/Sprites"
paths = [
    (318, "\\spr\\skill\\150\\sl\\sl_150_longchenbannuo_c.spr"),
    (271, "\\spr\\skill\\峨嵋\\tm_10_施法.spr"),
    (271, "\\spr\\skill\\emei\\tm_10_施法.spr"),
    (322, "\\spr\\skill\\1502\\cy\\cy_150_daocui_zd.spr"),
    (325, "\\spr\\skill\\tm\\bz_紫气东来.spr"),
]
for sid, p in paths:
    hits = []
    for signed in (True, False):
        uid = path_uid(p, signed)
        if os.path.exists(os.path.join(root, format(uid, "08x") + ".spr")):
            hits.append(uid)
    print(sid, p, "STAGED" if hits else "not staged", hits)
