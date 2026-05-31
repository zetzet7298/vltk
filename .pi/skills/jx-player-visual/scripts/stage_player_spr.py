#!/usr/bin/env python3
"""Stage a folder of JX player part SPRs into Unity StreamingAssets.

For every *.spr in a source folder it:
  1. computes the UNSIGNED runtime uid (see uid.py),
  2. copies the file to <unity_root>/Assets/StreamingAssets/Sprites/<uid>.spr,
  3. records a manifest entry {name, sourcePath, uid, unityPath, bytes}.

The manifest json is what humans inspect; the runtime itself does NOT read it,
it re-derives <uid>.spr from the sourcePath string in the C# catalog. So the
`sourcePath` written here MUST equal the catalog string exactly, including the
leading 'spr\\npcres\\...' backslash form.

Example:
    python3 stage_player_spr.py \
      --src jxwin-kinnox/SourceNew/swrod3/Utility/Run/spr/npcres/man \
      --source-prefix 'spr\\npcres\\man' \
      --unity-root /var/www/vltk-mobile \
      --manifest Assets/StreamingAssets/woman_player_sprites.json \
      --only MA_BD_019 MA_YY_999      # optional name filters (prefix match)
"""
import argparse
import datetime
import json
import os
import shutil

from uid import uid_hex


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--src", required=True, help="Folder of source .spr files")
    ap.add_argument("--source-prefix", required=True,
                    help=r"Backslash path prefix used by the C# catalog, e.g. spr\npcres\man")
    ap.add_argument("--unity-root", required=True)
    ap.add_argument("--manifest", required=True,
                    help="Manifest path relative to unity-root")
    ap.add_argument("--only", nargs="*", default=None,
                    help="Optional name prefixes to include (default: all)")
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()

    sprites_dir = os.path.join(args.unity_root, "Assets", "StreamingAssets", "Sprites")
    os.makedirs(sprites_dir, exist_ok=True)

    files = sorted(f for f in os.listdir(args.src) if f.lower().endswith(".spr"))
    if args.only:
        files = [f for f in files if any(f.startswith(p) for p in args.only)]

    entries, missing = [], []
    for name in files:
        src_path = os.path.join(args.src, name)
        source_path = args.source_prefix.rstrip("\\") + "\\" + name
        uid = uid_hex(source_path)
        if not uid:
            missing.append(name)
            continue
        unity_rel = f"Assets/StreamingAssets/Sprites/{uid}.spr"
        dest = os.path.join(args.unity_root, unity_rel)
        nbytes = os.path.getsize(src_path)
        if not args.dry_run:
            shutil.copyfile(src_path, dest)
        entries.append({
            "name": name,
            "sourcePath": source_path,
            "uid": uid,
            "unityPath": unity_rel,
            "bytes": nbytes,
        })
        print(f"{name:28} -> {uid}.spr ({nbytes} bytes)")

    manifest = {
        "generatedAt": datetime.datetime.now(datetime.timezone.utc).isoformat().replace("+00:00", "Z"),
        "source": args.src,
        "count": len(entries),
        "missing": missing,
        "sprites": entries,
    }
    out = os.path.join(args.unity_root, args.manifest)
    if not args.dry_run:
        with open(out, "w", encoding="utf-8") as fh:
            json.dump(manifest, fh, ensure_ascii=False, indent=2)
    print(f"\n{len(entries)} staged, {len(missing)} missing -> {out}"
          + (" (dry-run)" if args.dry_run else ""))


if __name__ == "__main__":
    main()
