#!/usr/bin/env python3
"""Generate/check the deterministic Cái Bang static parity oracle.

The input is the exact repo-local slice extracted from canonical PC skills.txt.
This script intentionally has no dependency on Unity/C# production code.
"""

from __future__ import annotations

import argparse
import csv
import hashlib
import json
from pathlib import Path

CANONICAL_ROOT = Path("/var/www/jx-pc")
CANONICAL_FILES = {
    "pak_unpacked/slistcache/settings/skills.txt": "c77892fb33b6e63783c554bd075caa4891d9b9ec8abb70084582a5c24156e40c",
    "01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/script/skill/gaibang.lua": "56d9910a0d601ee28f40f26f257af1bb6f98757c8319a1b336926bc9d4471ed8",
}

ROOT_IDS = (
    115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, 128,
    129, 130, 274, 277, 357, 358, 359, 360, 714, 720, 1073, 1074,
)

# Canonical gaibang.lua skill_collideevent relationships absent from static skills.txt rows.
RELATIONSHIP_OVERRIDES = {
    357: {"collideSkillId": 389},
    1073: {"collideSkillId": 1072},
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


def integer(value: str) -> int:
    value = value.strip()
    return int(value) if value else 0


def build(source: Path) -> bytes:
    source_bytes = source.read_bytes()
    # latin-1 is byte-preserving; the oracle only interprets ASCII headers/numbers/paths.
    rows = csv.DictReader(source_bytes.decode("latin-1").splitlines(), delimiter="\t")
    selected = {}
    for row in rows:
        skill_id = integer(row.get("SkillId", ""))
        if skill_id not in ROOT_IDS:
            continue
        item = {"skillId": skill_id}
        present = []
        for source_name, target in INT_FIELDS.items():
            raw = row.get(source_name, "").strip()
            if raw:
                value = int(raw)
                item[target] = (1 if value != 0 else 0) if target in BOOL_FIELDS else value
                present.append(target)
        item.update((target, row.get(source_name, "").strip()) for source_name, target in STRING_FIELDS.items())
        for target, value in RELATIONSHIP_OVERRIDES.get(skill_id, {}).items():
            item[target] = value
            if target not in present:
                present.append(target)
        item["present"] = sorted(present)
        selected[skill_id] = item

    missing = sorted(set(ROOT_IDS) - set(selected))
    if missing:
        raise SystemExit(f"missing Cái Bang root rows: {missing}")

    payload = {
        "schema": "vltk.caibang.static-oracle/v1",
        "canonicalSources": [
            {
                "path": "/var/www/jx-pc/pak_unpacked/slistcache/settings/skills.txt",
                "sha256": "c77892fb33b6e63783c554bd075caa4891d9b9ec8abb70084582a5c24156e40c",
                "provides": "static rows",
            },
            {
                "path": "/var/www/jx-pc/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/script/skill/gaibang.lua",
                "sha256": "56d9910a0d601ee28f40f26f257af1bb6f98757c8319a1b336926bc9d4471ed8",
                "provides": "357/1073 collide relationships",
            },
        ],
        "sliceSource": "Assets/StreamingAssets/Reference/PcCaiBangSkills.txt",
        "sliceSha256": hashlib.sha256(source_bytes).hexdigest(),
        "rootSkillIds": list(ROOT_IDS),
        "skills": [selected[skill_id] for skill_id in ROOT_IDS],
    }
    return (json.dumps(payload, ensure_ascii=True, sort_keys=True, separators=(",", ":")) + "\n").encode()


def verify_canonical_sources(canonical_root: Path) -> None:
    for relative, expected in CANONICAL_FILES.items():
        path = canonical_root / relative
        if not path.exists():
            raise SystemExit(f"missing canonical source: {path}")
        actual = hashlib.sha256(path.read_bytes()).hexdigest()
        if actual != expected:
            raise SystemExit(f"canonical source hash drift: {path} ({actual})")


def main() -> int:
    root = Path(__file__).resolve().parents[1]
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", type=Path, default=root / "Assets/StreamingAssets/Reference/PcCaiBangSkills.txt")
    parser.add_argument("--output", type=Path, default=root / "Assets/StreamingAssets/Reference/PcCaiBangOracle.json")
    parser.add_argument("--canonical-root", type=Path, default=CANONICAL_ROOT)
    parser.add_argument("--packaged-slice", type=Path, default=root / "Assets/Resources/Reference/PcCaiBangSkills.bytes")
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()

    verify_canonical_sources(args.canonical_root)
    source_hash = hashlib.sha256(args.source.read_bytes()).hexdigest()
    packaged_hash = hashlib.sha256(args.packaged_slice.read_bytes()).hexdigest()
    if packaged_hash != source_hash:
        raise SystemExit(
            f"packaged Cái Bang slice drift: {args.packaged_slice} ({packaged_hash} != {source_hash})"
        )
    expected = build(args.source)
    digest = hashlib.sha256(expected).hexdigest()
    hash_path = args.output.with_suffix(args.output.suffix + ".sha256")
    expected_hash = f"{digest}  {args.output.name}\n"

    if args.check:
        if not args.output.exists() or args.output.read_bytes() != expected:
            raise SystemExit(f"stale oracle: run {Path(__file__).name}")
        if not hash_path.exists() or hash_path.read_text(encoding="ascii") != expected_hash:
            raise SystemExit(f"stale oracle hash: run {Path(__file__).name}")
        print(f"Cái Bang oracle OK: {len(ROOT_IDS)} roots, sha256={digest}")
        return 0

    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_bytes(expected)
    hash_path.write_text(expected_hash, encoding="ascii", newline="\n")
    print(f"wrote {args.output} ({len(ROOT_IDS)} roots, sha256={digest})")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
