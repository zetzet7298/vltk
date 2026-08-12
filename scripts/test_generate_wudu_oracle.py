import json
import subprocess
import sys
from pathlib import Path

import pytest

sys.path.insert(0, str(Path(__file__).resolve().parent))
import generate_wudu_oracle as proof

ROOT = Path(__file__).resolve().parents[1]
STORY = ROOT / "harness/docs/stories/SKL-WDU-PROOF-001"
SOURCE = STORY / "PcWuDuSkills.txt"
PROVENANCE = STORY / "PcWuDuSkills.provenance.json"
TARGET_SOURCE = STORY / "PcWuDuRelationshipTargets.txt"
TARGET_PROVENANCE = STORY / "PcWuDuRelationshipTargets.provenance.json"
MEMBERSHIP = STORY / "membership-classification.json"
STATIC_PROOF = STORY / "static-catalog-proof.json"


def build():
    return proof.build(SOURCE, PROVENANCE, TARGET_SOURCE, TARGET_PROVENANCE)


def test_generated_proof_matches_pinned_evidence():
    membership, static_proof = build()
    assert membership == MEMBERSHIP.read_bytes()
    assert static_proof == STATIC_PROOF.read_bytes()
    assert proof.digest(membership) == "4cc693683e6112a6f299790d801fc1e8f856bf5b4a27597dc6665a6d4828194a"
    assert proof.digest(static_proof) == "bdf78af995faff3448217fef3663a159b7f0581c7ad5b047bd2b456c5d03b59e"


def test_membership_partition_and_no_cross_faction_promotion():
    data = json.loads(MEMBERSHIP.read_text())
    learned = set(data["pc_learned_evidence_ids"])
    display = set(data["unity_display_ids"])
    assert learned == {60, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 353, 355, 356, 384, 390, 711, 1066, 1067}
    assert display - learned == {76}
    assert len(learned) == 23 and len(display) == 16 and len(learned | display) == 24
    assert 76 not in learned
    assert data["ui_contract"]["ordered_skill_ids"] is None


def test_relationship_closure_is_support_only():
    data = json.loads(STATIC_PROOF.read_text())
    targets = set(data["learned_relationship_target_ids"])
    learned = set(data["pc_learned_skill_ids"])
    assert targets == {20, 30, 31, 32, 33, 34, 163, 165, 190, 203, 328, 329, 354, 383, 1094, 1095}
    assert not (targets & learned)
    assert len(targets) == 16


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
    result = subprocess.run([sys.executable, str(ROOT / "scripts/generate_wudu_oracle.py"), "--check"], capture_output=True, text=True)
    assert result.returncode == 0, result.stderr or result.stdout
