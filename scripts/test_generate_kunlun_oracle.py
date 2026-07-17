import json
import re
import sys
from pathlib import Path

import pytest

sys.path.insert(0, str(Path(__file__).resolve().parent))
import generate_kunlun_oracle as oracle


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Assets/StreamingAssets/Reference/PcKunLunSkills.txt"
PROVENANCE = ROOT / "Assets/StreamingAssets/Reference/PcKunLunSkills.provenance.json"
CLASSIFICATION = ROOT / "harness/docs/stories/SKL-KL-PROOF-001/membership-classification.json"
TARGET_SOURCE = ROOT / "Assets/StreamingAssets/Reference/PcKunLunRelationshipTargets.txt"
TARGET_PROVENANCE = ROOT / "Assets/StreamingAssets/Reference/PcKunLunRelationshipTargets.provenance.json"
OUTPUT = ROOT / "Assets/StreamingAssets/Reference/PcKunLunOracle.json"
PANEL = ROOT / "Assets/Scripts/UI/PcSkillPanelService.cs"


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


def progression_and_skillbooks():
    progression = oracle.progression_ids(oracle.CANONICAL_ROOT / oracle.PROGRESSION_REL)
    skillbooks = oracle.skillbook_by_level(oracle.CANONICAL_ROOT / oracle.SKILLBOOK_REL)
    return progression, skillbooks


def test_committed_oracle_matches_canonical_inputs():
    oracle.verify_canonical_sources(oracle.CANONICAL_ROOT)
    assert build() == OUTPUT.read_bytes()
    assert oracle.digest(OUTPUT.read_bytes()) == "3be6712946489b82d2595eae77894bcf022f0b6cd4d43977850572c700be399f"


def test_canonical_membership_union_is_exact():
    progression, skillbooks = progression_and_skillbooks()
    learned = sorted(set(progression) | {skill_id for ids in skillbooks.values() for skill_id in ids})
    assert learned == [
        90, 167, 168, 169, 171, 172, 173, 174, 175, 176, 178, 179, 181, 182,
        275, 372, 375, 392, 393, 394, 630, 717, 1080, 1081,
    ]
    assert len(learned) == 24


def test_frozen_display_observation_matches_unity_panel():
    panel = PANEL.read_text(encoding="utf-8")
    match = re.search(r"public static readonly int\[\] PcKunLunSkillOrder\s*=\s*\{(.*?)\};", panel, re.S)
    assert match is not None
    body = re.sub(r"//.*", "", match.group(1))
    observed = [int(value) for value in re.findall(r"\b\d+\b", body)]
    classification = json.loads(CLASSIFICATION.read_text())
    assert observed == classification["unity_display_ids"]


def test_shared_membership_tamper_is_rejected(tmp_path):
    data = json.loads(CLASSIFICATION.read_text())
    data["shared_ids"] = data["shared_ids"][:-1]
    changed = tmp_path / "classification.json"
    write_json(changed, data)
    with pytest.raises(SystemExit, match="shared membership"):
        build(classification=changed)


def test_special_progression_ids_are_validated(tmp_path):
    data = json.loads(CLASSIFICATION.read_text())
    data["special_progression_ids"] = [372, 375]  # dropped 717
    changed = tmp_path / "classification.json"
    write_json(changed, data)
    with pytest.raises(SystemExit, match="special progression"):
        build(classification=changed)


def test_relationship_target_closure_is_exact():
    derived = oracle.relationship_target_ids(learned_rows())
    assert derived == oracle.EXPECTED_RELATIONSHIP_TARGET_IDS
    assert len(derived) == 17


def test_169_targets_14_via_child():
    rows = learned_rows()
    assert rows[169]["ChildSkillId"].strip() == "14"
    assert 14 in oracle.relationship_target_ids(rows)


def test_relationship_self_references_are_learned_via_evidence():
    # 372 -> 178, 375 -> 181, 1081 -> 372: these targets are learned skills, but
    # learned via their own progression/skillbook evidence, not via the relationship.
    data = json.loads(OUTPUT.read_text())
    progression, skillbooks = progression_and_skillbooks()
    learned = set(progression) | {sid for ids in skillbooks.values() for sid in ids}
    self_refs = set(data["relationshipTargetIds"]) & learned
    assert self_refs == {178, 181, 372}


def test_relationship_support_targets_are_not_promoted_to_learned():
    data = json.loads(OUTPUT.read_text())
    learned = set(data["pcLearnedSkillIds"])
    support = set(data["relationshipTargetIds"]) - learned
    # The 14 pure support targets (summons/projectiles) must stay absent from learned.
    assert support == {14, 15, 16, 17, 18, 19, 20, 21, 22, 290, 342, 387, 399, 1109}
    assert not (support & learned)
    assert len(data["pcLearnedSkillIds"]) == 24


def test_relationship_target_cannot_enter_learned_membership(tmp_path):
    # No-promotion gate: adding a support target to the membership evidence list
    # must be rejected, because learned is re-derived from progression/skillbook.
    data = json.loads(CLASSIFICATION.read_text())
    data["pc_learned_evidence_ids"] = data["pc_learned_evidence_ids"] + [14]
    changed = tmp_path / "classification.json"
    write_json(changed, data)
    with pytest.raises(SystemExit, match="does not match canonical progression/skillbook union"):
        build(classification=changed)


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
    s167 = by_id[167]
    assert "manCastSndPath" not in s167
    assert "manCastSndPath" not in s167["present"]
    # Nonempty source cells appear as both a key and a `present` entry.
    s169 = by_id[169]
    assert s169["manCastSndPath"] == "\\sound\\skill\\sound_k001.wav"
    assert s169["fmCastSndPath"] == "\\sound\\skill\\sound_k006.wav"
    assert "manCastSndPath" in s169["present"]
    assert "fmCastSndPath" in s169["present"]
    assert "lvlSetScript" in s169["present"]
    s394 = by_id[394]
    assert s394["levelUpScript"] == "\\script\\skill\\lvlup_zuixian_cuogu.lua"
    assert "levelUpScript" in s394["present"]
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


def test_membership_schema_tamper_is_rejected(tmp_path):
    data = json.loads(CLASSIFICATION.read_text())
    data["schema"] = "vltk.kunlun.membership-classification/v2"
    changed = tmp_path / "classification.json"
    write_json(changed, data)
    with pytest.raises(SystemExit, match="membership schema"):
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


def test_provenance_source_hash_drift_is_rejected(tmp_path):
    # skills.txt source SHA is validated ONLY via vltktool provenance; flipping it
    # here simulates a skills.txt change and must be caught without hashing the
    # full file directly.
    data = json.loads(PROVENANCE.read_text())
    data["source"]["sha256"] = "0" * 64
    changed = tmp_path / "provenance.json"
    write_json(changed, data)
    with pytest.raises(SystemExit, match="canonical source hash drift"):
        build(provenance=changed)


def test_generator_never_hashes_full_skills_txt():
    # The forbidden full-skills hash pattern: skills.txt must not appear among the
    # sources hashed directly by verify_canonical_sources.
    assert all("skills.txt" not in path for path in oracle.CANONICAL_LUA)
    assert oracle.SKILLS_SHA not in {
        sha for sha in oracle.CANONICAL_LUA.values()
    }
