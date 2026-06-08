#!/usr/bin/env python3
"""Catalog PC Region_S trap/object records for generated map geometries.

This is metadata-only: it preserves authoritative PC Region_S Trap.dat and Obj_S.dat
records without rendering placeholder visuals. Format is from SceneDataDef.h:
- section 1: KTrapFileHead + KSPTrap[8 bytes]
- section 3: KObjFileHead + variable KSPObj records
"""
from __future__ import annotations

import argparse
import importlib.util
import json
import os
import re
import shutil
import struct
import sys
import time
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

UNITY_ROOT = Path('/var/www/vltk-mobile')
PC_ROOT = Path('/var/www/vltksource_new/vl_update_27')
JX_MAP_PORT = UNITY_ROOT / 'harness/.codex/skills/jx-map-port/scripts/jx_map_port.py'
VLTK_DECODE = Path('/var/www/vltktool/decode_item_texts_vi.py')
REGION_FILE_RE = re.compile(r'^(\d+)_(\d+)_Region_S\.dat$', re.IGNORECASE)
KILLBOSSMATCH_MAP_IDS = set(range(907, 917))


def load_module(path: Path, name: str):
    spec = importlib.util.spec_from_file_location(name, path)
    if spec is None or spec.loader is None:
        raise RuntimeError(f'Cannot import {path}')
    module = importlib.util.module_from_spec(spec)
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


jx = load_module(JX_MAP_PORT, 'jx_map_port_interactive')
try:
    decoder = load_module(VLTK_DECODE, 'vltk_decode_interactive')
except Exception:
    decoder = None


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


def decode_legacy_text(raw: bytes) -> str:
    if decoder is not None:
        try:
            text, _enc, _score = decoder.decode_best(raw)
            return text
        except Exception:
            pass
    for enc in ('utf-8', 'gb18030', 'cp1252'):
        try:
            return raw.decode(enc)
        except Exception:
            continue
    return raw.decode('utf-8', errors='replace')


def int_col(cols: list[str], index: int) -> int:
    if index < 0 or index >= len(cols):
        return 0
    try:
        return int(cols[index].strip() or '0')
    except ValueError:
        return 0


def normalize_resource_path(path: str) -> str:
    path = (path or '').strip().replace('/', '\\')
    if path and not path.startswith('\\'):
        path = '\\' + path
    return path


def load_objdata_templates(path: Path) -> dict[int, dict[str, Any]]:
    if not path.is_file():
        return {}
    text = decode_legacy_text(path.read_bytes())
    rows = [line.split('\t') for line in text.splitlines() if line.strip()]
    templates: dict[int, dict[str, Any]] = {}
    for cols in rows[1:]:
        if len(cols) < 5:
            continue
        data_id = int_col(cols, 1)
        if data_id <= 0:
            continue
        templates[data_id] = {
            'templateId': data_id,
            'nameVi': cols[0].strip(),
            'kind': cols[2].strip() if len(cols) > 2 else '',
            'scriptName': cols[3].strip() if len(cols) > 3 else '',
            'imageName': normalize_resource_path(cols[4] if len(cols) > 4 else ''),
            'soundName': cols[5].strip() if len(cols) > 5 else '',
            'lifeTime': int_col(cols, 6),
            'layer': int_col(cols, 7),
            'height': int_col(cols, 8),
            'imageTotalFrame': int_col(cols, 21),
            'imageCurFrame': int_col(cols, 22),
            'imageTotalDir': int_col(cols, 23),
            'imageCurDir': int_col(cols, 24),
            'imageInterval': int_col(cols, 25),
            'imageCgXpos': int_col(cols, 26),
            'imageCgYpos': int_col(cols, 27),
            'isUnseen': int_col(cols, 51),
            'obstacleKind': int_col(cols, 52),
            'loopAnimation': int_col(cols, 53),
        }
    return templates


def client_package_order(pc_root: Path) -> list[Path]:
    client_root = pc_root / 'Client 6.0'
    data_dir = client_root / 'data'
    package_ini = client_root / 'package1.ini'
    result: list[Path] = []
    if package_ini.is_file():
        for line in package_ini.read_text(encoding='utf-8', errors='ignore').splitlines():
            line = line.strip()
            if not line or line.startswith('[') or '=' not in line:
                continue
            key, value = line.split('=', 1)
            if not key.strip().isdigit():
                continue
            pak = data_dir / value.strip()
            if pak.is_file():
                result.append(pak)
    return result or sorted(data_dir.glob('*.pak'))


def build_priority_index(package_paths: list[Path]) -> dict[int, tuple[str, int, int, int]]:
    index: dict[int, tuple[str, int, int, int]] = {}
    for pak in package_paths:
        try:
            with pak.open('rb') as f:
                header = f.read(32)
                if len(header) < 32 or header[:4] != b'PACK':
                    continue
                count, index_offset = struct.unpack_from('<II', header, 4)
                f.seek(index_offset)
                for _ in range(count):
                    record = f.read(16)
                    if len(record) != 16:
                        break
                    uid, offset, size, flag = struct.unpack('<IIii', record)
                    index.setdefault(uid & 0xffffffff, (str(pak), offset, size, flag))
        except Exception as ex:
            print(f'WARN: failed to index {pak}: {ex}', file=sys.stderr)
    return index


def extract_spr(index: dict[int, tuple[str, int, int, int]], by_rel: dict[str, str], source_path: str) -> bytes | None:
    source_path = normalize_resource_path(source_path)
    if not source_path:
        return None
    uid = jx.g_filename2id(source_path.encode('gbk', errors='ignore'))
    loc = index.get(uid)
    if loc:
        raw, method, dsize = jx.read_entry(loc)
        data = jx.get_flat_spr(raw, method, dsize)
        if data:
            return data
    loose = jx.loose_path(by_rel, source_path) if by_rel else None
    if loose:
        return Path(loose).read_bytes()
    return None


def stage_object_templates(templates: dict[int, dict[str, Any]], used_template_ids: set[int],
                           pc_root: Path, out_root: Path, extract: bool) -> tuple[list[dict[str, Any]], dict[str, Any]]:
    out_root.mkdir(parents=True, exist_ok=True)
    index = build_priority_index(client_package_order(pc_root))
    by_rel = jx.loose_index([str(pc_root / 'Client 6.0')])

    catalog: list[dict[str, Any]] = []
    unique_paths: set[str] = set()
    staged_paths: set[str] = set()
    missing_paths: set[str] = set()
    for template_id in sorted(used_template_ids):
        template = templates.get(template_id)
        if not template:
            catalog.append({'templateId': template_id, 'missingTemplate': True})
            continue
        image = normalize_resource_path(template.get('imageName', ''))
        uid = jx.compute_path_uid(image) if image else ''
        staged = False
        size = 0
        if image:
            unique_paths.add(image)
            data = extract_spr(index, by_rel, image)
            if data:
                staged = True
                staged_paths.add(image)
                size = len(data)
                if extract:
                    (out_root / f'{uid}.spr').write_bytes(data)
            else:
                missing_paths.add(image)
        entry = dict(template)
        entry.update({
            'sourcePath': image.lstrip('\\'),
            'uid': uid,
            'staged': staged,
            'bytes': size,
            'missingTemplate': False,
        })
        catalog.append(entry)
    coverage = {
        'objectTemplatesUsed': len(used_template_ids),
        'objectTemplatesResolved': sum(1 for e in catalog if not e.get('missingTemplate')),
        'uniqueObjectSpritePaths': len(unique_paths),
        'stagedObjectSpritePaths': len(staged_paths),
        'missingObjectSpritePaths': len(missing_paths),
        'missingObjectSprites': sorted(p.lstrip('\\') for p in missing_paths),
    }
    return catalog, coverage


def server_root(pc_root: Path) -> Path:
    return pc_root / 'Server 6.0/server/home_jxser_bachkim_6.0/server1'


def decode_gbk(raw: bytes) -> str:
    return raw.decode('gbk', errors='replace')


def decode_server_text(raw: bytes) -> str:
    for enc in ('gb18030', 'gbk', 'utf-8'):
        try:
            return raw.decode(enc)
        except Exception:
            continue
    return raw.decode('gb18030', errors='replace')


def script_action_summary(source: str) -> dict[str, Any]:
    new_world = []
    for match in re.finditer(r'\bNewWorld\s*\(([^)]*)\)', source):
        args = [part.strip() for part in match.group(1).split(',')]
        new_world.append(args[:3])
    set_pos = []
    for match in re.finditer(r'\bSetPos\s*\(([^)]*)\)', source):
        args = [part.strip() for part in match.group(1).split(',')]
        set_pos.append(args[:2])
    return {
        'hasMain': re.search(r'function\s+main\s*\(', source) is not None,
        'newWorldCalls': new_world[:8],
        'setPosCalls': set_pos[:8],
        'setsFightState': 'SetFightState' in source,
        'talks': 'Talk(' in source or 'Msg2Player' in source,
    }


def build_script_hash_index(root: Path) -> dict[int, dict[str, Any]]:
    root_b = os.fsencode(root)
    index: dict[int, dict[str, Any]] = {}
    if not os.path.isdir(root_b):
        return index
    for dirpath, _dirnames, filenames in os.walk(root_b):
        for filename in filenames:
            if not filename.lower().endswith((b'.lua', b'.txt', b'.ini', b'.cfg', b'.tab')):
                continue
            full = os.path.join(dirpath, filename)
            rel = os.path.relpath(full, root_b).replace(os.sep.encode(), b'\\')
            source_bytes = b'\\' + rel
            script_id = jx.g_filename2id(source_bytes)
            entry = index.setdefault(script_id, {
                'trapId': script_id,
                'trapIdHex': f'0x{script_id:08X}',
                'scriptPath': decode_gbk(source_bytes),
                'sourceRelPath': decode_gbk(rel),
                'sourceFile': os.fsdecode(full),
            })
            if source_bytes.lower().startswith(b'\\script') and not entry['scriptPath'].lower().startswith('\\script'):
                entry.update({
                    'scriptPath': decode_gbk(source_bytes),
                    'sourceRelPath': decode_gbk(rel),
                    'sourceFile': os.fsdecode(full),
                })
    return index


def build_trap_script_catalog(trap_ids: set[int], pc_root: Path) -> tuple[list[dict[str, Any]], dict[str, Any]]:
    root = server_root(pc_root)
    script_index = build_script_hash_index(root)
    entries: list[dict[str, Any]] = []
    resolved = 0
    for trap_id in sorted(trap_ids):
        src = script_index.get(trap_id)
        entry = {
            'trapId': trap_id,
            'trapIdHex': f'0x{trap_id:08X}',
            'resolved': src is not None,
        }
        if src:
            resolved += 1
            try:
                text = decode_server_text(Path(src['sourceFile']).read_bytes())
            except Exception:
                text = ''
            entry.update({
                'scriptPath': src['scriptPath'],
                'sourceRelPath': src['sourceRelPath'],
                'actions': script_action_summary(text),
            })
        entries.append(entry)
    coverage = {
        'uniqueTrapIds': len(trap_ids),
        'resolvedTrapScripts': resolved,
        'missingTrapScripts': len(trap_ids) - resolved,
        'missingTrapScriptIds': [f'0x{e["trapId"]:08X}' for e in entries if not e.get('resolved')],
        'sourceScriptRoot': str(root / 'script'),
    }
    return entries, coverage


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


def enrich_object(obj: dict[str, Any], templates: dict[int, dict[str, Any]]) -> dict[str, Any]:
    template = templates.get(int(obj.get('templateId', 0)))
    if not template:
        return obj
    obj = dict(obj)
    obj.update({
        'nameVi': template.get('nameVi', ''),
        'kind': template.get('kind', ''),
        'imageName': template.get('imageName', ''),
        'imageUid': jx.compute_path_uid(template.get('imageName', '')) if template.get('imageName') else '',
        'imageCurFrame': template.get('imageCurFrame', 0),
        'imageCurDir': template.get('imageCurDir', 0),
        'imageTotalFrame': template.get('imageTotalFrame', 0),
        'imageTotalDir': template.get('imageTotalDir', 0),
        'imageInterval': template.get('imageInterval', 0),
        'imageCgXpos': template.get('imageCgXpos', 0),
        'imageCgYpos': template.get('imageCgYpos', 0),
        'height': template.get('height', 0),
        'layer': template.get('layer', 0),
        'isUnseen': template.get('isUnseen', 0),
        'obstacleKind': template.get('obstacleKind', 0),
        'loopAnimation': template.get('loopAnimation', 0),
    })
    return obj


def enrich_traps_in_geometries(geometries: list[dict[str, Any]], trap_scripts: dict[int, dict[str, Any]]) -> None:
    for geometry in geometries:
        inactive_maps = [mid for mid in geometry.get('mapIds', []) if mid in KILLBOSSMATCH_MAP_IDS]
        if inactive_maps:
            geometry['staticTrapClearMapIds'] = inactive_maps
        for trap in geometry.get('traps', []):
            trap_id = int(trap.get('trapId', 0))
            script = trap_scripts.get(trap_id)
            trap['trapIdHex'] = f'0x{trap_id:08X}'
            trap['scriptResolved'] = bool(script and script.get('resolved'))
            if script and script.get('resolved'):
                trap['scriptPath'] = script.get('scriptPath', '')
            if inactive_maps:
                trap['inactiveMapIds'] = inactive_maps


def build_catalog(region_root: Path, server_catalog_path: Path,
                  obj_templates: dict[int, dict[str, Any]]) -> tuple[list[dict[str, Any]], dict[str, Any], set[int], set[int]]:
    server = load_server_catalog(server_catalog_path)
    geometries = []
    total_traps = 0
    total_objects = 0
    unique_trap_ids = set()
    unique_obj_templates = set()
    used_template_ids: set[int] = set()
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
        objects = [enrich_object(obj, obj_templates) for obj in objects]
        for obj in objects:
            template_id = obj['templateId']
            unique_obj_templates.add(template_id)
            used_template_ids.add(template_id)
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
    return geometries, coverage, used_template_ids, unique_trap_ids


def main() -> int:
    parser = argparse.ArgumentParser(description='Catalog PC Region_S trap/object records.')
    parser.add_argument('--unity-root', type=Path, default=UNITY_ROOT)
    parser.add_argument('--pc-root', type=Path, default=PC_ROOT)
    parser.add_argument('--region-root', type=Path, default=None)
    parser.add_argument('--server-catalog', type=Path, default=None)
    parser.add_argument('--catalog-root', type=Path, default=None)
    parser.add_argument('--objdata', type=Path, default=None)
    parser.add_argument('--object-sprite-root', type=Path, default=None)
    parser.add_argument('--extract', action='store_true', help='write staged object SPR files under Generated/ObjectSprites')
    parser.add_argument('--clean', action='store_true', help='clean Generated/ObjectSprites before extracting')
    args = parser.parse_args()

    unity_root = args.unity_root.resolve()
    pc_root = args.pc_root.resolve()
    region_root = args.region_root or unity_root / 'Assets/StreamingAssets/Generated/MapServerRegions'
    server_catalog = args.server_catalog or unity_root / 'Assets/StreamingAssets/MapServerRegionCatalog.json'
    catalog_root = args.catalog_root or unity_root / 'Assets/StreamingAssets'
    objdata = args.objdata or unity_root / 'Assets/StreamingAssets/Reference/PcObj/objdata.txt'
    object_sprite_root = args.object_sprite_root or unity_root / 'Assets/StreamingAssets/Generated/ObjectSprites'
    if args.clean and object_sprite_root.exists():
        shutil.rmtree(object_sprite_root)

    start = time.time()
    obj_templates = load_objdata_templates(objdata)
    geometries, coverage, used_template_ids, used_trap_ids = build_catalog(region_root, server_catalog, obj_templates)
    trap_scripts, trap_coverage = build_trap_script_catalog(used_trap_ids, pc_root)
    enrich_traps_in_geometries(geometries, {entry['trapId']: entry for entry in trap_scripts})
    object_templates, object_coverage = stage_object_templates(
        obj_templates, used_template_ids, pc_root, object_sprite_root, args.extract)
    now = utc_now()
    catalog = {
        'schemaVersion': 1,
        'generatedAtUtc': now,
        'sourceRegionRoot': str(region_root),
        'sourceServerCatalog': str(server_catalog),
        'formatSource': 'Assets/StreamingAssets/Reference/SceneDataDef.h KSPTrap/KSPObj',
        'geometries': geometries,
    }
    object_catalog = {
        'schemaVersion': 1,
        'generatedAtUtc': now,
        'sourceObjData': str(objdata),
        'sourcePakOrder': str(pc_root / 'Client 6.0/package1.ini'),
        'spriteFolder': 'Generated/ObjectSprites',
        'templates': object_templates,
    }
    trap_catalog = {
        'schemaVersion': 1,
        'generatedAtUtc': now,
        'sourceScriptRoot': str(server_root(pc_root) / 'script'),
        'hashRule': 'PC g_FileName2Id signed-char over leading-backslash GBK script path',
        'entries': trap_scripts,
    }
    coverage_payload = {
        'schemaVersion': 1,
        'generatedAtUtc': now,
        'phase': 'phase4_region_s_trap_object_catalog',
        'scope': 'PC Region_S trap/object catalog + exact ObjData SPR staging; traps remain invisible metadata/no gameplay placeholder',
        'sourceRegionRoot': str(region_root),
        'sourceServerCatalog': str(server_catalog),
        'sourceObjData': str(objdata),
        **coverage,
        **object_coverage,
        **trap_coverage,
        'elapsedSeconds': round(time.time() - start, 3),
    }
    catalog_path = catalog_root / 'MapInteractiveCatalog.json'
    object_catalog_path = catalog_root / 'MapObjectTemplateCatalog.json'
    trap_catalog_path = catalog_root / 'MapTrapScriptCatalog.json'
    coverage_path = catalog_root / 'MapInteractiveCoverage.json'
    object_coverage_path = catalog_root / 'MapObjectSpriteCoverage.json'
    trap_coverage_path = catalog_root / 'MapTrapScriptCoverage.json'
    write_json(catalog_path, catalog)
    write_json(object_catalog_path, object_catalog)
    write_json(trap_catalog_path, trap_catalog)
    write_json(coverage_path, coverage_payload)
    write_json(object_coverage_path, {'schemaVersion': 1, 'generatedAtUtc': now, **object_coverage})
    write_json(trap_coverage_path, {'schemaVersion': 1, 'generatedAtUtc': now, **trap_coverage})
    make_meta(catalog_path)
    make_meta(object_catalog_path)
    make_meta(trap_catalog_path)
    make_meta(coverage_path)
    make_meta(object_coverage_path)
    make_meta(trap_coverage_path)
    print(json.dumps(coverage_payload, ensure_ascii=False, indent=2))
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
