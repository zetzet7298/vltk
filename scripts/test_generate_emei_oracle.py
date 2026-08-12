import json
import subprocess
import sys
from pathlib import Path

import pytest

sys.path.insert(0, str(Path(__file__).resolve().parent))
import generate_emei_oracle as proof

ROOT = Path(__file__).resolve().parents[1]
STORY = ROOT / "harness/docs/stories/SKL-EM-PROOF-001"
SOURCE = STORY / "PcEMeiSkills.txt"
PROVENANCE = STORY / "PcEMeiSkills.provenance.json"
TARGET_SOURCE = STORY / "PcEMeiRelationshipTargets.txt"
TARGET_PROVENANCE = STORY / "PcEMeiRelationshipTargets.provenance.json"
MEMBERSHIP = STORY / "membership-classification.json"
STATIC_PROOF = STORY / "static-catalog-proof.json"


def build():
    return proof.build(SOURCE, PROVENANCE, TARGET_SOURCE, TARGET_PROVENANCE)


def test_generated_proof_matches_pinned_evidence():
    membership, static_proof = build()
    assert membership == MEMBERSHIP.read_bytes()
    assert static_proof == STATIC_PROOF.read_bytes()
    assert proof.digest(membership) == "cafa206bbe716699e996dc15e5e892163c71e67ad3df34d08f955b9a19b89d62"
    assert proof.digest(static_proof) == "002618cbfb3c79c0e7e57bc7669de37653dc437ff912ffc5349bf4676db8873d"


def test_membership_partition_and_kunlun_90_exclusion():
    data = json.loads(MEMBERSHIP.read_text())
    learned = set(data["pc_learned_evidence_ids"])
    display = set(data["unity_display_ids"])
    assert learned == {77, 79, 80, 82, 85, 86, 88, 89, 91, 92, 93, 252, 282, 328, 332, 380, 385, 712, 1061, 1062, 1114}
    assert display - learned == {81, 83, 84, 87}
    assert len(learned) == 21 and len(display) == 15 and len(learned | display) == 25
    assert 90 not in learned
    assert data["ui_contract"]["ordered_skill_ids"] is None


def test_relationship_closure_is_support_only():
    data = json.loads(STATIC_PROOF.read_text())
    targets = set(data["learned_relationship_target_ids"])
    learned = set(data["pc_learned_skill_ids"])
    assert targets == {2, 3, 4, 5, 68, 101, 142, 186, 191, 206, 207, 208, 243, 281, 323, 324, 329, 331, 333, 375, 718, 1089, 1115}
    assert not (targets & learned)
    assert len(targets) == 23


def test_slice_tamper_is_rejected(tmp_path):
    bad = tmp_path / "skills.txt"
    bad.write_bytes(SOURCE.read_bytes() + b"tamper")
    with pytest.raises(SystemExit, match="slice provenance drift"):
        proof.build(bad, PROVENANCE, TARGET_SOURCE, TARGET_PROVENANCE)


def test_relationship_target_slice_tamper_is_rejected(tmp_path):
    bad = tmp_path / "targets.txt"
    bad.write_bytes(TARGET_SOURCE.read_bytes() + b"tamper")
    with pytest.raises(SystemExit):
        proof.build(SOURCE, PROVENANCE, bad, TARGET_PROVENANCE)


def test_generator_check_passes():
    result = subprocess.run([sys.executable, str(ROOT / "scripts/generate_emei_oracle.py"), "--check"], capture_output=True, text=True)
    assert result.returncode == 0, result.stderr or result.stdout
