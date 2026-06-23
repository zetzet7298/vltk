#!/usr/bin/env python3
r"""Extract one HUD/UI SPR (or a narrow folder) to PNG via the canonical vltktool.

WHY THIS IS A WRAPPER, NOT A DECODER
------------------------------------
Per AGENTS.md repo rules: do NOT write ad-hoc SPR/PAK decoders, and do NOT scan the whole
source tree (broad `rglob('*.spr')` can crash the machine and produces false confidence).
The canonical, tested decoder is `~/Projects/vltktool/extract_item_spr.py` (top-down rows
matching the game; `--legacy-flip` for the old inverted output). This script just points it
at the right input and enforces "narrow a region first".

SOURCE OF TRUTH
---------------
PC art lives in the canonical unpacked PAK tree:
    /var/www/jx-source/pak_unpacked
HUD/UI SPRs are under the `Client 6.0/data/<pak>/...` subtrees (notably `1024/`, `800/`,
`updatejx08/`). The old `jxwin-kinnox/.../Utility/Run/spr/Ui3` path does NOT exist on disk.

USAGE (you MUST pass an explicit --file or --src; this script refuses to scan everything)
    # one SPR -> PNG frames in Assets/UI/HUD/Art
    python3 extract_ui_spr.py --file '/var/www/jx-source/pak_unpacked/.../jx1024.spr'

    # a single narrow folder (e.g. one pak's UI subdir) -> PNGs
    python3 extract_ui_spr.py --src '/var/www/jx-source/pak_unpacked/<pak>/<uidir>'

    # only one frame, or rollback to legacy bottom-up rows
    python3 extract_ui_spr.py --file <spr> --frame 0
    python3 extract_ui_spr.py --file <spr> --legacy-flip

To FIND a UI SPR's uid/path first, hash the GBK path and match a pak index, or use the
vltktool resolvers (`resolve_uid.py`, `find_spr_by_image.py --pak <one pak>`). Never scan
the whole tree to "find" art.
"""
import argparse
import os
import subprocess
import sys

VLTKTOOL = "~/Projects/vltktool/extract_item_spr.py"
DEFAULT_OUT = "/var/www/vltk-mobile/Assets/UI/HUD/Art"
PAK_UNPACKED = "/var/www/jx-source/pak_unpacked"


def main():
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    g = ap.add_mutually_exclusive_group(required=True)
    g.add_argument("--file", help="A single .spr file to decode.")
    g.add_argument("--src", help="A NARROW folder of .spr files (one pak/subdir). "
                                 "Refuses to run against the whole pak_unpacked root.")
    ap.add_argument("--out-root", default=DEFAULT_OUT, help=f"PNG output (default {DEFAULT_OUT})")
    ap.add_argument("--frame", type=int, help="Extract only this frame index.")
    ap.add_argument("--legacy-flip", action="store_true",
                    help="Decode rows bottom-up (old inverted output). Default is top-down.")
    args = ap.parse_args()

    if not os.path.exists(VLTKTOOL):
        sys.exit(f"ERROR: canonical decoder missing: {VLTKTOOL}")

    cmd = ["python3", VLTKTOOL, "--out-root", args.out_root]
    if args.file:
        if not os.path.isfile(args.file):
            sys.exit(f"ERROR: --file not found: {args.file}")
        cmd += ["--file", args.file]
    else:
        src = os.path.abspath(args.src)
        if not os.path.isdir(src):
            sys.exit(f"ERROR: --src not a directory: {src}")
        if os.path.abspath(src) == os.path.abspath(PAK_UNPACKED):
            sys.exit("ERROR: refusing to scan the entire pak_unpacked root. "
                     "Narrow to one pak/subdir first (see this script's docstring).")
        cmd += ["--spr-root", src]
    if args.frame is not None:
        cmd += ["--frame", str(args.frame)]
    if args.legacy_flip:
        cmd += ["--legacy-flip"]

    os.makedirs(args.out_root, exist_ok=True)
    print("RUN:", " ".join(cmd))
    raise SystemExit(subprocess.call(cmd))


if __name__ == "__main__":
    main()
