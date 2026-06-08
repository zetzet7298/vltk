#!/usr/bin/env python3
"""Verify bulk PC->mobile map port coverage and local generated assets.

This is an audit gate, not a generator. It checks committed catalogs plus the
ignored StreamingAssets/Generated artifacts created by the bulk port scripts.
"""
from __future__ import annotations

import argparse
import json
import subprocess
import sys
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

UNITY_ROOT = Path('/var/www/vltk-mobile')
PC_ROOT = Path('/var/www/vltksource_new/vl_update_27')
EXPECTED_MAP_ALIASES = 1005
EXPECTED_RUNTIME_MAPS = 1006
EXPECTED_GEOMETRIES = 332
EXPECTED_REGION_C = 95246
EXPECTED_MAP_SPRITES = 2785
EXPECTED_SERVER_GEOMETRIES = 332
EXPECTED_STATIC_REGION_GEOMETRIES = 330
EXPECTED_STATIC_REGION_ALIASES = 1003
EXPECTED_MISSING_STATIC_ALIASES = {134, 1007}
EXPECTED_REGION_S = 84019
EXPECTED_NPC_RECORDS = 67680
EXPECTED_TRAP_RECORDS = 8692
EXPECTED_OBJECT_RECORDS = 453
EXPECTED_NPC_RES_TYPES = 375
EXPECTED_NPC_SPRITES = 1314
EXPECTED_OBJECT_TEMPLATES = 35
EXPECTED_OBJECT_SPRITES = 34
EXPECTED_TRAP_IDS = 817
EXPECTED_RESOLVED_TRAP_SCRIPTS = 816
EXPECTED_MISSING_TRAP_SCRIPTS = {'0xF51BA9A5'}
KNOWN_FAILED_MAP_SPRITES = {
    r'\system\spr\RegionTileDefault.spr',
    r'\system\spr\regiontiledefault.spr',
    r'\游戏资源\美术图素\d道路\d大理府\d道路\s石板路_03.spr',
    r'\游戏资源\美术图素\d道路\d大理府\d道路\s石板路_04.spr',
    r'\游戏资源\美术图素\沙漠区\中原崖壁雕刻.spr',
    r'\游戏资源\美术图素\野外\河提\倒影接边.spr',
}

REGENERATE_COMMANDS = [
    'python3 scripts/jx_map_bulk_port.py --extract --loose-fallback --clean',
    'python3 scripts/jx_map_server_region_port.py --extract --clean',
    'python3 scripts/jx_npc_spr_stage.py --extract --clean '
    '--extra-template-id 947 --extra-template-id 948 --extra-template-id 949 '
    '--extra-template-id 950 --extra-template-id 951 --extra-template-id 952 '
    '--extra-template-id 953 --extra-template-id 954 --extra-template-id 955 '
    '--extra-template-id 956',
    'python3 scripts/jx_region_s_interactive_catalog.py --extract --clean',
]


@dataclass
class Audit:
    errors: list[str] = field(default_factory=list)
    warnings: list[str] = field(default_factory=list)
    facts: dict[str, Any] = field(default_factory=dict)

    def require(self, ok: bool, message: str) -> None:
        if not ok:
            self.errors.append(message)

    def warn(self, message: str) -> None:
        self.warnings.append(message)


def load_json(path: Path) -> Any:
    with path.open('r', encoding='utf-8') as f:
        return json.load(f)


def count_files(path: Path, pattern: str) -> int:
    if not path.is_dir():
        return 0
    return sum(1 for _ in path.glob(pattern))


def read_pc_map_ids(maplist: Path) -> set[int]:
    ids: set[int] = set()
    for raw in maplist.read_bytes().splitlines():
        line = raw.strip()
        if not line or line.startswith((b';', b'#', b'[')) or b'=' not in line:
            continue
        key = line.split(b'=', 1)[0].strip()
        if key.isdigit():
            ids.add(int(key))
    return ids


def git_tracked_generated(unity_root: Path) -> int | None:
    try:
        out = subprocess.check_output(
            ['git', 'ls-files', 'Assets/StreamingAssets/Generated'],
            cwd=unity_root,
            text=True,
            stderr=subprocess.DEVNULL,
        )
    except Exception:
        return None
    return len([line for line in out.splitlines() if line.strip()])


def verify_visual_catalogs(audit: Audit, root: Path, pc_root: Path) -> tuple[dict[str, Any], dict[str, Any]]:
    sa = root / 'Assets/StreamingAssets'
    coverage = load_json(sa / 'MapPortCoverage.json')
    aliases = load_json(sa / 'MapAliasCatalog.json')
    geometries = load_json(sa / 'MapGeometryCatalog.json')
    pc_ids = read_pc_map_ids(pc_root / 'Client 6.0/settings/maplist.ini')
    alias_ids = {int(a['mapId']) for a in aliases.get('aliases', [])}
    geometry_keys = {g['geometryKey'] for g in geometries.get('geometries', [])}

    audit.require(len(pc_ids) == EXPECTED_MAP_ALIASES, f'PC maplist ids={len(pc_ids)} expected={EXPECTED_MAP_ALIASES}')
    audit.require(coverage.get('mapRowsTotal') == EXPECTED_MAP_ALIASES, 'MapPortCoverage.mapRowsTotal mismatch')
    audit.require(coverage.get('coveredAliases') == EXPECTED_MAP_ALIASES, 'MapPortCoverage.coveredAliases mismatch')
    audit.require(coverage.get('failedGeometries') == 0, 'MapPortCoverage.failedGeometries must be 0')
    audit.require(aliases.get('totalAliases') == EXPECTED_MAP_ALIASES, 'MapAliasCatalog.totalAliases mismatch')
    audit.require(len(alias_ids) == EXPECTED_MAP_ALIASES and alias_ids == pc_ids, 'MapAliasCatalog ids do not exactly match PC maplist')
    audit.require(geometries.get('totalGeometries') == EXPECTED_GEOMETRIES, 'MapGeometryCatalog.totalGeometries mismatch')
    audit.require(len(geometry_keys) == EXPECTED_GEOMETRIES, 'MapGeometryCatalog unique geometry count mismatch')
    failed_sprites = set(coverage.get('failedSpriteUniquePaths', []))
    if failed_sprites:
        audit.warn(f'map SPR unresolved unique paths={len(failed_sprites)} refs={coverage.get("failedSprites")}')
    audit.require(failed_sprites <= KNOWN_FAILED_MAP_SPRITES,
                  f'unexpected failed map SPR paths: {sorted(failed_sprites - KNOWN_FAILED_MAP_SPRITES)}')
    audit.facts['visualCatalog'] = {
        'pcMapIds': len(pc_ids),
        'aliases': len(alias_ids),
        'geometries': len(geometry_keys),
        'regionC': coverage.get('extractedRegions'),
        'mapSprites': coverage.get('stagedSprites'),
        'failedSpriteRefs': coverage.get('failedSprites'),
        'failedSpriteUniquePaths': sorted(failed_sprites),
    }
    return coverage, geometries


def verify_generated_visual_assets(audit: Audit, root: Path, coverage: dict[str, Any], geometries: dict[str, Any]) -> None:
    generated = root / 'Assets/StreamingAssets/Generated'
    map_regions = generated / 'MapRegions'
    map_sprites = generated / 'MapSprites'
    audit.require(map_regions.is_dir(), f'missing generated map regions folder: {map_regions}')
    audit.require(map_sprites.is_dir(), f'missing generated map sprites folder: {map_sprites}')

    total_region_c = 0
    missing_folders: list[str] = []
    mismatched_regions: list[str] = []
    for geometry in geometries.get('geometries', []):
        folder = root / 'Assets/StreamingAssets' / geometry.get('regionFolder', '')
        if not folder.is_dir():
            missing_folders.append(geometry.get('geometryKey', ''))
            continue
        count = count_files(folder, '*_Region_C.dat')
        total_region_c += count
        expected = int(geometry.get('regionCount', 0))
        if count != expected:
            mismatched_regions.append(f'{geometry.get("geometryKey")}: {count}!={expected}')

    sprite_count = count_files(map_sprites, '*.spr')
    audit.require(not missing_folders, f'missing Region_C geometry folders: {missing_folders[:8]}')
    audit.require(not mismatched_regions, f'Region_C count mismatch: {mismatched_regions[:8]}')
    audit.require(total_region_c == EXPECTED_REGION_C, f'generated Region_C files={total_region_c} expected={EXPECTED_REGION_C}')
    audit.require(sprite_count == EXPECTED_MAP_SPRITES, f'generated map SPR files={sprite_count} expected={EXPECTED_MAP_SPRITES}')
    audit.require(coverage.get('stagedSprites') == EXPECTED_MAP_SPRITES, 'MapPortCoverage.stagedSprites mismatch')
    tracked = git_tracked_generated(root)
    if tracked == 0:
        audit.warn('Assets/StreamingAssets/Generated is untracked/ignored; run regenerate commands on clean clone/build machine')
    elif tracked is not None:
        audit.warn(f'Generated tracked file count is {tracked}; verify repo policy before committing generated raw assets')
    audit.facts['generatedVisualAssets'] = {
        'regionCFolders': sum(1 for p in map_regions.iterdir() if p.is_dir()) if map_regions.is_dir() else 0,
        'regionCFiles': total_region_c,
        'mapSpriteFiles': sprite_count,
        'gitTrackedGeneratedFiles': tracked,
    }


def verify_server_region_catalogs(audit: Audit, root: Path) -> dict[str, Any]:
    sa = root / 'Assets/StreamingAssets'
    coverage = load_json(sa / 'MapSpawnCoverage.json')
    catalog = load_json(sa / 'MapServerRegionCatalog.json')
    entries = catalog.get('geometries', [])
    missing_aliases = {
        mid for entry in entries if int(entry.get('regionSCount', 0)) == 0
        for mid in entry.get('mapIds', [])
    }

    audit.require(catalog.get('totalGeometries') == EXPECTED_SERVER_GEOMETRIES, 'MapServerRegionCatalog.totalGeometries mismatch')
    audit.require(len(entries) == EXPECTED_SERVER_GEOMETRIES, 'MapServerRegionCatalog entry count mismatch')
    audit.require(coverage.get('geometriesWithRegionS') == EXPECTED_STATIC_REGION_GEOMETRIES, 'static Region_S geometry count mismatch')
    audit.require(coverage.get('coveredAliases') == EXPECTED_STATIC_REGION_ALIASES, 'static Region_S alias count mismatch')
    audit.require(missing_aliases == EXPECTED_MISSING_STATIC_ALIASES,
                  f'missing static Region_S aliases={sorted(missing_aliases)} expected={sorted(EXPECTED_MISSING_STATIC_ALIASES)}')
    audit.require(coverage.get('extractedRegionS') == EXPECTED_REGION_S, 'Region_S file count mismatch')
    audit.require(coverage.get('npcEntries') == EXPECTED_NPC_RECORDS, 'Region_S NPC record count mismatch')
    audit.require(coverage.get('trapEntries') == EXPECTED_TRAP_RECORDS, 'Region_S trap record count mismatch')
    audit.require(coverage.get('objectEntries') == EXPECTED_OBJECT_RECORDS, 'Region_S object record count mismatch')
    audit.facts['serverRegionCatalog'] = {
        'geometries': len(entries),
        'withStaticRegionS': coverage.get('geometriesWithRegionS'),
        'coveredAliases': coverage.get('coveredAliases'),
        'aliasesWithoutStaticRegionS': sorted(missing_aliases),
        'regionS': coverage.get('extractedRegionS'),
        'npcRecords': coverage.get('npcEntries'),
        'trapRecords': coverage.get('trapEntries'),
        'objectRecords': coverage.get('objectEntries'),
    }
    return catalog


def verify_generated_server_assets(audit: Audit, root: Path, catalog: dict[str, Any]) -> None:
    total = 0
    missing: list[str] = []
    mismatched: list[str] = []
    for entry in catalog.get('geometries', []):
        expected = int(entry.get('regionSCount', 0))
        if expected == 0:
            continue
        folder = root / 'Assets/StreamingAssets' / entry.get('serverRegionFolder', '')
        if not folder.is_dir():
            missing.append(entry.get('geometryKey', ''))
            continue
        count = count_files(folder, '*_Region_S.dat')
        total += count
        if count != expected:
            mismatched.append(f'{entry.get("geometryKey")}: {count}!={expected}')
    audit.require(not missing, f'missing Region_S geometry folders: {missing[:8]}')
    audit.require(not mismatched, f'Region_S count mismatch: {mismatched[:8]}')
    audit.require(total == EXPECTED_REGION_S, f'generated Region_S files={total} expected={EXPECTED_REGION_S}')
    audit.facts['generatedServerRegionAssets'] = {'regionSFiles': total}


def verify_npc_sprites(audit: Audit, root: Path) -> None:
    sa = root / 'Assets/StreamingAssets'
    coverage = load_json(sa / 'NpcSpriteCoverage.json')
    catalog = load_json(sa / 'NpcSpriteCatalog.json')
    entries = catalog.get('entries', [])
    sprite_root = sa / catalog.get('spriteFolder', 'Generated/NpcSprites')
    expected_files: set[str] = set()
    for entry in entries:
        for staged in entry.get('staged', []):
            uid = staged.get('uid')
            if uid:
                expected_files.add(uid + '.spr')
    present_files = {p.name for p in sprite_root.glob('*.spr')} if sprite_root.is_dir() else set()
    missing = sorted(expected_files - present_files)

    audit.require(coverage.get('uniqueResTypes') == EXPECTED_NPC_RES_TYPES, 'NpcSpriteCoverage.uniqueResTypes mismatch')
    audit.require(coverage.get('resTypesWithRuntimeSprite') == EXPECTED_NPC_RES_TYPES, 'NPC runtime sprite coverage mismatch')
    audit.require(coverage.get('resTypesMissingRuntimeSprite') == 0, 'NPC missing runtime sprite count must be 0')
    audit.require(len(expected_files) == EXPECTED_NPC_SPRITES, f'NPC staged sprite uid count={len(expected_files)} expected={EXPECTED_NPC_SPRITES}')
    audit.require(not missing, f'missing generated NPC SPR files: {missing[:8]}')
    audit.facts['npcSprites'] = {
        'resTypes': coverage.get('uniqueResTypes'),
        'runtimeCovered': coverage.get('resTypesWithRuntimeSprite'),
        'spriteFiles': len(present_files),
    }


def verify_interactive_catalogs(audit: Audit, root: Path) -> None:
    sa = root / 'Assets/StreamingAssets'
    interactive = load_json(sa / 'MapInteractiveCoverage.json')
    object_cov = load_json(sa / 'MapObjectSpriteCoverage.json')
    object_catalog = load_json(sa / 'MapObjectTemplateCatalog.json')
    trap_cov = load_json(sa / 'MapTrapScriptCoverage.json')
    trap_catalog = load_json(sa / 'MapTrapScriptCatalog.json')

    audit.require(interactive.get('totalGeometries') == EXPECTED_GEOMETRIES, 'MapInteractive total geometry mismatch')
    audit.require(interactive.get('trapEntries') == EXPECTED_TRAP_RECORDS, 'MapInteractive trap count mismatch')
    audit.require(interactive.get('objectEntries') == EXPECTED_OBJECT_RECORDS, 'MapInteractive object count mismatch')
    audit.require(object_cov.get('objectTemplatesResolved') == EXPECTED_OBJECT_TEMPLATES, 'object template resolved count mismatch')
    audit.require(object_cov.get('stagedObjectSpritePaths') == EXPECTED_OBJECT_SPRITES, 'object sprite staged count mismatch')
    audit.require(object_cov.get('missingObjectSpritePaths') == 0, 'object sprite missing paths must be 0')
    audit.require(trap_cov.get('uniqueTrapIds') == EXPECTED_TRAP_IDS, 'unique trap id count mismatch')
    audit.require(trap_cov.get('resolvedTrapScripts') == EXPECTED_RESOLVED_TRAP_SCRIPTS, 'resolved trap script count mismatch')
    missing_ids = set(trap_cov.get('missingTrapScriptIds', []))
    audit.require(missing_ids == EXPECTED_MISSING_TRAP_SCRIPTS,
                  f'missing trap scripts={sorted(missing_ids)} expected={sorted(EXPECTED_MISSING_TRAP_SCRIPTS)}')
    if missing_ids:
        audit.warn(f'trap scripts still source-missing: {sorted(missing_ids)}')

    sprite_root = sa / object_catalog.get('spriteFolder', 'Generated/ObjectSprites')
    expected = {t.get('uid') + '.spr' for t in object_catalog.get('templates', []) if t.get('staged') and t.get('uid')}
    present = {p.name for p in sprite_root.glob('*.spr')} if sprite_root.is_dir() else set()
    audit.require(len(expected) == EXPECTED_OBJECT_SPRITES, f'object expected sprite files={len(expected)} expected={EXPECTED_OBJECT_SPRITES}')
    audit.require(expected <= present, f'missing generated object SPR files: {sorted(expected - present)[:8]}')
    audit.require(len(trap_catalog.get('entries', [])) == EXPECTED_TRAP_IDS, 'MapTrapScriptCatalog entry count mismatch')
    audit.facts['interactive'] = {
        'traps': interactive.get('trapEntries'),
        'objects': interactive.get('objectEntries'),
        'objectTemplates': object_cov.get('objectTemplatesResolved'),
        'objectSpriteFiles': len(present),
        'trapIds': trap_cov.get('uniqueTrapIds'),
        'resolvedTrapScripts': trap_cov.get('resolvedTrapScripts'),
        'missingTrapScripts': sorted(missing_ids),
    }


def verify_default_map(audit: Audit, root: Path) -> None:
    manager = root / 'Assets/Scripts/Sandbox/SandboxManager.cs'
    text = manager.read_text(encoding='utf-8', errors='ignore')
    audit.require('public const int VuotAiNhiepThiTranMapId = 907;' in text,
                  'SandboxManager must keep Vượt ải Nhiếp Thí Trần constant at mapId 907')
    audit.require('public int defaultMapId = VuotAiNhiepThiTranMapId;' in text,
                  'SandboxManager defaultMapId must point to Vượt ải map 907')
    audit.facts['defaultMap'] = {'mapId': 907, 'nameVi': 'Vượt ải Nhiếp Thí Trần'}


def run_audit(args: argparse.Namespace) -> Audit:
    root = args.unity_root.resolve()
    pc_root = args.pc_root.resolve()
    audit = Audit()
    visual_coverage, geometries = verify_visual_catalogs(audit, root, pc_root)
    if not args.catalog_only:
        verify_generated_visual_assets(audit, root, visual_coverage, geometries)
    server_catalog = verify_server_region_catalogs(audit, root)
    if not args.catalog_only:
        verify_generated_server_assets(audit, root, server_catalog)
    verify_npc_sprites(audit, root)
    verify_interactive_catalogs(audit, root)
    verify_default_map(audit, root)
    audit.facts['regenerateCommands'] = REGENERATE_COMMANDS
    return audit


def main() -> int:
    parser = argparse.ArgumentParser(description='Verify full bulk PC map port catalogs and generated artifacts.')
    parser.add_argument('--unity-root', type=Path, default=UNITY_ROOT)
    parser.add_argument('--pc-root', type=Path, default=PC_ROOT)
    parser.add_argument('--catalog-only', action='store_true',
                        help='Do not require ignored StreamingAssets/Generated raw assets to exist locally.')
    parser.add_argument('--fail-on-known-gaps', action='store_true',
                        help='Return non-zero for known unresolved PC gaps as well as hard errors.')
    parser.add_argument('--pretty', action='store_true')
    args = parser.parse_args()

    audit = run_audit(args)
    known_gap_count = 0
    visual = audit.facts.get('visualCatalog', {})
    known_gap_count += int(visual.get('failedSpriteRefs') or 0)
    known_gap_count += len(audit.facts.get('interactive', {}).get('missingTrapScripts', []))
    status = 'pass'
    if audit.errors:
        status = 'fail'
    elif known_gap_count:
        status = 'pass_with_known_gaps'

    payload = {
        'schemaVersion': 1,
        'phase': 'full_map_port_audit',
        'status': status,
        'errors': audit.errors,
        'warnings': audit.warnings,
        'knownGapCount': known_gap_count,
        'facts': audit.facts,
    }
    print(json.dumps(payload, ensure_ascii=False, indent=2 if args.pretty else None, sort_keys=not args.pretty))
    if audit.errors:
        return 1
    if args.fail_on_known_gaps and known_gap_count:
        return 2
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
