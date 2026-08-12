#!/usr/bin/env python3
"""Fail-closed validator for the VLTK PC-to-mobile CNPM spec package."""

from __future__ import annotations

import argparse
from collections import Counter
import csv
from datetime import datetime, timezone
import hashlib
import hmac
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
import unicodedata
from urllib.error import HTTPError, URLError
from urllib.parse import quote, urlparse
from urllib.request import Request, urlopen
from dataclasses import dataclass, asdict
from pathlib import Path

import yaml
from jsonschema import Draft202012Validator
from jsonschema.exceptions import SchemaError
from markdown_it import MarkdownIt


ID_RE = re.compile(
    r"^(?:OBJ|FR|ADR|RISK|MIG|DEBT|CLAIM|EVID|CON|GAP|GOLD|PAR|RUN|DOM|GATE)-[A-Z0-9-]+$"
    r"|^NFR-[A-Z]+-[0-9]+$|^TEST-[A-Z]+-[0-9]+$"
)
SHA256_RE = re.compile(r"^[0-9a-f]{64}$")
REVISION_RE = re.compile(r"^[a-z][a-z0-9-]*:[0-9a-f]{40}(?:[0-9a-f]{24})?(?:\+dirty)?$")
STABLE_ID_PREFIXES = (
    "OBJ-",
    "FR-",
    "NFR-",
    "ADR-",
    "RISK-",
    "TEST-",
    "MIG-",
    "DEBT-",
    "CLAIM-",
    "EVID-",
    "CON-",
    "GAP-",
    "GOLD-",
    "PAR-",
)
MARKDOWN_ID_REF_RE = re.compile(
    r"(?<![A-Z0-9-])(?:OBJ|FR|NFR|ADR|RISK|TEST|MIG|DEBT|CLAIM|EVID|CON|GAP|GOLD|PAR|DOM|GATE)-[A-Z0-9]+(?:-[A-Z0-9]+)*(?![A-Z0-9-])"
)
GOVERNED_PREFIXES = STABLE_ID_PREFIXES
GOVERNANCE_FIELDS = {"dri", "reviewer", "phase", "status", "acceptance_evidence"}
LIFECYCLE_VALUES = {
    "DISCOVERED",
    "SOURCE_PROVEN",
    "SPECIFIED",
    "READY",
    "IMPLEMENTING",
    "FUNCTIONAL",
    "VERIFYING",
    "PARITY_DONE",
    "BLOCKED",
    "DEFERRED",
    "OUT_OF_SCOPE",
    "SUPERSEDED",
}
VERIFICATION_VALUES = {
    "UNVERIFIED",
    "VERIFIED",
    "AUTOMATED_VERIFIED",
    "RUNTIME_VERIFIED",
    "HUMAN_ACCEPTED",
    "BLOCKED",
    "VISUAL_DEBT",
    "STALE",
}
GOVERNANCE_STATUS_VALUES = {
    "DRAFT",
    "ACTIVE",
    "READY",
    "REQUIRED",
    "BLOCKED",
    "DEFERRED",
    "RESOLVED",
    "OPEN",
    "OPEN_BLOCKER",
    "APPROVED",
    "COMPLETED",
    "RETIRED",
    "SUPERSEDED",
    "OUT_OF_SCOPE",
    "ACCEPTED",
    "NOT_RUN",
    "PASS",
    "FAIL",
    "PLANNED",
} | LIFECYCLE_VALUES
STATUS_BY_PREFIX: dict[str, set[str]] = {
    "OBJ-": {
        "DRAFT",
        "ACTIVE",
        "READY",
        "BLOCKED",
        "DEFERRED",
        "COMPLETED",
        "SUPERSEDED",
    },
    "FR-": LIFECYCLE_VALUES | {"ACTIVE", "UNVERIFIED"},
    "NFR-": LIFECYCLE_VALUES | {"ACTIVE", "UNVERIFIED"},
    "ADR-": {"DRAFT", "ACCEPTED", "APPROVED", "SUPERSEDED", "DEFERRED"},
    "RISK-": {
        "OPEN",
        "BLOCKED",
        "DEFERRED",
        "RESOLVED",
        "RETIRED",
        "OUT_OF_SCOPE",
        "SUPERSEDED",
    },
    "TEST-": {"REQUIRED", "NOT_RUN", "PASS", "FAIL", "BLOCKED", "DEFERRED"},
    "MIG-": {
        "DRAFT",
        "PLANNED",
        "READY",
        "IMPLEMENTING",
        "COMPLETED",
        "BLOCKED",
        "DEFERRED",
        "SUPERSEDED",
    },
    "DEBT-": {
        "OPEN",
        "BLOCKED",
        "DEFERRED",
        "RESOLVED",
        "RETIRED",
        "OUT_OF_SCOPE",
        "SUPERSEDED",
    },
    "CLAIM-": {
        "ACTIVE",
        "DISCOVERED",
        "SOURCE_PROVEN",
        "STALE",
        "BLOCKED",
        "SUPERSEDED",
    },
    "EVID-": {
        "ACTIVE",
        "DISCOVERED",
        "SOURCE_PROVEN",
        "STALE",
        "BLOCKED",
        "SUPERSEDED",
    },
    "CON-": {"OPEN", "OPEN_BLOCKER", "RESOLVED", "SUPERSEDED"},
    "GAP-": {
        "OPEN",
        "BLOCKED",
        "DEFERRED",
        "RESOLVED",
        "RETIRED",
        "OUT_OF_SCOPE",
        "SUPERSEDED",
    },
    "GOLD-": {"REQUIRED", "READY", "BLOCKED", "APPROVED", "COMPLETED", "SUPERSEDED"},
    "PAR-": LIFECYCLE_VALUES | {"ACTIVE", "UNVERIFIED"},
}
CATALOG_ENTITY_TYPES = {
    "audio",
    "avatar",
    "avatarcandidate",
    "deferredscript",
    "goods",
    "lua",
    "map",
    "missile",
    "npc",
    "package",
    "quest",
    "questcandidate",
    "setting",
    "skill",
    "sprite",
    "uilua",
    "uispr",
    "uifile",
}
CATALOG_DISPOSITIONS = {
    "port",
    "adapt",
    "exclude",
    "defer",
    "defer_reference_only",
    "defer_out_of_scope",
    "devharness",
}
TRACE_RELATIONS: dict[str, tuple[tuple[str, ...], tuple[str, ...]]] = {
    "has_requirement": (("OBJ-",), ("FR-", "NFR-")),
    "decided_by": (("FR-", "NFR-", "GAP-", "RISK-", "DEBT-"), ("ADR-",)),
    "designed_by": (("FR-", "NFR-"), ("DOM-",)),
    "verified_by": (("FR-", "NFR-", "MIG-", "PAR-", "GAP-", "DEBT-"), ("TEST-",)),
    "gated_by": (("TEST-",), ("GATE-",)),
    "supported_by": (("CLAIM-", "CON-", "GAP-", "RISK-", "DEBT-", "DOM-"), ("EVID-",)),
    "reveals": (("CLAIM-", "EVID-"), ("GAP-", "RISK-", "DEBT-")),
    "addresses": (("FR-", "NFR-"), ("GAP-", "RISK-", "DEBT-")),
    "addressed_by": (("GAP-", "RISK-", "DEBT-"), ("FR-", "NFR-")),
    "migrated_by": (("FR-", "NFR-"), ("MIG-",)),
    "requires_golden": (("PAR-", "TEST-", "FR-", "NFR-"), ("GOLD-",)),
    "affects": (("CON-",), ("PAR-", "GAP-", "FR-", "NFR-")),
    "resolved_by": (("CON-",), ("ADR-", "FR-", "NFR-")),
    "verifies": (("PAR-",), ("FR-", "NFR-")),
}
MD_LINK_RE = re.compile(r"\[[^\]]+\]\(([^)]+)\)")
BAD_TEMPLATE_MARKERS = (
    "$true",
    "[Tên yêu cầu]",
    "[Chức năng 1]",
    "[Màn hình giao diện 1]",
)
HTTP_METHODS = {"get", "put", "post", "delete", "options", "head", "patch", "trace"}
POLICY_MARKER_TERMS = (
    "marker",
    "placeholder",
    "chuỗi",
    "token",
    "cú pháp",
    "quy tắc",
    "policy",
    "ví dụ",
    "mô tả",
    "literal",
    "validator",
    "scan",
    "được phép",
    "không được",
    "claim thiếu nguồn",
)


def _heading(level: int, title: str) -> tuple[int, str]:
    return level, title


CNPM_HEADINGS: dict[str, tuple[tuple[int, str], ...]] = {
    "01-yeu-cau.md": (
        _heading(1, "Mô hình cơ cấu tổ chức"),
        _heading(2, "Sơ đồ tổ chức"),
        _heading(2, "Ý nghĩa các bộ phận"),
        _heading(1, "Nhu cầu người dùng và Yêu cầu của phần mềm (NGHIỆP VỤ)"),
        _heading(1, "Biểu mẫu"),
        _heading(1, "Quy định"),
        _heading(1, "Danh sách yêu cầu"),
        _heading(2, "Danh sách yêu cầu nghiệp vụ"),
        _heading(2, "Danh sách yêu cầu tiến hóa"),
        _heading(2, "Danh sách yêu cầu hiệu quả"),
        _heading(2, "Danh sách yêu cầu tiện dụng"),
        _heading(2, "Danh sách yêu cầu bảo mật"),
        _heading(2, "Danh sách yêu cầu an toàn"),
        _heading(2, "Danh sách yêu cầu tương thích"),
        _heading(2, "Danh sách yêu cầu công nghệ"),
        _heading(1, "Bảng trách nhiệm"),
        _heading(2, "Bảng trách nhiệm yêu cầu nghiệp vụ"),
        _heading(2, "Bảng trách nhiệm yêu cầu tiến hóa"),
        _heading(2, "Bảng trách nhiệm yêu cầu hiệu quả"),
        _heading(2, "Bảng trách nhiệm yêu cầu tiện dụng"),
        _heading(2, "Bảng trách nhiệm yêu cầu bảo mật"),
        _heading(2, "Bảng trách nhiệm yêu cầu an toàn"),
        _heading(2, "Bảng trách nhiệm yêu cầu tương thích"),
        _heading(1, "Bảng mô tả chi tiết yêu cầu nghiệp vụ"),
    ),
    "02-mo-hinh-yeu-cau.md": (
        _heading(1, "Mô hình chức năng"),
        _heading(2, "Sơ đồ chức năng"),
        _heading(2, "Ý nghĩa các chức năng"),
        _heading(1, "Sơ đồ sử dụng chức năng"),
        _heading(1, "Sơ đồ phân quyền sử dụng"),
        _heading(1, "Sơ đồ luồng dữ liệu"),
        _heading(1, "Sơ đồ khai thác hệ thống"),
        _heading(2, "Cách thức triển khai"),
        _heading(2, "Sơ đồ triển khai"),
    ),
    "03-du-lieu.md": (
        _heading(1, "Thiết kế dữ liệu với tính đúng đắn"),
        _heading(2, "Xác định các bảng"),
        _heading(2, "Sơ đồ ERD"),
        _heading(2, "Chi tiết các bảng"),
        _heading(
            1,
            "Thiết kế dữ liệu với yêu cầu chất lượng (tối ưu tiến hóa, lưu trữ và tốc độ xử lý)",
        ),
        _heading(2, "Xác định các bảng"),
        _heading(2, "Sơ đồ ERD"),
        _heading(2, "Chi tiết các bảng"),
        _heading(2, "Nội dung bảng tham số"),
        _heading(2, "Các thuộc tính tối ưu tốc độ xử lý"),
        _heading(1, "Thiết kế dữ liệu với yêu cầu hệ thống"),
        _heading(2, "Yêu cầu bảo mật (Phân quyền, mã hóa dữ liệu)"),
        _heading(3, "Xác định các bảng"),
        _heading(3, "Sơ đồ ERD"),
        _heading(3, "Chi tiết các bảng"),
        _heading(2, "Yêu cầu an toàn (sao lưu backup, hồi phục dữ liệu, xóa dữ liệu)"),
        _heading(3, "Sao lưu backup"),
        _heading(3, "Hồi phục dữ liệu"),
        _heading(3, "Xóa dữ liệu"),
    ),
    "04-giao-dien.md": (
        _heading(1, "Bảng tiêu chuẩn thiết kế giao diện"),
        _heading(2, "Tiêu chuẩn đối với các màn hình"),
        _heading(2, "Tiêu chuẩn đối với các yếu tố trên màn hình"),
        _heading(1, "Sơ đồ giao diện tổng quát"),
        _heading(1, "Giao diện chi tiết"),
    ),
}


CNPM_TABLE_SIGNATURES: dict[str, tuple[tuple[tuple[str, ...], int], ...]] = {
    "01-yeu-cau.md": (
        (("STT", "Tên bộ phận", "Mô tả"), 1),
        (
            (
                "STT",
                "Nhu cầu",
                "Nghiệp vụ",
                "Ai",
                "",
                "",
                "",
                "Mức độ hỗ trợ",
                "Phân loại yêu cầu",
            ),
            1,
        ),
        (("Tên quy định", "Nội dung"), 1),
        (("STT", "Nghiệp vụ", "Mô tả tóm tắt", "Biểu mẫu", "Quy định", "Ghi chú"), 1),
        (("STT", "Nghiệp vụ", "Tham số cần thay đổi", "Miền giá trị cần thay đổi"), 1),
        (("STT", "Nghiệp vụ", "Tốc độ xử lí", "Dung lượng lưu trữ", "Ghi chú"), 1),
        (("STT", "Nghiệp vụ", "Mức độ dễ học", "Mức độ dễ sử dụng", "Ghi chú"), 1),
        (("STT", "Nghiệp vụ \\ Nhóm người dùng", "*", "*", "*"), 1),
        (("STT", "Nghiệp vụ", "Đối tượng", "Ghi chú"), 2),
        (("STT", "Yêu cầu", "Mô tả chi tiết", "Ghi chú"), 1),
        (("STT", "Nghiệp vụ", "Người dùng", "Phần mềm", "Ghi chú"), 7),
    ),
    "02-mo-hinh-yeu-cau.md": (
        (("STT", "Tên chức năng", "Mô tả"), 1),
        (
            (
                "Vai trò hệ thống",
                "Chức năng hệ thống",
                "Quyền (Admin, Add, Update, Delete, View)",
            ),
            1,
        ),
        (("TÊN THAO TÁC NGHIỆP VỤ", "*"), 1),
    ),
    "03-du-lieu.md": (
        (
            (
                "TT",
                "Tên thuộc tính (Field name)",
                "Kiểu dữ liệu",
                "Độ rộng",
                "Not NULL",
                "Ràng buộc / Miền giá trị",
                "Diễn giải",
            ),
            1,
        ),
        (("MaThamSo", "GiaTri", "GhiChu"), 1),
        (
            (
                "TT",
                "Thuộc tính",
                "Bảng của thuộc tính",
                "Bảng của thông tin gốc",
                "Xử lý tự động cập nhật",
            ),
            1,
        ),
        (
            (
                "TT",
                "Tên thuộc tính (Field name)",
                "Kiểu dữ liệu",
                "Độ rộng",
                "Not NULL",
                "Ràng buộc / Miền giá trị",
                "Mã hóa",
                "Diễn giải",
            ),
            1,
        ),
        (
            (
                "TT",
                "Thuộc tính sao lưu",
                "Bảng của thuộc tính",
                "Tần suất sao lưu",
                "Thời gian sao lưu",
                "Nơi sao lưu",
                "Tự động/bằng tay",
            ),
            1,
        ),
        (
            (
                "TT",
                "Thuộc tính hồi phục",
                "Bảng của thuộc tính",
                "Ai được phép",
                "Nơi hồi phục",
            ),
            1,
        ),
        (
            (
                "TT",
                "Thuộc tính xóa",
                "Bảng của thuộc tính",
                "Khi nào xóa",
                "Tự động / Bằng tay",
            ),
            1,
        ),
    ),
    "04-giao-dien.md": (
        (
            (
                "Yếu tố",
                "Kích thước",
                "Canh lề",
                "Cách tổ chức",
                "Phím nóng / phím tắt",
                "Yêu cầu kết xuất",
            ),
            1,
        ),
        (
            (
                "Yếu tố",
                "Font type",
                "Font size",
                "Font Color",
                "Canh lề",
                "Kích thước",
                "Hình dạng",
            ),
            1,
        ),
        (("STT", "Thao tác", "Ý nghĩa", "Xử lý liên quan", "Ghi chú"), 1),
    ),
}


class UniqueKeyLoader(yaml.SafeLoader):
    pass


def _construct_mapping(loader: yaml.Loader, node: yaml.Node, deep: bool = False):
    mapping = {}
    for key_node, value_node in node.value:
        key = loader.construct_object(key_node, deep=deep)
        if key in mapping:
            raise yaml.constructor.ConstructorError(
                "while constructing a mapping",
                node.start_mark,
                f"duplicate key: {key}",
                key_node.start_mark,
            )
        mapping[key] = loader.construct_object(value_node, deep=deep)
    return mapping


UniqueKeyLoader.add_constructor(
    yaml.resolver.BaseResolver.DEFAULT_MAPPING_TAG, _construct_mapping
)


def _json_unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError(f"duplicate JSON key: {key}")
        result[key] = value
    return result


def _walk_refs(value):
    if isinstance(value, dict):
        ref = value.get("$ref")
        if isinstance(ref, str):
            yield ref
        for child in value.values():
            yield from _walk_refs(child)
    elif isinstance(value, list):
        for child in value:
            yield from _walk_refs(child)


def _resolve_json_pointer(document, ref: str):
    if ref == "#":
        return document
    if not ref.startswith("#/"):
        raise ValueError("chỉ hỗ trợ local $ref '#/...' trong contract canonical")
    current = document
    for raw in ref[2:].split("/"):
        key = raw.replace("~1", "/").replace("~0", "~")
        if isinstance(current, dict) and key in current:
            current = current[key]
        elif isinstance(current, list) and key.isdigit() and int(key) < len(current):
            current = current[int(key)]
        else:
            raise KeyError(key)
    return current


def _sql_parentheses_balanced(text: str) -> bool:
    depth = 0
    index = 0
    state = "normal"
    dollar_tag = ""
    while index < len(text):
        pair = text[index : index + 2]
        char = text[index]
        if state == "line_comment":
            if char == "\n":
                state = "normal"
        elif state == "block_comment":
            if pair == "*/":
                state = "normal"
                index += 1
        elif state == "single":
            if pair == "''":
                index += 1
            elif char == "'":
                state = "normal"
        elif state == "double":
            if pair == '""':
                index += 1
            elif char == '"':
                state = "normal"
        elif state == "dollar":
            if text.startswith(dollar_tag, index):
                state = "normal"
                index += len(dollar_tag) - 1
        else:
            if pair == "--":
                state = "line_comment"
                index += 1
            elif pair == "/*":
                state = "block_comment"
                index += 1
            elif char == "'":
                state = "single"
            elif char == '"':
                state = "double"
            elif char == "$":
                match = re.match(r"\$[A-Za-z_][A-Za-z0-9_]*\$|\$\$", text[index:])
                if match:
                    dollar_tag = match.group(0)
                    state = "dollar"
                    index += len(dollar_tag) - 1
            elif char == "(":
                depth += 1
            elif char == ")":
                depth -= 1
                if depth < 0:
                    return False
        index += 1
    return depth == 0 and state not in {"block_comment", "single", "double", "dollar"}


def _sql_create_table_columns(text: str) -> dict[str, set[str]]:
    """Extract ordinary column declarations from CREATE TABLE bodies."""
    tables: dict[str, set[str]] = {}
    create_re = re.compile(
        r"\bCREATE\s+TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?([A-Za-z_][\w.]*)\s*\(",
        re.IGNORECASE,
    )
    for match in create_re.finditer(text):
        start = match.end() - 1
        depth = 1
        index = start + 1
        state = "normal"
        body_end = None
        while index < len(text):
            pair = text[index : index + 2]
            char = text[index]
            if state == "line_comment":
                if char == "\n":
                    state = "normal"
            elif state == "block_comment":
                if pair == "*/":
                    state = "normal"
                    index += 1
            elif state == "single":
                if pair == "''":
                    index += 1
                elif char == "'":
                    state = "normal"
            elif state == "double":
                if pair == '""':
                    index += 1
                elif char == '"':
                    state = "normal"
            else:
                if pair == "--":
                    state = "line_comment"
                    index += 1
                elif pair == "/*":
                    state = "block_comment"
                    index += 1
                elif char == "'":
                    state = "single"
                elif char == '"':
                    state = "double"
                elif char == "(":
                    depth += 1
                elif char == ")":
                    depth -= 1
                    if depth == 0:
                        body_end = index
                        break
            index += 1
        if body_end is None:
            continue
        body = text[start + 1 : body_end]
        parts: list[str] = []
        part_start = 0
        depth = 0
        state = "normal"
        index = 0
        while index < len(body):
            pair = body[index : index + 2]
            char = body[index]
            if state == "single":
                if pair == "''":
                    index += 1
                elif char == "'":
                    state = "normal"
            elif state == "double":
                if pair == '""':
                    index += 1
                elif char == '"':
                    state = "normal"
            else:
                if char == "'":
                    state = "single"
                elif char == '"':
                    state = "double"
                elif char == "(":
                    depth += 1
                elif char == ")":
                    depth -= 1
                elif char == "," and depth == 0:
                    parts.append(body[part_start:index])
                    part_start = index + 1
            index += 1
        parts.append(body[part_start:])
        columns: set[str] = set()
        for part in parts:
            declaration = part.strip()
            identifier = re.match(r'^(?:"([^"]+)"|([A-Za-z_][\w]*))\s+', declaration)
            if not identifier:
                continue
            name = identifier.group(1) or identifier.group(2)
            if name.upper() in {"PRIMARY", "UNIQUE", "FOREIGN", "CHECK", "CONSTRAINT", "EXCLUDE"}:
                continue
            columns.add(name.lower())
        table_name = match.group(1).split(".")[-1].lower()
        tables[table_name] = columns
    return tables


def _parse_timestamp(value: object) -> datetime:
    if not isinstance(value, str) or not value.strip():
        raise ValueError("timestamp phải là chuỗi ISO-8601 không rỗng")
    normalized = value.strip()
    if normalized.endswith("Z"):
        normalized = normalized[:-1] + "+00:00"
    parsed = datetime.fromisoformat(normalized)
    if parsed.tzinfo is None:
        parsed = parsed.replace(tzinfo=timezone.utc)
    return parsed.astimezone(timezone.utc)


def _sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def _aws_sigv4_headers(
    url: str,
    access_key: str,
    secret_key: str,
    region: str,
    session_token: str | None = None,
) -> dict[str, str]:
    """Build a minimal SigV4 GET request accepted by S3-compatible MinIO."""
    now = datetime.now(timezone.utc)
    amz_date = now.strftime("%Y%m%dT%H%M%SZ")
    date_stamp = now.strftime("%Y%m%d")
    parsed = urlparse(url)
    canonical_uri = quote(parsed.path or "/", safe="/-_.~")
    canonical_query = parsed.query
    headers = {
        "host": parsed.netloc,
        "x-amz-content-sha256": hashlib.sha256(b"").hexdigest(),
        "x-amz-date": amz_date,
    }
    if session_token:
        headers["x-amz-security-token"] = session_token
    signed_names = ";".join(sorted(headers))
    canonical_headers = "".join(
        f"{name}:{headers[name].strip()}\n" for name in sorted(headers)
    )
    canonical_request = "\n".join(
        (
            "GET",
            canonical_uri,
            canonical_query,
            canonical_headers,
            signed_names,
            headers["x-amz-content-sha256"],
        )
    )
    scope = f"{date_stamp}/{region}/s3/aws4_request"
    string_to_sign = "\n".join(
        (
            "AWS4-HMAC-SHA256",
            amz_date,
            scope,
            hashlib.sha256(canonical_request.encode()).hexdigest(),
        )
    )

    def sign(key: bytes, message: str) -> bytes:
        return hmac.new(key, message.encode(), hashlib.sha256).digest()

    signing_key = sign(
        sign(sign(("AWS4" + secret_key).encode(), date_stamp), region), "s3"
    )
    signing_key = sign(signing_key, "aws4_request")
    signature = hmac.new(
        signing_key, string_to_sign.encode(), hashlib.sha256
    ).hexdigest()
    headers["authorization"] = (
        f"AWS4-HMAC-SHA256 Credential={access_key}/{scope}, "
        f"SignedHeaders={signed_names}, Signature={signature}"
    )
    return headers


def _download_sha256(url: str, headers: dict[str, str]) -> tuple[str, int]:
    digest = hashlib.sha256()
    size = 0
    with urlopen(Request(url, headers=headers, method="GET"), timeout=20) as response:
        for chunk in iter(lambda: response.read(1024 * 1024), b""):
            digest.update(chunk)
            size += len(chunk)
    return digest.hexdigest(), size


@dataclass(frozen=True)
class Finding:
    code: str
    severity: str
    path: str
    message: str
    entity_id: str | None = None


class Validator:
    def __init__(self, root: Path, mode: str):
        self.root = root.resolve()
        self.mode = mode
        self.findings: list[Finding] = []
        self.defined_ids: dict[str, Path] = {}
        self.entities: dict[str, dict] = {}
        self.pending_id_refs: list[tuple[str, Path, str]] = []
        self.record_validator: Draft202012Validator | None = None

    def add(
        self,
        code: str,
        severity: str,
        path: Path,
        message: str,
        entity_id: str | None = None,
    ):
        try:
            shown = str(path.resolve().relative_to(self.root))
        except ValueError:
            shown = str(path)
        self.findings.append(Finding(code, severity, shown, message, entity_id))

    def load_yaml(self, path: Path):
        try:
            return yaml.load(path.read_text(encoding="utf-8"), Loader=UniqueKeyLoader)
        except Exception as exc:
            self.add("YAML_INVALID", "error", path, str(exc))
            return None

    def validate_required_layout(self):
        required = [
            "00-index.md",
            "spec.yaml",
            "01-yeu-cau.md",
            "02-mo-hinh-yeu-cau.md",
            "03-du-lieu.md",
            "04-giao-dien.md",
            "governance/source-authority.md",
            "governance/orchestration.md",
            "governance/lifecycle.md",
            "governance/glossary.md",
            "governance/mobile-targeting-research.md",
            "governance/adrs/ADR-0001-source-authority.md",
            "governance/adrs/ADR-0002-go-modular-monolith.md",
            "governance/adrs/ADR-0003-content-compiler.md",
            "governance/adrs/ADR-0004-mobile-ux.md",
            "governance/adrs/ADR-0005-parity-gates.md",
            "governance/adrs/ADR-0006-postgresql-durability.md",
            "as-is/baseline.md",
            "as-is/claims.yaml",
            "as-is/evidence.yaml",
            "as-is/gaps.yaml",
            "as-is/contradictions.yaml",
            "registry/objectives.yaml",
            "registry/requirements.yaml",
            "registry/tests.yaml",
            "registry/designs.yaml",
            "registry/adrs.yaml",
            "registry/risks.yaml",
            "registry/migrations.yaml",
            "registry/debts.yaml",
            "registry/parity-items.yaml",
            "registry/gates.yaml",
            "registry/traceability.csv",
            "registry/catalogs/index.yaml",
            "registry/catalogs/source-snapshot.yaml",
            "registry/golden-manifest.yaml",
            "registry/test-results/index.yaml",
            "contracts/openapi/game.v1.yaml",
            "contracts/proto/game/v1/game.proto",
            "contracts/sql/game.v1.sql",
            "contracts/sql/game.v1.negative.sql",
            "contracts/content/manifest.v1.schema.json",
            "contracts/errors.md",
            "contracts/idempotency.md",
            "contracts/realtime-semantics.md",
            "contracts/versioning.md",
            "contracts/protobuf-fuzz.md",
            "schemas/cnpm-contract.yaml",
            "schemas/records.schema.yaml",
            "schemas/golden-manifest.schema.json",
            "schemas/test-result.schema.json",
            "delivery/case-matrices/skill-parity-p0.schema.json",
            "delivery/case-matrices/skill-parity-case-p0.schema.json",
            "delivery/case-matrices/skill-parity-p0.json",
            "delivery/case-matrices/skill-static-selection.yaml",
            "delivery/fixtures/training-npcs.p0.json",
            "delivery/fixtures/training-npcs.p0.schema.json",
            "domains/server-runtime/README.md",
            "domains/server-runtime/postgresql-data-dictionary.md",
            "domains/ui-hud-panels/README.md",
            "domains/content-assets-avatar-audio/README.md",
            "delivery/acceptance-plan.md",
            "delivery/migration-plan.md",
            "delivery/release-plan.md",
            "delivery/spec-scope-manifest.yaml",
            "delivery/test-strategy.md",
        ]
        for rel in required:
            path = self.root / rel
            if not path.is_file():
                self.add("FILE_MISSING", "error", path, "Thiếu artifact bắt buộc")

    @staticmethod
    def _normalize_markdown_cell(value: str) -> str:
        value = unicodedata.normalize("NFC", value)
        value = re.sub(r"</?br\s*/?>", " ", value, flags=re.IGNORECASE)
        value = value.replace("**", "").replace("__", "").replace("`", "")
        return re.sub(r"\s+", " ", value).strip()

    @classmethod
    def _markdown_structure(cls, text: str):
        parser = MarkdownIt("commonmark").enable("table")
        tokens = parser.parse(text)
        headings: list[tuple[int, str]] = []
        tables: list[list[tuple[str, ...]]] = []
        table: list[tuple[str, ...]] | None = None
        row: list[str] | None = None
        pending_heading_level: int | None = None
        for token in tokens:
            if token.type == "heading_open":
                pending_heading_level = int(token.tag[1:])
            elif token.type == "inline" and pending_heading_level is not None:
                headings.append(
                    (pending_heading_level, cls._normalize_markdown_cell(token.content))
                )
                pending_heading_level = None
            elif token.type == "table_open":
                table = []
            elif token.type == "tr_open" and table is not None:
                row = []
            elif token.type == "inline" and row is not None:
                row.append(cls._normalize_markdown_cell(token.content))
            elif token.type == "tr_close" and table is not None and row is not None:
                table.append(tuple(row))
                row = None
            elif token.type == "table_close" and table is not None:
                tables.append(table)
                table = None
        return headings, tables

    @staticmethod
    def _row_matches(actual: tuple[str, ...], expected: tuple[str, ...]) -> bool:
        if len(actual) != len(expected):
            return False
        return all(
            want == "*" and bool(got) or want == got
            for got, want in zip(actual, expected)
        )

    def validate_cnpm(self):
        contract_path = self.root / "schemas/cnpm-contract.yaml"
        contract = self.load_yaml(contract_path)
        if not contract:
            return
        declared = contract.get("templates", {})
        for filename in CNPM_HEADINGS:
            path = self.root / filename
            if not path.is_file():
                continue
            if filename not in declared:
                self.add(
                    "CNPM_CONTRACT_MISSING",
                    "error",
                    contract_path,
                    f"Thiếu contract cho {filename}",
                )
            text = path.read_text(encoding="utf-8")
            headings, tables = self._markdown_structure(text)
            expected_headings = CNPM_HEADINGS[filename]
            cursor = 0
            for expected in expected_headings:
                try:
                    found = headings.index(expected, cursor)
                except ValueError:
                    same_title = [
                        level for level, title in headings if title == expected[1]
                    ]
                    detail = f"; level hiện có: {same_title}" if same_title else ""
                    self.add(
                        "CNPM_HEADING_SIGNATURE",
                        "error",
                        path,
                        f"Thiếu/sai thứ tự heading H{expected[0]}: {expected[1]}{detail}",
                    )
                else:
                    cursor = found + 1

            expected_counts = Counter(expected_headings)
            actual_counts = Counter(headings)
            for signature, count in expected_counts.items():
                if actual_counts[signature] > count:
                    self.add(
                        "CNPM_HEADING_DUPLICATE",
                        "error",
                        path,
                        f"Heading tĩnh H{signature[0]} '{signature[1]}' xuất hiện "
                        f"{actual_counts[signature]} lần, template cho phép {count}",
                    )

            rows = [row for table in tables for row in table]
            for signature, minimum in CNPM_TABLE_SIGNATURES[filename]:
                matches = sum(self._row_matches(row, signature) for row in rows)
                if matches < minimum:
                    rendered = " | ".join(signature)
                    normalized_text = self._normalize_markdown_cell(text)
                    fixed_cells = [cell for cell in signature if cell not in {"", "*"}]
                    parse_hint = ""
                    if fixed_cells and all(
                        cell in normalized_text for cell in fixed_cells
                    ):
                        parse_hint = "; text header có mặt nhưng table không parse, kiểm tra số cột separator"
                    self.add(
                        "CNPM_TABLE_SIGNATURE",
                        "error",
                        path,
                        f"Thiếu header {minimum}x ({rendered}); parser chỉ thấy {matches}{parse_hint}",
                    )
            for marker in BAD_TEMPLATE_MARKERS:
                if marker in text:
                    self.add(
                        "CNPM_TEMPLATE_MARKER",
                        "error",
                        path,
                        f"Còn marker template: {marker}",
                    )

    def validate_yaml_and_ids(self):
        record_schema_path = self.root / "schemas/records.schema.yaml"
        if record_schema_path.is_file():
            record_schema = self.load_yaml(record_schema_path)
            if isinstance(record_schema, dict):
                try:
                    Draft202012Validator.check_schema(record_schema)
                    self.record_validator = Draft202012Validator(record_schema)
                except SchemaError as exc:
                    self.add("RECORD_SCHEMA_INVALID", "error", record_schema_path, str(exc))
        for path in sorted(self.root.rglob("*.yaml")):
            data = self.load_yaml(path)
            if data is None:
                continue
            self._walk_ids(data, path)
        for source_id, path, target_id in self.pending_id_refs:
            if target_id not in self.defined_ids:
                self.add(
                    "METADATA_EVIDENCE_REF",
                    "error",
                    path,
                    f"acceptance_evidence tham chiếu ID chưa định nghĩa: {target_id}",
                    source_id,
                )

        self.validate_lifecycle_policy()
        self.validate_evidence_ledger()
        self.validate_parity_done_evidence()
        self.validate_markdown_id_references()

    def validate_markdown_id_references(self):
        for path in sorted(self.root.rglob("*.md")):
            for line_number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
                for reference in MARKDOWN_ID_REF_RE.findall(line):
                    if reference not in self.defined_ids:
                        self.add(
                            "MARKDOWN_ID_REF",
                            "error",
                            path,
                            f"Dòng {line_number} tham chiếu stable ID chưa định nghĩa: {reference}",
                            reference,
                        )

    def validate_parity_done_evidence(self):
        result_path = self.root / "registry/test-results/index.yaml"
        result_doc = self.load_yaml(result_path) if result_path.is_file() else {}
        result_doc = result_doc or {}
        results = {
            item.get("test_id"): item
            for item in result_doc.get("results", [])
            if isinstance(item, dict) and isinstance(item.get("test_id"), str)
        }
        for entity_id, entity in self.entities.items():
            if not entity_id.startswith("PAR-") or entity.get("lifecycle") != "PARITY_DONE":
                continue
            tests = entity.get("tests") if isinstance(entity.get("tests"), list) else []
            golden_ids = entity.get("golden_ids") if isinstance(entity.get("golden_ids"), list) else []
            signoff = entity.get("reviewer_signoff") if isinstance(entity.get("reviewer_signoff"), dict) else {}
            if not tests or not golden_ids or not signoff:
                self.add("PARITY_DONE_EVIDENCE", "error", self.defined_ids[entity_id], "PARITY_DONE cần tests, golden_ids và reviewer_signoff", entity_id)
                continue
            if signoff.get("reviewer") != entity.get("reviewer"):
                self.add("PARITY_DONE_SIGNOFF_REVIEWER", "error", self.defined_ids[entity_id], "Reviewer sign-off phải khớp reviewer governed", entity_id)
            try:
                _parse_timestamp(signoff.get("signed_at"))
            except (TypeError, ValueError) as exc:
                self.add("PARITY_DONE_SIGNOFF_TIME", "error", self.defined_ids[entity_id], str(exc), entity_id)
            revision = signoff.get("revision")
            if not isinstance(revision, str) or not REVISION_RE.fullmatch(revision):
                self.add("PARITY_DONE_SIGNOFF_REVISION", "error", self.defined_ids[entity_id], "Sign-off revision không được pin", entity_id)
            evidence_path = Path(str(signoff.get("evidence_path", "")))
            if not evidence_path.is_absolute():
                evidence_path = self.root / evidence_path
            digest = signoff.get("sha256")
            if not evidence_path.is_file() or not isinstance(digest, str) or not SHA256_RE.fullmatch(digest) or _sha256_file(evidence_path) != digest:
                self.add("PARITY_DONE_SIGNOFF_ARTIFACT", "error", self.defined_ids[entity_id], "Artifact sign-off thiếu hoặc SHA-256 không khớp", entity_id)
            for golden_id in golden_ids:
                golden = self.entities.get(golden_id)
                if not isinstance(golden, dict) or golden.get("status") not in {"APPROVED", "COMPLETED"}:
                    self.add("PARITY_DONE_GOLDEN", "error", self.defined_ids[entity_id], f"Golden chưa APPROVED/COMPLETED: {golden_id}", entity_id)
                    continue
                artifact = golden.get("artifact") if isinstance(golden.get("artifact"), dict) else {}
                if revision and artifact.get("source_revision") != revision:
                    self.add(
                        "PARITY_DONE_GOLDEN_REVISION",
                        "error",
                        self.defined_ids[entity_id],
                        f"Golden khác revision sign-off: {golden_id}",
                        entity_id,
                    )
            for test_id in tests:
                test = self.entities.get(test_id)
                result = results.get(test_id)
                if not isinstance(test, dict) or test.get("status") != "PASS" or not isinstance(result, dict) or result.get("status") != "PASS":
                    self.add("PARITY_DONE_TEST", "error", self.defined_ids[entity_id], f"Test chưa có registry + result PASS: {test_id}", entity_id)
                    continue
                if revision and result.get("revision") != revision:
                    self.add("PARITY_DONE_REVISION_MISMATCH", "error", self.defined_ids[entity_id], f"Test result khác revision sign-off: {test_id}", entity_id)
                result_goldens = result.get("golden_ids") if isinstance(result.get("golden_ids"), list) else []
                if not set(golden_ids).issubset(result_goldens):
                    self.add("PARITY_DONE_TEST_GOLDEN", "error", self.defined_ids[entity_id], f"Test result thiếu golden của parity item: {test_id}", entity_id)

    def _walk_ids(self, value, path: Path):
        if isinstance(value, dict):
            entity_id = value.get("id")
            if isinstance(entity_id, str):
                if not ID_RE.fullmatch(entity_id):
                    self.add("ID_FORMAT", "error", path, f"ID không hợp lệ: {entity_id}", entity_id)
                elif entity_id in self.defined_ids:
                    self.add("ID_DUPLICATE", "error", path, f"ID đã định nghĩa tại {self.defined_ids[entity_id]}", entity_id)
                else:
                    self.defined_ids[entity_id] = path
                    self.entities[entity_id] = value
                if entity_id.startswith(GOVERNED_PREFIXES):
                    if self.record_validator is not None:
                        for error in sorted(self.record_validator.iter_errors(value), key=lambda item: list(item.absolute_path)):
                            location = "/".join(str(part) for part in error.absolute_path)
                            self.add("RECORD_INSTANCE_INVALID", "error", path, f"{location}: {error.message}", entity_id)
                    missing = sorted(field for field in GOVERNANCE_FIELDS if field not in value or value.get(field) in (None, "", []))
                    if missing:
                        self.add("METADATA_MISSING", "error", path, f"Stable ID thiếu metadata governance: {', '.join(missing)}", entity_id)
                    status = value.get("status")
                    status_values = next((allowed for prefix, allowed in STATUS_BY_PREFIX.items() if entity_id.startswith(prefix)), GOVERNANCE_STATUS_VALUES)
                    if status is not None and status not in status_values:
                        self.add("STATUS_ENUM", "error", path, f"status không thuộc enum governance: {status!r}", entity_id)
                    acceptance = value.get("acceptance_evidence")
                    if acceptance not in (None, "") and not isinstance(acceptance, list):
                        self.add("METADATA_EVIDENCE_TYPE", "error", path, "acceptance_evidence phải là list không rỗng", entity_id)
                    elif isinstance(acceptance, list):
                        for item in acceptance:
                            if not isinstance(item, str) or not item.strip():
                                self.add("METADATA_EVIDENCE_ITEM", "error", path, "acceptance_evidence chỉ chứa chuỗi không rỗng", entity_id)
                            elif ID_RE.fullmatch(item):
                                self.pending_id_refs.append((entity_id, path, item))
                            elif "/" in item or Path(item).suffix:
                                candidate = Path(item.split("#", 1)[0])
                                if not candidate.is_absolute():
                                    candidate = self.root / candidate
                                if not candidate.exists():
                                    self.add("METADATA_EVIDENCE_PATH", "error", path, f"acceptance_evidence path không tồn tại: {item}", entity_id)
                            elif item.startswith("sha256:") and not SHA256_RE.fullmatch(item.removeprefix("sha256:")):
                                self.add("METADATA_EVIDENCE_HASH", "error", path, f"acceptance_evidence hash không hợp lệ: {item}", entity_id)
                            elif not item.startswith("BLOCKER:") and not item.startswith("sha256:"):
                                self.add("METADATA_EVIDENCE_ITEM", "error", path, "acceptance_evidence phải là stable ID, path, sha256 hoặc BLOCKER", entity_id)
                lifecycle = value.get("lifecycle")
                if lifecycle is not None and lifecycle not in LIFECYCLE_VALUES:
                    self.add("LIFECYCLE_ENUM", "error", path, f"lifecycle không hợp lệ: {lifecycle!r}", entity_id)
                verification = value.get("verification")
                if verification is not None and verification not in VERIFICATION_VALUES:
                    self.add("VERIFICATION_ENUM", "error", path, f"verification không hợp lệ: {verification!r}", entity_id)
                if lifecycle == "PARITY_DONE":
                    missing = [key for key in ("reviewer", "acceptance_evidence", "tests") if not value.get(key)]
                    if missing:
                        self.add("FALSE_PARITY_DONE", "error", path, f"Thiếu gate: {', '.join(missing)}", entity_id)
            for child in value.values():
                self._walk_ids(child, path)
        elif isinstance(value, list):
            for child in value:
                self._walk_ids(child, path)

    def validate_unmarked_placeholders(self):
        for path in sorted(self.root.rglob("*.md")):
            in_fence = False
            for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
                if re.match(r"^\s*(?:```|~~~)", line):
                    in_fence = not in_fence
                    continue
                if not in_fence and re.search(r"\bTBD\b", line, flags=re.IGNORECASE):
                    if "[CẦN XÁC NHẬN]" not in line:
                        self.add(
                            "UNMARKED_PLACEHOLDER",
                            "error",
                            path,
                            f"Dòng {number} còn TBD nhưng thiếu [CẦN XÁC NHẬN], owner và exit criteria",
                        )

    def validate_lifecycle_policy(self):
        spec_path = self.root / "spec.yaml"
        if not spec_path.is_file():
            return
        spec = self.load_yaml(spec_path) or {}
        policy_ref = spec.get("lifecycle_policy")
        if not isinstance(policy_ref, str) or not policy_ref:
            self.add(
                "LIFECYCLE_POLICY_REF",
                "error",
                spec_path,
                "spec.yaml thiếu lifecycle_policy",
            )
            return
        policy_path = self.root / policy_ref
        if not policy_path.is_file():
            self.add(
                "LIFECYCLE_POLICY_REF",
                "error",
                spec_path,
                f"lifecycle_policy không tồn tại: {policy_ref}",
            )
            return
        policy_text = policy_path.read_text(encoding="utf-8")
        missing = sorted(
            value
            for value in LIFECYCLE_VALUES
            if f"`{value}`" not in policy_text and value not in policy_text
        )
        if missing:
            self.add(
                "LIFECYCLE_POLICY_ENUM",
                "error",
                policy_path,
                f"Policy chưa định nghĩa enum: {', '.join(missing)}",
            )

    def validate_evidence_ledger(self):
        path = self.root / "as-is/evidence.yaml"
        if not path.is_file():
            return
        evidence_ids = sorted(
            entity_id for entity_id in self.entities if entity_id.startswith("EVID-")
        )
        required = {
            "kind",
            "path",
            "sha256",
            "revision",
            "captured_at",
            "fresh_until",
            "claim_limit",
        }
        now = datetime.now(timezone.utc)
        for entity_id in evidence_ids:
            item = self.entities[entity_id]
            missing = sorted(
                field for field in required if item.get(field) in (None, "")
            )
            if missing:
                self.add(
                    "EVIDENCE_FIELDS",
                    "error",
                    path,
                    f"Evidence thiếu: {', '.join(missing)}",
                    entity_id,
                )
                continue
            digest = item.get("sha256")
            if not isinstance(digest, str) or not SHA256_RE.fullmatch(digest):
                self.add(
                    "EVIDENCE_HASH_FORMAT",
                    "error",
                    path,
                    "sha256 phải là 64 hex lowercase",
                    entity_id,
                )
            revision = item.get("revision")
            if not isinstance(revision, str) or not REVISION_RE.fullmatch(revision):
                self.add(
                    "EVIDENCE_REVISION",
                    "error",
                    path,
                    "revision phải có dạng source:<40|64 hex>[+dirty]",
                    entity_id,
                )
            source = Path(str(item["path"]))
            if not source.is_absolute():
                source = self.root / source
            if not source.is_file():
                self.add(
                    "EVIDENCE_PATH",
                    "error",
                    path,
                    f"Evidence path không phải file: {item['path']}",
                    entity_id,
                )
            elif isinstance(digest, str) and SHA256_RE.fullmatch(digest):
                if _sha256_file(source) != digest:
                    self.add(
                        "EVIDENCE_HASH",
                        "error",
                        path,
                        "SHA-256 evidence không khớp file hiện tại",
                        entity_id,
                    )
            try:
                captured_at = _parse_timestamp(item["captured_at"])
                fresh_until = _parse_timestamp(item["fresh_until"])
                if fresh_until < captured_at:
                    raise ValueError("fresh_until phải bằng hoặc sau captured_at")
            except (TypeError, ValueError) as exc:
                self.add("EVIDENCE_FRESHNESS", "error", path, str(exc), entity_id)
                continue
            if self.mode == "release" and fresh_until < now:
                self.add(
                    "EVIDENCE_STALE",
                    "error",
                    path,
                    f"Evidence hết freshness từ {item['fresh_until']}",
                    entity_id,
                )

    def validate_links(self):
        for path in sorted(self.root.rglob("*.md")):
            text = path.read_text(encoding="utf-8")
            for target in MD_LINK_RE.findall(text):
                if target.startswith(("http://", "https://", "#", "mailto:")):
                    continue
                raw = target.split("#", 1)[0].strip("<>")
                if not raw:
                    continue
                resolved = (path.parent / raw).resolve()
                if not resolved.exists():
                    self.add(
                        "LINK_DANGLING", "error", path, f"Link không tồn tại: {target}"
                    )

    def validate_traceability(self):
        path = self.root / "registry/traceability.csv"
        if not path.is_file():
            return
        try:
            with path.open(encoding="utf-8", newline="") as handle:
                rows = list(csv.DictReader(handle))
        except Exception as exc:
            self.add("CSV_INVALID", "error", path, str(exc))
            return
        expected = {"source_id", "relation", "target_id"}
        columns = set(rows[0].keys() if rows else set())
        if not rows or not expected.issubset(columns):
            self.add(
                "TRACE_COLUMNS",
                "error",
                path,
                "Thiếu cột source_id, relation, target_id hoặc không có edge",
            )
            return
        known = set(self.defined_ids)
        edges: set[tuple[str, str, str]] = set()
        adjacency: dict[str, set[str]] = {}
        for number, row in enumerate(rows, start=2):
            source_id = (row.get("source_id") or "").strip()
            relation = (row.get("relation") or "").strip()
            target_id = (row.get("target_id") or "").strip()
            if not source_id or not relation or not target_id:
                self.add(
                    "TRACE_EMPTY",
                    "error",
                    path,
                    f"Dòng {number}: edge thiếu source/relation/target",
                )
                continue
            edge = (source_id, relation, target_id)
            if edge in edges:
                self.add(
                    "TRACE_DUPLICATE",
                    "error",
                    path,
                    f"Dòng {number}: edge trùng {edge}",
                )
            edges.add(edge)
            adjacency.setdefault(source_id, set()).add(target_id)
            for key in ("source_id", "target_id"):
                entity_id = row.get(key, "")
                if entity_id and entity_id not in known:
                    self.add(
                        "TRACE_ORPHAN",
                        "error",
                        path,
                        f"Dòng {number}: {entity_id} chưa được định nghĩa",
                        entity_id,
                    )
            relation_contract = TRACE_RELATIONS.get(relation)
            if relation_contract is None:
                self.add(
                    "TRACE_RELATION_UNKNOWN",
                    "error",
                    path,
                    f"Dòng {number}: relation không thuộc allowlist: {relation!r}",
                    source_id,
                )
            else:
                source_prefixes, target_prefixes = relation_contract
                if not source_id.startswith(source_prefixes):
                    self.add(
                        "TRACE_RELATION_SOURCE",
                        "error",
                        path,
                        f"Dòng {number}: {relation} không cho source {source_id}",
                        source_id,
                    )
                if not target_id.startswith(target_prefixes):
                    self.add(
                        "TRACE_RELATION_TARGET",
                        "error",
                        path,
                        f"Dòng {number}: {relation} không cho target {target_id}",
                        source_id,
                    )
            if source_id == target_id:
                self.add(
                    "TRACE_SELF_CYCLE",
                    "error",
                    path,
                    f"Dòng {number}: self-edge {source_id}",
                    source_id,
                )

        self._validate_trace_cycles(path, adjacency)

        if self.mode in {"premerge", "release"}:
            objectives = sorted(item for item in known if item.startswith("OBJ-"))
            requirements = sorted(
                item for item in known if item.startswith(("FR-", "NFR-"))
            )
            tests = sorted(item for item in known if item.startswith("TEST-"))
            for requirement in requirements:
                relations = {
                    relation for source, relation, _ in edges if source == requirement
                }
                if not relations.intersection({"decided_by", "designed_by"}):
                    self.add(
                        "TRACE_REQUIREMENT_DESIGN",
                        "error",
                        path,
                        "Requirement thiếu decided_by ADR hoặc designed_by DOM",
                        requirement,
                    )
                if "verified_by" not in relations:
                    self.add(
                        "TRACE_REQUIREMENT_TEST",
                        "error",
                        path,
                        "Requirement thiếu verified_by TEST",
                        requirement,
                    )
                if not any(
                    relation == "has_requirement" and target == requirement
                    for _, relation, target in edges
                ):
                    self.add(
                        "TRACE_REQUIREMENT_OBJECTIVE",
                        "error",
                        path,
                        "Requirement thiếu OBJ has_requirement",
                        requirement,
                    )
                if not self._trace_reaches(requirement, adjacency, "GATE-"):
                    self.add(
                        "TRACE_REQUIREMENT_GATE_REACHABILITY",
                        "error",
                        path,
                        "Requirement không reach được release gate",
                        requirement,
                    )
            for test_id in tests:
                if not any(
                    source == test_id and relation == "gated_by"
                    for source, relation, _ in edges
                ):
                    self.add(
                        "TRACE_TEST_GATE",
                        "error",
                        path,
                        "Test thiếu gated_by GATE",
                        test_id,
                    )
            for objective in objectives:
                if not any(
                    source == objective and relation == "has_requirement"
                    for source, relation, _ in edges
                ):
                    self.add(
                        "TRACE_OBJECTIVE_REQUIREMENT",
                        "error",
                        path,
                        "Objective thiếu requirement",
                        objective,
                    )
                elif not self._trace_reaches(objective, adjacency, "GATE-"):
                    self.add(
                        "TRACE_OBJECTIVE_GATE_REACHABILITY",
                        "error",
                        path,
                        "Objective không reach được release gate",
                        objective,
                    )
            for parity_id in sorted(item for item in known if item.startswith("PAR-")):
                parity_relations = {
                    relation for source, relation, _ in edges if source == parity_id
                }
                for relation in ("verifies", "verified_by", "requires_golden"):
                    if relation not in parity_relations:
                        self.add(
                            "TRACE_PARITY_CLOSURE",
                            "error",
                            path,
                            f"Parity item thiếu {relation}",
                            parity_id,
                        )
                if not self._trace_reaches(parity_id, adjacency, "GATE-"):
                    self.add(
                        "TRACE_PARITY_GATE",
                        "error",
                        path,
                        "Parity item không reach release gate",
                        parity_id,
                    )
            for contradiction_id in sorted(
                item for item in known if item.startswith("CON-")
            ):
                contradiction = self.entities.get(contradiction_id, {})
                if contradiction.get("status") == "RESOLVED" and not any(
                    source == contradiction_id and relation == "resolved_by"
                    for source, relation, _ in edges
                ):
                    self.add(
                        "TRACE_CONTRADICTION_RESOLUTION",
                        "error",
                        path,
                        "CON RESOLVED thiếu resolved_by",
                        contradiction_id,
                    )
            self._validate_brownfield_trace(path, edges, adjacency)

    @staticmethod
    def _trace_reaches(
        source: str, adjacency: dict[str, set[str]], prefix: str
    ) -> bool:
        pending = [source]
        visited = {source}
        while pending:
            current = pending.pop()
            for target in adjacency.get(current, set()):
                if target.startswith(prefix):
                    return True
                if target not in visited:
                    visited.add(target)
                    pending.append(target)
        return False

    def _validate_trace_cycles(self, path: Path, adjacency: dict[str, set[str]]):
        state: dict[str, int] = {}
        stack: list[str] = []
        reported: set[tuple[str, ...]] = set()

        def visit(node: str):
            state[node] = 1
            stack.append(node)
            for target in adjacency.get(node, set()):
                if state.get(target, 0) == 0:
                    visit(target)
                elif state.get(target) == 1:
                    start = stack.index(target)
                    cycle = tuple(stack[start:] + [target])
                    normalized = tuple(sorted(set(cycle)))
                    if normalized not in reported:
                        reported.add(normalized)
                        self.add(
                            "TRACE_CYCLE",
                            "error",
                            path,
                            f"Chu trình trace: {' -> '.join(cycle)}",
                            target,
                        )
            stack.pop()
            state[node] = 2

        for node in sorted(adjacency):
            if state.get(node, 0) == 0:
                visit(node)

    def _validate_brownfield_trace(
        self,
        path: Path,
        edges: set[tuple[str, str, str]],
        adjacency: dict[str, set[str]],
    ):
        known = set(self.defined_ids)
        for claim_id in sorted(item for item in known if item.startswith("CLAIM-")):
            if not any(
                source == claim_id and relation == "supported_by"
                for source, relation, _ in edges
            ):
                self.add(
                    "TRACE_CLAIM_EVIDENCE",
                    "error",
                    path,
                    "Claim thiếu supported_by EVID",
                    claim_id,
                )
        brownfield = sorted(
            item for item in known if item.startswith(("GAP-", "RISK-", "DEBT-"))
        )
        for item_id in brownfield:
            has_origin = any(
                target == item_id
                and relation == "reveals"
                or source == item_id
                and relation == "supported_by"
                for source, relation, target in edges
            )
            if not has_origin:
                self.add(
                    "TRACE_BROWNFIELD_EVIDENCE",
                    "error",
                    path,
                    "Gap/risk/debt thiếu claim/evidence origin",
                    item_id,
                )
            addressed = [
                target
                for source, relation, target in edges
                if source == item_id and relation == "addressed_by"
            ]
            addressed.extend(
                source
                for source, relation, target in edges
                if target == item_id and relation == "addresses"
            )
            status = self.entities.get(item_id, {}).get("status")
            if not addressed and status not in {
                "DEFERRED",
                "OUT_OF_SCOPE",
                "SUPERSEDED",
                "RETIRED",
            }:
                self.add(
                    "TRACE_BROWNFIELD_REQUIREMENT",
                    "error",
                    path,
                    "Gap/risk/debt chưa nối FR/NFR hoặc disposition",
                    item_id,
                )
            elif addressed and not any(
                self._trace_reaches(req, adjacency, "GATE-") for req in addressed
            ):
                self.add(
                    "TRACE_BROWNFIELD_GATE",
                    "error",
                    path,
                    "Brownfield chain không reach release gate",
                    item_id,
                )
        for migration in sorted(item for item in known if item.startswith("MIG-")):
            if not any(
                source == migration and relation == "verified_by"
                for source, relation, _ in edges
            ):
                self.add(
                    "TRACE_MIGRATION_TEST",
                    "error",
                    path,
                    "Migration thiếu verified_by TEST",
                    migration,
                )
            if not self._trace_reaches(migration, adjacency, "GATE-"):
                self.add(
                    "TRACE_MIGRATION_GATE",
                    "error",
                    path,
                    "Migration không reach release gate",
                    migration,
                )

    def validate_catalogs(self):
        index_path = self.root / "registry/catalogs/index.yaml"
        if not index_path.is_file():
            return
        index = self.load_yaml(index_path) or {}
        owner_registry = index.get("owner_registry")
        if not isinstance(owner_registry, dict) or not owner_registry:
            self.add("CATALOG_OWNER_REGISTRY", "error", index_path, "Thiếu owner_registry")
            owner_registry = {}
        else:
            for owner, design_id in owner_registry.items():
                if not isinstance(owner, str) or not owner or not isinstance(design_id, str) or design_id not in self.defined_ids:
                    self.add("CATALOG_OWNER_REGISTRY", "error", index_path, f"Owner mapping không hợp lệ: {owner!r} -> {design_id!r}")
        required = {
            "catalog_id",
            "entity_type",
            "source_path",
            "source_sha256",
            "owner_domain",
            "lifecycle",
            "verification",
            "disposition",
        }
        entries = index.get("catalogs")
        if not isinstance(entries, list) or not entries:
            self.add(
                "CATALOG_INDEX", "error", index_path, "catalogs phải là list không rỗng"
            )
            return
        seen_paths: set[str] = set()
        seen_ids: dict[tuple[str, str], tuple[Path, int]] = {}
        counts: Counter[str] = Counter()
        owned_counts: Counter[str] = Counter()
        disposition_counts: Counter[str] = Counter()
        for entry_number, entry in enumerate(entries, start=1):
            if not isinstance(entry, dict):
                self.add(
                    "CATALOG_INDEX_ENTRY",
                    "error",
                    index_path,
                    f"Catalog entry {entry_number} phải là mapping",
                )
                continue
            rel = entry.get("path")
            entity_type = entry.get("entity_type")
            if (
                not isinstance(rel, str)
                or not rel
                or Path(rel).is_absolute()
                or ".." in Path(rel).parts
            ):
                self.add(
                    "CATALOG_PATH",
                    "error",
                    index_path,
                    f"Catalog entry {entry_number} có path không an toàn",
                )
                continue
            if rel in seen_paths:
                self.add(
                    "CATALOG_INDEX_DUPLICATE",
                    "error",
                    index_path,
                    f"Catalog path trùng: {rel}",
                )
            seen_paths.add(rel)
            if entity_type not in CATALOG_ENTITY_TYPES:
                self.add(
                    "CATALOG_ENTITY_ENUM",
                    "error",
                    index_path,
                    f"entity_type index không hợp lệ: {entity_type!r}",
                )
            path = index_path.parent / rel
            if not path.is_file():
                self.add(
                    "CATALOG_MISSING",
                    "error",
                    path,
                    "Catalog trong index không tồn tại",
                )
                continue
            payload = path.read_bytes()
            actual_sha = hashlib.sha256(payload).hexdigest()
            declared_sha = entry.get("sha256")
            if not isinstance(declared_sha, str) or not SHA256_RE.fullmatch(
                declared_sha
            ):
                self.add(
                    "CATALOG_HASH_FORMAT",
                    "error",
                    path,
                    "SHA-256 index phải là 64 hex lowercase",
                )
            elif actual_sha != declared_sha:
                self.add("CATALOG_HASH", "error", path, "SHA-256 không khớp index")
            lines = payload.splitlines()
            declared_records = entry.get("records")
            if not isinstance(declared_records, int) or declared_records < 0:
                self.add(
                    "CATALOG_COUNT_TYPE", "error", path, "records phải là integer >= 0"
                )
            elif len(lines) != declared_records:
                self.add(
                    "CATALOG_COUNT", "error", path, "Record count không khớp index"
                )
            for number, raw in enumerate(lines, start=1):
                try:
                    record = json.loads(raw, object_pairs_hook=_json_unique_object)
                except Exception as exc:
                    self.add("CATALOG_JSON", "error", path, f"Dòng {number}: {exc}")
                    continue
                if not isinstance(record, dict):
                    self.add(
                        "CATALOG_RECORD_TYPE",
                        "error",
                        path,
                        f"Dòng {number}: record phải là object",
                    )
                    continue
                missing = sorted(required - set(record))
                if missing:
                    self.add(
                        "CATALOG_FIELDS",
                        "error",
                        path,
                        f"Dòng {number} thiếu: {', '.join(missing)}",
                    )
                    continue
                record_type = record.get("entity_type")
                if record_type not in CATALOG_ENTITY_TYPES:
                    self.add(
                        "CATALOG_ENTITY_ENUM",
                        "error",
                        path,
                        f"Dòng {number}: entity_type không hợp lệ {record_type!r}",
                    )
                elif record_type != entity_type:
                    self.add(
                        "CATALOG_ENTITY_MISMATCH",
                        "error",
                        path,
                        f"Dòng {number}: entity_type khác index {entity_type!r}",
                    )
                catalog_id = record.get("catalog_id")
                if not isinstance(catalog_id, str) or not catalog_id.strip():
                    self.add(
                        "CATALOG_ID",
                        "error",
                        path,
                        f"Dòng {number}: catalog_id phải là chuỗi không rỗng",
                    )
                else:
                    key = (str(record_type), catalog_id)
                    if key in seen_ids:
                        first_path, first_line = seen_ids[key]
                        self.add(
                            "CATALOG_ID_DUPLICATE",
                            "error",
                            path,
                            f"Dòng {number}: {key} trùng {first_path.name}:{first_line}",
                        )
                    else:
                        seen_ids[key] = (path, number)
                lifecycle = record.get("lifecycle")
                if lifecycle not in LIFECYCLE_VALUES:
                    self.add(
                        "CATALOG_LIFECYCLE_ENUM",
                        "error",
                        path,
                        f"Dòng {number}: lifecycle không hợp lệ {lifecycle!r}",
                    )
                verification = record.get("verification")
                if verification not in VERIFICATION_VALUES:
                    self.add(
                        "CATALOG_VERIFICATION_ENUM",
                        "error",
                        path,
                        f"Dòng {number}: verification không hợp lệ {verification!r}",
                    )
                disposition = record.get("disposition")
                if disposition not in CATALOG_DISPOSITIONS:
                    self.add(
                        "CATALOG_DISPOSITION_ENUM",
                        "error",
                        path,
                        f"Dòng {number}: disposition không hợp lệ {disposition!r}",
                    )
                source_sha = record.get("source_sha256")
                missing_package = (
                    record_type == "package"
                    and record.get("present") is False
                    and isinstance(record.get("blocker"), str)
                    and bool(record["blocker"].strip())
                )
                if missing_package and source_sha is not None:
                    self.add(
                        "CATALOG_SOURCE_HASH",
                        "error",
                        path,
                        f"Dòng {number}: package absent phải có source_sha256 null",
                    )
                elif not missing_package and (
                    not isinstance(source_sha, str)
                    or not SHA256_RE.fullmatch(source_sha)
                ):
                    self.add(
                        "CATALOG_SOURCE_HASH",
                        "error",
                        path,
                        f"Dòng {number}: source_sha256 không hợp lệ",
                    )
                if isinstance(record_type, str):
                    counts[record_type] += 1
                    owner_domain = record.get("owner_domain")
                    if isinstance(owner_domain, str) and owner_domain in owner_registry:
                        owned_counts[record_type] += 1
                    else:
                        self.add(
                            "CATALOG_OWNER_UNKNOWN",
                            "error",
                            path,
                            f"Dòng {number}: owner_domain chưa đăng ký {owner_domain!r}",
                        )
                    if disposition in CATALOG_DISPOSITIONS:
                        disposition_counts[record_type] += 1

        coverage = index.get("coverage")
        if not isinstance(coverage, dict):
            self.add(
                "CATALOG_COVERAGE_DECLARATION",
                "error",
                index_path,
                "Thiếu coverage mapping định lượng",
            )
            return
        for entity_type, actual in sorted(counts.items()):
            declaration = coverage.get(entity_type)
            if not isinstance(declaration, dict):
                self.add(
                    "CATALOG_COVERAGE_ENTITY",
                    "error",
                    index_path,
                    f"Thiếu coverage.{entity_type}",
                )
                continue
            expected_values = {
                "discovered": actual,
                "cataloged": actual,
                "owned": owned_counts[entity_type],
                "dispositioned": disposition_counts[entity_type],
            }
            for field, expected_value in expected_values.items():
                value = declaration.get(field)
                if (
                    not isinstance(value, int)
                    or isinstance(value, bool)
                    or value != expected_value
                ):
                    self.add(
                        "CATALOG_COVERAGE_COUNT",
                        "error",
                        index_path,
                        f"coverage.{entity_type}.{field}={value!r}, cần {expected_value}",
                    )
            if (
                owned_counts[entity_type] != actual
                or disposition_counts[entity_type] != actual
            ):
                self.add(
                    "CATALOG_COVERAGE_INCOMPLETE",
                    "error",
                    index_path,
                    f"{entity_type}: entity vô chủ/disposition chưa đủ",
                )
            unresolved = declaration.get("unresolved")
            if (
                not isinstance(unresolved, int)
                or isinstance(unresolved, bool)
                or unresolved < 0
            ):
                self.add(
                    "CATALOG_COVERAGE_UNRESOLVED",
                    "error",
                    index_path,
                    f"coverage.{entity_type}.unresolved phải là integer >= 0",
                )
            elif self.mode == "release" and unresolved != 0:
                self.add(
                    "CATALOG_RELEASE_UNRESOLVED",
                    "error",
                    index_path,
                    f"coverage.{entity_type}.unresolved phải bằng 0 khi release",
                )

    def validate_catalog_source_census(self):
        index_path = self.root / "registry/catalogs/index.yaml"
        snapshot_path = self.root / "registry/catalogs/source-snapshot.yaml"
        if not index_path.is_file() or not snapshot_path.is_file():
            return
        index = self.load_yaml(index_path) or {}
        snapshot = self.load_yaml(snapshot_path) or {}
        census = index.get("source_census")
        source_root_raw = snapshot.get("source_root")
        if not isinstance(census, dict) or not isinstance(source_root_raw, str):
            self.add("CATALOG_SOURCE_CENSUS", "error", index_path, "Thiếu source_census hoặc source_root")
            return
        source_root = Path(source_root_raw)
        client_settings = source_root / "bin/client/settings"
        client_ui = source_root / "bin/client/Ui"
        client_spr = source_root / "bin/client/Spr"
        client_music = source_root / "bin/client/music"
        package_ini = source_root / "bin/client/package.ini"
        server_settings = (source_root / "bin/Server/settings", source_root / "bin/Server/Server/settings")
        script_roots = (source_root / "bin/client/script", source_root / "bin/Server/Server/script", source_root / "bin/Server/script")

        def relative(path: Path) -> str:
            return path.relative_to(source_root).as_posix()

        def pathset(paths) -> tuple[int, str, set[str]]:
            values = sorted({relative(path) for path in paths}, key=os.fsencode)
            payload = b"".join(os.fsencode(value) + b"\n" for value in values)
            return len(values), hashlib.sha256(payload).hexdigest(), set(values)

        def table_rows(path: Path) -> int:
            lines = path.read_bytes().splitlines()
            return sum(1 for line in lines[1:] if line.strip())

        settings = sorted(path for root in (client_settings, *server_settings) if root.exists() for path in root.rglob("*") if path.is_file() and path.suffix.lower() in {".txt", ".ini"})
        ui_files = sorted(path for path in client_ui.rglob("*") if path.is_file() and path.suffix.lower() in {".ini", ".txt"}) if client_ui.exists() else []
        lua_files = [path for path in client_ui.rglob("*") if path.is_file() and path.suffix.lower() == ".lua"] if client_ui.exists() else []
        for root in script_roots:
            if root.exists():
                lua_files.extend(path for path in root.rglob("*") if path.is_file() and path.suffix.lower() == ".lua")
        lua_files = sorted(set(lua_files), key=lambda path: os.fsencode(relative(path)))
        spr_paths = [path for root in (client_ui, client_spr) if root.exists() for path in root.rglob("*") if path.is_file() and path.suffix.lower() == ".spr"]
        avatar_tokens = ("avatar", "face", "newplayer", "selplayer", "/npc/series/")
        avatar_spr = [path for path in spr_paths if any(token in f"/{relative(path).lower().replace(chr(92), '/')}/" for token in avatar_tokens)]
        avatar_tables = [path for path in client_settings.rglob("*") if path.is_file() and path.suffix.lower() in {".txt", ".ini"} and (path.stem.lower().endswith("res") or path.name.lower() in {"npcs.txt", "npcname.txt", "horse.txt"} or "/settings/npc/player/" in f"/{relative(path).lower().replace(chr(92), '/')}/")]
        audio_files = [path for path in client_music.rglob("*") if path.is_file() and path.suffix.lower() in {".mp3", ".wav", ".ogg", ".mid"}] if client_music.exists() else []

        table_inputs = {
            "skill": client_settings / "Skills.txt",
            "missile": client_settings / "Missles.txt",
            "npc": client_settings / "Npcs.txt",
            "goods": client_settings / "Goods.txt",
        }
        expected_counts: dict[str, int] = {}
        for entity, input_path in table_inputs.items():
            expected = {"records": table_rows(input_path), "path": relative(input_path), "source_sha256": _sha256_file(input_path)}
            expected_counts[entity] = expected["records"]
            declaration = census.get(entity)
            if not isinstance(declaration, dict) or any(declaration.get(key) != value for key, value in expected.items()):
                self.add("CATALOG_SOURCE_CENSUS_DRIFT", "error", index_path, f"source_census.{entity} không khớp input")
        map_path = client_settings / "MapList.ini"
        map_ids = {match.group(1) for match in re.finditer(rb"(?m)^([0-9]+)=", map_path.read_bytes())}
        map_expected = {"records": len(map_ids), "path": relative(map_path), "source_sha256": _sha256_file(map_path)}
        expected_counts["map"] = len(map_ids)
        if not isinstance(census.get("map"), dict) or any(census["map"].get(key) != value for key, value in map_expected.items()):
            self.add("CATALOG_SOURCE_CENSUS_DRIFT", "error", index_path, "source_census.map không khớp input")

        path_inputs = {
            "setting": settings,
            "uifile": ui_files,
            "lua": lua_files,
            "quest": lua_files,
            "sprite": spr_paths,
            "avatar": [*avatar_spr, *avatar_tables],
            "audio": audio_files,
        }
        input_pathsets: dict[str, set[str]] = {}
        for entity, paths in path_inputs.items():
            count, digest, values = pathset(paths)
            expected_counts[entity] = count
            input_pathsets[entity] = values
            declaration = census.get(entity)
            if not isinstance(declaration, dict) or declaration.get("files") != count or declaration.get("paths_sha256") != digest:
                self.add("CATALOG_SOURCE_CENSUS_DRIFT", "error", index_path, f"source_census.{entity} path set không khớp input")

        configured_packages = 0
        for raw in package_ini.read_text(encoding="ascii").splitlines():
            line = raw.strip()
            if line and not line.startswith((";", "#", "[")) and "=" in line and line.split("=", 1)[0].strip().isdigit():
                configured_packages += 1
        package_expected = {"records": configured_packages, "path": relative(package_ini), "source_sha256": _sha256_file(package_ini)}
        expected_counts["package"] = configured_packages
        if not isinstance(census.get("package"), dict) or any(census["package"].get(key) != value for key, value in package_expected.items()):
            self.add("CATALOG_SOURCE_CENSUS_DRIFT", "error", index_path, "source_census.package không khớp input")

        coverage = index.get("coverage") if isinstance(index.get("coverage"), dict) else {}
        for entity, expected in expected_counts.items():
            declaration = coverage.get(entity)
            if not isinstance(declaration, dict) or declaration.get("discovered") != expected or declaration.get("cataloged") != expected:
                self.add("CATALOG_SOURCE_COVERAGE", "error", index_path, f"coverage.{entity} không phủ source census {expected}")

        catalog_pathsets: dict[str, set[str]] = {name: set() for name in path_inputs}
        for entry in index.get("catalogs", []):
            if not isinstance(entry, dict) or not isinstance(entry.get("path"), str):
                continue
            catalog_path = index_path.parent / entry["path"]
            if not catalog_path.is_file():
                continue
            for raw in catalog_path.read_bytes().splitlines():
                try:
                    record = json.loads(raw)
                except (ValueError, json.JSONDecodeError):
                    continue
                record_type = record.get("entity_type")
                source_path = record.get("source_path")
                if isinstance(source_path, str) and record_type in catalog_pathsets:
                    catalog_pathsets[record_type].add(source_path)
                if isinstance(source_path, str) and record_type == "lua":
                    catalog_pathsets["quest"].add(source_path)
        for entity, expected_paths in input_pathsets.items():
            if catalog_pathsets.get(entity, set()) != expected_paths:
                self.add("CATALOG_SOURCE_PATH_COVERAGE", "error", index_path, f"Catalog {entity} không phủ đúng input path set")

    def validate_source_snapshot(self):
        path = self.root / "registry/catalogs/source-snapshot.yaml"
        if not path.is_file():
            return
        snapshot = self.load_yaml(path) or {}
        source_root_raw = snapshot.get("source_root")
        source_root = Path(source_root_raw) if isinstance(source_root_raw, str) else None
        for name in ("source_git", "vltktool"):
            state = snapshot.get(name)
            if not isinstance(state, dict):
                self.add("SOURCE_SNAPSHOT_STATE", "error", path, f"Thiếu state {name}")
                continue
            revision = state.get("revision")
            dirty = state.get("dirty")
            if not isinstance(revision, str) or not re.fullmatch(r"[0-9a-f]{40}", revision):
                self.add("SOURCE_SNAPSHOT_REVISION", "error", path, f"{name}.revision phải là Git SHA-1 40 hex")
            if not isinstance(dirty, bool):
                self.add("SOURCE_SNAPSHOT_DIRTY_TYPE", "error", path, f"{name}.dirty phải là boolean")
            elif self.mode == "release" and dirty:
                self.add("SOURCE_SNAPSHOT_DIRTY_RELEASE", "error", path, f"{name} dirty chặn release")
        generator = snapshot.get("generator")
        generator_path = Path(str(generator.get("path", ""))) if isinstance(generator, dict) else None
        generator_sha = generator.get("sha256") if isinstance(generator, dict) else None
        if generator_path is None or not generator_path.is_file() or not isinstance(generator_sha, str) or _sha256_file(generator_path) != generator_sha:
            self.add("SOURCE_SNAPSHOT_GENERATOR", "error", path, "Generator path/SHA-256 không khớp snapshot")
        if source_root is None or not source_root.is_dir():
            self.add("SOURCE_SNAPSHOT_ROOT", "error", path, "source_root không tồn tại")
            return
        source_state = snapshot.get("source_git") if isinstance(snapshot.get("source_git"), dict) else {}
        expected_revision = source_state.get("revision")
        if source_state.get("dirty") is True and isinstance(expected_revision, str):
            expected_revision += "+dirty"
        expected_revision = f"jx-pc:{expected_revision}" if isinstance(expected_revision, str) else None
        for entity_id, entity in self.entities.items():
            if not entity_id.startswith("EVID-") or entity.get("kind") != "source":
                continue
            evidence_path = Path(str(entity.get("path", "")))
            if evidence_path.is_relative_to(source_root) and entity.get("revision") != expected_revision:
                self.add(
                    "EVIDENCE_SNAPSHOT_REVISION",
                    "error",
                    self.defined_ids[entity_id],
                    f"Evidence dưới source_root phải pin {expected_revision}",
                    entity_id,
                )

    def validate_contract_declarations(self):
        spec_path = self.root / "spec.yaml"
        if not spec_path.is_file():
            return
        spec = self.load_yaml(spec_path) or {}
        declarations = spec.get("canonical_contracts")
        if not isinstance(declarations, dict):
            self.add("CONTRACT_DECLARATIONS", "error", spec_path, "canonical_contracts phải là mapping")
            return
        for name in ("rest", "realtime", "content", "data"):
            rel = declarations.get(name)
            if not isinstance(rel, str) or not rel:
                self.add("CONTRACT_DECLARATION_MISSING", "error", spec_path, f"Thiếu canonical_contracts.{name}")
                continue
            if not (self.root / rel).is_file():
                self.add("CONTRACT_DECLARATION_PATH", "error", spec_path, f"canonical_contracts.{name} trỏ tới file không tồn tại: {rel}")
        negative_rel = declarations.get("data_negative")
        if not isinstance(negative_rel, str) or not negative_rel:
            self.add("CONTRACT_DECLARATION_MISSING", "error", spec_path, "Thiếu canonical_contracts.data_negative")
        elif not (self.root / negative_rel).is_file():
            self.add("CONTRACT_DECLARATION_PATH", "error", spec_path, f"canonical_contracts.data_negative trỏ tới file không tồn tại: {negative_rel}")

    def validate_catalog_reproducibility(self):
        if self.mode != "release":
            return
        generator = Path(__file__).resolve().parents[1] / "generate-jx-spec-catalog.py"
        index_path = self.root / "registry/catalogs/index.yaml"
        if not generator.is_file():
            self.add("CATALOG_GENERATOR_MISSING", "error", generator, "Thiếu generator catalog bắt buộc cho release")
            return
        if not index_path.is_file():
            self.add("CATALOG_INDEX_MISSING", "error", index_path, "Thiếu catalog index để kiểm reproducibility")
            return
        with tempfile.TemporaryDirectory() as tmp:
            result = subprocess.run([sys.executable, str(generator), "--output", tmp], capture_output=True, text=True, check=False)
            if result.returncode != 0:
                self.add("CATALOG_REGEN_FAILED", "error", generator, (result.stderr or result.stdout or "generator failed").strip())
                return
            index = self.load_yaml(index_path) or {}
            expected = [item.get("path") for item in index.get("catalogs", []) if isinstance(item, dict) and isinstance(item.get("path"), str)] + ["index.yaml", "source-snapshot.yaml"]
            for rel in expected:
                committed = self.root / "registry/catalogs" / rel
                generated = Path(tmp) / rel
                if not committed.is_file() or not generated.is_file() or committed.read_bytes() != generated.read_bytes():
                    self.add("CATALOG_NOT_REPRODUCIBLE", "error", committed, f"Catalog tái sinh không khớp byte-for-byte: {rel}")

    def validate_openapi(self):
        path = self.root / "contracts/openapi/game.v1.yaml"
        if not path.is_file():
            return
        document = self.load_yaml(path)
        if not isinstance(document, dict):
            self.add("OPENAPI_INVALID", "error", path, "OpenAPI root phải là mapping")
            return
        version = document.get("openapi")
        if not isinstance(version, str) or not version.startswith("3.1."):
            self.add(
                "OPENAPI_VERSION", "error", path, f"Cần OpenAPI 3.1.x, nhận {version!r}"
            )
        info = document.get("info")
        if (
            not isinstance(info, dict)
            or not info.get("title")
            or not info.get("version")
        ):
            self.add(
                "OPENAPI_INFO", "error", path, "info.title và info.version là bắt buộc"
            )
        paths = document.get("paths")
        if not isinstance(paths, dict) or not paths:
            self.add("OPENAPI_PATHS", "error", path, "paths phải là mapping không rỗng")
            paths = {}
        operation_ids: dict[str, str] = {}
        for route, item in paths.items():
            if (
                not isinstance(route, str)
                or not route.startswith("/")
                or not isinstance(item, dict)
            ):
                self.add(
                    "OPENAPI_PATH_ITEM",
                    "error",
                    path,
                    f"Path item không hợp lệ: {route!r}",
                )
                continue
            for method, operation in item.items():
                if method not in HTTP_METHODS:
                    continue
                if not isinstance(operation, dict):
                    self.add(
                        "OPENAPI_OPERATION",
                        "error",
                        path,
                        f"{method.upper()} {route} phải là mapping",
                    )
                    continue
                operation_id = operation.get("operationId")
                if not isinstance(operation_id, str) or not operation_id:
                    self.add(
                        "OPENAPI_OPERATION_ID",
                        "error",
                        path,
                        f"{method.upper()} {route} thiếu operationId",
                    )
                elif operation_id in operation_ids:
                    self.add(
                        "OPENAPI_OPERATION_ID_DUPLICATE",
                        "error",
                        path,
                        f"operationId {operation_id} trùng giữa {operation_ids[operation_id]} và {method.upper()} {route}",
                    )
                else:
                    operation_ids[operation_id] = f"{method.upper()} {route}"
                responses = operation.get("responses")
                if not isinstance(responses, dict) or not responses:
                    self.add(
                        "OPENAPI_RESPONSES",
                        "error",
                        path,
                        f"{method.upper()} {route} thiếu responses",
                    )
        for ref in _walk_refs(document):
            try:
                _resolve_json_pointer(document, ref)
            except (KeyError, ValueError) as exc:
                self.add(
                    "OPENAPI_REF", "error", path, f"$ref không resolve: {ref} ({exc})"
                )

    def validate_proto(self):
        path = self.root / "contracts/proto/game/v1/game.proto"
        if not path.is_file():
            return
        protoc = shutil.which("protoc")
        if not protoc:
            self.add(
                "PROTO_TOOL_UNAVAILABLE",
                "error",
                path,
                "Không tìm thấy protoc; chưa thể compile contract",
            )
            return
        proto_root = self.root / "contracts/proto"
        with tempfile.TemporaryDirectory() as tmp:
            descriptor = Path(tmp) / "game.pb"
            result = subprocess.run(
                [
                    protoc,
                    f"--proto_path={proto_root}",
                    f"--descriptor_set_out={descriptor}",
                    "game/v1/game.proto",
                ],
                capture_output=True,
                text=True,
                check=False,
            )
        if result.returncode != 0:
            message = (result.stderr or result.stdout or "protoc failed").strip()
            self.add("PROTO_COMPILE", "error", path, message)

    def validate_sql_contract(self):
        path = self.root / "contracts/sql/game.v1.sql"
        if not path.is_file():
            return
        text = path.read_text(encoding="utf-8")
        upper = text.upper()
        leading_transaction = re.match(
            r"\s*(?:(?:--[^\n]*(?:\n|$))|(?:/\*.*?\*/\s*))*BEGIN\s*;",
            text,
            re.IGNORECASE | re.DOTALL,
        )
        if not leading_transaction:
            self.add("SQL_TRANSACTION", "error", path, "Schema phải bắt đầu bằng BEGIN;")
        if not re.search(r"COMMIT\s*;\s*$", text, re.IGNORECASE):
            self.add("SQL_TRANSACTION", "error", path, "Schema phải kết thúc bằng COMMIT;")
        if not _sql_parentheses_balanced(text):
            self.add("SQL_SYNTAX_STATIC", "error", path, "Ngoặc/comment/string SQL không cân bằng")
        tables = re.findall(
            r"\bCREATE\s+TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?([A-Za-z_][\w.]*)",
            text,
            re.IGNORECASE,
        )
        duplicates = sorted(
            name for name, count in Counter(name.lower() for name in tables).items() if count > 1
        )
        if duplicates:
            self.add("SQL_TABLE_DUPLICATE", "error", path, f"CREATE TABLE trùng: {', '.join(duplicates)}")
        if not tables:
            self.add("SQL_TABLES", "error", path, "Không có CREATE TABLE")
        if re.search(r"\b(?:DROP\s+TABLE|TRUNCATE)\b", upper):
            self.add("SQL_DESTRUCTIVE", "error", path, "Contract schema không được chứa DROP TABLE/TRUNCATE")
        if "[CẦN XÁC NHẬN]" in upper or re.search(r"\bTODO\b", upper):
            self.add("SQL_PLACEHOLDER", "error", path, "SQL contract còn placeholder/TODO")

    def validate_data_dictionary(self):
        sql_path = self.root / "contracts/sql/game.v1.sql"
        dictionary_path = self.root / "domains/server-runtime/postgresql-data-dictionary.md"
        if not sql_path.is_file() or not dictionary_path.is_file():
            return
        sql_tables = _sql_create_table_columns(sql_path.read_text(encoding="utf-8"))
        dictionary = dictionary_path.read_text(encoding="utf-8")
        coverage = re.search(r"DATA_DICTIONARY_COVERAGE:\s*tables=(\d+)\s+columns=(\d+)", dictionary)
        if not coverage:
            self.add("DATA_DICTIONARY_MARKER", "error", dictionary_path, "Thiếu marker DATA_DICTIONARY_COVERAGE")
        dictionary_hash = re.search(r"SHA-256 contract tại lần sinh\s*\|\s*`([0-9a-f]{64})`", dictionary)
        sql_hash = _sha256_file(sql_path)
        if not dictionary_hash or dictionary_hash.group(1) != sql_hash:
            self.add("DATA_DICTIONARY_HASH", "error", dictionary_path, "SHA-256 contract trong dictionary không khớp SQL")
        headings = {
            match.group(1).lower(): set()
            for match in re.finditer(r"^###\s+\d+\.\s+`(\w+)`\s*$", dictionary, re.MULTILINE)
        }
        current_table: str | None = None
        for line in dictionary.splitlines():
            heading = re.match(r"^###\s+\d+\.\s+`(\w+)`\s*$", line)
            if heading:
                current_table = heading.group(1).lower()
                continue
            column = re.match(r"^\|\s*\d+\s*\|\s*`(\w+)`\s*\|", line)
            if column and current_table in headings:
                headings[current_table].add(column.group(1).lower())
        if set(headings) != set(sql_tables):
            self.add(
                "DATA_DICTIONARY_TABLE_COVERAGE",
                "error",
                dictionary_path,
                f"Dictionary table coverage khác SQL: missing={sorted(set(sql_tables) - set(headings))}, extra={sorted(set(headings) - set(sql_tables))}",
            )
        for table, columns in sql_tables.items():
            if headings.get(table, set()) != columns:
                self.add(
                    "DATA_DICTIONARY_COLUMN_COVERAGE",
                    "error",
                    dictionary_path,
                    f"Dictionary columns khác SQL cho {table}: missing={sorted(columns - headings.get(table, set()))}, extra={sorted(headings.get(table, set()) - columns)}",
                )
        if coverage and (
            int(coverage.group(1)) != len(sql_tables)
            or int(coverage.group(2)) != sum(map(len, sql_tables.values()))
        ):
            self.add("DATA_DICTIONARY_COUNT", "error", dictionary_path, "DATA_DICTIONARY_COVERAGE không khớp SQL")

    def validate_content_schema(self):
        path = self.root / "contracts/content/manifest.v1.schema.json"
        if not path.is_file():
            return
        try:
            document = json.loads(
                path.read_text(encoding="utf-8"), object_pairs_hook=_json_unique_object
            )
            Draft202012Validator.check_schema(document)
        except (ValueError, json.JSONDecodeError, SchemaError) as exc:
            self.add("CONTENT_SCHEMA_INVALID", "error", path, str(exc))

    def validate_skill_case_matrix(self):
        base = self.root / "delivery/case-matrices"
        matrix_path = base / "skill-parity-p0.json"
        schema_path = base / "skill-parity-p0.schema.json"
        if not matrix_path.is_file() or not schema_path.is_file():
            self.add(
                "SKILL_MATRIX_MISSING",
                "error",
                matrix_path,
                "Thiếu manifest hoặc schema ma trận parity kỹ năng P0",
            )
            return
        try:
            matrix = json.loads(
                matrix_path.read_text(encoding="utf-8"),
                object_pairs_hook=_json_unique_object,
            )
            schema = json.loads(
                schema_path.read_text(encoding="utf-8"),
                object_pairs_hook=_json_unique_object,
            )
            Draft202012Validator.check_schema(schema)
        except (ValueError, json.JSONDecodeError, SchemaError) as exc:
            self.add("SKILL_MATRIX_SCHEMA", "error", schema_path, str(exc))
            return
        for error in sorted(
            Draft202012Validator(schema).iter_errors(matrix),
            key=lambda item: list(item.absolute_path),
        ):
            location = "/".join(str(part) for part in error.absolute_path) or "(root)"
            self.add(
                "SKILL_MATRIX_INVALID",
                "error",
                matrix_path,
                f"{location}: {error.message}",
            )

        case_schema = None
        case_schema_ref = matrix.get("case_schema")
        if not isinstance(case_schema_ref, str):
            self.add("SKILL_CASE_SCHEMA_REF", "error", matrix_path, "Thiếu case_schema")
        else:
            case_schema_path = (base / case_schema_ref).resolve()
            if not case_schema_path.is_relative_to(base.resolve()) or not case_schema_path.is_file():
                self.add("SKILL_CASE_SCHEMA_REF", "error", matrix_path, f"case_schema không hợp lệ: {case_schema_ref}")
            else:
                try:
                    case_schema = json.loads(case_schema_path.read_text(encoding="utf-8"), object_pairs_hook=_json_unique_object)
                    Draft202012Validator.check_schema(case_schema)
                except (ValueError, json.JSONDecodeError, SchemaError) as exc:
                    self.add("SKILL_CASE_SCHEMA_INVALID", "error", case_schema_path, str(exc))

        catalog = matrix.get("catalog") if isinstance(matrix.get("catalog"), dict) else {}
        catalog_ref = catalog.get("path")
        catalog_records: dict[str, dict] = {}
        if isinstance(catalog_ref, str):
            catalog_path = (base / catalog_ref).resolve()
            if not catalog_path.is_file():
                self.add("SKILL_MATRIX_CATALOG", "error", matrix_path, "Catalog skill không tồn tại")
            else:
                actual_hash = hashlib.sha256(catalog_path.read_bytes()).hexdigest()
                actual_count = len(catalog_path.read_bytes().splitlines())
                for number, line in enumerate(catalog_path.read_text(encoding="utf-8").splitlines(), 1):
                    try:
                        record = json.loads(line, object_pairs_hook=_json_unique_object)
                        catalog_id = record.get("catalog_id")
                        if isinstance(catalog_id, str):
                            catalog_records[catalog_id] = record
                    except (ValueError, json.JSONDecodeError) as exc:
                        self.add("SKILL_MATRIX_CATALOG", "error", catalog_path, f"Dòng {number}: {exc}")
                if actual_hash != catalog.get("sha256") or actual_count != catalog.get("records_discovered"):
                    self.add("SKILL_MATRIX_CATALOG", "error", matrix_path, "Hash hoặc số record skill catalog không khớp matrix")

        fixture_ref = matrix.get("fixture_ref")
        fixture_schema_ref = matrix.get("fixture_schema")
        fixture_path = (base / fixture_ref.split("#", 1)[0]).resolve() if isinstance(fixture_ref, str) else None
        fixture_schema_path = (base / fixture_schema_ref).resolve() if isinstance(fixture_schema_ref, str) else None
        if fixture_path is None or not fixture_path.is_relative_to(self.root) or not fixture_path.is_file():
            self.add("SKILL_MATRIX_FIXTURE", "error", matrix_path, "Fixture training NPC không tồn tại")
        elif fixture_schema_path is None or not fixture_schema_path.is_relative_to(self.root) or not fixture_schema_path.is_file():
            self.add("SKILL_FIXTURE_SCHEMA", "error", matrix_path, "Schema fixture training NPC không tồn tại")
        else:
            try:
                fixture = json.loads(fixture_path.read_text(encoding="utf-8"), object_pairs_hook=_json_unique_object)
                fixture_schema = json.loads(fixture_schema_path.read_text(encoding="utf-8"), object_pairs_hook=_json_unique_object)
                Draft202012Validator.check_schema(fixture_schema)
                for error in Draft202012Validator(fixture_schema).iter_errors(fixture):
                    self.add("SKILL_FIXTURE_INVALID", "error", fixture_path, error.message)
                fragment = fixture_ref.partition("#")[2]
                if fragment and fixture.get("fixture_id") != fragment:
                    self.add("SKILL_FIXTURE_FRAGMENT", "error", matrix_path, "fixture_ref không khớp fixture_id")
            except (ValueError, json.JSONDecodeError, SchemaError) as exc:
                self.add("SKILL_FIXTURE_SCHEMA", "error", fixture_schema_path, str(exc))

        case_instances = matrix.get("case_instances")
        parsed_cases: list[dict] = []
        if isinstance(case_instances, list) and case_schema is not None:
            for case_ref in case_instances:
                if not isinstance(case_ref, str):
                    continue
                case_path = (base / case_ref).resolve()
                if not case_path.is_relative_to(base.resolve()) or not case_path.is_file():
                    self.add("SKILL_CASE_INSTANCE_MISSING", "error", matrix_path, f"Case không tồn tại: {case_ref}")
                    continue
                try:
                    case = json.loads(case_path.read_text(encoding="utf-8"), object_pairs_hook=_json_unique_object)
                    parsed_cases.append(case)
                    for error in Draft202012Validator(case_schema).iter_errors(case):
                        self.add("SKILL_CASE_INSTANCE_INVALID", "error", case_path, error.message)
                except (ValueError, json.JSONDecodeError) as exc:
                    self.add("SKILL_CASE_INSTANCE_INVALID", "error", case_path, str(exc))

        if self.mode == "release":
            groups = matrix.get("groups") if isinstance(matrix.get("groups"), list) else []
            blocked = catalog.get("authority") != "PARITY_DONE" or not groups or any(
                group.get("selection_status") != "PARITY_DONE" for group in groups if isinstance(group, dict)
            ) or bool(matrix.get("current_blocked_fields")) or not case_instances
            if blocked:
                self.add("SKILL_MATRIX_RELEASE_BLOCKED", "error", matrix_path, "Release cần authority/group PARITY_DONE, case instances đầy đủ và không còn blocked_fields")
            selected_group: dict[str, str] = {}
            duplicate_selection: set[str] = set()
            for group in groups:
                if not isinstance(group, dict) or not isinstance(group.get("catalog_selection"), list):
                    continue
                for catalog_id in group["catalog_selection"]:
                    if catalog_id in selected_group:
                        duplicate_selection.add(catalog_id)
                    elif isinstance(catalog_id, str):
                        selected_group[catalog_id] = str(group.get("group"))
            missing_selection = set(catalog_records) - set(selected_group)
            extra_selection = set(selected_group) - set(catalog_records)
            if duplicate_selection or missing_selection or extra_selection:
                self.add(
                    "SKILL_MATRIX_CATALOG_COVERAGE",
                    "error",
                    matrix_path,
                    f"Selection phải phủ đúng một lần catalog: duplicate={len(duplicate_selection)}, missing={len(missing_selection)}, extra={len(extra_selection)}",
                )

            case_ids = [case.get("case_id") for case in parsed_cases]
            if len(case_ids) != len(set(case_ids)):
                self.add("SKILL_CASE_DUPLICATE", "error", matrix_path, "case_id phải duy nhất")
            invalid_case_state = 0
            observed: set[tuple[str, int, str]] = set()
            for case in parsed_cases:
                catalog_id = case.get("catalog_ref")
                level = case.get("skill_level")
                variant = case.get("variant")
                if isinstance(catalog_id, str) and isinstance(level, int) and isinstance(variant, str):
                    observed.add((catalog_id, level, variant))
                statuses = []
                for section in (case.get("dimensions"), case.get("oracle")):
                    if isinstance(section, dict):
                        statuses.extend(value.get("status") for value in section.values() if isinstance(value, dict))
                if (
                    selected_group.get(catalog_id) != case.get("group")
                    or case.get("lifecycle") != "PARITY_DONE"
                    or bool(case.get("blocked_fields"))
                    or any(status not in {"PARITY_DONE", "NOT_APPLICABLE"} for status in statuses)
                ):
                    invalid_case_state += 1
            if invalid_case_state:
                self.add("SKILL_CASE_RELEASE_STATE", "error", matrix_path, f"{invalid_case_state} case chưa PARITY_DONE, sai group hoặc còn blocked field")

            required_variants = matrix.get("required_variants") if isinstance(matrix.get("required_variants"), list) else []
            expected: set[tuple[str, int, str]] = set()
            for catalog_id, record in catalog_records.items():
                raw_max = record.get("fields", {}).get("MaxLevel", "1") if isinstance(record.get("fields"), dict) else "1"
                try:
                    max_level = max(1, int(raw_max or 1))
                except (TypeError, ValueError):
                    max_level = 1
                for level in range(1, max_level + 1):
                    for variant in required_variants:
                        if isinstance(variant, str):
                            expected.add((catalog_id, level, variant))
            missing_cases = expected - observed
            extra_cases = observed - expected
            if missing_cases or extra_cases:
                self.add(
                    "SKILL_CASE_EXPANSION_COVERAGE",
                    "error",
                    matrix_path,
                    f"Case phải phủ mọi skill/level/variant: missing={len(missing_cases)}, extra={len(extra_cases)}",
                )

    def validate_golden_manifest(self):
        path = self.root / "registry/golden-manifest.yaml"
        if not path.is_file():
            self.add("GOLDEN_MANIFEST_MISSING", "error", path, "Thiếu golden manifest")
            return
        document = self.load_yaml(path) or {}
        storage = document.get("storage")
        storage_required = {
            "backend",
            "bucket",
            "key_policy",
            "endpoint_env",
            "access_key_env",
            "secret_key_env",
            "region",
            "credentials_in_repo",
        }
        if not isinstance(storage, dict):
            self.add("GOLDEN_STORAGE", "error", path, "storage phải là mapping MinIO")
            storage = {}
        missing_storage = sorted(
            field for field in storage_required if field not in storage
        )
        if missing_storage:
            self.add(
                "GOLDEN_STORAGE_FIELDS",
                "error",
                path,
                f"storage thiếu: {', '.join(missing_storage)}",
            )
        if storage.get("backend") != "minio":
            self.add(
                "GOLDEN_STORAGE_BACKEND", "error", path, "storage.backend phải là minio"
            )
        if storage.get("key_policy") != "sha256-content-addressed":
            self.add(
                "GOLDEN_KEY_POLICY",
                "error",
                path,
                "key_policy phải là sha256-content-addressed",
            )
        if storage.get("credentials_in_repo") is not False:
            self.add(
                "GOLDEN_CREDENTIAL_POLICY",
                "error",
                path,
                "credentials_in_repo phải false",
            )

        goldens = document.get("goldens")
        if not isinstance(goldens, list) or not goldens:
            self.add("GOLDEN_ENTRIES", "error", path, "goldens phải là list không rỗng")
            return
        seen: set[str] = set()
        for number, item in enumerate(goldens, start=1):
            if not isinstance(item, dict):
                self.add(
                    "GOLDEN_ENTRY",
                    "error",
                    path,
                    f"Golden entry {number} phải là mapping",
                )
                continue
            entity_id = item.get("id")
            if not isinstance(entity_id, str) or not entity_id.startswith("GOLD-"):
                self.add(
                    "GOLDEN_ID",
                    "error",
                    path,
                    f"Golden entry {number} có id không hợp lệ",
                )
                continue
            if entity_id in seen:
                self.add(
                    "GOLDEN_DUPLICATE",
                    "error",
                    path,
                    "Golden ID trùng trong manifest",
                    entity_id,
                )
            seen.add(entity_id)
            required_for = item.get("required_for")
            if not isinstance(required_for, list) or not required_for:
                self.add(
                    "GOLDEN_REQUIRED_FOR",
                    "error",
                    path,
                    "required_for phải là list không rỗng",
                    entity_id,
                )
            else:
                for target in required_for:
                    if not isinstance(target, str) or not target.startswith(
                        ("PAR-", "TEST-", "FR-", "NFR-")
                    ):
                        self.add(
                            "GOLDEN_REQUIRED_REF_TYPE",
                            "error",
                            path,
                            f"required_for ref không hợp lệ: {target!r}",
                            entity_id,
                        )
                    elif target not in self.defined_ids:
                        self.add(
                            "GOLDEN_REQUIRED_REF",
                            "error",
                            path,
                            f"required_for chưa định nghĩa: {target}",
                            entity_id,
                        )
            status = item.get("status")
            artifact = item.get("artifact")
            ready = status in {"READY", "APPROVED", "COMPLETED"}
            if not ready:
                if not item.get("blocker"):
                    self.add(
                        "GOLDEN_BLOCKER",
                        "error",
                        path,
                        "Golden chưa ready phải có blocker",
                        entity_id,
                    )
                if self.mode == "release":
                    self.add(
                        "GOLDEN_NOT_READY",
                        "error",
                        path,
                        f"Golden status {status!r} chặn release",
                        entity_id,
                    )
                continue
            artifact_required = {
                "object_key",
                "sha256",
                "size_bytes",
                "captured_at",
                "source_revision",
                "tool_revision",
                "content_type",
            }
            if not isinstance(artifact, dict):
                self.add(
                    "GOLDEN_ARTIFACT",
                    "error",
                    path,
                    "Golden ready phải có artifact mapping",
                    entity_id,
                )
                continue
            missing = sorted(
                field
                for field in artifact_required
                if artifact.get(field) in (None, "")
            )
            if missing:
                self.add(
                    "GOLDEN_ARTIFACT_FIELDS",
                    "error",
                    path,
                    f"artifact thiếu: {', '.join(missing)}",
                    entity_id,
                )
                continue
            digest = artifact.get("sha256")
            if not isinstance(digest, str) or not SHA256_RE.fullmatch(digest):
                self.add(
                    "GOLDEN_HASH_FORMAT",
                    "error",
                    path,
                    "artifact.sha256 phải là 64 hex lowercase",
                    entity_id,
                )
                continue
            size = artifact.get("size_bytes")
            if not isinstance(size, int) or isinstance(size, bool) or size < 0:
                self.add(
                    "GOLDEN_SIZE",
                    "error",
                    path,
                    "artifact.size_bytes phải là integer >= 0",
                    entity_id,
                )
                continue
            object_key = artifact.get("object_key")
            if (
                not isinstance(object_key, str)
                or object_key.startswith("/")
                or ".." in Path(object_key).parts
            ):
                self.add(
                    "GOLDEN_OBJECT_KEY",
                    "error",
                    path,
                    "artifact.object_key không an toàn",
                    entity_id,
                )
                continue
            if digest not in object_key:
                self.add(
                    "GOLDEN_CONTENT_ADDRESS",
                    "error",
                    path,
                    "object_key phải chứa SHA-256 theo key_policy",
                    entity_id,
                )
            try:
                _parse_timestamp(artifact["captured_at"])
            except (TypeError, ValueError) as exc:
                self.add("GOLDEN_CAPTURED_AT", "error", path, str(exc), entity_id)
            if self.mode == "release":
                self._validate_minio_object(path, storage, artifact, entity_id)

    def _validate_minio_object(
        self, path: Path, storage: dict, artifact: dict, entity_id: str
    ):
        endpoint_name = storage.get("endpoint_env")
        endpoint = (
            os.environ.get(endpoint_name, "") if isinstance(endpoint_name, str) else ""
        )
        if not endpoint:
            self.add(
                "MINIO_ENDPOINT_UNAVAILABLE",
                "error",
                path,
                f"Thiếu biến endpoint MinIO: {endpoint_name!r}",
                entity_id,
            )
            return
        bucket = storage.get("bucket")
        if not isinstance(bucket, str) or not bucket:
            self.add(
                "MINIO_BUCKET", "error", path, "storage.bucket không hợp lệ", entity_id
            )
            return
        object_key = artifact["object_key"]
        url = f"{endpoint.rstrip('/')}/{quote(bucket, safe='-_.~')}/{quote(object_key, safe='/-_.~')}"
        access_name = storage.get("access_key_env")
        secret_name = storage.get("secret_key_env")
        access_key = (
            os.environ.get(access_name, "") if isinstance(access_name, str) else ""
        )
        secret_key = (
            os.environ.get(secret_name, "") if isinstance(secret_name, str) else ""
        )
        if bool(access_key) != bool(secret_key):
            self.add(
                "MINIO_CREDENTIALS",
                "error",
                path,
                "MinIO access/secret phải cùng có hoặc cùng không",
                entity_id,
            )
            return
        headers: dict[str, str] = {}
        if access_key and secret_key:
            headers = _aws_sigv4_headers(
                url,
                access_key,
                secret_key,
                str(storage.get("region") or "us-east-1"),
                os.environ.get("AWS_SESSION_TOKEN"),
            )
        try:
            actual_sha, actual_size = _download_sha256(url, headers)
        except (HTTPError, URLError, OSError, TimeoutError) as exc:
            self.add(
                "MINIO_FETCH",
                "error",
                path,
                f"Không tải được golden object: {exc}",
                entity_id,
            )
            return
        if actual_sha != artifact["sha256"]:
            self.add(
                "MINIO_HASH",
                "error",
                path,
                f"MinIO SHA-256 {actual_sha} không khớp manifest",
                entity_id,
            )
        if actual_size != artifact["size_bytes"]:
            self.add(
                "MINIO_SIZE",
                "error",
                path,
                f"MinIO size {actual_size} không khớp manifest",
                entity_id,
            )

    def validate_test_results(self):
        path = self.root / "registry/test-results/index.yaml"
        if not path.is_file():
            if self.mode == "release":
                self.add(
                    "TEST_RESULT_INDEX_MISSING",
                    "error",
                    path,
                    "Thiếu test-result index",
                )
            return
        document = self.load_yaml(path) or {}
        schema_path = self.root / "schemas/test-result.schema.json"
        result_schema_validator = None
        if schema_path.is_file():
            try:
                schema = json.loads(
                    schema_path.read_text(encoding="utf-8"),
                    object_pairs_hook=_json_unique_object,
                )
                Draft202012Validator.check_schema(schema)
                result_schema_validator = Draft202012Validator(schema)
            except (ValueError, json.JSONDecodeError, SchemaError) as exc:
                self.add("TEST_RESULT_SCHEMA_INVALID", "error", schema_path, str(exc))
        status = document.get("status")
        if status not in {"BLOCKED", "READY"}:
            self.add(
                "TEST_RESULT_INDEX_STATUS",
                "error",
                path,
                "status phải BLOCKED hoặc READY",
            )
        if status == "BLOCKED" and not document.get("blocker"):
            self.add(
                "TEST_RESULT_INDEX_BLOCKER",
                "error",
                path,
                "Index BLOCKED phải có blocker",
            )
        results = document.get("results")
        if not isinstance(results, list):
            self.add("TEST_RESULT_ENTRIES", "error", path, "results phải là list")
            return
        seen: set[str] = set()
        passed: set[str] = set()
        snapshot = (
            self.load_yaml(self.root / "registry/catalogs/source-snapshot.yaml") or {}
        )
        current_revision = (snapshot.get("source_git") or {}).get("revision")
        for number, result in enumerate(results, start=1):
            if not isinstance(result, dict):
                self.add(
                    "TEST_RESULT_ENTRY",
                    "error",
                    path,
                    f"Result {number} phải là mapping",
                )
                continue
            if result_schema_validator is not None:
                for error in result_schema_validator.iter_errors(result):
                    location = (
                        ".".join(str(part) for part in error.absolute_path) or "(root)"
                    )
                    self.add(
                        "TEST_RESULT_SCHEMA",
                        "error",
                        path,
                        f"Result {number} {location}: {error.message}",
                    )
            required = {
                "test_id",
                "status",
                "revision",
                "executed_at",
                "result_path",
                "sha256",
                "reviewer",
                "golden_ids",
            }
            missing = sorted(
                field for field in required if result.get(field) in (None, "")
            )
            if missing:
                self.add(
                    "TEST_RESULT_FIELDS",
                    "error",
                    path,
                    f"Result {number} thiếu: {', '.join(missing)}",
                )
                continue
            test_id = result["test_id"]
            if (
                not isinstance(test_id, str)
                or not test_id.startswith("TEST-")
                or test_id not in self.defined_ids
            ):
                self.add(
                    "TEST_RESULT_TEST_REF",
                    "error",
                    path,
                    f"test_id không hợp lệ/chưa định nghĩa: {test_id!r}",
                )
                continue
            if test_id in seen:
                self.add(
                    "TEST_RESULT_DUPLICATE",
                    "error",
                    path,
                    f"Test result trùng: {test_id}",
                    test_id,
                )
            seen.add(test_id)
            if result["status"] not in {"PASS", "FAIL"}:
                self.add(
                    "TEST_RESULT_STATUS",
                    "error",
                    path,
                    "Result status phải PASS hoặc FAIL",
                    test_id,
                )
            elif result["status"] == "PASS":
                passed.add(test_id)
            if current_revision and result["revision"] != current_revision:
                severity = "error" if self.mode == "release" else "warning"
                self.add(
                    "TEST_RESULT_REVISION",
                    severity,
                    path,
                    "Result revision khác source snapshot",
                    test_id,
                )
            try:
                _parse_timestamp(result["executed_at"])
            except (TypeError, ValueError) as exc:
                self.add("TEST_RESULT_EXECUTED_AT", "error", path, str(exc), test_id)
            golden_ids = result["golden_ids"]
            if not isinstance(golden_ids, list):
                self.add(
                    "TEST_RESULT_GOLDENS",
                    "error",
                    path,
                    "golden_ids phải là list",
                    test_id,
                )
            else:
                for golden_id in golden_ids:
                    if (
                        not isinstance(golden_id, str)
                        or not golden_id.startswith("GOLD-")
                        or golden_id not in self.defined_ids
                    ):
                        self.add(
                            "TEST_RESULT_GOLDEN_REF",
                            "error",
                            path,
                            f"Golden ref không hợp lệ: {golden_id!r}",
                            test_id,
                        )
            digest = result["sha256"]
            artifact_path = Path(str(result["result_path"]))
            if not artifact_path.is_absolute():
                artifact_path = self.root / artifact_path
            if not isinstance(digest, str) or not SHA256_RE.fullmatch(digest):
                self.add(
                    "TEST_RESULT_HASH_FORMAT",
                    "error",
                    path,
                    "sha256 phải là 64 hex lowercase",
                    test_id,
                )
            elif not artifact_path.is_file():
                self.add(
                    "TEST_RESULT_PATH",
                    "error",
                    path,
                    f"result_path không tồn tại: {result['result_path']}",
                    test_id,
                )
            elif _sha256_file(artifact_path) != digest:
                self.add(
                    "TEST_RESULT_HASH",
                    "error",
                    path,
                    "Test result SHA-256 không khớp",
                    test_id,
                )
        if self.mode == "release":
            if status != "READY" or not results:
                self.add(
                    "TEST_RESULTS_NOT_READY",
                    "error",
                    path,
                    "Release cần index READY và results không rỗng",
                )
            for test_id in sorted(
                item for item in self.defined_ids if item.startswith("TEST-")
            ):
                if test_id not in passed:
                    self.add(
                        "TEST_RESULT_MISSING_PASS",
                        "error",
                        path,
                        "Test bắt buộc chưa có PASS result",
                        test_id,
                    )

    def validate_contracts(self):
        if self.mode not in {"premerge", "release"}:
            return
        self.validate_contract_declarations()
        self.validate_openapi()
        self.validate_proto()
        self.validate_sql_contract()
        self.validate_content_schema()
        self.validate_skill_case_matrix()
        if self.mode == "release":
            self.validate_release_contract_tools()

    def validate_release_contract_tools(self):
        openapi_path = self.root / "contracts/openapi/game.v1.yaml"
        redocly = shutil.which("redocly")
        if not redocly:
            self.add(
                "OPENAPI_TOOL_UNAVAILABLE",
                "error",
                openapi_path,
                "Release cần redocly CLI đã pin; không fallback network qua npx",
            )
        else:
            result = subprocess.run(
                [redocly, "lint", str(openapi_path)],
                capture_output=True,
                text=True,
                check=False,
            )
            if result.returncode != 0:
                self.add(
                    "OPENAPI_LINT",
                    "error",
                    openapi_path,
                    (result.stderr or result.stdout or "redocly lint failed").strip(),
                )

        sql_path = self.root / "contracts/sql/game.v1.sql"
        psql = shutil.which("psql")
        dsn = os.environ.get("SPEC_POSTGRES_VALIDATION_DSN")
        if not psql:
            self.add(
                "SQL_TOOL_UNAVAILABLE",
                "error",
                sql_path,
                "Release cần psql PostgreSQL 16",
            )
        elif not dsn:
            self.add(
                "SQL_VALIDATION_DSN",
                "error",
                sql_path,
                "Thiếu SPEC_POSTGRES_VALIDATION_DSN trỏ tới database disposable",
            )
        else:
            version = subprocess.run(
                [psql, "--version"], capture_output=True, text=True, check=False
            )
            major_match = re.search(r"(\d+)(?:\.\d+)?", version.stdout)
            if not major_match or int(major_match.group(1)) != 16:
                self.add(
                    "SQL_TOOL_VERSION",
                    "error",
                    sql_path,
                    f"Cần psql major 16, nhận {(version.stdout or version.stderr).strip()}",
                )
            else:
                result = subprocess.run(
                    [psql, dsn, "--set", "ON_ERROR_STOP=1", "--file", str(sql_path)],
                    capture_output=True,
                    text=True,
                    check=False,
                )
                if result.returncode != 0:
                    self.add(
                        "SQL_RUNTIME_VALIDATE",
                        "error",
                        sql_path,
                        (result.stderr or result.stdout or "psql failed").strip(),
                    )
                else:
                    negative_path = self.root / "contracts/sql/game.v1.negative.sql"
                    negative_result = subprocess.run(
                        [psql, dsn, "--set", "ON_ERROR_STOP=1", "--file", str(negative_path)],
                        capture_output=True,
                        text=True,
                        check=False,
                    )
                    if negative_result.returncode != 0:
                        self.add(
                            "SQL_NEGATIVE_RUNTIME_VALIDATE",
                            "error",
                            negative_path,
                            (negative_result.stderr or negative_result.stdout or "negative SQL contract failed").strip(),
                        )

    def validate_contradictions(self):
        path = self.root / "as-is/contradictions.yaml"
        if not path.is_file():
            return
        document = self.load_yaml(path) or {}
        contradictions = document.get("contradictions")
        if not isinstance(contradictions, list):
            self.add(
                "CONTRADICTIONS_SHAPE", "error", path, "contradictions phải là list"
            )
            return
        for item in contradictions:
            if not isinstance(item, dict):
                self.add(
                    "CONTRADICTION_ENTRY",
                    "error",
                    path,
                    "Contradiction phải là mapping",
                )
                continue
            entity_id = item.get("id")
            status = item.get("status")
            if status == "OPEN":
                severity = (
                    "error" if self.mode in {"premerge", "release"} else "warning"
                )
                self.add(
                    "OPEN_CONTRADICTION",
                    severity,
                    path,
                    "Contradiction OPEN chưa có disposition fail-closed",
                    entity_id,
                )
            elif status == "OPEN_BLOCKER":
                missing = [
                    field
                    for field in ("blocker", "dri", "reviewer", "acceptance_evidence")
                    if not item.get(field)
                ]
                if missing:
                    self.add(
                        "OPEN_BLOCKER_FIELDS",
                        "error",
                        path,
                        f"OPEN_BLOCKER thiếu: {', '.join(missing)}",
                        entity_id,
                    )
                if self.mode == "release":
                    self.add(
                        "OPEN_BLOCKER_RELEASE",
                        "error",
                        path,
                        "OPEN_BLOCKER chặn release",
                        entity_id,
                    )

    def validate_release(self):
        if self.mode != "release":
            return
        for path in sorted(self.root.rglob("*.md")):
            text = path.read_text(encoding="utf-8")
            priority_file = bool(re.search(r"\bP[01]\b", text))
            in_fence = False
            section = ""
            for number, line in enumerate(text.splitlines(), start=1):
                if re.match(r"^\s*(?:```|~~~)", line):
                    in_fence = not in_fence
                    continue
                if in_fence:
                    continue
                heading = re.match(r"^#{1,6}\s+(.+?)\s*$", line)
                if heading:
                    section = heading.group(1)
                if "[CẦN XÁC NHẬN]" not in line:
                    continue
                lowered = unicodedata.normalize("NFC", line).lower()
                marker_is_literal = "`[cần xác nhận]`" in lowered
                if marker_is_literal and any(
                    term in lowered for term in POLICY_MARKER_TERMS
                ):
                    continue
                priority_occurrence = priority_file or bool(
                    re.search(r"\bP[01]\b", line + " " + section)
                )
                severity = "error" if priority_occurrence else "warning"
                code = (
                    "RELEASE_UNCONFIRMED"
                    if priority_occurrence
                    else "RELEASE_UNCONFIRMED_NON_GATE"
                )
                self.add(
                    code,
                    severity,
                    path,
                    f"Dòng {number} còn [CẦN XÁC NHẬN] thực; section: {section or '(root)'}",
                )

    def validate_gate_status(self):
        gates_doc = self.load_yaml(self.root / "registry/gates.yaml") or {}
        tests_doc = self.load_yaml(self.root / "registry/tests.yaml") or {}
        results_doc = self.load_yaml(self.root / "registry/test-results/index.yaml") or {}
        tests = tests_doc.get("tests") if isinstance(tests_doc.get("tests"), list) else []
        results = results_doc.get("results") if isinstance(results_doc.get("results"), list) else []
        result_status = {item.get("test_id"): item.get("status") for item in results if isinstance(item, dict)}
        for gate in gates_doc.get("gates", []):
            if not isinstance(gate, dict):
                continue
            phase = gate.get("phase")
            status = gate.get("status")
            if status == "READY" and phase != "G0":
                required = [item for item in tests if isinstance(item, dict) and item.get("gate") == phase]
                missing = [item.get("id") for item in required if item.get("status") != "PASS" or result_status.get(item.get("id")) != "PASS"]
                if missing:
                    self.add("GATE_READY_WITHOUT_PASS", "error", self.root / "registry/gates.yaml", f"{gate.get('id')} READY nhưng thiếu PASS artifact: {', '.join(str(x) for x in missing)}", gate.get("id"))
            if self.mode == "release" and status not in {"READY", "COMPLETED"}:
                self.add("GATE_NOT_READY_RELEASE", "error", self.root / "registry/gates.yaml", f"{gate.get('id')} có status {status}; release yêu cầu READY/COMPLETED", gate.get("id"))

    def run(self):
        self.validate_required_layout()
        self.validate_cnpm()
        self.validate_unmarked_placeholders()
        self.validate_yaml_and_ids()
        self.validate_links()
        self.validate_traceability()
        self.validate_catalogs()
        self.validate_catalog_source_census()
        self.validate_source_snapshot()
        self.validate_catalog_reproducibility()
        self.validate_contracts()
        self.validate_data_dictionary()
        self.validate_contradictions()
        self.validate_golden_manifest()
        self.validate_test_results()
        self.validate_gate_status()
        self.validate_release()
        return self.findings


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("root", type=Path)
    parser.add_argument(
        "--mode", choices=("authoring", "premerge", "release"), default="authoring"
    )
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args()
    validator = Validator(args.root, args.mode)
    findings = validator.run()
    if args.json:
        print(
            json.dumps(
                [asdict(item) for item in findings], ensure_ascii=False, indent=2
            )
        )
    else:
        for item in findings:
            suffix = f" [{item.entity_id}]" if item.entity_id else ""
            print(
                f"{item.severity.upper()} {item.code} {item.path}: {item.message}{suffix}"
            )
        print(f"spec-validator: {len(findings)} finding(s), mode={args.mode}")
    return 1 if any(item.severity == "error" for item in findings) else 0


if __name__ == "__main__":
    sys.exit(main())
