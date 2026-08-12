#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any

try:
    from .builder import MAP_ID, MAP_NAME_VI, canonical_bytes, sha256
except ImportError:  # direct script execution
    from builder import MAP_ID, MAP_NAME_VI, canonical_bytes, sha256

REQUIRED = {
    "artifact": "map-runtime.v1.json",
    "provenance": "map-runtime.v1.provenance.json",
    "signature": "map-runtime.v1.signature.json",
    "catalog": "map-runtime.catalog.v1.json",
}
FORBIDDEN_TEXT = ("TestData", "Map_79", "mapId=79", "/var/", "C:\\")


class VerifyError(RuntimeError):
    pass


def load(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8"))


def assert_false(value: Any, label: str) -> None:
    if value is not False:
        raise VerifyError(f"{label} must be false")


def verify_dir(runtime_dir: Path, require_production_signature: bool = False) -> dict[str, Any]:
    paths = {key: runtime_dir / name for key, name in REQUIRED.items()}
    missing = [str(p) for p in paths.values() if not p.is_file()]
    if missing:
        raise VerifyError("missing map-runtime files: " + ", ".join(missing))

    artifact = load(paths["artifact"])
    provenance = load(paths["provenance"])
    signature = load(paths["signature"])
    catalog = load(paths["catalog"])

    if artifact.get("schema") != "map-runtime.v1" or catalog.get("schema") != "map-runtime.catalog.v1":
        raise VerifyError("schema mismatch")
    if artifact.get("mapId") != MAP_ID or artifact.get("canonicalIdentity", {}).get("mapId") != MAP_ID:
        raise VerifyError("only direct mapId 53 accepted")
    if artifact.get("canonicalIdentity", {}).get("nameVi") != MAP_NAME_VI:
        raise VerifyError("map name drift")
    if 79 in artifact.get("movement", {}).get("rules", {}).get("allowMapIds", []):
        raise VerifyError("map 79 alias/remap accepted")

    text = paths["artifact"].read_text(encoding="utf-8") + paths["catalog"].read_text(encoding="utf-8")
    for token in FORBIDDEN_TEXT:
        if token in text:
            raise VerifyError(f"forbidden runtime fallback token: {token}")

    movement_rules = artifact.get("movement", {}).get("rules", {})
    for key in ("filesystemFallbackAllowed", "testDataAllowed", "loosePcFolderFallbackAllowed", "aliasRemapAllowed", "absoluteRuntimePathsAllowed"):
        assert_false(movement_rules.get(key), f"movement.rules.{key}")
    security = catalog.get("security", {})
    for key in ("filesystemFallbackAllowed", "testDataAllowed", "aliasRemapAllowed", "map79Allowed"):
        assert_false(security.get(key), f"catalog.security.{key}")

    artifact_sha = sha256(paths["artifact"].read_bytes())
    provenance_sha = sha256(paths["provenance"].read_bytes())
    signature_sha = sha256(paths["signature"].read_bytes())
    if signature.get("artifactSha256") != artifact_sha:
        raise VerifyError("signature artifact digest mismatch")
    if artifact.get("sourceProvenanceSha256") != provenance_sha:
        raise VerifyError("artifact provenance digest mismatch")

    by_logical = {a["logicalPath"]: a for a in catalog.get("artifacts", [])}
    expected = {
        "map-runtime.v1.json": artifact_sha,
        "map-runtime.v1.provenance.json": provenance_sha,
        "map-runtime.v1.signature.json": signature_sha,
    }
    for logical, digest in expected.items():
        if by_logical.get(logical, {}).get("sha256") != digest:
            raise VerifyError(f"catalog digest mismatch: {logical}")

    if provenance.get("schema") != "map-runtime.provenance.v1" or not provenance.get("sources"):
        raise VerifyError("missing provenance")
    policy = provenance.get("extraction", {})
    for key in ("aliasRemapAllowed", "map79Allowed", "testDataAllowed", "loosePcFolderFallbackAllowed", "absoluteRuntimePathsAllowed", "filesystemFallbackAllowed"):
        assert_false(policy.get(key), f"provenance.extraction.{key}")

    bounds = artifact["bounds"]["world"]
    spawn = artifact["spawn"]["world"]
    if not (bounds["x"] <= spawn["x"] <= bounds["x"] + bounds["width"] and bounds["y"] <= spawn["y"] <= bounds["y"] + bounds["height"]):
        raise VerifyError("spawn outside bounds")
    if artifact["spawn"]["regionCell"] not in artifact["walkability"]["walkableRegionCells"]:
        raise VerifyError("spawn not walkable")
    walk = dict(artifact["walkability"])
    walk_sha = walk.pop("sha256", None)
    if walk_sha != sha256(canonical_bytes(walk)):
        raise VerifyError("walkability digest mismatch")

    verification = signature.get("verification", {})
    if verification.get("productionSignatureVerified") is not False or verification.get("status") != "fail_closed_no_production_key":
        raise VerifyError("signature status must fail closed without production key")
    if require_production_signature:
        raise VerifyError("production map-runtime signature unavailable")

    return {
        "status": "verified_unsigned_fail_closed",
        "artifactSha256": artifact_sha,
        "provenanceSha256": provenance_sha,
        "signatureSha256": signature_sha,
        "productionSignatureVerified": False,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Verify canonical map-runtime.v1 files.")
    parser.add_argument("--runtime-dir", type=Path, default=Path("Assets/StreamingAssets/MapRuntime"))
    parser.add_argument("--require-production-signature", action="store_true")
    parser.add_argument("--pretty", action="store_true")
    args = parser.parse_args()
    try:
        result = verify_dir(args.runtime_dir, args.require_production_signature)
    except VerifyError as ex:
        print(json.dumps({"status": "fail", "error": str(ex)}, ensure_ascii=False, sort_keys=True))
        return 2
    print(json.dumps(result, ensure_ascii=False, indent=2 if args.pretty else None, sort_keys=not args.pretty))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
