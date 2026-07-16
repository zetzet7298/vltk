#!/usr/bin/env python3
"""Generate deterministic, source-backed catalogs for the VLTK port specs."""

from __future__ import annotations

import argparse
import csv
import hashlib
import json
import os
import subprocess
import sys
from pathlib import Path

import yaml


SOURCE_ROOT = Path("/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem")
CLIENT_ROOT = SOURCE_ROOT / "bin/client"
CLIENT_SETTINGS = SOURCE_ROOT / "bin/client/settings"
SERVER_SETTINGS = (
    SOURCE_ROOT / "bin/Server/settings",
    SOURCE_ROOT / "bin/Server/Server/settings",
)
PACKAGE_INI = SOURCE_ROOT / "bin/client/package.ini"
CLIENT_UI = SOURCE_ROOT / "bin/client/Ui"
CLIENT_SPR = SOURCE_ROOT / "bin/client/Spr"
CLIENT_MUSIC = SOURCE_ROOT / "bin/client/music"
VLTKTOOL = Path("/home/zet/Projects/vltktool")
SCRIPT_ROOTS = (
    SOURCE_ROOT / "bin/client/script",
    SOURCE_ROOT / "bin/Server/Server/script",
    SOURCE_ROOT / "bin/Server/script",
)


def sha256_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def git_state(path: Path) -> dict[str, object]:
    def run(*args: str) -> str:
        return subprocess.run(
            ["git", "-C", str(path), *args], text=True, capture_output=True, check=True
        ).stdout.strip()

    try:
        revision = run("rev-parse", "HEAD")
        dirty = bool(run("status", "--porcelain"))
    except (subprocess.CalledProcessError, FileNotFoundError):
        return {"revision": "UNAVAILABLE", "dirty": None}
    return {"revision": revision, "dirty": dirty}


def json_line(record: dict[str, object]) -> str:
    return json.dumps(record, ensure_ascii=False, sort_keys=True, separators=(",", ":"))


def write_jsonl(path: Path, records: list[dict[str, object]]) -> dict[str, object]:
    records.sort(key=lambda item: str(item.get("catalog_id", item.get("source_path", ""))))
    payload = "".join(json_line(record) + "\n" for record in records).encode("utf-8")
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(payload)
    entity_types = sorted({str(record["entity_type"]) for record in records})
    if len(entity_types) != 1:
        raise ValueError(f"{path.name}: expected exactly one entity_type, got {entity_types}")
    return {
        "path": path.name,
        "entity_type": entity_types[0],
        "records": len(records),
        "sha256": sha256_bytes(payload),
    }


def rel_source(path: Path) -> str:
    try:
        return path.relative_to(SOURCE_ROOT).as_posix()
    except ValueError:
        return path.as_posix()


def read_text(path: Path, decode_best) -> tuple[str, str, int]:
    return decode_best(path.read_bytes())


def source_path_digest(path: Path) -> str:
    """Hash raw filesystem path bytes without pretending they are JX logical-path bytes."""
    return sha256_bytes(os.fsencode(rel_source(path)))


def pathset_census(paths: list[Path]) -> dict[str, object]:
    """Fingerprint the input path set independently from emitted catalog rows."""
    relative = sorted({os.fsencode(rel_source(path)) for path in paths})
    payload = b"".join(path + b"\n" for path in relative)
    return {"files": len(relative), "paths_sha256": sha256_bytes(payload)}


def out_of_scope_category(path: Path) -> str | None:
    """Classify only explicit PC subsystems excluded by the approved port scope."""
    parts = [part.lower() for part in Path(rel_source(path)).parts]
    stem = path.stem.lower()
    if any(part in {"gm", "gm_tool", "gamemaster", "game_master"} for part in parts) or stem in {
        "gm",
        "gmscript",
    }:
        return "gm_backoffice"
    if any(part in {"paysys", "pay_sys", "billing"} for part in parts):
        return "pc_paysys"
    if any(part in {"launcher", "patcher", "updater"} for part in parts):
        return "pc_launcher_patcher"
    if any(part in {"anticheat", "anti_cheat"} for part in parts):
        return "pc_anticheat"
    return None


def record_governance(
    path: Path,
    *,
    phase: str,
    acceptance_id: str,
    evidence_status: str,
    disposition: str = "port",
) -> dict[str, object]:
    excluded = out_of_scope_category(path)
    if excluded:
        return {
            "phase": "OUT_OF_SCOPE",
            "acceptance_id": "TEST-COVERAGE-001",
            "evidence_status": evidence_status,
            "disposition": "defer",
            "disposition_reason": excluded,
        }
    return {
        "phase": phase,
        "acceptance_id": acceptance_id,
        "evidence_status": evidence_status,
        "disposition": disposition,
    }


def table_records(path: Path, entity: str, id_columns: tuple[str, ...], decode_best) -> list[dict[str, object]]:
    text, encoding, score = read_text(path, decode_best)
    reader = csv.DictReader(text.splitlines(), delimiter="\t")
    records: list[dict[str, object]] = []
    source_sha = sha256_file(path)
    for row_number, row in enumerate(reader, start=2):
        if not row or not any((value or "").strip() for value in row.values()):
            continue
        identity = next(((row.get(name) or "").strip() for name in id_columns if (row.get(name) or "").strip()), str(row_number))
        name = (row.get("SkillName") or row.get("Name") or row.get("NpcName") or row.get("GoodsName") or "").strip()
        # Preserve every source column, including empty values. Skills.txt has 114
        # columns; a convenience whitelist silently discarded combat semantics.
        fields = {key: (value or "").strip() for key, value in row.items() if key}
        governance = {
            "SKILL": ("P0", "TEST-SKL-001"),
            "MISSILE": ("P0", "TEST-SKL-002"),
            "NPC": ("P1", "TEST-NQ-001"),
            "GOODS": ("P1", "TEST-IIEL-001"),
        }[entity]
        record = {
            "catalog_id": f"{entity}-{identity}",
            "entity_type": entity.lower(),
            "legacy_id": identity,
            "name_vi": name or None,
            "source_path": rel_source(path),
            "source_row": row_number,
            "source_sha256": source_sha,
            "encoding": encoding,
            "decode_score": score,
            "source_column_count": len(reader.fieldnames or ()),
            "fields": fields,
            "lifecycle": "DISCOVERED",
            "verification": "UNVERIFIED",
            "owner_domain": {
                "SKILL": "skills",
                "MISSILE": "skills",
                "NPC": "npc-world",
                "GOODS": "items-economy",
            }[entity],
        }
        record.update(
            record_governance(
                path,
                phase=governance[0],
                acceptance_id=governance[1],
                evidence_status="STATIC_SOURCE_HASHED",
            )
        )
        records.append(record)
    return records


def map_records(path: Path, decode_best) -> list[dict[str, object]]:
    text, encoding, score = read_text(path, decode_best)
    values: dict[str, str] = {}
    for raw in text.splitlines():
        line = raw.strip()
        if not line or line.startswith((";", "#", "[")) or "=" not in line:
            continue
        key, value = line.split("=", 1)
        values[key.strip()] = value.strip()
    ids = sorted({int(key) for key in values if key.isdigit()})
    source_sha = sha256_file(path)
    return [{
        "catalog_id": f"MAP-{map_id}",
        "entity_type": "map",
        "legacy_id": map_id,
        "name_vi": values.get(f"{map_id}_name"),
        "logical_path_text": values.get(str(map_id)),
        "map_type": values.get(f"{map_id}_MapType"),
        "map_position": values.get(f"{map_id}_MapPos"),
        "source_path": rel_source(path),
        "source_sha256": source_sha,
        "encoding": encoding,
        "decode_score": score,
        "lifecycle": "DISCOVERED",
        "verification": "UNVERIFIED",
          "owner_domain": "world-map",
        "priority": "P1" if map_id == 53 else "P2",
        "canonical_runtime_id": map_id,
          "alias_allowed": False,
          **record_governance(
              path,
              phase="P1" if map_id == 53 else "P2",
              acceptance_id="TEST-MAP-053" if map_id == 53 else "TEST-COVERAGE-001",
              evidence_status="STATIC_SOURCE_HASHED",
          ),
    } for map_id in ids]


def file_catalog(
    paths: list[Path],
    entity: str,
    *,
    owner_domain: str,
    phase: str,
    acceptance_id: str,
    evidence_status: str = "STATIC_SOURCE_HASHED",
    metadata=None,
) -> list[dict[str, object]]:
    records = []
    for path in sorted(set(paths), key=lambda item: os.fsencode(rel_source(item))):
        raw = path.read_bytes()
        extra = metadata(path) if metadata else {}
        governance = record_governance(
            path,
            phase=phase,
            acceptance_id=acceptance_id,
            evidence_status=evidence_status,
        )
        record = {
            "catalog_id": f"{entity}-{sha256_bytes(rel_source(path).encode('utf-8'))[:16].upper()}",
            "entity_type": entity.lower(),
            "source_path": rel_source(path),
            "source_path_bytes_sha256": source_path_digest(path),
            "byte_count": len(raw),
            "source_sha256": sha256_bytes(raw),
            "lifecycle": "DISCOVERED",
            "verification": "UNVERIFIED",
            "owner_domain": owner_domain,
            **governance,
            **extra,
        }
        records.append(record)
    return records


def classify_lua(path: Path) -> dict[str, object]:
    rel = rel_source(path).replace("\\", "/")
    lowered = rel.lower()
    parts = lowered.split("/")
    explicit_ui = path.is_relative_to(CLIENT_UI) or "/script/ui/" in f"/{lowered}/"
    quest_strong = {"missions", "task", "startgame", "tagnewplayer"}
    quest_tokens = {"quest", "quests", "nhiemvu", "nhiem_vu", "mission", "missions"}
    if explicit_ui:
        classification = "ui_runtime_script"
        basis = "root client/Ui hoặc component script/ui"
    elif any(part in quest_strong for part in parts):
        classification = "quest_candidate_strong"
        basis = "component đường dẫn missions/task/startgame/tagnewplayer"
    elif any(token in part for part in parts for token in quest_tokens):
        classification = "quest_candidate_weak"
        basis = "token quest/mission/nhiemvu trong đường dẫn"
    elif out_of_scope_category(path):
        classification = "deferred_out_of_scope"
        basis = "component đường dẫn khớp subsystem loại trừ"
    else:
        classification = "gameplay_or_event_script_unclassified"
        basis = "không khớp rule path hẹp"
    return {
        "classification": classification,
        "classification_basis": basis,
        "classification_limit": (
            "Chỉ là heuristic path deterministic; không chứng minh script được load, "
            "không phân tích call graph/host API và có thể bỏ sót tên tiếng Trung."
        ),
    }


def classify_ui_spr(path: Path) -> dict[str, object]:
    rel = rel_source(path).replace("\\", "/")
    lowered = rel.lower()
    if path.is_relative_to(CLIENT_UI):
        scope = "client_ui_embedded"
    elif "/spr/ui/" in f"/{lowered}/" or "/spr/ui3/" in f"/{lowered}/":
        scope = "spr_ui_explicit_tree"
    else:
        scope = "spr_shared_visual_candidate"
    return {
        "candidate_scope": scope,
        "logical_path": None,
        "jx_uid": None,
        "winner_package": None,
        "winner_status": "BLOCKED",
        "winner_blocker": (
            "Thiếu package order 0 vltkcache.pak và logical path bytes chưa được "
            "candidate resolver chứng minh đầy đủ."
        ),
    }


def classify_avatar_candidate(path: Path) -> dict[str, object]:
    return {
        "candidate_kind": "extracted_spr" if path.suffix.lower() == ".spr" else "resource_reference_table",
        "candidate_rule": (
            "Token avatar/face/player/newplayer/selplayer/Npc/Series hoặc bảng *Res; "
            "candidate không đồng nghĩa runtime layer."
        ),
        "logical_path": None,
        "jx_uid": None,
        "winner_package": None,
        "winner_status": "BLOCKED",
        "winner_blocker": (
            "Thiếu reference -> logical path bytes và package order 0 vltkcache.pak; "
            "không thể chốt UID/first-match winner."
        ),
    }


def classify_audio(path: Path) -> dict[str, object]:
    relative_music = path.relative_to(CLIENT_MUSIC)
    variant = "backup" if relative_music.parts and relative_music.parts[0].lower() == "bak" else "active_loose"
    return {
        "source_variant": variant,
        "phase": "REFERENCE_ONLY" if variant == "backup" else "P2",
        "disposition": "defer" if variant == "backup" else "port",
        "logical_path": None,
        "jx_uid": None,
        "winner_package": None,
        "winner_status": "BLOCKED",
        "winner_blocker": (
            "Loose-file presence không chứng minh runtime PAK winner/cue binding; "
            "package order 0 vltkcache.pak đang thiếu."
        ),
    }


def parse_packages(pak_uids) -> list[dict[str, object]]:
    package_dir = ""
    configured: list[tuple[int, str]] = []
    for raw_line in PACKAGE_INI.read_text(encoding="ascii").splitlines():
        line = raw_line.strip()
        if not line or line.startswith((";", "#", "[")) or "=" not in line:
            continue
        key, value = (part.strip() for part in line.split("=", 1))
        if key.lower() == "path":
            package_dir = value.strip("\\/")
        elif key.isdigit():
            configured.append((int(key), value))

    records: list[dict[str, object]] = []
    for order, filename in sorted(configured):
        path = PACKAGE_INI.parent / package_dir / filename
        present = path.is_file()
        uid_count = None
        uid_index_sha = None
        index_status = "BLOCKED_MISSING_PACKAGE"
        index_blocker = "Package file thiếu tại configured client data root."
        if present:
            try:
                uids = sorted(pak_uids(path))
                uid_count = len(uids)
                uid_index_sha = sha256_bytes(
                    "".join(f"{uid:08x}\n" for uid in uids).encode("ascii")
                )
                index_status = "STATIC_UID_INDEXED"
                index_blocker = None
            except (OSError, ValueError, SystemExit) as exc:
                index_status = "BLOCKED_INVALID_INDEX"
                index_blocker = f"Không đọc được PAK UID index: {exc}"
        record = {
            "catalog_id": f"PACKAGE-{order:02d}",
            "entity_type": "package",
            "package_order": order,
            "package_name": filename,
            "configured": True,
            "configured_relative_path": f"{package_dir}/{filename}",
            "present": present,
            "source_path": rel_source(path),
            "byte_count": path.stat().st_size if present else None,
            "source_sha256": sha256_file(path) if present else None,
            "lifecycle": "DISCOVERED",
            "verification": "UNVERIFIED",
            "owner_domain": "content-config",
            "unique_uid_count": uid_count,
            "uid_index_sha256": uid_index_sha,
            "asset_index_status": index_status,
            "asset_index_blocker": index_blocker,
            "winner_resolution_status": "BLOCKED",
            "blocker": (
                "Package file thiếu tại configured client data root."
                if not present
                else (
                    "UID index đã census nhưng thiếu package order 0 và các package 26-28; "
                    "candidate path resolver không đủ chứng minh first-match winner toàn cục."
                )
            ),
            **record_governance(
                PACKAGE_INI,
                phase="P0",
                acceptance_id="TEST-SOURCE-001",
                evidence_status="BLOCKED_PACKAGE_RESOLUTION",
            ),
        }
        records.append(record)
    return records


def coverage_for(catalog_records: list[list[dict[str, object]]]) -> dict[str, dict[str, int]]:
    coverage: dict[str, dict[str, int]] = {}
    for records in catalog_records:
        for record in records:
            entity = str(record["entity_type"])
            bucket = coverage.setdefault(
                entity,
                {"discovered": 0, "cataloged": 0, "owned": 0, "dispositioned": 0, "unresolved": 0},
            )
            bucket["discovered"] += 1
            bucket["cataloged"] += 1
            bucket["owned"] += int(bool(record.get("owner_domain")))
            bucket["dispositioned"] += int(bool(record.get("disposition")))
            bucket["unresolved"] += int(str(record.get("evidence_status", "")).startswith("BLOCKED"))
    return dict(sorted(coverage.items()))


def count_by(records: list[dict[str, object]], field: str) -> dict[str, int]:
    counts: dict[str, int] = {}
    for record in records:
        value = str(record.get(field, "UNSET"))
        counts[value] = counts.get(value, 0) + 1
    return dict(sorted(counts.items()))


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    out = args.output.resolve()
    out.mkdir(parents=True, exist_ok=True)

    sys.path.insert(0, str(VLTKTOOL))
    from decode_item_texts_vi import decode_best  # type: ignore
    from resolve_uid import pak_uids  # type: ignore

    specialized: list[tuple[str, list[dict[str, object]]]] = [
        ("skills.jsonl", table_records(CLIENT_SETTINGS / "Skills.txt", "SKILL", ("SkillId",), decode_best)),
        ("missiles.jsonl", table_records(CLIENT_SETTINGS / "Missles.txt", "MISSILE", ("MissleId", "MissleID", "Id"), decode_best)),
        ("npcs.jsonl", table_records(CLIENT_SETTINGS / "Npcs.txt", "NPC", ("NpcId", "NpcID", "Id"), decode_best)),
        ("goods.jsonl", table_records(CLIENT_SETTINGS / "Goods.txt", "GOODS", ("GoodsId", "GoodsID", "Id"), decode_best)),
        ("maps.jsonl", map_records(CLIENT_SETTINGS / "MapList.ini", decode_best)),
    ]

    settings_roots = (CLIENT_SETTINGS, *SERVER_SETTINGS)
    setting_files = sorted(
        path for root in settings_roots if root.exists() for path in root.rglob("*")
        if path.is_file() and path.suffix.lower() in {".txt", ".ini"}
    )
    ui_files = sorted(
        path for path in CLIENT_UI.rglob("*")
        if path.is_file() and path.suffix.lower() in {".ini", ".txt"}
    ) if CLIENT_UI.exists() else []
    lua_files: list[Path] = [
        path for path in CLIENT_UI.rglob("*") if path.is_file() and path.suffix.lower() == ".lua"
    ] if CLIENT_UI.exists() else []
    for root in SCRIPT_ROOTS:
        if not root.exists():
            continue
        lua_files.extend(
            path for path in root.rglob("*") if path.is_file() and path.suffix.lower() == ".lua"
        )

    all_lua = sorted(set(lua_files), key=lambda path: os.fsencode(rel_source(path)))
    ui_lua = [path for path in all_lua if classify_lua(path)["classification"] == "ui_runtime_script"]
    quest_lua = [path for path in all_lua if str(classify_lua(path)["classification"]).startswith("quest_candidate")]
    deferred_lua = [path for path in all_lua if classify_lua(path)["classification"] == "deferred_out_of_scope"]
    classified_lua = set(ui_lua) | set(quest_lua) | set(deferred_lua)
    general_lua = [path for path in all_lua if path not in classified_lua]

    spr_paths = [
        path
        for root in (CLIENT_UI, CLIENT_SPR)
        if root.exists()
        for path in root.rglob("*")
        if path.is_file() and path.suffix.lower() == ".spr"
    ]
    avatar_tokens = ("avatar", "face", "newplayer", "selplayer", "/npc/series/")
    avatar_spr = [
        path
        for path in spr_paths
        if any(token in f"/{rel_source(path).lower().replace(chr(92), '/')}/" for token in avatar_tokens)
    ]
    avatar_tables = [
        path
        for path in CLIENT_SETTINGS.rglob("*")
        if path.is_file()
        and path.suffix.lower() in {".txt", ".ini"}
        and (
            path.stem.lower().endswith("res")
            or path.name.lower() in {"npcs.txt", "npcname.txt", "horse.txt"}
            or "/settings/npc/player/" in f"/{rel_source(path).lower().replace(chr(92), '/')}/"
        )
    ]
    audio_files = [
        path
        for path in CLIENT_MUSIC.rglob("*")
        if path.is_file() and path.suffix.lower() in {".mp3", ".wav", ".ogg", ".mid"}
    ] if CLIENT_MUSIC.exists() else []

    catalogs: list[tuple[str, list[dict[str, object]]]] = list(specialized)
    catalogs.extend(
        [
            (
                "setting-files.jsonl",
                file_catalog(
                    setting_files,
                    "SETTING",
                    owner_domain="content-config",
                    phase="P1",
                    acceptance_id="TEST-COVERAGE-001",
                ),
            ),
            (
                "ui-files.jsonl",
                file_catalog(
                    ui_files,
                    "UIFILE",
                    owner_domain="ui-content",
                    phase="P1",
                    acceptance_id="TEST-UI-007",
                ),
            ),
            (
                "ui-spr-assets.jsonl",
                file_catalog(
                    spr_paths,
                    "SPRITE",
                    owner_domain="ui-content",
                    phase="P1",
                    acceptance_id="TEST-UI-007",
                    evidence_status="BLOCKED_WINNER_UID",
                    metadata=classify_ui_spr,
                ),
            ),
            (
                "avatar-asset-candidates.jsonl",
                file_catalog(
                    [*avatar_spr, *avatar_tables],
                    "AVATAR",
                    owner_domain="content-avatar-audio",
                    phase="P1",
                    acceptance_id="TEST-AVATAR-001",
                    evidence_status="BLOCKED_ASSET_RESOLUTION",
                    metadata=classify_avatar_candidate,
                ),
            ),
            (
                "audio-assets.jsonl",
                file_catalog(
                    audio_files,
                    "AUDIO",
                    owner_domain="content-avatar-audio",
                    phase="P2",
                    acceptance_id="TEST-AUDIO-001",
                    evidence_status="BLOCKED_WINNER_CUE",
                    metadata=classify_audio,
                ),
            ),
            (
                "ui-lua-scripts.jsonl",
                file_catalog(
                    ui_lua,
                    "LUA",
                    owner_domain="ui-content",
                    phase="P1",
                    acceptance_id="TEST-UI-003",
                    metadata=classify_lua,
                ),
            ),
            (
                "quest-candidates.jsonl",
                file_catalog(
                    quest_lua,
                    "LUA",
                    owner_domain="npc-quest",
                    phase="P2",
                    acceptance_id="TEST-NQ-002",
                    evidence_status="BLOCKED_BEHAVIOR_CLASSIFICATION",
                    metadata=classify_lua,
                ),
            ),
            (
                "deferred-scripts.jsonl",
                file_catalog(
                    deferred_lua,
                    "LUA",
                    owner_domain="content-config",
                    phase="OUT_OF_SCOPE",
                    acceptance_id="TEST-COVERAGE-001",
                    metadata=classify_lua,
                ),
            ),
            (
                "lua-scripts.jsonl",
                file_catalog(
                    general_lua,
                    "LUA",
                    owner_domain="quest-event-lua",
                    phase="P2",
                    acceptance_id="TEST-LUA-001",
                    metadata=classify_lua,
                ),
            ),
            ("packages.jsonl", parse_packages(pak_uids)),
        ]
    )

    outputs = [write_jsonl(out / filename, records) for filename, records in catalogs]
    coverage = coverage_for([records for _, records in catalogs])
    coverage["quest"] = {
        "discovered": len(all_lua),
        "cataloged": len(all_lua),
        "owned": len(all_lua),
        "dispositioned": len(all_lua),
        "unresolved": len(general_lua),
    }
    coverage = dict(sorted(coverage.items()))
    package_records = next(records for filename, records in catalogs if filename == "packages.jsonl")
    ui_spr_records = next(records for filename, records in catalogs if filename == "ui-spr-assets.jsonl")
    avatar_records = next(records for filename, records in catalogs if filename == "avatar-asset-candidates.jsonl")
    audio_records = next(records for filename, records in catalogs if filename == "audio-assets.jsonl")

    snapshot = {
        "schema_version": 2,
        "source_root": str(SOURCE_ROOT),
        "source_git": git_state(SOURCE_ROOT),
        "package_ini": {
            "path": str(PACKAGE_INI),
            "sha256": sha256_file(PACKAGE_INI),
            "authority": "active Vietnamese client manifest",
        },
        "packages": [
            {
                "order": record["package_order"],
                "name": record["package_name"],
                "configured": record["configured"],
                "present": record["present"],
                "path": record["source_path"],
                "byte_count": record["byte_count"],
                "sha256": record["source_sha256"],
                "asset_index_status": record["asset_index_status"],
                "asset_index_blocker": record["asset_index_blocker"],
                "unique_uid_count": record["unique_uid_count"],
                "uid_index_sha256": record["uid_index_sha256"],
                "winner_resolution_status": record["winner_resolution_status"],
                "blocker": record["blocker"],
            }
            for record in package_records
        ],
        "vltktool": {**git_state(VLTKTOOL), "decode_module": "decode_item_texts_vi.decode_best"},
        "generator": {
            "path": str(Path(__file__).resolve()),
            "sha256": sha256_file(Path(__file__).resolve()),
        },
        "outputs": outputs,
    }
    (out / "source-snapshot.yaml").write_text(
        yaml.safe_dump(snapshot, allow_unicode=True, sort_keys=True), encoding="utf-8"
    )
    index = {
        "schema_version": 2,
        "coverage_rule": "Discovered totals come from source_census input scopes, not emitted rows; every input needs exactly one catalog partition, owner, disposition and evidence state.",
        "catalogs": outputs,
        "coverage": coverage,
        "owner_registry": {
                "skills": "DOM-SKL",
                "npc-world": "DOM-NQ",
                "items-economy": "DOM-IIEL",
                "world-map": "DOM-WMM",
                "content-config": "DOM-CONTENT",
                "ui-content": "DOM-UI",
                "content-avatar-audio": "DOM-CONTENT",
                "npc-quest": "DOM-NQ",
                "quest-event-lua": "DOM-NQ",
        },
        "source_census": {
                "skill": {"records": len(specialized[0][1]), "path": rel_source(CLIENT_SETTINGS / "Skills.txt"), "source_sha256": sha256_file(CLIENT_SETTINGS / "Skills.txt")},
                "missile": {"records": len(specialized[1][1]), "path": rel_source(CLIENT_SETTINGS / "Missles.txt"), "source_sha256": sha256_file(CLIENT_SETTINGS / "Missles.txt")},
                "npc": {"records": len(specialized[2][1]), "path": rel_source(CLIENT_SETTINGS / "Npcs.txt"), "source_sha256": sha256_file(CLIENT_SETTINGS / "Npcs.txt")},
                "goods": {"records": len(specialized[3][1]), "path": rel_source(CLIENT_SETTINGS / "Goods.txt"), "source_sha256": sha256_file(CLIENT_SETTINGS / "Goods.txt")},
                "map": {"records": len(specialized[4][1]), "path": rel_source(CLIENT_SETTINGS / "MapList.ini"), "source_sha256": sha256_file(CLIENT_SETTINGS / "MapList.ini")},
                "setting": pathset_census(setting_files),
                "uifile": pathset_census(ui_files),
                "lua": pathset_census(all_lua),
                "quest": {**pathset_census(all_lua), "scope": "all Lua paths classified exactly once; unclassified remains unresolved"},
                "sprite": pathset_census(spr_paths),
                "avatar": pathset_census([*avatar_spr, *avatar_tables]),
                "audio": pathset_census(audio_files),
                "package": {"records": len(package_records), "path": rel_source(PACKAGE_INI), "source_sha256": sha256_file(PACKAGE_INI)},
        },
        "lua_partition": {
            "discovered_unique_paths": len(all_lua),
            "general": len(general_lua),
            "quest_candidates": len(quest_lua),
            "ui_runtime": len(ui_lua),
            "deferred_out_of_scope": len(deferred_lua),
            "overlap": 0,
        },
        "quest_classification_limit": (
            "Path heuristic hẹp; không chứng minh load/call graph/host API và có thể bỏ sót tên tiếng Trung."
        ),
        "asset_partitions": {
            "ui_spr_candidate_scope": count_by(ui_spr_records, "candidate_scope"),
            "avatar_candidate_kind": count_by(avatar_records, "candidate_kind"),
            "audio_source_variant": count_by(audio_records, "source_variant"),
            "package_presence": {
                "configured": len(package_records),
                "present": sum(int(bool(record["present"])) for record in package_records),
                "missing": sum(int(not bool(record["present"])) for record in package_records),
            },
        },
        "known_blockers": [
            "27 PAK present đã census unique UID index; 4 package configured còn thiếu.",
            "Package order 0 vltkcache.pak thiếu nên first-match winner toàn cục không thể chứng minh.",
            "Candidate logical path bytes/encoding không có coverage-complete guarantee từ resolve_uid.py.",
            "UI SPR/avatar/audio đã census loose bytes nhưng winner/locale/cue binding vẫn BLOCKED.",
            "Live PC runtime golden capture is unavailable.",
            "Quest/Lua behavior classification requires recursive call/host-API analysis.",
        ],
    }
    (out / "index.yaml").write_text(
        yaml.safe_dump(index, allow_unicode=True, sort_keys=True), encoding="utf-8"
    )
    print(json.dumps({"output": str(out), "catalogs": outputs}, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
