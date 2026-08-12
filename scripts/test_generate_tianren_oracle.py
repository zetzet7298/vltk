import json
import subprocess
import sys
from pathlib import Path

import pytest

sys.path.insert(0, str(Path(__file__).resolve().parent))
import generate_tianren_oracle as proof

ROOT = Path(__file__).resolve().parents[1]
STORY = ROOT / "harness/docs/stories/SKL-TR-PROOF-001"
SOURCE = STORY / "PcTianRenSkills.txt"
PROVENANCE = STORY / "PcTianRenSkills.provenance.json"
TARGET_SOURCE = STORY / "PcTianRenRelationshipTargets.txt"
TARGET_PROVENANCE = STORY / "PcTianRenRelationshipTargets.provenance.json"
MEMBERSHIP = STORY / "membership-classification.json"
STATIC_PROOF = STORY / "static-catalog-proof.json"


def build():
    return proof.build(SOURCE, PROVENANCE, TARGET_SOURCE, TARGET_PROVENANCE)


def test_generated_proof_matches_pinned_evidence():
    membership, static_proof = build()
    assert membership == MEMBERSHIP.read_bytes()
    assert static_proof == STATIC_PROOF.read_bytes()
    assert proof.digest(membership) == "466c38f90cf841ae279c2580f0b1b513e9573cf382da1b1eb70b27814c985685"
    assert proof.digest(static_proof) == "d75fa4da27e66c51920db80273999d222547287ccf87c4bdf131b5136a2bef45"


def test_membership_partition_and_no_cross_faction_promotion():
    data = json.loads(MEMBERSHIP.read_text())
    learned = set(data["pc_learned_evidence_ids"])
    display = set(data["unity_display_ids"])
    assert learned == {131, 132, 135, 136, 137, 138, 140, 141, 142, 143, 145, 148, 150, 361, 362, 364, 391, 715, 1075, 1076}
    assert display - learned == {139, 144, 146, 147, 149}
    assert len(learned) == 20 and len(display) == 18 and len(learned | display) == 25
    assert 90 not in learned
    assert data["ui_contract"]["ordered_skill_ids"] is None


def test_relationship_closure_is_support_only():
    data = json.loads(STATIC_PROOF.read_text())
    targets = set(data["learned_relationship_target_ids"])
    learned = set(data["pc_learned_skill_ids"])
    assert targets == {20, 54, 55, 56, 57, 58, 69, 169, 171, 192, 337, 363, 366, 723, 1131}
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
    result = subprocess.run([sys.executable, str(ROOT / "scripts/generate_tianren_oracle.py"), "--check"], capture_output=True, text=True)
    assert result.returncode == 0, result.stderr or result.stdout
