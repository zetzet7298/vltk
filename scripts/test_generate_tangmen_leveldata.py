#!/usr/bin/env python3
"""Deterministic parser/source tests for scripts/generate_tangmen_leveldata.py.

Pins the canonical lua hash, the generated reference hash, known per-level values
sourced from tangmen.lua (skill 302 learned + collide target 304), canonical
extrapolation past the last curve mark, and parser edge cases (bareword vs [n] subtable
keys, nil-in-lua queries, function-valued attributes).
"""

from __future__ import annotations

import importlib.util
import json
import math
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[1]
REF = ROOT / "Assets/StreamingAssets/Reference"

spec = importlib.util.spec_from_file_location("g", ROOT / "scripts/generate_tangmen_leveldata.py")
g = importlib.util.module_from_spec(spec)
spec.loader.exec_module(g)


def _load_ref() -> dict:
    return json.loads((REF / "PcTangMenSkillLevelData.json").read_text(encoding="ascii"))


def _build() -> bytes:
    return g.build(
        REF / "PcTangMenSkillLevelData.lua",
        REF / "PcTangMenSkills.txt",
        REF / "PcTangMenRelationshipTargets.txt",
    )


def test_vendored_lua_is_hash_pinned_to_canonical_server_source():
    lua = (REF / "PcTangMenSkillLevelData.lua").read_bytes()
    assert g.digest(lua) == g.LUA_SHA256
    assert g.digest(lua) == "3f2e7c2aba8329508adab3a6293f41be29a0d68a8d63ac5a34903bece0578c90"


def test_generator_reproduces_pinned_reference_byte_exact():
    expected = _build()
    on_disk = (REF / "PcTangMenSkillLevelData.json").read_bytes()
    assert on_disk == expected, "PcTangMenSkillLevelData.json is stale; rerun generate_tangmen_leveldata.py"


def test_generator_is_idempotent_and_hashes_match():
    expected = _build()
    expected_hash = g.digest(expected)
    sidecar = (REF / "PcTangMenSkillLevelData.json.sha256").read_text(encoding="ascii")
    assert sidecar == f"{expected_hash}  PcTangMenSkillLevelData.json\n"


def test_materializes_exactly_16_learned_plus_12_damage_bearing_targets():
    ref = _load_ref()
    ids = {s["skillId"] for s in ref["skills"]}
    learned = {249, 302, 303, 339, 341, 342, 343, 345, 347, 349, 351, 710, 1069, 1070, 1071, 1110}
    targets = {227, 301, 304, 340, 344, 346, 348, 350, 352, 1097, 1098, 1113}
    assert ids == learned | targets
    assert ref["materializedSkillCount"] == 28
    assert ref["excludedSharedRoots"] == [43, 45, 47, 48, 50, 54, 58]


def test_skill_302_learned_values_match_lua_baoyu_lihua():
    ref = _load_ref()
    rows = {(r["bucket"], r["kind"]): (r["v1"], r["v2"], r["v3"])
            for r in ref["rows"] if r["skillId"] == 302 and r["level"] == 1}
    # baoyu_lihua: physicsenhance_p {{{1,15},{15,200},{20,434}}} -> 15 at L1
    assert rows[("damage", "PhysicsEnhanceP")][0] == 15
    # seriesdamage_p {{{1,20},{15,20},{20,60},{21,62}}} -> 20 at L1
    assert rows[("damage", "SeriesDamageP")][0] == 20
    # poisondamage_v {{{1,1},{20,19}},{{1,60},{20,60}},{{1,10},{20,10}}} -> 1,60,10 at L1
    assert rows[("damage", "PoisonDamageV")] == (1, 60, 10)
    # skill_cost_v {{{1,25},{20,80}}} -> 25 at L1
    assert rows[("skill", "SkillCostV")][0] == 25
    # L20 physicsenhance_p endpoint
    r20 = next(r for r in ref["rows"] if r["skillId"] == 302 and r["level"] == 20
               and r["bucket"] == "damage" and r["kind"] == "PhysicsEnhanceP")
    assert r20["v1"] == 434


def test_collide_target_304_values_match_lua_duci_gu():
    ref = _load_ref()
    rows = {(r["bucket"], r["kind"]): (r["v1"], r["v2"], r["v3"])
            for r in ref["rows"] if r["skillId"] == 304 and r["level"] == 1}
    # duci_gu poisondamage_v {[1]={{1,8},{20,40}},[2]={{1,100},{20,100}},[3]={{1,10},{20,10}}}
    assert rows[("damage", "PoisonDamageV")] == (8, 100, 10)
    assert rows[("damage", "SeriesDamageP")][0] == 1
    assert rows[("skill", "SkillCostV")][0] == 20
    # 304 maxLevel is 30 (relationship target row), so it must emit 30 levels.
    levels = {r["level"] for r in ref["rows"] if r["skillId"] == 304}
    assert levels == set(range(1, 31))


def test_pc_extrapolation_past_last_curve_mark_for_1069():
    # physicsenhance_p {{{1,30},{15,180},{20,360},{23,576},{26,684}}}
    # -> canonical PC Link/Line extrapolates the final segment to L27=720.
    ref = _load_ref()
    r26 = next(r for r in ref["rows"] if r["skillId"] == 1069 and r["level"] == 26
               and r["kind"] == "PhysicsEnhanceP")
    r27 = next(r for r in ref["rows"] if r["skillId"] == 1069 and r["level"] == 27
               and r["kind"] == "PhysicsEnhanceP")
    assert r26["v1"] == 684
    assert r27["v1"] == 720


def test_nil_in_lua_queries_are_dropped_not_invented():
    # deadlystrike_p:nutang150 (1070) and deadlystrike_p:tianluo_diwang1 (227) are queried by
    # skills.txt but undefined in the referenced lua key -> must NOT appear as attributes.
    ref = _load_ref()
    for sid in (1070, 227):
        kinds = {(r["bucket"], r["kind"]) for r in ref["rows"] if r["skillId"] == sid}
        assert ("state", "DeadlyStrikeP") not in kinds, f"skill {sid} must not invent deadlystrike_p"
        residuals = next(s["residuals"] for s in ref["skills"] if s["skillId"] == sid)
        assert any("deadlystrike_p" in r and "nil_in_lua" in r for r in residuals)


def test_residuals_record_unsupported_kinds():
    ref = _load_ref()
    total = sum(len(s["residuals"]) for s in ref["skills"])
    assert total > 0
    # missle_lifetime_v / skill_attackradius / addskilldamage* / event+exp curves are unsupported.
    s302 = next(s for s in ref["skills"] if s["skillId"] == 302)
    residual_attrs = {r.split(":")[0] for r in s302["residuals"]}
    assert {"missle_lifetime_v", "skill_attackradius", "addskilldamage1",
            "skill_eventskilllevel", "skill_flyevent", "skill_showevent"} <= residual_attrs


def test_parser_handles_bareword_and_indexed_subtable_forms():
    lua = (REF / "PcTangMenSkillLevelData.lua").read_bytes().decode("latin-1")
    skills = g.parse_skills_table(lua)
    # pili_dan uses anonymous multi-subtable form; duci_gu uses explicit [1]/[2]/[3] form.
    pd = g.normalize_subtables(skills["pili_dan"]["poisondamage_v"])
    dg = g.normalize_subtables(skills["duci_gu"]["poisondamage_v"])
    assert pd[1] == [[1, 1], [20, 5]] and pd[2] == [[1, 60], [20, 60]] and pd[3] == [[1, 10], [20, 10]]
    assert dg[1] == [[1, 8], [20, 40]] and dg[2] == [[1, 100], [20, 100]] and dg[3] == [[1, 10], [20, 10]]
    # function-valued attribute (skill_desc) parses without raising and is skipped.
    assert "skill_desc" in skills["tangmen120"]
    assert skills["tangmen120"]["skill_desc"] is None


def test_link_matches_pc_extrapolation():
    # Within-range linear, endpoint extrapolation, absent => 0.
    assert g.link_pc(1, [[1, 50], [20, 344]]) == 50
    assert g.link_pc(20, [[1, 50], [20, 344]]) == 344
    assert g.link_pc(27, [[1, 50], [20, 344]]) == 452
    assert g.link_pc(0, [[1, 50], [20, 344]]) == 34
    assert g.link_pc(10, [[1, 50], [20, 344]]) == math.floor(50 + (10 - 1) / 19 * (344 - 50))
    assert g.link_pc(5, None) == 0
    # multi-mark: 1069 physicsenhance_p L20 between {15,180} and {20,360}.
    pts = [[1, 30], [15, 180], [20, 360], [23, 576], [26, 684]]
    assert g.link_pc(20, pts) == 360


def test_generator_rejects_drifted_lua_hash(monkeypatch):
    bad = REF / "PcTangMenSkillLevelData.lua"
    # Point the build at a file whose hash does not match the pinned canonical hash.
    other = ROOT / "Assets/StreamingAssets/Reference/PcTangMenOracle.json"
    with pytest.raises(SystemExit):
        g.build(other, REF / "PcTangMenSkills.txt", REF / "PcTangMenRelationshipTargets.txt")


if __name__ == "__main__":
    raise SystemExit(pytest.main([__file__, "-q"]))
