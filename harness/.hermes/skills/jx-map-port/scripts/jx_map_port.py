#!/usr/bin/env python3
"""
jx_map_port.py — Extract ANY JX Online 1 / VLTK PC map (regions + SPR art) into the
Unity mobile client, at 99% fidelity, straight from the game's client paks.

THE KEY INSIGHT (do not re-derive — see SKILL.md):
  JX paks key entries by g_FileName2Id (engine.dll). It hashes each path byte as a
  SIGNED char (movsx) after lowercasing ASCII A-Z. Using UNSIGNED bytes makes every
  Chinese (GBK) path miss -> people wrongly conclude "data not in pak". Signed = works.

Usage:
  python3 jx_map_port.py --map-name '两湖区\\巴陵县' --project-map-id 79 \
      --unity-root /var/www/vltk-mobile [--data-dir ...] [--bounds minX minY maxX maxY]

The map name is the MapList.ini value (region\\name, GBK). Grid bounds are read from the
map's .wor (rect=minX,minY,maxX,maxY); pass --bounds to override or if the .wor is absent.
The default data source is /var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/data, then the
mounted VMDK if the extracted source tree is unavailable.
"""
import argparse, struct, ctypes, os, json, glob, shutil, sys

DEFAULT_PC_ROOT = "/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem"
DEFAULT_VMDK_ROOT = "/mnt/jxwin/SourceNew/swrod3"

# ---------------------------------------------------------------- UCL
_ucl = ctypes.CDLL("libucl.so")
for _fn in ("ucl_nrv2b_decompress_8", "ucl_nrv2d_decompress_8", "ucl_nrv2e_decompress_8"):
    _f = getattr(_ucl, _fn)
    _f.argtypes = [ctypes.POINTER(ctypes.c_ubyte), ctypes.c_uint,
                   ctypes.POINTER(ctypes.c_ubyte), ctypes.POINTER(ctypes.c_uint), ctypes.c_void_p]
    _f.restype = ctypes.c_int
TF = 0xFF000000

def ucl_decompress(cd, method, outlen):
    if method == 0:
        return cd
    if outlen <= 0 or outlen > 80_000_000:
        return None
    dst = (ctypes.c_ubyte * outlen)(); dl = ctypes.c_uint(outlen)
    src = (ctypes.c_ubyte * len(cd)).from_buffer_copy(cd)
    try:
        if method in (0x10000000, 0x01000000, 0x20000000, 0x02000000):
            r = _ucl.ucl_nrv2b_decompress_8(src, len(cd), dst, ctypes.byref(dl), None)
        elif method in (0x30000000, 0x03000000):
            r = _ucl.ucl_nrv2d_decompress_8(src, len(cd), dst, ctypes.byref(dl), None)
        elif method in (0x40000000, 0x04000000):
            r = _ucl.ucl_nrv2e_decompress_8(src, len(cd), dst, ctypes.byref(dl), None)
        else:
            return None
    except Exception:
        return None
    return bytes(dst[:dl.value]) if r == 0 else None

# ---------------------------------------------------------------- hashes
def g_filename2id(path_bytes):
    """engine.dll g_FileName2Id: lowercase ASCII, SIGNED char, *prime, *(-0x11)."""
    v = 0; i = 0
    for b in path_bytes:
        bb = b + 32 if 65 <= b <= 90 else b
        c = bb - 256 if bb >= 128 else bb          # signed char (movsx) — the fix
        i += 1
        v = ((v + i * c) % 0x8000000B) * 0xFFFFFFEF
        v &= 0xFFFFFFFF
    return v ^ 0x12345678

def compute_path_uid(name, enc="gb2312"):
    """Legacy UNSIGNED-byte staged-filename hash (extractor<->runtime naming only).

    This names staged copies `{uid}.spr`; it is NEVER a pak lookup (use file_id_from_bytes
    for that). C# `SprRuntimeService.ComputePathUid` now DEFAULTS to the signed-byte variant,
    and `ResolveSpr` tries uidFromPath -> signed -> unsigned, so existing unsigned-named
    staged files are still found as the final fallback. Kept unsigned here to avoid renaming
    the already-staged asset set; for pure-ASCII art names signed==unsigned anyway. If you
    ever re-stage CJK-named art, prefer matching the C# signed default to keep names aligned.
    """
    s = name.strip().replace('/', '\\')
    if not s.startswith('\\'):
        s = '\\' + s
    b = s.encode(enc, 'ignore'); v = 0
    for i, ch in enumerate(b):
        c = ch + 0x20 if 65 <= ch <= 90 else ch
        v = (((v + (i + 1) * c) % 0x8000000B) * 0xFFFFFFEF) & 0xFFFFFFFF
    return f"{(v ^ 0x12345678) & 0xffffffff:08x}"

def normalize_resource_path(name):
    s = name.strip().rstrip('\x00').replace('/', '\\')
    if s and not s.startswith('\\'):
        s = '\\' + s
    return s

# ---------------------------------------------------------------- pak index
def build_index(data_dir):
    idx = {}
    for pak in glob.glob(os.path.join(data_dir, "*.pak")):
        try:
            with open(pak, 'rb') as f:
                h = f.read(32)
                if h[:4] != b'PACK':
                    continue
                count, io = struct.unpack_from('<II', h, 4); f.seek(io)
                for _ in range(count):
                    u, o, sz, fl = struct.unpack('<IIii', f.read(16))
                    idx.setdefault(u & 0xffffffff, (pak, o, sz, fl))
        except Exception:
            pass
    return idx

def read_entry(loc):
    pak, o, sz, fl = loc
    with open(pak, 'rb') as f:
        f.seek(o); raw = f.read(fl & ~TF)
    return raw, (fl & TF), sz

# ---------------------------------------------------------------- region parsing
def parse_sections(d):
    if len(d) < 4:
        raise ValueError("Region_C too short")
    sc = struct.unpack_from('<I', d, 0)[0]; h = 4 + sc * 8
    if sc <= 0 or sc > 16:
        raise ValueError(f"invalid Region_C section count {sc}")
    if len(d) < h:
        raise ValueError(f"Region_C header truncated: sections={sc} size={len(d)}")
    return sc, h, [struct.unpack_from('<II', d, 4 + i * 8) for i in range(sc)]

def _clean(b):
    n = b.find(b'\x00')
    if n >= 0:
        b = b[:n]
    try:
        return b.decode('gbk')
    except Exception:
        return None

def collect_names(d):
    names = set(); sc, h, secs = parse_sections(d)
    if len(secs) > 4 and secs[4][1] > 0:           # GROUND: tiles + cover objects
        off, ln = secs[4]; seg = d[h+off:h+off+ln]
        nT, nO, oo = struct.unpack_from('<III', seg, 0); pos = 12
        for _ in range(nT):
            if pos + 8 > len(seg):
                break
            a, b, fr, nl = struct.unpack_from('<HHHH', seg, pos); pos += 8
            nm = _clean(seg[pos:pos+nl]); pos += nl
            if nm:
                names.add(nm)
        if 0 < oo < len(seg):
            pos = oo
        for _ in range(nO):
            if pos + 146 > len(seg):
                break
            nm = _clean(seg[pos+8:pos+8+128]); pos += 146
            if nm:
                names.add(nm)
    if len(secs) > 5 and secs[5][1] > 0:           # BUILTIN: name at +56, stride 228
        off, ln = secs[5]; seg = d[h+off:h+off+ln]
        if len(seg) >= 16:
            nB = struct.unpack_from('<I', seg, 0)[0]; pos = 16
            for _ in range(nB):
                if pos + 228 > len(seg):
                    break
                nm = _clean(seg[pos+56:pos+56+128]); pos += 228
                if nm:
                    names.add(nm)
    return names

# ---------------------------------------------------------------- SPR rebuild
def spr_is_flat_valid(raw):
    if len(raw) < 32 or raw[:3] != b'SPR':
        return False
    cc = struct.unpack_from('<H', raw, 14)[0]; fc = struct.unpack_from('<H', raw, 12)[0]
    hp = 32 + cc * 3
    if fc == 0 or hp + 8 > len(raw):
        return False
    o0, l0 = struct.unpack_from('<II', raw, hp)
    fb = hp + fc * 8
    if fb + o0 + l0 > len(raw) or l0 < 8 or l0 > 20_000_000:
        return False
    fw, fh = struct.unpack_from('<HH', raw, fb + o0)
    return 0 < fw <= 16384 and 0 < fh <= 16384

def spr_rebuild_perframe(raw):
    """Pak per-frame SPR -> flat SPR. After head+pal: frame_info[Frames]{compress_size:i32,
    size:i32}, then blobs; size<0 => raw(len=-size), size>=0 => UCL with orig length=size."""
    if len(raw) < 32 or raw[:3] != b'SPR':
        return None
    cc = struct.unpack_from('<H', raw, 14)[0]; fc = struct.unpack_from('<H', raw, 12)[0]
    hp = 32 + cc * 3
    if hp + fc * 8 > len(raw):
        return None
    finfo = [struct.unpack_from('<ii', raw, hp + k * 8) for k in range(fc)]
    p = hp + fc * 8; frames = []
    for csize, size in finfo:
        if csize < 0 or p + csize > len(raw):
            return None
        blob = raw[p:p+csize]; p += csize
        if size < 0:
            fr = blob[:(-size)]
        else:
            fr = ucl_decompress(blob, 0x10000000, size)
            if fr is None:
                return None
        frames.append(fr)
    out = bytearray(raw[:hp]); offs = bytearray(); fd = bytearray(); cur = 0
    for fr in frames:
        offs += struct.pack('<II', cur, len(fr)); fd += fr; cur += len(fr)
    out += offs + fd
    return bytes(out)

def get_flat_spr(raw, method, dsize):
    if raw[:3] != b'SPR':                           # whole-UCL stored
        dec = ucl_decompress(raw, method, dsize)
        return dec if dec and dec[:3] == b'SPR' else None
    if spr_is_flat_valid(raw):
        return raw
    return spr_rebuild_perframe(raw)                # per-frame compressed

# ---------------------------------------------------------------- loose art fallback
def loose_index(art_roots):
    by_rel = {}
    for root in art_roots:
        if not os.path.isdir(root):
            continue
        for dp, _, files in os.walk(root):
            for fn in files:
                if fn.lower().endswith('.spr'):
                    rel = os.path.relpath(os.path.join(dp, fn), root).replace('\\', '/').lower()
                    by_rel.setdefault(rel, os.path.join(dp, fn))
    return by_rel

def loose_path(by_rel, nm):
    rel = nm.lstrip('\\').replace('\\', '/').lower()
    if rel in by_rel:
        return by_rel[rel]
    for k, v in by_rel.items():
        if k.endswith(rel):
            return v
    return None

# ---------------------------------------------------------------- bounds from .wor
def read_wor_bounds(run_maps_dir, map_name):
    if not run_maps_dir:
        return None
    wor = os.path.join(run_maps_dir, *map_name.split('\\')) + ".wor"
    if not os.path.exists(wor):
        return None
    try:
        txt = open(wor, 'rb').read().decode('gbk', 'replace')
        for line in txt.splitlines():
            if line.lower().startswith('rect='):
                a = [int(x) for x in line.split('=', 1)[1].split(',')]
                if len(a) == 4:
                    return tuple(a)            # minX,minY,maxX,maxY
    except Exception:
        pass
    return None

# ---------------------------------------------------------------- main
def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--map-name", required=True, help=r"MapList value, GBK, e.g. 两湖区\巴陵县")
    ap.add_argument("--project-map-id", type=int, required=True, help="Unity StreamingAssets Map_{id}_C")
    ap.add_argument("--unity-root", default="/var/www/vltk-mobile")
    ap.add_argument("--pc-root", default=DEFAULT_PC_ROOT, help="extracted PC source root")
    ap.add_argument("--vmdk-root", default=DEFAULT_VMDK_ROOT, help="optional mounted VMDK root")
    ap.add_argument("--data-dir", default=None, help="override pak dir (default extracted PC client, then VMDK)")
    ap.add_argument("--bounds", nargs=4, type=int, metavar=("MINX","MINY","MAXX","MAXY"),
                    help="grid bounds; overrides .wor")
    ap.add_argument("--scan-pad", type=int, default=0, help="expand bounds by N cells when scanning")
    ap.add_argument("--extra-spr", action="append", default=[],
                    help="additional SPR path to stage with the map (repeatable; useful for mission NPC visuals)")
    ap.add_argument("--extra-spr-file", default=None,
                    help="newline-delimited extra SPR paths to stage with the map")
    args = ap.parse_args()

    pc_client = os.path.join(args.pc_root, "Client 6.0")
    pc_data = os.path.join(pc_client, "data")
    vmdk_data = os.path.join(args.vmdk_root, "bin/Client/data")
    if args.data_dir:
        data_dir = args.data_dir
    elif os.path.isdir(pc_data):
        data_dir = pc_data
    else:
        data_dir = vmdk_data

    run_maps_candidates = [
        os.path.join(pc_client, "maps"),
        os.path.join(args.vmdk_root, "Utility/Run/maps"),
    ]
    run_maps = next((p for p in run_maps_candidates if os.path.isdir(p)), None)
    art_roots = [p for p in [
        pc_client,
        os.path.join(args.vmdk_root, "Utility/Run"),
        os.path.join(args.vmdk_root, "bin/Client"),
    ] if os.path.isdir(p)]
    dest_reg = os.path.join(args.unity_root, "Assets/StreamingAssets/TestData/Regions",
                            f"Map_{args.project_map_id}_C")
    dest_spr = os.path.join(args.unity_root, "Assets/StreamingAssets/Sprites")

    if not os.path.isdir(data_dir):
        print(f"ERROR: pak dir not found: {data_dir}. Check --pc-root/--data-dir or mount the VMDK.", file=sys.stderr)
        return 2

    bounds = tuple(args.bounds) if args.bounds else read_wor_bounds(run_maps, args.map_name)
    if not bounds:
        print("ERROR: no .wor bounds found; pass --bounds MINX MINY MAXX MAXY", file=sys.stderr)
        return 2
    minX, minY, maxX, maxY = bounds
    p = args.scan_pad
    minX, minY, maxX, maxY = minX - p, minY - p, maxX + p, maxY + p
    print(f"map='{args.map_name}' bounds=({minX},{minY})-({maxX},{maxY}) -> Map_{args.project_map_id}_C")

    idx = build_index(data_dir)
    print(f"pak entries indexed: {len(idx)}")

    os.makedirs(dest_reg, exist_ok=True); os.makedirs(dest_spr, exist_ok=True)
    for f in glob.glob(os.path.join(dest_reg, "*_Region_C.dat")):
        os.remove(f)
        if os.path.exists(f + ".meta"):
            os.remove(f + ".meta")

    # 1) regions
    regions = []; allnames = set(); n = 0; invalid = []
    for X in range(minX, maxX + 1):
        for Y in range(minY, maxY + 1):
            path = f"\\maps\\{args.map_name}\\v_{Y:03d}\\{X:03d}_Region_C.dat"
            uid = g_filename2id(path.encode("gbk", "ignore"))
            if uid not in idx:
                continue
            raw, method, dsize = read_entry(idx[uid])
            d = ucl_decompress(raw, method, dsize)
            if not d or len(d) < 4:
                continue
            try:
                sc, h, secs = parse_sections(d)
                allnames |= collect_names(d)
            except ValueError as ex:
                invalid.append((X, Y, str(ex)))
                continue
            open(os.path.join(dest_reg, f"{X}_{Y}_Region_C.dat"), 'wb').write(d)
            regions.append({"col": X, "row": Y,
                            "hasGround": len(secs) > 4 and secs[4][1] > 0,
                            "hasBuiltin": len(secs) > 5 and secs[5][1] > 0,
                            "size": len(d)})
            n += 1
    json.dump({"mapId": args.project_map_id, "name": args.map_name.split('\\')[-1],
               "source": "client paks via g_FileName2Id (signed-byte hash)",
               "regionSceneWidth": 512, "regionSceneHeight": 1024,
               "groundCell": 32, "screenYScale": 0.5, "regions": regions},
              open(os.path.join(dest_reg, "manifest.json"), "w"), ensure_ascii=False, indent=2)
    json.dump(sorted(allnames), open(os.path.join(dest_reg, "image_names.json"), "w"),
              ensure_ascii=False, indent=2)
    print(f"extracted regions: {n}; distinct imageNames: {len(allnames)}")
    extra_spr = [normalize_resource_path(x) for x in args.extra_spr if x and x.strip()]
    if args.extra_spr_file:
        with open(args.extra_spr_file, "r", encoding="utf-8") as f:
            extra_spr.extend(normalize_resource_path(x) for x in f if x.strip())
    extra_spr = sorted(set(x for x in extra_spr if x))
    if extra_spr:
        json.dump(extra_spr, open(os.path.join(dest_reg, "extra_spr_names.json"), "w"),
                  ensure_ascii=False, indent=2)
        allnames |= set(extra_spr)
        print(f"extra SPRs to stage: {len(extra_spr)}")
    if invalid:
        print(f"skipped invalid/colliding Region_C entries: {len(invalid)}")
        for X, Y, reason in invalid[:20]:
            print(f"  INVALID {X}_{Y}: {reason}")
    if n == 0:
        print("WARNING: 0 regions. Check map name spelling/encoding and bounds. "
              "See references/pitfalls.md.", file=sys.stderr)

    # 2) art
    by_rel = loose_index(art_roots)
    ok = 0; fail = []
    for nm in allnames:
        dest = os.path.join(dest_spr, compute_path_uid(nm) + ".spr")
        uid = g_filename2id(nm.encode("gbk", "ignore"))
        written = os.path.exists(dest) and os.path.getsize(dest) > 0
        if written:
            ok += 1
        if uid in idx:
            raw, method, dsize = read_entry(idx[uid])
            flat = get_flat_spr(raw, method, dsize)
            if flat:
                open(dest, 'wb').write(flat)
                if not written:
                    ok += 1
                written = True
        if not written:
            lp = loose_path(by_rel, nm)
            if lp:
                shutil.copy(lp, dest); written = True; ok += 1
        if not written:
            fail.append(nm)
    print(f"staged art: {ok}/{len(allnames)} failed={len(fail)}")
    for m in fail[:40]:
        print("  FAIL", m)
    return 0

if __name__ == "__main__":
    sys.exit(main())
