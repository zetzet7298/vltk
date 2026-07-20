#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
from typing import Any

REPO = Path(__file__).resolve().parents[2]
DEFAULT_OUT = REPO / "Assets/StreamingAssets/MapRuntime"
MAP_ID = 53
MAP_NAME_VI = "Ba Lăng huyện"
GEOMETRY_KEY = "g_1bbe240c72569d69"
SCHEMA = "map-runtime.v1"
CATALOG_SCHEMA = "map-runtime.catalog.v1"
PROVENANCE_SCHEMA = "map-runtime.provenance.v1"
SIGNATURE_SCHEMA = "map-runtime.signature.v1"

SOURCE_PATHS = {
    "maplist": "Assets/StreamingAssets/Reference/PcMap/maplist.ini",
    "revivepos": "Assets/StreamingAssets/Reference/PcMap/revivepos.ini",
    "aliasCatalog": "Assets/StreamingAssets/MapAliasCatalog.json",
    "geometryCatalog": "Assets/StreamingAssets/MapGeometryCatalog.json",
    "serverRegionCatalog": "Assets/StreamingAssets/MapServerRegionCatalog.json",
    "visualRegionManifest": f"Assets/StreamingAssets/Generated/MapRegions/{GEOMETRY_KEY}/manifest.json",
    "serverRegionManifest": f"Assets/StreamingAssets/Generated/MapServerRegions/{GEOMETRY_KEY}/manifest.json",
}


def canonical_bytes(obj: Any) -> bytes:
    return (json.dumps(obj, ensure_ascii=False, sort_keys=True, separators=(",", ":")) + "\n").encode("utf-8")


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def read_json(repo: Path, rel: str) -> Any:
    return json.loads((repo / rel).read_text(encoding="utf-8"))


def file_sha(repo: Path, rel: str) -> str:
    return sha256((repo / rel).read_bytes())


def find_one(rows: list[dict[str, Any]], pred, label: str) -> dict[str, Any]:
    found = [row for row in rows if pred(row)]
    if len(found) != 1:
        raise SystemExit(f"expected exactly one {label}, got {len(found)}")
    return found[0]


def raw_line_info(repo: Path, rel: str, prefixes: tuple[bytes, ...]) -> list[dict[str, Any]]:
    out = []
    for idx, raw in enumerate((repo / rel).read_bytes().splitlines(), 1):
        if raw.startswith(prefixes):
            out.append({"line": idx, "sha256": sha256(raw), "bytesHex": raw.hex()})
    return out


def walkability_from_regions(regions: list[dict[str, Any]], min_col: int, min_row: int, max_col: int, max_row: int) -> dict[str, Any]:
    walkable = sorted([[int(r["col"]), int(r["row"])] for r in regions if r.get("hasGround")])
    walkable_set = {(c, r) for c, r in walkable}
    blocked = [[c, r] for c in range(min_col, max_col + 1) for r in range(min_row, max_row + 1) if (c, r) not in walkable_set]
    payload = {
        "representation": "region-cell-occupancy.v1",
        "resolutionWorldUnits": {"x": 512, "y": 512},
        "rule": "walkable iff canonical Region_C cell exists and has ground section",
        "walkableRegionCells": walkable,
        "blockedRegionCells": blocked,
    }
    payload["sha256"] = sha256(canonical_bytes({k: v for k, v in payload.items() if k != "sha256"}))
    return payload


def build(repo: Path = REPO) -> tuple[dict[str, Any], dict[str, Any], dict[str, Any], dict[str, Any]]:
    alias_catalog = read_json(repo, SOURCE_PATHS["aliasCatalog"])
    geometry_catalog = read_json(repo, SOURCE_PATHS["geometryCatalog"])
    server_catalog = read_json(repo, SOURCE_PATHS["serverRegionCatalog"])
    visual_manifest = read_json(repo, SOURCE_PATHS["visualRegionManifest"])
    server_manifest = read_json(repo, SOURCE_PATHS["serverRegionManifest"])

    alias = find_one(alias_catalog["aliases"], lambda r: int(r.get("mapId", 0)) == MAP_ID, "mapId 53 alias")
    geometry = find_one(geometry_catalog["geometries"], lambda r: r.get("geometryKey") == GEOMETRY_KEY and r.get("mapIds") == [MAP_ID], "mapId 53 geometry")
    server = find_one(server_catalog["geometries"], lambda r: r.get("geometryKey") == GEOMETRY_KEY and r.get("mapIds") == [MAP_ID], "mapId 53 server geometry")

    forbidden_map_ids = set(alias.get("mapIds", [])) | set(geometry.get("mapIds", [])) | set(server.get("mapIds", []))
    if alias.get("primaryMapId") != MAP_ID or geometry.get("primaryMapId") != MAP_ID or server.get("primaryMapId") != MAP_ID or 79 in forbidden_map_ids:
        raise SystemExit("map 53 must be direct canonical identity; alias/remap rejected")
    if alias.get("nameVi") != MAP_NAME_VI or alias.get("geometryKey") != GEOMETRY_KEY:
        raise SystemExit("map 53 alias provenance drift")
    if visual_manifest.get("mapIds") != [MAP_ID] or server_manifest.get("mapIds") != [MAP_ID]:
        raise SystemExit("map 53 generated evidence drift")

    min_col, min_row = int(geometry["minCol"]), int(geometry["minRow"])
    max_col, max_row = int(geometry["maxCol"]), int(geometry["maxRow"])
    bounds = geometry["bounds"]
    walkability = walkability_from_regions(visual_manifest["regions"], min_col, min_row, max_col, max_row)

    # ponytail: geometry-center spawn until production importer supplies verified player-start semantic.
    spawn_col = (min_col + max_col) // 2
    spawn_row = (min_row + max_row) // 2
    spawn = {
        "source": "geometry_center_from_canonical_region_c",
        "sourceStatus": "deterministic_safe_spawn_not_pc_revive_semantic",
        "regionCell": [spawn_col, spawn_row],
        "world": {"x": (spawn_col + 0.5) * 512.0, "y": -((spawn_row + 0.5) * 512.0)},
        "reviveposRaw": {"x": 48032, "y": 117504, "sourcePath": SOURCE_PATHS["revivepos"], "line": 122},
    }
    if [spawn_col, spawn_row] not in walkability["walkableRegionCells"]:
        raise SystemExit("derived spawn is not walkable")

    provenance = {
        "schema": PROVENANCE_SCHEMA,
        "mapId": MAP_ID,
        "sources": [
            {"id": key, "path": rel, "sha256": file_sha(repo, rel)} for key, rel in SOURCE_PATHS.items()
        ],
        "sourceLines": {
            "maplist53": raw_line_info(repo, SOURCE_PATHS["maplist"], (b"53=", b"53_")),
            "revivepos53": raw_line_info(repo, SOURCE_PATHS["revivepos"], (b"53=",)),
        },
        "extraction": {
            "mapIdPolicy": "direct-only",
            "aliasRemapAllowed": False,
            "map79Allowed": False,
            "testDataAllowed": False,
            "loosePcFolderFallbackAllowed": False,
            "absoluteRuntimePathsAllowed": False,
            "filesystemFallbackAllowed": False,
        },
        "knownGap": "Production signing key unavailable; player revivepos semantic not promoted because raw revivepos is outside generated visual bounds.",
    }

    artifact = {
        "schema": SCHEMA,
        "mapId": MAP_ID,
        "canonicalIdentity": {"mapId": MAP_ID, "nameVi": MAP_NAME_VI, "pcMapPath": alias["pcMapPath"], "geometryKey": GEOMETRY_KEY},
        "sourceProvenanceSha256": sha256(canonical_bytes(provenance)),
        "bounds": {
            "world": bounds,
            "region": {"minCol": min_col, "minRow": min_row, "maxCol": max_col, "maxRow": max_row, "countX": int(geometry["regionCountX"]), "countY": int(geometry["regionCountY"]), "count": int(geometry["regionCount"])},
        },
        "spawn": spawn,
        "movement": {
            "coordinateSpace": "MapRenderer screen world pixels",
            "regionSceneWidth": 512,
            "regionSceneHeight": 1024,
            "screenYScale": 0.5,
            "groundCell": 32,
            "rules": {
                "allowMapIds": [MAP_ID],
                "rejectMapIds": [79],
                "requiresWalkableRegionCell": True,
                "filesystemFallbackAllowed": False,
                "testDataAllowed": False,
                "loosePcFolderFallbackAllowed": False,
                "aliasRemapAllowed": False,
                "absoluteRuntimePathsAllowed": False,
            },
        },
        "walkability": walkability,
        "collision": {
            "representation": "coarse-region-cell-collision.v1",
            "blockingRule": "blocked iff region cell is absent from walkableRegionCells",
            "blockedRegionCellsSha256": sha256(canonical_bytes(walkability["blockedRegionCells"])),
            "serverStaticSummary": {"regionSCount": int(server["regionSCount"]), "npcCount": int(server["npcCount"]), "trapCount": int(server["trapCount"]), "objCount": int(server["objCount"]), "rescueScanUsed": bool(server["rescueScanUsed"])},
        },
    }

    artifact_sha = sha256(canonical_bytes(artifact))
    signature = {
        "schema": SIGNATURE_SCHEMA,
        "artifactSha256": artifact_sha,
        "canonicalization": "json.sort_keys.compact.trailing_lf",
        "signingKeyId": None,
        "signature": None,
        "verification": {
            "productionSignatureVerified": False,
            "status": "fail_closed_no_production_key",
            "reason": "repository has no trusted production map-runtime signing key; no private material read or invented",
            "trustedKeyConvention": "matches SkillPort production verifier: reject test-only/missing key; fail closed when keyring absent",
        },
    }
    catalog = {
        "schema": CATALOG_SCHEMA,
        "mapRuntimeVersion": 1,
        "maps": [{"mapId": MAP_ID, "nameVi": MAP_NAME_VI, "artifact": "map-runtime.v1.json", "sha256": artifact_sha}],
        "artifacts": [
            {"logicalPath": "map-runtime.v1.json", "kind": "map", "mediaType": "application/vnd.vltk.map-runtime.v1+json", "sha256": artifact_sha},
            {"logicalPath": "map-runtime.v1.provenance.json", "kind": "provenance", "mediaType": "application/json", "sha256": sha256(canonical_bytes(provenance))},
            {"logicalPath": "map-runtime.v1.signature.json", "kind": "signature", "mediaType": "application/json", "sha256": sha256(canonical_bytes(signature))},
        ],
        "security": {"filesystemFallbackAllowed": False, "testDataAllowed": False, "aliasRemapAllowed": False, "map79Allowed": False, "productionSignatureVerified": False},
    }
    return artifact, provenance, signature, catalog


def write_all(out_dir: Path, repo: Path = REPO) -> dict[str, str]:
    artifact, provenance, signature, catalog = build(repo)
    out_dir.mkdir(parents=True, exist_ok=True)
    files = {
        "map-runtime.v1.json": artifact,
        "map-runtime.v1.provenance.json": provenance,
        "map-runtime.v1.signature.json": signature,
        "map-runtime.catalog.v1.json": catalog,
    }
    hashes = {}
    for name, payload in files.items():
        data = canonical_bytes(payload)
        (out_dir / name).write_bytes(data)
        hashes[name] = sha256(data)
    return hashes


def main() -> int:
    parser = argparse.ArgumentParser(description="Build canonical direct map-runtime.v1 for mapId 53.")
    parser.add_argument("--repo", type=Path, default=REPO)
    parser.add_argument("--out-dir", type=Path, default=DEFAULT_OUT)
    args = parser.parse_args()
    hashes = write_all(args.out_dir, args.repo)
    print(json.dumps({"status": "generated", "outDir": str(args.out_dir), "hashes": hashes}, ensure_ascii=False, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
