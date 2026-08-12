import contextlib
import io
import json
import tempfile
import unittest
from pathlib import Path

from validate import Validator

class ValidatorSmoke(unittest.TestCase):
    def _copy_min_tree(self) -> Path:
        root = Path(tempfile.mkdtemp())
        spec = root / "specs" / "jx-pc-mobile-port"
        (spec / "registry").mkdir(parents=True)
        (spec / "contracts" / "proto" / "game" / "v1").mkdir(parents=True)
        (spec / "contracts" / "sql").mkdir(parents=True)
        (root / "docs" / "stories" / "SKL-ALL-PARITY-001").mkdir(parents=True)
        for name in ["01-yeu-cau.md", "02-mo-hinh-yeu-cau.md", "03-du-lieu.md", "04-giao-dien.md"]:
            Path("specs/jx-pc-mobile-port", name).replace if False else None
        return root

    def test_reconnect_30_fails(self):
        root = self._copy_min_tree()
        spec = root / "specs" / "jx-pc-mobile-port"
        for name, headings in {
            "01-yeu-cau.md": ["Mô hình cơ cấu tổ chức", "Nhu cầu người dùng và Yêu cầu của phần mềm (NGHIỆP VỤ)", "Biểu mẫu", "Quy định", "Danh sách yêu cầu", "Bảng trách nhiệm", "Bảng mô tả chi tiết yêu cầu nghiệp vụ"],
            "02-mo-hinh-yeu-cau.md": ["Mô hình chức năng", "Sơ đồ sử dụng chức năng", "Sơ đồ phân quyền sử dụng", "Sơ đồ luồng dữ liệu", "Sơ đồ khai thác hệ thống"],
            "03-du-lieu.md": ["Thiết kế dữ liệu với tính đúng đắn", "Thiết kế dữ liệu với yêu cầu chất lượng (tối ưu tiến hóa, lưu trữ và tốc độ xử lý)", "Thiết kế dữ liệu với yêu cầu hệ thống"],
            "04-giao-dien.md": ["Bảng tiêu chuẩn thiết kế giao diện", "Sơ đồ giao diện tổng quát", "Giao diện chi tiết"],
        }.items():
            (spec / name).write_text("\n".join(f"# {h}" for h in headings) + "\n")
        (spec / "01-yeu-cau.md").write_text((spec / "01-yeu-cau.md").read_text() + "Grace 30 giây\n")
        (spec / "registry" / "traceability.csv").write_text("source_id,relation,target_id\n")
        (root / "docs" / "stories" / "SKL-ALL-PARITY-001" / "coverage-matrix.json").write_text(json.dumps({"global_union_size": 242, "summary_counts": {"union_rows_total": 242}}))
        (spec / "contracts" / "proto" / "game" / "v1" / "game.proto").write_text('syntax = "proto3"; package game.v1; message ContentDigest{} message RuntimeSkillPolicy{} message EncounterPreloadAck{} message ActiveCombatResyncState{} message ServerHello{uint32 reconnect_grace_seconds=15;} enum CombatEventKind{COMBAT_EVENT_KIND_UNSPECIFIED=0; COMBAT_EVENT_KIND_CAST_RECOVERY_STARTED=1; COMBAT_EVENT_KIND_MISSILE_COLLIDED=2; COMBAT_EVENT_KIND_STATUS_REFRESHED=3; COMBAT_EVENT_KIND_STATUS_EXPIRED=4;}')
        (spec / "contracts" / "sql" / "game.v1.sql").write_text("CREATE TABLE admission_tickets(reconnect_grace_seconds integer NOT NULL DEFAULT 15 CHECK (reconnect_grace_seconds=15), catalog_union_size integer); CREATE TABLE encounter_preload_acks(id integer); CREATE TABLE combat_lifecycle_events(id integer);")
        v = Validator(root, "premerge")
        with contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(io.StringIO()):
            result = v.run()
        self.assertEqual(result, 1)
        self.assertTrue(any("30-second" in f.message for f in v.findings))

    def test_release_mode_rejects_test_only_manifest_key(self):
        base = Path(tempfile.mkdtemp())
        root = base / "harness"
        (root / "specs" / "jx-pc-mobile-port").mkdir(parents=True)
        manifest = base / "Assets" / "StreamingAssets" / "Generated" / "SkillPort" / "manifest.json"
        manifest.parent.mkdir(parents=True)
        manifest.write_text(json.dumps({"signingKeyId": "test-only-skill-port-ed25519-fixture-v1"}))
        v = Validator(root, "release")
        v.check_release_signing_gate()
        self.assertTrue(any("forbids development signing key" in f.message for f in v.findings))

    def test_proto_duplicate_tag_fails(self):
        root = Path(tempfile.mkdtemp())
        spec = root / "specs" / "jx-pc-mobile-port" / "contracts" / "proto" / "game" / "v1"
        spec.mkdir(parents=True)
        (spec / "game.proto").write_text('''syntax = "proto3";
package game.v1;
message ContentDigest {
  string a = 1;
  string b = 1;
}
''')
        v = Validator(root, "premerge")
        v.check_proto()
        self.assertTrue(any("reuses tag" in f.message for f in v.findings))

if __name__ == "__main__":
    unittest.main()
