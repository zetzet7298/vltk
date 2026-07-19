"""Deterministic inventory tests for the all-faction skill membership matrix.

These recomputations are independent of the generator's own ranking/union code
where possible; they fail (rather than force) if the canonical evidence moves.
"""

from __future__ import annotations

import json
import re
import subprocess
import sys
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "scripts"))
import audit_skill_coverage as audit

CANONICAL_ROOT = audit.CANONICAL_ROOT
PROGRESSION = CANONICAL_ROOT / "01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/script/global/skills_table.lua"
SKILLBOOK = CANONICAL_ROOT / "01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/script/item/skillbook.lua"
SKILLS_TXT = CANONICAL_ROOT / "pak_unpacked/slistcache/settings/skills.txt"
PANEL = (ROOT / "Assets/Scripts/UI/PcSkillPanelService.cs").read_text(encoding="utf-8")

MATRIX = ROOT / "harness/docs/stories/SKL-ALL-PARITY-001/coverage-matrix.json"
SLICE = ROOT / "Assets/StreamingAssets/Reference/PcAllFactionLearnedDisplaySkills.txt"
PROVENANCE = ROOT / "Assets/StreamingAssets/Reference/PcAllFactionLearnedDisplaySkills.provenance.json"


# ---- independent re-implementation of the membership/ranking math ----

def _progression(faction: str) -> set[int]:
    lines = PROGRESSION.read_bytes().splitlines()
    start = next(i for i, l in enumerate(lines) if re.match(rb"^\s*" + faction.encode() + rb"\s*=\s*\{\s*$", l))
    end = next(i for i in range(start + 1, len(lines)) if re.match(rb"^\s*[A-Za-z_][A-Za-z0-9_]*\s*=\s*\{\s*$", lines[i]))
    ids: set[int] = set()
    for l in lines[start + 1:end]:
        m = re.match(rb"^\s*\[\d+\]\s*=\s*\{([^}]*)\}", l)
        if m:
            ids |= {int(v) for v in re.findall(rb"\d+", m.group(1))}
    return ids


def _skillbook(index: int) -> set[int]:
    pat = rb"^\s*\[" + str(index).encode() + rb"\]\s*=\s*\{(.*)\}\s*,?\s*$"
    row = next(re.match(pat, l) for l in SKILLBOOK.read_bytes().splitlines() if re.match(pat, l))
    ids: set[int] = set()
    for _, vals in re.findall(rb"\[(\d+)\]\s*=\s*\{([^}]*)\}", row.group(1)):
        ids |= {int(v) for v in re.findall(rb"\d+", vals)}
    return ids


def _display(order: str) -> set[int]:
    m = re.search(rf"public static readonly int\[\] {order}\s*=\s*\{{(.*?)\}};", PANEL, re.S)
    body = re.sub(r"//.*", "", m.group(1))
    return {int(v) for v in re.findall(r"\b\d+\b", body)}


def _all_factions():
    out = {}
    for key, _, order, faction, idx in audit.FACTIONS:
        learned = _progression(faction) | _skillbook(idx)
        display = _display(order)
        out[key] = {
            "learned": learned,
            "display": display,
            "union": learned | display,
            "shared": learned & display,
            "learned_only": learned - display,
            "unity_only": display - learned,
        }
    return out


def test_canonical_sources_match_pinned_hashes():
    audit.verify_canonical_sources(CANONICAL_ROOT)
    assert audit.digest(PROGRESSION.read_bytes()) == audit.PROGRESSION_SHA
    assert audit.digest(SKILLBOOK.read_bytes()) == audit.SKILLBOOK_SHA
    # Encoded skills.txt is never read/hashed here; its source hash authority is
    # the checked vltktool provenance.
    assert json.loads(PROVENANCE.read_text())["source"]["sha256"] == audit.SKILLS_SHA


def test_global_union_size_is_242():
    fac = _all_factions()
    union = set()
    for f in fac.values():
        union |= f["union"]
    assert len(union) == 242


def test_emei_probe_is_gap_14_before_emei_completed_wave_exclusion():
    fac = _all_factions()
    completed = audit.verify_completed_waves(ROOT, [dict(key=key, **value) for key, value in fac.items()])
    completed.pop("EMei")
    rel_fields = ("ChildSkillId", "StartSkillId", "FlySkillId", "CollidSkillId", "VanishedSkillId")
    slice_rows = audit.read_slice_rows(SLICE.read_bytes())
    candidates = []
    for key, f in fac.items():
        if key in completed:
            continue
        gap = len(f["learned_only"]) + len(f["unity_only"])
        rel = sum(
            1 for sid in f["union"]
            if any(audit._int(slice_rows[sid].get(fld, "")) for fld in rel_fields)
        )
        candidates.append((key, gap, rel))
    candidates.sort(key=lambda c: (-c[1], -c[2], c[0]))
    assert candidates[0] == ("EMei", 14, 20)


def test_emei_partition_is_pinned_without_promoting_kunlun_90():
    emei = _all_factions()["EMei"]
    assert emei["learned"] == {77, 79, 80, 82, 85, 86, 88, 89, 91, 92, 93, 252, 282, 328, 332, 380, 385, 712, 1061, 1062, 1114}
    assert emei["unity_only"] == {81, 83, 84, 87}
    assert 90 not in emei["learned"]


def test_committed_matrix_matches_recompute():
    sources = {"root": CANONICAL_ROOT, "skill_txt": SKILLS_TXT, "progression": PROGRESSION, "skillbook": SKILLBOOK}
    _, serialized = audit.build(ROOT, sources, SLICE, PROVENANCE)
    assert serialized == MATRIX.read_bytes()
    assert audit.digest(serialized) == "866532662b6440e3f35257b9d4840412d3432c13f822cd15eccadaf8db9f3254"


def test_partitions_and_unions_are_exact():
    data = json.loads(MATRIX.read_text())
    for fe in data["factions"]:
        shared = {r["skill_id"] for r in fe["membership_rows"] if r["classification"] == "shared"}
        learned_only = {r["skill_id"] for r in fe["membership_rows"] if r["classification"] == "pc_learned_only"}
        unity_only = {r["skill_id"] for r in fe["membership_rows"] if r["classification"] == "unity_display_only_unresolved"}
        union = set(fe["union_skill_ids"])
        # Three classes partition the union exactly.
        assert shared.isdisjoint(learned_only)
        assert shared.isdisjoint(unity_only)
        assert learned_only.isdisjoint(unity_only)
        assert shared | learned_only | unity_only == union
        # Union is exactly learned ∪ display.
        assert set(fe["pc_learned_evidence_skill_ids"]) | set(fe["unity_display_skill_ids"]) == union
        # Row count equals union size.
        assert len(fe["membership_rows"]) == len(union)
        # Gap and counts are consistent.
        assert fe["pc_learned_only_count"] == len(learned_only)
        assert fe["unity_display_only_unresolved_count"] == len(unity_only)
        assert fe["symmetric_gap_count"] == len(learned_only) + len(unity_only)


def test_every_faction_membership_matches_independent_sources():
    independent = _all_factions()
    data = json.loads(MATRIX.read_text())
    for fe in data["factions"]:
        expected = independent[fe["key"]]
        assert set(fe["pc_learned_evidence_skill_ids"]) == expected["learned"]
        assert set(fe["unity_display_skill_ids"]) == expected["display"]
        assert set(fe["union_skill_ids"]) == expected["union"]
        classes = {row["skill_id"]: row["classification"] for row in fe["membership_rows"]}
        assert {sid for sid, cls in classes.items() if cls == "shared"} == expected["shared"]
        assert {sid for sid, cls in classes.items() if cls == "pc_learned_only"} == expected["learned_only"]
        assert {sid for sid, cls in classes.items() if cls == "unity_display_only_unresolved"} == expected["unity_only"]


def test_global_partition_invariant():
    data = json.loads(MATRIX.read_text())
    seen = set()
    for fe in data["factions"]:
        for r in fe["membership_rows"]:
            seen.add(r["skill_id"])
    assert seen == set(data["global_union_skill_ids"])
    s = data["summary_counts"]
    assert s["shared_total"] + s["pc_learned_only_total"] + s["unity_display_only_unresolved_total"] == s["union_rows_total"]


def test_ranking_order_and_exclusion():
    data = json.loads(MATRIX.read_text())
    keys = [r["key"] for r in data["ranking"]]
    completed = audit.verify_completed_waves(ROOT, _completed_factions())
    assert set(completed) == {"Shaolin", "TianWang", "EMei", "TianRen", "WuDang", "WuDu", "CuiYan", "TangMen", "CaiBang", "KunLun"}
    assert all(key not in keys for key in completed)
    # Deterministic sort: descending gap, then descending rel count, then key.
    triples = [(r["symmetric_gap_count"], r["relationship_bearing_union_row_count"], r["key"]) for r in data["ranking"]]
    assert triples == sorted(triples, key=lambda t: (-t[0], -t[1], t[2]))
    assert keys == []


def _completed_factions():
    return [dict(key=key, **value) for key, value in _all_factions().items()]


def test_completed_shaolin_display_drift_is_rejected():
    factions = _completed_factions()
    shaolin = next(faction for faction in factions if faction["key"] == "Shaolin")
    shaolin["display"] = set(shaolin["display"]) - {4}
    with pytest.raises(SystemExit, match="Shaolin completed-wave proof scope evidence drift"):
        audit.verify_completed_waves(ROOT, factions)


def test_completed_kunlun_display_drift_is_rejected():
    factions = _completed_factions()
    kunlun = next(faction for faction in factions if faction["key"] == "KunLun")
    kunlun["display"] = set(kunlun["display"]) - {167}
    with pytest.raises(SystemExit, match="KunLun completed-wave scope evidence drift"):
        audit.verify_completed_waves(ROOT, factions)


def test_completed_cuiyan_display_drift_is_rejected():
    factions = _completed_factions()
    cuiyan = next(faction for faction in factions if faction["key"] == "CuiYan")
    cuiyan["display"] = set(cuiyan["display"]) - {95}
    with pytest.raises(SystemExit, match="CuiYan completed-wave proof scope evidence drift"):
        audit.verify_completed_waves(ROOT, factions)


def test_proof_states_and_recommended_story():
    data = json.loads(MATRIX.read_text())
    by_key = {fe["key"]: fe for fe in data["factions"]}
    completed = audit.verify_completed_waves(ROOT, _completed_factions())
    assert {item["key"] for item in data["excluded_from_ranking"]} == set(completed)
    assert by_key["CaiBang"]["proof_state"] == "canonical_static_verified_display_scope"
    assert by_key["Shaolin"]["proof_state"] == "canonical_static_verified_learned_scope"
    assert by_key["TangMen"]["proof_state"] == "canonical_static_verified_learned_scope"
    assert by_key["KunLun"]["proof_state"] == "canonical_static_verified_learned_scope"
    assert by_key["EMei"]["proof_state"] == "canonical_static_verified_learned_scope"
    assert by_key["TianRen"]["proof_state"] == "canonical_static_verified_learned_scope"
    assert by_key["WuDang"]["proof_state"] == "canonical_static_verified_learned_scope"
    assert by_key["WuDu"]["proof_state"] == "canonical_static_verified_learned_scope"
    assert by_key["TianWang"]["proof_state"] == "canonical_static_verified_learned_scope"
    assert by_key["CuiYan"]["proof_state"] == "canonical_static_verified_learned_scope"
    rec = data["recommended_next_story"]
    assert rec["id"] is None
    assert rec["winner"] is None
    assert rec["symmetric_gap_count"] is None


def test_slice_and_provenance_boundary():
    manifest = json.loads(PROVENANCE.read_text())
    slice_bytes = SLICE.read_bytes()
    assert manifest["schema"] == "vltk.table-slice-provenance/v1"
    assert manifest["encoding"] == "byte-preserving"
    assert manifest["source"]["sha256"] == audit.SKILLS_SHA
    assert manifest["slice"]["sha256"] == audit.digest(slice_bytes)
    data = json.loads(MATRIX.read_text())
    assert manifest["requested_ids"] == data["global_union_skill_ids"]
    assert set(manifest["selected_ids"]) == set(data["global_union_skill_ids"])
    # Generator only parsed the slice: every membership row resolves to a slice line.
    line_by_id = {item["id"]: item["line"] for item in manifest["source_lines"]}
    for fe in data["factions"]:
        for r in fe["membership_rows"]:
            assert r["canonical_source"]["line"] == line_by_id[r["skill_id"]]


def test_generator_check_passes():
    result = subprocess.run(
        [sys.executable, str(ROOT / "scripts/audit_skill_coverage.py"), "--check"],
        capture_output=True, text=True,
    )
    assert result.returncode == 0, result.stderr or result.stdout


def test_stale_slice_is_detected(tmp_path):
    sources = {"root": CANONICAL_ROOT, "skill_txt": SKILLS_TXT, "progression": PROGRESSION, "skillbook": SKILLBOOK}
    bad_slice = tmp_path / "slice.txt"
    bad_slice.write_bytes(SLICE.read_bytes() + b"tamper")
    with pytest.raises(SystemExit, match="slice hash drift"):
        audit.build(ROOT, sources, bad_slice, PROVENANCE)


def test_stale_provenance_ids_are_detected(tmp_path):
    sources = {"root": CANONICAL_ROOT, "skill_txt": SKILLS_TXT, "progression": PROGRESSION, "skillbook": SKILLBOOK}
    manifest = json.loads(PROVENANCE.read_text())
    manifest["requested_ids"] = manifest["requested_ids"][:-1]
    bad = tmp_path / "prov.json"
    bad.write_text(json.dumps(manifest, ensure_ascii=False, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    with pytest.raises(SystemExit, match="requested_ids drift"):
        audit.build(ROOT, sources, SLICE, bad)


def test_membership_row_relationship_and_categories():
    data = json.loads(MATRIX.read_text())
    kl = next(fe for fe in data["factions"] if fe["key"] == "KunLun")
    # KunLun has relationship-bearing union rows (rel count 20).
    rel_rows = [r for r in kl["membership_rows"] if r["direct_relationships"]]
    assert len(rel_rows) == kl["relationship_bearing_union_row_count"]
    assert len(rel_rows) > 0
    # Categories never empty and first entry is the active style class.
    for r in kl["membership_rows"]:
        assert r["categories"]
        assert r["categories"][0] in {"missile_active", "melee_active", "buff_state", "passive", "unknown"}
