#!/usr/bin/env python3
"""Read-only reconnaissance for one JX PC skill.

This script intentionally does not choose the active PAK winner, execute Lua,
or emulate C++ dispatch. It exposes exact rows and source locations so an agent
can build a parity ledger before editing Unity.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from pathlib import Path
from typing import Any, Iterable


DEFAULT_JX_ROOT = Path("/var/www/jx-pc/pak_unpacked")

TCVN3_TABLE = (
    0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
    16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31,
    32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47,
    48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63,
    64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79,
    80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95,
    96, 97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111,
    112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127,
    128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143,
    144, 145, 146, 147, 148, 149, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159,
    160, 258, 194, 202, 212, 416, 431, 272, 259, 226, 234, 244, 417, 432, 273, 175,
    176, 177, 178, 179, 180, 224, 7843, 227, 225, 7841, 186, 7857, 7859, 7861, 7855, 191,
    192, 193, 194, 195, 196, 197, 7863, 7847, 7849, 7851, 7845, 7853, 232, 205, 7867, 7869,
    233, 7865, 7873, 7875, 7877, 7871, 7879, 236, 7881, 217, 218, 219, 297, 237,
    7883, 242, 224, 7887, 245, 243, 7885, 7891, 7893, 7895, 7889, 7897, 7901, 7903,
    7905, 7899, 7907, 249, 240, 7911, 361, 250, 7909, 7915, 7917, 7919, 7913, 7921,
    7923, 7927, 7929, 253, 7925, 255,
)

VIET_CHARS = set(
    "ăâđêôơưáàảãạắằẳẵặấầẩẫậéèẻẽẹếềểễệ"
    "íìỉĩịóòỏõọốồổỗộớờởỡợúùủũụứừửữựýỳỷỹỵ"
    "ĂÂĐÊÔƠƯÁÀẢÃẠẮẰẲẴẶẤẦẨẪẬÉÈẺẼẸẾỀỂỄỆ"
    "ÍÌỈĨỊÓÒỎÕỌỐỒỔỖỘỚỜỞỠỢÚÙỦŨỤỨỪỬỮỰÝỲỶỸỴ"
)
MOJIBAKE_CHARS = set("ÃÂÊÎÔÛÐÑÒÓÕÖ×ØÙÚÛÜÝÞß¶·¸¹º»¼½¾¿±")

TCVN3_FIELDS = {
    "SkillName",
    "Property",
    "SkillDesc",
    "MissleName",
    "Param1Memo",
    "Param2Memo",
}

SKILL_FIELDS = (
    "SkillName", "Property", "SkillId", "Attrib", "SkillStyle", "SkillIcon",
    "PreCastSpr", "ManCastSnd", "FMCastSnd", "StateSpecialId", "StatePriority",
    "IsAura", "LRSkill", "NeedShadow", "AttackRadius", "MaxShadowNum",
    "MslsGenerate", "MslsGenerateData", "CharClass", "MisslesForm",
    "ChildSkillId", "ChildSkillLevel", "ChildSkillNum", "BaseSkill",
    "CharAnimId", "EventSkillLevel", "IsMelee", "WaitTime", "IsSaveCd",
    "ClientSend", "SkillCostType", "CostValue", "TimePerCast",
    "TimePerCastOnHorse", "IsPhysical", "TargetOnly", "TargetEnemy",
    "TargetAlly", "TargetSelf", "TargetOther", "TargetObj", "TargetNoNpc",
    "ByMissle", "IsUseAR", "StartEvent", "StartSkillId", "FlyEvent",
    "FlySkillId", "FlyEventTime", "CollideEvent", "CollidSkillId",
    "VanishedEvent", "VanishedSkillId", "ReqLevel", "MaxLevel", "EqtLimit",
    "HorseLimit", "DoHurt", "WeaponSkill", "Param1", "Param1Memo",
    "Param2", "Param2Memo", "StopWhenMove", "HeelAtParent",
    "RelativePosType", "PeaceCanUse", "ShowEvent", "IsExpSkill", "Series",
    "ShowAddition", "LvlSetScript", "LevelUpScript", "SkillDesc",
)

MISSILE_FIELDS = (
    "MissleId", "MissleName", "MoveKind", "FollowKind", "ColFollowTarget",
    "MissleHeight", "CollidRange", "IsRangeDmg", "DmgRange", "DmgInterval",
    "LifeTime", "Speed", "Zspeed", "Zacc", "LoopPlay", "SubLoop",
    "SubStart", "SubStop", "ResponseSkill", "CanDestroy", "ColVanish",
    "CanSlow", "CanColFriend", "AutoExplode", "MissRate", "Param1",
    "Param2", "Param3", "MultiShow", "AnimFile1", "AnimFileInfo1",
    "SndFile1", "AnimFile2", "AnimFileInfo2", "SndFile2", "AnimFile3",
    "AnimFileInfo3", "SndFile3", "AnimFile4", "AnimFileInfo4", "SndFile4",
    "AnimFileB1", "AnimFileInfoB1", "SndFileB1", "AnimFileB2",
    "AnimFileInfoB2", "SndFileB2", "AnimFileB3", "AnimFileInfoB3",
    "SndFileB3", "AnimFileB4", "AnimFileInfoB4", "SndFileB4",
    "RedLum", "GreenLum", "BlueLum", "LightRadius",
)

SKILL_STYLE_NAMES = {
    0: "SKILL_SS_Missles",
    1: "SKILL_SS_Melee",
    2: "SKILL_SS_InitiativeNpcState",
    3: "SKILL_SS_PassivityNpcState",
    4: "SKILL_SS_CreateNpc",
    5: "SKILL_SS_BuildPoison",
    6: "SKILL_SS_AddPoison",
    7: "SKILL_SS_GetObjDirectly",
    8: "SKILL_SS_StrideObstacle",
    9: "SKILL_SS_BodyToObject",
    10: "SKILL_SS_Mining",
    11: "SKILL_SS_RepairWeapon",
    12: "SKILL_SS_Capture",
    13: "SKILL_SS_Thief",
}

MISSILE_FORM_NAMES = {
    0: "SKILL_MF_Wall",
    1: "SKILL_MF_Line",
    2: "SKILL_MF_Spread",
    3: "SKILL_MF_Circle",
    4: "SKILL_MF_Random",
    5: "SKILL_MF_Zone",
    6: "SKILL_MF_AtTarget",
    7: "SKILL_MF_AtFirer",
}

MOVE_KIND_NAMES = {
    0: "MISSLE_MMK_Stand",
    1: "MISSLE_MMK_Line",
    2: "MISSLE_MMK_Random",
    3: "MISSLE_MMK_Circle",
    4: "MISSLE_MMK_Helix",
    5: "MISSLE_MMK_Follow",
    6: "MISSLE_MMK_Motion",
    7: "MISSLE_MMK_Parabola",
    8: "MISSLE_MMK_SingleLine",
    100: "MISSLE_MMK_RollBack",
    101: "MISSLE_MMK_Toss",
}

FOLLOW_KIND_NAMES = {
    0: "MISSLE_MFK_None",
    1: "MISSLE_MFK_NPC",
    2: "MISSLE_MFK_Missle",
}

RESOURCE_RE = re.compile(r"\.(?:spr|wav|mp3|ogg)$", re.IGNORECASE)
LUA_SYMBOL_RE = re.compile(r"^[A-Za-z_][A-Za-z0-9_]*$")
INT_RE = re.compile(r"^-?\d+$")


def tcvn3_decode(raw: bytes) -> str:
    western = raw.decode("cp1252", errors="replace")
    chars: list[str] = []
    for char in western:
        code = ord(char)
        chars.append(chr(TCVN3_TABLE[code]) if code < len(TCVN3_TABLE) else char)
    return "".join(chars)


def score_text(text: str) -> int:
    score = 0
    for char in text:
        if char in VIET_CHARS:
            score += 4
        elif "\u4e00" <= char <= "\u9fff":
            score += 8
        elif char == "\ufffd":
            score -= 20
        elif char in MOJIBAKE_CHARS:
            score -= 4
        elif ord(char) < 32 and char not in "\n\r\t":
            score -= 10
        elif char.isprintable() or char.isspace():
            score += 1
    return score


def decode_field(raw: bytes, field_name: str) -> str:
    raw = raw.strip()
    if not raw:
        return ""
    try:
        return raw.decode("ascii")
    except UnicodeDecodeError:
        pass

    if field_name in TCVN3_FIELDS:
        return tcvn3_decode(raw).strip()

    lowered = raw.lower()
    if b"\\" in raw or any(ext in lowered for ext in (b".spr", b".wav", b".lua", b".ini")):
        return raw.decode("gb18030", errors="replace").strip()

    candidates: list[str] = []
    try:
        candidates.append(raw.decode("utf-8"))
    except UnicodeDecodeError:
        pass
    candidates.extend(
        (
            raw.decode("gb18030", errors="replace"),
            tcvn3_decode(raw),
            raw.decode("cp1252", errors="replace"),
        )
    )
    return max(candidates, key=score_text).strip()


def coerce(value: str) -> Any:
    value = value.strip()
    if INT_RE.fullmatch(value):
        return int(value)
    return value


def parse_int(value: Any, default: int = 0) -> int:
    try:
        return int(value)
    except (TypeError, ValueError):
        return default


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def resolve_casefold(root: Path, pc_path: str) -> Path | None:
    normalized = pc_path.replace("\\", "/").lstrip("/")
    parts = [part for part in normalized.split("/") if part and part != "."]
    if not parts or ".." in parts:
        return None

    current = root
    for part in parts:
        direct = current / part
        if direct.exists():
            current = direct
            continue
        if not current.is_dir():
            return None
        matches = [entry for entry in current.iterdir() if entry.name.casefold() == part.casefold()]
        if not matches:
            return None
        current = sorted(matches, key=lambda entry: entry.name)[0]
    return current


def locate_required(root: Path, relative_path: str) -> Path:
    path = resolve_casefold(root, relative_path)
    if path is None or not path.is_file():
        raise FileNotFoundError(f"Required PC file not found: {root / relative_path}")
    return path


def read_tsv_row(path: Path, id_column: str, wanted_id: int) -> tuple[dict[str, str], int]:
    raw_lines = path.read_bytes().splitlines()
    if not raw_lines:
        raise ValueError(f"Empty PC table: {path}")

    header_parts = raw_lines[0].lstrip(b"\xef\xbb\xbf").split(b"\t")
    headers = [part.decode("ascii", errors="replace").strip() for part in header_parts]
    try:
        id_index = headers.index(id_column)
    except ValueError as exc:
        raise ValueError(f"{path} has no {id_column} column") from exc

    for line_number, raw_line in enumerate(raw_lines[1:], start=2):
        parts = raw_line.rstrip(b"\r").split(b"\t")
        if id_index >= len(parts):
            continue
        try:
            row_id = int(parts[id_index].strip().decode("ascii"))
        except (UnicodeDecodeError, ValueError):
            continue
        if row_id != wanted_id:
            continue

        row: dict[str, str] = {}
        for index, header in enumerate(headers):
            raw_value = parts[index] if index < len(parts) else b""
            row[header] = decode_field(raw_value, header)
        return row, line_number

    raise ValueError(f"{id_column}={wanted_id} not found in {path}")


def select_fields(row: dict[str, str], names: Iterable[str]) -> dict[str, Any]:
    return {name: coerce(row.get(name, "")) for name in names if row.get(name, "") != ""}


def level_bindings(row: dict[str, str]) -> list[dict[str, Any]]:
    bindings: list[dict[str, Any]] = []
    for slot in range(1, 21):
        setting = row.get(f"LvlSetting{slot}", "").strip()
        data = row.get(f"LvlData{slot}", "").strip()
        if setting or data:
            bindings.append({"slot": slot, "setting": setting, "data": coerce(data)})
    return bindings


def decode_lua_lines(path: Path) -> list[str]:
    return [line.decode("gb18030", errors="replace") for line in path.read_bytes().splitlines()]


def extract_lua_block(path: Path, symbol: str, level: int) -> dict[str, Any] | None:
    lines = decode_lua_lines(path)
    start_re = re.compile(rf"^\s*{re.escape(symbol)}\s*=\s*\{{")
    start_index = -1
    depth = 0

    for index, line in enumerate(lines):
        code = line.split("--", 1)[0]
        if start_index < 0:
            if not start_re.search(code):
                continue
            start_index = index
        depth += code.count("{") - code.count("}")
        if start_index >= 0 and depth <= 0:
            end_index = index
            break
    else:
        if start_index < 0:
            return None
        end_index = min(len(lines) - 1, start_index + 239)

    excerpt_lines = lines[start_index : end_index + 1]
    level_re = re.compile(rf"\{{\s*{level}\s*,")
    mentions: list[dict[str, Any]] = []
    for offset, text in enumerate(excerpt_lines):
        executable = text.split("--", 1)[0]
        if level_re.search(executable):
            mentions.append(
                {
                    "line": start_index + offset + 1,
                    "text": executable.strip(),
                }
            )
    return {
        "symbol": symbol,
        "source": str(path),
        "start_line": start_index + 1,
        "end_line": end_index + 1,
        "requested_level_mentions": mentions,
        "text": "\n".join(excerpt_lines),
    }


def enum_hint(value: Any, names: dict[int, str], evidence: str) -> dict[str, Any]:
    numeric = parse_int(value, default=-1)
    return {
        "value": numeric,
        "name": names.get(numeric, "unknown"),
        "evidence": evidence,
    }


def collect_resources(sources: Iterable[tuple[str, dict[str, str]]]) -> list[dict[str, str]]:
    resources: list[dict[str, str]] = []
    seen: set[tuple[str, str]] = set()
    for source_name, row in sources:
        for field, value in row.items():
            value = value.strip()
            if not value or not RESOURCE_RE.search(value):
                continue
            key = (source_name, value.casefold())
            if key in seen:
                continue
            seen.add(key)
            resources.append(
                {
                    "source": source_name,
                    "field": field,
                    "pc_path": value,
                    "resolver_required": "jx-pc-resource-resolver",
                }
            )
    return resources


def event_references(row: dict[str, str]) -> list[dict[str, Any]]:
    definitions = (
        ("start", "StartEvent", "StartSkillId"),
        ("fly", "FlyEvent", "FlySkillId"),
        ("collide", "CollideEvent", "CollidSkillId"),
        ("vanish", "VanishedEvent", "VanishedSkillId"),
    )
    refs: list[dict[str, Any]] = []
    for kind, gate_field, id_field in definitions:
        gate = parse_int(row.get(gate_field))
        skill_id = parse_int(row.get(id_field))
        if gate or skill_id:
            refs.append(
                {
                    "kind": kind,
                    "static_gate": gate,
                    "skill_id": skill_id,
                    "note": "Lua level data may override or enable this edge.",
                }
            )
    return refs


def build_report(args: argparse.Namespace) -> dict[str, Any]:
    package_arg = Path(args.package).expanduser()
    package_root = package_arg if package_arg.is_absolute() else args.jx_root / package_arg
    package_root = package_root.resolve()
    if not package_root.is_dir():
        raise FileNotFoundError(f"Package directory not found: {package_root}")

    skills_path = locate_required(package_root, "settings/skills.txt")
    missiles_path = locate_required(package_root, "settings/missles.txt")
    skill_row, skill_line = read_tsv_row(skills_path, "SkillId", args.skill_id)

    warnings = [
        "The selected package is reconnaissance only; determine the active PAK winner separately.",
        "Lua excerpts are not executed. Confirm interpolation, duplicate anchors, Conic, and missing-table semantics in PC code.",
        "Enum hints do not replace reading the current C++ dispatch function.",
        "Recurse through dynamic child/event skills before implementation.",
    ]

    child_id = parse_int(skill_row.get("ChildSkillId"))
    base_skill = parse_int(skill_row.get("BaseSkill"))
    child: dict[str, Any] | None = None
    resource_sources: list[tuple[str, dict[str, str]]] = [("skill", skill_row)]
    missile_row: dict[str, str] | None = None

    if child_id > 0 and base_skill:
        try:
            missile_row, missile_line = read_tsv_row(missiles_path, "MissleId", child_id)
            child = {
                "kind": "missile",
                "source": str(missiles_path),
                "line": missile_line,
                "fields": select_fields(missile_row, MISSILE_FIELDS),
            }
            resource_sources.append(("child_missile", missile_row))
        except ValueError as exc:
            warnings.append(str(exc))
            child = {"kind": "missile", "id": child_id, "error": str(exc)}
    elif child_id > 0:
        try:
            child_skill_row, child_skill_line = read_tsv_row(skills_path, "SkillId", child_id)
            child = {
                "kind": "skill",
                "source": str(skills_path),
                "line": child_skill_line,
                "fields": select_fields(child_skill_row, SKILL_FIELDS),
                "level_bindings": level_bindings(child_skill_row),
            }
            resource_sources.append(("child_skill", child_skill_row))
        except ValueError as exc:
            warnings.append(str(exc))
            child = {"kind": "skill", "id": child_id, "error": str(exc)}

    lua_pc_path = skill_row.get("LvlSetScript", "").strip()
    lua_result: dict[str, Any] = {
        "pc_path": lua_pc_path,
        "requested_level": args.level,
        "blocks": [],
        "evaluation": "not executed; inspect the PC evaluator/consumer",
    }
    if lua_pc_path:
        lua_path = resolve_casefold(package_root, lua_pc_path)
        if lua_path is None or not lua_path.is_file():
            warnings.append(f"Lua source not found in selected package: {lua_pc_path}")
        else:
            lua_result["source"] = str(lua_path)
            lua_result["sha256"] = sha256_file(lua_path)
            symbols: list[str] = []
            for binding in level_bindings(skill_row):
                data = str(binding["data"])
                if LUA_SYMBOL_RE.fullmatch(data) and data not in symbols:
                    symbols.append(data)
            for symbol in symbols:
                block = extract_lua_block(lua_path, symbol, args.level)
                if block is not None:
                    lua_result["blocks"].append(block)
                else:
                    warnings.append(f"Lua table not found: {symbol} in {lua_path}")

    decoded_enums: dict[str, Any] = {
        "skill_style": enum_hint(
            skill_row.get("SkillStyle"),
            SKILL_STYLE_NAMES,
            "SkillDef.h eSKillStyle; inspect KSkills.cpp dispatch",
        ),
        "missile_form": enum_hint(
            skill_row.get("MisslesForm"),
            MISSILE_FORM_NAMES,
            "SkillDef.h eMisslesForm lines 120-130; inspect KSkills.cpp cast function",
        ),
    }
    if missile_row is not None:
        decoded_enums["move_kind"] = enum_hint(
            missile_row.get("MoveKind"),
            MOVE_KIND_NAMES,
            "SkillDef.h eMissleMoveKind lines 20-33; inspect KMissle.cpp update",
        )
        decoded_enums["follow_kind"] = enum_hint(
            missile_row.get("FollowKind"),
            FOLLOW_KIND_NAMES,
            "SkillDef.h eMissleFollowKind lines 38-43",
        )

    return {
        "schema_version": 1,
        "request": {
            "skill_id": args.skill_id,
            "level": args.level,
        },
        "package": {
            "argument": args.package,
            "root": str(package_root),
            "selection_status": "reconnaissance-only",
        },
        "skill": {
            "source": str(skills_path),
            "line": skill_line,
            "sha256": sha256_file(skills_path),
            "fields": select_fields(skill_row, SKILL_FIELDS),
            "level_bindings": level_bindings(skill_row),
        },
        "decoded_enums": decoded_enums,
        "child": child,
        "static_event_references": event_references(skill_row),
        "lua": lua_result,
        "resource_paths": collect_resources(resource_sources),
        "source_files": {
            "skills": {"path": str(skills_path), "sha256": sha256_file(skills_path)},
            "missiles": {"path": str(missiles_path), "sha256": sha256_file(missiles_path)},
        },
        "next_pc_evidence": [
            "Resolve active package/PAK winner and every resource path.",
            "Read SkillDef.h enum definitions for the selected numeric values.",
            "Read KSkills.cpp dispatch and formation/cast function.",
            "Read KMissle.cpp movement, collision, event, and lifecycle code.",
            "Evaluate requested Lua level with the real PC semantics.",
            "Recurse through child, response, and dynamic event skills.",
        ],
        "warnings": warnings,
    }


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Audit one JX PC skill row, child row, Lua block, and resource paths as JSON."
    )
    parser.add_argument("--skill-id", type=int, required=True, help="PC Skills.txt SkillId.")
    parser.add_argument("--level", type=int, default=20, help="Requested skill level context.")
    parser.add_argument(
        "--package",
        default="slistcache",
        help="Package name under --jx-root, or an absolute extracted package directory.",
    )
    parser.add_argument(
        "--jx-root",
        type=Path,
        default=DEFAULT_JX_ROOT,
        help=f"Extracted PAK root (default: {DEFAULT_JX_ROOT}).",
    )
    args = parser.parse_args(argv)
    if args.skill_id <= 0:
        parser.error("--skill-id must be positive")
    if args.level <= 0:
        parser.error("--level must be positive")
    return args


def main(argv: list[str] | None = None) -> int:
    args = parse_args(argv or sys.argv[1:])
    try:
        report = build_report(args)
    except (FileNotFoundError, OSError, ValueError) as exc:
        print(json.dumps({"error": str(exc)}, ensure_ascii=False, indent=2), file=sys.stderr)
        return 2
    json.dump(report, sys.stdout, ensure_ascii=False, indent=2)
    sys.stdout.write("\n")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
