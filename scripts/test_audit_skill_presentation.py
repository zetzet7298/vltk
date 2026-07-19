"""Deterministic checks for PC presentation inventory artifact."""

from __future__ import annotations

import hashlib
import json
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "scripts"))
import audit_skill_presentation as audit  # type: ignore

ARTIFACT = ROOT / "Assets/StreamingAssets/Reference/PcAllFactionPresentationInventory.json"
META = ROOT / "Assets/StreamingAssets/Reference/PcAllFactionPresentationInventory.json.meta"
EXPECTED_SHA256 = "a59462df02b8f422aab62f368a70ac27b4c8ffb1fe26e4f2f4ee283a15d124fd"

CANONICAL_CHILD_SKILLS = {
    712: (718, 720),
    1055: (1083, 1083),
    1058: (1084, 1084),
    1059: (1087, 1087),
    1060: (1088, 1088),
}


def _build():
    sources = audit.resolve_sources(ROOT)
    return audit.build_inventory(ROOT, sources, ARTIFACT, check=False)


def test_inventory_matches_deterministic_recompute():
    inventory, serialized = _build()
    assert inventory["schema"] == audit.SCHEMA
    assert serialized == ARTIFACT.read_bytes()
    assert hashlib.sha256(serialized).hexdigest() == EXPECTED_SHA256


def test_row_and_source_counts_are_pinned():
    data = json.loads(ARTIFACT.read_text(encoding="utf-8"))
    summary = data["summary_counts"]
    assert summary["rows"] == 242
    assert len(data["rows"]) == 242
    assert summary["global_union_rows"] == 242
    assert summary["factions"] == 10
    assert summary["source_counts"] == {"skill_slice_rows": 242, "full_skill_rows": 1711, "missile_rows": 513, "state_rows": 49}
    assert {f["key"]: f["union_count"] for f in data["factions"]} == {
        "Shaolin": 26,
        "TianWang": 23,
        "TangMen": 23,
        "WuDu": 24,
        "EMei": 25,
        "CuiYan": 19,
        "CaiBang": 26,
        "TianRen": 25,
        "WuDang": 22,
        "KunLun": 29,
    }


def test_proof_state_and_blocker_counts_are_explicit():
    summary = json.loads(ARTIFACT.read_text(encoding="utf-8"))["summary_counts"]
    assert summary["field_proof_state_counts"] == {"verified": 9196}
    assert summary["link_proof_state_counts"] == {"external_verified": 220, "pc_absent_no_visual": 5, "pc_stub_no_visual": 12, "source_only": 1181, "verified": 34}
    assert summary["row_proof_state_counts"] == {"source_only": 242}
    assert summary["blocker_counts"] == {}
    assert summary["child_skill_relation_counts"] == {"canonical_skill": 34, "missile": 138, "none": 70}


def test_child_skill_join_uses_pc_base_skill_namespace():
    data = json.loads(ARTIFACT.read_text(encoding="utf-8"))
    rows = {row["skill_id"]: row for row in data["rows"]}
    full_source = data["source_provenance"]["pc_skills1_full"]

    for parent_id, (target_id, source_line) in CANONICAL_CHILD_SKILLS.items():
        link = rows[parent_id]["relations"]["child_skill"]
        assert link["target_id"] == target_id
        assert link["namespace_rule"] == "skill"
        assert link["namespace_source_ref"]["column"] == "BaseSkill"
        assert link["target_kind"] == "canonical_skill"
        assert link["proof_state"] == "verified"
        assert link["blockers"] == []
        assert link["target_ref"]["skill_id"] == target_id
        assert link["target_ref"]["source_ref"] == {
            "path": full_source["path"],
            "sha256": full_source["sha256"],
            "line": source_line,
            "column": "SkillId",
            "encoding": "latin-1-byte-preserving",
        }

    missile_link = rows[358]["relations"]["child_skill"]
    assert missile_link["target_id"] == 167
    assert missile_link["namespace_rule"] == "missile"
    assert missile_link["target_kind"] == "missile"
    assert missile_link["proof_state"] == "external_verified"
    assert missile_link["target_ref"]["missile_id"] == 167
    assert missile_link["target_ref"]["source_ref"]["path"] == data["source_provenance"]["missles1"]["path"]

    # These ids also exist in the missile table, but PC BaseSkill=0 dispatches
    # them through g_SkillManager as child skills (KSkills.cpp:1203-1213).
    for parent_id, target_id in {10: 216, 325: 408, 332: 333}.items():
        link = rows[parent_id]["relations"]["child_skill"]
        assert link["namespace_rule"] == "skill"
        assert link["target_id"] == target_id
        assert link["target_kind"] in {"skill", "canonical_skill"}
        assert link["proof_state"] == "verified"

    for row in data["rows"]:
        link = row["relations"]["child_skill"]
        if link["target_id"] <= 0:
            continue
        expected_namespace = "missile" if row["fields"]["base_skill"]["value"] != 0 else "skill"
        assert link["namespace_rule"] == expected_namespace
        assert (link["target_kind"] == "missile") == (expected_namespace == "missile")


def test_rows_have_field_level_refs_and_unity_state():
    data = json.loads(ARTIFACT.read_text(encoding="utf-8"))
    assert {row["skill_id"] for row in data["rows"]} == set(data["source_provenance"]["skill_slice"]["requested_ids"])
    for row in data["rows"]:
        for field in ("char_anim_id", "time_per_cast", "time_per_cast_on_horse", "man_cast_snd_path", "fm_cast_snd_path", "start_skill_id", "fly_skill_id", "collid_skill_id", "vanished_skill_id", "pre_cast_spr_path"):
            item = row["fields"][field]
            assert item["source_ref"]["column"]
            assert item["proof_state"] == "verified"
            assert item["blockers"] == []
            assert isinstance(item["unity_ref"], list) and item["unity_ref"]
            for candidate in item["unity_ref"]:
                assert candidate["status"] == "verified"
                assert candidate["owner_method"]
                assert candidate["source_column"] == item["source_ref"]["column"]
                assert "lines" not in candidate
        assert row["fields"]["pre_cast_spr_path"]["unity_ref"][0]["match_kind"] == "parser_local_to_effect_source"
        assert row["fields"]["collid_skill_id"]["unity_ref"][0]["match_kind"] == "factory_local_to_field"


def test_unity_binding_matcher_is_fail_closed():
    direct = dict(audit.UNITY_FIELD_BINDINGS["skill_style"])
    direct_src = """
    class PcConfigParser {
      public static List<SkillDefinition> ParseSkillsLines(IReadOnlyList<string> lines) {
        var skill = new SkillDefinition();
        int ci = 0;
        ci += 4;
        skill.skillStyle = (PcSkillStyle)IntCol(cols, ref ci);
      }
    }
    """
    assert audit.match_unity_field_binding("Fixture.cs", direct_src, direct)["match_kind"] == "parser_direct_field"

    wrong_property = direct_src.replace("skill.skillStyle", "skill.stateSpecialId")
    assert audit.match_unity_field_binding("Fixture.cs", wrong_property, direct) is None

    wrong_column = direct_src.replace("ci += 4", "ci += 5")
    assert audit.match_unity_field_binding("Fixture.cs", wrong_column, direct) is None

    line_comment = direct_src.replace("skill.skillStyle", "// skill.skillStyle")
    assert audit.match_unity_field_binding("Fixture.cs", line_comment, direct) is None

    block_comment = direct_src.replace("skill.skillStyle = (PcSkillStyle)IntCol(cols, ref ci);", "/* skill.skillStyle = (PcSkillStyle)IntCol(cols, ref ci); */")
    assert audit.match_unity_field_binding("Fixture.cs", block_comment, direct) is None

    string_literal = direct_src.replace("skill.skillStyle = (PcSkillStyle)IntCol(cols, ref ci);", 'var text = "skill.skillStyle = (PcSkillStyle)IntCol(cols, ref ci);";')
    assert audit.match_unity_field_binding("Fixture.cs", string_literal, direct) is None

    stale_symbol = direct_src.replace("ParseSkillsLines", "ParseSkillLines")
    assert audit.match_unity_field_binding("Fixture.cs", stale_symbol, direct) is None

    other_method = direct_src.replace("ParseSkillsLines", "OtherMethod")
    assert audit.match_unity_field_binding("Fixture.cs", other_method, direct) is None

    event = dict(audit.UNITY_FIELD_BINDINGS["start_skill_id"])
    event_src = """
    class PcCombatCatalogFactory {
      private static void ApplyCaiBangPcStaticRows(List<SkillDefinition> skills) {
        skill.startSkillId = Value(row, "StartSkillId", skill.startSkillId);
      }
    }
    """
    assert audit.match_unity_field_binding("Fixture.cs", event_src, event)["match_kind"] == "factory_value_to_field"
    assert audit.match_unity_field_binding("Fixture.cs", event_src.replace("StartSkillId", "FlySkillId"), event) is None

    precast = dict(audit.UNITY_FIELD_BINDINGS["pre_cast_spr_path"])
    precast_src = """
    class PcConfigParser {
      public static List<SkillDefinition> ParseSkillsLines(IReadOnlyList<string> lines) {
        var skill = new SkillDefinition();
        int ci = 0; ci += 6;
        string preCastSprPath = ColSafe(cols, ci); ci++;
        if (!string.IsNullOrEmpty(preCastSprPath) && skill.effectSourceId == null) {
          skill.effectSourceId = new SourceAssetId { sourcePath = preCastSprPath };
        }
      }
    }
    """
    assert audit.match_unity_field_binding("Fixture.cs", precast_src, precast)["match_kind"] == "parser_local_to_effect_source"
    assert audit.match_unity_field_binding("Fixture.cs", precast_src.replace("skill.effectSourceId", "skill.iconSourceId"), precast) is None


EXPECTED_EXTERNAL_EVENTS = {
    (20, "start_skill", 22),
    (82, "start_skill", 243),
    (102, "start_skill", 398),
    (111, "start_skill", 112),
    (148, "start_skill", 192),
    (172, "start_skill", 399),
    (328, "start_skill", 329),
    (368, "start_skill", 371),
    (380, "start_skill", 331),
    (715, "start_skill", 723),
    (716, "start_skill", 738),
    (1057, "start_skill", 1085),
    (1061, "start_skill", 1089),
    (1065, "start_skill", 1102),
    (1066, "start_skill", 1094),
    (1073, "start_skill", 1101),
    (1075, "start_skill", 1131),
    (1079, "start_skill", 1107),
    (302, "fly_skill", 301),
    (337, "fly_skill", 338),
    (1065, "fly_skill", 1093),
    (1070, "fly_skill", 1098),
    (1073, "fly_skill", 1103),
    (1114, "fly_skill", 1115),
    (58, "collide_skill", 227),
    (303, "collide_skill", 304),
    (339, "collide_skill", 340),
    (343, "collide_skill", 344),
    (345, "collide_skill", 346),
    (347, "collide_skill", 348),
    (349, "collide_skill", 350),
    (351, "collide_skill", 352),
    (355, "collide_skill", 383),
    (375, "collide_skill", 387),
    (1063, "collide_skill", 1064),
    (1067, "collide_skill", 1095),
    (1069, "collide_skill", 1097),
    (13, "vanish_skill", 188),
    (15, "vanish_skill", 186),
    (18, "vanish_skill", 185),
    (353, "vanish_skill", 354),
    (362, "vanish_skill", 363),
    (1076, "vanish_skill", 363),
    (1081, "vanish_skill", 1109),
    (1110, "vanish_skill", 1113),
}


def test_external_event_targets_use_full_table_without_runtime_claim():
    data = json.loads(ARTIFACT.read_text(encoding="utf-8"))
    rows = {row["skill_id"]: row for row in data["rows"]}
    actual = set()
    for row in rows.values():
        for rel in ("start_skill", "fly_skill", "collide_skill", "vanish_skill"):
            link = row["relations"][rel]
            if link["target_kind"] == "external_canonical_skill":
                actual.add((row["skill_id"], rel, link["target_id"]))
                assert link["proof_state"] == "external_verified"
                assert link["runtime_registration"] == "not_asserted"
                assert link["target_id"] not in rows
                assert link["target_ref"]["source_ref"]["path"] == data["source_provenance"]["pc_skills1_full"]["path"]
                assert link["blockers"] == []
    assert actual == EXPECTED_EXTERNAL_EVENTS


def test_state_residuals_are_no_visual_taxonomy_not_missing():
    data = json.loads(ARTIFACT.read_text(encoding="utf-8"))
    rows = {row["skill_id"]: row for row in data["rows"]}
    expected_stub = {(15, 52), (90, 64), (175, 54), (273, 53), (277, 57), (282, 55), (332, 56), (356, 54), (364, 58), (391, 59), (392, 63), (394, 60)}
    expected_absent = {(174, 66), (177, 65), (393, 65), (716, 122), (720, 120)}

    for skill_id, state_id in expected_stub:
        link = rows[skill_id]["relations"]["state_visual"]
        assert link["target_id"] == state_id
        assert link["target_kind"] == "pc_state_stub_no_bytes"
        assert link["proof_state"] == "pc_stub_no_visual"
        assert link["target_ref"]["visual_claim"] == "none"
        assert link["blockers"] == []

    for skill_id, state_id in expected_absent:
        link = rows[skill_id]["relations"]["state_visual"]
        assert link["target_id"] == state_id
        assert link["target_kind"] == "pc_state_absent_no_visual"
        assert link["proof_state"] == "pc_absent_no_visual"
        assert link["target_ref"]["visual_claim"] == "none"
        assert link["blockers"] == []



def test_all_242_skill_refs_dereference_exact_slice_row_and_column():
    data = json.loads(ARTIFACT.read_text(encoding="utf-8"))
    slice_path = ROOT / data["source_provenance"]["skill_slice"]["path"]
    raw = slice_path.read_bytes()
    assert hashlib.sha256(raw).hexdigest() == data["source_provenance"]["skill_slice"]["sha256"]
    lines = raw.decode("latin-1").splitlines()
    header = lines[0].split("\t")
    skill_id_col = header.index("SkillId")

    for row in data["rows"]:
        refs = [row["source_ref"]]
        refs.extend(field["source_ref"] for field in row["fields"].values())
        refs.extend(row["relations"][key]["source_ref"] for key in (
            "child_skill", "state_visual", "start_skill", "fly_skill", "collide_skill", "vanish_skill"
        ))
        for ref in refs:
            assert ref["path"] == data["source_provenance"]["skill_slice"]["path"]
            assert ref["sha256"] == hashlib.sha256(raw).hexdigest()
            assert 2 <= ref["line"] <= len(lines)
            cells = lines[ref["line"] - 1].split("\t")
            assert int(cells[skill_id_col]) == row["skill_id"]
            assert ref["column"] in header


def test_artifact_is_checkout_path_independent_and_has_no_absolute_paths(tmp_path):
    inventory, serialized = _build()
    assert "/var/www/" not in serialized.decode("utf-8")

    alias = tmp_path / "repo-alias"
    alias.symlink_to(ROOT, target_is_directory=True)
    alias_sources = audit.resolve_sources(alias)
    _, alias_serialized = audit.build_inventory(
        alias,
        alias_sources,
        alias / "Assets/StreamingAssets/Reference/PcAllFactionPresentationInventory.json",
        check=False,
    )
    assert alias_serialized == serialized


def test_generator_distinguishes_freshness_from_blocked_source_audit(tmp_path):
    fresh = subprocess.run(
        [sys.executable, str(ROOT / "scripts/audit_skill_presentation.py"), "--freshness-only"],
        capture_output=True,
        text=True,
    )
    assert fresh.returncode == 0, fresh.stderr or fresh.stdout
    assert "presentation inventory freshness-only: rows=242" in fresh.stdout

    checked = subprocess.run(
        [sys.executable, str(ROOT / "scripts/audit_skill_presentation.py"), "--check"],
        capture_output=True,
        text=True,
    )
    if checked.returncode == 0:
        assert "presentation inventory check: rows=242" in checked.stdout
    else:
        assert "presentation inventory blocked: missile_audit_hash_mismatch" in (checked.stderr + checked.stdout)

    stale = tmp_path / "inventory.json"
    stale.write_bytes(ARTIFACT.read_bytes() + b"tamper")
    bad = subprocess.run(
        [sys.executable, str(ROOT / "scripts/audit_skill_presentation.py"), "--freshness-only", "--output", str(stale)],
        capture_output=True,
        text=True,
    )
    assert bad.returncode != 0
    assert "stale presentation inventory" in (bad.stderr + bad.stdout)


def test_unity_meta_is_stable_for_streaming_asset():
    assert META.read_text(encoding="ascii").splitlines()[1] == "guid: 29a269ba697f3aea5053d7ea27ecfbdf"
