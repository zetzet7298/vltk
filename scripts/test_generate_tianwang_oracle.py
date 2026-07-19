import json
import subprocess
import sys
from pathlib import Path

import pytest

sys.path.insert(0, str(Path(__file__).resolve().parent))
import generate_tianwang_oracle as proof

ROOT = Path(__file__).resolve().parents[1]
STORY = ROOT / "harness/docs/stories/SKL-TW-PROOF-001"
SOURCE = STORY / "PcTianWangSkills.txt"
PROVENANCE = STORY / "PcTianWangSkills.provenance.json"
TARGET_SOURCE = STORY / "PcTianWangRelationshipTargets.txt"
TARGET_PROVENANCE = STORY / "PcTianWangRelationshipTargets.provenance.json"
MEMBERSHIP = STORY / "membership-classification.json"
STATIC_PROOF = STORY / "static-catalog-proof.json"


def build():
    return proof.build(SOURCE, PROVENANCE, TARGET_SOURCE, TARGET_PROVENANCE)


def test_generated_proof_matches_pinned_evidence():
    membership, static_proof = build()
    assert membership == MEMBERSHIP.read_bytes()
    assert static_proof == STATIC_PROOF.read_bytes()
    assert proof.digest(membership) == "5cc932f515c69355179dfe485017288bc60d1134cba4c2cb8f74fde5c0a8cb67"
    assert proof.digest(static_proof) == "2601a8f03517ad07e930e0fd248f80b547315b4591a07a28a567407ea55a469e"


def test_membership_partition_and_no_cross_faction_promotion():
    data = json.loads(MEMBERSHIP.read_text())
    learned = set(data["pc_learned_evidence_ids"])
    display = set(data["unity_display_ids"])
    assert learned == {23, 24, 26, 29, 30, 31, 32, 33, 34, 35, 36, 37, 40, 41, 42, 322, 323, 324, 325, 708, 1058, 1059, 1060}
    assert display - learned == set()
    assert len(learned) == 23 and len(display) == 15 and len(learned | display) == 23
    assert not (display - learned)
    assert data["ui_contract"]["ordered_skill_ids"] is None


def test_relationship_closure_is_support_only():
    data = json.loads(STATIC_PROOF.read_text())
    targets = set(data["learned_relationship_target_ids"])
    learned = set(data["pc_learned_skill_ids"])
    assert targets == {219, 220, 221, 222, 224, 225, 326, 327, 404, 405, 406, 407, 408, 1084, 1087, 1088}
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
    result = subprocess.run([sys.executable, str(ROOT / "scripts/generate_tianwang_oracle.py"), "--check"], capture_output=True, text=True)
    assert result.returncode == 0, result.stderr or result.stdout
