import json
import subprocess
import sys
from pathlib import Path

import pytest

sys.path.insert(0, str(Path(__file__).resolve().parent))
import generate_wudang_oracle as proof

ROOT = Path(__file__).resolve().parents[1]
STORY = ROOT / "harness/docs/stories/SKL-WD-PROOF-001"
SOURCE = STORY / "PcWuDangSkills.txt"
PROVENANCE = STORY / "PcWuDangSkills.provenance.json"
TARGET_SOURCE = STORY / "PcWuDangRelationshipTargets.txt"
TARGET_PROVENANCE = STORY / "PcWuDangRelationshipTargets.provenance.json"
MEMBERSHIP = STORY / "membership-classification.json"
STATIC_PROOF = STORY / "static-catalog-proof.json"


def build():
    return proof.build(SOURCE, PROVENANCE, TARGET_SOURCE, TARGET_PROVENANCE)


def test_generated_proof_matches_pinned_evidence():
    membership, static_proof = build()
    assert membership == MEMBERSHIP.read_bytes()
    assert static_proof == STATIC_PROOF.read_bytes()
    assert proof.digest(membership) == "35104b51d81ceb96934798ed6c79b19af42c1837ba786ab92fa8110712e45472"
    assert proof.digest(static_proof) == "1cbab681c8b4bc6ab808bace54e20620c8a5b4028c5f618de2838a4a3b3fd351"


def test_membership_partition_and_no_cross_faction_promotion():
    data = json.loads(MEMBERSHIP.read_text())
    learned = set(data["pc_learned_evidence_ids"])
    display = set(data["unity_display_ids"])
    assert learned == {151, 152, 153, 155, 157, 158, 159, 160, 164, 165, 166, 267, 365, 368, 716, 1078, 1079}
    assert display - learned == {154, 156, 161, 162, 163}
    assert len(learned) == 17 and len(display) == 16 and len(learned | display) == 22
    assert 154 not in learned
    assert data["ui_contract"]["ordered_skill_ids"] is None


def test_relationship_closure_is_support_only():
    data = json.loads(STATIC_PROOF.read_text())
    targets = set(data["learned_relationship_target_ids"])
    learned = set(data["pc_learned_skill_ids"])
    assert targets == {24, 25, 26, 28, 29, 110, 173, 175, 211, 274, 340, 341, 371, 738, 1107}
    assert not (targets & learned)
    assert len(targets) == 15


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
    result = subprocess.run([sys.executable, str(ROOT / "scripts/generate_wudang_oracle.py"), "--check"], capture_output=True, text=True)
    assert result.returncode == 0, result.stderr or result.stdout
