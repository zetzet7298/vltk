#!/usr/bin/env python3
"""Generate/check the deterministic TangMen static membership oracle."""

from __future__ import annotations

import argparse
import csv
import hashlib
import json
import re
from pathlib import Path


CANONICAL_ROOT = Path("/var/www/jx-pc")
CANONICAL_FILES = {
    "pak_unpacked/slistcache/settings/skills.txt": "c77892fb33b6e63783c554bd075caa4891d9b9ec8abb70084582a5c24156e40c",
    "01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/script/global/skills_table.lua": "7e46896c4d5c3fc33cf3b1119ec3e6cf7b1a2c8d7a64ab25d2087331646642b3",
    "01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/script/item/skillbook.lua": "4e5361a6d2756f3596fcc86155dd579b8bf15f69c73651d7f9e8c40f3337d0d9",
}

INT_FIELDS = {
    "SkillStyle": "skillStyle",
    "StateSpecialId": "stateSpecialId",
    "IsAura": "isAura",
    "AttackRadius": "attackRadius",
    "MslsGenerate": "missilesGenerate",
    "MslsGenerateData": "missilesGenerateData",
    "MisslesForm": "missileForm",
    "ChildSkillId": "childSkillId",
    "ChildSkillLevel": "childSkillLevel",
    "ChildSkillNum": "childSkillNum",
    "BaseSkill": "baseSkill",
    "CharAnimId": "charAnimId",
    "IsMelee": "isMelee",
    "WaitTime": "waitTime",
    "SkillCostType": "skillCostType",
    "CostValue": "cost",
    "TimePerCast": "timePerCast",
    "IsPhysical": "isPhysical",
    "TargetOnly": "targetOnly",
    "TargetEnemy": "targetEnemy",
    "TargetAlly": "targetAlly",
    "TargetSelf": "targetSelf",
    "TargetObj": "targetObj",
    "ByMissle": "byMissile",
    "IsUseAR": "isUseAttackRating",
    "ReqLevel": "reqLevel",
    "MaxLevel": "maxLevel",
    "EqtLimit": "equipLimit",
    "HorseLimit": "horseLimit",
    "DoHurt": "doHurt",
    "WeaponSkill": "weaponSkill",
    "StartSkillId": "startSkillId",
    "FlySkillId": "flySkillId",
    "FlyEventTime": "flyEventTime",
    "CollidSkillId": "collideSkillId",
    "VanishedSkillId": "vanishSkillId",
}

BOOL_FIELDS = {
    "isAura", "baseSkill", "isMelee", "isPhysical", "targetOnly", "targetEnemy",
    "targetAlly", "targetSelf", "targetObj", "byMissile", "isUseAttackRating",
    "doHurt", "weaponSkill",
}

STRING_FIELDS = {
    "ManCastSnd": "manCastSndPath",
    "FMCastSnd": "fmCastSndPath",
    "LvlSetScript": "lvlSetScript",
    "LevelUpScript": "levelUpScript",
}

# Relationship fields whose nonzero values on a learned row name another skill
# (child summon, projectile launch/fly, collision, vanish). These define the
# closed set of relationship targets the oracle proves but never treats as
# learned/root membership.
RELATIONSHIP_SOURCE_FIELDS = (
    "ChildSkillId",
    "StartSkillId",
    "FlySkillId",
    "CollidSkillId",
    "VanishedSkillId",
)

# Exact frozen closure derived from nonzero relationship fields of the 23
# learned source rows. Drift here means the slice or source changed.
EXPECTED_RELATIONSHIP_TARGET_IDS = [
    35, 37, 38, 67, 96, 106, 116, 127, 149, 151, 152, 153, 155, 157, 159,
    161, 227, 301, 304, 331, 332, 333, 340, 344, 346, 348, 350, 352, 374,
    1097, 1098, 1113,
]


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def load_json(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))


def progression_ids(path: Path) -> list[int]:
    lines = path.read_bytes().splitlines()
    starts = [i for i, line in enumerate(lines) if re.match(rb"^\s*tangmen\s*=\s*\{\s*$", line)]
    if len(starts) != 1:
        raise SystemExit("expected exactly one active TangMen progression block")
    start = starts[0]
    end = next(
        (i for i in range(start + 1, len(lines)) if re.match(rb"^\s*[A-Za-z_][A-Za-z0-9_]*\s*=\s*\{\s*$", lines[i])),
        None,
    )
    if end is None:
        raise SystemExit("unterminated TangMen progression block")
    ids = []
    for line in lines[start + 1:end]:
        if line.lstrip().startswith(b"--"):
            continue
        match = re.match(rb"^\s*\[\d+\]\s*=\s*\{([^}]*)\}", line)
        if match:
            ids.extend(int(value) for value in re.findall(rb"\d+", match.group(1)))
    if not ids or len(ids) != len(set(ids)):
        raise SystemExit("invalid TangMen progression IDs")
    return ids


def skillbook_by_level(path: Path) -> dict[int, list[int]]:
    pattern = rb"^\s*\[2\]\s*=\s*\{(.*)\}\s*,?\s*$"
    matches = [re.match(pattern, line) for line in path.read_bytes().splitlines()]
    matches = [match for match in matches if match]
    if len(matches) != 1:
        raise SystemExit("expected exactly one TangMen skillbook row")
    result = {}
    for level_raw, ids_raw in re.findall(rb"\[(\d+)\]\s*=\s*\{([^}]*)\}", matches[0].group(1)):
        level = int(level_raw)
        ids = [int(value) for value in re.findall(rb"\d+", ids_raw)]
        if not ids or level in result:
            raise SystemExit(f"invalid TangMen skillbook level {level}")
        result[level] = ids
    flat = [skill_id for ids in result.values() for skill_id in ids]
    if set(result) != {90, 120, 150} or len(flat) != len(set(flat)):
        raise SystemExit("invalid TangMen skillbook grants")
    return result


def verify_canonical_sources(canonical_root: Path) -> None:
    for relative, expected in CANONICAL_FILES.items():
        path = canonical_root / relative
        if not path.is_file():
            raise SystemExit(f"missing canonical source: {path}")
        actual = digest(path.read_bytes())
        if actual != expected:
            raise SystemExit(f"canonical source hash drift: {path} ({actual})")


def validate_membership(classification, learned_ids, progression, skillbooks) -> tuple[list[int], list[dict]]:
    if classification.get("schema") != "vltk.tangmen.membership-classification/v1":
        raise SystemExit("unexpected TangMen membership schema")
    if classification.get("pc_learned_evidence_ids") != learned_ids:
        raise SystemExit("membership artifact does not match canonical progression/skillbook union")
    all_skillbook = set(skillbooks[90]) | set(skillbooks[120]) | set(skillbooks[150])
    if classification.get("regular_progression_ids") != sorted(set(progression) - all_skillbook):
        raise SystemExit("membership regular progression drift")
    if classification.get("special_progression_ids") != sorted(set(progression) & all_skillbook):
        raise SystemExit("membership special progression drift")
    if classification.get("skillbook_by_level") != {str(level): ids for level, ids in sorted(skillbooks.items())}:
        raise SystemExit("membership skillbook grants drift")
    unity_ids = classification.get("unity_display_ids")
    if not isinstance(unity_ids, list):
        raise SystemExit("missing observed Unity display IDs")
    unresolved = classification.get("unity_only_unresolved")
    if [item.get("skill_id") for item in unresolved or []] != [51, 55, 57]:
        raise SystemExit("unexpected unresolved Unity-only IDs")
    if any(item.get("oracle_include") is not False for item in unresolved):
        raise SystemExit("unresolved Unity-only ID promoted into oracle")
    if classification.get("ui_contract", {}).get("ordered_skill_ids") is not None:
        raise SystemExit("UI order must remain outside the static membership oracle")
    learned_only = sorted(set(learned_ids) - set(unity_ids))
    if [item.get("skill_id") for item in classification.get("pc_learned_only", [])] != learned_only:
        raise SystemExit("PC learned-only classification drift")
    return unity_ids, unresolved


def integer(value: str) -> int:
    value = value.strip()
    return int(value) if value else 0


def relationship_target_ids(rows: dict[int, dict[str, str]]) -> list[int]:
    """Closed set of nonzero relationship targets named by learned source rows."""
    targets = set()
    for row in rows.values():
        for field in RELATIONSHIP_SOURCE_FIELDS:
            raw = row.get(field, "").strip()
            if raw and int(raw) != 0:
                targets.add(int(raw))
    return sorted(targets)


def read_table_rows(data: bytes) -> list[dict[str, str]]:
    return list(csv.DictReader(data.decode("latin-1").splitlines(), delimiter="\t"))


def build(
    source: Path,
    provenance: Path,
    classification: Path,
    canonical_root: Path,
    target_source: Path,
    target_provenance: Path,
) -> bytes:
    source_bytes = source.read_bytes()
    provenance_bytes = provenance.read_bytes()
    classification_bytes = classification.read_bytes()
    target_bytes = target_source.read_bytes()
    target_provenance_bytes = target_provenance.read_bytes()
    provenance = json.loads(provenance_bytes.decode("utf-8"))
    classification = json.loads(classification_bytes.decode("utf-8"))
    target_prov = json.loads(target_provenance_bytes.decode("utf-8"))

    progression_path = canonical_root / "01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/script/global/skills_table.lua"
    skillbook_path = canonical_root / "01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/script/item/skillbook.lua"
    progression = progression_ids(progression_path)
    skillbooks = skillbook_by_level(skillbook_path)
    learned_ids = sorted(set(progression) | {skill_id for ids in skillbooks.values() for skill_id in ids})
    unity_ids, unresolved = validate_membership(classification, learned_ids, progression, skillbooks)

    skills_sha = CANONICAL_FILES["pak_unpacked/slistcache/settings/skills.txt"]

    if provenance.get("schema") != "vltk.table-slice-provenance/v1":
        raise SystemExit("unexpected vltktool provenance schema")
    if provenance.get("encoding") != "byte-preserving":
        raise SystemExit("TangMen slice provenance is not byte-preserving")
    if provenance.get("source", {}).get("sha256") != skills_sha:
        raise SystemExit("TangMen provenance canonical source hash drift")
    if provenance.get("slice", {}).get("sha256") != digest(source_bytes):
        raise SystemExit("TangMen slice hash drift")
    if provenance.get("slice", {}).get("bytes") != len(source_bytes):
        raise SystemExit("TangMen slice byte count drift")
    if provenance.get("requested_ids") != learned_ids or provenance.get("selected_ids") != learned_ids:
        raise SystemExit("TangMen provenance selected IDs drift")
    source_lines = {item["id"]: item["line"] for item in provenance.get("source_lines", [])}
    if sorted(source_lines) != learned_ids:
        raise SystemExit("TangMen provenance source lines drift")

    # Parse and validate the learned source rows (byte-exact slice membership).
    raw_rows: dict[int, dict[str, str]] = {}
    for row in read_table_rows(source_bytes):
        skill_id = integer(row.get("SkillId", ""))
        if skill_id not in learned_ids:
            raise SystemExit(f"unexpected row in TangMen slice: {skill_id}")
        if skill_id in raw_rows:
            raise SystemExit(f"duplicate row in TangMen slice: {skill_id}")
        raw_rows[skill_id] = row
    missing = sorted(set(learned_ids) - set(raw_rows))
    if missing:
        raise SystemExit(f"missing TangMen learned rows: {missing}")

    # Derive the exact relationship-target closure from the learned rows and
    # prove it against the frozen expectation plus the separate target slice.
    relationship_targets = relationship_target_ids(raw_rows)
    if relationship_targets != EXPECTED_RELATIONSHIP_TARGET_IDS:
        raise SystemExit("TangMen relationship-target closure drift")
    if set(relationship_targets) & set(learned_ids):
        raise SystemExit("TangMen relationship target treated as learned/root")
    if target_prov.get("schema") != "vltk.table-slice-provenance/v1":
        raise SystemExit("unexpected target provenance schema")
    if target_prov.get("encoding") != "byte-preserving":
        raise SystemExit("TangMen target slice provenance is not byte-preserving")
    if target_prov.get("source", {}).get("sha256") != skills_sha:
        raise SystemExit("TangMen target provenance canonical source hash drift")
    if target_prov.get("slice", {}).get("sha256") != digest(target_bytes):
        raise SystemExit("TangMen target slice hash drift")
    if target_prov.get("slice", {}).get("bytes") != len(target_bytes):
        raise SystemExit("TangMen target slice byte count drift")
    if target_prov.get("requested_ids") != relationship_targets or target_prov.get("selected_ids") != relationship_targets:
        raise SystemExit("TangMen target provenance selected IDs drift")
    target_source_lines = {item["id"]: item["line"] for item in target_prov.get("source_lines", [])}
    if sorted(target_source_lines) != relationship_targets:
        raise SystemExit("TangMen target provenance source lines drift")

    target_rows: dict[int, dict[str, str]] = {}
    for row in read_table_rows(target_bytes):
        skill_id = integer(row.get("SkillId", ""))
        if skill_id not in relationship_targets:
            raise SystemExit(f"unexpected row in TangMen target slice: {skill_id}")
        if skill_id in target_rows:
            raise SystemExit(f"duplicate row in TangMen target slice: {skill_id}")
        target_rows[skill_id] = row
    target_missing = sorted(set(relationship_targets) - set(target_rows))
    if target_missing:
        raise SystemExit(f"missing TangMen target rows: {target_missing}")

    progression_set = set(progression)
    skillbook_levels = {skill_id: level for level, ids in skillbooks.items() for skill_id in ids}
    selected = {}
    for skill_id in learned_ids:
        row = raw_rows[skill_id]
        item = {
            "skillId": skill_id,
            "sourceLine": source_lines[skill_id],
            "membershipEvidence": (["progression"] if skill_id in progression_set else [])
            + ([f"skillbook_{skillbook_levels[skill_id]}"] if skill_id in skillbook_levels else []),
        }
        present = []
        for source_name, target in INT_FIELDS.items():
            raw = row.get(source_name, "").strip()
            if raw:
                value = int(raw)
                item[target] = (1 if value != 0 else 0) if target in BOOL_FIELDS else value
                present.append(target)
        # Explicit presence semantics for strings: a field appears only when its
        # source cell is nonempty, and every emitted field is named in `present`.
        # Absent cells never materialize as default "" values.
        for source_name, target in STRING_FIELDS.items():
            raw = row.get(source_name, "").strip()
            if raw:
                item[target] = raw
                present.append(target)
        item["present"] = sorted(present)
        selected[skill_id] = item

    static_source = classification.get("canonical_sources", {}).get("static_rows", {})
    if static_source.get("slice_sha256") != digest(source_bytes):
        raise SystemExit("membership artifact slice hash drift")
    progression_source = classification["canonical_sources"]["regular_progression"]
    skillbook_source = classification["canonical_sources"]["skillbook_grants"]
    if progression_source.get("sha256") != CANONICAL_FILES["01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/script/global/skills_table.lua"]:
        raise SystemExit("membership progression hash drift")
    if skillbook_source.get("sha256") != CANONICAL_FILES["01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/script/item/skillbook.lua"]:
        raise SystemExit("membership skillbook hash drift")

    payload = {
        "schema": "vltk.tangmen.static-oracle/v1",
        "canonicalSources": [
            {"path": str(canonical_root / relative), "sha256": sha256, "provides": provides}
            for (relative, sha256), provides in zip(
                CANONICAL_FILES.items(),
                ("static rows", "progression membership", "90/120/150 skillbook grants"),
            )
        ],
        "sliceSource": "Assets/StreamingAssets/Reference/PcTangMenSkills.txt",
        "sliceSha256": digest(source_bytes),
        "provenanceSource": "Assets/StreamingAssets/Reference/PcTangMenSkills.provenance.json",
        "provenanceSha256": digest(provenance_bytes),
        "relationshipTargetSource": "Assets/StreamingAssets/Reference/PcTangMenRelationshipTargets.txt",
        "relationshipTargetSha256": digest(target_bytes),
        "relationshipTargetProvenanceSource": "Assets/StreamingAssets/Reference/PcTangMenRelationshipTargets.provenance.json",
        "relationshipTargetProvenanceSha256": digest(target_provenance_bytes),
        "relationshipTargetIds": relationship_targets,
        "membershipSource": "harness/docs/stories/SKL-TM-PROOF-001/membership-classification.json",
        "membershipSha256": digest(classification_bytes),
        "pcLearnedSkillIds": learned_ids,
        "observedUnityDisplayIds": unity_ids,
        "unresolvedUnityOnly": unresolved,
        "uiOrder": None,
        "skills": [selected[skill_id] for skill_id in learned_ids],
    }
    return (json.dumps(payload, ensure_ascii=True, sort_keys=True, separators=(",", ":")) + "\n").encode("ascii")


def main() -> int:
    root = Path(__file__).resolve().parents[1]
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", type=Path, default=root / "Assets/StreamingAssets/Reference/PcTangMenSkills.txt")
    parser.add_argument("--provenance", type=Path, default=root / "Assets/StreamingAssets/Reference/PcTangMenSkills.provenance.json")
    parser.add_argument("--classification", type=Path, default=root / "harness/docs/stories/SKL-TM-PROOF-001/membership-classification.json")
    parser.add_argument("--target-source", type=Path, default=root / "Assets/StreamingAssets/Reference/PcTangMenRelationshipTargets.txt")
    parser.add_argument("--target-provenance", type=Path, default=root / "Assets/StreamingAssets/Reference/PcTangMenRelationshipTargets.provenance.json")
    parser.add_argument("--output", type=Path, default=root / "Assets/StreamingAssets/Reference/PcTangMenOracle.json")
    parser.add_argument("--canonical-root", type=Path, default=CANONICAL_ROOT)
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()

    verify_canonical_sources(args.canonical_root)
    expected = build(
        args.source, args.provenance, args.classification, args.canonical_root,
        args.target_source, args.target_provenance,
    )
    oracle_hash = digest(expected)
    hash_path = args.output.with_suffix(args.output.suffix + ".sha256")
    expected_hash = f"{oracle_hash}  {args.output.name}\n"

    if args.check:
        if not args.output.is_file() or args.output.read_bytes() != expected:
            raise SystemExit(f"stale TangMen oracle: run {Path(__file__).name}")
        if not hash_path.is_file() or hash_path.read_text(encoding="ascii") != expected_hash:
            raise SystemExit(f"stale TangMen oracle hash: run {Path(__file__).name}")
        print(f"TangMen oracle OK: 23 learned skills, 32 relationship targets, sha256={oracle_hash}")
        return 0

    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_bytes(expected)
    hash_path.write_text(expected_hash, encoding="ascii", newline="\n")
    print(f"wrote {args.output} (23 learned skills, 32 relationship targets, sha256={oracle_hash})")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
