#!/usr/bin/env python3
"""Deterministic canonical-PC-first all-faction skill membership matrix.

Independent parse of every PC progression/skillbook membership and every Unity
display array -> per-faction row-level union of (PC-learned ∪ Unity-display),
classified as shared / pc_learned_only / unity_display_only_unresolved. The
global union is materialized as an exact-byte vltktool skills.txt slice; this
generator never parses the full encoded canonical skills.txt, only the slice.
"""

from __future__ import annotations

import argparse
import csv
import hashlib
import json
import re
import subprocess
import sys
from pathlib import Path

CANONICAL_ROOT = Path("/var/www/jx-source")
VLTKTOOL = Path("/home/zet/Projects/vltktool/extract_table_slice.py")

CANONICAL_FILES = {
    "pak_unpacked/slistcache/settings/skills.txt": "c77892fb33b6e63783c554bd075caa4891d9b9ec8abb70084582a5c24156e40c",
    "01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/script/global/skills_table.lua": "7e46896c4d5c3fc33cf3b1119ec3e6cf7b1a2c8d7a64ab25d2087331646642b3",
    "01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/script/item/skillbook.lua": "4e5361a6d2756f3596fcc86155dd579b8bf15f69c73651d7f9e8c40f3337d0d9",
}
SKILLS_SHA = CANONICAL_FILES["pak_unpacked/slistcache/settings/skills.txt"]
PROGRESSION_SHA = CANONICAL_FILES["01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/script/global/skills_table.lua"]
SKILLBOOK_SHA = CANONICAL_FILES["01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/script/item/skillbook.lua"]

SCHEMA = "vltk.all-faction.membership-matrix/v1"

# key, display name, Unity array, progression lua key, skillbook faction index.
FACTIONS = [
    ("Shaolin", "Thiếu Lâm", "PcShaolinSkillOrder", "shaolin", 0),
    ("TianWang", "Thiên Vương", "PcTianWangSkillOrder", "tianwang", 1),
    ("TangMen", "Đường Môn", "PcTangMenSkillOrder", "tangmen", 2),
    ("WuDu", "Ngũ Độc", "PcWuDuSkillOrder", "wudu", 3),
    ("EMei", "Nga My", "PcEMeiSkillOrder", "emei", 4),
    ("CuiYan", "Thúy Yên", "PcCuiYanSkillOrder", "cuiyan", 5),
    ("CaiBang", "Cái Bang", "PcCaiBangSkillOrder", "gaibang", 6),
    ("TianRen", "Thiên Nhẫn", "PcTianRenSkillOrder", "tianren", 7),
    ("WuDang", "Võ Đang", "PcWuDangSkillOrder", "wudang", 8),
    ("KunLun", "Côn Luân", "PcKunLunSkillOrder", "kunlun", 9),
]

RELATIONSHIP_FIELDS = (
    ("ChildSkillId", "child_skill_id"),
    ("StartSkillId", "start_skill_id"),
    ("FlySkillId", "fly_skill_id"),
    ("CollidSkillId", "collide_skill_id"),
    ("VanishedSkillId", "vanished_skill_id"),
)

# Completed-wave exclusions are accepted only after their repo-local proof
# artifacts pass the pinned integrity and scope checks below.
COMPLETED_WAVE_SPECS = {
    "Shaolin": {
        "proof": "harness/docs/stories/SKL-S-PROOF-001/static-catalog-proof.json",
        "proof_sha256": "049c73b9e67f2a18dddbcc026ff8a6996ce1117210686e004eac07398be059a1",
        "proof_schema": "vltk.shaolin.static-catalog-proof/v1",
        "membership": "harness/docs/stories/SKL-S-PROOF-001/membership-classification.json",
        "membership_sha256": "73bf4f791e37b2b63d276740ad5886b05c22286c2941cb521cdfc839f5e517a6",
        "membership_schema": "vltk.shaolin.membership-classification/v1",
        "slice": "harness/docs/stories/SKL-S-PROOF-001/PcShaolinSkills.txt",
        "provenance": "harness/docs/stories/SKL-S-PROOF-001/PcShaolinSkills.provenance.json",
        "proof_state": "canonical_static_verified_learned_scope",
        "stories": ["SKL-S-PROOF-001"],
        "require_current_display": True,
    },
    "EMei": {
        "proof": "harness/docs/stories/SKL-EM-PROOF-001/static-catalog-proof.json",
        "proof_sha256": "002618cbfb3c79c0e7e57bc7669de37653dc437ff912ffc5349bf4676db8873d",
        "proof_schema": "vltk.emei.static-catalog-proof/v1",
        "membership": "harness/docs/stories/SKL-EM-PROOF-001/membership-classification.json",
        "membership_sha256": "cafa206bbe716699e996dc15e5e892163c71e67ad3df34d08f955b9a19b89d62",
        "membership_schema": "vltk.emei.membership-classification/v1",
        "slice": "harness/docs/stories/SKL-EM-PROOF-001/PcEMeiSkills.txt",
        "provenance": "harness/docs/stories/SKL-EM-PROOF-001/PcEMeiSkills.provenance.json",
        "proof_state": "canonical_static_verified_learned_scope",
        "stories": ["SKL-EM-PROOF-001"],
        "require_current_display": True,
    },
    "TianRen": {
        "proof": "harness/docs/stories/SKL-TR-PROOF-001/static-catalog-proof.json",
        "proof_sha256": "d75fa4da27e66c51920db80273999d222547287ccf87c4bdf131b5136a2bef45",
        "proof_schema": "vltk.tianren.static-catalog-proof/v1",
        "membership": "harness/docs/stories/SKL-TR-PROOF-001/membership-classification.json",
        "membership_sha256": "466c38f90cf841ae279c2580f0b1b513e9573cf382da1b1eb70b27814c985685",
        "membership_schema": "vltk.tianren.membership-classification/v1",
        "slice": "harness/docs/stories/SKL-TR-PROOF-001/PcTianRenSkills.txt",
        "provenance": "harness/docs/stories/SKL-TR-PROOF-001/PcTianRenSkills.provenance.json",
        "proof_state": "canonical_static_verified_learned_scope",
        "stories": ["SKL-TR-PROOF-001"],
        "require_current_display": True,
    },
    "WuDang": {
        "proof": "harness/docs/stories/SKL-WD-PROOF-001/static-catalog-proof.json",
        "proof_sha256": "1cbab681c8b4bc6ab808bace54e20620c8a5b4028c5f618de2838a4a3b3fd351",
        "proof_schema": "vltk.wudang.static-catalog-proof/v1",
        "membership": "harness/docs/stories/SKL-WD-PROOF-001/membership-classification.json",
        "membership_sha256": "35104b51d81ceb96934798ed6c79b19af42c1837ba786ab92fa8110712e45472",
        "membership_schema": "vltk.wudang.membership-classification/v1",
        "slice": "harness/docs/stories/SKL-WD-PROOF-001/PcWuDangSkills.txt",
        "provenance": "harness/docs/stories/SKL-WD-PROOF-001/PcWuDangSkills.provenance.json",
        "proof_state": "canonical_static_verified_learned_scope",
        "stories": ["SKL-WD-PROOF-001"],
        "require_current_display": True,
    },
    "WuDu": {
        "proof": "harness/docs/stories/SKL-WDU-PROOF-001/static-catalog-proof.json",
        "proof_sha256": "bdf78af995faff3448217fef3663a159b7f0581c7ad5b047bd2b456c5d03b59e",
        "proof_schema": "vltk.wudu.static-catalog-proof/v1",
        "membership": "harness/docs/stories/SKL-WDU-PROOF-001/membership-classification.json",
        "membership_sha256": "4cc693683e6112a6f299790d801fc1e8f856bf5b4a27597dc6665a6d4828194a",
        "membership_schema": "vltk.wudu.membership-classification/v1",
        "slice": "harness/docs/stories/SKL-WDU-PROOF-001/PcWuDuSkills.txt",
        "provenance": "harness/docs/stories/SKL-WDU-PROOF-001/PcWuDuSkills.provenance.json",
        "proof_state": "canonical_static_verified_learned_scope",
        "stories": ["SKL-WDU-PROOF-001"],
        "require_current_display": True,
    },
    "TianWang": {
        "proof": "harness/docs/stories/SKL-TW-PROOF-001/static-catalog-proof.json",
        "proof_sha256": "2601a8f03517ad07e930e0fd248f80b547315b4591a07a28a567407ea55a469e",
        "proof_schema": "vltk.tianwang.static-catalog-proof/v1",
        "membership": "harness/docs/stories/SKL-TW-PROOF-001/membership-classification.json",
        "membership_sha256": "5cc932f515c69355179dfe485017288bc60d1134cba4c2cb8f74fde5c0a8cb67",
        "membership_schema": "vltk.tianwang.membership-classification/v1",
        "slice": "harness/docs/stories/SKL-TW-PROOF-001/PcTianWangSkills.txt",
        "provenance": "harness/docs/stories/SKL-TW-PROOF-001/PcTianWangSkills.provenance.json",
        "proof_state": "canonical_static_verified_learned_scope",
        "stories": ["SKL-TW-PROOF-001"],
        "require_current_display": True,
    },
    "CuiYan": {
        "proof": "harness/docs/stories/SKL-CY-PROOF-001/static-catalog-proof.json",
        "proof_sha256": "5e2123bf27ff82b6889260d9c14d4b598a81f41680c9f61667d0ed004cbf108a",
        "proof_schema": "vltk.cuiyan.static-catalog-proof/v1",
        "membership": "harness/docs/stories/SKL-CY-PROOF-001/membership-classification.json",
        "membership_sha256": "b045ad3292ca61820d947ff2c3d37e8876ad9b0027f437234eacfda5beb1eac1",
        "membership_schema": "vltk.cuiyan.membership-classification/v1",
        "slice": "harness/docs/stories/SKL-CY-PROOF-001/PcCuiYanSkills.txt",
        "provenance": "harness/docs/stories/SKL-CY-PROOF-001/PcCuiYanSkills.provenance.json",
        "proof_state": "canonical_static_verified_learned_scope",
        "stories": ["SKL-CY-PROOF-001"],
        "require_current_display": True,
    },
    "TangMen": {
        "oracle": "Assets/StreamingAssets/Reference/PcTangMenOracle.json",
        "oracle_sha256": "e4270bd12a534b229c962c3fc322a9271aaefc6b99d062e3df0711a5b0f84f89",
        "oracle_schema": "vltk.tangmen.static-oracle/v1",
        "membership": "harness/docs/stories/SKL-TM-PROOF-001/membership-classification.json",
        "membership_sha256": "5802e1abfd4df48b75e708baef9d4767adf0be5d02da771c37465142eb175f2a",
        "membership_schema": "vltk.tangmen.membership-classification/v1",
        "proof_state": "canonical_static_verified_learned_scope",
        "stories": ["SKL-TM-PROOF-001", "SKL-TM-CATALOG-001"],
    },
    "CaiBang": {
        "oracle": "Assets/StreamingAssets/Reference/PcCaiBangOracle.json",
        "oracle_sha256": "91d3251aef30f755f3480a2104a48227eaffd8e7ea8fbf495d189dd185ed84da",
        "oracle_schema": "vltk.caibang.static-oracle/v1",
        "proof_state": "canonical_static_verified_display_scope",
        "stories": ["SKL-CB-PROOF-002"],
    },
    "KunLun": {
        "oracle": "Assets/StreamingAssets/Reference/PcKunLunOracle.json",
        "oracle_sha256": "3be6712946489b82d2595eae77894bcf022f0b6cd4d43977850572c700be399f",
        "oracle_schema": "vltk.kunlun.static-oracle/v1",
        "membership": "harness/docs/stories/SKL-KL-PROOF-001/membership-classification.json",
        "membership_sha256": "1d2643f30287f3386dadefdb3dcceb0a1d6666e2497c68d9f9a7827be80d86ca",
        "membership_schema": "vltk.kunlun.membership-classification/v1",
        "proof_state": "canonical_static_verified_learned_scope",
        "stories": ["SKL-KL-PROOF-001"],
        "require_current_display": True,
    },
}

# Stable GUIDs for the Unity .meta sidecars (written only when missing).
META_GUIDS = {
    "PcAllFactionLearnedDisplaySkills.txt": ("0f549447a4694ae7a02fc024846bf425", "TextScriptImporter"),
    "PcAllFactionLearnedDisplaySkills.provenance.json": ("c8285a79c61249a6b82ef2f60f97e161", "DefaultImporter"),
}


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def verify_canonical_sources(root: Path) -> None:
    # skills.txt is encoded PAK-derived data: only vltktool may read/hash it.
    # Its pinned hash is checked through vltktool provenance in
    # verify_slice_artifacts(), never by this generator.
    for relative in (
        "01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/script/global/skills_table.lua",
        "01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/script/item/skillbook.lua",
    ):
        expected = CANONICAL_FILES[relative]
        path = root / relative
        if not path.is_file():
            raise SystemExit(f"missing canonical source: {path}")
        actual = digest(path.read_bytes())
        if actual != expected:
            raise SystemExit(f"canonical source hash drift: {path} ({actual})")


def verify_completed_waves(repo: Path, factions: list[dict]) -> dict[str, dict]:
    """Return exclusions only when frozen proof matches current scope evidence."""
    by_key = {faction["key"]: faction for faction in factions}
    completed: dict[str, dict] = {}
    for key, spec in COMPLETED_WAVE_SPECS.items():
        faction = by_key.get(key)
        if faction is None:
            raise SystemExit(f"missing completed-wave faction: {key}")
        if "proof" in spec:
            proof_path = repo / spec["proof"]
            membership_path = repo / spec["membership"]
            slice_path = repo / spec["slice"]
            provenance_path = repo / spec["provenance"]
            if not all(path.is_file() for path in (proof_path, membership_path, slice_path, provenance_path)):
                raise SystemExit(f"missing {key} completed-wave proof artifact")
            proof_bytes = proof_path.read_bytes()
            membership_bytes = membership_path.read_bytes()
            if digest(proof_bytes) != spec["proof_sha256"] or digest(membership_bytes) != spec["membership_sha256"]:
                raise SystemExit(f"{key} completed-wave proof hash drift")
            proof = json.loads(proof_bytes)
            membership = json.loads(membership_bytes)
            learned = set(faction["learned"])
            display = set(faction["display"])
            union = sorted(learned | display)
            run_vltktool(CANONICAL_ROOT / "pak_unpacked/slistcache/settings/skills.txt", union, slice_path, provenance_path, check=True)
            manifest = json.loads(provenance_path.read_text(encoding="utf-8"))
            rows = read_slice_rows(slice_path.read_bytes())
            shared = set(membership.get("shared_ids", []))
            learned_only = {item["skill_id"] for item in membership.get("pc_learned_only", [])}
            unresolved = {item["skill_id"] for item in membership.get("unity_only_unresolved", [])}
            relationship_targets = sorted({
                target for skill_id in learned for target in direct_relationships(rows[skill_id]).values()
            })
            if (
                proof.get("schema") != spec["proof_schema"]
                or proof.get("canonical_sources", {}).get("static_rows", {}).get("sha256") != SKILLS_SHA
                or proof.get("canonical_sources", {}).get("progression", {}).get("sha256") != PROGRESSION_SHA
                or proof.get("canonical_sources", {}).get("skillbook", {}).get("sha256") != SKILLBOOK_SHA
                or proof.get("membership_source") != spec["membership"]
                or proof.get("membership_sha256") != spec["membership_sha256"]
                or proof.get("slice_source") != spec["slice"]
                or proof.get("slice_sha256") != digest(slice_path.read_bytes())
                or proof.get("provenance_source") != spec["provenance"]
                or proof.get("provenance_sha256") != digest(provenance_path.read_bytes())
                or proof.get("pc_learned_skill_ids") != sorted(learned)
                or proof.get("observed_unity_display_ids") != sorted(display)
                or proof.get("shared_ids") != sorted(learned & display)
                or proof.get("pc_learned_only_ids") != sorted(learned - display)
                or proof.get("unity_only_unresolved_ids") != sorted(display - learned)
                or proof.get("learned_relationship_target_ids") != relationship_targets
                or proof.get("relationship_self_reference_learned_ids") != sorted(set(relationship_targets) & learned)
                or proof.get("unity_only_relationship_target_ids") != sorted((display - learned) & set(relationship_targets))
                or proof.get("ui_order", "missing") is not None
                or membership.get("schema") != spec["membership_schema"]
                or membership.get("status") != "reviewed_static_membership_evidence"
                or membership.get("ui_contract", {}).get("ordered_skill_ids", "missing") is not None
                or set(membership.get("pc_learned_evidence_ids", [])) != learned
                or set(membership.get("unity_display_ids", [])) != display
                or shared & learned_only
                or shared | learned_only != learned
                or shared & unresolved
                or shared | unresolved != display
                or manifest.get("schema") != "vltk.table-slice-provenance/v1"
                or manifest.get("source", {}).get("sha256") != SKILLS_SHA
                or manifest.get("requested_ids") != union
                or manifest.get("selected_ids") != union
                or set(rows) != set(union)
            ):
                raise SystemExit(f"{key} completed-wave proof scope evidence drift")
            completed[key] = {
                "proof_state": spec["proof_state"], "stories": spec["stories"],
                "proof_path": spec["proof"], "proof_sha256": spec["proof_sha256"],
                "membership_path": spec["membership"], "membership_sha256": spec["membership_sha256"],
            }
            continue
        oracle_path = repo / spec["oracle"]
        if not oracle_path.is_file():
            raise SystemExit(f"missing completed-wave oracle: {oracle_path}")
        oracle_bytes = oracle_path.read_bytes()
        if digest(oracle_bytes) != spec["oracle_sha256"]:
            raise SystemExit(f"completed-wave oracle hash drift: {oracle_path}")
        sidecar = oracle_path.with_name(oracle_path.name + ".sha256")
        if not sidecar.is_file() or sidecar.read_text(encoding="ascii").split()[0] != spec["oracle_sha256"]:
            raise SystemExit(f"completed-wave oracle sidecar drift: {sidecar}")
        oracle = json.loads(oracle_bytes)
        if oracle.get("schema") != spec["oracle_schema"]:
            raise SystemExit(f"{key} completed-wave oracle schema drift")

        if "membership" in spec:
            membership_path = repo / spec["membership"]
            if not membership_path.is_file():
                raise SystemExit(f"missing completed-wave membership: {membership_path}")
            membership_bytes = membership_path.read_bytes()
            if digest(membership_bytes) != spec["membership_sha256"]:
                raise SystemExit(f"completed-wave membership hash drift: {membership_path}")
            membership = json.loads(membership_bytes)
            learned = set(faction["learned"])
            shared = set(membership.get("shared_ids", []))
            learned_only = {item["skill_id"] for item in membership.get("pc_learned_only", [])}
            unresolved = {item["skill_id"] for item in membership.get("unity_only_unresolved", [])}
            historical_display = set(membership.get("unity_display_ids", []))
            if (
                membership.get("schema") != spec["membership_schema"]
                or membership.get("status") != "reviewed_static_membership_evidence"
                or membership.get("ui_contract", {}).get("ordered_skill_ids", "missing") is not None
                or oracle.get("membershipSha256") != spec["membership_sha256"]
                or oracle.get("membershipSource") != spec["membership"]
                or oracle.get("uiOrder", "missing") is not None
                or set(membership.get("pc_learned_evidence_ids", [])) != learned
                or set(oracle.get("pcLearnedSkillIds", [])) != learned
                or shared & learned_only
                or shared | learned_only != learned
                or shared & unresolved
                or shared | unresolved != historical_display
                or {item["skill_id"] for item in oracle.get("unresolvedUnityOnly", [])} != unresolved
                or (spec.get("require_current_display") and (
                    historical_display != set(faction["display"])
                    or set(oracle.get("observedUnityDisplayIds", [])) != set(faction["display"])
                ))
            ):
                raise SystemExit(f"{key} completed-wave scope evidence drift")
        elif set(oracle.get("rootSkillIds", [])) != set(faction["display"]):
            raise SystemExit(f"{key} completed-wave display scope evidence drift")

        completed[key] = {
            "proof_state": spec["proof_state"],
            "stories": spec["stories"],
            "oracle_path": spec["oracle"],
            "oracle_sha256": spec["oracle_sha256"],
            **({"membership_path": spec["membership"], "membership_sha256": spec["membership_sha256"]}
               if "membership" in spec else {}),
        }
    return completed


def _int(raw: str) -> int:
    raw = raw.strip()
    return int(raw) if raw and raw.lstrip("-").isdigit() else 0


def progression_by_tier(path: Path, faction: str) -> tuple[list[int], dict[int, list[int]], dict]:
    """Active progression tiers for one PC faction (tiers [1]..[9] in source)."""
    lines = path.read_bytes().splitlines()
    key = faction.encode("ascii")
    starts = [i for i, line in enumerate(lines) if re.match(rb"^\s*" + key + rb"\s*=\s*\{\s*$", line)]
    if len(starts) != 1:
        raise SystemExit(f"expected exactly one active progression block: {faction}")
    start = starts[0]
    end = next(
        (i for i in range(start + 1, len(lines)) if re.match(rb"^\s*[A-Za-z_][A-Za-z0-9_]*\s*=\s*\{\s*$", lines[i])),
        None,
    )
    if end is None:
        raise SystemExit(f"unterminated progression block: {faction}")
    by_tier: dict[int, list[int]] = {}
    for line in lines[start + 1:end]:
        if line.lstrip().startswith(b"--"):
            continue
        match = re.match(rb"^\s*\[(\d+)\]\s*=\s*\{([^}]*)\}", line)
        if not match:
            continue
        tier = int(match.group(1))
        ids = [int(v) for v in re.findall(rb"\d+", match.group(2))]
        if tier in by_tier:
            raise SystemExit(f"duplicate progression tier {tier} for {faction}")
        by_tier[tier] = ids
    if not by_tier:
        raise SystemExit(f"empty progression block: {faction}")
    flat = [i for ids in by_tier.values() for i in ids]
    if len(flat) != len(set(flat)):
        raise SystemExit(f"duplicate progression skill id: {faction}")
    return flat, by_tier, {"line_start": start + 1, "line_end": end, "faction": faction}


def skillbook_by_level(path: Path, faction_index: int) -> tuple[list[int], dict[int, list[int]], dict]:
    pattern = rb"^\s*\[" + str(faction_index).encode("ascii") + rb"\]\s*=\s*\{(.*)\}\s*,?\s*$"
    matches = [(i, re.match(pattern, line)) for i, line in enumerate(path.read_bytes().splitlines())]
    matches = [(i, m) for i, m in matches if m]
    if len(matches) != 1:
        raise SystemExit(f"expected one skillbook faction row: {faction_index}")
    index, match = matches[0]
    by_level: dict[int, list[int]] = {}
    for level_raw, values_raw in re.findall(rb"\[(\d+)\]\s*=\s*\{([^}]*)\}", match.group(1)):
        level = int(level_raw)
        ids = [int(v) for v in re.findall(rb"\d+", values_raw)]
        if not ids or level in by_level:
            raise SystemExit(f"invalid skillbook level: faction={faction_index} level={level}")
        by_level[level] = ids
    flat = [i for ids in by_level.values() for i in ids]
    if not flat or len(flat) != len(set(flat)):
        raise SystemExit(f"invalid skillbook ids: {faction_index}")
    return flat, by_level, {"line": index + 1, "faction_index": faction_index}


def unity_display_ids(panel_source: str, order_name: str) -> list[int]:
    match = re.search(rf"public static readonly int\[\] {order_name}\s*=\s*\{{(.*?)\}};", panel_source, re.S)
    if not match:
        raise SystemExit(f"missing Unity display array: {order_name}")
    body = re.sub(r"//.*", "", match.group(1))
    ids = [int(v) for v in re.findall(r"\b\d+\b", body)]
    if len(ids) != len(set(ids)):
        raise SystemExit(f"duplicate Unity display id: {order_name}")
    return ids


def categories(row: dict[str, str]) -> list[str]:
    style = _int(row.get("SkillStyle", ""))
    cats = [{0: "missile_active", 1: "melee_active", 2: "buff_state", 3: "passive"}.get(style, "unknown")]
    if _int(row.get("IsAura", "")):
        cats.append("aura")
    if _int(row.get("ByMissle", "")):
        cats.append("by_missile")
    for field, label in RELATIONSHIP_FIELDS:
        if _int(row.get(field, "")):
            cats.append(label)
    return cats


def direct_relationships(row: dict[str, str]) -> dict[str, int]:
    rel: dict[str, int] = {}
    for field, target in RELATIONSHIP_FIELDS:
        value = _int(row.get(field, ""))
        if value:
            rel[target] = value
    return rel


def read_slice_rows(slice_bytes: bytes) -> dict[int, dict[str, str]]:
    rows: dict[int, dict[str, str]] = {}
    for row in csv.DictReader(slice_bytes.decode("latin-1").splitlines(), delimiter="\t"):
        skill_id = _int(row.get("SkillId", ""))
        if not skill_id:
            continue
        if skill_id in rows:
            raise SystemExit(f"duplicate row in slice: {skill_id}")
        rows[skill_id] = row
    return rows


def ensure_meta(asset_path: Path) -> None:
    base = asset_path.name
    guid, importer = META_GUIDS[base]
    meta_path = asset_path.with_name(base + ".meta")
    if meta_path.is_file():
        return
    meta_path.write_text(
        "fileFormatVersion: 2\n"
        f"guid: {guid}\n"
        f"{importer}:\n"
        "  externalObjects: {}\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n",
        encoding="ascii",
        newline="\n",
    )


def run_vltktool(skill_txt: Path, union: list[int], slice_path: Path, manifest_path: Path, check: bool) -> None:
    cmd = [
        sys.executable, str(VLTKTOOL),
        "--input", str(skill_txt),
        "--key-column", "SkillId",
        "--ids", ",".join(str(i) for i in union),
        "--output", str(slice_path),
        "--manifest", str(manifest_path),
    ]
    if check:
        cmd.append("--check")
    result = subprocess.run(cmd, capture_output=True, text=True)
    if result.returncode != 0:
        raise SystemExit(f"vltktool failed: {result.stderr.strip() or result.stdout.strip()}")


def verify_slice_artifacts(union: list[int], slice_path: Path, manifest_path: Path) -> dict[int, dict[str, str]]:
    if not slice_path.is_file() or not manifest_path.is_file():
        raise SystemExit("missing slice or provenance artifact; run generator without --check")
    slice_bytes = slice_path.read_bytes()
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    if manifest.get("schema") != "vltk.table-slice-provenance/v1":
        raise SystemExit("unexpected provenance schema")
    if manifest.get("encoding") != "byte-preserving":
        raise SystemExit("provenance is not byte-preserving")
    if manifest.get("key_column") != "SkillId":
        raise SystemExit("provenance key column drift")
    if manifest.get("source", {}).get("sha256") != SKILLS_SHA:
        raise SystemExit("provenance canonical source hash drift")
    if manifest.get("slice", {}).get("sha256") != digest(slice_bytes):
        raise SystemExit("slice hash drift")
    if manifest.get("slice", {}).get("bytes") != len(slice_bytes):
        raise SystemExit("slice byte count drift")
    if manifest.get("requested_ids") != union:
        raise SystemExit("provenance requested_ids drift")
    selected = manifest.get("selected_ids", [])
    if len(selected) != len(union) or set(selected) != set(union):
        raise SystemExit("provenance selected_ids drift")
    source_lines = manifest.get("source_lines", [])
    if [item["id"] for item in source_lines] != selected:
        raise SystemExit("provenance source_lines drift")
    rows = read_slice_rows(slice_bytes)
    if set(rows) != set(union):
        raise SystemExit("slice row ids do not match union")
    # Selected ids are in source order; the slice rows must appear in that same order.
    if list(rows.keys()) != selected:
        raise SystemExit("slice row order drift")
    return rows


def compute_membership(repo: Path, sources: dict) -> tuple[list[dict], list[int]]:
    panel_source = (repo / "Assets/Scripts/UI/PcSkillPanelService.cs").read_text(encoding="utf-8")
    progression_path = sources["progression"]
    skillbook_path = sources["skillbook"]
    factions: list[dict] = []
    global_union: set[int] = set()
    for key, name, order_name, pc_faction, faction_index in FACTIONS:
        prog_flat, prog_by_tier, prog_meta = progression_by_tier(progression_path, pc_faction)
        sb_flat, sb_by_level, sb_meta = skillbook_by_level(skillbook_path, faction_index)
        display = unity_display_ids(panel_source, order_name)
        learned = set(prog_flat) | set(sb_flat)
        union = learned | set(display)
        global_union |= union
        factions.append({
            "key": key, "name": name, "faction_index": faction_index,
            "progression_flat": prog_flat, "progression_by_tier": prog_by_tier,
            "progression_meta": {**prog_meta, "path": str(progression_path)},
            "skillbook_flat": sb_flat, "skillbook_by_level": sb_by_level,
            "skillbook_meta": {**sb_meta, "path": str(skillbook_path)},
            "display": display, "learned": learned, "union": union,
        })
    return factions, sorted(global_union)


def build_membership_rows(faction: dict, rows: dict[int, dict[str, str]], skill_txt: Path) -> list[dict]:
    learned = faction["learned"]
    display = set(faction["display"])
    prog_by_tier = faction["progression_by_tier"]
    sb_by_level = faction["skillbook_by_level"]
    membership_rows = []
    for skill_id in sorted(faction["union"]):
        if skill_id not in learned and skill_id not in display:
            raise SystemExit(f"union id outside both sets: {skill_id}")
        classification = (
            "shared" if (skill_id in learned and skill_id in display)
            else "pc_learned_only" if skill_id in learned
            else "unity_display_only_unresolved"
        )
        progression_evidence = [f"progression_tier_{tier}" for tier in sorted(prog_by_tier) if skill_id in prog_by_tier[tier]]
        skillbook_evidence = [f"skillbook_{level}" for level in sorted(sb_by_level) if skill_id in sb_by_level[level]]
        row = rows[skill_id]
        membership_rows.append({
            "skill_id": skill_id,
            "classification": classification,
            "progression_evidence": progression_evidence,
            "skillbook_evidence": skillbook_evidence,
            "pc_learned": skill_id in learned,
            "observed_display": skill_id in display,
            "canonical_source": {"path": str(skill_txt), "line": None},
            "categories": categories(row),
            "direct_relationships": direct_relationships(row),
        })
    return membership_rows


def build(repo: Path, sources: dict, slice_path: Path, manifest_path: Path) -> tuple[dict, bytes]:
    verify_canonical_sources(sources["root"])
    factions_data, global_union = compute_membership(repo, sources)
    completed_waves = verify_completed_waves(repo, factions_data)
    rows = verify_slice_artifacts(global_union, slice_path, manifest_path)
    # Canonical source line per row comes from the verified vltktool provenance.
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    line_by_id = {item["id"]: item["line"] for item in manifest["source_lines"]}

    skill_txt = sources["skill_txt"]
    slice_bytes = slice_path.read_bytes()
    manifest_bytes = manifest_path.read_bytes()

    faction_entries = []
    summary_counts = {"factions": 0, "union_rows_total": 0, "shared_total": 0,
                      "pc_learned_only_total": 0, "unity_display_only_unresolved_total": 0}
    for faction in factions_data:
        learned = faction["learned"]
        display = set(faction["display"])
        union = faction["union"]
        shared = learned & display
        learned_only = learned - display
        unity_only = display - learned
        # Assert partitions/unions exactly.
        assert shared | learned_only | unity_only == union
        assert shared & learned_only == set() and shared & unity_only == set() and learned_only & unity_only == set()
        assert learned | display == union
        membership_rows = build_membership_rows(faction, rows, skill_txt)
        for mr in membership_rows:
            mr["canonical_source"]["line"] = line_by_id[mr["skill_id"]]
        relationship_bearing = sum(1 for mr in membership_rows if mr["direct_relationships"])
        proof_state = completed_waves.get(faction["key"], {}).get("proof_state", "weak_or_partial")
        gap = len(learned_only) + len(unity_only)
        faction_entries.append({
            "key": faction["key"],
            "name": faction["name"],
            "faction_index": faction["faction_index"],
            "pc_progression": {
                "skill_ids": faction["progression_flat"],
                "by_tier": {str(t): ids for t, ids in sorted(faction["progression_by_tier"].items())},
                "source": faction["progression_meta"],
            },
            "pc_skillbook": {
                "skill_ids": faction["skillbook_flat"],
                "by_level": {str(l): ids for l, ids in sorted(faction["skillbook_by_level"].items())},
                "source": faction["skillbook_meta"],
            },
            "pc_learned_evidence_skill_ids": sorted(learned),
            "unity_display_skill_ids": sorted(display),
            "union_skill_ids": sorted(union),
            "shared_count": len(shared),
            "pc_learned_only_count": len(learned_only),
            "unity_display_only_unresolved_count": len(unity_only),
            "symmetric_gap_count": gap,
            "relationship_bearing_union_row_count": relationship_bearing,
            "proof_state": proof_state,
            "membership_rows": membership_rows,
        })
        summary_counts["factions"] += 1
        summary_counts["union_rows_total"] += len(union)
        summary_counts["shared_total"] += len(shared)
        summary_counts["pc_learned_only_total"] += len(learned_only)
        summary_counts["unity_display_only_unresolved_total"] += len(unity_only)

    ranking = rank_factions(faction_entries, completed_waves)
    winner = ranking[0] if ranking else None
    winner_initials = _initials(winner["key"]) if winner else ""
    recommended = {
        "id": f"SKL-{winner_initials}-PROOF-001" if winner else None,
        "title": f"{winner['name']} canonical learned-membership and static catalog proof" if winner else None,
        "winner": winner["key"] if winner else None,
        "symmetric_gap_count": winner["symmetric_gap_count"] if winner else None,
        "reason": (
            "Highest symmetric membership gap among non-completed factions: PC-learned and "
            "Unity-display membership must be reconciled against canonical sources before any oracle."
        ),
    }

    matrix = {
        "schema": SCHEMA,
        "generated_by": "scripts/audit_skill_coverage.py",
        "canonical_sources": {
            "static_rows": {
                "path": str(skill_txt),
                "sha256": SKILLS_SHA,
                "provides": "canonical static skill rows (parsed only via the exact-byte slice)",
            },
            "progression": {
                "path": str(sources["progression"]),
                "sha256": PROGRESSION_SHA,
                "provides": "PC active-category progression membership (skills_table.lua)",
            },
            "skillbook": {
                "path": str(sources["skillbook"]),
                "sha256": SKILLBOOK_SHA,
                "provides": "level 90/120/150 skillbook grants (skillbook.lua)",
            },
        },
        "generated_artifacts": {
            "slice": {
                "path": "Assets/StreamingAssets/Reference/PcAllFactionLearnedDisplaySkills.txt",
                "sha256": digest(slice_bytes),
                "bytes": len(slice_bytes),
            },
            "provenance": {
                "path": "Assets/StreamingAssets/Reference/PcAllFactionLearnedDisplaySkills.provenance.json",
                "sha256": digest(manifest_bytes),
                "schema": "vltk.table-slice-provenance/v1",
            },
        },
        "selection_authority": (
            "PC progression (skills_table.lua) and skillbook (skillbook.lua) are canonical "
            "learned-membership evidence; Unity PcSkillPanelService arrays are observed display only."
        ),
        "membership_caveat": (
            "These sources prove learned-membership evidence and observed display scope; they do not "
            "prove UI slot order, runtime/platform parity, or child/support/event resolution."
        ),
        "ui_order_caveat": (
            "UI order is never inferred: PC tiers and skillbook grants prove membership, not panel slot order."
        ),
        "global_union_size": len(global_union),
        "global_union_skill_ids": global_union,
        "factions": faction_entries,
        "ranking": ranking,
        "excluded_from_ranking": [
            {"key": key, **evidence} for key, evidence in completed_waves.items()
        ],
        "recommended_next_story": recommended,
        "summary_counts": summary_counts,
    }
    # Global partition invariant: every membership row id across factions is in the slice union.
    seen = set()
    for fe in faction_entries:
        for mr in fe["membership_rows"]:
            seen.add(mr["skill_id"])
    assert seen == set(global_union), "membership rows do not cover the global union exactly"
    serialized = (json.dumps(matrix, ensure_ascii=False, indent=2) + "\n").encode("utf-8")
    return matrix, serialized


def _initials(key: str) -> str:
    """Stable unique story codes; WuDang and WuDu both otherwise produce WD."""
    return {"WuDang": "WD", "WuDu": "WDU"}.get(key, "".join(c for c in key if c.isupper()))


def rank_factions(faction_entries: list[dict], completed_waves: dict[str, dict]) -> list[dict]:
    candidates = [fe for fe in faction_entries if fe["key"] not in completed_waves]
    candidates.sort(
        key=lambda fe: (
            -fe["symmetric_gap_count"],
            -fe["relationship_bearing_union_row_count"],
            fe["key"],
        )
    )
    return [
        {
            "rank": rank,
            "key": fe["key"],
            "name": fe["name"],
            "symmetric_gap_count": fe["symmetric_gap_count"],
            "relationship_bearing_union_row_count": fe["relationship_bearing_union_row_count"],
            "pc_learned_only_count": fe["pc_learned_only_count"],
            "unity_display_only_unresolved_count": fe["unity_display_only_unresolved_count"],
        }
        for rank, fe in enumerate(candidates, start=1)
    ]


def resolve_sources(args) -> dict:
    root = Path(args.canonical_root)
    return {
        "root": root,
        "skill_txt": root / "pak_unpacked/slistcache/settings/skills.txt",
        "progression": root / "01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/script/global/skills_table.lua",
        "skillbook": root / "01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/script/item/skillbook.lua",
    }


def main() -> int:
    repo = Path(__file__).resolve().parents[1]
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", default=str(repo))
    parser.add_argument("--canonical-root", default=str(CANONICAL_ROOT))
    parser.add_argument(
        "--slice",
        default=str(repo / "Assets/StreamingAssets/Reference/PcAllFactionLearnedDisplaySkills.txt"),
    )
    parser.add_argument(
        "--manifest",
        default=str(repo / "Assets/StreamingAssets/Reference/PcAllFactionLearnedDisplaySkills.provenance.json"),
    )
    parser.add_argument(
        "--output",
        default=str(repo / "harness/docs/stories/SKL-ALL-PARITY-001/coverage-matrix.json"),
    )
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()

    sources = resolve_sources(args)
    verify_canonical_sources(sources["root"])
    slice_path = Path(args.slice)
    manifest_path = Path(args.manifest)
    output_path = Path(args.output)

    # Compute the union once to drive vltktool extraction/check.
    _, global_union = compute_membership(Path(args.repo), sources)

    if args.check:
        run_vltktool(sources["skill_txt"], global_union, slice_path, manifest_path, check=True)
        matrix, serialized = build(Path(args.repo), sources, slice_path, manifest_path)
        if not output_path.is_file() or output_path.read_bytes() != serialized:
            raise SystemExit(f"stale coverage matrix: run {Path(__file__).name}")
        print(f"coverage matrix OK: union={len(global_union)}, "
              f"winner={matrix['recommended_next_story']['winner']}, "
              f"sha256={digest(serialized)}")
        return 0

    run_vltktool(sources["skill_txt"], global_union, slice_path, manifest_path, check=False)
    ensure_meta(slice_path)
    ensure_meta(manifest_path)
    matrix, serialized = build(Path(args.repo), sources, slice_path, manifest_path)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_bytes(serialized)
    winner = matrix["recommended_next_story"]["winner"]
    print(f"wrote {output_path} (union={len(global_union)}, winner={winner}, sha256={digest(serialized)})")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
