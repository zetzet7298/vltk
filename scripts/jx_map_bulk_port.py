#!/usr/bin/env python3
"""Bulk visual-map port planner/extractor for JX/VLTK PC maps.

Phase 1 scope: visual Region_C + map art only. NPC/trap/mission logic is handled by
separate enemy/mission port workflows. This script deduplicates PC map IDs that share
the same visual map path and writes compact runtime alias/geometry catalogs; raw
Region_C/SPR files are intentionally emitted under ignored StreamingAssets/Generated.
"""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
import re
import shutil
import sys
import time
from pathlib import Path
from typing import Any

UNITY_ROOT = Path('/var/www/vltk-mobile')
PC_ROOT = Path('/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem')
JX_MAP_PORT_CANDIDATES = (
    UNITY_ROOT / 'harness/.agents/skills/jx-map-port/scripts/jx_map_port.py',
    UNITY_ROOT / 'stale-harness/.agents/skills/jx-map-port/scripts/jx_map_port.py',
)
VLTK_DECODE = Path('~/Projects/vltktool/decode_item_texts_vi.py')
REGION_SCENE_WIDTH = 512
REGION_SCENE_HEIGHT = 1024
GROUND_CELL = 32


def load_module(path: Path, name: str):
    spec = importlib.util.spec_from_file_location(name, path)
    if spec is None or spec.loader is None:
        raise RuntimeError(f'Cannot import {path}')
    mod = importlib.util.module_from_spec(spec)
    sys.modules[name] = mod
    spec.loader.exec_module(mod)
    return mod


def resolve_jx_map_port() -> Path:
    for candidate in JX_MAP_PORT_CANDIDATES:
        if candidate.is_file():
            return candidate
    tried = ', '.join(str(p) for p in JX_MAP_PORT_CANDIDATES)
    raise RuntimeError(f'Cannot find repository-local jx_map_port.py; tried {tried}')


jx = load_module(resolve_jx_map_port(), 'jx_map_port_single')
try:
    decoder = load_module(VLTK_DECODE, 'vltk_decode_text')
except Exception:
    decoder = None


def decode_gb(raw: bytes) -> str:
    return raw.decode('gb18030', errors='replace').strip().strip('"')


def decode_legacy_vi(raw: bytes) -> str:
    if decoder is not None:
        try:
            tcvn3 = decoder.tcvn3_to_unicode(raw.strip().decode('cp1252', errors='replace'))
            if any(ch in decoder.VIET_CHARS for ch in tcvn3):
                return tcvn3.strip().strip('"')
            text, _enc, _score = decoder.decode_best(raw.strip())
            return text.strip().strip('"')
        except Exception:
            pass
    return raw.decode('cp1252', errors='replace').strip().strip('"')


def geometry_key(pc_map_path: str) -> str:
    digest = hashlib.sha1(pc_map_path.encode('gb18030', errors='replace')).hexdigest()[:16]
    return f'g_{digest}'


def runtime_bounds_from_cols(min_col: int, min_row: int, max_col: int, max_row: int) -> dict[str, float]:
    width_regions = max_col - min_col + 1
    height_regions = max_row - min_row + 1
    width = width_regions * float(REGION_SCENE_WIDTH)
    height = height_regions * float(REGION_SCENE_WIDTH)
    return {
        'x': min_col * float(REGION_SCENE_WIDTH),
        'y': -(min_row * float(REGION_SCENE_WIDTH)) - height,
        'width': width,
        'height': height,
    }


def parse_maplist(maplist_path: Path) -> list[dict[str, Any]]:
    groups: dict[int, dict[str, Any]] = {}
    for raw_line in maplist_path.read_bytes().splitlines():
        line = raw_line.strip()
        if not line or line.startswith((b';', b'#')) or line.startswith(b'['):
            continue
        if b'=' not in line:
            continue
        key_raw, value_raw = line.split(b'=', 1)
        key = key_raw.decode('ascii', errors='ignore').strip()
        value = value_raw.strip()
        if not key:
            continue
        if key.isdigit():
            mid = int(key)
            row = groups.setdefault(mid, {'mapId': mid})
            row['pcMapPath'] = decode_gb(value)
            continue
        if '_' not in key:
            continue
        mid_str, sub_key = key.split('_', 1)
        if not mid_str.isdigit():
            continue
        mid = int(mid_str)
        row = groups.setdefault(mid, {'mapId': mid})
        sub = sub_key.lower()
        if sub == 'name':
            row['nameVi'] = decode_legacy_vi(value)
        elif sub == 'maptype':
            row['mapType'] = value.decode('ascii', errors='replace').strip()
        elif sub == 'mappos':
            parts = value.decode('ascii', errors='replace').split(',')
            if len(parts) >= 2:
                try:
                    row['mapPosX'] = int(parts[0].strip())
                    row['mapPosY'] = int(parts[1].strip())
                except ValueError:
                    pass

    rows = [v for v in groups.values() if v.get('mapId', 0) > 0 and v.get('pcMapPath')]
    rows.sort(key=lambda x: x['mapId'])
    return rows


def group_by_geometry(rows: list[dict[str, Any]]) -> list[dict[str, Any]]:
    by_path: dict[str, list[dict[str, Any]]] = {}
    for row in rows:
        by_path.setdefault(row['pcMapPath'], []).append(row)
    groups = []
    for pc_path, aliases in by_path.items():
        gids = [a['mapId'] for a in aliases]
        groups.append({
            'geometryKey': geometry_key(pc_path),
            'pcMapPath': pc_path,
            'primaryMapId': min(gids),
            'mapIds': sorted(gids),
            'aliases': aliases,
        })
    groups.sort(key=lambda g: g['primaryMapId'])
    return groups


def read_pak_entry(index: dict[int, Any], resource_path: str) -> bytes | None:
    uid = jx.g_filename2id(resource_path.encode('gbk', errors='ignore'))
    if uid not in index:
        return None
    raw, method, dsize = jx.read_entry(index[uid])
    return raw if method == 0 else jx.ucl_decompress(raw, method, dsize)


def read_wor_rect(index: dict[int, Any], pc_map_path: str) -> tuple[int, int, int, int] | None:
    data = read_pak_entry(index, f'\\maps\\{pc_map_path}.wor')
    if not data:
        return None
    text = data.decode('gb18030', errors='replace')
    match = re.search(r'^\s*rect\s*=\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)', text, re.I | re.M)
    if not match:
        return None
    vals = tuple(int(match.group(i)) for i in range(1, 5))
    min_col, min_row, max_col, max_row = vals
    if max_col < min_col or max_row < min_row:
        return None
    return vals


def iter_scan_cells(rect: tuple[int, int, int, int] | None, scan_max: int, scan_pad: int):
    if rect is not None:
        min_col, min_row, max_col, max_row = rect
        min_col = max(0, min_col - scan_pad)
        min_row = max(0, min_row - scan_pad)
        max_col += scan_pad
        max_row += scan_pad
    else:
        min_col = min_row = 0
        max_col = max_row = scan_max
    for col in range(min_col, max_col + 1):
        for row in range(min_row, max_row + 1):
            yield col, row


def read_region(index: dict[int, Any], pc_map_path: str, col: int, row: int) -> tuple[bytes | None, list[Any] | None, str | None]:
    resource_path = f'\\maps\\{pc_map_path}\\v_{row:03d}\\{col:03d}_Region_C.dat'
    uid = jx.g_filename2id(resource_path.encode('gbk', errors='ignore'))
    if uid not in index:
        return None, None, 'missing'
    raw, method, dsize = jx.read_entry(index[uid])
    data = jx.ucl_decompress(raw, method, dsize)
    if not data:
        return None, None, 'decompress failed'
    try:
        _sc, _h, sections = jx.parse_sections(data)
    except Exception as ex:
        return None, None, str(ex)
    return data, sections, None


def discover_regions(index: dict[int, Any], pc_map_path: str, rect: tuple[int, int, int, int] | None,
                     scan_max: int, scan_pad: int) -> dict[str, Any]:
    regions: list[dict[str, Any]] = []
    image_names: set[str] = set()
    invalid: list[dict[str, Any]] = []
    attempted = 0
    raw_hits = 0
    for col, row in iter_scan_cells(rect, scan_max, scan_pad):
        attempted += 1
        data, sections, reason = read_region(index, pc_map_path, col, row)
        if reason == 'missing':
            continue
        raw_hits += 1
        if data is None or sections is None:
            invalid.append({'col': col, 'row': row, 'reason': reason or 'invalid'})
            continue
        try:
            image_names |= jx.collect_names(data)
        except Exception as ex:
            invalid.append({'col': col, 'row': row, 'reason': f'collect_names: {ex}'})
        regions.append({
            'col': col,
            'row': row,
            'hasGround': len(sections) > 4 and sections[4][1] > 0,
            'hasBuiltin': len(sections) > 5 and sections[5][1] > 0,
            'size': len(data),
        })

    if not regions:
        return {
            'status': 'failed',
            'attemptedCells': attempted,
            'rawHits': raw_hits,
            'regions': [],
            'imageNames': sorted(image_names),
            'invalid': invalid,
        }

    min_col = min(r['col'] for r in regions)
    max_col = max(r['col'] for r in regions)
    min_row = min(r['row'] for r in regions)
    max_row = max(r['row'] for r in regions)
    return {
        'status': 'discovered',
        'attemptedCells': attempted,
        'rawHits': raw_hits,
        'regions': regions,
        'imageNames': sorted(image_names),
        'invalid': invalid,
        'minCol': min_col,
        'minRow': min_row,
        'maxCol': max_col,
        'maxRow': max_row,
        'regionCountX': max_col - min_col + 1,
        'regionCountY': max_row - min_row + 1,
        'bounds': runtime_bounds_from_cols(min_col, min_row, max_col, max_row),
    }


def stage_spr(index: dict[int, Any], by_rel: dict[str, str], source_name: str, sprite_root: Path) -> tuple[bool, str]:
    sprite_root.mkdir(parents=True, exist_ok=True)
    dest = sprite_root / f'{jx.compute_path_uid(source_name)}.spr'
    if dest.exists() and dest.stat().st_size > 0:
        return True, 'exists'
    uid = jx.g_filename2id(source_name.encode('gbk', errors='ignore'))
    if uid in index:
        raw, method, dsize = jx.read_entry(index[uid])
        flat = jx.get_flat_spr(raw, method, dsize)
        if flat:
            dest.write_bytes(flat)
            return True, 'pak'
    loose = jx.loose_path(by_rel, source_name)
    if loose:
        shutil.copy(loose, dest)
        return True, 'loose'
    return False, 'missing'


def write_json(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


def extract_geometry(index: dict[int, Any], by_rel: dict[str, str], geometry: dict[str, Any], generated_root: Path,
                     clean: bool, skip_spr: bool) -> dict[str, Any]:
    region_root = generated_root / 'MapRegions' / geometry['geometryKey']
    sprite_root = generated_root / 'MapSprites'
    if clean and region_root.exists():
        shutil.rmtree(region_root)
    region_root.mkdir(parents=True, exist_ok=True)

    image_names: set[str] = set()
    written = 0
    invalid: list[dict[str, Any]] = []
    manifest_regions: list[dict[str, Any]] = []
    for r in geometry.get('regions', []):
        col, row = int(r['col']), int(r['row'])
        data, sections, reason = read_region(index, geometry['pcMapPath'], col, row)
        if data is None or sections is None:
            invalid.append({'col': col, 'row': row, 'reason': reason or 'invalid'})
            continue
        try:
            image_names |= jx.collect_names(data)
        except Exception as ex:
            invalid.append({'col': col, 'row': row, 'reason': f'collect_names: {ex}'})
        (region_root / f'{col}_{row}_Region_C.dat').write_bytes(data)
        written += 1
        manifest_regions.append({
            'col': col,
            'row': row,
            'hasGround': len(sections) > 4 and sections[4][1] > 0,
            'hasBuiltin': len(sections) > 5 and sections[5][1] > 0,
            'size': len(data),
        })

    staged = 0
    failed_spr: list[dict[str, str]] = []
    if not skip_spr:
        for name in sorted(image_names):
            ok, source = stage_spr(index, by_rel, name, sprite_root)
            if ok:
                staged += 1
            else:
                failed_spr.append({'name': name, 'reason': source})

    manifest = {
        'geometryKey': geometry['geometryKey'],
        'primaryMapId': geometry['primaryMapId'],
        'mapIds': geometry['mapIds'],
        'name': geometry['pcMapPath'].split('\\')[-1],
        'pcMapPath': geometry['pcMapPath'],
        'source': 'client paks via g_FileName2Id signed-byte hash',
        'regionSceneWidth': REGION_SCENE_WIDTH,
        'regionSceneHeight': REGION_SCENE_HEIGHT,
        'groundCell': GROUND_CELL,
        'screenYScale': 0.5,
        'regions': manifest_regions,
    }
    write_json(region_root / 'manifest.json', manifest)
    write_json(region_root / 'image_names.json', sorted(image_names))
    report = {
        'geometryKey': geometry['geometryKey'],
        'pcMapPath': geometry['pcMapPath'],
        'writtenRegions': written,
        'plannedRegions': len(geometry.get('regions', [])),
        'imageNameCount': len(image_names),
        'stagedSprites': staged,
        'failedSprites': failed_spr,
        'invalid': invalid,
    }
    write_json(region_root / 'extract_report.json', report)
    return report


def compact_geometry(g: dict[str, Any]) -> dict[str, Any]:
    return {
        'geometryKey': g['geometryKey'],
        'pcMapPath': g['pcMapPath'],
        'primaryMapId': g['primaryMapId'],
        'mapIds': g['mapIds'],
        'regionFolder': f'Generated/MapRegions/{g["geometryKey"]}',
        'spriteFolder': 'Generated/MapSprites',
        'status': g.get('status', 'failed'),
        'hasWor': bool(g.get('hasWor')),
        'worRect': list(g['worRect']) if g.get('worRect') else None,
        'minCol': int(g.get('minCol', 0)),
        'minRow': int(g.get('minRow', 0)),
        'maxCol': int(g.get('maxCol', 0)),
        'maxRow': int(g.get('maxRow', 0)),
        'regionCountX': int(g.get('regionCountX', 0)),
        'regionCountY': int(g.get('regionCountY', 0)),
        'regionCount': len(g.get('regions', [])),
        'imageNameCount': len(g.get('imageNames', [])),
        'attemptedCells': int(g.get('attemptedCells', 0)),
        'rawHits': int(g.get('rawHits', 0)),
        'bounds': g.get('bounds'),
    }


def build_alias_rows(groups: list[dict[str, Any]]) -> list[dict[str, Any]]:
    aliases: list[dict[str, Any]] = []
    for g in groups:
        for a in g['aliases']:
            aliases.append({
                'mapId': a['mapId'],
                'nameVi': a.get('nameVi', ''),
                'pcMapPath': g['pcMapPath'],
                'geometryKey': g['geometryKey'],
                'primaryMapId': g['primaryMapId'],
                'mapType': a.get('mapType', ''),
                'mapPosX': int(a.get('mapPosX', 0)),
                'mapPosY': int(a.get('mapPosY', 0)),
            })
    aliases.sort(key=lambda x: x['mapId'])
    return aliases


def resolve_output_path(root: Path, value: str) -> Path:
    p = Path(value)
    return p if p.is_absolute() else root / value


def main() -> int:
    ap = argparse.ArgumentParser(description='Bulk-port/deduplicate all PC visual map geometries.')
    ap.add_argument('--unity-root', default=str(UNITY_ROOT))
    ap.add_argument('--pc-root', default=str(PC_ROOT))
    ap.add_argument('--data-dir', default=None, help='Override PC client PAK dir.')
    ap.add_argument('--maplist', default=None, help='Override settings/maplist.ini path.')
    ap.add_argument('--generated-root', default='Assets/StreamingAssets/Generated')
    ap.add_argument('--catalog-root', default='Assets/StreamingAssets')
    ap.add_argument('--extract', action='store_true', help='Write generated Region_C/SPR raw assets.')
    ap.add_argument('--skip-spr', action='store_true', help='When extracting, write regions only and do not stage SPR files.')
    ap.add_argument('--loose-fallback', action='store_true', help='If an SPR is not in PAK, scan PC loose folders as fallback.')
    ap.add_argument('--clean', action='store_true', help='Remove each geometry region folder before extraction.')
    ap.add_argument('--limit', type=int, default=0, help='Process only the first N unique visual paths (debug).')
    ap.add_argument('--only-map-id', type=int, action='append', default=[], help='Process geometries used by these map IDs only.')
    ap.add_argument('--scan-max', type=int, default=255, help='Fallback grid scan max when .wor is absent.')
    ap.add_argument('--rescue-scan-max', type=int, default=512, help='Retry failed geometries with 0..N full scan.')
    ap.add_argument('--scan-pad', type=int, default=0, help='Expand .wor rect by N cells.')
    ap.add_argument('--no-write-catalogs', action='store_true', help='Do not write Map*Catalog JSON files.')
    ap.add_argument('--strict', action='store_true', help='Return non-zero if any geometry/SPR fails.')
    args = ap.parse_args()

    unity_root = Path(args.unity_root)
    pc_root = Path(args.pc_root)
    pc_client = pc_root / 'Client 6.0'
    data_dir = Path(args.data_dir) if args.data_dir else pc_client / 'data'
    maplist_path = Path(args.maplist) if args.maplist else pc_client / 'settings/maplist.ini'
    generated_root = resolve_output_path(unity_root, args.generated_root)
    catalog_root = resolve_output_path(unity_root, args.catalog_root)

    if not maplist_path.exists():
        print(f'ERROR: maplist not found: {maplist_path}', file=sys.stderr)
        return 2
    if not data_dir.is_dir():
        print(f'ERROR: pak dir not found: {data_dir}', file=sys.stderr)
        return 2

    rows = parse_maplist(maplist_path)
    all_groups = group_by_geometry(rows)
    groups = all_groups
    if args.only_map_id:
        wanted = set(args.only_map_id)
        groups = [g for g in groups if any(mid in wanted for mid in g['mapIds'])]
    if args.limit and args.limit > 0:
        groups = groups[:args.limit]

    print(f'map rows: {len(rows)}; unique visual paths: {len(all_groups)}; processing: {len(groups)}')
    print(f'pak dir: {data_dir}')
    start = time.time()
    index = jx.build_index(str(data_dir))
    print(f'pak entries indexed: {len(index)} in {time.time() - start:.1f}s')

    by_rel: dict[str, str] = {}
    if args.extract and not args.skip_spr and args.loose_fallback:
        art_roots = [str(p) for p in [pc_client] if p.is_dir()]
        print(f'building loose SPR fallback index from {len(art_roots)} root(s)...')
        by_rel = jx.loose_index(art_roots)
        print(f'loose SPR fallback entries: {len(by_rel)}')

    planned: list[dict[str, Any]] = []
    extract_reports: list[dict[str, Any]] = []
    for idx, group in enumerate(groups, 1):
        rect = read_wor_rect(index, group['pcMapPath'])
        print(f'[{idx}/{len(groups)}] mapId={group["primaryMapId"]} aliases={len(group["mapIds"])} wor={"yes" if rect else "no"} {group["pcMapPath"]}')
        result = discover_regions(index, group['pcMapPath'], rect, args.scan_max, args.scan_pad)
        rescue_used = False
        if result.get('status') != 'discovered' and args.rescue_scan_max > args.scan_max:
            print(f'  no valid cells; retry full scan 0..{args.rescue_scan_max}')
            result = discover_regions(index, group['pcMapPath'], None, args.rescue_scan_max, 0)
            rescue_used = True

        geometry = dict(group)
        geometry.update(result)
        geometry['hasWor'] = rect is not None
        geometry['worRect'] = list(rect) if rect else None
        geometry['rescueScanUsed'] = rescue_used
        planned.append(geometry)

        if result.get('status') == 'discovered':
            print(f'  regions={len(result["regions"])} bounds={result["minCol"]},{result["minRow"]}-{result["maxCol"]},{result["maxRow"]} images={len(result.get("imageNames", []))}')
            if args.extract:
                report = extract_geometry(index, by_rel, geometry, generated_root, args.clean, args.skip_spr)
                extract_reports.append(report)
                print(f'  extracted regions={report["writtenRegions"]}/{report["plannedRegions"]} sprites={report["stagedSprites"]}/{report["imageNameCount"]} failedSpr={len(report["failedSprites"])}')
        else:
            print(f'  FAILED rawHits={result.get("rawHits", 0)} attempted={result.get("attemptedCells", 0)} invalid={len(result.get("invalid", []))}')
        sys.stdout.flush()

    discovered = [g for g in planned if g.get('status') == 'discovered']
    failed = [g for g in planned if g.get('status') != 'discovered']
    aliases = build_alias_rows(planned)
    covered_aliases = [a for a in aliases if any(g['geometryKey'] == a['geometryKey'] and g.get('status') == 'discovered' for g in planned)]
    duplicate_groups = [g for g in all_groups if len(g['mapIds']) > 1]
    partial = len(groups) != len(all_groups)
    staged_sprite_refs = sum(int(r.get('stagedSprites', 0)) for r in extract_reports)
    staged_sprite_files = 0
    if args.extract:
        sprite_dir = generated_root / 'MapSprites'
        if sprite_dir.is_dir():
            staged_sprite_files = sum(1 for _ in sprite_dir.glob('*.spr'))
    failed_sprite_names = sorted({
        failed.get('name', '')
        for report in extract_reports
        for failed in report.get('failedSprites', [])
        if failed.get('name')
    })

    coverage = {
        'schemaVersion': 1,
        'generatedAtUtc': time.strftime('%Y-%m-%dT%H:%M:%SZ', time.gmtime()),
        'phase': 'phase1_visual_region_c_spr',
        'scope': 'visual maps only; NPC/trap/mission logic excluded',
        'sourceRoot': str(pc_root),
        'sourceMaplist': str(maplist_path),
        'pakDir': str(data_dir),
        'isPartialRun': partial,
        'mapRowsTotal': len(rows),
        'mapRowsProcessed': len(aliases),
        'uniqueVisualPathsTotal': len(all_groups),
        'uniqueVisualPathsProcessed': len(planned),
        'duplicateVisualPathGroups': len(duplicate_groups),
        'duplicateAliasRowsTotal': sum(max(0, len(g['mapIds']) - 1) for g in all_groups),
        'discoveredGeometries': len(discovered),
        'failedGeometries': len(failed),
        'coveredAliases': len(covered_aliases),
        'extractedGeometries': len(extract_reports),
        'extractedRegions': sum(int(r.get('writtenRegions', 0)) for r in extract_reports),
        'stagedSprites': staged_sprite_files,
        'stagedSpriteReferences': staged_sprite_refs,
        'failedSprites': sum(len(r.get('failedSprites', [])) for r in extract_reports),
        'failedSpriteUniquePaths': failed_sprite_names,
        'failedGeometryKeys': [g['geometryKey'] for g in failed],
    }

    if not args.no_write_catalogs:
        geometry_catalog = {
            'schemaVersion': 1,
            'generatedAtUtc': coverage['generatedAtUtc'],
            'sourceMaplist': str(maplist_path),
            'isPartialRun': partial,
            'totalGeometries': len(planned),
            'geometries': [compact_geometry(g) for g in planned],
        }
        alias_catalog = {
            'schemaVersion': 1,
            'generatedAtUtc': coverage['generatedAtUtc'],
            'sourceMaplist': str(maplist_path),
            'isPartialRun': partial,
            'totalAliases': len(aliases),
            'aliases': aliases,
        }
        write_json(catalog_root / 'MapGeometryCatalog.json', geometry_catalog)
        write_json(catalog_root / 'MapAliasCatalog.json', alias_catalog)
        write_json(catalog_root / 'MapPortCoverage.json', coverage)
        print(f'wrote catalogs under {catalog_root}')

    elapsed = time.time() - start
    print('SUMMARY ' + json.dumps(coverage, ensure_ascii=False, sort_keys=True))
    print(f'elapsed: {elapsed:.1f}s')
    if args.strict and (failed or coverage['failedSprites'] > 0):
        return 1
    return 0


if __name__ == '__main__':
    sys.exit(main())
