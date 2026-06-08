#!/usr/bin/env python3
"""Catalog PC Region_S trap/object records for generated map geometries.

This is metadata-only: it preserves authoritative PC Region_S Trap.dat and Obj_S.dat
records without rendering placeholder visuals. Format is from SceneDataDef.h:
- section 1: KTrapFileHead + KSPTrap[8 bytes]
- section 3: KObjFileHead + variable KSPObj records
"""
from __future__ import annotations

import argparse
import json
import re
import struct
import time
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

UNITY_ROOT = Path('/var/www/vltk-mobile')
REGION_FILE_RE = re.compile(r'^(\d+)_(\d+)_Region_S\.dat$', re.IGNORECASE)


def utc_now() -> str:
    return datetime.now(timezone.utc).strftime('%Y-%m-%dT%H:%M:%SZ')


def write_json(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


def make_meta(path: Path) -> None:
    meta = path.with_suffix(path.suffix + '.meta')
    if meta.exists():
        return
    meta.write_text(
        'fileFormatVersion: 2\n'
        f'guid: {uuid.uuid4().hex}\n'
        'DefaultImporter:\n'
        '  externalObjects: {}\n'
        '  userData:\n'
        '  assetBundleName:\n'
        '  assetBundleVariant:\n', encoding='utf-8')


def sections(data: bytes) -> tuple[int, int, list[tuple[int, int]]]:
    if len(data) < 4:
        return 0, 0, []
    count = struct.unpack_from('<I', data, 0)[0]
    if count <= 0 or count > 20:
        return 0, 0, []
    header = 4 + count * 8
    if len(data) < header:
        return 0, 0, []
    return count, header, [struct.unpack_from('<II', data, 4 + i * 8) for i in range(count)]


def clean_script(raw: bytes) -> str:
    try:
        return raw.split(b'\0', 1)[0].decode('gb2312', errors='ignore').strip()
    except Exception:
        return ''


def parse_traps(data: bytes, header: int, secs: list[tuple[int, int]], col: int, row: int) -> list[dict[str, Any]]:
    if len(secs) <= 1:
        return []
    off, length = secs[1]
    start = header + off
    if length < 12 or start + length > len(data):
        return []
    count = struct.unpack_from('<I', data, start)[0]
    pos = start + 12
    end = start + length
    traps = []
    for idx in range(count):
        if pos + 8 > end:
            break
        cell_x, cell_y, num_cell, reserved = struct.unpack_from('<BBBB', data, pos)
        trap_id = struct.unpack_from('<I', data, pos + 4)[0]
        pos += 8
        traps.append({
            'regionCol': col,
            'regionRow': row,
            'index': idx,
            'cellX': cell_x,
            'cellY': cell_y,
            'numCell': num_cell,
            'trapId': int(trap_id),
            'reserved': reserved,
        })
    return traps


def parse_objects(data: bytes, header: int, secs: list[tuple[int, int]], col: int, row: int) -> list[dict[str, Any]]:
    if len(secs) <= 3:
        return []
    off, length = secs[3]
    start = header + off
    if length < 12 or start + length > len(data):
        return []
    count = struct.unpack_from('<I', data, start)[0]
    pos = start + 12
    end = start + length
    objects = []
    for idx in range(count):
        if pos + 24 > end:
            break
        template_id = struct.unpack_from('<i', data, pos)[0]
        state = struct.unpack_from('<h', data, pos + 4)[0]
        bio_index = struct.unpack_from('<H', data, pos + 6)[0]
        x, y, z = struct.unpack_from('<iii', data, pos + 8)
        direction = struct.unpack_from('<b', data, pos + 20)[0]
        skip_paint = data[pos + 21] != 0
        script_len = struct.unpack_from('<H', data, pos + 22)[0]
        pos += 24
        script = ''
        if script_len > 0 and pos + script_len <= end:
            script = clean_script(data[pos:pos + script_len])
            pos += script_len
        objects.append({
            'regionCol': col,
            'regionRow': row,
            'index': idx,
            'templateId': template_id,
            'state': state,
            'bioIndex': bio_index,
            'mpsX': x,
            'mpsY': y,
            'z': z,
            'direction': direction,
            'skipPaint': skip_paint,
            'script': script,
        })
    return objects


def parse_region_file(path: Path) -> tuple[list[dict[str, Any]], list[dict[str, Any]]]:
    m = REGION_FILE_RE.match(path.name)
    if not m:
        return [], []
    col, row = int(m.group(1)), int(m.group(2))
    data = path.read_bytes()
    _, header, secs = sections(data)
    if not secs:
        return [], []
    return parse_traps(data, header, secs, col, row), parse_objects(data, header, secs, col, row)


def load_server_catalog(path: Path) -> dict[str, dict[str, Any]]:
    data = json.loads(path.read_text(encoding='utf-8'))
    result = {}
    for entry in data.get('geometries', []):
        key = entry.get('geometryKey')
        if key:
            result[key] = entry
    return result


def build_catalog(region_root: Path, server_catalog_path: Path) -> tuple[list[dict[str, Any]], dict[str, Any]]:
    server = load_server_catalog(server_catalog_path)
    geometries = []
    total_traps = 0
    total_objects = 0
    unique_trap_ids = set()
    unique_obj_templates = set()
    for folder in sorted(p for p in region_root.iterdir() if p.is_dir()):
        key = folder.name
        server_entry = server.get(key, {})
        traps: list[dict[str, Any]] = []
        objects: list[dict[str, Any]] = []
        for region_file in sorted(folder.glob('*_Region_S.dat')):
            file_traps, file_objects = parse_region_file(region_file)
            traps.extend(file_traps)
            objects.extend(file_objects)
        for trap in traps:
            unique_trap_ids.add(trap['trapId'])
        for obj in objects:
            unique_obj_templates.add(obj['templateId'])
        total_traps += len(traps)
        total_objects += len(objects)
        geometries.append({
            'geometryKey': key,
            'primaryMapId': server_entry.get('primaryMapId', 0),
            'mapIds': server_entry.get('mapIds', []),
            'pcMapPath': server_entry.get('pcMapPath', ''),
            'serverMapPath': server_entry.get('serverMapPath', server_entry.get('pcMapPath', '')),
            'trapCount': len(traps),
            'objectCount': len(objects),
            'traps': traps,
            'objects': objects,
        })
    coverage = {
        'totalGeometries': len(geometries),
        'geometriesWithTraps': sum(1 for g in geometries if g['trapCount'] > 0),
        'geometriesWithObjects': sum(1 for g in geometries if g['objectCount'] > 0),
        'trapEntries': total_traps,
        'objectEntries': total_objects,
        'uniqueTrapIds': len(unique_trap_ids),
        'uniqueObjectTemplates': len(unique_obj_templates),
    }
    return geometries, coverage


def main() -> int:
    parser = argparse.ArgumentParser(description='Catalog PC Region_S trap/object records.')
    parser.add_argument('--unity-root', type=Path, default=UNITY_ROOT)
    parser.add_argument('--region-root', type=Path, default=None)
    parser.add_argument('--server-catalog', type=Path, default=None)
    parser.add_argument('--catalog-root', type=Path, default=None)
    args = parser.parse_args()

    unity_root = args.unity_root.resolve()
    region_root = args.region_root or unity_root / 'Assets/StreamingAssets/Generated/MapServerRegions'
    server_catalog = args.server_catalog or unity_root / 'Assets/StreamingAssets/MapServerRegionCatalog.json'
    catalog_root = args.catalog_root or unity_root / 'Assets/StreamingAssets'

    start = time.time()
    geometries, coverage = build_catalog(region_root, server_catalog)
    now = utc_now()
    catalog = {
        'schemaVersion': 1,
        'generatedAtUtc': now,
        'sourceRegionRoot': str(region_root),
        'sourceServerCatalog': str(server_catalog),
        'formatSource': 'Assets/StreamingAssets/Reference/SceneDataDef.h KSPTrap/KSPObj',
        'geometries': geometries,
    }
    coverage_payload = {
        'schemaVersion': 1,
        'generatedAtUtc': now,
        'phase': 'phase4_region_s_trap_object_catalog',
        'scope': 'metadata-only PC Region_S trap/object catalog; no placeholder visuals rendered',
        'sourceRegionRoot': str(region_root),
        'sourceServerCatalog': str(server_catalog),
        **coverage,
        'elapsedSeconds': round(time.time() - start, 3),
    }
    catalog_path = catalog_root / 'MapInteractiveCatalog.json'
    coverage_path = catalog_root / 'MapInteractiveCoverage.json'
    write_json(catalog_path, catalog)
    write_json(coverage_path, coverage_payload)
    make_meta(catalog_path)
    make_meta(coverage_path)
    print(json.dumps(coverage_payload, ensure_ascii=False, indent=2))
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
