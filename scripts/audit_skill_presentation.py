#!/usr/bin/env python3
"""Deterministic PC-first presentation inventory for player-facing skill rows.

Builds one row per learned/display union skill id, joins canonical skill slice
metadata with missile/state source tables, and records only static Unity source
inspection. No combat/runtime behavior changes.
"""

from __future__ import annotations

import argparse
import csv
import hashlib
import json
import re
import sys
from collections import Counter
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import audit_skill_coverage as coverage  # type: ignore

SCHEMA = "vltk.pc.presentation-inventory/v1"
PC_MISSILE_AUDIT = Path("Assets/StreamingAssets/Reference/PcMissileSourceAudit.json")
PC_MISSILE_TABLE = Path("Assets/StreamingAssets/Reference/PcAttrib/missles1.txt")
PC_FULL_SKILL_TABLE = Path("Assets/StreamingAssets/Reference/PcSkill/skills1_full.txt")
STATE_VISUAL_TABLE = Path("Assets/StreamingAssets/Reference/PcAttrib/state_visual_mapping.txt")
OUTPUT_DEFAULT = Path("Assets/StreamingAssets/Reference/PcAllFactionPresentationInventory.json")
PACKAGED_SLICE_DEFAULT = Path("Assets/Resources/Reference/PcAllFactionLearnedDisplaySkills.bytes")

UNITY_REFS = {
    "skill_style": [
        {"path": "Assets/Scripts/Core/PcConfigParser.cs", "lines": "228-228"},
        {"path": "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs", "lines": "175-175"},
    ],
    "state_special_id": [
        {"path": "Assets/Scripts/Core/PcConfigParser.cs", "lines": "250-250"},
        {"path": "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs", "lines": "176-176"},
    ],
    "is_aura": [
        {"path": "Assets/Scripts/Core/PcConfigParser.cs", "lines": "252-252"},
        {"path": "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs", "lines": "177-177"},
    ],
    "attack_radius": [
        {"path": "Assets/Scripts/Core/PcConfigParser.cs", "lines": "254-254"},
        {"path": "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs", "lines": "178-178"},
    ],
    "missiles_generate": [
        {"path": "Assets/Scripts/Core/PcConfigParser.cs", "lines": "256-256"},
        {"path": "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs", "lines": "179-179"},
    ],
    "missiles_generate_data": [
        {"path": "Assets/Scripts/Core/PcConfigParser.cs", "lines": "257-257"},
        {"path": "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs", "lines": "180-180"},
    ],
    "missile_form": [
        {"path": "Assets/Scripts/Core/PcConfigParser.cs", "lines": "260-260"},
        {"path": "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs", "lines": "181-182"},
    ],
    "child_skill_id": [
        {"path": "Assets/Scripts/Core/PcConfigParser.cs", "lines": "261-262"},
        {"path": "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs", "lines": "183-185"},
        {"path": "Assets/Scripts/Sandbox/MissileSpawner.cs", "lines": "55-64"},
    ],
    "child_skill_level": [
        {"path": "Assets/Scripts/Core/PcConfigParser.cs", "lines": "298-298"},
        {"path": "Assets/Scripts/Model/SkillDefinition.cs", "lines": "59-59"},
        {"path": "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs", "lines": "184-184"},
    ],
    "child_skill_num": [
        {"path": "Assets/Scripts/Core/PcConfigParser.cs", "lines": "263-263"},
        {"path": "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs", "lines": "185-185"},
    ],
    "base_skill": [
        {"path": "Assets/Scripts/Core/PcConfigParser.cs", "lines": "264-264"},
        {"path": "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs", "lines": "186-186"},
    ],
    "char_anim_id": [
        {"path": "Assets/Scripts/Core/PcConfigParser.cs", "lines": "265-265"},
        {"path": "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs", "lines": "187-187"},
        {"path": "Assets/Scripts/Sandbox/MalePlayerSpriteCatalog.cs", "lines": "317-331"},
    ],
    "wait_time": [
        {"path": "Assets/Scripts/Core/PcConfigParser.cs", "lines": "268-268"},
        {"path": "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs", "lines": "189-189"},
    ],
    "skill_cost_type": [
        {"path": "Assets/Scripts/Core/PcConfigParser.cs", "lines": "270-270"},
        {"path": "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs", "lines": "190-190"},
    ],
    "cost_value": [
        {"path": "Assets/Scripts/Core/PcConfigParser.cs", "lines": "271-271"},
        {"path": "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs", "lines": "191-191"},
    ],
    "time_per_cast": [
        {"path": "Assets/Scripts/Core/PcConfigParser.cs", "lines": "272-272"},
        {"path": "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs", "lines": "192-192"},
    ],
    "time_per_cast_on_horse": [
        {"path": "Assets/Scripts/Core/PcConfigParser.cs", "lines": "273-273"},
        {"path": "Assets/Scripts/Model/SkillDefinition.cs", "lines": "46-46"},
    ],
    "is_physical": [
        {"path": "Assets/Scripts/Core/PcConfigParser.cs", "lines": "274-274"},
        {"path": "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs", "lines": "193-193"},
    ],
    "target_only": [
        {"path": "Assets/Scripts/Core/PcConfigParser.cs", "lines": "275-275"},
        {"path": "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs", "lines": "194-194"},
    ],
    "target_enemy": [
        {"path": "Assets/Scripts/Core/PcConfigParser.cs", "lines": "276-276"},
        {"path": "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs", "lines": "195-195"},
    ],
    "target_ally": [
        {"path": "Assets/Scripts/Core/PcConfigParser.cs", "lines": "277-277"},
        {"path": "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs", "lines": "196-196"},
    ],
    "target_self": [
        {"path": "Assets/Scripts/Core/PcConfigParser.cs", "lines": "278-278"},
        {"path": "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs", "lines": "197-197"},
    ],
    "target_obj": [
        {"path": "Assets/Scripts/Core/PcConfigParser.cs", "lines": "280-280"},
        {"path": "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs", "lines": "198-198"},
    ],
    "by_missile": [
        {"path": "Assets/Scripts/Core/PcConfigParser.cs", "lines": "282-282"},
        {"path": "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs", "lines": "199-199"},
    ],
    "is_use_attack_rating": [
        {"path": "Assets/Scripts/Core/PcConfigParser.cs", "lines": "283-283"},
        {"path": "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs", "lines": "200-200"},
    ],
    "req_level": [
        {"path": "Assets/Scripts/Core/PcConfigParser.cs", "lines": "285-286"},
        {"path": "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs", "lines": "201-202"},
    ],
    "max_level": [
        {"path": "Assets/Scripts/Core/PcConfigParser.cs", "lines": "286-286"},
        {"path": "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs", "lines": "202-202"},
    ],
    "equip_limit": [
        {"path": "Assets/Scripts/Core/PcConfigParser.cs", "lines": "287-288"},
        {"path": "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs", "lines": "203-203"},
    ],
    "horse_limit": [
        {"path": "Assets/Scripts/Core/PcConfigParser.cs", "lines": "289-289"},
        {"path": "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs", "lines": "204-204"},
    ],
    "do_hurt": [
        {"path": "Assets/Scripts/Core/PcConfigParser.cs", "lines": "290-290"},
        {"path": "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs", "lines": "205-205"},
    ],
    "weapon_skill": [
        {"path": "Assets/Scripts/Core/PcConfigParser.cs", "lines": "291-291"},
        {"path": "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs", "lines": "206-206"},
    ],
    "start_skill_id": [
        {"path": "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs", "lines": "207-207"},
        {"path": "Assets/Scripts/Model/SkillDefinition.cs", "lines": "109-110"},
    ],
    "fly_skill_id": [
        {"path": "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs", "lines": "208-209"},
        {"path": "Assets/Scripts/Model/SkillDefinition.cs", "lines": "106-108"},
    ],
    "collid_skill_id": [
        {"path": "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs", "lines": "210-211"},
        {"path": "Assets/Scripts/Model/SkillDefinition.cs", "lines": "102-105"},
    ],
    "vanished_skill_id": [
        {"path": "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs", "lines": "212-212"},
        {"path": "Assets/Scripts/Model/SkillDefinition.cs", "lines": "104-105"},
    ],
    "pre_cast_spr_path": [
        {"path": "Assets/Scripts/Core/PcConfigParser.cs", "lines": "235-247"},
        {"path": "Assets/Scripts/Model/SkillDefinition.cs", "lines": "116-116"},
    ],
    "man_cast_snd_path": [
        {"path": "Assets/Scripts/Core/PcConfigParser.cs", "lines": "237-237"},
        {"path": "Assets/Scripts/Model/SkillDefinition.cs", "lines": "124-124"},
    ],
    "fm_cast_snd_path": [
        {"path": "Assets/Scripts/Core/PcConfigParser.cs", "lines": "239-239"},
        {"path": "Assets/Scripts/Model/SkillDefinition.cs", "lines": "125-125"},
    ],
}

FIELD_SPECS = [
    ("skill_style", "SkillStyle"),
    ("state_special_id", "StateSpecialId"),
    ("is_aura", "IsAura"),
    ("attack_radius", "AttackRadius"),
    ("missiles_generate", "MslsGenerate"),
    ("missiles_generate_data", "MslsGenerateData"),
    ("missile_form", "MisslesForm"),
    ("child_skill_id", "ChildSkillId"),
    ("child_skill_level", "ChildSkillLevel"),
    ("child_skill_num", "ChildSkillNum"),
    ("base_skill", "BaseSkill"),
    ("char_anim_id", "CharAnimId"),
    ("wait_time", "WaitTime"),
    ("skill_cost_type", "SkillCostType"),
    ("cost_value", "CostValue"),
    ("time_per_cast", "TimePerCast"),
    ("time_per_cast_on_horse", "TimePerCastOnHorse"),
    ("is_physical", "IsPhysical"),
    ("target_only", "TargetOnly"),
    ("target_enemy", "TargetEnemy"),
    ("target_ally", "TargetAlly"),
    ("target_self", "TargetSelf"),
    ("target_obj", "TargetObj"),
    ("by_missile", "ByMissle"),
    ("is_use_attack_rating", "IsUseAR"),
    ("req_level", "ReqLevel"),
    ("max_level", "MaxLevel"),
    ("equip_limit", "EqtLimit"),
    ("horse_limit", "HorseLimit"),
    ("do_hurt", "DoHurt"),
    ("weapon_skill", "WeaponSkill"),
    ("start_skill_id", "StartSkillId"),
    ("fly_skill_id", "FlySkillId"),
    ("collid_skill_id", "CollidSkillId"),
    ("vanished_skill_id", "VanishedSkillId"),
    ("pre_cast_spr_path", "PreCastSpr"),
    ("man_cast_snd_path", "ManCastSnd"),
    ("fm_cast_snd_path", "FMCastSnd"),
]

PARSER_PATH = "Assets/Scripts/Core/PcConfigParser.cs"
FACTORY_PATH = "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs"
PARSER_METHOD = "PcConfigParser.ParseSkillsLines"
FACTORY_METHOD = "PcCombatCatalogFactory.ApplyCaiBangPcStaticRows"

FIELD_PROPERTIES = {
    "skill_style": "skillStyle",
    "state_special_id": "stateSpecialId",
    "is_aura": "isAura",
    "attack_radius": "attackRadius",
    "missiles_generate": "missilesGenerate",
    "missiles_generate_data": "missilesGenerateData",
    "missile_form": "missileForm",
    "child_skill_id": "childSkillId",
    "child_skill_level": "childSkillLevel",
    "child_skill_num": "childSkillNum",
    "base_skill": "baseSkill",
    "char_anim_id": "charAnimId",
    "wait_time": "waitTime",
    "skill_cost_type": "skillCostType",
    "cost_value": "cost",
    "time_per_cast": "timePerCast",
    "time_per_cast_on_horse": "timePerCastOnHorse",
    "is_physical": "isPhysical",
    "target_only": "targetOnly",
    "target_enemy": "targetEnemy",
    "target_ally": "targetAlly",
    "target_self": "targetSelf",
    "target_obj": "targetObj",
    "by_missile": "byMissile",
    "is_use_attack_rating": "isUseAttackRating",
    "req_level": "reqLevel",
    "max_level": "maxLevel",
    "equip_limit": "equipLimit",
    "horse_limit": "horseLimit",
    "do_hurt": "doHurt",
    "weapon_skill": "weaponSkill",
    "start_skill_id": "startSkillId",
    "fly_skill_id": "flySkillId",
    "collid_skill_id": "collideSkillId",
    "vanished_skill_id": "vanishSkillId",
    "pre_cast_spr_path": "effectSourceId",
    "man_cast_snd_path": "manCastSndPath",
    "fm_cast_snd_path": "fmCastSndPath",
}

PARSER_SOURCE_INDEX = {
    "skill_style": 4,
    "pre_cast_spr_path": 6,
    "man_cast_snd_path": 7,
    "fm_cast_snd_path": 8,
    "state_special_id": 9,
    "is_aura": 11,
    "attack_radius": 14,
    "missiles_generate": 16,
    "missiles_generate_data": 17,
    "missile_form": 19,
    "child_skill_id": 20,
    "child_skill_level": 21,
    "child_skill_num": 22,
    "base_skill": 23,
    "char_anim_id": 24,
}

EVENT_FIELD_SPECS = {"start_skill_id", "fly_skill_id", "collid_skill_id", "vanished_skill_id"}

UNITY_FIELD_BINDINGS = {
    field: {
        "field": field,
        "path": FACTORY_PATH if field in EVENT_FIELD_SPECS else PARSER_PATH,
        "owner_method": FACTORY_METHOD if field in EVENT_FIELD_SPECS else PARSER_METHOD,
        "method_name": "ApplyCaiBangPcStaticRows" if field in EVENT_FIELD_SPECS else "ParseSkillsLines",
        "property": FIELD_PROPERTIES[field],
        "column": column,
        "match_kind": (
            "factory_local_to_field" if field == "collid_skill_id" else
            "factory_value_to_field" if field in EVENT_FIELD_SPECS else
            "parser_local_to_effect_source" if field == "pre_cast_spr_path" else
            "parser_direct_field"
        ),
        **({"source_index": PARSER_SOURCE_INDEX[field]} if field in PARSER_SOURCE_INDEX else {}),
    }
    for field, column in FIELD_SPECS
}

STATE_STUB_NO_BYTES_IDS = set(range(52, 65))
STATE_PC_ABSENT_NO_VISUAL_IDS = {65, 66, 120, 122}


def sha256_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def int_val(raw: str | None) -> int:
    raw = (raw or "").strip()
    if not raw or raw == "-":
        return 0
    try:
        return int(raw)
    except ValueError:
        return 0


def safe_str(raw: str | None) -> str:
    return (raw or "").strip()


def ref(path: str, line: int | None = None, **extra) -> dict:
    out = {"path": path}
    if line is not None:
        out["line"] = line
    out.update(extra)
    return out


def repo_relpath(path: Path, repo: Path) -> str:
    if path.is_absolute():
        try:
            return path.relative_to(repo).as_posix()
        except ValueError:
            return path.as_posix()
    return path.as_posix()


def _lex_csharp(source: str) -> tuple[str, dict[str, str]]:
    out: list[str] = []
    strings: dict[str, str] = {}
    i = 0
    n = len(source)
    while i < n:
        ch = source[i]
        nxt = source[i + 1] if i + 1 < n else ""
        if ch == "/" and nxt == "/":
            i += 2
            while i < n and source[i] not in "\r\n":
                i += 1
            continue
        if ch == "/" and nxt == "*":
            i += 2
            while i + 1 < n and not (source[i] == "*" and source[i + 1] == "/"):
                out.append("\n" if source[i] in "\r\n" else " ")
                i += 1
            i += 2
            continue
        if ch in {"'", '"'} or (ch in {"@", "$"} and nxt in {"'", '"', "@", "$"}):
            prefix = ""
            while i < n and source[i] in {"@", "$"}:
                prefix += source[i]
                i += 1
            if i >= n or source[i] not in {"'", '"'}:
                out.append(prefix)
                continue
            quote = source[i]
            verbatim = "@" in prefix
            i += 1
            value: list[str] = []
            while i < n:
                c = source[i]
                if verbatim and c == quote and i + 1 < n and source[i + 1] == quote:
                    value.append(quote)
                    i += 2
                    continue
                if not verbatim and c == "\\" and i + 1 < n:
                    value.append(source[i + 1])
                    i += 2
                    continue
                if c == quote:
                    i += 1
                    break
                value.append(c)
                i += 1
            token = f"__STR{len(strings)}__"
            strings[token] = "".join(value)
            out.append(token)
            continue
        out.append(ch)
        i += 1
    return "".join(out), strings


def _method_body(stripped: str, method_name: str) -> str | None:
    for match in re.finditer(rf"\b{re.escape(method_name)}\s*\(", stripped):
        pos = match.end() - 1
        depth = 0
        while pos < len(stripped):
            if stripped[pos] == "(":
                depth += 1
            elif stripped[pos] == ")":
                depth -= 1
                if depth == 0:
                    break
            pos += 1
        pos += 1
        while pos < len(stripped) and stripped[pos].isspace():
            pos += 1
        if pos >= len(stripped) or stripped[pos] != "{":
            continue
        start = pos + 1
        depth = 1
        pos += 1
        while pos < len(stripped) and depth:
            if stripped[pos] == "{":
                depth += 1
            elif stripped[pos] == "}":
                depth -= 1
            pos += 1
        if depth == 0:
            return stripped[start:pos - 1]
    return None


def _header_column_vars(body: str, strings: dict[str, str]) -> dict[str, str]:
    out = {}
    for match in re.finditer(r"\bint\s+(\w+)\s*=\s*HeaderCol\s*\(\s*headerIndex\s*,\s*(__STR\d+__)\s*\)", body):
        out[match.group(1)] = strings.get(match.group(2), "")
    return out


def _value_column(match: re.Match, strings: dict[str, str]) -> str:
    return strings.get(match.group(1), "")


def _statement_source_index(statement: str, ci: int, header_vars: dict[str, str], expected_column: str) -> int | None:
    explicit = re.search(r"\b(?:IntColSafe|ColSafe)\s*\(\s*cols\s*,\s*(\d+)\s*\)", statement)
    if explicit:
        return int(explicit.group(1))
    if "ref ci" in statement or re.search(r"\bColSafe\s*\(\s*cols\s*,\s*ci\s*\)", statement):
        return ci
    for var, column in header_vars.items():
        if column == expected_column and re.search(rf"\b{re.escape(var)}\b", statement):
            return -1
    return None


def _parser_direct_match(body: str, strings: dict[str, str], spec: dict) -> dict | None:
    ci = 0
    header_vars = _header_column_vars(body, strings)
    local_sources: dict[str, int] = {}
    for raw in re.split(r";", body):
        statement = raw.strip()
        if not statement:
            continue
        local_assign = re.search(r"\b(?:int|string)\s+(\w+)\s*=\s*(.+)", statement, re.S)
        if local_assign:
            source_index = _statement_source_index(local_assign.group(2), ci, header_vars, spec["column"])
            if source_index is not None:
                local_sources[local_assign.group(1)] = source_index
        assign = re.search(rf"\bskill\.{re.escape(spec['property'])}\s*=\s*(.+)", statement, re.S)
        if assign:
            source_index = _statement_source_index(statement, ci, header_vars, spec["column"])
            if source_index is None:
                rhs_local = re.search(r"\b(\w+)\b", assign.group(1))
                if rhs_local:
                    source_index = local_sources.get(rhs_local.group(1))
            if "source_index" in spec:
                if source_index != spec["source_index"]:
                    return None
            elif source_index != -1:
                return None
            return {
                "status": "verified",
                "path": spec["path"],
                "owner_method": spec["owner_method"],
                "lhs": f"skill.{spec['property']}",
                "source_column": spec["column"],
                "match_kind": spec["match_kind"],
                **({"source_index": source_index} if source_index is not None and source_index >= 0 else {}),
            }
        ci += len(re.findall(r"\bref\s+ci\b", statement))
        for inc in re.finditer(r"\bci\s*\+\+", statement):
            ci += 1
        plus = re.search(r"\bci\s*\+=\s*(\d+)", statement)
        if plus:
            ci += int(plus.group(1))
        set_ci = re.search(r"\bci\s*=\s*(\d+)", statement)
        if set_ci:
            ci = int(set_ci.group(1))
    return None


def _parser_precast_match(body: str, spec: dict) -> dict | None:
    ci = 0
    local = None
    for raw in re.split(r";", body):
        statement = raw.strip()
        local_match = re.search(r"\bstring\s+(\w+)\s*=\s*ColSafe\s*\(\s*cols\s*,\s*ci\s*\)", statement)
        if local_match and ci == spec["source_index"]:
            local = local_match.group(1)
        ci += len(re.findall(r"\bref\s+ci\b", statement))
        for inc in re.finditer(r"\bci\s*\+\+", statement):
            ci += 1
        plus = re.search(r"\bci\s*\+=\s*(\d+)", statement)
        if plus:
            ci += int(plus.group(1))
        set_ci = re.search(r"\bci\s*=\s*(\d+)", statement)
        if set_ci:
            ci = int(set_ci.group(1))
    if not local:
        return None
    pattern = rf"\bskill\.effectSourceId\s*=\s*new\s+SourceAssetId\b[\s\S]*?\bsourcePath\s*=\s*{re.escape(local)}\b"
    if not re.search(pattern, body):
        return None
    return {
        "status": "verified",
        "path": spec["path"],
        "owner_method": spec["owner_method"],
        "lhs": "skill.effectSourceId.sourcePath",
        "source_column": spec["column"],
        "match_kind": spec["match_kind"],
        "source_index": spec["source_index"],
        "local": local,
    }


def _factory_value_match(body: str, strings: dict[str, str], spec: dict) -> dict | None:
    pattern = rf"\bskill\.{re.escape(spec['property'])}\s*=\s*[^;]*?\bValue\s*\(\s*row\s*,\s*(__STR\d+__)"
    match = re.search(pattern, body, re.S)
    if not match or _value_column(match, strings) != spec["column"]:
        return None
    return {
        "status": "verified",
        "path": spec["path"],
        "owner_method": spec["owner_method"],
        "lhs": f"skill.{spec['property']}",
        "source_column": spec["column"],
        "match_kind": spec["match_kind"],
    }


def _factory_local_match(body: str, strings: dict[str, str], spec: dict) -> dict | None:
    for match in re.finditer(r"\bint\s+(\w+)\s*=\s*Value\s*\(\s*row\s*,\s*(__STR\d+__)", body):
        local = match.group(1)
        if strings.get(match.group(2), "") != spec["column"]:
            continue
        if re.search(rf"\bskill\.{re.escape(spec['property'])}\s*=\s*{re.escape(local)}\b", body):
            return {
                "status": "verified",
                "path": spec["path"],
                "owner_method": spec["owner_method"],
                "lhs": f"skill.{spec['property']}",
                "source_column": spec["column"],
                "match_kind": spec["match_kind"],
                "local": local,
            }
    return None


def match_unity_field_binding(path: str, source: str, spec: dict) -> dict | None:
    stripped, strings = _lex_csharp(source)
    body = _method_body(stripped, spec["method_name"])
    if body is None:
        return None
    if spec["match_kind"] == "parser_local_to_effect_source":
        proof = _parser_precast_match(body, spec)
    elif spec["match_kind"] == "factory_value_to_field":
        proof = _factory_value_match(body, strings, spec)
    elif spec["match_kind"] == "factory_local_to_field":
        proof = _factory_local_match(body, strings, spec)
    else:
        proof = _parser_direct_match(body, strings, spec)
    if proof:
        proof["path"] = path
    return proof


def resolve_unity_bindings(repo: Path) -> dict[str, list[dict]]:
    cache: dict[str, tuple[str, str]] = {}
    out: dict[str, list[dict]] = {}
    missing: list[str] = []
    for field, spec in UNITY_FIELD_BINDINGS.items():
        path = spec["path"]
        if path not in cache:
            file_path = repo / path
            cache[path] = (file_path.read_text(encoding="utf-8"), sha256_bytes(file_path.read_bytes()))
        source, digest = cache[path]
        proof = match_unity_field_binding(path, source, spec)
        if not proof:
            missing.append(field)
            continue
        proof["sha256"] = digest
        out[field] = [proof]
    if missing:
        raise SystemExit("Unity field binding proof missing: " + ", ".join(sorted(missing)))
    return out


def unity_ref(field: str, verified: dict[str, list[dict]]) -> list[dict]:
    return [dict(item) for item in verified.get(field, [])]


def load_missile_audit(path: Path) -> dict:
    audit = json.loads(path.read_text(encoding="utf-8"))
    for item in audit.get("sources", []):
        if item.get("label") == "repo-pcattrib/missles1.txt":
            return {
                "audit_path": str(path),
                "audit_sha256": sha256_bytes(path.read_bytes()),
                "source_path": item["path"],
                "source_sha256": item["sha256"],
                "header_sha256": item["headerSha256"],
                "expected_header_columns": item["headerColumns"],
                "duplicate_policy": audit.get("notes", [""])[3],
                "runtime_loader": audit.get("notes", [""])[0],
                "runtime_claim": audit.get("runtimeClaim", False),
                "duplicate_ids": item.get("duplicateMissileIds", []),
                "unique_ids": item.get("uniqueIds", 0),
                "parsed_ids": item.get("parsedIds", 0),
                "physical_lines": item.get("physicalLines", 0),
                "data_rows": item.get("dataRows", 0),
            }
    raise SystemExit(f"missing repo-pcattrib/missles1.txt entry in {path}")


def parse_missile_table(path: Path, expected_sha256: str) -> dict[int, dict]:
    data = path.read_bytes()
    actual_sha256 = sha256_bytes(data)
    text = data.decode("latin-1")
    lines = text.splitlines()
    if len(lines) < 2:
        raise SystemExit(f"missles1 table too small: {path}")
    header = lines[0].split("\t")
    col = {name: idx for idx, name in enumerate(header)}
    rows: dict[int, dict] = {}
    line_by_id: dict[int, int] = {}
    for lineno, line in enumerate(lines[1:], start=2):
        if not line.strip():
            continue
        cells = line.split("\t")
        missile_id = int_val(cells[col.get("MissleId", 0)] if col else cells[0])
        if missile_id <= 0:
            continue
        entry = {
            "missile_id": missile_id,
            "line": lineno,
            "move_kind": int_val(cells[col.get("MoveKind", 2)] if col else cells[2] if len(cells) > 2 else None),
            "follow_kind": int_val(cells[col.get("FollowKind", 3)] if col else cells[3] if len(cells) > 3 else None),
            "col_follow_target": int_val(cells[col.get("ColFollowTarget", 4)] if col else cells[4] if len(cells) > 4 else None),
            "missle_height": int_val(cells[col.get("MissleHeight", 5)] if col else cells[5] if len(cells) > 5 else None),
            "collid_range": int_val(cells[col.get("CollidRange", 6)] if col else cells[6] if len(cells) > 6 else None),
            "is_range_dmg": int_val(cells[col.get("IsRangeDmg", 7)] if col else cells[7] if len(cells) > 7 else None),
            "dmg_range": int_val(cells[col.get("DmgRange", 8)] if col else cells[8] if len(cells) > 8 else None),
            "dmg_interval": int_val(cells[col.get("DmgInterval", 9)] if col else cells[9] if len(cells) > 9 else None),
            "lifetime": int_val(cells[col.get("LifeTime", 10)] if col else cells[10] if len(cells) > 10 else None),
            "speed": int_val(cells[col.get("Speed", 11)] if col else cells[11] if len(cells) > 11 else None),
            "zspeed": int_val(cells[col.get("Zspeed", 12)] if col else cells[12] if len(cells) > 12 else None),
            "zacc": int_val(cells[col.get("Zacc", 13)] if col else cells[13] if len(cells) > 13 else None),
            "loop_play": int_val(cells[col.get("LoopPlay", 14)] if col else cells[14] if len(cells) > 14 else None),
            "sub_loop": int_val(cells[col.get("SubLoop", 15)] if col else cells[15] if len(cells) > 15 else None),
            "sub_start": int_val(cells[col.get("SubStart", 16)] if col else cells[16] if len(cells) > 16 else None),
            "sub_stop": int_val(cells[col.get("SubStop", 17)] if col else cells[17] if len(cells) > 17 else None),
            "response_skill": int_val(cells[col.get("ResponseSkill", 18)] if col else cells[18] if len(cells) > 18 else None),
            "can_destroy": int_val(cells[col.get("CanDestroy", 19)] if col else cells[19] if len(cells) > 19 else None),
            "col_vanish": int_val(cells[col.get("ColVanish", 20)] if col else cells[20] if len(cells) > 20 else None),
            "can_slow": int_val(cells[col.get("CanSlow", 21)] if col else cells[21] if len(cells) > 21 else None),
            "can_col_friend": int_val(cells[col.get("CanColFriend", 22)] if col else cells[22] if len(cells) > 22 else None),
            "auto_explode": int_val(cells[col.get("AutoExplode", 23)] if col else cells[23] if len(cells) > 23 else None),
            "red_lum": int_val(cells[col.get("RedLum", 53)] if col else cells[53] if len(cells) > 53 else None),
            "green_lum": int_val(cells[col.get("GreenLum", 54)] if col else cells[54] if len(cells) > 54 else None),
            "blue_lum": int_val(cells[col.get("BlueLum", 55)] if col else cells[55] if len(cells) > 55 else None),
            "light_radius": int_val(cells[col.get("LightRadius", 56)] if col else cells[56] if len(cells) > 56 else None),
            "visual_slots": [],
        }
        for slot in range(1, 5):
            anim = safe_str(cells[col.get(f"AnimFile{slot}", -1)] if col else None)
            info = safe_str(cells[col.get(f"AnimFileInfo{slot}", -1)] if col else None)
            snd = safe_str(cells[col.get(f"SndFile{slot}", -1)] if col else None)
            if anim or info or snd:
                entry["visual_slots"].append({"slot": slot, "anim": anim, "info": info, "sound": snd})
        for slot in range(1, 5):
            anim = safe_str(cells[col.get(f"AnimFileB{slot}", -1)] if col else None)
            info = safe_str(cells[col.get(f"AnimFileInfoB{slot}", -1)] if col else None)
            snd = safe_str(cells[col.get(f"SndFileB{slot}", -1)] if col else None)
            if anim or info or snd:
                entry["visual_slots"].append({"slot": f"B{slot}", "anim": anim, "info": info, "sound": snd})
        rows[missile_id] = entry
        line_by_id[missile_id] = lineno
    return {"rows": rows, "line_by_id": line_by_id, "header": header, "sha256": actual_sha256, "audit_expected_sha256": expected_sha256, "audit_matches_file": actual_sha256 == expected_sha256, "line_count": len(lines), "unique_ids": len(rows)}


def parse_state_visual_table(path: Path) -> dict[int, dict]:
    data = path.read_bytes()
    text = data.decode("latin-1")
    lines = text.splitlines()
    rows: dict[int, dict] = {}
    line_by_id: dict[int, int] = {}
    for lineno, line in enumerate(lines[1:], start=2):
        if not line.strip():
            continue
        cells = line.split("\t")
        match = re.match(r"[^0-9]*([0-9]+)", cells[0] if cells else "")
        if not match:
            continue
        state_id = int(match.group(1))
        rows[state_id] = {
            "state_id": state_id,
            "line": lineno,
            "fields": cells,
        }
        line_by_id[state_id] = lineno
    return {"rows": rows, "line_by_id": line_by_id, "sha256": sha256_bytes(data), "line_count": len(lines), "data_rows": len(rows)}


def parse_skill_table_index(path: Path, label: str, *, allow_duplicate_ids: bool = False) -> dict:
    data = path.read_bytes()
    lines = data.decode("latin-1").splitlines()
    if len(lines) < 2:
        raise SystemExit(f"{label} too small: {path}")
    header = lines[0].split("\t")
    try:
        skill_id_col = header.index("SkillId")
    except ValueError as exc:
        raise SystemExit(f"{label} missing SkillId header: {path}") from exc
    name_col = header.index("SkillName") if "SkillName" in header else -1
    line_by_id: dict[int, int] = {}
    rows: dict[int, dict] = {}
    duplicate_ids: list[int] = []
    for lineno, line in enumerate(lines[1:], start=2):
        if not line.strip():
            continue
        cells = line.split("\t")
        skill_id = int_val(cells[skill_id_col] if skill_id_col < len(cells) else None)
        if skill_id <= 0:
            continue
        if skill_id in line_by_id:
            if not allow_duplicate_ids:
                raise SystemExit(f"duplicate SkillId {skill_id} in {label}: {path}")
            duplicate_ids.append(skill_id)
        line_by_id[skill_id] = lineno
        rows[skill_id] = {
            "skill_id": skill_id,
            "skill_name": safe_str(cells[name_col] if 0 <= name_col < len(cells) else ""),
            "line": lineno,
        }
    return {
        "header": header,
        "line_by_id": line_by_id,
        "rows": rows,
        "sha256": sha256_bytes(data),
        "line_count": len(lines),
        "unique_ids": len(rows),
        "duplicate_ids": duplicate_ids,
    }


def parse_skill_slice_index(path: Path) -> dict:
    return parse_skill_table_index(path, "skill slice")


def direct_field(row: dict[str, str], field: str) -> int:
    return int_val(row.get(field, "0"))


def skill_source_ref(repo: Path, line: int, field: str, slice_path: Path, provenance: dict) -> dict:
    return {
        "path": repo_relpath(slice_path, repo),
        "sha256": provenance["slice"]["sha256"],
        "line": line,
        "column": field,
        "provenance_path": provenance["path"],
        "provenance_sha256": provenance["sha256"],
    }


def make_field(row: dict[str, str], skill_line: int, field: str, column: str, repo: Path, slice_path: Path, provenance: dict, unity_bindings: dict[str, list[dict]]) -> dict:
    value = row.get(column, "")
    refs = unity_ref(field, unity_bindings)
    return {
        "value": int_val(value) if field != "pre_cast_spr_path" and field not in {"man_cast_snd_path", "fm_cast_snd_path"} else safe_str(value),
        "source_ref": skill_source_ref(repo, skill_line, column, slice_path, provenance),
        "unity_ref": refs,
        "proof_state": "verified" if refs else "missing",
        "blockers": [] if refs else ["unity_field_absent"],
    }


def build_link_target(skill_id: int, inventory_index: dict[int, dict]) -> dict | None:
    row = inventory_index.get(skill_id)
    if not row:
        return None
    return {
        "skill_id": skill_id,
        "skill_name": row["skill_name"],
        "source_line": row["source_line"],
        "canonical_source_line": row["canonical_source_line"],
    }


def build_canonical_skill_target(skill_id: int, full_skill_table: dict, repo: Path) -> dict | None:
    row = full_skill_table["rows"].get(skill_id)
    if not row:
        return None
    return {
        "skill_id": skill_id,
        "skill_name": row["skill_name"],
        "source_ref": {
            "path": repo_relpath(PC_FULL_SKILL_TABLE, repo),
            "sha256": full_skill_table["sha256"],
            "line": row["line"],
            "column": "SkillId",
            "encoding": "latin-1-byte-preserving",
        },
    }


def inventory_factions(skill_id: int, factions: list[dict]) -> list[dict]:
    out = []
    for faction in factions:
        learned = set(faction["learned"])
        display = set(faction["display"])
        union = learned | display
        if skill_id not in union:
            continue
        out.append({
            "key": faction["key"],
            "name": faction["name"],
            "classification": (
                "shared" if skill_id in learned and skill_id in display else
                "pc_learned_only" if skill_id in learned else
                "unity_display_only_unresolved"
            ),
        })
    return out


def select_row_proof_state(field_states: list[str], link_states: list[str]) -> str:
    states = field_states + link_states
    if any(state == "missing" for state in states):
        return "missing"
    if any(state == "source_only" for state in states):
        return "source_only"
    return "verified"


def build_inventory(repo: Path, sources: dict, output_path: Path, check: bool) -> tuple[dict, bytes]:
    coverage.verify_canonical_sources(sources["root"])
    factions, global_union = coverage.compute_membership(repo, sources)
    completed = coverage.verify_completed_waves(repo, factions)
    slice_rows = coverage.verify_slice_artifacts(global_union, sources["slice"], sources["provenance"])
    provenance_path = Path(sources["provenance"])
    provenance = json.loads(provenance_path.read_text(encoding="utf-8"))
    provenance["path"] = repo_relpath(provenance_path, repo)
    provenance["sha256"] = sha256_bytes(provenance_path.read_bytes())
    slice_index = parse_skill_slice_index(Path(sources["slice"]))
    if slice_index["sha256"] != provenance["slice"]["sha256"]:
        raise SystemExit("skill slice hash does not match vltktool provenance")
    if set(slice_index["line_by_id"]) != set(global_union):
        raise SystemExit("skill slice line index does not match 242-row membership union")
    missile_audit_path = repo / PC_MISSILE_AUDIT
    missile_path = repo / PC_MISSILE_TABLE
    full_skill_path = repo / PC_FULL_SKILL_TABLE
    state_path = repo / STATE_VISUAL_TABLE
    unity_bindings = resolve_unity_bindings(repo)
    missile_audit = load_missile_audit(missile_audit_path)
    missile_audit["audit_path"] = repo_relpath(missile_audit_path, repo)
    missile_table = parse_missile_table(missile_path, missile_audit["source_sha256"])
    full_skill_table = parse_skill_table_index(full_skill_path, "full skill table", allow_duplicate_ids=True)
    state_table = parse_state_visual_table(state_path)

    canonical_line_by_id = {item["id"]: item["line"] for item in provenance["source_lines"]}
    line_by_id = slice_index["line_by_id"]
    inventory_rows: list[dict] = []
    inventory_index: dict[int, dict] = {
        sid: {
            "skill_name": safe_str(slice_rows[sid].get("SkillName", "")),
            "source_line": line_by_id[sid],
            "canonical_source_line": canonical_line_by_id[sid],
        }
        for sid in global_union
    }

    summary = {
        "rows": 0,
        "factions": len(factions),
        "global_union_rows": len(global_union),
        "source_counts": {
            "skill_slice_rows": len(slice_rows),
            "full_skill_rows": full_skill_table["unique_ids"],
            "missile_rows": missile_table["unique_ids"],
            "state_rows": state_table["data_rows"],
        },
        "faction_union_counts": {},
        "faction_shared_counts": {},
        "faction_pc_learned_only_counts": {},
        "faction_unity_display_only_unresolved_counts": {},
        "field_proof_state_counts": Counter(),
        "link_proof_state_counts": Counter(),
        "row_proof_state_counts": Counter(),
        "blocker_counts": Counter(),
        "child_skill_relation_counts": Counter(),
        "state_relation_counts": Counter(),
        "event_relation_counts": Counter(),
    }
    if not missile_table["audit_matches_file"]:
        summary["blocker_counts"]["missile_audit_hash_mismatch"] += 1

    faction_counts: dict[str, dict] = {}
    for faction in factions:
        learned = set(faction["learned"])
        display = set(faction["display"])
        union = set(faction["union"])
        counts = {
            "union": len(union),
            "shared": len(learned & display),
            "learned_only": len(learned - display),
            "unity_only": len(display - learned),
            "proof_state": completed.get(faction["key"], {}).get("proof_state", "weak_or_partial"),
            "stories": completed.get(faction["key"], {}).get("stories", []),
        }
        faction_counts[faction["key"]] = counts
        summary["faction_union_counts"][faction["key"]] = counts["union"]
        summary["faction_shared_counts"][faction["key"]] = counts["shared"]
        summary["faction_pc_learned_only_counts"][faction["key"]] = counts["learned_only"]
        summary["faction_unity_display_only_unresolved_counts"][faction["key"]] = counts["unity_only"]

    for skill_id in sorted(global_union):
        row = slice_rows[skill_id]
        skill_line = line_by_id[skill_id]
        row_fields = {}
        field_states: list[str] = []
        link_states: list[str] = []

        for field, column in FIELD_SPECS:
            field_obj = make_field(row, skill_line, field, column, repo, Path(sources["slice"]), provenance, unity_bindings)
            row_fields[field] = field_obj
            field_states.append(field_obj["proof_state"])
            summary["field_proof_state_counts"][field_obj["proof_state"]] += 1
            for blocker in field_obj["blockers"]:
                summary["blocker_counts"][blocker] += 1

        child_id = direct_field(row, "ChildSkillId")
        base_skill = direct_field(row, "BaseSkill")
        child_link = {
            "target_id": child_id,
            "source_ref": skill_source_ref(repo, skill_line, "ChildSkillId", Path(sources["slice"]), provenance),
            "namespace_source_ref": skill_source_ref(repo, skill_line, "BaseSkill", Path(sources["slice"]), provenance),
            "namespace_rule": "missile" if base_skill != 0 else "skill",
            "unity_ref": [
                {"path": "Assets/Scripts/Sandbox/MissileSpawner.cs", "lines": "45-64"},
                {"path": "Assets/Scripts/Sandbox/PcMissileFullVisualParser.cs", "lines": "175-266"},
            ],
            "blockers": [],
        }
        if child_id <= 0:
            child_link["target_kind"] = "none"
            child_link["proof_state"] = "source_only"
            link_states.append(child_link["proof_state"])
            summary["link_proof_state_counts"][child_link["proof_state"]] += 1
            summary["child_skill_relation_counts"]["none"] += 1
        elif base_skill != 0 and child_id in missile_table["rows"]:
            mrow = missile_table["rows"][child_id]
            child_link["target_kind"] = "missile"
            child_link["proof_state"] = "external_verified"
            child_link["target_ref"] = {
                "missile_id": child_id,
                "source_ref": {
                    "path": repo_relpath(PC_MISSILE_TABLE, repo),
                    "sha256": missile_table["sha256"],
                    "line": mrow["line"],
                    "duplicate_policy": "last-row-wins",
                    "provenance_path": missile_audit["audit_path"],
                    "provenance_sha256": missile_audit["audit_sha256"],
                },
                "numeric_fields": {k: v for k, v in mrow.items() if k in {"move_kind", "follow_kind", "col_follow_target", "missle_height", "collid_range", "is_range_dmg", "dmg_range", "dmg_interval", "lifetime", "speed", "zspeed", "zacc", "loop_play", "sub_loop", "sub_start", "sub_stop", "response_skill", "can_destroy", "col_vanish", "can_slow", "can_col_friend", "auto_explode", "red_lum", "green_lum", "blue_lum", "light_radius"}},
                "visual_slots": mrow["visual_slots"],
            }
            link_states.append(child_link["proof_state"])
            summary["link_proof_state_counts"][child_link["proof_state"]] += 1
            summary["child_skill_relation_counts"]["missile"] += 1
        elif base_skill != 0:
            child_link["target_kind"] = "missing_missile"
            child_link["proof_state"] = "missing"
            child_link["blockers"].append("child_missile_target_missing")
            link_states.append(child_link["proof_state"])
            summary["link_proof_state_counts"][child_link["proof_state"]] += 1
            summary["blocker_counts"]["child_missile_target_missing"] += 1
            summary["child_skill_relation_counts"]["missing_missile"] += 1
        elif child_id in global_union:
            child_link["target_kind"] = "skill"
            child_link["proof_state"] = "verified"
            child_link["target_ref"] = build_link_target(child_id, inventory_index)
            link_states.append(child_link["proof_state"])
            summary["link_proof_state_counts"][child_link["proof_state"]] += 1
            summary["child_skill_relation_counts"]["skill"] += 1
        elif child_id in full_skill_table["rows"]:
            child_link["target_kind"] = "canonical_skill"
            child_link["proof_state"] = "verified"
            child_link["target_ref"] = build_canonical_skill_target(child_id, full_skill_table, repo)
            link_states.append(child_link["proof_state"])
            summary["link_proof_state_counts"][child_link["proof_state"]] += 1
            summary["child_skill_relation_counts"]["canonical_skill"] += 1
        else:
            child_link["target_kind"] = "missing"
            child_link["proof_state"] = "missing"
            child_link["blockers"].append("child_skill_target_missing")
            link_states.append(child_link["proof_state"])
            summary["link_proof_state_counts"][child_link["proof_state"]] += 1
            summary["blocker_counts"]["child_skill_target_missing"] += 1
            summary["child_skill_relation_counts"]["missing"] += 1

        state_id = direct_field(row, "StateSpecialId")
        state_link = {
            "target_id": state_id,
            "source_ref": skill_source_ref(repo, skill_line, "StateSpecialId", Path(sources["slice"]), provenance),
            "unity_ref": [
                {"path": "Assets/Scripts/Core/PcConfigParser.cs", "lines": "250-252"},
                {"path": "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs", "lines": "176-177"},
            ],
            "blockers": [],
        }
        if state_id <= 0:
            state_link["target_kind"] = "none"
            state_link["proof_state"] = "source_only"
            link_states.append(state_link["proof_state"])
            summary["link_proof_state_counts"][state_link["proof_state"]] += 1
            summary["state_relation_counts"]["none"] += 1
        elif state_id in state_table["rows"]:
            srow = state_table["rows"][state_id]
            state_link["target_kind"] = "state_visual"
            state_link["proof_state"] = "external_verified"
            state_link["target_ref"] = {
                "state_id": state_id,
                "source_ref": {
                    "path": repo_relpath(STATE_VISUAL_TABLE, repo),
                    "sha256": state_table["sha256"],
                    "line": srow["line"],
                    "encoding": "latin-1-byte-preserving",
                },
                "fields": srow["fields"],
            }
            link_states.append(state_link["proof_state"])
            summary["link_proof_state_counts"][state_link["proof_state"]] += 1
            summary["state_relation_counts"]["state_visual"] += 1
        elif state_id in STATE_STUB_NO_BYTES_IDS:
            state_link["target_kind"] = "pc_state_stub_no_bytes"
            state_link["proof_state"] = "pc_stub_no_visual"
            state_link["target_ref"] = {
                "state_id": state_id,
                "visual_claim": "none",
                "classification": "pc_stub_without_package_bytes",
                "evidence": "scout conclusion: state ids 52-64 are non-loader stub rows naming style3.spr with no winner/package bytes",
            }
            link_states.append(state_link["proof_state"])
            summary["link_proof_state_counts"][state_link["proof_state"]] += 1
            summary["state_relation_counts"][state_link["target_kind"]] += 1
        elif state_id in STATE_PC_ABSENT_NO_VISUAL_IDS:
            state_link["target_kind"] = "pc_state_absent_no_visual"
            state_link["proof_state"] = "pc_absent_no_visual"
            state_link["target_ref"] = {
                "state_id": state_id,
                "visual_claim": "none",
                "classification": "pc_clears_state_visual_name",
                "evidence": "scout conclusion: no canonical state-table row/path and PC clears the name",
            }
            link_states.append(state_link["proof_state"])
            summary["link_proof_state_counts"][state_link["proof_state"]] += 1
            summary["state_relation_counts"][state_link["target_kind"]] += 1
        else:
            state_link["target_kind"] = "missing"
            state_link["proof_state"] = "missing"
            state_link["blockers"].append("state_mapping_absent")
            link_states.append(state_link["proof_state"])
            summary["link_proof_state_counts"][state_link["proof_state"]] += 1
            summary["blocker_counts"]["state_mapping_absent"] += 1
            summary["state_relation_counts"]["missing"] += 1

        start_links = {}
        event_unity_fields = {
            "StartSkillId": "start_skill_id",
            "FlySkillId": "fly_skill_id",
            "CollidSkillId": "collid_skill_id",
            "VanishedSkillId": "vanished_skill_id",
        }
        for key, source_field in (("start_skill", "StartSkillId"), ("fly_skill", "FlySkillId"), ("collide_skill", "CollidSkillId"), ("vanish_skill", "VanishedSkillId")):
            target_id = direct_field(row, source_field)
            link = {
                "target_id": target_id,
                "source_ref": skill_source_ref(repo, skill_line, source_field, Path(sources["slice"]), provenance),
                "unity_ref": unity_ref(event_unity_fields[source_field], unity_bindings),
                "blockers": [],
            }
            if target_id <= 0:
                link["target_kind"] = "none"
                link["proof_state"] = "source_only"
            elif target_id in global_union:
                link["target_kind"] = "skill"
                link["proof_state"] = "verified"
                link["target_ref"] = build_link_target(target_id, inventory_index)
            elif target_id in full_skill_table["rows"]:
                link["target_kind"] = "external_canonical_skill"
                link["proof_state"] = "external_verified"
                link["runtime_registration"] = "not_asserted"
                link["target_ref"] = build_canonical_skill_target(target_id, full_skill_table, repo)
            else:
                link["target_kind"] = "missing"
                link["proof_state"] = "missing"
                link["blockers"].append("target_skill_not_in_inventory")
                summary["blocker_counts"]["target_skill_not_in_inventory"] += 1
            start_links[key] = link
            link_states.append(link["proof_state"])
            summary["link_proof_state_counts"][link["proof_state"]] += 1
            summary["event_relation_counts"][link["proof_state"]] += 1

        row_obj = {
            "skill_id": skill_id,
            "skill_name": safe_str(row.get("SkillName", "")),
            "source_line": skill_line,
            "canonical_source_line": canonical_line_by_id[skill_id],
            "source_ref": skill_source_ref(repo, skill_line, "SkillId", Path(sources["slice"]), provenance),
            "factions": inventory_factions(skill_id, factions),
            "fields": row_fields,
            "relations": {
                "child_skill": child_link,
                "state_visual": state_link,
                **start_links,
            },
            "proof_state": select_row_proof_state(field_states, link_states),
        }
        summary["row_proof_state_counts"][row_obj["proof_state"]] += 1
        inventory_rows.append(row_obj)
        summary["rows"] += 1

    summary["field_proof_state_counts"] = Counter()
    summary["link_proof_state_counts"] = Counter()
    summary["row_proof_state_counts"] = Counter()
    summary["blocker_counts"] = Counter()
    summary["child_skill_relation_counts"] = Counter()
    summary["state_relation_counts"] = Counter()
    summary["event_relation_counts"] = Counter()

    if not missile_table["audit_matches_file"]:
        summary["blocker_counts"]["missile_audit_hash_mismatch"] += 1

    for row_obj in inventory_rows:
        summary["row_proof_state_counts"][row_obj["proof_state"]] += 1
        for field_obj in row_obj["fields"].values():
            summary["field_proof_state_counts"][field_obj["proof_state"]] += 1
            for blocker in field_obj["blockers"]:
                summary["blocker_counts"][blocker] += 1
        child_link = row_obj["relations"]["child_skill"]
        summary["link_proof_state_counts"][child_link["proof_state"]] += 1
        summary["child_skill_relation_counts"][child_link["target_kind"]] += 1
        for blocker in child_link["blockers"]:
            summary["blocker_counts"][blocker] += 1
        state_link = row_obj["relations"]["state_visual"]
        summary["link_proof_state_counts"][state_link["proof_state"]] += 1
        summary["state_relation_counts"][state_link["target_kind"]] += 1
        for blocker in state_link["blockers"]:
            summary["blocker_counts"][blocker] += 1
        for key in ("start_skill", "fly_skill", "collide_skill", "vanish_skill"):
            link = row_obj["relations"][key]
            summary["link_proof_state_counts"][link["proof_state"]] += 1
            summary["event_relation_counts"][link["proof_state"]] += 1
            for blocker in link["blockers"]:
                summary["blocker_counts"][blocker] += 1

    inventory = {
        "schema": SCHEMA,
        "generated_by": "scripts/audit_skill_presentation.py",
        "source_provenance": {
            "skill_slice": {
                "path": repo_relpath(Path(sources["slice"]), repo),
                "provenance_path": provenance["path"],
                "sha256": provenance["slice"]["sha256"],
                "provenance_sha256": provenance["sha256"],
                "requested_ids": provenance["requested_ids"],
                "selected_ids": provenance["selected_ids"],
                "source_lines_count": len(provenance["source_lines"]),
                "slice_line_count": slice_index["line_count"],
                "slice_indexed_ids": len(slice_index["line_by_id"]),
            },
            "missles1": {
                "path": str(PC_MISSILE_TABLE),
                "sha256": missile_table["sha256"],
                "audit_expected_sha256": missile_table["audit_expected_sha256"],
                "audit_matches_file": missile_table["audit_matches_file"],
                "audit_path": missile_audit["audit_path"],
                "audit_sha256": missile_audit["audit_sha256"],
                "header_sha256": missile_audit["header_sha256"],
                "expected_header_columns": missile_audit["expected_header_columns"],
                "duplicate_policy": missile_audit["duplicate_policy"],
                "unique_ids": missile_audit["unique_ids"],
                "parsed_ids": missile_audit["parsed_ids"],
                "physical_lines": missile_audit["physical_lines"],
            },
            "pc_skills1_full": {
                "path": str(PC_FULL_SKILL_TABLE),
                "sha256": full_skill_table["sha256"],
                "encoding": "latin-1-byte-preserving",
                "line_count": full_skill_table["line_count"],
                "unique_ids": full_skill_table["unique_ids"],
                "duplicate_ids": full_skill_table["duplicate_ids"],
            },
            "state_visual_mapping": {
                "path": repo_relpath(STATE_VISUAL_TABLE, repo),
                "sha256": state_table["sha256"],
                "encoding": "latin-1-byte-preserving",
                "line_count": state_table["line_count"],
                "data_rows": state_table["data_rows"],
            },
            "unity_static_inspection": [
                {"path": "Assets/Scripts/Core/PcConfigParser.cs", "lines": "176-307"},
                {"path": "Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs", "lines": "165-214"},
                {"path": "Assets/Scripts/Sandbox/MalePlayerSpriteCatalog.cs", "lines": "317-331"},
                {"path": "Assets/Scripts/Sandbox/PcMissileFullVisualParser.cs", "lines": "175-266"},
                {"path": "Assets/Scripts/Sandbox/MissileSpawner.cs", "lines": "45-64"},
            ],
        },
        "summary_counts": {
            **summary,
            "field_proof_state_counts": dict(sorted(summary["field_proof_state_counts"].items())),
            "link_proof_state_counts": dict(sorted(summary["link_proof_state_counts"].items())),
            "row_proof_state_counts": dict(sorted(summary["row_proof_state_counts"].items())),
            "blocker_counts": dict(sorted(summary["blocker_counts"].items())),
            "child_skill_relation_counts": dict(sorted(summary["child_skill_relation_counts"].items())),
            "state_relation_counts": dict(sorted(summary["state_relation_counts"].items())),
            "event_relation_counts": dict(sorted(summary["event_relation_counts"].items())),
        },
        "factions": [
            {
                "key": faction["key"],
                "name": faction["name"],
                "faction_index": faction["faction_index"],
                "union_count": faction_counts[faction["key"]]["union"],
                "shared_count": faction_counts[faction["key"]]["shared"],
                "pc_learned_only_count": faction_counts[faction["key"]]["learned_only"],
                "unity_display_only_unresolved_count": faction_counts[faction["key"]]["unity_only"],
                "symmetric_gap_count": faction_counts[faction["key"]]["learned_only"] + faction_counts[faction["key"]]["unity_only"],
                "proof_state": faction_counts[faction["key"]]["proof_state"],
                "stories": faction_counts[faction["key"]]["stories"],
            }
            for faction in factions
        ],
        "rows": inventory_rows,
    }
    serialized = (json.dumps(inventory, ensure_ascii=False, sort_keys=True, indent=2) + "\n").encode("utf-8")
    return inventory, serialized


def ensure_meta(asset_path: Path) -> None:
    meta_path = asset_path.with_name(asset_path.name + ".meta")
    if meta_path.is_file():
        return
    guid = hashlib.sha256(asset_path.name.encode("utf-8")).hexdigest()[:32]
    meta_path.write_text(
        "fileFormatVersion: 2\n"
        f"guid: {guid}\n"
        "DefaultImporter:\n"
        "  externalObjects: {}\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n",
        encoding="ascii",
        newline="\n",
    )


def resolve_sources(repo: Path) -> dict:
    root = coverage.CANONICAL_ROOT
    return {
        "root": root,
        "skill_txt": root / "pak_unpacked/slistcache/settings/skills.txt",
        "progression": root / "01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/script/global/skills_table.lua",
        "skillbook": root / "01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/script/item/skillbook.lua",
        "slice": repo / "Assets/StreamingAssets/Reference/PcAllFactionLearnedDisplaySkills.txt",
        "provenance": repo / "Assets/StreamingAssets/Reference/PcAllFactionLearnedDisplaySkills.provenance.json",
    }


def main() -> int:
    repo = Path(__file__).resolve().parents[1]
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", default=str(repo))
    parser.add_argument("--slice", default=str(repo / "Assets/StreamingAssets/Reference/PcAllFactionLearnedDisplaySkills.txt"))
    parser.add_argument("--provenance", default=str(repo / "Assets/StreamingAssets/Reference/PcAllFactionLearnedDisplaySkills.provenance.json"))
    parser.add_argument("--output", default=str(repo / OUTPUT_DEFAULT))
    parser.add_argument("--packaged-slice", default=str(repo / PACKAGED_SLICE_DEFAULT))
    parser.add_argument("--check", action="store_true")
    parser.add_argument("--freshness-only", action="store_true")
    args = parser.parse_args()

    repo_path = Path(args.repo)
    sources = resolve_sources(repo_path)
    sources["slice"] = Path(args.slice)
    sources["provenance"] = Path(args.provenance)
    output_path = Path(args.output)
    packaged_slice_path = Path(args.packaged_slice)

    inventory, serialized = build_inventory(repo_path, sources, output_path, args.check)
    canonical_slice_bytes = Path(sources["slice"]).read_bytes()

    if args.check or args.freshness_only:
        if not output_path.is_file() or output_path.read_bytes() != serialized:
            raise SystemExit(f"stale presentation inventory: run {Path(__file__).name}")
        if (not packaged_slice_path.is_file() or
                packaged_slice_path.read_bytes() != canonical_slice_bytes):
            raise SystemExit(f"stale packaged skill slice: run {Path(__file__).name}")
        if args.check and inventory["summary_counts"]["blocker_counts"].get("missile_audit_hash_mismatch"):
            raise SystemExit("presentation inventory blocked: missile_audit_hash_mismatch")
        mode = "freshness-only" if args.freshness_only else "check"
        print(
            f"presentation inventory {mode}: rows={inventory['summary_counts']['rows']}, "
            f"missiles={inventory['summary_counts']['source_counts']['missile_rows']}, "
            f"states={inventory['summary_counts']['source_counts']['state_rows']}, "
            f"sha256={sha256_bytes(serialized)}"
        )
        return 0

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_bytes(serialized)
    ensure_meta(output_path)
    packaged_slice_path.parent.mkdir(parents=True, exist_ok=True)
    packaged_slice_path.write_bytes(canonical_slice_bytes)
    ensure_meta(packaged_slice_path)
    print(
        f"wrote {output_path} (rows={inventory['summary_counts']['rows']}, "
        f"sha256={sha256_bytes(serialized)})"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
