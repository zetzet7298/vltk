#!/usr/bin/env python3
"""Generate/check the deterministic TangMen per-level damage/state reference.

Parses the vendored canonical PC server ``tangmen.lua`` SKILLS table together
with the hash-pinned ``PcTangMenSkills.txt`` / ``PcTangMenRelationshipTargets.txt``
slices (which pin each skill id to its queried ``(attribute, lua_key)`` pairs and
``MaxLevel``), then emits a flat level-data JSON reference that the Unity
factory + EditMode parity test consume as the independent expected authority.

Only magic attributes the existing ``MagicAttributeKind`` model can represent are
materialized; every other queried attribute is recorded as an explicit residual
so unsupported semantics are never silently dropped.

Link interpolation matches canonical PC ``Link()``/``Line`` semantics: linear
between adjacent curve marks and extrapolated from the nearest endpoint segment
outside the marks.
"""

from __future__ import annotations

import argparse
import csv
import hashlib
import json
import math
import re
from pathlib import Path


LUA_SHA256 = "3f2e7c2aba8329508adab3a6293f41be29a0d68a8d63ac5a34903bece0578c90"
LEARNED_SLICE_SHA256 = "e4a6657ccfd87be51e5404143df81ce60a022fbbd17303cb9c9c1c59841108ad"
TARGET_SLICE_SHA256 = "888c93cde48ec22160e12386580bca3aafc2b74d5bc16ba21b70c06a9a8007ba"

# Roots that already ship hand-authored pcLevelData (SKL-TM-CATALOG-001 shared
# roots). They are excluded from this level-data wave: their existing data and
# residuals are owned by a separate story.
SHARED_ROOT_IDS = {43, 45, 47, 48, 50, 54, 58}

# (lua attribute name) -> (MagicAttributeKind enum name, factory bucket).
# Buckets mirror the existing TangMen shared-root builders so production and
# reference stay byte-aligned. Residual attributes (missle_speed_v, event/exp/
# addskilldamage* curves, poisonenhance_p, ...) are NOT mapped here.
SUPPORTED_ATTRS = {
    "physicsenhance_p": ("PhysicsEnhanceP", "damage"),
    "seriesdamage_p": ("SeriesDamageP", "damage"),
    "poisondamage_v": ("PoisonDamageV", "damage"),
    "physicsdamage_v": ("PhysicsDamageV", "damage"),
    "colddamage_v": ("ColdDamageV", "damage"),
    "firedamage_v": ("FireDamageV", "damage"),
    "lightingdamage_v": ("LightingDamageV", "damage"),
    "deadlystrike_p": ("DeadlyStrikeP", "state"),
    "stun_p": ("StunP", "state"),
    "skill_cost_v": ("SkillCostV", "skill"),
}

TANGMEN_LVL_SCRIPT = "tangmen.lua"


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


# --------------------------------------------------------------------------- #
# Lua SKILLS table parser (subset: tables of numbers, keyed/positional).
# --------------------------------------------------------------------------- #
def strip_comments(src: str) -> str:
    out: list[str] = []
    i, n = 0, len(src)
    state = "code"
    quote = ""
    while i < n:
        c = src[i]
        if state == "code":
            if c == "-" and src[i + 1 : i + 2] == "-":
                if src[i + 2 : i + 4] == "[[":
                    state = "block"
                    i += 4
                    continue
                state = "line"
                i += 2
                continue
            if c in "\"'":
                state = "str"
                quote = c
                out.append(c)
                i += 1
                continue
            out.append(c)
            i += 1
        elif state == "str":
            out.append(c)
            if c == "\\" and i + 1 < n:
                out.append(src[i + 1])
                i += 2
                continue
            if c == quote:
                state = "code"
            i += 1
        elif state == "line":
            if c == "\n":
                state = "code"
                out.append(c)
            i += 1
        else:  # block comment
            if c == "]" and src[i + 1 : i + 2] == "]":
                state = "code"
                i += 2
                continue
            i += 1
    return "".join(out)


def _skip_ws(s: str, i: int) -> int:
    while i < len(s) and s[i] in " \t\r\n":
        i += 1
    return i


def _skip_call_group(s: str, j: int) -> int:
    """If a `(` call group follows a bareword token, consume the balanced group."""
    k = _skip_ws(s, j)
    if k < len(s) and s[k] == "(":
        depth = 0
        p = k
        while p < len(s):
            if s[p] == "(":
                depth += 1
            elif s[p] == ")":
                depth -= 1
                if depth == 0:
                    return p + 1
            p += 1
    return j


def parse_value(s: str, i: int):
    i = _skip_ws(s, i)
    if i >= len(s):
        return None, i
    if s[i] == "{":
        return parse_table(s, i)
    if s[i : i + 8] == "function":
        # Skip to the matching `end` keyword (brace-depth 0). These bodies
        # (skill_desc) are never consumed; we only advance past them.
        depth = 0
        j = i + 8
        while j < len(s):
            if s[j] == "{":
                depth += 1
            elif s[j] == "}":
                depth -= 1
            elif (
                depth <= 0
                and s[j : j + 3] == "end"
                and not (j > 0 and (s[j - 1].isalnum() or s[j - 1] == "_"))
                and not s[j + 3 : j + 4].isalnum()
                and s[j + 3 : j + 4] != "_"
            ):
                return None, j + 3
            j += 1
        return None, j
    if s[i] in "\"'":
        quote = s[i]
        j = i + 1
        while j < len(s) and s[j] != quote:
            if s[j] == "\\":
                j += 2
                continue
            j += 1
        return s[i + 1 : j], j + 1
    m = re.match(r"[-\w.]+", s[i:])
    if not m:
        return s[i], i + 1  # opaque single char (e.g. stray parenthesis in call args)
    token = m.group(0)
    return token, _skip_call_group(s, i + len(token))


def parse_table(s: str, i: int):
    assert s[i] == "{"
    i += 1
    items: list = []
    keyed: dict = {}
    has_key = False
    while i < len(s):
        i = _skip_ws(s, i)
        if i >= len(s):
            break
        if s[i] == "}":
            return (keyed if has_key else items), i + 1
        if s[i] in ",;":
            i += 1
            continue
        if s[i] == "[":
            j = s.index("]", i)
            key = s[i + 1 : j].strip()
            i = _skip_ws(s, j + 1)
            if s[i] == "=":
                i += 1
            val, i = parse_value(s, i)
            keyed[int(key) if key.lstrip("-").isdigit() else key] = val
            has_key = True
        else:
            # Lua sugar: `name = value` is `['name'] = value`. Match a bare
            # identifier followed by exactly one `=` (not `==`); otherwise the
            # item is positional (number / string / table / token).
            name_match = re.match(r"([A-Za-z_]\w*)\s*=(?!=)", s[i:])
            if name_match:
                key = name_match.group(1)
                i += name_match.end()
                val, i = parse_value(s, i)
                keyed[key] = val
                has_key = True
            else:
                val, i = parse_value(s, i)
                items.append(val)
    raise SystemExit("unterminated lua table while parsing tangmen.lua")


def parse_skills_table(lua_src: str) -> dict:
    cleaned = strip_comments(lua_src)
    m = re.search(r"SKILLS\s*=\s*\{", cleaned)
    if not m:
        raise SystemExit("SKILLS table not found in tangmen.lua")
    start = cleaned.index("{", m.start())
    depth = 0
    end = start
    for idx in range(start, len(cleaned)):
        if cleaned[idx] == "{":
            depth += 1
        elif cleaned[idx] == "}":
            depth -= 1
            if depth == 0:
                end = idx
                break
    body = cleaned[start : end + 1]
    skills: dict[str, dict] = {}
    i = 1
    n = len(body) - 1
    while i < n:
        while i < n and body[i] in " \t\r\n,;":
            i += 1
        if i >= n:
            break
        m = re.match(r"(\w+)\s*=\s*", body[i:])
        if not m:
            raise SystemExit(f"unexpected token in SKILLS body at offset {i}: {body[i:i+40]!r}")
        name = m.group(1)
        i += m.end()
        value, i = parse_value(body, i)
        if isinstance(value, dict):
            skills[name] = value
        # positional-list skill bodies (none in tangmen.lua) are skipped.
    return skills


def normalize_subtables(attr_value) -> dict:
    """Return {1: points, 2: points, 3: points}; default [[0,0],[20,0]] per lua."""
    default = [[0, 0], [20, 0]]
    out: dict[int, list] = {}
    if isinstance(attr_value, dict):
        for k, v in attr_value.items():
            if isinstance(k, int) and 1 <= k <= 3:
                out[k] = _points(v)
    elif isinstance(attr_value, list):
        for idx, v in enumerate(attr_value, start=1):
            if 1 <= idx <= 3:
                out[idx] = _points(v)
    for k in (1, 2, 3):
        out.setdefault(k, [list(p) for p in default])
    return out


def _points(subtable) -> list:
    pts: list = []
    items = subtable if isinstance(subtable, list) else []
    for p in items:
        if isinstance(p, list) and len(p) >= 2:
            pts.append([int(p[0]), int(p[1])])
    return pts or [[0, 0], [20, 0]]


def link_pc(level: int, points: list) -> int:
    if not points:
        return 0
    if len(points) == 1:
        return int(points[0][1])
    if level < points[0][0]:
        x0, y0 = points[0]
        x1, y1 = points[1]
        return int(y1) if x1 == x0 else math.floor(y0 + (level - x0) / (x1 - x0) * (y1 - y0))
    if level > points[-1][0]:
        x0, y0 = points[-2]
        x1, y1 = points[-1]
        return int(y1) if x1 == x0 else math.floor(y0 + (level - x0) / (x1 - x0) * (y1 - y0))
    for k in range(1, len(points)):
        if level <= points[k][0]:
            x0, y0 = points[k - 1]
            x1, y1 = points[k]
            if x1 == x0:
                return int(y1)
            ratio = (level - x0) / (x1 - x0)
            return math.floor(y0 + ratio * (y1 - y0))
    return int(points[-1][1])


# --------------------------------------------------------------------------- #
# skills.txt slice parsing.
# --------------------------------------------------------------------------- #
def read_rows(path: Path) -> dict[int, dict]:
    rows: dict[int, dict] = {}
    for row in csv.DictReader(path.read_bytes().decode("latin-1").splitlines(), delimiter="\t"):
        raw = (row.get("SkillId") or "").strip()
        if not raw or not raw.lstrip("-").isdigit():
            continue
        rows[int(raw)] = row
    return rows


def queried_attr_pairs(row: dict) -> list[tuple[str, str]]:
    pairs: list[tuple[str, str]] = []
    for n in range(1, 21):
        attr = (row.get(f"LvlSetting{n}") or "").strip()
        key = (row.get(f"LvlData{n}") or "").strip()
        if attr and key:
            pairs.append((attr, key))
    return pairs


# --------------------------------------------------------------------------- #
# Build.
# --------------------------------------------------------------------------- #
def build(lua_path: Path, learned_slice: Path, target_slice: Path) -> bytes:
    lua_bytes = lua_path.read_bytes()
    if digest(lua_bytes) != LUA_SHA256:
        raise SystemExit(f"vendored tangmen.lua hash drift: {lua_path}")
    learned_bytes = learned_slice.read_bytes()
    if digest(learned_bytes) != LEARNED_SLICE_SHA256:
        raise SystemExit(f"TangMen learned slice hash drift: {learned_slice}")
    target_bytes = target_slice.read_bytes()
    if digest(target_bytes) != TARGET_SLICE_SHA256:
        raise SystemExit(f"TangMen relationship-target slice hash drift: {target_slice}")

    skills_lua = parse_skills_table(lua_bytes.decode("latin-1"))

    all_rows: dict[int, dict] = {}
    all_rows.update(read_rows(learned_slice))
    all_rows.update(read_rows(target_slice))

    materialized_skills: list[dict] = []
    flat_rows: list[dict] = []
    residual_globals: list[dict] = []

    for skill_id in sorted(all_rows):
        row = all_rows[skill_id]
        lvl_script = (row.get("LvlSetScript") or "").strip().replace("\\", "/")
        if not lvl_script.endswith(TANGMEN_LVL_SCRIPT):
            continue
        if skill_id in SHARED_ROOT_IDS:
            continue
        max_level = int((row.get("MaxLevel") or "0").strip() or "0")
        if max_level <= 0:
            continue
        pairs = queried_attr_pairs(row)

        per_skill_supported: list[tuple[str, str]] = []
        per_skill_residuals: list[str] = []
        for attr, luakey in pairs:
            mapping = SUPPORTED_ATTRS.get(attr)
            present = luakey in skills_lua and isinstance(skills_lua[luakey], dict) and attr in skills_lua[luakey]
            if mapping and present:
                kind, bucket = mapping
                sub = normalize_subtables(skills_lua[luakey][attr])
                for level in range(1, max_level + 1):
                    v1 = link_pc(level, sub[1])
                    v2 = link_pc(level, sub[2])
                    v3 = link_pc(level, sub[3])
                    flat_rows.append({
                        "skillId": skill_id,
                        "level": level,
                        "bucket": bucket,
                        "kind": kind,
                        "v1": v1,
                        "v2": v2,
                        "v3": v3,
                    })
                per_skill_supported.append((attr, luakey))
            else:
                # Residual: either unsupported kind, or the queried lua key does
                # not define the attribute (e.g. deadlystrike_p:nutang150 -> nil).
                tag = "unsupported_kind" if mapping is None else "nil_in_lua"
                per_skill_residuals.append(f"{attr}:{luakey} ({tag})")

        if not per_skill_supported:
            continue

        materialized_skills.append({
            "skillId": skill_id,
            "maxLevel": max_level,
            "supported": [ {"attr": a, "luaKey": k} for a, k in per_skill_supported ],
            "residuals": per_skill_residuals,
            "luaQueryPairs": [ {"attr": a, "luaKey": k} for a, k in pairs ],
        })
        if per_skill_residuals:
            residual_globals.append({
                "skillId": skill_id,
                "residuals": per_skill_residuals,
            })

    # Deterministic ordering for the flat rows.
    bucket_rank = {"damage": 0, "state": 1, "skill": 2}
    kind_rank = {name: i for i, name in enumerate([
        "PhysicsEnhanceP", "SeriesDamageP", "PoisonDamageV", "PhysicsDamageV",
        "ColdDamageV", "FireDamageV", "LightingDamageV", "DeadlyStrikeP",
        "StunP", "SkillCostV",
    ])}
    flat_rows.sort(key=lambda r: (r["skillId"], r["level"], bucket_rank[r["bucket"]], kind_rank.get(r["kind"], 99), r["v1"], r["v2"], r["v3"]))

    payload = {
        "schema": "vltk.tangmen.leveldata/v1",
        "sourceLua": {
            "path": "Assets/StreamingAssets/Reference/PcTangMenSkillLevelData.lua",
            "sha256": LUA_SHA256,
            "bytes": len(lua_bytes),
            "provenance": "Assets/StreamingAssets/Reference/PcTangMenSkillLevelData.lua.provenance.json",
        },
        "learnedSliceSha256": LEARNED_SLICE_SHA256,
        "targetSliceSha256": TARGET_SLICE_SHA256,
        "excludedSharedRoots": sorted(SHARED_ROOT_IDS),
        "bucketConvention": "damage=<PhysicsEnhanceP,SeriesDamageP,*DamageV>; state=<DeadlyStrikeP,StunP>; skill=<SkillCostV>; mirrors the existing TangMen shared-root builders (43/45/47/48/50/54/58)",
        "linkSemantics": "Canonical PC tangmen.lua Link()/Line: linear between adjacent curve marks and extrapolated from the nearest endpoint segment outside the marks.",
        "materializedSkillCount": len(materialized_skills),
        "residualUnsupported": residual_globals,
        "skills": materialized_skills,
        "rows": flat_rows,
    }
    return (json.dumps(payload, ensure_ascii=True, sort_keys=False, separators=(",", ":")) + "\n").encode("ascii")


def main() -> int:
    root = Path(__file__).resolve().parents[1]
    ref = root / "Assets/StreamingAssets/Reference"
    parser = argparse.ArgumentParser()
    parser.add_argument("--lua", type=Path, default=ref / "PcTangMenSkillLevelData.lua")
    parser.add_argument("--learned-slice", type=Path, default=ref / "PcTangMenSkills.txt")
    parser.add_argument("--target-slice", type=Path, default=ref / "PcTangMenRelationshipTargets.txt")
    parser.add_argument("--output", type=Path, default=ref / "PcTangMenSkillLevelData.json")
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()

    expected = build(args.lua, args.learned_slice, args.target_slice)
    expected_hash = digest(expected)
    hash_path = args.output.with_suffix(args.output.suffix + ".sha256")
    expected_hash_text = f"{expected_hash}  {args.output.name}\n"

    if args.check:
        if not args.output.is_file() or args.output.read_bytes() != expected:
            raise SystemExit(f"stale TangMen level-data reference: run {Path(__file__).name}")
        if not hash_path.is_file() or hash_path.read_text(encoding="ascii") != expected_hash_text:
            raise SystemExit(f"stale TangMen level-data hash: run {Path(__file__).name}")
        print(f"TangMen level-data OK: sha256={expected_hash}")
        return 0

    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_bytes(expected)
    hash_path.write_text(expected_hash_text, encoding="ascii", newline="\n")
    print(f"wrote {args.output} (sha256={expected_hash})")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
