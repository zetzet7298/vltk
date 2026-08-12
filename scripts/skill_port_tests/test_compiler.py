from __future__ import annotations

import copy
import json
import subprocess
import tempfile
import unittest
from pathlib import Path

from jsonschema import Draft202012Validator

from scripts.skill_port import compiler
from scripts.skill_port.gen.content.v1 import skill_catalog_pb2 as skillpb


REPO = Path(__file__).resolve().parents[2]
COVERAGE = REPO / "harness/docs/stories/SKL-ALL-PARITY-001/coverage-matrix.json"
PRESENTATION = REPO / "Assets/StreamingAssets/Reference/PcAllFactionPresentationInventory.json"
SKILL_SLICE = REPO / "Assets/StreamingAssets/Reference/PcAllFactionLearnedDisplaySkills.txt"
PROVENANCE = REPO / "Assets/StreamingAssets/Reference/PcAllFactionLearnedDisplaySkills.provenance.json"
MANIFEST_SCHEMA = REPO / "harness/specs/jx-pc-mobile-port/contracts/content/manifest.v1.schema.json"
CONTENT_PROTO = REPO / "harness/specs/jx-pc-mobile-port/contracts/content/v1/skill_catalog.proto"


class SkillPortCompilerTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.artifacts = compiler.build_artifacts(REPO, COVERAGE, PRESENTATION, SKILL_SLICE, PROVENANCE)
        cls.ir = json.loads(cls.artifacts["skill_port.ir.json"].decode("utf-8"))
        cls.index = json.loads(cls.artifacts["skill_port.index.json"].decode("utf-8"))

    def test_current_catalog_counts(self) -> None:
        counts = self.ir["summary_counts"]
        self.assertEqual(counts["rows"], 242)
        self.assertEqual(counts["static_fields_verified_from_slice"], 9196)
        self.assertEqual(counts["missile_rows_discoverable"], 513)
        self.assertEqual(counts["state_rows_discoverable"], 49)
        self.assertEqual(counts["relationship_classes"]["child;missile"], 138)
        self.assertEqual(counts["relationship_classes"]["child;canonical_skill"], 34)
        self.assertEqual(counts["relationship_classes"]["child;none"], 70)
        self.assertEqual(self.index["counts"]["exposed"], 140)
        self.assertEqual(self.index["counts"]["evidence_pending"], 37)
        self.assertEqual(self.index["counts"]["pc_only"], 65)
        self.assertEqual(self.index["counts"]["golden_ready"], 0)

    def test_deterministic_double_run_hashes(self) -> None:
        again = compiler.build_artifacts(REPO, COVERAGE, PRESENTATION, SKILL_SLICE, PROVENANCE)
        self.assertEqual(self.artifacts, again)
        self.assertEqual(
            compiler.sha256_bytes(self.artifacts["skill_port.ir.json"]),
            compiler.sha256_bytes(again["skill_port.ir.json"]),
        )

    def test_content_proto_compiles_with_pinned_protoc(self) -> None:
        self.assertEqual(subprocess.check_output(["protoc", "--version"], text=True).strip(), "libprotoc 25.1")
        subprocess.run(
            ["protoc", f"--proto_path={CONTENT_PROTO.parents[2]}", "--descriptor_set_out=/dev/null", str(CONTENT_PROTO.relative_to(CONTENT_PROTO.parents[2]))],
            check=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
        )

    def test_protobuf_projection_shapes(self) -> None:
        catalog = skillpb.SkillCatalog.FromString(self.artifacts["skill_port.catalog.pb"])
        server = skillpb.ServerSkillCatalog.FromString(self.artifacts["skill_port.server.pb"])
        client = skillpb.ClientSkillCatalog.FromString(self.artifacts["skill_port.client.pb"])
        self.assertEqual(len(catalog.rows), 242)
        self.assertEqual(len(server.rows), 242)
        self.assertEqual(len(client.rows), 242)
        self.assertEqual(catalog.header.golden_ready_count, 0)
        self.assertFalse(catalog.runtime_skill_policy.filesystem_fallback_allowed)
        self.assertFalse(catalog.runtime_skill_policy.runtime_parity_claimed)
        self.assertEqual(catalog.runtime_skill_policy.pc_runtime_evidence_status, "BLOCKED")
        self.assertRegex(client.header.projection_sha256, r"^[0-9a-f]{64}$")

    def test_manifest_v1_schema_and_dev_signature_gate(self) -> None:
        manifest = json.loads(self.artifacts["manifest.json"].decode("utf-8"))
        Draft202012Validator(json.loads(MANIFEST_SCHEMA.read_text())).validate(manifest)
        self.assertEqual(manifest["schemaVersion"], 1)
        self.assertEqual(manifest["contentDigest"]["catalogUnionSize"], 242)
        self.assertEqual(manifest["contentDigest"]["clientProjectionSha256"], compiler.sha256_bytes(self.artifacts["skill_port.client.pb"]))
        self.assertEqual(manifest["signingKeyId"], compiler.TEST_ONLY_SIGNING_KEY_ID)
        compiler.verify_test_only_manifest_signature(manifest)
        with self.assertRaisesRegex(ValueError, "forbidden development signing key"):
            compiler.validate_production_manifest(manifest)
        artifact_paths = {a["logicalPath"] for a in manifest["artifacts"]}
        self.assertEqual(artifact_paths, set(self.artifacts) - {"manifest.json"})

    def test_write_then_check_generated_set(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            out = Path(tmp) / "SkillPort"
            compiler.compile_catalog(
                REPO, out, coverage=COVERAGE, presentation=PRESENTATION,
                skill_slice=SKILL_SLICE, provenance=PROVENANCE, write=True, check=False,
            )
            manifest = compiler.compile_catalog(
                REPO, out, coverage=COVERAGE, presentation=PRESENTATION,
                skill_slice=SKILL_SLICE, provenance=PROVENANCE, write=False, check=True,
            )
            self.assertTrue(manifest["double_run_hash_equal"])
            self.assertTrue((out / "manifest.json").is_file())

    def test_negative_duplicate_ids_fail_closed(self) -> None:
        ir = copy.deepcopy(self.ir)
        ir["rows"][1] = copy.deepcopy(ir["rows"][0])
        with self.assertRaisesRegex(ValueError, "242 unique skill ids"):
            compiler.validate_ir(ir)

    def test_negative_unbounded_relation_cycle_fail_closed(self) -> None:
        ir = copy.deepcopy(self.ir)
        a = ir["rows"][0]["skill_id"]
        b = ir["rows"][1]["skill_id"]
        ir["rows"][0]["relations"][0].update({"target_id": b, "target_kind": "skill", "proof_state": "verified"})
        ir["rows"][1]["relations"][0].update({"target_id": a, "target_kind": "skill", "proof_state": "verified"})
        with self.assertRaisesRegex(ValueError, "unbounded relation cycle"):
            compiler.validate_ir(ir)

    def test_negative_unsupported_typed_node_fail_closed(self) -> None:
        ir = copy.deepcopy(self.ir)
        ir["rows"][0]["typed_nodes"][0]["kind"] = "invented_gameplay_semantics"
        with self.assertRaisesRegex(ValueError, "unsupported typed node kind"):
            compiler.validate_ir(ir)

    def test_negative_hash_drift_fail_closed(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            bad_prov = Path(tmp) / "prov.json"
            data = json.loads(PROVENANCE.read_text(encoding="utf-8"))
            data["slice"]["sha256"] = "0" * 64
            bad_prov.write_text(json.dumps(data), encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "slice hash drift"):
                compiler.build_artifacts(REPO, COVERAGE, PRESENTATION, SKILL_SLICE, bad_prov)

    def test_negative_missing_mandatory_source_fail_closed(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            with self.assertRaisesRegex(ValueError, "missing mandatory source"):
                compiler.build_artifacts(REPO, COVERAGE, Path(tmp) / "missing.json", SKILL_SLICE, PROVENANCE)


if __name__ == "__main__":
    unittest.main()
