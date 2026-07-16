from functools import partial
import hashlib
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
import json
import os
import shutil
import tempfile
import threading
import unittest
from pathlib import Path
from unittest.mock import patch

import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from validate import Validator


class ValidatorTests(unittest.TestCase):
    def test_markdown_stable_id_reference_must_be_defined(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "domain.md").write_text(
                "Hợp lệ `FR-CBT-001`, không hợp lệ `GAP-AUTO-001`.\n",
                encoding="utf-8",
            )
            validator = Validator(root, "authoring")
            validator.defined_ids["FR-CBT-001"] = root / "requirements.yaml"

            validator.validate_markdown_id_references()

            self.assertEqual([finding.code for finding in validator.findings], ["MARKDOWN_ID_REF"])
            self.assertEqual(validator.findings[0].entity_id, "GAP-AUTO-001")

    def test_data_dictionary_must_cover_every_sql_column(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            sql_path = root / "contracts/sql/game.v1.sql"
            dictionary_path = root / "domains/server-runtime/postgresql-data-dictionary.md"
            sql_path.parent.mkdir(parents=True)
            dictionary_path.parent.mkdir(parents=True)
            sql_path.write_text(
                "BEGIN;\nCREATE TABLE fixture (id uuid PRIMARY KEY, name text NOT NULL);\nCOMMIT;\n",
                encoding="utf-8",
            )
            dictionary_path.write_text(
                "<!-- DATA_DICTIONARY_COVERAGE: tables=1 columns=2 -->\n"
                "### 1. `fixture`\n"
                "| STT | Field | Type | Required | Width | Rules | Meaning |\n"
                "| --- | --- | --- | --- | --- | --- | --- |\n"
                "| 1 | `id` | `uuid` | Có | Không | PK | ID |\n",
                encoding="utf-8",
            )
            validator = Validator(root, "authoring")

            validator.validate_data_dictionary()

            self.assertIn(
                "DATA_DICTIONARY_COLUMN_COVERAGE",
                {finding.code for finding in validator.findings},
            )

    def write_valid_ui_cnpm(self, root: Path):
        schema = root / "schemas"
        schema.mkdir(parents=True)
        (schema / "cnpm-contract.yaml").write_text(
            "schema_version: 1\ntemplates:\n  04-giao-dien.md: {}\n", encoding="utf-8"
        )
        (root / "04-giao-dien.md").write_text(
            """# Bảng tiêu chuẩn thiết kế giao diện
## Tiêu chuẩn đối với các màn hình
| **Yếu tố** | **Kích thước** | **Canh lề** | **Cách tổ chức** | **Phím nóng / phím tắt** | **Yêu cầu kết xuất** |
| --- | --- | --- | --- | --- | --- |
| Màn hình chính | 1 | 2 | 3 | Không áp dụng: mobile | 4 |
## Tiêu chuẩn đối với các yếu tố trên màn hình
| **Yếu tố** | **Font type** | **Font size** | **Font Color** | **Canh lề** | **Kích thước** | **Hình dạng** |
| --- | --- | --- | --- | --- | --- | --- |
| Label | Font | 16 | Black | Left | 48 | Text |
# Sơ đồ giao diện tổng quát
Không áp dụng: fixture unit test.
# Giao diện chi tiết
## Màn hình fixture
| STT | Thao tác | Ý nghĩa | Xử lý liên quan | Ghi chú |
| --- | --- | --- | --- | --- |
| 1 | Tap | Test | Validate | Fixture |
""",
            encoding="utf-8",
        )

    def write_source_census_fixture(
        self,
        root: Path,
        *,
        omitted_catalog_path: str | None = None,
        drift_entity: str | None = None,
    ):
        source_root = root / "source"
        settings = source_root / "bin/client/settings"
        ui = source_root / "bin/client/Ui"
        spr = source_root / "bin/client/Spr"
        music = source_root / "bin/client/music"
        script = source_root / "bin/client/script"
        for directory in (settings, ui, spr, music, script):
            directory.mkdir(parents=True, exist_ok=True)

        for name in ("Skills.txt", "Missles.txt", "Npcs.txt", "Goods.txt"):
            (settings / name).write_text("id\tname\n1\tfixture\n", encoding="utf-8")
        (settings / "MapList.ini").write_text("1=FixtureMap\n", encoding="ascii")
        (source_root / "bin/client/package.ini").write_text("1=fixture.pak\n", encoding="ascii")
        (ui / "panel.ini").write_text("[Panel]\n", encoding="ascii")
        (ui / "quest.lua").write_text("return {}\n", encoding="ascii")
        (script / "logic.lua").write_text("return true\n", encoding="ascii")
        (ui / "icon.spr").write_bytes(b"ui-sprite")
        (spr / "effect.spr").write_bytes(b"effect-sprite")
        (music / "tone.wav").write_bytes(b"audio")

        def relative(path: Path) -> str:
            return path.relative_to(source_root).as_posix()

        def pathset(paths: list[Path]) -> tuple[int, str, list[str]]:
            values = sorted({relative(path) for path in paths}, key=os.fsencode)
            payload = b"".join(os.fsencode(value) + b"\n" for value in values)
            return len(values), hashlib.sha256(payload).hexdigest(), values

        table_names = {
            "skill": "Skills.txt",
            "missile": "Missles.txt",
            "npc": "Npcs.txt",
            "goods": "Goods.txt",
        }
        census = {
            entity: {
                "records": 1,
                "path": relative(settings / name),
                "source_sha256": hashlib.sha256((settings / name).read_bytes()).hexdigest(),
            }
            for entity, name in table_names.items()
        }
        census["map"] = {
            "records": 1,
            "path": relative(settings / "MapList.ini"),
            "source_sha256": hashlib.sha256((settings / "MapList.ini").read_bytes()).hexdigest(),
        }
        package = source_root / "bin/client/package.ini"
        census["package"] = {
            "records": 1,
            "path": relative(package),
            "source_sha256": hashlib.sha256(package.read_bytes()).hexdigest(),
        }

        path_inputs = {
            "setting": sorted(settings.iterdir()),
            "uifile": [ui / "panel.ini"],
            "lua": [ui / "quest.lua", script / "logic.lua"],
            "quest": [ui / "quest.lua", script / "logic.lua"],
            "sprite": [ui / "icon.spr", spr / "effect.spr"],
            "avatar": [],
            "audio": [music / "tone.wav"],
        }
        path_values = {}
        for entity, paths in path_inputs.items():
            count, digest, values = pathset(paths)
            census[entity] = {"files": count, "paths_sha256": digest}
            path_values[entity] = values

        expected_counts = {
            **{entity: 1 for entity in (*table_names, "map", "package")},
            **{entity: len(values) for entity, values in path_values.items()},
        }
        if drift_entity is not None:
            census[drift_entity]["records"] += 1

        records = []
        for entity in ("setting", "uifile", "lua", "sprite", "audio"):
            for source_path in path_values[entity]:
                if source_path != omitted_catalog_path:
                    records.append({"entity_type": entity, "source_path": source_path})
        catalog_dir = root / "registry/catalogs"
        catalog_dir.mkdir(parents=True)
        catalog_path = catalog_dir / "paths.jsonl"
        catalog_path.write_text(
            "".join(json.dumps(record) + "\n" for record in records), encoding="utf-8"
        )
        (catalog_dir / "index.yaml").write_text(
            json.dumps(
                {
                    "source_census": census,
                    "coverage": {
                        entity: {"discovered": count, "cataloged": count}
                        for entity, count in expected_counts.items()
                    },
                    "catalogs": [{"path": "paths.jsonl"}],
                }
            ),
            encoding="utf-8",
        )
        (catalog_dir / "source-snapshot.yaml").write_text(
            json.dumps({"source_root": str(source_root)}), encoding="utf-8"
        )

    def test_missing_layout_fails(self):
        with tempfile.TemporaryDirectory() as tmp:
            findings = Validator(Path(tmp), "authoring").run()
            self.assertTrue(any(item.code == "FILE_MISSING" for item in findings))

    def test_duplicate_yaml_key_fails(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            path = root / "bad.yaml"
            path.write_text("a: 1\na: 2\n", encoding="utf-8")
            validator = Validator(root, "authoring")
            validator.validate_yaml_and_ids()
            self.assertTrue(
                any(item.code == "YAML_INVALID" for item in validator.findings)
            )

    def test_false_parity_done_fails(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            path = root / "registry.yaml"
            path.write_text(
                "items:\n  - id: PAR-0001\n    lifecycle: PARITY_DONE\n",
                encoding="utf-8",
            )
            validator = Validator(root, "authoring")
            validator.validate_yaml_and_ids()
            self.assertTrue(
                any(item.code == "FALSE_PARITY_DONE" for item in validator.findings)
            )

    def test_parity_done_requires_golden_signoff_and_test_result(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            record_path = root / "registry/parity-items.yaml"
            record_path.parent.mkdir(parents=True)
            result_path = root / "registry/test-results/index.yaml"
            result_path.parent.mkdir(parents=True)
            result_path.write_text("results: []\n", encoding="utf-8")
            validator = Validator(root, "premerge")
            validator.defined_ids = {
                "PAR-0001": record_path,
                "GOLD-0001": record_path,
                "TEST-UNIT-001": record_path,
            }
            validator.entities = {
                "PAR-0001": {
                    "lifecycle": "PARITY_DONE",
                    "reviewer": "qa-lead",
                    "tests": ["TEST-UNIT-001"],
                    "golden_ids": ["GOLD-0001"],
                    "reviewer_signoff": {
                        "reviewer": "qa-lead",
                        "signed_at": "2026-01-01T00:00:00Z",
                        "revision": "rev-1",
                        "evidence_path": "missing-signoff.json",
                        "sha256": "a" * 64,
                    },
                },
                "GOLD-0001": {"status": "REQUIRED"},
                "TEST-UNIT-001": {"status": "PASS"},
            }

            validator.validate_parity_done_evidence()

            codes = {item.code for item in validator.findings}
            self.assertIn("PARITY_DONE_SIGNOFF_ARTIFACT", codes)
            self.assertIn("PARITY_DONE_GOLDEN", codes)
            self.assertIn("PARITY_DONE_TEST", codes)

    def test_parity_done_missing_evidence_lists_fails_closed(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            record_path = root / "registry/parity-items.yaml"
            validator = Validator(root, "authoring")
            validator.defined_ids = {"PAR-0001": record_path}
            validator.entities = {"PAR-0001": {"lifecycle": "PARITY_DONE"}}

            validator.validate_parity_done_evidence()

            self.assertTrue(
                any(item.code == "PARITY_DONE_EVIDENCE" for item in validator.findings)
            )

    def test_parity_done_golden_revision_must_match_signoff(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            record_path = root / "registry/parity-items.yaml"
            record_path.parent.mkdir(parents=True)
            signoff_path = root / "signoff.json"
            signoff_path.write_text('{"approved":true}', encoding="utf-8")
            result_path = root / "registry/test-results/index.yaml"
            result_path.parent.mkdir(parents=True)
            result_path.write_text(
                "results:\n"
                "  - test_id: TEST-UNIT-001\n"
                "    status: PASS\n"
                "    revision: rev-1\n"
                "    golden_ids: [GOLD-0001]\n",
                encoding="utf-8",
            )
            validator = Validator(root, "premerge")
            validator.defined_ids = {
                "PAR-0001": record_path,
                "GOLD-0001": record_path,
                "TEST-UNIT-001": record_path,
            }
            validator.entities = {
                "PAR-0001": {
                    "lifecycle": "PARITY_DONE",
                    "reviewer": "qa-lead",
                    "tests": ["TEST-UNIT-001"],
                    "golden_ids": ["GOLD-0001"],
                    "reviewer_signoff": {
                        "reviewer": "qa-lead",
                        "signed_at": "2026-01-01T00:00:00Z",
                        "revision": "rev-1",
                        "evidence_path": str(signoff_path),
                        "sha256": hashlib.sha256(signoff_path.read_bytes()).hexdigest(),
                    },
                },
                "GOLD-0001": {
                    "status": "APPROVED",
                    "artifact": {"source_revision": "rev-2"},
                },
                "TEST-UNIT-001": {"status": "PASS"},
            }

            validator.validate_parity_done_evidence()

            self.assertTrue(
                any(
                    item.code == "PARITY_DONE_GOLDEN_REVISION"
                    for item in validator.findings
                )
            )

    def test_domain_and_gate_ids_are_valid(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / "records.yaml").write_text(
                "items:\n  - {id: DOM-COMBAT-001}\n  - {id: GATE-G3-001}\n",
                encoding="utf-8",
            )
            validator = Validator(root, "authoring")
            validator.validate_yaml_and_ids()
            self.assertFalse(
                any(item.code == "ID_FORMAT" for item in validator.findings)
            )

    def test_premerge_trace_requires_design_test_and_gate(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            path = root / "registry/traceability.csv"
            path.parent.mkdir(parents=True)
            path.write_text(
                "source_id,relation,target_id\nFR-001,verified_by,TEST-UNIT-001\n",
                encoding="utf-8",
            )
            validator = Validator(root, "premerge")
            validator.defined_ids = {
                "FR-001": root / "requirements.yaml",
                "TEST-UNIT-001": root / "tests.yaml",
                "DOM-CORE-001": root / "designs.yaml",
                "GATE-G1-001": root / "gates.yaml",
            }
            validator.validate_traceability()
            codes = {item.code for item in validator.findings}
            self.assertIn("TRACE_REQUIREMENT_DESIGN", codes)
            self.assertIn("TRACE_TEST_GATE", codes)

    def test_premerge_trace_complete_chain_passes(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            path = root / "registry/traceability.csv"
            path.parent.mkdir(parents=True)
            path.write_text(
                "source_id,relation,target_id\n"
                "OBJ-001,has_requirement,FR-001\n"
                "FR-001,designed_by,DOM-CORE-001\n"
                "FR-001,verified_by,TEST-UNIT-001\n"
                "TEST-UNIT-001,gated_by,GATE-G1-001\n",
                encoding="utf-8",
            )
            validator = Validator(root, "premerge")
            validator.defined_ids = {
                "OBJ-001": root / "objectives.yaml",
                "FR-001": root / "requirements.yaml",
                "TEST-UNIT-001": root / "tests.yaml",
                "DOM-CORE-001": root / "designs.yaml",
                "GATE-G1-001": root / "gates.yaml",
            }
            validator.validate_traceability()
            self.assertEqual([], validator.findings)

    def test_cnpm_exact_heading_and_table_signatures_pass(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            self.write_valid_ui_cnpm(root)
            validator = Validator(root, "authoring")
            validator.validate_cnpm()
            self.assertEqual([], validator.findings)

    def test_cnpm_wrong_heading_level_fails(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            self.write_valid_ui_cnpm(root)
            path = root / "04-giao-dien.md"
            path.write_text(
                path.read_text(encoding="utf-8").replace(
                    "## Tiêu chuẩn đối với các màn hình",
                    "### Tiêu chuẩn đối với các màn hình",
                ),
                encoding="utf-8",
            )
            validator = Validator(root, "authoring")
            validator.validate_cnpm()
            self.assertTrue(
                any(
                    item.code == "CNPM_HEADING_SIGNATURE" for item in validator.findings
                )
            )

    def test_cnpm_wrong_table_columns_fail(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            self.write_valid_ui_cnpm(root)
            path = root / "04-giao-dien.md"
            path.write_text(
                path.read_text(encoding="utf-8").replace(
                    "| STT | Thao tác | Ý nghĩa | Xử lý liên quan | Ghi chú |",
                    "| STT | Thao tác | Ý nghĩa | Xử lý liên quan | Notes |",
                ),
                encoding="utf-8",
            )
            validator = Validator(root, "authoring")
            validator.validate_cnpm()
            self.assertTrue(
                any(item.code == "CNPM_TABLE_SIGNATURE" for item in validator.findings)
            )

    def test_openapi_dangling_local_ref_fails(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            path = root / "contracts/openapi/game.v1.yaml"
            path.parent.mkdir(parents=True)
            path.write_text(
                """openapi: 3.1.0
info: {title: Test, version: 1.0.0}
paths:
  /test:
    get:
      operationId: test
      responses:
        '200': {$ref: '#/components/responses/Missing'}
components: {responses: {}}
""",
                encoding="utf-8",
            )
            validator = Validator(root, "premerge")
            validator.validate_openapi()
            self.assertTrue(
                any(item.code == "OPENAPI_REF" for item in validator.findings)
            )

    @unittest.skipUnless(shutil.which("protoc"), "protoc is not installed")
    def test_invalid_proto_fails_compile(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            path = root / "contracts/proto/game/v1/game.proto"
            path.parent.mkdir(parents=True)
            path.write_text(
                'syntax = "proto3"; message Broken { string value = ; }\n',
                encoding="utf-8",
            )
            validator = Validator(root, "premerge")
            validator.validate_proto()
            self.assertTrue(
                any(item.code == "PROTO_COMPILE" for item in validator.findings)
            )

    def test_invalid_sql_contract_fails(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            path = root / "contracts/sql/game.v1.sql"
            path.parent.mkdir(parents=True)
            path.write_text(
                "BEGIN; CREATE TABLE broken (id integer; COMMIT;\n", encoding="utf-8"
            )
            validator = Validator(root, "premerge")
            validator.validate_sql_contract()
            codes = {item.code for item in validator.findings}
            self.assertIn("SQL_SYNTAX_STATIC", codes)

    def test_invalid_content_json_schema_fails(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            path = root / "contracts/content/manifest.v1.schema.json"
            path.parent.mkdir(parents=True)
            path.write_text(
                '{"$schema":"https://json-schema.org/draft/2020-12/schema","type":"wrong"}',
                encoding="utf-8",
            )
            validator = Validator(root, "premerge")
            validator.validate_content_schema()
            self.assertTrue(
                any(
                    item.code == "CONTENT_SCHEMA_INVALID" for item in validator.findings
                )
            )

    def test_skill_matrix_instance_must_match_matrix_schema(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            base = root / "delivery/case-matrices"
            base.mkdir(parents=True)
            (base / "skill-parity-p0.json").write_text("{}", encoding="utf-8")
            (base / "skill-parity-p0.schema.json").write_text(
                '{"$schema":"https://json-schema.org/draft/2020-12/schema",'
                '"type":"object","required":["matrix_id"]}',
                encoding="utf-8",
            )
            validator = Validator(root, "premerge")
            validator.validate_skill_case_matrix()
            self.assertTrue(
                any(item.code == "SKILL_MATRIX_INVALID" for item in validator.findings)
            )

    def test_release_policy_literal_is_not_unconfirmed(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            path = root / "policy.md"
            path.write_text(
                "# P0 policy\nClaim thiếu nguồn phải ghi `[CẦN XÁC NHẬN]` và BLOCKED.\n",
                encoding="utf-8",
            )
            validator = Validator(root, "release")
            validator.validate_release()
            self.assertFalse(
                any(
                    item.code.startswith("RELEASE_UNCONFIRMED")
                    for item in validator.findings
                )
            )

    def test_release_real_p1_placeholder_fails(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            path = root / "domain.md"
            path.write_text(
                "# Domain\n- P1 exact threshold `[CẦN XÁC NHẬN]`.\n", encoding="utf-8"
            )
            validator = Validator(root, "release")
            validator.validate_release()
            self.assertTrue(
                any(item.code == "RELEASE_UNCONFIRMED" for item in validator.findings)
            )

    def test_release_fenced_policy_example_is_ignored(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            path = root / "policy.md"
            path.write_text(
                "# P0 policy\n```text\n[CẦN XÁC NHẬN]\n```\n", encoding="utf-8"
            )
            validator = Validator(root, "release")
            validator.validate_release()
            self.assertFalse(
                any(
                    item.code.startswith("RELEASE_UNCONFIRMED")
                    for item in validator.findings
                )
            )

    def test_governed_stable_id_requires_complete_metadata_and_enums(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / "record.yaml").write_text(
                "items:\n  - {id: FR-001, lifecycle: MAGIC, status: UNKNOWN}\n",
                encoding="utf-8",
            )
            validator = Validator(root, "authoring")
            validator.validate_yaml_and_ids()
            codes = {item.code for item in validator.findings}
            self.assertIn("METADATA_MISSING", codes)
            self.assertIn("LIFECYCLE_ENUM", codes)
            self.assertIn("STATUS_ENUM", codes)

    def test_evidence_hash_and_release_freshness_fail_closed(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            source = root / "source.txt"
            source.write_text("authoritative", encoding="utf-8")
            evidence = root / "as-is/evidence.yaml"
            evidence.parent.mkdir(parents=True)
            evidence.write_text(
                """evidence:
  - id: EVID-0001
    kind: source
    path: source.txt
    sha256: aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa
    revision: rev-1
    captured_at: '2020-01-01T00:00:00Z'
    fresh_until: '2020-01-02T00:00:00Z'
    claim_limit: fixture
    dri: qa
    reviewer: reviewer
    phase: P0
    status: ACTIVE
    acceptance_evidence: ['BLOCKER: fixture']
""",
                encoding="utf-8",
            )
            validator = Validator(root, "release")
            validator.validate_yaml_and_ids()
            codes = {item.code for item in validator.findings}
            self.assertIn("EVIDENCE_HASH", codes)
            self.assertIn("EVIDENCE_REVISION", codes)
            self.assertIn("EVIDENCE_STALE", codes)

    def test_trace_relation_allowlist_and_cycle_fail(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            path = root / "registry/traceability.csv"
            path.parent.mkdir(parents=True)
            path.write_text(
                "source_id,relation,target_id\n"
                "OBJ-001,has_requirement,FR-001\n"
                "FR-001,loops_to,OBJ-001\n",
                encoding="utf-8",
            )
            validator = Validator(root, "authoring")
            validator.defined_ids = {"OBJ-001": path, "FR-001": path}
            validator.validate_traceability()
            codes = {item.code for item in validator.findings}
            self.assertIn("TRACE_RELATION_UNKNOWN", codes)
            self.assertIn("TRACE_CYCLE", codes)

    def test_premerge_brownfield_trace_complete_chain_passes(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            path = root / "registry/traceability.csv"
            path.parent.mkdir(parents=True)
            path.write_text(
                "source_id,relation,target_id\n"
                "CLAIM-0001,supported_by,EVID-0001\n"
                "CLAIM-0001,reveals,GAP-001\n"
                "GAP-001,addressed_by,FR-001\n"
                "OBJ-001,has_requirement,FR-001\n"
                "FR-001,designed_by,DOM-CORE-001\n"
                "FR-001,verified_by,TEST-UNIT-001\n"
                "FR-001,migrated_by,MIG-001\n"
                "MIG-001,verified_by,TEST-UNIT-001\n"
                "TEST-UNIT-001,gated_by,GATE-G1-001\n",
                encoding="utf-8",
            )
            ids = (
                "CLAIM-0001 EVID-0001 GAP-001 OBJ-001 FR-001 DOM-CORE-001 "
                "MIG-001 TEST-UNIT-001 GATE-G1-001"
            ).split()
            validator = Validator(root, "premerge")
            validator.defined_ids = {entity_id: path for entity_id in ids}
            validator.entities = {"GAP-001": {"status": "ACTIVE"}}
            validator.validate_traceability()
            self.assertEqual([], validator.findings)

    def test_domain_supported_by_evidence_trace_passes(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            path = root / "registry/traceability.csv"
            path.parent.mkdir(parents=True)
            path.write_text(
                "source_id,relation,target_id\nDOM-COMBAT-001,supported_by,EVID-0001\n",
                encoding="utf-8",
            )
            validator = Validator(root, "authoring")
            validator.defined_ids = {
                "DOM-COMBAT-001": path,
                "EVID-0001": path,
            }

            validator.validate_traceability()

            self.assertEqual([], validator.findings)

    def test_catalog_duplicate_enum_and_missing_coverage_fail(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            catalog_dir = root / "registry/catalogs"
            catalog_dir.mkdir(parents=True)
            record = {
                "catalog_id": "SKILL-1",
                "entity_type": "skill",
                "source_path": "skills.txt",
                "source_sha256": "a" * 64,
                "owner_domain": "skills",
                "lifecycle": "MAGIC",
                "verification": "UNVERIFIED",
                "disposition": "invent",
            }
            payload = (json.dumps(record) + "\n" + json.dumps(record) + "\n").encode()
            (catalog_dir / "skills.jsonl").write_bytes(payload)
            (catalog_dir / "index.yaml").write_text(
                "catalogs:\n"
                "  - path: skills.jsonl\n"
                "    entity_type: skill\n"
                "    records: 2\n"
                f"    sha256: {hashlib.sha256(payload).hexdigest()}\n",
                encoding="utf-8",
            )
            validator = Validator(root, "premerge")
            validator.validate_catalogs()
            codes = {item.code for item in validator.findings}
            self.assertIn("CATALOG_ID_DUPLICATE", codes)
            self.assertIn("CATALOG_LIFECYCLE_ENUM", codes)
            self.assertIn("CATALOG_DISPOSITION_ENUM", codes)
            self.assertIn("CATALOG_COVERAGE_DECLARATION", codes)

    def test_catalog_unknown_owner_fails(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            catalog_dir = root / "registry/catalogs"
            catalog_dir.mkdir(parents=True)
            record = {
                "catalog_id": "SKILL-1",
                "entity_type": "skill",
                "source_path": "bin/client/settings/Skills.txt",
                "source_sha256": "a" * 64,
                "owner_domain": "unregistered-owner",
                "lifecycle": "SOURCE_PROVEN",
                "verification": "VERIFIED",
                "disposition": "port",
            }
            payload = (json.dumps(record) + "\n").encode()
            (catalog_dir / "skills.jsonl").write_bytes(payload)
            (catalog_dir / "index.yaml").write_text(
                "owner_registry:\n"
                "  skills: DOM-SKILL-001\n"
                "catalogs:\n"
                "  - path: skills.jsonl\n"
                "    entity_type: skill\n"
                "    records: 1\n"
                f"    sha256: {hashlib.sha256(payload).hexdigest()}\n"
                "coverage:\n"
                "  skill:\n"
                "    discovered: 1\n"
                "    cataloged: 1\n"
                "    owned: 0\n"
                "    dispositioned: 1\n"
                "    unresolved: 0\n",
                encoding="utf-8",
            )
            validator = Validator(root, "premerge")
            validator.defined_ids = {"DOM-SKILL-001": root / "registry/designs.yaml"}

            validator.validate_catalogs()

            self.assertTrue(
                any(item.code == "CATALOG_OWNER_UNKNOWN" for item in validator.findings)
            )

    def test_catalog_source_census_drift_fails(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            self.write_source_census_fixture(root, drift_entity="skill")
            validator = Validator(root, "premerge")

            validator.validate_catalog_source_census()

            self.assertTrue(
                any(item.code == "CATALOG_SOURCE_CENSUS_DRIFT" for item in validator.findings)
            )

    def test_catalog_source_path_omission_fails(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            self.write_source_census_fixture(
                root, omitted_catalog_path="bin/client/Spr/effect.spr"
            )
            validator = Validator(root, "premerge")

            validator.validate_catalog_source_census()

            self.assertTrue(
                any(item.code == "CATALOG_SOURCE_PATH_COVERAGE" for item in validator.findings)
            )

    def test_dirty_source_snapshot_blocks_release(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            source_root = root / "source"
            source_root.mkdir()
            generator = root / "generator.py"
            generator.write_text("# fixture\n", encoding="utf-8")
            snapshot = root / "registry/catalogs/source-snapshot.yaml"
            snapshot.parent.mkdir(parents=True)
            snapshot.write_text(
                json.dumps(
                    {
                        "source_root": str(source_root),
                        "source_git": {"revision": "a" * 40, "dirty": True},
                        "vltktool": {"revision": "b" * 40, "dirty": False},
                        "generator": {
                            "path": str(generator),
                            "sha256": hashlib.sha256(generator.read_bytes()).hexdigest(),
                        },
                    }
                ),
                encoding="utf-8",
            )
            validator = Validator(root, "release")

            validator.validate_source_snapshot()

            self.assertTrue(
                any(item.code == "SOURCE_SNAPSHOT_DIRTY_RELEASE" for item in validator.findings)
            )

    def test_release_golden_minio_sha_is_verified(self):
        class QuietHandler(SimpleHTTPRequestHandler):
            def log_message(self, _format, *_args):
                return

        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            served = root / "served"
            payload = b"golden-runtime-frames"
            digest = hashlib.sha256(payload).hexdigest()
            object_key = f"sha256/{digest}/frames.bin"
            target = served / "bucket" / object_key
            target.parent.mkdir(parents=True)
            target.write_bytes(payload)
            handler = partial(QuietHandler, directory=str(served))
            server = ThreadingHTTPServer(("127.0.0.1", 0), handler)
            thread = threading.Thread(target=server.serve_forever, daemon=True)
            thread.start()
            try:
                manifest = root / "registry/golden-manifest.yaml"
                manifest.parent.mkdir(parents=True)
                manifest.write_text(
                    f"""storage:
  backend: minio
  bucket: bucket
  key_policy: sha256-content-addressed
  endpoint_env: TEST_MINIO_ENDPOINT
  access_key_env: TEST_MINIO_ACCESS
  secret_key_env: TEST_MINIO_SECRET
  region: us-east-1
  credentials_in_repo: false
goldens:
  - id: GOLD-0001
    status: READY
    required_for: [PAR-0001]
    artifact:
      object_key: {object_key}
      sha256: {digest}
      size_bytes: {len(payload)}
      captured_at: '2026-01-01T00:00:00Z'
      source_revision: rev-1
      tool_revision: tool-1
      content_type: application/octet-stream
""",
                    encoding="utf-8",
                )
                validator = Validator(root, "release")
                validator.defined_ids = {"PAR-0001": manifest, "GOLD-0001": manifest}
                with patch.dict(
                    os.environ,
                    {"TEST_MINIO_ENDPOINT": f"http://127.0.0.1:{server.server_port}"},
                    clear=False,
                ):
                    validator.validate_golden_manifest()
                self.assertEqual([], validator.findings)
                target.write_bytes(b"tampered-runtime-frame")
                tampered = Validator(root, "release")
                tampered.defined_ids = validator.defined_ids
                with patch.dict(
                    os.environ,
                    {"TEST_MINIO_ENDPOINT": f"http://127.0.0.1:{server.server_port}"},
                    clear=False,
                ):
                    tampered.validate_golden_manifest()
                self.assertTrue(
                    any(item.code == "MINIO_HASH" for item in tampered.findings)
                )
            finally:
                server.shutdown()
                thread.join(timeout=2)
                server.server_close()

    def test_open_blocker_always_blocks_release(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            path = root / "as-is/contradictions.yaml"
            path.parent.mkdir(parents=True)
            path.write_text(
                "contradictions:\n"
                "  - {id: CON-0001, status: OPEN_BLOCKER, blocker: runtime, dri: qa, reviewer: lead, acceptance_evidence: [GOLD-0001]}\n",
                encoding="utf-8",
            )
            validator = Validator(root, "release")
            validator.validate_contradictions()
            self.assertTrue(
                any(item.code == "OPEN_BLOCKER_RELEASE" for item in validator.findings)
            )

    def test_test_result_hash_and_revision_pass_premerge(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            artifact = root / "results/test.json"
            artifact.parent.mkdir(parents=True)
            artifact.write_text('{"pass":true}', encoding="utf-8")
            digest = hashlib.sha256(artifact.read_bytes()).hexdigest()
            snapshot = root / "registry/catalogs/source-snapshot.yaml"
            snapshot.parent.mkdir(parents=True)
            snapshot.write_text("source_git: {revision: rev-1}\n", encoding="utf-8")
            index = root / "registry/test-results/index.yaml"
            index.parent.mkdir(parents=True, exist_ok=True)
            index.write_text(
                f"""status: READY
results:
  - test_id: TEST-UNIT-001
    status: PASS
    revision: rev-1
    executed_at: '2026-01-01T00:00:00Z'
    result_path: results/test.json
    sha256: {digest}
    reviewer: qa
    golden_ids: [GOLD-0001]
""",
                encoding="utf-8",
            )
            validator = Validator(root, "premerge")
            validator.defined_ids = {"TEST-UNIT-001": index, "GOLD-0001": index}
            validator.validate_test_results()
            self.assertEqual([], validator.findings)
            artifact.write_text('{"pass":false}', encoding="utf-8")
            tampered = Validator(root, "premerge")
            tampered.defined_ids = validator.defined_ids
            tampered.validate_test_results()
            self.assertTrue(
                any(item.code == "TEST_RESULT_HASH" for item in tampered.findings)
            )

    def test_release_contract_tools_are_fail_closed(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            openapi = root / "contracts/openapi/game.v1.yaml"
            sql = root / "contracts/sql/game.v1.sql"
            openapi.parent.mkdir(parents=True)
            sql.parent.mkdir(parents=True)
            openapi.write_text("openapi: 3.1.0\n", encoding="utf-8")
            sql.write_text("BEGIN; COMMIT;\n", encoding="utf-8")
            validator = Validator(root, "release")
            with patch("validate.shutil.which", return_value=None):
                validator.validate_release_contract_tools()
            codes = {item.code for item in validator.findings}
            self.assertIn("OPENAPI_TOOL_UNAVAILABLE", codes)
            self.assertIn("SQL_TOOL_UNAVAILABLE", codes)

    def test_missing_protoc_is_error_in_premerge(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            proto = root / "contracts/proto/game/v1/game.proto"
            proto.parent.mkdir(parents=True)
            proto.write_text('syntax = "proto3";\n', encoding="utf-8")
            validator = Validator(root, "premerge")
            with patch("validate.shutil.which", return_value=None):
                validator.validate_proto()
            self.assertTrue(
                any(
                    item.code == "PROTO_TOOL_UNAVAILABLE" for item in validator.findings
                )
            )

    def test_records_schema_rejects_unexpected_field(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            schema_dir = root / "schemas"
            schema_dir.mkdir()
            source = Path(__file__).resolve().parents[3] / "specs/jx-pc-mobile-port/schemas/records.schema.yaml"
            shutil.copy2(source, schema_dir / "records.schema.yaml")
            record = root / "records.yaml"
            record.write_text(
                """items:
- id: OBJ-UNIT-001
  dri: owner
  reviewer: reviewer
  phase: P0
  status: ACTIVE
  acceptance_evidence: [note.md]
  title: Unit
  owner: owner
  unexpected_schema_field: true
""",
                encoding="utf-8",
            )
            (root / "note.md").write_text("evidence", encoding="utf-8")
            validator = Validator(root, "premerge")
            validator.validate_yaml_and_ids()
            self.assertTrue(any(item.code == "RECORD_INSTANCE_INVALID" for item in validator.findings))

    def test_records_schema_is_required_layout(self):
        with tempfile.TemporaryDirectory() as tmp:
            validator = Validator(Path(tmp), "premerge")
            validator.validate_required_layout()
            self.assertTrue(
                any(item.code == "FILE_MISSING" and item.path == "schemas/records.schema.yaml" for item in validator.findings)
            )

    def test_orchestration_and_mandatory_artifacts_are_required_layout(self):
        with tempfile.TemporaryDirectory() as tmp:
            validator = Validator(Path(tmp), "premerge")

            validator.validate_required_layout()

            missing = {
                item.path
                for item in validator.findings
                if item.code == "FILE_MISSING"
            }
            self.assertIn("governance/orchestration.md", missing)
            self.assertIn("registry/catalogs/source-snapshot.yaml", missing)
            self.assertIn("contracts/protobuf-fuzz.md", missing)
            self.assertIn("contracts/sql/game.v1.negative.sql", missing)
            self.assertIn("delivery/spec-scope-manifest.yaml", missing)
            self.assertIn("delivery/case-matrices/skill-static-selection.yaml", missing)

    def test_unmarked_tbd_fails(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / "spec.md").write_text("Retention TBD\n", encoding="utf-8")
            validator = Validator(root, "authoring")
            validator.validate_unmarked_placeholders()
            self.assertTrue(any(item.code == "UNMARKED_PLACEHOLDER" for item in validator.findings))

    def test_data_contract_declaration_is_required(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / "spec.yaml").write_text(
                "canonical_contracts: {rest: a, realtime: b, content: c}\n",
                encoding="utf-8",
            )
            validator = Validator(root, "premerge")
            validator.validate_contract_declarations()
            self.assertTrue(
                any(item.code == "CONTRACT_DECLARATION_MISSING" and "data" in item.message for item in validator.findings)
            )

    def test_training_fixture_is_validated_against_schema(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            source = Path(__file__).resolve().parents[3] / "specs/jx-pc-mobile-port"
            shutil.copytree(source / "delivery", root / "delivery")
            catalog_dir = root / "registry/catalogs"
            catalog_dir.mkdir(parents=True)
            shutil.copy2(source / "registry/catalogs/skills.jsonl", catalog_dir / "skills.jsonl")
            fixture = root / "delivery/fixtures/training-npcs.p0.json"
            document = json.loads(fixture.read_text(encoding="utf-8"))
            document.pop("fixture_id")
            fixture.write_text(json.dumps(document), encoding="utf-8")
            validator = Validator(root, "premerge")
            validator.validate_skill_case_matrix()
            self.assertTrue(any(item.code == "SKILL_FIXTURE_INVALID" for item in validator.findings))

    def test_skill_release_requires_full_catalog_case_expansion(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            source = Path(__file__).resolve().parents[3] / "specs/jx-pc-mobile-port"
            shutil.copytree(source / "delivery", root / "delivery")
            catalog_dir = root / "registry/catalogs"
            catalog_dir.mkdir(parents=True)
            shutil.copy2(source / "registry/catalogs/skills.jsonl", catalog_dir / "skills.jsonl")
            validator = Validator(root, "release")
            validator.validate_skill_case_matrix()
            codes = {item.code for item in validator.findings}
            self.assertIn("SKILL_MATRIX_CATALOG_COVERAGE", codes)
            self.assertIn("SKILL_CASE_EXPANSION_COVERAGE", codes)

    def test_ready_gate_requires_pass_artifacts(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            registry = root / "registry/test-results"
            registry.mkdir(parents=True)
            (root / "registry/gates.yaml").write_text(
                "gates:\n- {id: GATE-G2, phase: G2, status: READY}\n", encoding="utf-8"
            )
            (root / "registry/tests.yaml").write_text(
                "tests:\n- {id: TEST-UNIT-001, gate: G2, status: NOT_RUN}\n", encoding="utf-8"
            )
            (registry / "index.yaml").write_text("results: []\n", encoding="utf-8")
            validator = Validator(root, "premerge")
            validator.validate_gate_status()
            self.assertTrue(any(item.code == "GATE_READY_WITHOUT_PASS" for item in validator.findings))


if __name__ == "__main__":
    unittest.main()
