import json
import subprocess
import sys
from pathlib import Path

import pytest

sys.path.insert(0, str(Path(__file__).resolve().parent))
import generate_cuiyan_oracle as proof

ROOT = Path(__file__).resolve().parents[1]
STORY = ROOT / "harness/docs/stories/SKL-CY-PROOF-001"
SOURCE = STORY / "PcCuiYanSkills.txt"
PROVENANCE = STORY / "PcCuiYanSkills.provenance.json"
TARGET_SOURCE = STORY / "PcCuiYanRelationshipTargets.txt"
TARGET_PROVENANCE = STORY / "PcCuiYanRelationshipTargets.provenance.json"
MEMBERSHIP = STORY / "membership-classification.json"
STATIC_PROOF = STORY / "static-catalog-proof.json"


def build():
    return proof.build(SOURCE, PROVENANCE, TARGET_SOURCE, TARGET_PROVENANCE)


def test_generated_proof_matches_pinned_evidence():
    membership, static_proof = build()
    assert membership == MEMBERSHIP.read_bytes()
    assert static_proof == STATIC_PROOF.read_bytes()
    assert proof.digest(membership) == "b045ad3292ca61820d947ff2c3d37e8876ad9b0027f437234eacfda5beb1eac1"
    assert proof.digest(static_proof) == "5e2123bf27ff82b6889260d9c14d4b598a81f41680c9f61667d0ed004cbf108a"


def test_membership_partition_and_no_cross_faction_promotion():
    data = json.loads(MEMBERSHIP.read_text())
    learned = set(data["pc_learned_evidence_ids"])
    display = set(data["unity_display_ids"])
    assert learned == {95, 97, 99, 100, 102, 105, 108, 109, 111, 113, 114, 269, 336, 337, 713, 1063, 1065}
    assert display - learned == {101, 103}
    assert len(learned) == 17 and len(display) == 13 and len(learned | display) == 19
    assert display - learned == {101, 103}
    assert data["ui_contract"]["ordered_skill_ids"] is None


def test_relationship_closure_is_support_only():
    data = json.loads(STATIC_PROOF.read_text())
    targets = set(data["learned_relationship_target_ids"])
    learned = set(data["pc_learned_skill_ids"])
    assert targets == {6, 7, 8, 9, 10, 12, 111, 112, 146, 147, 326, 327, 338, 398, 1064, 1093, 1102}
    assert targets & learned == {111}
    assert len(targets) == 17


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
    result = subprocess.run([sys.executable, str(ROOT / "scripts/generate_cuiyan_oracle.py"), "--check"], capture_output=True, text=True)
    assert result.returncode == 0, result.stderr or result.stdout
