#!/usr/bin/env python3
"""Build checked static learned-membership proof for TianWang from pinned PC evidence."""
from __future__ import annotations

import argparse
import csv
import hashlib
import json
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CANONICAL_ROOT = Path("/var/www/jx-pc")
VLTKTOOL = Path("/home/zet/Projects/vltktool/extract_table_slice.py")
PROGRESSION_REL = "01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/script/global/skills_table.lua"
SKILLBOOK_REL = "01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/script/item/skillbook.lua"
SKILLS_REL = "pak_unpacked/slistcache/settings/skills.txt"
PROGRESSION_SHA = "7e46896c4d5c3fc33cf3b1119ec3e6cf7b1a2c8d7a64ab25d2087331646642b3"
SKILLBOOK_SHA = "4e5361a6d2756f3596fcc86155dd579b8bf15f69c73651d7f9e8c40f3337d0d9"
SKILLS_SHA = "c77892fb33b6e63783c554bd075caa4891d9b9ec8abb70084582a5c24156e40c"
MEMBERSHIP_SCHEMA = "vltk.tianwang.membership-classification/v1"
PROOF_SCHEMA = "vltk.tianwang.static-catalog-proof/v1"
RELATIONSHIP_FIELDS = ("ChildSkillId", "StartSkillId", "FlySkillId", "CollidSkillId", "VanishedSkillId")


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def dump(data: object) -> bytes:
    return (json.dumps(data, ensure_ascii=False, indent=2, sort_keys=True) + "\n").encode()


def integer(value: str) -> int:
    value = value.strip()
    return int(value) if value and value.lstrip("-").isdigit() else 0


def progression_by_tier(path: Path) -> tuple[dict[int, list[int]], dict[int, int]]:
    lines = path.read_bytes().splitlines()
    start = next(i for i, line in enumerate(lines) if re.match(rb"^\s*tianwang\s*=\s*\{\s*$", line))
    end = next(i for i in range(start + 1, len(lines)) if re.match(rb"^\s*[A-Za-z_][A-Za-z0-9_]*\s*=\s*\{\s*$", lines[i]))
    tiers, line_by_tier = {}, {}
    for i, line in enumerate(lines[start + 1:end], start + 2):
        match = re.match(rb"^\s*\[(\d+)\]\s*=\s*\{([^}]*)\}", line)
        if match:
            tier = int(match.group(1))
            tiers[tier] = [int(value) for value in re.findall(rb"\d+", match.group(2))]
            line_by_tier[tier] = i
    return tiers, line_by_tier


def skillbook_by_level(path: Path) -> tuple[dict[int, list[int]], int]:
    lines = path.read_bytes().splitlines()
    pattern = rb"^\s*\[1\]\s*=\s*\{(.*)\}\s*,?\s*$"
    index, row = next((i + 1, match) for i, line in enumerate(lines) if (match := re.match(pattern, line)))
    return {int(level): [int(value) for value in re.findall(rb"\d+", ids)] for level, ids in re.findall(rb"\[(\d+)\]\s*=\s*\{([^}]*)\}", row.group(1))}, index


def display_ids(panel: Path) -> list[int]:
    match = re.search(r"public static readonly int\[\] PcTianWangSkillOrder\s*=\s*\{(.*?)\};", panel.read_text(encoding="utf-8"), re.S)
    if not match:
        raise SystemExit("missing PcTianWangSkillOrder")
    return [int(value) for value in re.findall(r"\b\d+\b", re.sub(r"//.*", "", match.group(1)))]


def rows(path: Path) -> dict[int, dict[str, str]]:
    return {integer(row["SkillId"]): row for row in csv.DictReader(path.read_bytes().decode("latin-1").splitlines(), delimiter="\t")}


def relationships(table: dict[int, dict[str, str]], learned: set[int]) -> tuple[list[int], dict[int, dict[str, int]]]:
    by_skill = {}
    for skill_id in sorted(learned):
        found = {field: integer(table[skill_id].get(field, "")) for field in RELATIONSHIP_FIELDS}
        found = {field: target for field, target in found.items() if target}
        if found:
            by_skill[skill_id] = found
    return sorted({target for found in by_skill.values() for target in found.values()}), by_skill


def run_slice(ids: list[int], output: Path, manifest: Path, check: bool) -> None:
    command = [sys.executable, str(VLTKTOOL), "--input", str(CANONICAL_ROOT / SKILLS_REL), "--key-column", "SkillId", "--ids", ",".join(map(str, ids)), "--output", str(output), "--manifest", str(manifest)]
    if check:
        command.append("--check")
    result = subprocess.run(command, capture_output=True, text=True)
    if result.returncode:
        raise SystemExit(result.stderr or result.stdout)


def verify_manifest(path: Path, ids: list[int], source: Path) -> None:
    data = json.loads(path.read_text())
    if (data.get("schema") != "vltk.table-slice-provenance/v1" or data.get("source", {}).get("sha256") != SKILLS_SHA
            or data.get("requested_ids") != ids or data.get("selected_ids") != ids
            or data.get("slice", {}).get("sha256") != digest(source.read_bytes())):
        raise SystemExit("slice provenance drift")


def verify_lua(canonical_root: Path) -> None:
    for relative, expected in ((PROGRESSION_REL, PROGRESSION_SHA), (SKILLBOOK_REL, SKILLBOOK_SHA)):
        path = canonical_root / relative
        if not path.is_file() or digest(path.read_bytes()) != expected:
            raise SystemExit(f"canonical source hash drift: {path}")


def build(source: Path, provenance: Path, target_source: Path, target_provenance: Path,
          canonical_root: Path = CANONICAL_ROOT) -> tuple[bytes, bytes]:
    verify_lua(canonical_root)
    tiers, tier_lines = progression_by_tier(canonical_root / PROGRESSION_REL)
    skillbooks, skillbook_line = skillbook_by_level(canonical_root / SKILLBOOK_REL)
    progression = {skill_id for ids in tiers.values() for skill_id in ids}
    grants = {skill_id for ids in skillbooks.values() for skill_id in ids}
    learned = progression | grants
    display = set(display_ids(ROOT / "Assets/Scripts/UI/PcSkillPanelService.cs"))
    union = sorted(learned | display)
    if len(learned) != 23 or len(display) != 15 or len(union) != 23:
        raise SystemExit("unexpected TianWang membership counts")
    verify_manifest(provenance, union, source)
    table = rows(source)
    if set(table) != set(union):
        raise SystemExit("TianWang union slice rows drift")
    targets, by_skill = relationships(table, learned)
    if len(targets) != 16:
        raise SystemExit("unexpected TianWang relationship closure")
    run_slice(targets, target_source, target_provenance, check=True)
    verify_manifest(target_provenance, targets, target_source)
    if set(rows(target_source)) != set(targets):
        raise SystemExit("TianWang relationship target slice rows drift")

    evidence = {}
    for tier, ids in tiers.items():
        for skill_id in ids:
            evidence.setdefault(skill_id, []).append(f"progression_tier_{tier}")
    for level, ids in skillbooks.items():
        for skill_id in ids:
            evidence.setdefault(skill_id, []).append(f"skillbook_{level}")
    shared = sorted(learned & display)
    learned_only = sorted(learned - display)
    unity_only = sorted(display - learned)
    membership = {
        "schema": MEMBERSHIP_SCHEMA, "status": "reviewed_static_membership_evidence",
        "canonical_sources": {
            "regular_progression": {"path": str(canonical_root / PROGRESSION_REL), "line_start": min(tier_lines.values()), "line_end": max(tier_lines.values()), "sha256": PROGRESSION_SHA},
            "skillbook_grants": {"path": str(canonical_root / SKILLBOOK_REL), "line": skillbook_line, "sha256": SKILLBOOK_SHA},
            "static_rows": {"path": str(canonical_root / SKILLS_REL), "hash_status": "verified_vltktool_provenance", "source_sha256": SKILLS_SHA, "slice_path": str(source.relative_to(ROOT)), "slice_sha256": digest(source.read_bytes()), "slice_bytes": len(source.read_bytes()), "provenance_path": str(provenance.relative_to(ROOT))},
        },
        "regular_progression_ids": [skill_id for tier in sorted(tiers) for skill_id in tiers[tier]],
        "skillbook_by_level": {str(level): ids for level, ids in skillbooks.items()},
        "pc_learned_evidence_ids": sorted(learned), "unity_display_ids": display_ids(ROOT / "Assets/Scripts/UI/PcSkillPanelService.cs"), "shared_ids": shared,
        "pc_learned_only": [{"skill_id": skill_id, "membership": "pc_learned", "evidence": evidence[skill_id], "oracle_include": True} for skill_id in learned_only],
        "unity_only_unresolved": [{"skill_id": skill_id, "membership": "unresolved", "relationship_target_of": sorted(skill_id for skill_id, found in by_skill.items() if skill_id in found.values()), "oracle_include": False, "reason": "No direct PC progression or skillbook grant evidence found"} for skill_id in unity_only],
        "ui_contract": {"ordered_skill_ids": None, "reason": "No canonical UI ordering evidence; static learned proof must not infer order"},
    }
    membership_bytes = dump(membership)
    membership_path = ROOT / "harness/docs/stories/SKL-TW-PROOF-001/membership-classification.json"
    proof = {
        "schema": PROOF_SCHEMA, "membership_source": str(membership_path.relative_to(ROOT)), "membership_sha256": digest(membership_bytes),
        "slice_source": str(source.relative_to(ROOT)), "slice_sha256": digest(source.read_bytes()),
        "provenance_source": str(provenance.relative_to(ROOT)), "provenance_sha256": digest(provenance.read_bytes()),
        "relationship_target_slice_source": str(target_source.relative_to(ROOT)), "relationship_target_slice_sha256": digest(target_source.read_bytes()),
        "relationship_target_provenance_source": str(target_provenance.relative_to(ROOT)), "relationship_target_provenance_sha256": digest(target_provenance.read_bytes()),
        "pc_learned_skill_ids": sorted(learned), "observed_unity_display_ids": sorted(display), "shared_ids": shared,
        "pc_learned_only_ids": learned_only, "unity_only_unresolved_ids": unity_only,
        "learned_relationship_target_ids": targets, "relationship_self_reference_learned_ids": sorted(set(targets) & learned),
        "unity_only_relationship_target_ids": sorted(set(unity_only) & set(targets)), "ui_order": None,
        "canonical_sources": {
            "static_rows": {"path": str(canonical_root / SKILLS_REL), "sha256": SKILLS_SHA, "provides": "canonical static skill rows (parsed only via exact-byte vltktool slices)"},
            "progression": {"path": str(canonical_root / PROGRESSION_REL), "sha256": PROGRESSION_SHA, "provides": "PC active-category progression membership"},
            "skillbook": {"path": str(canonical_root / SKILLBOOK_REL), "sha256": SKILLBOOK_SHA, "provides": "level 90/120/150 skillbook grants"},
        },
    }
    return membership_bytes, dump(proof)


def main() -> int:
    parser = argparse.ArgumentParser()
    story = ROOT / "harness/docs/stories/SKL-TW-PROOF-001"
    parser.add_argument("--source", type=Path, default=story / "PcTianWangSkills.txt")
    parser.add_argument("--provenance", type=Path, default=story / "PcTianWangSkills.provenance.json")
    parser.add_argument("--target-source", type=Path, default=story / "PcTianWangRelationshipTargets.txt")
    parser.add_argument("--target-provenance", type=Path, default=story / "PcTianWangRelationshipTargets.provenance.json")
    parser.add_argument("--membership", type=Path, default=story / "membership-classification.json")
    parser.add_argument("--proof", type=Path, default=story / "static-catalog-proof.json")
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()
    union = sorted({23, 24, 26, 29, 30, 31, 32, 33, 34, 35, 36, 37, 40, 41, 42, 322, 323, 324, 325, 708, 1058, 1059, 1060})
    run_slice(union, args.source, args.provenance, check=True)
    membership, proof = build(args.source, args.provenance, args.target_source, args.target_provenance)
    if args.check:
        if not args.membership.is_file() or not args.proof.is_file() or args.membership.read_bytes() != membership or args.proof.read_bytes() != proof:
            raise SystemExit("TianWang proof artifact drift")
    else:
        args.membership.write_bytes(membership)
        args.proof.write_bytes(proof)
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
