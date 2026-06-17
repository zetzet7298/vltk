#!/usr/bin/env python3
"""
list_maps.py — List/search JX maps from MapList.ini and report each map's .wor bounds.
Use this to find the exact --map-name and bounds before running jx_map_port.py.

Usage:
  python3 list_maps.py                 # list all maps (index, name, bounds)
  python3 list_maps.py 巴陵            # filter by substring (UTF-8 or pinyin-free)
  python3 list_maps.py --id 53         # show one map by MapList index
"""
import argparse, os, re, sys

PC_ROOT = "/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem"
VMDK = "/mnt/jxwin/SourceNew/swrod3"

def first_existing(paths):
    return next((p for p in paths if os.path.exists(p)), None)

def wor_bounds(map_name, run_maps_dirs):
    for root in run_maps_dirs:
        if not root or not os.path.isdir(root):
            continue
        wor = os.path.join(root, *map_name.split('\\')) + ".wor"
        if os.path.exists(wor):
            break
    else:
        return None
    if not os.path.exists(wor):
        return None
    try:
        for line in open(wor, 'rb').read().decode('gbk', 'replace').splitlines():
            if line.lower().startswith('rect='):
                a = [int(x) for x in line.split('=', 1)[1].split(',')]
                if len(a) == 4:
                    return tuple(a)
    except Exception:
        pass
    return None

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("filter", nargs="?", default=None, help="substring to match in the map name")
    ap.add_argument("--id", type=int, default=None, help="MapList index to show")
    ap.add_argument("--maplist", default=None, help="override MapList.ini path")
    ap.add_argument("--run-maps", default=None, help="override .wor maps root")
    args = ap.parse_args()

    maplist = args.maplist or first_existing([
        os.path.join(PC_ROOT, "Client 6.0/settings/maplist.ini"),
        os.path.join(PC_ROOT, "Server 6.0/server/home_jxser_bachkim_6.0/server1/settings/maplist.ini"),
        os.path.join(VMDK, "Utility/Run/Settings/MapList.ini"),
    ])
    run_maps_dirs = [args.run_maps] if args.run_maps else [
        os.path.join(PC_ROOT, "Client 6.0/maps"),
        os.path.join(PC_ROOT, "Server 6.0/server/home_jxser_bachkim_6.0/server1/maps"),
        os.path.join(VMDK, "Utility/Run/maps"),
    ]

    if not maplist or not os.path.exists(maplist):
        print("ERROR: MapList.ini not found; pass --maplist or check PC source/VMDK", file=sys.stderr)
        return 2

    txt = open(maplist, 'rb').read().decode('gbk', 'replace')
    rows = []
    for line in txt.splitlines():
        m = re.match(r'^(\d+)\s*=\s*(.+?)\s*$', line)
        if m and '\\' in m.group(2):
            rows.append((int(m.group(1)), m.group(2).strip()))

    for idx, name in rows:
        if args.id is not None and idx != args.id:
            continue
        if args.filter and args.filter not in name:
            continue
        b = wor_bounds(name, run_maps_dirs)
        bstr = f"rect={b[0]},{b[1]},{b[2]},{b[3]}" if b else "(.wor missing -> pass --bounds)"
        print(f"  id={idx:<4} {name}    {bstr}")
    return 0

if __name__ == "__main__":
    sys.exit(main())
