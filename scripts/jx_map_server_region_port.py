#!/usr/bin/env python3
"""Extract PC server Region_S data for every generated visual map geometry.

Uses the already-generated MapGeometryCatalog as the source of visual map paths and
bounds, then extracts matching *_Region_S.dat files from the PC server PACK/MPS files.
Raw Region_S outputs live under ignored StreamingAssets/Generated/MapServerRegions.
"""
from __future__ import annotations

import argparse
import importlib.util
import json
import shutil
import struct
import sys
import time
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

UNITY_ROOT = Path('/var/www/vltk-mobile')
PC_ROOT = Path('/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem')
JX_MAP_PORT = UNITY_ROOT / 'harness/.agents/skills/jx-map-port/scripts/jx_map_port.py'
REGION_WARN_LIMIT = 4096


def load_module(path: Path, name: str):
    spec = importlib.util.spec_from_file_location(name, path)
    if spec is None or spec.loader is None:
        raise RuntimeError(f'Cannot import {path}')
    mod = importlib.util.module_from_spec(spec)
    sys.modules[name] = mod
    spec.loader.exec_module(mod)
    return mod


jx = load_module(JX_MAP_PORT, 'jx_map_port_single')


def write_json(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


def utc_now() -> str:
    return datetime.now(timezone.utc).strftime('%Y-%m-%dT%H:%M:%SZ')


def normalize_map_path(path: str) -> str:
    return (path or '').strip().strip('\\/').replace('/', '\\').lower()


def server_pak_dir(pc_root: Path) -> Path:
    return pc_root / 'Server 6.0/server/home_jxser_bachkim_6.0/server1/pak'


def package_order(pak_dir: Path) -> list[Path]:
    preferred = ['maps.pak', 'yanwuchang.mps', 'qianchonglou.mps', 'jingjichang.mps', 'update_map.pak', 'update3.pak']
    result: list[Path] = []
    for name in preferred:
        p = pak_dir / name
        if p.is_file():
            result.append(p)
    for p in sorted(list(pak_dir.glob('*.pak')) + list(pak_dir.glob('*.mps'))):
        if p not in result:
            result.append(p)
    return result


def build_index(paths: list[Path]) -> dict[int, tuple[str, int, int, int]]:
    index: dict[int, tuple[str, int, int, int]] = {}
    for pak in paths:
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
                    index[uid & 0xffffffff] = (str(pak), offset, size, flag)
        except Exception as ex:
            print(f'WARN: failed to index {pak}: {ex}', file=sys.stderr)
    return index


def read_region_s(index: dict[int, tuple[str, int, int, int]], pc_map_path: str, col: int, row: int) -> bytes | None:
    path = f'\\maps\\{pc_map_path}\\v_{row:03d}\\{col:03d}_Region_S.dat'
    uid = jx.g_filename2id(path.encode('gbk', errors='ignore'))
    loc = index.get(uid)
    if not loc:
        return None
    raw, method, dsize = jx.read_entry(loc)
    return raw if method == 0 else jx.ucl_decompress(raw, method, dsize)


def iter_cells(geometry: dict[str, Any], scan_max: int):
    rect = geometry.get('worRect')
    if rect and len(rect) == 4:
        min_col, min_row, max_col, max_row = [int(x) for x in rect]
    elif all(k in geometry for k in ('minCol', 'minRow', 'maxCol', 'maxRow')):
        min_col = int(geometry['minCol'])
        min_row = int(geometry['minRow'])
        max_col = int(geometry['maxCol'])
        max_row = int(geometry['maxRow'])
    else:
        min_col = min_row = 0
        max_col = max_row = scan_max
    min_col = max(0, min_col)
    min_row = max(0, min_row)
    for col in range(min_col, max_col + 1):
        for row in range(min_row, max_row + 1):
            yield col, row


def parse_region_summary(data: bytes) -> dict[str, int]:
    summary = {'sectionCount': 0, 'trapCount': 0, 'npcCount': 0, 'objCount': 0}
    if not data or len(data) < 4:
        return summary
    section_count = struct.unpack_from('<I', data, 0)[0]
    summary['sectionCount'] = int(section_count)
    if section_count <= 0 or section_count > 20:
        return summary
    header_size = 4 + int(section_count) * 8
    if len(data) < header_size:
        return summary
    sections: list[tuple[int, int]] = []
    for i in range(int(section_count)):
        off, length = struct.unpack_from('<II', data, 4 + i * 8)
        sections.append((int(off), int(length)))

    def count_u32(section_index: int, skip_head: int = 0) -> int:
        if section_index >= len(sections):
            return 0
        off, length = sections[section_index]
        if length <= 0:
            return 0
        start = header_size + off + skip_head
        if start + 4 > len(data) or start > header_size + off + length:
            return 0
        value = struct.unpack_from('<I', data, start)[0]
        return int(value) if value < 100000 else 0

    summary['trapCount'] = count_u32(1)
    summary['npcCount'] = count_u32(2)
    summary['objCount'] = count_u32(3)
    return summary


def parse_maps_txt_cells(pak_dir: Path) -> dict[str, set[tuple[int, int]]]:
    """Read maps.pak.txt path index to avoid blind full-grid scans for base server maps."""
    result: dict[str, set[tuple[int, int]]] = {}
    txt = pak_dir / 'maps.pak.txt'
    if not txt.is_file():
        return result
    try:
        lines = txt.read_text(encoding='gbk', errors='ignore').splitlines()
    except Exception as ex:
        print(f'WARN: failed to read {txt}: {ex}', file=sys.stderr)
        return result

    marker = '\\maps\\'
    suffix = '_region_s.dat'
    for line in lines:
        lower = line.lower()
        start = lower.find(marker)
        if start < 0 or suffix not in lower:
            continue
        path = line[start:].split('\t', 1)[0].strip()
        parts = path.split('\\')
        # ['', 'maps', '<region>', '<map>', 'v_095', '094_region_s.dat']
        if len(parts) < 5 or parts[1].lower() != 'maps':
            continue
        file_name = parts[-1].lower()
        folder = parts[-2].lower()
        if not folder.startswith('v_') or not file_name.endswith(suffix):
            continue
        try:
            row = int(folder[2:5])
            col = int(file_name[:3])
        except ValueError:
            continue
        pc_map_path = '\\'.join(parts[2:-2])
        result.setdefault(normalize_map_path(pc_map_path), set()).add((col, row))
    return result


def candidate_cells(geometry: dict[str, Any], listed_cells: dict[str, set[tuple[int, int]]], scan_max: int):
    seen: set[tuple[int, int]] = set()
    for cell in sorted(listed_cells.get(normalize_map_path(geometry.get('pcMapPath', '')), set())):
        seen.add(cell)
        yield cell
    for cell in iter_cells(geometry, scan_max):
        if cell in seen:
            continue
        seen.add(cell)
        yield cell


def leaf_path_aliases(listed_cells: dict[str, set[tuple[int, int]]]) -> dict[str, str]:
    by_leaf: dict[str, list[str]] = {}
    for path in listed_cells.keys():
        leaf = path.rsplit('\\', 1)[-1]
        by_leaf.setdefault(leaf, []).append(path)
    return {leaf: paths[0] for leaf, paths in by_leaf.items() if len(paths) == 1}


def rescue_cells(scan_max: int):
    for col in range(0, scan_max + 1):
        for row in range(0, scan_max + 1):
            yield col, row


def extract_geometry(index: dict[int, tuple[str, int, int, int]], geometry: dict[str, Any], out_root: Path,
                     clean: bool, scan_max: int, listed_cells: dict[str, set[tuple[int, int]]],
                     leaf_aliases: dict[str, str], rescue_scan_max: int, extract: bool) -> dict[str, Any]:
    key = geometry['geometryKey']
    folder = out_root / key
    if extract:
        if clean and folder.exists():
            shutil.rmtree(folder)
        folder.mkdir(parents=True, exist_ok=True)

    regions: list[dict[str, Any]] = []
    attempted = 0
    total_npc = total_trap = total_obj = 0
    tried: set[tuple[int, int]] = set()
    scan_geometry = dict(geometry)
    server_map_path = geometry.get('pcMapPath', '')

    for col, row in candidate_cells(scan_geometry, listed_cells, scan_max):
        tried.add((col, row))
        attempted += 1
        data = read_region_s(index, scan_geometry['pcMapPath'], col, row)
        if not data:
            continue
        summary = parse_region_summary(data)
        if extract:
            target = folder / f'{col}_{row}_Region_S.dat'
            target.write_bytes(data)
        total_npc += summary['npcCount']
        total_trap += summary['trapCount']
        total_obj += summary['objCount']
        regions.append({
            'col': col,
            'row': row,
            'size': len(data),
            'sectionCount': summary['sectionCount'],
            'npcCount': summary['npcCount'],
            'trapCount': summary['trapCount'],
            'objCount': summary['objCount'],
        })

    rescued = False
    if len(regions) == 0:
        leaf = normalize_map_path(geometry.get('pcMapPath', '')).rsplit('\\', 1)[-1]
        alias = leaf_aliases.get(leaf)
        if alias and alias != normalize_map_path(geometry.get('pcMapPath', '')):
            server_map_path = alias
            scan_geometry = dict(geometry)
            scan_geometry['pcMapPath'] = alias
            for col, row in candidate_cells(scan_geometry, listed_cells, scan_max):
                attempted += 1
                data = read_region_s(index, alias, col, row)
                if not data:
                    continue
                summary = parse_region_summary(data)
                if extract:
                    target = folder / f'{col}_{row}_Region_S.dat'
                    target.write_bytes(data)
                total_npc += summary['npcCount']
                total_trap += summary['trapCount']
                total_obj += summary['objCount']
                regions.append({
                    'col': col,
                    'row': row,
                    'size': len(data),
                    'sectionCount': summary['sectionCount'],
                    'npcCount': summary['npcCount'],
                    'trapCount': summary['trapCount'],
                    'objCount': summary['objCount'],
                })

    if len(regions) == 0 and rescue_scan_max >= 0:
        rescued = True
        for col, row in rescue_cells(rescue_scan_max):
            if (col, row) in tried:
                continue
            attempted += 1
            data = read_region_s(index, server_map_path, col, row)
            if not data:
                continue
            summary = parse_region_summary(data)
            if extract:
                target = folder / f'{col}_{row}_Region_S.dat'
                target.write_bytes(data)
            total_npc += summary['npcCount']
            total_trap += summary['trapCount']
            total_obj += summary['objCount']
            regions.append({
                'col': col,
                'row': row,
                'size': len(data),
                'sectionCount': summary['sectionCount'],
                'npcCount': summary['npcCount'],
                'trapCount': summary['trapCount'],
                'objCount': summary['objCount'],
            })

    manifest = {
        'geometryKey': key,
        'pcMapPath': geometry['pcMapPath'],
        'serverMapPath': server_map_path,
        'primaryMapId': geometry.get('primaryMapId', 0),
        'mapIds': geometry.get('mapIds', []),
        'source': 'PC server PACK/MPS Region_S via g_FileName2Id signed-byte hash',
        'serverRegionFolder': f'Generated/MapServerRegions/{key}',
        'status': 'extracted' if regions else 'no_static_region_s',
        'regions': regions,
        'regionCount': len(regions),
        'npcCount': total_npc,
        'trapCount': total_trap,
        'objCount': total_obj,
        'attemptedCells': attempted,
        'rescueScanUsed': rescued,
    }
    if extract:
        write_json(folder / 'manifest.json', manifest)
    return manifest


def load_geometries(path: Path) -> list[dict[str, Any]]:
    data = json.loads(path.read_text(encoding='utf-8'))
    geometries = data.get('geometries') if isinstance(data, dict) else data
    if not isinstance(geometries, list):
        raise RuntimeError(f'{path} does not contain a geometries array')
    return [g for g in geometries if isinstance(g, dict) and g.get('geometryKey')]


def filter_geometries(geometries: list[dict[str, Any]], only_map_id: int | None, limit: int | None) -> list[dict[str, Any]]:
    result: list[dict[str, Any]] = []
    for g in geometries:
        if only_map_id is not None:
            ids = [int(x) for x in g.get('mapIds', [])]
            if int(g.get('primaryMapId', 0)) != only_map_id and only_map_id not in ids:
                continue
        result.append(g)
        if limit is not None and len(result) >= limit:
            break
    return result


def catalog_entry(manifest: dict[str, Any]) -> dict[str, Any]:
    return {
        'geometryKey': manifest['geometryKey'],
        'pcMapPath': manifest.get('pcMapPath', ''),
        'serverMapPath': manifest.get('serverMapPath', manifest.get('pcMapPath', '')),
        'primaryMapId': manifest.get('primaryMapId', 0),
        'mapIds': manifest.get('mapIds', []),
        'serverRegionFolder': manifest.get('serverRegionFolder', ''),
        'status': manifest.get('status', 'missing'),
        'regionSCount': manifest.get('regionCount', 0),
        'npcCount': manifest.get('npcCount', 0),
        'trapCount': manifest.get('trapCount', 0),
        'objCount': manifest.get('objCount', 0),
        'attemptedCells': manifest.get('attemptedCells', 0),
        'rescueScanUsed': manifest.get('rescueScanUsed', False),
    }


def make_meta(path: Path) -> None:
    meta = path.with_suffix(path.suffix + '.meta')
    if meta.exists():
        return
    import uuid
    meta.write_text(
        'fileFormatVersion: 2\n'
        f'guid: {uuid.uuid4().hex}\n'
        'DefaultImporter:\n'
        '  externalObjects: {}\n'
        '  userData:\n'
        '  assetBundleName:\n'
        '  assetBundleVariant:\n',
        encoding='utf-8')


def main() -> int:
    parser = argparse.ArgumentParser(description='Bulk-extract PC server Region_S data for generated map geometries.')
    parser.add_argument('--unity-root', type=Path, default=UNITY_ROOT)
    parser.add_argument('--pc-root', type=Path, default=PC_ROOT)
    parser.add_argument('--geometry-catalog', type=Path, default=None)
    parser.add_argument('--generated-root', type=Path, default=None)
    parser.add_argument('--catalog-root', type=Path, default=None)
    parser.add_argument('--extract', action='store_true', help='Write Region_S files and per-geometry manifests.')
    parser.add_argument('--clean', action='store_true', help='Delete generated Region_S output before writing selected geometries.')
    parser.add_argument('--limit', type=int, default=None)
    parser.add_argument('--only-map-id', type=int, default=None)
    parser.add_argument('--scan-max', type=int, default=512)
    parser.add_argument('--rescue-scan-max', type=int, default=255,
                        help='Full rescue scan max when a geometry has no hits. Use -1 to disable.')
    parser.add_argument('--strict', action='store_true')
    args = parser.parse_args()

    unity_root = args.unity_root.resolve()
    pc_root = args.pc_root.resolve()
    geometry_catalog = args.geometry_catalog or unity_root / 'Assets/StreamingAssets/MapGeometryCatalog.json'
    generated_root = args.generated_root or unity_root / 'Assets/StreamingAssets/Generated/MapServerRegions'
    catalog_root = args.catalog_root or unity_root / 'Assets/StreamingAssets'
    pak_dir = server_pak_dir(pc_root)

    geometries_all = load_geometries(geometry_catalog)
    geometries = filter_geometries(geometries_all, args.only_map_id, args.limit)
    is_partial = len(geometries) != len(geometries_all) or args.only_map_id is not None

    packages = package_order(pak_dir)
    if not packages:
        raise RuntimeError(f'No server PACK/MPS files found in {pak_dir}')
    print(f'Indexing {len(packages)} server packages from {pak_dir}')
    index = build_index(packages)
    print(f'Indexed {len(index)} hashed entries')
    listed_cells = parse_maps_txt_cells(pak_dir)
    aliases_by_leaf = leaf_path_aliases(listed_cells)
    print(f'maps.pak.txt Region_S map paths: {len(listed_cells)}')

    if args.clean and args.extract and not is_partial and generated_root.exists():
        shutil.rmtree(generated_root)

    start = time.time()
    manifests: list[dict[str, Any]] = []
    for idx, geometry in enumerate(geometries, 1):
        manifest = extract_geometry(index, geometry, generated_root, args.clean, args.scan_max,
                                    listed_cells, aliases_by_leaf, args.rescue_scan_max, args.extract)
        manifests.append(manifest)
        if idx == 1 or idx % 25 == 0 or idx == len(geometries):
            print(f'[{idx}/{len(geometries)}] {manifest["geometryKey"]} '
                  f'regions={manifest["regionCount"]} npc={manifest["npcCount"]} '
                  f'attempted={manifest["attemptedCells"]}')

    entries = [catalog_entry(m) for m in manifests]
    extracted = [e for e in entries if int(e['regionSCount']) > 0]
    missing = [e for e in entries if int(e['regionSCount']) == 0]
    alias_total = sum(len(e.get('mapIds', [])) for e in entries)
    alias_covered = sum(len(e.get('mapIds', [])) for e in extracted)
    now = utc_now()

    server_catalog = {
        'schemaVersion': 1,
        'generatedAtUtc': now,
        'sourceRoot': str(pc_root),
        'sourcePakDir': str(pak_dir),
        'sourceGeometryCatalog': str(geometry_catalog),
        'isPartialRun': is_partial,
        'totalGeometries': len(geometries_all),
        'processedGeometries': len(entries),
        'geometries': entries,
    }
    coverage = {
        'schemaVersion': 1,
        'generatedAtUtc': now,
        'phase': 'phase2_server_region_s_spawns',
        'scope': 'server Region_S binary extraction only; final NPC visuals/combat/object/trap wiring excluded',
        'sourceRoot': str(pc_root),
        'sourcePakDir': str(pak_dir),
        'sourcePackages': [str(p) for p in packages],
        'sourceGeometryCatalog': str(geometry_catalog),
        'isPartialRun': is_partial,
        'totalGeometries': len(geometries_all),
        'processedGeometries': len(entries),
        'geometriesWithRegionS': len(extracted),
        'geometriesMissingRegionS': len(missing),
        'geometriesWithoutStaticRegionS': len(missing),
        'coveredAliases': alias_covered,
        'processedAliases': alias_total,
        'catalogedAliases': alias_total,
        'aliasesWithoutStaticRegionS': alias_total - alias_covered,
        'extractedRegionS': sum(int(e['regionSCount']) for e in entries),
        'npcEntries': sum(int(e['npcCount']) for e in entries),
        'trapEntries': sum(int(e['trapCount']) for e in entries),
        'objectEntries': sum(int(e['objCount']) for e in entries),
        'missingGeometryKeys': [e['geometryKey'] for e in missing],
        'elapsedSeconds': round(time.time() - start, 3),
    }
    catalog_root.mkdir(parents=True, exist_ok=True)
    server_catalog_path = catalog_root / 'MapServerRegionCatalog.json'
    coverage_path = catalog_root / 'MapSpawnCoverage.json'
    write_json(server_catalog_path, server_catalog)
    write_json(coverage_path, coverage)
    make_meta(server_catalog_path)
    make_meta(coverage_path)

    print(json.dumps(coverage, ensure_ascii=False, indent=2))
    if args.strict and missing:
        return 2
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
