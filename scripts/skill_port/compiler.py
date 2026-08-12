#!/usr/bin/env python3
"""Deterministic offline SkillPort catalog compiler."""

from __future__ import annotations

import argparse
import base64
import csv
import hashlib
import json
import shutil
import subprocess
import sys
from collections import Counter, defaultdict
from pathlib import Path
from typing import Any

import google.protobuf
from cryptography.hazmat.primitives.asymmetric import ed25519

from scripts.skill_port.gen.content.v1 import skill_catalog_pb2 as skillpb

SCHEMA_VERSION = "vltk.skill_port.catalog/v1"
TOOL_REVISION = "skill_port_compiler_py_protobuf_ed25519/v2"
FIXED_BUILD_TIME_UTC = "1970-01-01T00:00:00Z"
DEFAULT_OUT = Path("Assets/StreamingAssets/Generated/SkillPort")
MANIFEST_SCHEMA_VERSION = 1
CONTENT_RELEASE_ID = "00000000-0000-4000-8000-000000000242"
REALM_ID = "00000000-0000-4000-8000-000000000001"
MANIFEST_VERSION = "skill-port-dev-19700101"
RUNTIME_SKILL_POLICY_ID = "skill-port-runtime-policy-gate0-blocked-v1"
TEST_ONLY_SIGNING_KEY_ID = "test-only-skill-port-ed25519-fixture-v1"
TEST_ONLY_SIGNING_SEED = hashlib.sha256(b"vltk-mobile skill-port test-only ed25519 fixture v1").digest()
PRODUCTION_FORBIDDEN_SIGNING_KEY_IDS = {TEST_ONLY_SIGNING_KEY_ID}

FIELDS: tuple[tuple[str, str, str], ...] = (
    ("skill_style", "SkillStyle", "int"),
    ("state_special_id", "StateSpecialId", "int"),
    ("is_aura", "IsAura", "int"),
    ("attack_radius", "AttackRadius", "int"),
    ("missiles_generate", "MslsGenerate", "int"),
    ("missiles_generate_data", "MslsGenerateData", "int"),
    ("missile_form", "MisslesForm", "int"),
    ("child_skill_id", "ChildSkillId", "int"),
    ("child_skill_level", "ChildSkillLevel", "int"),
    ("child_skill_num", "ChildSkillNum", "int"),
    ("base_skill", "BaseSkill", "int"),
    ("char_anim_id", "CharAnimId", "int"),
    ("wait_time", "WaitTime", "ticks"),
    ("skill_cost_type", "SkillCostType", "int"),
    ("cost_value", "CostValue", "int"),
    ("time_per_cast", "TimePerCast", "ticks"),
    ("time_per_cast_on_horse", "TimePerCastOnHorse", "ticks"),
    ("is_physical", "IsPhysical", "int"),
    ("target_only", "TargetOnly", "int"),
    ("target_enemy", "TargetEnemy", "int"),
    ("target_ally", "TargetAlly", "int"),
    ("target_self", "TargetSelf", "int"),
    ("target_obj", "TargetObj", "int"),
    ("by_missile", "ByMissle", "int"),
    ("is_use_attack_rating", "IsUseAR", "int"),
    ("req_level", "ReqLevel", "int"),
    ("max_level", "MaxLevel", "int"),
    ("equip_limit", "EqtLimit", "int"),
    ("horse_limit", "HorseLimit", "int"),
    ("do_hurt", "DoHurt", "int"),
    ("weapon_skill", "WeaponSkill", "int"),
    ("start_skill_id", "StartSkillId", "int"),
    ("fly_skill_id", "FlySkillId", "int"),
    ("collide_skill_id", "CollidSkillId", "int"),
    ("vanish_skill_id", "VanishedSkillId", "int"),
    ("pre_cast_spr_path", "PreCastSpr", "path"),
    ("man_cast_snd_path", "ManCastSnd", "path"),
    ("fm_cast_snd_path", "FMCastSnd", "path"),
)

RELATION_COLUMNS = {
    "child": "ChildSkillId",
    "start": "StartSkillId",
    "fly": "FlySkillId",
    "collide": "CollidSkillId",
    "vanish": "VanishedSkillId",
}
RELATION_TYPES = set(RELATION_COLUMNS)
TARGET_KINDS = {
    "none", "skill", "missile", "canonical_skill", "external_canonical_skill",
    "missing", "missing_missile", "external_unknown",
}
STATE_KINDS = {"none", "state_visual", "pc_state_stub_no_bytes", "pc_state_absent_no_visual", "missing"}
NODE_KINDS = {"effect", "presentation", "lifecycle", "state", "missile"}
TEXT_FIELDS = {"pre_cast_spr_path", "man_cast_snd_path", "fm_cast_snd_path"}


def sha256_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def canonical_json(obj: Any) -> bytes:
    return (json.dumps(obj, ensure_ascii=False, sort_keys=True, indent=2) + "\n").encode("utf-8")


def protoc_version() -> str:
    proc = subprocess.run(["protoc", "--version"], text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    if proc.returncode != 0:
        raise ValueError(f"protoc failed: {proc.stderr.strip()}")
    return proc.stdout.strip()


def test_only_public_key_b64() -> str:
    public = ed25519.Ed25519PrivateKey.from_private_bytes(TEST_ONLY_SIGNING_SEED).public_key()
    return base64.b64encode(public.public_bytes_raw()).decode("ascii")


def sign_test_only(payload: bytes) -> str:
    # ponytail: fixture key only makes schema-valid dev artifacts; release gate rejects it.
    sig = ed25519.Ed25519PrivateKey.from_private_bytes(TEST_ONLY_SIGNING_SEED).sign(payload)
    return base64.b64encode(sig).decode("ascii")


def manifest_hash_payload(manifest: dict[str, Any]) -> dict[str, Any]:
    payload = json.loads(json.dumps(manifest, ensure_ascii=False))
    payload.pop("signature", None)
    payload.pop("manifestSha256", None)
    payload.get("contentDigest", {}).pop("manifestSha256", None)
    return payload


def manifest_signing_payload(manifest: dict[str, Any]) -> dict[str, Any]:
    payload = json.loads(json.dumps(manifest, ensure_ascii=False))
    payload.pop("signature", None)
    return payload


def verify_test_only_manifest_signature(manifest: dict[str, Any]) -> None:
    signature = base64.b64decode(manifest["signature"])
    public = ed25519.Ed25519PrivateKey.from_private_bytes(TEST_ONLY_SIGNING_SEED).public_key()
    public.verify(signature, canonical_json(manifest_signing_payload(manifest)))


def validate_production_manifest(manifest: dict[str, Any]) -> None:
    key_id = manifest.get("signingKeyId")
    if key_id in PRODUCTION_FORBIDDEN_SIGNING_KEY_IDS or str(key_id).startswith("test-only-"):
        raise ValueError(f"production manifest uses forbidden development signing key: {key_id}")


def load_json(path: Path) -> Any:
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except FileNotFoundError as exc:
        raise ValueError(f"missing mandatory source: {path}") from exc


def relpath(path: Path, repo: Path) -> str:
    try:
        return path.resolve().relative_to(repo.resolve()).as_posix()
    except ValueError:
        return path.as_posix()


def int_value(raw: Any) -> int:
    if raw is None:
        return 0
    text = str(raw).strip()
    if text in {"", "-"}:
        return 0
    try:
        return int(text)
    except ValueError:
        return 0


def str_value(raw: Any) -> str:
    return "" if raw is None else str(raw).strip()


def read_skill_slice(path: Path) -> tuple[list[dict[str, str]], str, int]:
    data = path.read_bytes()
    rows: list[dict[str, str]] = []
    seen: set[int] = set()
    for row in csv.DictReader(data.decode("latin-1").splitlines(), delimiter="\t"):
        sid = int_value(row.get("SkillId"))
        if sid <= 0:
            continue
        if sid in seen:
            raise ValueError(f"duplicate SkillId in skill slice: {sid}")
        seen.add(sid)
        rows.append(row)
    return rows, sha256_bytes(data), len(data)


def digest_path(path: Path) -> dict[str, Any]:
    data = path.read_bytes()
    return {"path": path.as_posix(), "sha256": sha256_bytes(data), "bytes": len(data)}


def source_snapshot(repo: Path, paths: dict[str, Path], coverage: dict, provenance: dict) -> dict[str, Any]:
    out = {name: {**digest_path(path), "path": relpath(path, repo)} for name, path in paths.items()}
    static_src = coverage.get("canonical_sources", {}).get("static_rows", {})
    out["canonical_static_rows"] = {
        "path": static_src.get("path"),
        "sha256_from_vltktool_provenance": provenance.get("source", {}).get("sha256"),
        "coverage_sha256": static_src.get("sha256"),
        "not_hashed_by_compiler": True,
    }
    out["progression"] = coverage.get("canonical_sources", {}).get("progression", {})
    out["skillbook"] = coverage.get("canonical_sources", {}).get("skillbook", {})
    package_ini = Path("/var/www/jx-pc/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/package.ini")
    out["package_ini"] = (
        {"path": package_ini.as_posix(), "sha256": sha256_bytes(package_ini.read_bytes()), "bytes": package_ini.stat().st_size}
        if package_ini.is_file() else {"path": package_ini.as_posix(), "blocker": "package_ini_missing"}
    )
    kpak_candidates = [package_ini.with_name("KPakList.ini"), package_ini.with_name("KPakList.txt")]
    present = [p for p in kpak_candidates if p.is_file()]
    out["kpaklist"] = (
        [{"path": p.as_posix(), "sha256": sha256_bytes(p.read_bytes()), "bytes": p.stat().st_size} for p in present]
        if present else {"blocker": "active_KPakList_not_discoverable", "searched": [p.as_posix() for p in kpak_candidates]}
    )
    return out


def validate_sources(coverage: dict, presentation: dict, provenance: dict, slice_hash: str, rows: list[dict[str, str]]) -> list[int]:
    if coverage.get("schema") != "vltk.all-faction.membership-matrix/v1":
        raise ValueError("coverage schema mismatch")
    if presentation.get("schema") != "vltk.pc.presentation-inventory/v1":
        raise ValueError("presentation schema mismatch")
    if provenance.get("schema") != "vltk.table-slice-provenance/v1":
        raise ValueError("slice provenance schema mismatch")
    if provenance.get("encoding") != "byte-preserving":
        raise ValueError("slice provenance not byte-preserving")
    expected_slice = provenance.get("slice", {}).get("sha256")
    if expected_slice != slice_hash:
        raise ValueError(f"slice hash drift: expected {expected_slice}, got {slice_hash}")
    static_sha = coverage.get("canonical_sources", {}).get("static_rows", {}).get("sha256")
    if provenance.get("source", {}).get("sha256") != static_sha:
        raise ValueError("canonical static row provenance hash drift")
    union = coverage.get("global_union_skill_ids")
    if not isinstance(union, list) or len(union) != 242:
        raise ValueError(f"expected 242 union ids, got {len(union) if isinstance(union, list) else 'invalid'}")
    if len(union) != len(set(union)):
        raise ValueError("duplicate ids in coverage union")
    row_ids = [int_value(r.get("SkillId")) for r in rows]
    if sorted(row_ids) != sorted(union):
        raise ValueError("skill slice ids do not match 242-row union")
    p_rows = presentation.get("summary_counts", {}).get("rows")
    if p_rows != 242 or len(presentation.get("rows", [])) != 242:
        raise ValueError(f"presentation rows mismatch: {p_rows}/{len(presentation.get('rows', []))}")
    return [int(v) for v in union]


def classification_for(row: dict[str, Any]) -> tuple[str, list[str]]:
    classes = {f.get("classification") for f in row.get("factions", [])}
    if classes == {"unity_display_only_unresolved"}:
        return "evidence_pending", ["unity_only_requires_runtime_capture_or_two_static_sources"]
    if classes == {"pc_learned_only"}:
        return "pc_only", []
    if "shared" in classes:
        return "exposed", []
    return "evidence_pending", ["membership_classification_unresolved"]


def static_fields(row: dict[str, str]) -> dict[str, Any]:
    out: dict[str, Any] = {}
    for name, col, kind in FIELDS:
        raw = row.get(col, "")
        value: Any = str_value(raw) if name in TEXT_FIELDS else int_value(raw)
        node_kind = "presentation" if name in TEXT_FIELDS or name in {"char_anim_id"} else "effect"
        if name.endswith("skill_id"):
            node_kind = "lifecycle"
        if name == "state_special_id":
            node_kind = "state"
        out[name] = {
            "column": col,
            "kind": kind,
            "node_kind": node_kind,
            "value": value,
        }
    return out


def relation_from_presentation(skill_id: int, rel: str, p_row: dict[str, Any], row: dict[str, str]) -> dict[str, Any]:
    p_key = {"child": "child_skill", "start": "start_skill", "fly": "fly_skill", "collide": "collide_skill", "vanish": "vanish_skill"}[rel]
    p_link = p_row.get("relations", {}).get(p_key, {})
    target_id = int_value(row.get(RELATION_COLUMNS[rel]))
    return {
        "type": rel,
        "source_skill_id": skill_id,
        "target_id": target_id,
        "target_kind": p_link.get("target_kind", "none" if target_id <= 0 else "external_unknown"),
        "proof_state": p_link.get("proof_state", "source_only" if target_id <= 0 else "missing"),
        "blockers": sorted(p_link.get("blockers", [])),
    }


def state_relation(p_row: dict[str, Any], row: dict[str, str], skill_id: int) -> dict[str, Any]:
    link = p_row.get("relations", {}).get("state_visual", {})
    return {
        "type": "state_visual",
        "source_skill_id": skill_id,
        "target_id": int_value(row.get("StateSpecialId")),
        "target_kind": link.get("target_kind", "none"),
        "proof_state": link.get("proof_state", "source_only"),
        "blockers": sorted(link.get("blockers", [])),
    }


def asset_dependencies(skill_id: int, fields: dict[str, Any], p_row: dict[str, Any]) -> list[dict[str, Any]]:
    deps: list[dict[str, Any]] = []
    for name in ("pre_cast_spr_path", "man_cast_snd_path", "fm_cast_snd_path"):
        value = fields[name]["value"]
        if value:
            deps.append({
                "skill_id": skill_id,
                "kind": "spr" if name.endswith("spr_path") else "sound",
                "source_field": name,
                "source_path": value,
                "status": "blocked_asset_provenance",
                "blockers": ["package_ini_winner_not_resolved_for_skill_asset", "KPakList_authority_missing_or_unlinked"],
            })
    child = p_row.get("relations", {}).get("child_skill", {})
    target = child.get("target_ref", {})
    for slot in target.get("visual_slots", []) if isinstance(target, dict) else []:
        for key, kind in (("anim", "missile_anim"), ("info", "missile_anim_info"), ("sound", "missile_sound")):
            value = slot.get(key)
            if value:
                deps.append({
                    "skill_id": skill_id,
                    "kind": kind,
                    "source_field": f"missile.visual_slots.{slot.get('slot')}.{key}",
                    "source_path": value,
                    "status": "blocked_asset_provenance",
                    "blockers": ["missile_asset_package_winner_not_resolved"],
                })
    state = p_row.get("relations", {}).get("state_visual", {})
    if state.get("target_kind") == "state_visual":
        deps.append({
            "skill_id": skill_id,
            "kind": "state_visual_mapping",
            "source_field": "state_visual",
            "source_path": "Assets/StreamingAssets/Reference/PcAttrib/state_visual_mapping.txt",
            "status": "source_only",
            "blockers": ["state_visual_asset_paths_unparsed_from_mapping"],
        })
    return deps


def enum_value(prefix: str, value: str) -> int:
    name = f"{prefix}_{value.upper()}"
    return int(getattr(skillpb, name))


def static_field_message(name: str, data: dict[str, Any]) -> skillpb.StaticField:
    field = skillpb.StaticField(
        name=name,
        source_column=data["column"],
        kind=enum_value("STATIC_FIELD_KIND", data["kind"]),
        node_kind=enum_value("NODE_KIND", data["node_kind"]),
    )
    if data["kind"] == "path":
        field.text_value = str(data["value"])
    else:
        field.int_value = int_value(data["value"])
    return field


def faction_message(data: dict[str, Any]) -> skillpb.FactionMembership:
    return skillpb.FactionMembership(
        faction_index=int_value(data.get("faction_index")),
        key=str_value(data.get("key")),
        name=str_value(data.get("name")),
        classification=str_value(data.get("classification")),
    )


def relation_message(data: dict[str, Any]) -> skillpb.LifecycleRelation:
    return skillpb.LifecycleRelation(
        type=enum_value("LIFECYCLE_RELATION_TYPE", str_value(data.get("type"))),
        source_skill_id=int_value(data.get("source_skill_id")),
        target_id=int_value(data.get("target_id")),
        target_kind=str_value(data.get("target_kind")),
        proof_state=str_value(data.get("proof_state")),
        blockers=sorted(data.get("blockers", [])),
    )


def state_message(data: dict[str, Any]) -> skillpb.StateRelation:
    return skillpb.StateRelation(
        source_skill_id=int_value(data.get("source_skill_id")),
        target_id=int_value(data.get("target_id")),
        target_kind=str_value(data.get("target_kind")),
        proof_state=str_value(data.get("proof_state")),
        blockers=sorted(data.get("blockers", [])),
    )


def dependency_message(data: dict[str, Any]) -> skillpb.AssetDependency:
    return skillpb.AssetDependency(
        skill_id=int_value(data.get("skill_id")),
        kind=str_value(data.get("kind")),
        source_field=str_value(data.get("source_field")),
        source_path=str_value(data.get("source_path")),
        status=str_value(data.get("status")),
        blockers=sorted(data.get("blockers", [])),
    )


def faction_progression_message(data: dict[str, Any]) -> skillpb.FactionProgression:
    msg = skillpb.FactionProgression(
        faction_index=int_value(data.get("faction_index")),
        key=str_value(data.get("key")),
        name=str_value(data.get("name")),
        progression_skill_ids=[int_value(v) for v in data.get("progression_skill_ids", [])],
        skillbook_skill_ids=[int_value(v) for v in data.get("skillbook_skill_ids", [])],
        union_skill_ids=[int_value(v) for v in data.get("union_skill_ids", [])],
        unity_display_skill_ids=[int_value(v) for v in data.get("unity_display_skill_ids", [])],
    )
    for tier, ids in sorted(data.get("progression_by_tier", {}).items(), key=lambda kv: str(kv[0])):
        msg.progression_by_tier.append(skillpb.TierSkillIds(tier=str(tier), skill_ids=[int_value(v) for v in ids]))
    for level, ids in sorted(data.get("skillbook_by_level", {}).items(), key=lambda kv: int_value(kv[0])):
        msg.skillbook_by_level.append(skillpb.LevelSkillIds(level=int_value(level), skill_ids=[int_value(v) for v in ids]))
    return msg


def runtime_policy_message(catalog_union_sha256: str) -> skillpb.RuntimeSkillPolicy:
    return skillpb.RuntimeSkillPolicy(
        policy_id=RUNTIME_SKILL_POLICY_ID,
        catalog_union_size=242,
        catalog_union_sha256=catalog_union_sha256,
        source_tool="vltktool",
        filesystem_fallback_allowed=False,
        runtime_parity_claimed=False,
        pc_runtime_evidence_status="BLOCKED",
        android_physical_evidence_status="BLOCKED",
        blockers=[
            "canonical_KPak_provenance_absent",
            "runtime_skill_formula_effect_evidence_absent",
            "android_physical_runtime_evidence_absent",
        ],
    )


def build_rows(union: list[int], slice_rows: list[dict[str, str]], presentation: dict) -> tuple[list[dict[str, Any]], list[dict[str, Any]]]:
    p_by_id = {int(r["skill_id"]): r for r in presentation.get("rows", [])}
    s_by_id = {int_value(r.get("SkillId")): r for r in slice_rows}
    out: list[dict[str, Any]] = []
    deps: list[dict[str, Any]] = []
    for sid in sorted(union):
        if sid not in p_by_id or sid not in s_by_id:
            raise ValueError(f"missing row for union id: {sid}")
        p_row = p_by_id[sid]
        s_row = s_by_id[sid]
        exposure, blockers = classification_for(p_row)
        fields = static_fields(s_row)
        rels = [relation_from_presentation(sid, rel, p_row, s_row) for rel in ("child", "start", "fly", "collide", "vanish")]
        state = state_relation(p_row, s_row, sid)
        row_deps = asset_dependencies(sid, fields, p_row)
        if row_deps:
            blockers.append("asset_provenance_blocked")
        if p_row.get("proof_state") == "source_only":
            blockers.append("presentation_lifecycle_source_only")
        for rel in rels + [state]:
            blockers.extend(rel.get("blockers", []))
            if rel.get("proof_state") in {"missing"}:
                blockers.append(f"{rel['type']}_relation_missing")
        out.append({
            "skill_id": sid,
            "skill_name": str_value(s_row.get("SkillName")),
            "exposure_state": exposure,
            "factions": sorted(p_row.get("factions", []), key=lambda f: (f.get("key", ""), f.get("classification", ""))),
            "static_fields": fields,
            "typed_nodes": [
                {"kind": "effect", "status": "source_only", "field_count": sum(1 for f in fields.values() if f["node_kind"] == "effect")},
                {"kind": "presentation", "status": "source_only", "field_count": sum(1 for f in fields.values() if f["node_kind"] == "presentation")},
                {"kind": "lifecycle", "status": "source_only", "field_count": sum(1 for f in fields.values() if f["node_kind"] == "lifecycle")},
                {"kind": "state", "status": state["proof_state"], "field_count": 1},
                {"kind": "missile", "status": "external_verified" if rels[0]["target_kind"] == "missile" else "source_only", "field_count": 0},
            ],
            "relations": rels,
            "state_relation": state,
            "asset_dependencies": row_deps,
            "blockers": sorted(set(blockers)),
            "risk_rank_key": [0 if exposure == "evidence_pending" else 1 if exposure == "pc_only" else 2, -len(set(blockers)), sid],
        })
        deps.extend(row_deps)
    return out, deps


def validate_ir(ir: dict[str, Any]) -> None:
    rows = ir.get("rows", [])
    ids = [r.get("skill_id") for r in rows]
    if len(ids) != 242 or len(set(ids)) != 242:
        raise ValueError("IR must contain exactly 242 unique skill ids")
    idset = set(ids)
    graph: dict[int, list[int]] = defaultdict(list)
    for row in rows:
        for node in row.get("typed_nodes", []):
            if node.get("kind") not in NODE_KINDS:
                raise ValueError(f"unsupported typed node kind: {node.get('kind')}")
        for rel in row.get("relations", []):
            if rel.get("type") not in RELATION_TYPES:
                raise ValueError(f"unsupported relation type: {rel.get('type')}")
            if rel.get("target_kind") not in TARGET_KINDS:
                raise ValueError(f"unsupported relation target_kind: {rel.get('target_kind')}")
            target = int_value(rel.get("target_id"))
            if target in idset:
                graph[int(row["skill_id"])].append(target)
            if target and rel.get("proof_state") == "missing":
                raise ValueError(f"missing relation target: {row['skill_id']}->{target}")
        state = row.get("state_relation", {})
        if state.get("target_kind") not in STATE_KINDS:
            raise ValueError(f"unsupported state target_kind: {state.get('target_kind')}")
    visiting: set[int] = set()
    visited: set[int] = set()
    def dfs(node: int, path: list[int]) -> None:
        if node in visiting:
            raise ValueError("unbounded relation cycle: " + "->".join(map(str, path + [node])))
        if node in visited:
            return
        visiting.add(node)
        for nxt in graph.get(node, []):
            dfs(nxt, path + [node])
        visiting.remove(node)
        visited.add(node)
    for sid in sorted(idset):
        dfs(sid, [])


def reproducibility_message(artifact_hashes: dict[str, dict[str, Any]]) -> skillpb.ReproducibilityMetadata:
    return skillpb.ReproducibilityMetadata(
        protoc_version=protoc_version(),
        python_protobuf_version=google.protobuf.__version__,
        compiler_tool_revision=TOOL_REVISION,
        deterministic_build_time_utc=FIXED_BUILD_TIME_UTC,
        artifact_hashes=[
            skillpb.ArtifactHash(logical_path=name, sha256=meta["sha256"], size_bytes=meta["bytes"])
            for name, meta in sorted(artifact_hashes.items())
        ],
        double_run_required_by_cli=True,
    )


def serialize_proto(msg: Any) -> bytes:
    return msg.SerializeToString(deterministic=True)


def build_protobuf_artifacts(rows: list[dict[str, Any]], faction_order: list[dict[str, Any]], catalog_union_sha256: str, source_snapshot_id: str, slice_hash: str, base_hashes: dict[str, dict[str, Any]], summary: dict[str, Any]) -> dict[str, bytes]:
    policy = runtime_policy_message(catalog_union_sha256)
    factions = [faction_progression_message(f) for f in faction_order]
    repro = reproducibility_message(base_hashes)
    header = skillpb.CatalogHeader(
        schema_version=1,
        schema_id="content.v1.SkillCatalog",
        tool_revision=TOOL_REVISION,
        deterministic_build_time_utc=FIXED_BUILD_TIME_UTC,
        catalog_union_size=len(rows),
        catalog_union_sha256=catalog_union_sha256,
        source_snapshot_id=source_snapshot_id,
        skill_slice_sha256=slice_hash,
        static_field_count=summary["static_fields_verified_from_slice"],
        missile_rows_discoverable=summary["missile_rows_discoverable"],
        state_rows_discoverable=summary["state_rows_discoverable"],
        golden_ready_count=summary["golden_ready_count"],
    )
    full = skillpb.SkillCatalog(header=header, runtime_skill_policy=policy, factions=factions, reproducibility=repro)
    server = skillpb.ServerSkillCatalog(
        header=skillpb.ProjectionHeader(schema_version=1, projection_name="server", tool_revision=TOOL_REVISION, deterministic_build_time_utc=FIXED_BUILD_TIME_UTC, catalog_union_size=len(rows), catalog_union_sha256=catalog_union_sha256, source_snapshot_id=source_snapshot_id),
        runtime_skill_policy=policy,
        factions=factions,
        reproducibility=repro,
    )
    client = skillpb.ClientSkillCatalog(
        header=skillpb.ProjectionHeader(schema_version=1, projection_name="client", tool_revision=TOOL_REVISION, deterministic_build_time_utc=FIXED_BUILD_TIME_UTC, catalog_union_size=len(rows), catalog_union_sha256=catalog_union_sha256, source_snapshot_id=source_snapshot_id),
        runtime_skill_policy=policy,
        factions=factions,
        reproducibility=repro,
    )
    for row in rows:
        exposure = enum_value("EXPOSURE_STATE", row["exposure_state"])
        all_static = [static_field_message(name, data) for name, data in sorted(row["static_fields"].items())]
        presentation_static = [static_field_message(name, data) for name, data in sorted(row["static_fields"].items()) if data["node_kind"] == "presentation"]
        server_static = [static_field_message(name, data) for name, data in sorted(row["static_fields"].items()) if data["node_kind"] != "presentation"]
        rels = [relation_message(rel) for rel in row["relations"]]
        deps = [dependency_message(dep) for dep in sorted(row["asset_dependencies"], key=lambda d: (d["kind"], d["source_field"], d["source_path"]))]
        full.rows.append(skillpb.SkillRow(
            skill_id=row["skill_id"],
            skill_name=row["skill_name"],
            exposure_state=exposure,
            factions=[faction_message(f) for f in row["factions"]],
            static_fields=all_static,
            typed_nodes=[skillpb.TypedNode(kind=enum_value("NODE_KIND", n["kind"]), status=n["status"], field_count=n["field_count"]) for n in row["typed_nodes"]],
            relations=rels,
            state_relation=state_message(row["state_relation"]),
            asset_dependencies=deps,
            blockers=row["blockers"],
        ))
        server.rows.append(skillpb.ServerSkillRow(
            skill_id=row["skill_id"],
            skill_name=row["skill_name"],
            exposure_state=exposure,
            static_fields=server_static,
            relations=rels,
            blockers=row["blockers"],
        ))
        client.rows.append(skillpb.ClientSkillRow(
            skill_id=row["skill_id"],
            skill_name=row["skill_name"],
            exposure_state=exposure,
            factions=[faction_message(f) for f in row["factions"]],
            presentation_fields=presentation_static,
            state_relation=state_message(row["state_relation"]),
            asset_dependencies=deps,
            blockers=row["blockers"],
        ))
    server_bytes = serialize_proto(server)
    server.header.projection_sha256 = sha256_bytes(server_bytes)
    server_bytes = serialize_proto(server)
    client_bytes = serialize_proto(client)
    client.header.projection_sha256 = sha256_bytes(client_bytes)
    client_bytes = serialize_proto(client)
    return {
        "skill_port.catalog.pb": serialize_proto(full),
        "skill_port.server.pb": server_bytes,
        "skill_port.client.pb": client_bytes,
    }


def artifact_entry(logical_path: str, data: bytes, source_snapshot_id: str, kind: str = "config", media_type: str | None = None) -> dict[str, Any]:
    media = media_type or ("application/x-protobuf" if logical_path.endswith(".pb") else "application/json")
    return {
        "logicalPath": logical_path,
        "kind": kind,
        "mediaType": media,
        "sizeBytes": len(data),
        "sha256": sha256_bytes(data),
        "uri": f"urn:sha256:{sha256_bytes(data)}",
        "provenance": {
            "sourceSnapshotId": source_snapshot_id,
            "sourcePath": logical_path,
            "discoveryTool": "importer",
            "parserName": "scripts.skill_port.compiler",
            "parserVersion": TOOL_REVISION,
        },
    }


def build_artifacts(repo: Path, coverage_path: Path, presentation_path: Path, slice_path: Path, provenance_path: Path) -> dict[str, bytes]:
    coverage = load_json(coverage_path)
    presentation = load_json(presentation_path)
    provenance = load_json(provenance_path)
    slice_rows, slice_hash, slice_bytes_len = read_skill_slice(slice_path)
    union = validate_sources(coverage, presentation, provenance, slice_hash, slice_rows)
    rows, deps = build_rows(union, slice_rows, presentation)
    counts = Counter(r["exposure_state"] for r in rows)
    blocker_counts = Counter(b for r in rows for b in r["blockers"])
    dep_counts = Counter(d["status"] for d in deps)
    relation_counts = Counter((rel["type"], rel["target_kind"]) for r in rows for rel in r["relations"])
    golden = [r["skill_id"] for r in rows if r["exposure_state"] == "exposed" and not r["blockers"]]
    faction_order = [
        {
            "faction_index": f.get("faction_index"),
            "key": f.get("key"),
            "name": f.get("name"),
            "progression_skill_ids": f.get("pc_progression", {}).get("skill_ids", []),
            "progression_by_tier": f.get("pc_progression", {}).get("by_tier", {}),
            "skillbook_skill_ids": f.get("pc_skillbook", {}).get("skill_ids", []),
            "skillbook_by_level": f.get("pc_skillbook", {}).get("by_level", {}),
            "union_skill_ids": f.get("union_skill_ids", []),
            "unity_display_skill_ids": f.get("unity_display_skill_ids", []),
        }
        for f in sorted(coverage.get("factions", []), key=lambda f: int_value(f.get("faction_index")))
    ]
    ir = {
        "schema": SCHEMA_VERSION,
        "schema_version": 1,
        "tool_revision": TOOL_REVISION,
        "deterministic_build_time_utc": FIXED_BUILD_TIME_UTC,
        "integer_timebase": {"unit": "source integer ticks", "tick_rate_hz": None, "runtime_seconds_not_inferred": True},
        "field_schema": [{"name": n, "source_column": c, "kind": k} for n, c, k in FIELDS],
        "faction_progression_order": faction_order,
        "summary_counts": {
            "rows": len(rows),
            "static_fields_verified_from_slice": len(rows) * len(FIELDS),
            "missile_rows_discoverable": presentation.get("summary_counts", {}).get("source_counts", {}).get("missile_rows"),
            "state_rows_discoverable": presentation.get("summary_counts", {}).get("source_counts", {}).get("state_rows"),
            "relationship_classes": {";".join(k): v for k, v in sorted(relation_counts.items())},
            "exposure_state_counts": dict(sorted(counts.items())),
            "blocker_counts": dict(sorted(blocker_counts.items())),
            "asset_dependency_status_counts": dict(sorted(dep_counts.items())),
            "golden_ready_count": len(golden),
        },
        "rows": rows,
    }
    validate_ir(ir)
    source_paths = {
        "coverage_matrix": coverage_path,
        "presentation_inventory": presentation_path,
        "skill_slice": slice_path,
        "skill_slice_provenance": provenance_path,
    }
    provenance_doc = {
        "schema": "vltk.skill_port.provenance/v1",
        "tool_revision": TOOL_REVISION,
        "deterministic_build_time_utc": FIXED_BUILD_TIME_UTC,
        "source_snapshot": source_snapshot(repo, source_paths, coverage, provenance),
        "vltktool_provenance_validated": True,
        "canonical_static_rows_hash_policy": "never hash canonical skills.txt directly; accept only vltktool byte-preserving slice provenance",
        "slice": {"sha256": slice_hash, "bytes": slice_bytes_len, "rows": len(slice_rows)},
    }
    server = {
        "schema": "vltk.skill_port.server_projection/v1",
        "tool_revision": TOOL_REVISION,
        "faction_progression_order": faction_order,
        "rows": [
            {
                "skill_id": r["skill_id"],
                "skill_name": r["skill_name"],
                "exposure_state": r["exposure_state"],
                "static_fields": {k: v["value"] for k, v in r["static_fields"].items() if v["node_kind"] != "presentation"},
                "relations": r["relations"],
                "blockers": r["blockers"],
            } for r in rows
        ],
    }
    client = {
        "schema": "vltk.skill_port.client_projection/v1",
        "tool_revision": TOOL_REVISION,
        "faction_progression_order": faction_order,
        "rows": [
            {
                "skill_id": r["skill_id"],
                "skill_name": r["skill_name"],
                "exposure_state": r["exposure_state"],
                "factions": r["factions"],
                "presentation_fields": {k: v["value"] for k, v in r["static_fields"].items() if v["node_kind"] == "presentation"},
                "state_relation": r["state_relation"],
                "asset_dependencies": r["asset_dependencies"],
                "blockers": r["blockers"],
            } for r in rows
        ],
    }
    dependency = {
        "schema": "vltk.skill_port.dependency_manifest/v1",
        "tool_revision": TOOL_REVISION,
        "counts": dict(sorted(dep_counts.items())),
        "dependencies": sorted(deps, key=lambda d: (d["skill_id"], d["kind"], d["source_field"], d["source_path"])),
        "blockers": [
            "skill asset package.ini/KPakList winners not resolved in current evidence",
            "missing canonical assets are blockers; no fallback paths substituted",
        ],
    }
    index = {
        "schema": "vltk.skill_port.index/v1",
        "tool_revision": TOOL_REVISION,
        "counts": {
            "preserved_union_ids": len(rows),
            "exposed": counts.get("exposed", 0),
            "evidence_pending": counts.get("evidence_pending", 0),
            "pc_only": counts.get("pc_only", 0),
            "blocked_asset_provenance": sum(1 for r in rows if r["asset_dependencies"] or "presentation_lifecycle_source_only" in r["blockers"]),
            "golden_ready": len(golden),
        },
        "exposed": [r["skill_id"] for r in rows if r["exposure_state"] == "exposed"],
        "evidence_pending": [r["skill_id"] for r in rows if r["exposure_state"] == "evidence_pending"],
        "pc_only": [r["skill_id"] for r in rows if r["exposure_state"] == "pc_only"],
        "blocked_asset_provenance": [r["skill_id"] for r in rows if r["asset_dependencies"] or "presentation_lifecycle_source_only" in r["blockers"]],
        "golden_ready": golden,
        "risk_ranking": [
            {"rank": i + 1, "skill_id": r["skill_id"], "exposure_state": r["exposure_state"], "blocker_count": len(r["blockers"]), "blockers": r["blockers"]}
            for i, r in enumerate(sorted(rows, key=lambda r: tuple(r["risk_rank_key"])))
        ],
    }
    catalog_union_sha256 = sha256_bytes(canonical_json({"schema": "vltk.skill_port.union/v1", "skill_ids": sorted(union)}))
    source_snapshot_id = "skillport-gate0-dev-snapshot"
    manifest_source_snapshot = {
        "snapshotId": source_snapshot_id,
        "sourceRoot": "/var/www/jx-pc",
        "capturedAt": FIXED_BUILD_TIME_UTC,
        "treeSha256": sha256_bytes(canonical_json(provenance_doc["source_snapshot"])),
        "vcsRevision": "0000000-test-only-dev-artifact",
        "catalogGeneratorRevision": TOOL_REVISION,
    }
    adapter = {
        "schema": "vltk.skill_port.protobuf_adapter/v1",
        "tool_revision": TOOL_REVISION,
        "deterministic_json_schema": SCHEMA_VERSION,
        "protobuf_emitted": True,
        "protobuf_contract": "harness/specs/jx-pc-mobile-port/contracts/content/v1/skill_catalog.proto",
        "protobuf_package": "content.v1",
        "protoc_version": protoc_version(),
        "python_binding": "scripts/skill_port/gen/content/v1/skill_catalog_pb2.py",
        "strict_mapping": {
            "content.v1.SkillRow.skill_id": "uint32 from 242-row union",
            "content.v1.StaticField": "sint64 for integer/ticks; string only for source path evidence",
            "content.v1.LifecycleRelation": "typed child/start/fly/collide/vanish refs; missing proof remains blocker",
            "content.v1.ClientSkillRow.asset_dependencies": "client projection blockers only; no fallback path substitution",
        },
    }
    artifacts_obj = {
        "skill_port.ir.json": ir,
        "skill_port.provenance.json": provenance_doc,
        "skill_port.server.json": server,
        "skill_port.client.json": client,
        "skill_port.dependencies.json": dependency,
        "skill_port.index.json": index,
        "skill_port.protobuf_adapter.json": adapter,
    }
    artifacts = {name: canonical_json(obj) for name, obj in artifacts_obj.items()}
    manifest_entries = {name: {"sha256": sha256_bytes(data), "bytes": len(data)} for name, data in sorted(artifacts.items())}
    artifacts.update(build_protobuf_artifacts(rows, faction_order, catalog_union_sha256, source_snapshot_id, slice_hash, manifest_entries, ir["summary_counts"]))
    manifest_entries = {name: {"sha256": sha256_bytes(data), "bytes": len(data)} for name, data in sorted(artifacts.items())}
    client_projection_sha256 = manifest_entries["skill_port.client.pb"]["sha256"]
    reproducibility = {
        "schema": "vltk.skill_port.reproducibility/v1",
        "tool_revision": TOOL_REVISION,
        "deterministic_build_time_utc": FIXED_BUILD_TIME_UTC,
        "protoc_version": protoc_version(),
        "python_protobuf_version": google.protobuf.__version__,
        "artifact_hashes": manifest_entries,
        "double_run_required_by_cli": True,
    }
    artifacts["skill_port.reproducibility.json"] = canonical_json(reproducibility)
    release_gate = {
        "schema": "vltk.skill_port.release_gate/v1",
        "tool_revision": TOOL_REVISION,
        "signing_status": "development-test-only",
        "signing_key_id": TEST_ONLY_SIGNING_KEY_ID,
        "signing_public_key_base64": test_only_public_key_b64(),
        "production_allowed": False,
        "production_blocker": f"production release gate rejects signingKeyId {TEST_ONLY_SIGNING_KEY_ID}",
        "forbidden_production_key_ids": sorted(PRODUCTION_FORBIDDEN_SIGNING_KEY_IDS),
    }
    artifacts["skill_port.release_gate.json"] = canonical_json(release_gate)
    manifest_entries = {name: {"sha256": sha256_bytes(data), "bytes": len(data)} for name, data in sorted(artifacts.items())}
    manifest = {
        "schemaVersion": MANIFEST_SCHEMA_VERSION,
        "releaseId": CONTENT_RELEASE_ID,
        "realmId": REALM_ID,
        "version": MANIFEST_VERSION,
        "createdAt": FIXED_BUILD_TIME_UTC,
        "userFacingLocale": "vi",
        "hotReloadAllowed": False,
        "sourceSnapshot": manifest_source_snapshot,
        "luaPolicy": {
            "runtime": "Lua 5.1",
            "sandboxPolicyVersion": "skill-port-no-lua-runtime-v1",
            "hostApiWhitelist": ["SkillPort.ReadOnlyCatalog"],
            "hostApiWhitelistSha256": sha256_bytes(canonical_json(["SkillPort.ReadOnlyCatalog"])),
        },
        "signingKeyId": TEST_ONLY_SIGNING_KEY_ID,
        "artifacts": [artifact_entry(name, data, source_snapshot_id, kind="binary" if name.endswith(".pb") else "config") for name, data in sorted(artifacts.items())],
        "contentDigest": {
            "contentReleaseId": CONTENT_RELEASE_ID,
            "manifestSha256": "0" * 64,
            "sourceSnapshotId": source_snapshot_id,
            "catalogUnionSize": len(rows),
            "catalogUnionSha256": catalog_union_sha256,
            "clientProjectionSha256": client_projection_sha256,
            "runtimeSkillPolicyId": RUNTIME_SKILL_POLICY_ID,
        },
        "runtimeSkillPolicy": {
            "policyId": RUNTIME_SKILL_POLICY_ID,
            "catalogUnionSize": len(rows),
            "catalogUnionSha256": catalog_union_sha256,
            "sourceTool": "vltktool",
            "filesystemFallbackAllowed": False,
            "runtimeParityClaimed": False,
            "pcRuntimeEvidenceStatus": "BLOCKED",
            "androidPhysicalEvidenceStatus": "BLOCKED",
        },
        "manifestSha256": "0" * 64,
        "signature": "",
    }
    manifest_sha = sha256_bytes(canonical_json(manifest_hash_payload(manifest)))
    manifest["manifestSha256"] = manifest_sha
    manifest["contentDigest"]["manifestSha256"] = manifest_sha
    manifest["signature"] = sign_test_only(canonical_json(manifest_signing_payload(manifest)))
    artifacts["manifest.json"] = canonical_json(manifest)
    return artifacts


def compile_catalog(repo: Path, output_dir: Path, *, coverage: Path, presentation: Path, skill_slice: Path, provenance: Path, write: bool, check: bool) -> dict[str, Any]:
    first = build_artifacts(repo, coverage, presentation, skill_slice, provenance)
    second = build_artifacts(repo, coverage, presentation, skill_slice, provenance)
    if first != second:
        raise ValueError("non-deterministic compiler output across double run")
    if write:
        output_dir.mkdir(parents=True, exist_ok=True)
        for name, data in sorted(first.items()):
            (output_dir / name).write_bytes(data)
    if check:
        missing = [name for name in first if not (output_dir / name).is_file()]
        if missing:
            raise ValueError("missing generated artifacts: " + ", ".join(sorted(missing)))
        stale = [name for name, data in first.items() if (output_dir / name).read_bytes() != data]
        if stale:
            raise ValueError("stale generated artifacts: " + ", ".join(sorted(stale)))
    manifest = json.loads(first["manifest.json"].decode("utf-8"))
    manifest["summary_counts"] = json.loads(first["skill_port.ir.json"].decode("utf-8"))["summary_counts"]
    manifest["double_run_hash_equal"] = True
    return manifest


def main(argv: list[str] | None = None) -> int:
    repo_default = Path(__file__).resolve().parents[2]
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", default=str(repo_default))
    parser.add_argument("--output-dir", default=str(repo_default / DEFAULT_OUT))
    parser.add_argument("--coverage", default=str(repo_default / "harness/docs/stories/SKL-ALL-PARITY-001/coverage-matrix.json"))
    parser.add_argument("--presentation", default=str(repo_default / "Assets/StreamingAssets/Reference/PcAllFactionPresentationInventory.json"))
    parser.add_argument("--skill-slice", default=str(repo_default / "Assets/StreamingAssets/Reference/PcAllFactionLearnedDisplaySkills.txt"))
    parser.add_argument("--provenance", default=str(repo_default / "Assets/StreamingAssets/Reference/PcAllFactionLearnedDisplaySkills.provenance.json"))
    parser.add_argument("--check", action="store_true", help="verify generated artifacts are current")
    parser.add_argument("--no-write", action="store_true", help="compile in memory only")
    args = parser.parse_args(argv)
    repo = Path(args.repo)
    try:
        manifest = compile_catalog(
            repo,
            Path(args.output_dir),
            coverage=Path(args.coverage),
            presentation=Path(args.presentation),
            skill_slice=Path(args.skill_slice),
            provenance=Path(args.provenance),
            write=not args.no_write and not args.check,
            check=args.check,
        )
    except ValueError as exc:
        print(f"skill_port compiler failed: {exc}", file=sys.stderr)
        return 2
    counts = manifest["summary_counts"]
    artifact_hashes = {a["logicalPath"]: a for a in manifest["artifacts"]}
    print(
        "skill_port compiler OK: "
        f"rows={counts['rows']} fields={counts['static_fields_verified_from_slice']} "
        f"missiles={counts['missile_rows_discoverable']} states={counts['state_rows_discoverable']} "
        f"golden_ready={counts['golden_ready_count']} sha256={artifact_hashes['skill_port.ir.json']['sha256']}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
