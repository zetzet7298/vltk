import json
import sys
from pathlib import Path

import pytest

sys.path.insert(0, str(Path(__file__).resolve().parent))
import generate_tangmen_oracle as oracle


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Assets/StreamingAssets/Reference/PcTangMenSkills.txt"
PROVENANCE = ROOT / "Assets/StreamingAssets/Reference/PcTangMenSkills.provenance.json"
CLASSIFICATION = ROOT / "harness/docs/stories/SKL-TM-PROOF-001/membership-classification.json"
TARGET_SOURCE = ROOT / "Assets/StreamingAssets/Reference/PcTangMenRelationshipTargets.txt"
TARGET_PROVENANCE = ROOT / "Assets/StreamingAssets/Reference/PcTangMenRelationshipTargets.provenance.json"
OUTPUT = ROOT / "Assets/StreamingAssets/Reference/PcTangMenOracle.json"


def build(source=SOURCE, provenance=PROVENANCE, classification=CLASSIFICATION,
          target_source=TARGET_SOURCE, target_provenance=TARGET_PROVENANCE):
    return oracle.build(
        source, provenance, classification, oracle.CANONICAL_ROOT,
        target_source, target_provenance,
    )


def write_json(path, data):
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2, sort_keys=True) + "\n")


def learned_rows():
    rows = {}
    for row in oracle.read_table_rows(SOURCE.read_bytes()):
        rows[oracle.integer(row["SkillId"])] = row
    return rows


def test_committed_oracle_matches_canonical_inputs():
    oracle.verify_canonical_sources(oracle.CANONICAL_ROOT)
    assert build() == OUTPUT.read_bytes()
    assert oracle.digest(OUTPUT.read_bytes()) == "e4270bd12a534b229c962c3fc322a9271aaefc6b99d062e3df0711a5b0f84f89"


def test_canonical_membership_union_is_exact():
    progression = oracle.progression_ids(
        oracle.CANONICAL_ROOT / "01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/script/global/skills_table.lua"
    )
    skillbooks = oracle.skillbook_by_level(
        oracle.CANONICAL_ROOT / "01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/script/item/skillbook.lua"
    )
    learned = sorted(set(progression) | {skill_id for ids in skillbooks.values() for skill_id in ids})
    assert learned == [43, 45, 47, 48, 50, 54, 58, 249, 302, 303, 339, 341, 342, 343, 345, 347, 349, 351, 710, 1069, 1070, 1071, 1110]


def test_special_progression_ids_are_validated(tmp_path):
    data = json.loads(CLASSIFICATION.read_text())
    data["special_progression_ids"] = [302, 339, 342]  # dropped 710
    changed = tmp_path / "classification.json"
    write_json(changed, data)
    with pytest.raises(SystemExit, match="special progression"):
        build(classification=changed)


def test_relationship_target_closure_is_exact():
    derived = oracle.relationship_target_ids(learned_rows())
    assert derived == oracle.EXPECTED_RELATIONSHIP_TARGET_IDS
    assert len(derived) == 32


def test_58_targets_227_via_collide():
    rows = learned_rows()
    assert rows[58]["CollidSkillId"].strip() == "227"
    assert 227 in oracle.relationship_target_ids(rows)


def test_relationship_targets_do_not_overlap_learned():
    progression = oracle.progression_ids(
        oracle.CANONICAL_ROOT / "01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/script/global/skills_table.lua"
    )
    skillbooks = oracle.skillbook_by_level(
        oracle.CANONICAL_ROOT / "01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/script/item/skillbook.lua"
    )
    learned = set(progression) | {sid for ids in skillbooks.values() for sid in ids}
    assert not (set(oracle.EXPECTED_RELATIONSHIP_TARGET_IDS) & learned)


def test_target_slice_tamper_is_rejected(tmp_path):
    changed = tmp_path / "targets.txt"
    changed.write_bytes(TARGET_SOURCE.read_bytes() + b"tamper")
    with pytest.raises(SystemExit, match="target slice hash drift"):
        build(target_source=changed)


def test_target_provenance_ids_drift_is_rejected(tmp_path):
    data = json.loads(TARGET_PROVENANCE.read_text())
    data["selected_ids"] = data["selected_ids"][:-1]  # drop last id, keep slice
    data["requested_ids"] = data["selected_ids"]
    changed = tmp_path / "targets.provenance.json"
    write_json(changed, data)
    with pytest.raises(SystemExit, match="target provenance selected IDs drift"):
        build(target_provenance=changed)


def test_string_presence_semantics_are_explicit():
    data = json.loads(OUTPUT.read_text())
    by_id = {skill["skillId"]: skill for skill in data["skills"]}
    string_targets = set(oracle.STRING_FIELDS.values())
    # Absent source cells never materialize as default "" keys and never claim presence.
    s43 = by_id[43]
    assert "manCastSndPath" not in s43
    assert "manCastSndPath" not in s43["present"]
    assert "fmCastSndPath" not in s43
    # Nonempty source cells appear as both a key and a `present` entry.
    s45 = by_id[45]
    assert s45["manCastSndPath"] == "\\sound\\skill\\sound_k001.wav"
    assert "manCastSndPath" in s45["present"]
    assert "fmCastSndPath" in s45["present"]
    assert "lvlSetScript" in s45["present"]
    # Every skill's `present` set equals exactly its emitted non-meta keys.
    meta = {"skillId", "sourceLine", "membershipEvidence", "present"}
    for skill in data["skills"]:
        emitted = set(skill) - meta
        assert set(skill["present"]) == emitted
        assert all(value != "" for key, value in skill.items() if key in string_targets)


def test_negative_relationship_values_preserved():
    data = json.loads(OUTPUT.read_text())
    for skill in data["skills"]:
        assert skill["childSkillLevel"] == -1
        assert "childSkillLevel" in skill["present"]


def test_unresolved_unity_id_cannot_be_promoted(tmp_path):
    data = json.loads(CLASSIFICATION.read_text())
    data["unity_only_unresolved"][0]["oracle_include"] = True
    changed = tmp_path / "classification.json"
    write_json(changed, data)
    with pytest.raises(SystemExit, match="promoted"):
        build(classification=changed)


def test_ui_order_cannot_enter_static_oracle(tmp_path):
    data = json.loads(CLASSIFICATION.read_text())
    data["ui_contract"]["ordered_skill_ids"] = data["unity_display_ids"]
    changed = tmp_path / "classification.json"
    write_json(changed, data)
    with pytest.raises(SystemExit, match="UI order"):
        build(classification=changed)


def test_provenance_hash_tamper_is_rejected(tmp_path):
    data = json.loads(PROVENANCE.read_text())
    data["slice"]["sha256"] = "0" * 64
    changed = tmp_path / "provenance.json"
    write_json(changed, data)
    with pytest.raises(SystemExit, match="slice hash drift"):
        build(provenance=changed)


def test_slice_tamper_is_rejected(tmp_path):
    changed = tmp_path / "slice.txt"
    changed.write_bytes(SOURCE.read_bytes() + b"tamper")
    with pytest.raises(SystemExit, match="slice hash drift"):
        build(source=changed)
