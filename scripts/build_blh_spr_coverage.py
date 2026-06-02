#!/usr/bin/env python3
from __future__ import annotations

import json
import shutil
from pathlib import Path


REPO = Path(__file__).resolve().parents[1]
# NOTE: jxwin-kinnox directory removed. If re-running, copy Region_S.dat
# files to Assets/StreamingAssets/TestData/Regions/ first.
PC_BLH_ROOT = (
    REPO
    / "Assets/StreamingAssets/TestData/Regions"
)
OUT_ROOT = REPO / "Assets/StreamingAssets/TestData/Regions/Map_79"
OUT_REPORT = REPO / "Assets/StreamingAssets/TestData/Regions/Map_79_coverage.json"

MAP_ID = 79
RECT_LEFT = 94
RECT_TOP = 91
RECT_RIGHT = 118
RECT_BOTTOM = 103


def main() -> int:
    OUT_ROOT.mkdir(parents=True, exist_ok=True)

    copied: list[dict[str, object]] = []
    missing: list[str] = []

    for y in range(RECT_TOP, RECT_BOTTOM + 1):
        row_dir = PC_BLH_ROOT / f"v_{y:03d}"
        for x in range(RECT_LEFT, RECT_RIGHT + 1):
            src = row_dir / f"{x:03d}_Region_S.dat"
            dst = OUT_ROOT / f"{x:03d}_{y:03d}_Region_S.dat"
            if src.exists():
                shutil.copy2(src, dst)
                copied.append(
                    {
                        "x": x,
                        "y": y,
                        "source": str(src.relative_to(REPO)),
                        "output": str(dst.relative_to(REPO)),
                        "bytes": dst.stat().st_size,
                    }
                )
            else:
                missing.append(str(src.relative_to(REPO)))

    report = {
        "version": 1,
        "mapId": MAP_ID,
        "displayName": "巴陵县",
        "displayNameVi": "Ba Lăng Huyện",
        "rect": {
            "left": RECT_LEFT,
            "top": RECT_TOP,
            "right": RECT_RIGHT,
            "bottom": RECT_BOTTOM,
        },
        "expectedRegions": (RECT_RIGHT - RECT_LEFT + 1) * (RECT_BOTTOM - RECT_TOP + 1),
        "copiedRegions": len(copied),
        "missingRegions": len(missing),
        "totalBytes": sum(int(r["bytes"]) for r in copied),
        "regions": copied,
        "missing": missing,
        "note": "Server Region_S fixtures for Ba Lăng Huyện default sandbox map.",
    }

    OUT_REPORT.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n")
    print(
        f"BLH map {MAP_ID}: copied {len(copied)} regions, "
        f"missing {len(missing)}, report={OUT_REPORT}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
