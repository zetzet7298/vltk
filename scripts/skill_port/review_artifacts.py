#!/usr/bin/env python3
"""Generate deterministic SkillPort tuple/review evidence without claiming parity."""

from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any

from scripts.skill_port.compiler import canonical_json, sha256_bytes


SCHEMA = "vltk.skill-port.visual-tuple-matrix/v1"
SUMMARY_SCHEMA = "vltk.skill-port.review-summary/v1"
DEFAULT_CONTENT = Path("Assets/StreamingAssets/Generated/SkillPort")
DEFAULT_OUT = Path("harness/specs/jx-pc-mobile-port/delivery/review-artifacts")

GENDERS = ("male", "female")
MOUNT_VISUAL_IDS = (0, 1, 3, 5, 7, 9)
WEAPONS: tuple[tuple[str, str, int], ...] = (
    ("empty_hand", "empty", 0),
    ("short_weapon", "equipped", 1),
    ("long_weapon", "equipped", 10),
    ("dual_weapon", "equipped", 13),
    ("hidden_weapon", "hidden", 0),
)


def _load_json(path: Path) -> dict[str, Any]:
    value = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(value, dict):
        raise ValueError(f"{path} must contain an object")
    return value


def _artifact(manifest: dict[str, Any], logical_path: str) -> dict[str, Any]:
    matches = [item for item in manifest.get("artifacts", []) if item.get("logicalPath") == logical_path]
    if len(matches) != 1:
        raise ValueError(f"manifest must contain exactly one {logical_path} artifact")
    return matches[0]


def _canonical_tuple(mounted: bool, mount_visual_id: int, visibility: str, weapon_visual_id: int) -> bool:
    if mounted != (mount_visual_id > 0):
        return False
    if visibility == "equipped":
        return weapon_visual_id > 0
    if visibility in {"empty", "hidden"}:
        return weapon_visual_id == 0
    return False


def build_review_artifacts(content_dir: Path = DEFAULT_CONTENT) -> dict[str, bytes]:
    client_path = content_dir / "skill_port.client.json"
    protobuf_path = content_dir / "skill_port.client.pb"
    manifest_path = content_dir / "manifest.json"
    client_bytes = client_path.read_bytes()
    protobuf_bytes = protobuf_path.read_bytes()
    manifest_bytes = manifest_path.read_bytes()
    client = json.loads(client_bytes.decode("utf-8"))
    manifest = json.loads(manifest_bytes.decode("utf-8"))

    if client.get("schema") != "vltk.skill_port.client_projection/v1":
        raise ValueError("client projection schema mismatch")
    rows = client.get("rows")
    if not isinstance(rows, list) or len(rows) != 242:
        raise ValueError("client projection must contain exactly 242 rows")
    ids = [row.get("skill_id") for row in rows]
    if any(not isinstance(skill_id, int) or skill_id <= 0 for skill_id in ids) or len(set(ids)) != 242:
        raise ValueError("client projection must contain 242 unique positive skill ids")

    client_artifact = _artifact(manifest, "skill_port.client.json")
    protobuf_artifact = _artifact(manifest, "skill_port.client.pb")
    client_sha = sha256_bytes(client_bytes)
    protobuf_sha = sha256_bytes(protobuf_bytes)
    if client_artifact.get("sha256") != client_sha or client_artifact.get("sizeBytes") != len(client_bytes):
        raise ValueError("client json artifact hash drift")
    if protobuf_artifact.get("sha256") != protobuf_sha or protobuf_artifact.get("sizeBytes") != len(protobuf_bytes):
        raise ValueError("client protobuf artifact hash drift")
    if manifest.get("contentDigest", {}).get("clientProjectionSha256") != protobuf_sha:
        raise ValueError("manifest client projection digest drift")

    sorted_rows = sorted(rows, key=lambda row: row["skill_id"])
    skill_blockers: list[dict[str, Any]] = []
    cases: list[dict[str, Any]] = []
    blocked_skill_ids: set[int] = set()
    for row in sorted_rows:
        skill_id = row["skill_id"]
        blockers = sorted(set(row.get("blockers") or []))
        blocked = bool(blockers) or row.get("exposure_state") != "exposed"
        if blocked:
            blocked_skill_ids.add(skill_id)
        skill_blockers.append(
            {
                "skill_id": skill_id,
                "skill_name": row.get("skill_name", ""),
                "exposure_state": row.get("exposure_state", "unspecified"),
                "blocked": blocked,
                "blockers": blockers,
                "factions": sorted({f.get("key", "") for f in row.get("factions", []) if f.get("key")}),
            }
        )

        for gender in GENDERS:
            for mount_visual_id in MOUNT_VISUAL_IDS:
                mounted = mount_visual_id > 0
                for weapon_name, visibility, weapon_visual_id in WEAPONS:
                    if not _canonical_tuple(mounted, mount_visual_id, visibility, weapon_visual_id):
                        raise ValueError("generator produced a non-canonical tuple")
                    cases.append(
                        {
                            "case_id": f"SKL-{skill_id}-{gender}-m{mount_visual_id}-{weapon_name}",
                            "skill_id": skill_id,
                            "gender": gender,
                            "mounted": mounted,
                            "mount_visual_id": mount_visual_id,
                            "weapon": weapon_name,
                            "weapon_visibility": visibility,
                            "weapon_visual_id": weapon_visual_id,
                            "blocked": blocked,
                            "blocker_ref": skill_id,
                        }
                    )

    cases_per_skill = len(GENDERS) * len(MOUNT_VISUAL_IDS) * len(WEAPONS)
    expected_cases = len(sorted_rows) * cases_per_skill
    if len(cases) != expected_cases:
        raise ValueError("tuple matrix expansion count mismatch")

    matrix = {
        "schema": SCHEMA,
        "generator": "scripts.skill_port.review_artifacts/v1",
        "source": {
            "client_json_sha256": client_sha,
            "client_protobuf_sha256": protobuf_sha,
            "manifest_file_sha256": sha256_bytes(manifest_bytes),
            "signing_key_id": manifest.get("signingKeyId", ""),
        },
        "dimensions": {
            "genders": list(GENDERS),
            "mount_visual_ids": list(MOUNT_VISUAL_IDS),
            "weapons": [name for name, _, _ in WEAPONS],
        },
        "counts": {
            "skills": len(sorted_rows),
            "cases_per_skill": cases_per_skill,
            "tuple_cases": len(cases),
            "blocked_skills": len(blocked_skill_ids),
            "review_ready_skills": len(sorted_rows) - len(blocked_skill_ids),
        },
        "skills": skill_blockers,
        "cases": cases,
    }
    summary = {
        "schema": SUMMARY_SCHEMA,
        "matrix_sha256": sha256_bytes(canonical_json(matrix)),
        "catalog_union_size": len(sorted_rows),
        "tuple_cases": len(cases),
        "blocked_tuple_cases": sum(1 for case in cases if case["blocked"]),
        "review_ready_tuple_cases": sum(1 for case in cases if not case["blocked"]),
        "production_signing_ready": not str(manifest.get("signingKeyId", "")).startswith("test-only-"),
        "pc_golden_evidence": "BLOCKED",
        "android_physical_evidence": "BLOCKED",
        "parity_done": False,
    }
    return {
        "skill-port-tuple-matrix.json": canonical_json(matrix),
        "skill-port-review-summary.json": canonical_json(summary),
    }


def write_or_check(content_dir: Path, out_dir: Path, check: bool) -> None:
    artifacts = build_review_artifacts(content_dir)
    if check:
        for name, expected in artifacts.items():
            path = out_dir / name
            if not path.is_file() or path.read_bytes() != expected:
                raise ValueError(f"review artifact drift: {path}")
        return
    out_dir.mkdir(parents=True, exist_ok=True)
    for name, data in artifacts.items():
        (out_dir / name).write_bytes(data)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--content", type=Path, default=DEFAULT_CONTENT)
    parser.add_argument("--out", type=Path, default=DEFAULT_OUT)
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()
    write_or_check(args.content, args.out, args.check)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
