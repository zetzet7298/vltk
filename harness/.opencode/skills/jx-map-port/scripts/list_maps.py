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

VMDK = "/mnt/jxwin/SourceNew/swrod3"
MAPLIST = os.path.join(VMDK, "Utility/Run/Settings/MapList.ini")
RUN_MAPS = os.path.join(VMDK, "Utility/Run/maps")

def wor_bounds(map_name):
    wor = os.path.join(RUN_MAPS, *map_name.split('\\')) + ".wor"
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
    args = ap.parse_args()

    if not os.path.exists(MAPLIST):
        print(f"ERROR: {MAPLIST} not found (is the VMDK mounted?)", file=sys.stderr)
        return 2

    txt = open(MAPLIST, 'rb').read().decode('gbk', 'replace')
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
        b = wor_bounds(name)
        bstr = f"rect={b[0]},{b[1]},{b[2]},{b[3]}" if b else "(.wor missing -> pass --bounds)"
        print(f"  id={idx:<4} {name}    {bstr}")
    return 0

if __name__ == "__main__":
    sys.exit(main())
