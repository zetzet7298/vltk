#!/usr/bin/env python3
import json
import sys
from pathlib import Path

EXPECTED = {
    "g_7e0478bbbbc310c5": {"mapId": 396, "regionC": 176, "regionS": 122, "imageNames": 68},
    "g_15f3c8b336d024d4": {"mapId": 397, "regionC": 826, "regionS": 826, "imageNames": 10},
}
ALLOWED_MISSING = {"\\system\\spr\\RegionTileDefault.spr"}


def load(path: Path):
    with path.open(encoding="utf-8-sig") as f:
        return json.load(f)


def count_files(path: Path, suffix: str) -> int:
    if not path.exists():
        return -1
    return sum(1 for p in path.iterdir() if p.name.endswith(suffix))


def main() -> int:
    root = Path(sys.argv[1]) if len(sys.argv) > 1 else Path.cwd()
    errors = []
    warnings = []

    manifest_path = root / "TECHNICAL_MANIFEST.json"
    if not manifest_path.exists():
        errors.append(f"missing {manifest_path}")
        manifest = {}
    else:
        manifest = load(manifest_path)

    sa = root / "Assets" / "StreamingAssets"
    alias_path = sa / "MapAliasCatalog.json"
    geom_path = sa / "MapGeometryCatalog.json"
    server_path = sa / "MapServerRegionCatalog.json"

    for p in [alias_path, geom_path, server_path]:
        if not p.exists():
            errors.append(f"missing {p.relative_to(root) if p.is_absolute() else p}")

    if alias_path.exists():
        aliases = load(alias_path).get("aliases", [])
        ids = sorted(a.get("mapId") for a in aliases)
        if ids != [396, 397]:
            errors.append(f"alias map ids expected [396,397], got {ids}")

    if geom_path.exists():
        geometries = load(geom_path).get("geometries", [])
        keys = sorted(g.get("geometryKey") for g in geometries)
        if keys != sorted(EXPECTED):
            errors.append(f"geometry keys mismatch: {keys}")

    for key, exp in EXPECTED.items():
        region_dir = sa / "Generated" / "MapRegions" / key
        server_dir = sa / "Generated" / "MapServerRegions" / key
        c = count_files(region_dir, "_Region_C.dat")
        s = count_files(server_dir, "_Region_S.dat")
        if c != exp["regionC"]:
            errors.append(f"{key} Region_C expected {exp['regionC']}, got {c}")
        if s != exp["regionS"]:
            errors.append(f"{key} Region_S expected {exp['regionS']}, got {s}")
        image_path = region_dir / "image_names.json"
        if image_path.exists():
            n = len(load(image_path))
            if n != exp["imageNames"]:
                errors.append(f"{key} image_names expected {exp['imageNames']}, got {n}")
        else:
            errors.append(f"missing {image_path.relative_to(root)}")

    missing = manifest.get("missingSprites", []) if isinstance(manifest, dict) else []
    unexpected_missing = [m for m in missing if m.get("ref") not in ALLOWED_MISSING]
    if unexpected_missing:
        errors.append(f"unexpected missing sprites: {unexpected_missing}")
    elif missing:
        warnings.append(f"known missing sprites accepted: {[m.get('ref') for m in missing]}")

    sprite_dir = sa / "Generated" / "MapSprites"
    spr_count = count_files(sprite_dir, ".spr")
    if spr_count < 75:
        errors.append(f"expected at least 75 copied .spr files, got {spr_count}")

    result = {
        "status": "PASS" if not errors else "FAIL",
        "errors": errors,
        "warnings": warnings,
        "facts": {
            "root": str(root),
            "spriteFiles": spr_count,
            "maps": EXPECTED,
        },
    }
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return 0 if not errors else 1


if __name__ == "__main__":
    raise SystemExit(main())
