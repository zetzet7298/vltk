#!/usr/bin/env python3
"""Gate 0 validator for jx-pc-mobile-port spec/contracts.

Fail closed on the small surface Gate 0 owns: machine-readable syntax, CNPM
shape, registry traceability closure, approved reconnect/catalog decisions, and
basic additive protobuf/SQL contract invariants.
"""
from __future__ import annotations

import argparse
import csv
import json
import re
import sqlite3
import subprocess
import sys
from collections import defaultdict
from dataclasses import dataclass
from pathlib import Path
from typing import Any

import yaml
from jsonschema import Draft202012Validator
from markdown_it import MarkdownIt

ID_RE = re.compile(r"\b(?:OBJ|FR|NFR|ADR|RISK|TEST|MIG|DEBT|CLAIM|EVID|CON|GAP|GOLD|PAR|DOM|GATE)-[A-Z0-9]+(?:-[A-Z0-9]+)*\b")
FIELD_RE = re.compile(r"^\s*(?:repeated\s+)?(?:optional\s+)?[A-Za-z_][\w.<>]*\s+([a-zA-Z_][\w]*)\s*=\s*(\d+)\b")
ONEOF_FIELD_RE = FIELD_RE
ENUM_VALUE_RE = re.compile(r"^\s*([A-Z][A-Z0-9_]*)\s*=\s*(\d+)\b")
MESSAGE_RE = re.compile(r"^\s*message\s+(\w+)\s*{")
ENUM_RE = re.compile(r"^\s*enum\s+(\w+)\s*{")
BAD_RECONNECT_RE = re.compile(r"(?:grace\s*30|30\s*giây|30\s*seconds|reconnect_grace_seconds\s*=\s*30|DEFAULT\s+30)", re.I)

CNPM_HEADINGS = {
    "01-yeu-cau.md": [
        (1, "Mô hình cơ cấu tổ chức"),
        (1, "Nhu cầu người dùng và Yêu cầu của phần mềm (NGHIỆP VỤ)"),
        (1, "Biểu mẫu"),
        (1, "Quy định"),
        (1, "Danh sách yêu cầu"),
        (1, "Bảng trách nhiệm"),
        (1, "Bảng mô tả chi tiết yêu cầu nghiệp vụ"),
    ],
    "02-mo-hinh-yeu-cau.md": [
        (1, "Mô hình chức năng"),
        (1, "Sơ đồ sử dụng chức năng"),
        (1, "Sơ đồ phân quyền sử dụng"),
        (1, "Sơ đồ luồng dữ liệu"),
        (1, "Sơ đồ khai thác hệ thống"),
    ],
    "03-du-lieu.md": [
        (1, "Thiết kế dữ liệu với tính đúng đắn"),
        (1, "Thiết kế dữ liệu với yêu cầu chất lượng (tối ưu tiến hóa, lưu trữ và tốc độ xử lý)"),
        (1, "Thiết kế dữ liệu với yêu cầu hệ thống"),
    ],
    "04-giao-dien.md": [
        (1, "Bảng tiêu chuẩn thiết kế giao diện"),
        (1, "Sơ đồ giao diện tổng quát"),
        (1, "Giao diện chi tiết"),
    ],
}

@dataclass
class Finding:
    path: str
    message: str

class Validator:
    def __init__(self, root: Path, mode: str):
        self.root = root
        self.spec = root / "specs" / "jx-pc-mobile-port"
        self.mode = mode
        self.findings: list[Finding] = []
        self.ids: set[str] = set()

    def fail(self, path: Path | str, message: str) -> None:
        p = str(path)
        try:
            p = str(Path(path).relative_to(self.root))
        except Exception:
            pass
        self.findings.append(Finding(p, message))

    def run(self) -> int:
        if not self.spec.exists():
            self.fail(self.spec, "spec root missing")
        else:
            self.check_machine_readable()
            self.collect_registry_ids()
            self.check_cnpm_markdown()
            self.check_traceability()
            self.check_gate0_decisions()
            self.check_proto()
            self.check_content_proto()
            self.check_content_manifest_schema()
            self.check_release_signing_gate()
            self.check_sql_contract()
        if self.findings:
            for f in self.findings:
                print(f"FAIL {f.path}: {f.message}", file=sys.stderr)
            return 1
        print(f"spec-validator: PASS mode={self.mode} root={self.spec.relative_to(self.root)}")
        return 0

    def check_machine_readable(self) -> None:
        for path in self.spec.rglob("*"):
            if not path.is_file():
                continue
            try:
                if path.suffix == ".json":
                    data = json.loads(path.read_text())
                    if path.name.endswith(".schema.json"):
                        Draft202012Validator.check_schema(data)
                elif path.suffix in {".yaml", ".yml"}:
                    yaml.safe_load(path.read_text())
                elif path.suffix == ".csv":
                    with path.open(newline="") as fh:
                        rows = list(csv.reader(fh))
                    if not rows:
                        self.fail(path, "empty csv")
            except Exception as exc:
                self.fail(path, f"parse/schema error: {exc}")

    def collect_registry_ids(self) -> None:
        for path in [self.spec / "registry", self.spec / "as-is"]:
            if not path.exists():
                continue
            for file in path.rglob("*"):
                if file.suffix not in {".yaml", ".yml", ".csv"}:
                    continue
                text = file.read_text(errors="ignore")
                self.ids.update(ID_RE.findall(text))
        for file in (self.spec / "governance" / "adrs").glob("ADR-*.md"):
            self.ids.add(file.stem)
        self.ids.update({"DOM-ACS", "DOM-WMM", "DOM-CBT", "DOM-SKL", "DOM-PRG", "DOM-IIEL", "DOM-NQ", "DOM-AUTO"})

    def check_cnpm_markdown(self) -> None:
        md = MarkdownIt()
        for name, expected in CNPM_HEADINGS.items():
            path = self.spec / name
            if not path.exists():
                self.fail(path, "CNPM file missing")
                continue
            found: list[tuple[int, str]] = []
            tokens = md.parse(path.read_text())
            for i, token in enumerate(tokens[:-1]):
                if token.type == "heading_open" and tokens[i + 1].type == "inline":
                    found.append((int(token.tag[1]), tokens[i + 1].content.strip()))
            cursor = 0
            for heading in expected:
                try:
                    cursor = found.index(heading, cursor) + 1
                except ValueError:
                    self.fail(path, f"missing/out-of-order heading {heading[0]} {heading[1]!r}")
            text = path.read_text()
            for marker in ("$true", "[Tên yêu cầu]", "[Chức năng 1]", "[Màn hình giao diện 1]"):
                if marker in text:
                    self.fail(path, f"template marker left behind: {marker}")

    def check_traceability(self) -> None:
        path = self.spec / "registry" / "traceability.csv"
        if not path.exists():
            self.fail(path, "traceability missing")
            return
        with path.open(newline="") as fh:
            for n, row in enumerate(csv.DictReader(fh), start=2):
                for col in ("source_id", "target_id"):
                    value = row.get(col, "")
                    if value and value not in self.ids:
                        self.fail(path, f"row {n} references unknown {col}={value}")

    def check_gate0_decisions(self) -> None:
        text_paths = [
            self.spec / "01-yeu-cau.md",
            self.spec / "02-mo-hinh-yeu-cau.md",
            self.spec / "03-du-lieu.md",
            self.spec / "delivery" / "acceptance-plan.md",
            self.spec / "delivery" / "release-plan.md",
            self.spec / "delivery" / "test-strategy.md",
            self.spec / "domains" / "account-character-session.md",
            self.spec / "domains" / "server-runtime" / "postgresql-data-dictionary.md",
            self.spec / "contracts" / "sql" / "game.v1.sql",
        ]
        for path in text_paths:
            if path.exists() and BAD_RECONNECT_RE.search(path.read_text(errors="ignore")):
                self.fail(path, "stale 30-second reconnect grace; approved value is 15 seconds")
        cm = self.root / "docs" / "stories" / "SKL-ALL-PARITY-001" / "coverage-matrix.json"
        if cm.exists():
            data = json.loads(cm.read_text())
            if data.get("global_union_size") != 242:
                self.fail(cm, "global_union_size must be 242")
            summary = data.get("summary_counts", {})
            if summary.get("union_rows_total") != 242:
                self.fail(cm, "summary_counts.union_rows_total must be 242")
        if self.mode == "release":
            # Release still cannot pass while runtime/device proof is blocked, but spec validator
            # must prove contracts do not claim those unavailable passes.
            all_text = "\n".join(p.read_text(errors="ignore") for p in self.spec.rglob("*.md"))
            if re.search(r"PC runtime.*PASS|Android physical.*PASS|PARITY_DONE.*Android", all_text, re.I):
                self.fail(self.spec, "release docs claim unavailable PC/Android parity")

    def check_proto(self) -> None:
        path = self.spec / "contracts" / "proto" / "game" / "v1" / "game.proto"
        if not path.exists():
            self.fail(path, "game.proto missing")
            return
        stack: list[tuple[str, str, dict[int, str]]] = []
        for n, line in enumerate(path.read_text().splitlines(), start=1):
            m = MESSAGE_RE.match(line)
            e = ENUM_RE.match(line)
            if m:
                stack.append(("message", m.group(1), {})); continue
            if e:
                stack.append(("enum", e.group(1), {})); continue
            if "}" in line and stack:
                stack.pop(); continue
            if not stack:
                continue
            kind, name, numbers = stack[-1]
            fm = FIELD_RE.match(line) if kind == "message" else ENUM_VALUE_RE.match(line)
            if fm:
                number = int(fm.group(2))
                previous = numbers.get(number)
                if previous and previous != fm.group(1):
                    self.fail(path, f"{name} reuses tag/value {number}: {previous} vs {fm.group(1)} at line {n}")
                numbers[number] = fm.group(1)
        text = path.read_text()
        required_terms = [
            "ContentDigest", "RuntimeSkillPolicy", "EncounterPreloadAck", "ActiveCombatResyncState",
            "COMBAT_EVENT_KIND_CAST_RECOVERY_STARTED", "COMBAT_EVENT_KIND_MISSILE_COLLIDED",
            "COMBAT_EVENT_KIND_STATUS_REFRESHED", "COMBAT_EVENT_KIND_STATUS_EXPIRED",
            "reconnect_grace_seconds",
        ]
        for term in required_terms:
            if term not in text:
                self.fail(path, f"missing Gate 0 proto contract term {term}")
        proc = subprocess.run(
            ["protoc", f"--proto_path={path.parent}", "--descriptor_set_out=/dev/null", str(path.name)],
            cwd=path.parent,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
        )
        if proc.returncode != 0:
            self.fail(path, f"protoc failed: {proc.stderr.strip()}")

    def check_content_proto(self) -> None:
        path = self.spec / "contracts" / "content" / "v1" / "skill_catalog.proto"
        if not path.exists():
            self.fail(path, "content.v1 skill_catalog.proto missing")
            return
        text = path.read_text()
        for term in ["package content.v1", "message SkillCatalog", "message ServerSkillCatalog", "message ClientSkillCatalog", "message RuntimeSkillPolicy", "message ReproducibilityMetadata"]:
            if term not in text:
                self.fail(path, f"missing content proto contract term {term}")
        proc = subprocess.run(
            ["protoc", f"--proto_path={self.spec / 'contracts'}", "--descriptor_set_out=/dev/null", str(path.relative_to(self.spec / "contracts"))],
            cwd=self.spec / "contracts",
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
        )
        if proc.returncode != 0:
            self.fail(path, f"protoc failed: {proc.stderr.strip()}")

    def check_content_manifest_schema(self) -> None:
        path = self.spec / "contracts" / "content" / "manifest.v1.schema.json"
        if not path.exists():
            self.fail(path, "content manifest schema missing")
            return
        data = json.loads(path.read_text())
        props = data.get("$defs", {}).get("contentDigest", {}).get("properties", {})
        required = data.get("$defs", {}).get("contentDigest", {}).get("required", [])
        if "clientProjectionSha256" not in props or "clientProjectionSha256" not in required:
            self.fail(path, "contentDigest must require clientProjectionSha256")

    def check_release_signing_gate(self) -> None:
        if self.mode != "release":
            return
        manifest_path = self.root.parent / "Assets" / "StreamingAssets" / "Generated" / "SkillPort" / "manifest.json"
        if not manifest_path.exists():
            return
        manifest = json.loads(manifest_path.read_text())
        key_id = str(manifest.get("signingKeyId", ""))
        if key_id.startswith("test-only-"):
            self.fail(manifest_path, f"production release forbids development signing key {key_id}")

    def check_sql_contract(self) -> None:
        path = self.spec / "contracts" / "sql" / "game.v1.sql"
        if not path.exists():
            self.fail(path, "SQL contract missing")
            return
        text = path.read_text()
        balance = 0
        for ch in re.sub(r"--.*", "", text):
            if ch == "(": balance += 1
            elif ch == ")": balance -= 1
            if balance < 0:
                self.fail(path, "unbalanced SQL parentheses")
                break
        if balance != 0:
            self.fail(path, "unbalanced SQL parentheses")
        for term in ["reconnect_grace_seconds integer NOT NULL DEFAULT 15", "catalog_union_size", "encounter_preload_acks", "combat_lifecycle_events"]:
            if term not in text:
                self.fail(path, f"missing Gate 0 SQL term {term}")
        if "CHECK (reconnect_grace_seconds=15)" not in text:
            self.fail(path, "reconnect grace SQL check must be 15")

def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--mode", choices=("premerge", "release"), default="premerge")
    args = parser.parse_args(argv)
    return Validator(args.root.resolve(), args.mode).run()

if __name__ == "__main__":
    raise SystemExit(main())
