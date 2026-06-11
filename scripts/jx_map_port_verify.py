#!/usr/bin/env python3
"""Verify bulk PC->mobile map port coverage and local generated assets.

This is an audit gate, not a generator. It checks committed catalogs plus the
ignored StreamingAssets/Generated artifacts created by the bulk port scripts.
"""
from __future__ import annotations

import argparse
import importlib.util
import json
import subprocess
import sys
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

UNITY_ROOT = Path('/var/www/vltk-mobile')
PC_ROOT = Path('/var/www/vltksource_new/vl_update_27')
JX_MAP_PORT_REL = Path('harness/.codex/skills/jx-map-port/scripts/jx_map_port.py')
SPR_LABEL_MAP = Path('/var/www/vltksource_new/vl_update_27/pak_unpacked/_labels.json')
SPR_UNPACK_MANIFEST = Path('/var/www/vltksource_new/vl_update_27/pak_unpacked/_unpack_summary.json')
PROVENANCE_SAMPLE_LIMIT = 5
EXPECTED_MAP_ALIASES = 1005
EXPECTED_RUNTIME_MAPS = 1005
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
EXPECTED_OBJECT_SCRIPT_REFS = 449
EXPECTED_OBJECT_SCRIPTS = 299
EXPECTED_RESOLVED_OBJECT_SCRIPTS = 299
EXPECTED_DETERMINISTIC_OBJECT_ACTIONS = 299
EXPECTED_DETERMINISTIC_OBJECT_NEWWORLD_ACTIONS = 7
EXPECTED_DETERMINISTIC_OBJECT_PICKUP_MESSAGE_ACTIONS = 19
EXPECTED_DETERMINISTIC_OBJECT_TASK_OPTIONAL_PICKUP_MESSAGE_ACTIONS = 1
EXPECTED_DETERMINISTIC_OBJECT_TASK_MISSING_ITEM_PICKUP_MESSAGE_ACTIONS = 2
EXPECTED_DETERMINISTIC_OBJECT_TASK_ITEM_CONSUME_MESSAGE_ACTIONS = 3
EXPECTED_DETERMINISTIC_OBJECT_TASK_ITEM_BRANCH_MESSAGE_ACTIONS = 16
EXPECTED_DETERMINISTIC_OBJECT_PROMPT_BRANCH_MESSAGE_ACTIONS = 10
EXPECTED_DETERMINISTIC_OBJECT_SAY_MESSAGE_ACTIONS = 144
EXPECTED_DETERMINISTIC_OBJECT_TALK_MESSAGE_ACTIONS = 1
EXPECTED_DETERMINISTIC_OBJECT_TASK_TALK_MESSAGE_ACTIONS = 1
EXPECTED_DETERMINISTIC_OBJECT_OPEN_BOX_ACTIONS = 51
EXPECTED_DETERMINISTIC_OBJECT_FACTION_OPEN_BOX_ACTIONS = 19
EXPECTED_DETERMINISTIC_OBJECT_CAMP_OPEN_BOX_ACTIONS = 2
EXPECTED_DETERMINISTIC_OBJECT_SHOW_LADDER_ACTIONS = 23
EXPECTED_TRAP_IDS = 817
EXPECTED_RESOLVED_TRAP_SCRIPTS = 817
EXPECTED_DETERMINISTIC_TRAP_ACTIONS = 804
EXPECTED_DETERMINISTIC_NEWWORLD_TRAP_ACTIONS = 532
EXPECTED_DETERMINISTIC_SETPOS_TRAP_ACTIONS = 1
EXPECTED_DETERMINISTIC_FIGHTSTATE_SETPOS_TRAP_ACTIONS = 112
EXPECTED_DETERMINISTIC_TRAP_MESSAGE_ACTIONS = 37
EXPECTED_DETERMINISTIC_TRAP_MSG2PLAYER_ACTIONS = 1
EXPECTED_DETERMINISTIC_TRAP_SAY_MESSAGE_ACTIONS = 23
EXPECTED_DETERMINISTIC_TRAP_TALK_MESSAGE_ACTIONS = 2
EXPECTED_DETERMINISTIC_TRAP_PROMPT_MESSAGE_ACTIONS = 11
EXPECTED_DETERMINISTIC_TRAP_MSG2PLAYER_NEWWORLD_ACTIONS = 3
EXPECTED_DETERMINISTIC_TRAP_LEVEL_GATE_NEWWORLD_ACTIONS = 20
EXPECTED_DETERMINISTIC_TRAP_LEVEL_BRACKET_NEWWORLD_ACTIONS = 2
EXPECTED_DETERMINISTIC_TRAP_OPEN_SERVER_DATE_GATE_SETPOS_ACTIONS = 37
EXPECTED_DETERMINISTIC_TRAP_RANDOM_NEWWORLD_ACTIONS = 14
EXPECTED_DETERMINISTIC_TRAP_MESSAGE_RANDOM_NEWWORLD_ACTIONS = 1
EXPECTED_DETERMINISTIC_TRAP_REVIVE_RETURN_NEWWORLD_ACTIONS = 6
EXPECTED_DETERMINISTIC_TRAP_TASK_SETPOS_MESSAGE_ACTIONS = 3
EXPECTED_DETERMINISTIC_TRAP_TASK_OPTIONAL_MESSAGE_NEWWORLD_ACTIONS = 1
EXPECTED_DETERMINISTIC_TRAP_TASK_FACTION_GATE_NEWWORLD_ACTIONS = 1
EXPECTED_DETERMINISTIC_TRAP_TASK_PROMPT_DEFAULT_NEWWORLD_ACTIONS = 3
EXPECTED_DETERMINISTIC_TRAP_TASK_FACTION_MESSAGE_GATE_NEWWORLD_ACTIONS = 1
EXPECTED_DETERMINISTIC_TRAP_TASK_FACTION_PROMPT_GATE_NEWWORLD_ACTIONS = 1
EXPECTED_DETERMINISTIC_TRAP_TASK_CURRENT_MAP_RETURN_NEWWORLD_ACTIONS = 1
EXPECTED_DETERMINISTIC_TRAP_TASK_SETTASK_FACTION_GATE_NEWWORLD_ACTIONS = 1
EXPECTED_DETERMINISTIC_TRAP_TASK_SETTASK_PROMPT_CALLBACK_NEWWORLD_ACTIONS = 1
EXPECTED_DETERMINISTIC_TRAP_TASK_ITEM_CONSUME_FACTION_GATE_NEWWORLD_ACTIONS = 2
EXPECTED_DETERMINISTIC_TRAP_TASK_MULTI_ITEM_PROMPT_CALLBACK_NEWWORLD_ACTIONS = 1
EXPECTED_DETERMINISTIC_TRAP_CITYWAR_CAMP_GATE_SETPOS_ACTIONS = 6
EXPECTED_DETERMINISTIC_TRAP_CITYWAR_CAMP_RETURN_NEWWORLD_ACTIONS = 2
EXPECTED_DETERMINISTIC_TRAP_SONGJIN_REBIRTH_ACTIONS = 2
EXPECTED_DETERMINISTIC_TRAP_PARTNER_BAIHUA_ENTRY_ACTIONS = 1
EXPECTED_DETERMINISTIC_TRAP_PARTNER_BAIHUA_EXIT_ACTIONS = 1
EXPECTED_DETERMINISTIC_TRAP_CLEARSKILL_SWITCH_ACTIONS = 4
EXPECTED_DETERMINISTIC_TRAP_CLEARSKILL_LEAVE_ACTIONS = 4
EXPECTED_DETERMINISTIC_TRAP_CS_ARENA_LEAVE_ACTIONS = 1
EXPECTED_DETERMINISTIC_TRAP_TASK_TRIPLET_LEAVE_ACTIONS = 2
EXPECTED_MISSING_TRAP_SCRIPTS: set[str] = set()
EXPECTED_DEFERRED_RESOLVED_TRAP_ACTIONS = 13
EXPECTED_DEFERRED_RESOLVED_TRAP_ACTION_IDS = {
    '0x33494BB9', '0x3619F2C2', '0x5175834A', '0x592C5C47',
    '0x5D608EF1', '0x78CC1B49', '0x9BC53FC2', '0xA4DE52D8',
    '0xC2BCE58F', '0xC69A1B00', '0xDA09D68D',
    '0xDE574C0E', '0xE568D545',
}
EXPECTED_DEFERRED_RESOLVED_TRAP_ACTION_KIND_COUNTS = {
    'CityWarJoinRouter': 1,
    'ClearSkillTeamEnterHole': 4,
    'TongMapEntrance': 8,
}
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


def load_jx_module(root: Path) -> Any | None:
    path = root / JX_MAP_PORT_REL
    if not path.is_file():
        return None
    try:
        spec = importlib.util.spec_from_file_location('jx_map_port_verify_jx', path)
        if spec is None or spec.loader is None:
            return None
        module = importlib.util.module_from_spec(spec)
        sys.modules['jx_map_port_verify_jx'] = module
        spec.loader.exec_module(module)
        return module
    except Exception:
        return None


def normalize_spr_path(name: str) -> str:
    path = (name or '').strip().replace('/', '\\').lstrip('\\')
    return path.lower()


def canonical_spr_path(name: str) -> str:
    path = (name or '').strip().replace('/', '\\')
    return path if path.startswith('\\') else '\\' + path


def load_label_lookups() -> tuple[set[str], dict[str, int]]:
    if not SPR_LABEL_MAP.is_file():
        return set(), {}
    labels = load_json(SPR_LABEL_MAP)
    exact: set[str] = set()
    basenames: dict[str, int] = {}
    for key in labels.keys():
        norm = normalize_spr_path(str(key))
        exact.add(norm)
        base = norm.rsplit('\\', 1)[-1]
        basenames[base] = basenames.get(base, 0) + 1
    return exact, basenames


def map_sprite_verdict(path: str, present_in_pak: bool, label_hits: int) -> str:
    norm = normalize_spr_path(path)
    if present_in_pak or label_hits > 0:
        return 'unexpected_resolved_elsewhere'
    if norm == 'system\\spr\\regiontiledefault.spr':
        return 'engine_default_fallback_source_missing'
    return 'source_missing_in_scoped_pc_paks'


def build_missing_sprite_provenance(
    audit: Audit,
    root: Path,
    pc_root: Path,
    geometries: dict[str, Any],
    missing_paths: set[str],
    include_region_refs: bool,
) -> list[dict[str, Any]]:
    if not missing_paths:
        return []
    jx = load_jx_module(root)
    if jx is None:
        audit.warn('missing map SPR provenance unavailable: cannot load jx-map-port hash helpers')
        return []

    data_dir = pc_root / 'Client 6.0/data'
    try:
        pak_index = jx.build_index(str(data_dir))
    except Exception:
        pak_index = {}
        audit.warn(f'missing map SPR provenance could not index client PAK dir: {data_dir}')
    label_exact, label_basenames = load_label_lookups()

    by_exact = {canonical_spr_path(path): path for path in missing_paths}
    records: dict[str, dict[str, Any]] = {}
    for path in sorted(missing_paths):
        canonical = canonical_spr_path(path)
        engine_uid = jx.g_filename2id(canonical.encode('gbk', errors='ignore'))
        staged_uid = jx.compute_path_uid(canonical)
        norm = normalize_spr_path(canonical)
        basename = norm.rsplit('\\', 1)[-1]
        label_hits = 1 if norm in label_exact else 0
        basename_hits = label_basenames.get(basename, 0)
        present = engine_uid in pak_index
        records[path] = {
            'path': path,
            'engineUid': f'0x{engine_uid:08X}',
            'stagingUid': staged_uid,
            'presentInClientPak': present,
            'labelExactHits': label_hits,
            'labelBasenameHits': basename_hits,
            'geometryCount': 0,
            'sampleGeometries': [],
            'verdict': map_sprite_verdict(path, present, label_hits),
        }

    sa = root / 'Assets/StreamingAssets'
    for geometry in geometries.get('geometries', []):
        folder = sa / geometry.get('regionFolder', '')
        names_path = folder / 'image_names.json'
        if not names_path.is_file():
            continue
        try:
            image_names = load_json(names_path)
        except Exception:
            continue
        matched_paths = {
            by_exact[key]
            for name in image_names
            if (key := canonical_spr_path(str(name))) in by_exact
        }
        for path in matched_paths:
            rec = records[path]
            rec['geometryCount'] += 1
            samples = rec['sampleGeometries']
            if len(samples) < PROVENANCE_SAMPLE_LIMIT:
                samples.append({
                    'geometryKey': geometry.get('geometryKey'),
                    'primaryMapId': geometry.get('primaryMapId'),
                    'mapIds': geometry.get('mapIds', [])[:PROVENANCE_SAMPLE_LIMIT],
                    'pcMapPath': geometry.get('pcMapPath'),
                })

    if include_region_refs:
        for rec in records.values():
            rec['regionFileRefCount'] = 0
        for geometry in geometries.get('geometries', []):
            folder = sa / geometry.get('regionFolder', '')
            if not folder.is_dir():
                continue
            for region_path in folder.glob('*_Region_C.dat'):
                try:
                    names = jx.collect_names(region_path.read_bytes())
                except Exception:
                    continue
                for name in names:
                    key = by_exact.get(canonical_spr_path(str(name)))
                    if key:
                        records[key]['regionFileRefCount'] += 1
    return [records[path] for path in sorted(records)]


def verify_visual_catalogs(
    audit: Audit,
    root: Path,
    pc_root: Path,
    include_missing_spr_region_refs: bool,
) -> tuple[dict[str, Any], dict[str, Any]]:
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
    provenance = build_missing_sprite_provenance(
        audit,
        root,
        pc_root,
        geometries,
        failed_sprites,
        include_missing_spr_region_refs,
    )
    audit.facts['visualCatalog'] = {
        'pcMapIds': len(pc_ids),
        'aliases': len(alias_ids),
        'geometries': len(geometry_keys),
        'regionC': coverage.get('extractedRegions'),
        'mapSprites': coverage.get('stagedSprites'),
        'failedSpriteRefs': coverage.get('failedSprites'),
        'failedSpriteUniquePaths': sorted(failed_sprites),
        'missingSpriteProvenance': provenance,
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
    trap_action_catalog = load_json(sa / 'MapTrapActionCatalog.json')
    object_script_cov = load_json(sa / 'MapObjectScriptCoverage.json')
    object_script_catalog = load_json(sa / 'MapObjectScriptCatalog.json')
    object_action_catalog = load_json(sa / 'MapObjectActionCatalog.json')

    audit.require(interactive.get('totalGeometries') == EXPECTED_GEOMETRIES, 'MapInteractive total geometry mismatch')
    audit.require(interactive.get('trapEntries') == EXPECTED_TRAP_RECORDS, 'MapInteractive trap count mismatch')
    audit.require(interactive.get('objectEntries') == EXPECTED_OBJECT_RECORDS, 'MapInteractive object count mismatch')
    audit.require(object_cov.get('objectTemplatesResolved') == EXPECTED_OBJECT_TEMPLATES, 'object template resolved count mismatch')
    audit.require(object_cov.get('stagedObjectSpritePaths') == EXPECTED_OBJECT_SPRITES, 'object sprite staged count mismatch')
    audit.require(object_cov.get('missingObjectSpritePaths') == 0, 'object sprite missing paths must be 0')
    audit.require(object_script_cov.get('objectScriptRefs') == EXPECTED_OBJECT_SCRIPT_REFS, 'object script ref count mismatch')
    audit.require(object_script_cov.get('uniqueObjectScripts') == EXPECTED_OBJECT_SCRIPTS, 'unique object script count mismatch')
    audit.require(object_script_cov.get('resolvedObjectScripts') == EXPECTED_RESOLVED_OBJECT_SCRIPTS, 'resolved object script count mismatch')
    audit.require(object_script_cov.get('missingObjectScripts') == 0, 'object script missing count must be 0')
    audit.require(len(object_script_catalog.get('entries', [])) == EXPECTED_OBJECT_SCRIPTS, 'MapObjectScriptCatalog entry count mismatch')
    audit.require(len(object_action_catalog.get('entries', [])) == EXPECTED_DETERMINISTIC_OBJECT_ACTIONS, 'MapObjectActionCatalog deterministic action count mismatch')
    object_actions = object_action_catalog.get('entries', [])
    object_new_world = sum(1 for e in object_actions if e.get('actionKind') == 'NewWorld')
    object_pickup_message = sum(1 for e in object_actions if e.get('actionKind') == 'PickupMessage')
    object_task_optional_pickup_message = sum(1 for e in object_actions if e.get('actionKind') == 'TaskOptionalPickupMessage')
    object_task_missing_item_pickup_message = sum(1 for e in object_actions if e.get('actionKind') == 'TaskMissingItemPickupMessage')
    object_task_item_consume_message = sum(1 for e in object_actions if e.get('actionKind') == 'TaskItemConsumeMessage')
    object_task_item_branch_message = sum(1 for e in object_actions if e.get('actionKind') == 'TaskItemBranchMessage')
    object_prompt_branch_message = sum(1 for e in object_actions if e.get('actionKind') == 'PromptBranchMessage')
    object_say_message = sum(1 for e in object_actions if e.get('actionKind') == 'SayMessage')
    object_talk_message = sum(1 for e in object_actions if e.get('actionKind') == 'TalkMessage')
    object_task_talk_message = sum(1 for e in object_actions if e.get('actionKind') == 'TaskTalkMessage')
    object_open_box = sum(1 for e in object_actions if e.get('actionKind') == 'OpenBox')
    object_faction_open_box = sum(1 for e in object_actions if e.get('actionKind') == 'FactionOpenBox')
    object_camp_open_box = sum(1 for e in object_actions if e.get('actionKind') == 'CampOpenBox')
    object_show_ladder = sum(1 for e in object_actions if e.get('actionKind') == 'ShowLadder')
    audit.require(object_new_world == EXPECTED_DETERMINISTIC_OBJECT_NEWWORLD_ACTIONS, 'MapObjectActionCatalog NewWorld count mismatch')
    audit.require(object_pickup_message == EXPECTED_DETERMINISTIC_OBJECT_PICKUP_MESSAGE_ACTIONS, 'MapObjectActionCatalog PickupMessage count mismatch')
    audit.require(object_task_optional_pickup_message == EXPECTED_DETERMINISTIC_OBJECT_TASK_OPTIONAL_PICKUP_MESSAGE_ACTIONS, 'MapObjectActionCatalog TaskOptionalPickupMessage count mismatch')
    audit.require(object_task_missing_item_pickup_message == EXPECTED_DETERMINISTIC_OBJECT_TASK_MISSING_ITEM_PICKUP_MESSAGE_ACTIONS, 'MapObjectActionCatalog TaskMissingItemPickupMessage count mismatch')
    audit.require(object_task_item_consume_message == EXPECTED_DETERMINISTIC_OBJECT_TASK_ITEM_CONSUME_MESSAGE_ACTIONS, 'MapObjectActionCatalog TaskItemConsumeMessage count mismatch')
    audit.require(object_task_item_branch_message == EXPECTED_DETERMINISTIC_OBJECT_TASK_ITEM_BRANCH_MESSAGE_ACTIONS, 'MapObjectActionCatalog TaskItemBranchMessage count mismatch')
    audit.require(object_prompt_branch_message == EXPECTED_DETERMINISTIC_OBJECT_PROMPT_BRANCH_MESSAGE_ACTIONS, 'MapObjectActionCatalog PromptBranchMessage count mismatch')
    audit.require(object_say_message == EXPECTED_DETERMINISTIC_OBJECT_SAY_MESSAGE_ACTIONS, 'MapObjectActionCatalog SayMessage count mismatch')
    audit.require(object_talk_message == EXPECTED_DETERMINISTIC_OBJECT_TALK_MESSAGE_ACTIONS, 'MapObjectActionCatalog TalkMessage count mismatch')
    audit.require(object_task_talk_message == EXPECTED_DETERMINISTIC_OBJECT_TASK_TALK_MESSAGE_ACTIONS, 'MapObjectActionCatalog TaskTalkMessage count mismatch')
    audit.require(object_open_box == EXPECTED_DETERMINISTIC_OBJECT_OPEN_BOX_ACTIONS, 'MapObjectActionCatalog OpenBox count mismatch')
    audit.require(object_faction_open_box == EXPECTED_DETERMINISTIC_OBJECT_FACTION_OPEN_BOX_ACTIONS, 'MapObjectActionCatalog FactionOpenBox count mismatch')
    audit.require(object_camp_open_box == EXPECTED_DETERMINISTIC_OBJECT_CAMP_OPEN_BOX_ACTIONS, 'MapObjectActionCatalog CampOpenBox count mismatch')
    audit.require(object_show_ladder == EXPECTED_DETERMINISTIC_OBJECT_SHOW_LADDER_ACTIONS, 'MapObjectActionCatalog ShowLadder count mismatch')
    audit.require(trap_cov.get('uniqueTrapIds') == EXPECTED_TRAP_IDS, 'unique trap id count mismatch')
    audit.require(trap_cov.get('resolvedTrapScripts') == EXPECTED_RESOLVED_TRAP_SCRIPTS, 'resolved trap script count mismatch')
    missing_ids = set(trap_cov.get('missingTrapScriptIds', []))
    audit.require(missing_ids == EXPECTED_MISSING_TRAP_SCRIPTS,
                  f'missing trap scripts={sorted(missing_ids)} expected={sorted(EXPECTED_MISSING_TRAP_SCRIPTS)}')
    if missing_ids:
        audit.warn(f'trap scripts still source-missing: {sorted(missing_ids)}')
    action_entries = trap_action_catalog.get('entries', [])
    action_ids = {e.get('trapIdHex') for e in action_entries}
    deferred_ids = set(trap_cov.get('deferredResolvedTrapActionIds', []))
    deferred_kind_counts = trap_cov.get('deferredResolvedTrapActionKindCounts', {})
    unclassified_deferred_ids = set(trap_cov.get('unclassifiedResolvedTrapActionIds', []))
    deferred_catalog_entries = [
        e for e in trap_catalog.get('entries', [])
        if e.get('actionPortStatus') == 'deferred_requires_pc_runtime'
    ]
    audit.require(trap_cov.get('deferredResolvedTrapActions') == EXPECTED_DEFERRED_RESOLVED_TRAP_ACTIONS,
                  'deferred resolved trap action count mismatch')
    audit.require(deferred_ids == EXPECTED_DEFERRED_RESOLVED_TRAP_ACTION_IDS,
                  f'deferred trap ids={sorted(deferred_ids)} expected={sorted(EXPECTED_DEFERRED_RESOLVED_TRAP_ACTION_IDS)}')
    audit.require(deferred_kind_counts == EXPECTED_DEFERRED_RESOLVED_TRAP_ACTION_KIND_COUNTS,
                  f'deferred trap kind counts={deferred_kind_counts} expected={EXPECTED_DEFERRED_RESOLVED_TRAP_ACTION_KIND_COUNTS}')
    audit.require(not unclassified_deferred_ids,
                  f'unclassified resolved trap action ids must be empty, got {sorted(unclassified_deferred_ids)}')
    audit.require(len(deferred_catalog_entries) == EXPECTED_DEFERRED_RESOLVED_TRAP_ACTIONS,
                  'MapTrapScriptCatalog deferred action entry count mismatch')
    audit.require({e.get('trapIdHex') for e in deferred_catalog_entries} == EXPECTED_DEFERRED_RESOLVED_TRAP_ACTION_IDS,
                  'MapTrapScriptCatalog deferred action ids mismatch')
    audit.require(len(action_ids) == len(action_entries), 'MapTrapActionCatalog must have unique trap action ids')
    audit.require(action_ids.isdisjoint(deferred_ids), 'deterministic and deferred trap action ids must not overlap')
    audit.require(len(action_entries) + len(deferred_catalog_entries) == EXPECTED_RESOLVED_TRAP_SCRIPTS,
                  'resolved trap scripts must be either deterministic actions or explicitly deferred')

    sprite_root = sa / object_catalog.get('spriteFolder', 'Generated/ObjectSprites')
    expected = {t.get('uid') + '.spr' for t in object_catalog.get('templates', []) if t.get('staged') and t.get('uid')}
    present = {p.name for p in sprite_root.glob('*.spr')} if sprite_root.is_dir() else set()
    audit.require(len(expected) == EXPECTED_OBJECT_SPRITES, f'object expected sprite files={len(expected)} expected={EXPECTED_OBJECT_SPRITES}')
    audit.require(expected <= present, f'missing generated object SPR files: {sorted(expected - present)[:8]}')
    action_new_world = sum(1 for e in action_entries if e.get('actionKind') == 'NewWorld')
    action_set_pos = sum(1 for e in action_entries if e.get('actionKind') == 'SetPos')
    action_fight_state_set_pos = sum(1 for e in action_entries if e.get('actionKind') == 'FightStateSetPos')
    action_msg2_player = sum(1 for e in action_entries if e.get('actionKind') == 'Msg2Player')
    action_say_message = sum(1 for e in action_entries if e.get('actionKind') == 'SayMessage')
    action_talk_message = sum(1 for e in action_entries if e.get('actionKind') == 'TalkMessage')
    action_prompt_message = sum(1 for e in action_entries if e.get('actionKind') == 'PromptMessage')
    action_msg2_player_new_world = sum(1 for e in action_entries if e.get('actionKind') == 'Msg2PlayerNewWorld')
    action_level_gate_new_world = sum(1 for e in action_entries if e.get('actionKind') == 'LevelGateNewWorld')
    action_level_bracket_new_world = sum(1 for e in action_entries if e.get('actionKind') == 'LevelBracketNewWorld')
    action_open_server_date_gate_setpos = sum(1 for e in action_entries if e.get('actionKind') == 'OpenServerDateGateSetPos')
    action_random_new_world = sum(1 for e in action_entries if e.get('actionKind') == 'RandomNewWorld')
    action_message_random_new_world = sum(1 for e in action_entries if e.get('actionKind') == 'MessageRandomNewWorld')
    action_revive_return_new_world = sum(1 for e in action_entries if e.get('actionKind') == 'ReviveReturnNewWorld')
    action_task_setpos_message = sum(1 for e in action_entries if e.get('actionKind') == 'TaskSetPosMessage')
    action_task_optional_message_newworld = sum(1 for e in action_entries if e.get('actionKind') == 'TaskOptionalMessageNewWorld')
    action_task_faction_gate_newworld = sum(1 for e in action_entries if e.get('actionKind') == 'TaskFactionGateNewWorld')
    action_task_prompt_default_newworld = sum(1 for e in action_entries if e.get('actionKind') == 'TaskPromptDefaultNewWorld')
    action_task_faction_message_gate_newworld = sum(1 for e in action_entries if e.get('actionKind') == 'TaskFactionMessageGateNewWorld')
    action_task_faction_prompt_gate_newworld = sum(1 for e in action_entries if e.get('actionKind') == 'TaskFactionPromptGateNewWorld')
    action_task_current_map_return_newworld = sum(1 for e in action_entries if e.get('actionKind') == 'TaskCurrentMapReturnNewWorld')
    action_task_settask_faction_gate_newworld = sum(1 for e in action_entries if e.get('actionKind') == 'TaskSetTaskFactionGateNewWorld')
    action_task_settask_prompt_callback_newworld = sum(1 for e in action_entries if e.get('actionKind') == 'TaskSetTaskPromptCallbackNewWorld')
    action_task_item_consume_faction_gate_newworld = sum(1 for e in action_entries if e.get('actionKind') == 'TaskItemConsumeFactionGateNewWorld')
    action_task_multi_item_prompt_callback_newworld = sum(1 for e in action_entries if e.get('actionKind') == 'TaskMultiItemPromptCallbackNewWorld')
    action_citywar_camp_gate_setpos = sum(1 for e in action_entries if e.get('actionKind') == 'CityWarCampGateSetPos')
    action_citywar_camp_return_newworld = sum(1 for e in action_entries if e.get('actionKind') == 'CityWarCampReturnNewWorld')
    action_songjin_rebirth = sum(1 for e in action_entries if e.get('actionKind') == 'SongJinRebirthCampState')
    action_partner_baihua_entry = sum(1 for e in action_entries if e.get('actionKind') == 'PartnerBaihuaEntryGate')
    action_partner_baihua_exit = sum(1 for e in action_entries if e.get('actionKind') == 'PartnerBaihuaExitGate')
    action_clearskill_switch = sum(1 for e in action_entries if e.get('actionKind') == 'ClearSkillSwitchTrap')
    action_clearskill_leave = sum(1 for e in action_entries if e.get('actionKind') == 'ClearSkillLeaveGame')
    action_cs_arena_leave = sum(1 for e in action_entries if e.get('actionKind') == 'CsArenaLeaveTrap')
    action_task_triplet_leave = sum(1 for e in action_entries if e.get('actionKind') == 'TaskTripletLeaveTrap')
    action_message = action_msg2_player + action_say_message + action_talk_message + action_prompt_message
    audit.require(len(trap_catalog.get('entries', [])) == EXPECTED_TRAP_IDS, 'MapTrapScriptCatalog entry count mismatch')
    audit.require(len(action_entries) == EXPECTED_DETERMINISTIC_TRAP_ACTIONS, 'MapTrapActionCatalog deterministic action count mismatch')
    audit.require(action_new_world == EXPECTED_DETERMINISTIC_NEWWORLD_TRAP_ACTIONS, 'MapTrapActionCatalog NewWorld count mismatch')
    audit.require(action_set_pos == EXPECTED_DETERMINISTIC_SETPOS_TRAP_ACTIONS, 'MapTrapActionCatalog SetPos count mismatch')
    audit.require(action_fight_state_set_pos == EXPECTED_DETERMINISTIC_FIGHTSTATE_SETPOS_TRAP_ACTIONS, 'MapTrapActionCatalog FightStateSetPos count mismatch')
    audit.require(action_message == EXPECTED_DETERMINISTIC_TRAP_MESSAGE_ACTIONS, 'MapTrapActionCatalog message-only count mismatch')
    audit.require(action_msg2_player == EXPECTED_DETERMINISTIC_TRAP_MSG2PLAYER_ACTIONS, 'MapTrapActionCatalog Msg2Player count mismatch')
    audit.require(action_say_message == EXPECTED_DETERMINISTIC_TRAP_SAY_MESSAGE_ACTIONS, 'MapTrapActionCatalog SayMessage count mismatch')
    audit.require(action_talk_message == EXPECTED_DETERMINISTIC_TRAP_TALK_MESSAGE_ACTIONS, 'MapTrapActionCatalog TalkMessage count mismatch')
    audit.require(action_prompt_message == EXPECTED_DETERMINISTIC_TRAP_PROMPT_MESSAGE_ACTIONS, 'MapTrapActionCatalog PromptMessage count mismatch')
    audit.require(action_msg2_player_new_world == EXPECTED_DETERMINISTIC_TRAP_MSG2PLAYER_NEWWORLD_ACTIONS, 'MapTrapActionCatalog Msg2PlayerNewWorld count mismatch')
    audit.require(action_level_gate_new_world == EXPECTED_DETERMINISTIC_TRAP_LEVEL_GATE_NEWWORLD_ACTIONS, 'MapTrapActionCatalog LevelGateNewWorld count mismatch')
    audit.require(action_level_bracket_new_world == EXPECTED_DETERMINISTIC_TRAP_LEVEL_BRACKET_NEWWORLD_ACTIONS,
                  'MapTrapActionCatalog LevelBracketNewWorld count mismatch')
    audit.require(action_open_server_date_gate_setpos == EXPECTED_DETERMINISTIC_TRAP_OPEN_SERVER_DATE_GATE_SETPOS_ACTIONS,
                  'MapTrapActionCatalog OpenServerDateGateSetPos count mismatch')
    audit.require(action_random_new_world == EXPECTED_DETERMINISTIC_TRAP_RANDOM_NEWWORLD_ACTIONS,
                  'MapTrapActionCatalog RandomNewWorld count mismatch')
    audit.require(action_message_random_new_world == EXPECTED_DETERMINISTIC_TRAP_MESSAGE_RANDOM_NEWWORLD_ACTIONS,
                  'MapTrapActionCatalog MessageRandomNewWorld count mismatch')
    audit.require(action_revive_return_new_world == EXPECTED_DETERMINISTIC_TRAP_REVIVE_RETURN_NEWWORLD_ACTIONS,
                  'MapTrapActionCatalog ReviveReturnNewWorld count mismatch')
    audit.require(action_task_setpos_message == EXPECTED_DETERMINISTIC_TRAP_TASK_SETPOS_MESSAGE_ACTIONS,
                  'MapTrapActionCatalog TaskSetPosMessage count mismatch')
    audit.require(action_task_optional_message_newworld == EXPECTED_DETERMINISTIC_TRAP_TASK_OPTIONAL_MESSAGE_NEWWORLD_ACTIONS,
                  'MapTrapActionCatalog TaskOptionalMessageNewWorld count mismatch')
    audit.require(action_task_faction_gate_newworld == EXPECTED_DETERMINISTIC_TRAP_TASK_FACTION_GATE_NEWWORLD_ACTIONS,
                  'MapTrapActionCatalog TaskFactionGateNewWorld count mismatch')
    audit.require(action_task_prompt_default_newworld == EXPECTED_DETERMINISTIC_TRAP_TASK_PROMPT_DEFAULT_NEWWORLD_ACTIONS,
                  'MapTrapActionCatalog TaskPromptDefaultNewWorld count mismatch')
    audit.require(action_task_faction_message_gate_newworld == EXPECTED_DETERMINISTIC_TRAP_TASK_FACTION_MESSAGE_GATE_NEWWORLD_ACTIONS,
                  'MapTrapActionCatalog TaskFactionMessageGateNewWorld count mismatch')
    audit.require(action_task_faction_prompt_gate_newworld == EXPECTED_DETERMINISTIC_TRAP_TASK_FACTION_PROMPT_GATE_NEWWORLD_ACTIONS,
                  'MapTrapActionCatalog TaskFactionPromptGateNewWorld count mismatch')
    audit.require(action_task_current_map_return_newworld == EXPECTED_DETERMINISTIC_TRAP_TASK_CURRENT_MAP_RETURN_NEWWORLD_ACTIONS,
                  'MapTrapActionCatalog TaskCurrentMapReturnNewWorld count mismatch')
    audit.require(action_task_settask_faction_gate_newworld == EXPECTED_DETERMINISTIC_TRAP_TASK_SETTASK_FACTION_GATE_NEWWORLD_ACTIONS,
                  'MapTrapActionCatalog TaskSetTaskFactionGateNewWorld count mismatch')
    audit.require(action_task_settask_prompt_callback_newworld == EXPECTED_DETERMINISTIC_TRAP_TASK_SETTASK_PROMPT_CALLBACK_NEWWORLD_ACTIONS,
                  'MapTrapActionCatalog TaskSetTaskPromptCallbackNewWorld count mismatch')
    audit.require(action_task_item_consume_faction_gate_newworld == EXPECTED_DETERMINISTIC_TRAP_TASK_ITEM_CONSUME_FACTION_GATE_NEWWORLD_ACTIONS,
                  'MapTrapActionCatalog TaskItemConsumeFactionGateNewWorld count mismatch')
    audit.require(action_task_multi_item_prompt_callback_newworld == EXPECTED_DETERMINISTIC_TRAP_TASK_MULTI_ITEM_PROMPT_CALLBACK_NEWWORLD_ACTIONS,
                  'MapTrapActionCatalog TaskMultiItemPromptCallbackNewWorld count mismatch')
    audit.require(action_citywar_camp_gate_setpos == EXPECTED_DETERMINISTIC_TRAP_CITYWAR_CAMP_GATE_SETPOS_ACTIONS,
                  'MapTrapActionCatalog CityWarCampGateSetPos count mismatch')
    audit.require(action_citywar_camp_return_newworld == EXPECTED_DETERMINISTIC_TRAP_CITYWAR_CAMP_RETURN_NEWWORLD_ACTIONS,
                  'MapTrapActionCatalog CityWarCampReturnNewWorld count mismatch')
    audit.require(action_songjin_rebirth == EXPECTED_DETERMINISTIC_TRAP_SONGJIN_REBIRTH_ACTIONS,
                  'MapTrapActionCatalog SongJinRebirthCampState count mismatch')
    audit.require(action_partner_baihua_entry == EXPECTED_DETERMINISTIC_TRAP_PARTNER_BAIHUA_ENTRY_ACTIONS,
                  'MapTrapActionCatalog PartnerBaihuaEntryGate count mismatch')
    audit.require(action_partner_baihua_exit == EXPECTED_DETERMINISTIC_TRAP_PARTNER_BAIHUA_EXIT_ACTIONS,
                  'MapTrapActionCatalog PartnerBaihuaExitGate count mismatch')
    audit.require(action_clearskill_switch == EXPECTED_DETERMINISTIC_TRAP_CLEARSKILL_SWITCH_ACTIONS,
                  'MapTrapActionCatalog ClearSkillSwitchTrap count mismatch')
    audit.require(action_clearskill_leave == EXPECTED_DETERMINISTIC_TRAP_CLEARSKILL_LEAVE_ACTIONS,
                  'MapTrapActionCatalog ClearSkillLeaveGame count mismatch')
    audit.require(action_cs_arena_leave == EXPECTED_DETERMINISTIC_TRAP_CS_ARENA_LEAVE_ACTIONS,
                  'MapTrapActionCatalog CsArenaLeaveTrap count mismatch')
    audit.require(action_task_triplet_leave == EXPECTED_DETERMINISTIC_TRAP_TASK_TRIPLET_LEAVE_ACTIONS,
                  'MapTrapActionCatalog TaskTripletLeaveTrap count mismatch')
    audit.facts['interactive'] = {
        'traps': interactive.get('trapEntries'),
        'objects': interactive.get('objectEntries'),
        'objectTemplates': object_cov.get('objectTemplatesResolved'),
        'objectSpriteFiles': len(present),
        'objectScriptRefs': object_script_cov.get('objectScriptRefs'),
        'objectScripts': object_script_cov.get('uniqueObjectScripts'),
        'resolvedObjectScripts': object_script_cov.get('resolvedObjectScripts'),
        'deterministicObjectActions': len(object_actions),
        'deterministicObjectNewWorldActions': object_new_world,
        'deterministicObjectPickupMessageActions': object_pickup_message,
        'deterministicObjectTaskOptionalPickupMessageActions': object_task_optional_pickup_message,
        'deterministicObjectTaskMissingItemPickupMessageActions': object_task_missing_item_pickup_message,
        'deterministicObjectTaskItemConsumeMessageActions': object_task_item_consume_message,
        'deterministicObjectTaskItemBranchMessageActions': object_task_item_branch_message,
        'deterministicObjectPromptBranchMessageActions': object_prompt_branch_message,
        'deterministicObjectSayMessageActions': object_say_message,
        'deterministicObjectTalkMessageActions': object_talk_message,
        'deterministicObjectTaskTalkMessageActions': object_task_talk_message,
        'deterministicObjectOpenBoxActions': object_open_box,
        'deterministicObjectFactionOpenBoxActions': object_faction_open_box,
        'deterministicObjectCampOpenBoxActions': object_camp_open_box,
        'deterministicObjectShowLadderActions': object_show_ladder,
        'trapIds': trap_cov.get('uniqueTrapIds'),
        'resolvedTrapScripts': trap_cov.get('resolvedTrapScripts'),
        'missingTrapScripts': sorted(missing_ids),
        'deferredResolvedTrapActions': trap_cov.get('deferredResolvedTrapActions'),
        'deferredResolvedTrapActionKindCounts': deferred_kind_counts,
        'unclassifiedResolvedTrapActionIds': sorted(unclassified_deferred_ids),
        'deterministicTrapActions': len(action_entries),
        'deterministicNewWorldActions': action_new_world,
        'deterministicSetPosActions': action_set_pos,
        'deterministicFightStateSetPosActions': action_fight_state_set_pos,
        'deterministicTrapMessageActions': action_message,
        'deterministicTrapMsg2PlayerActions': action_msg2_player,
        'deterministicTrapSayMessageActions': action_say_message,
        'deterministicTrapTalkMessageActions': action_talk_message,
        'deterministicTrapPromptMessageActions': action_prompt_message,
        'deterministicTrapMsg2PlayerNewWorldActions': action_msg2_player_new_world,
        'deterministicTrapLevelGateNewWorldActions': action_level_gate_new_world,
        'deterministicTrapLevelBracketNewWorldActions': action_level_bracket_new_world,
        'deterministicTrapOpenServerDateGateSetPosActions': action_open_server_date_gate_setpos,
        'deterministicTrapRandomNewWorldActions': action_random_new_world,
        'deterministicTrapMessageRandomNewWorldActions': action_message_random_new_world,
        'deterministicTrapReviveReturnNewWorldActions': action_revive_return_new_world,
        'deterministicTrapTaskSetPosMessageActions': action_task_setpos_message,
        'deterministicTrapTaskOptionalMessageNewWorldActions': action_task_optional_message_newworld,
        'deterministicTrapTaskFactionGateNewWorldActions': action_task_faction_gate_newworld,
        'deterministicTrapTaskPromptDefaultNewWorldActions': action_task_prompt_default_newworld,
        'deterministicTrapTaskFactionMessageGateNewWorldActions': action_task_faction_message_gate_newworld,
        'deterministicTrapTaskFactionPromptGateNewWorldActions': action_task_faction_prompt_gate_newworld,
        'deterministicTrapTaskCurrentMapReturnNewWorldActions': action_task_current_map_return_newworld,
        'deterministicTrapTaskSetTaskFactionGateNewWorldActions': action_task_settask_faction_gate_newworld,
        'deterministicTrapTaskSetTaskPromptCallbackNewWorldActions': action_task_settask_prompt_callback_newworld,
        'deterministicTrapTaskItemConsumeFactionGateNewWorldActions': action_task_item_consume_faction_gate_newworld,
        'deterministicTrapTaskMultiItemPromptCallbackNewWorldActions': action_task_multi_item_prompt_callback_newworld,
        'deterministicTrapCityWarCampGateSetPosActions': action_citywar_camp_gate_setpos,
        'deterministicTrapCityWarCampReturnNewWorldActions': action_citywar_camp_return_newworld,
        'deterministicTrapSongJinRebirthCampStateActions': action_songjin_rebirth,
        'deterministicTrapPartnerBaihuaEntryGateActions': action_partner_baihua_entry,
        'deterministicTrapPartnerBaihuaExitGateActions': action_partner_baihua_exit,
        'deterministicTrapClearSkillSwitchTrapActions': action_clearskill_switch,
        'deterministicTrapClearSkillLeaveGameActions': action_clearskill_leave,
        'deterministicTrapCsArenaLeaveTrapActions': action_cs_arena_leave,
        'deterministicTrapTaskTripletLeaveTrapActions': action_task_triplet_leave,
    }


def verify_default_map(audit: Audit, root: Path) -> None:
    manager = root / 'Assets/Scripts/Sandbox/SandboxManager.cs'
    text = manager.read_text(encoding='utf-8', errors='ignore')
    manifest_text = (root / 'Assets/Scripts/Sandbox/MapPortManifest.cs').read_text(encoding='utf-8', errors='ignore')
    expected_manifest_ids = {
        'PhuongTuongId': 1,
        'ThanhDoId': 11,
        'GiangTanThonId': 20,
        'BienKinhId': 37,
        'BaLangHuyenId': 53,
        'TuongDuongId': 78,
        'DaiLyId': 162,
        'LamAnId': 176,
        'DaoHoaDaoId': 235,
        'VuotAiNhiepThiTranId': 907,
    }
    for const_name, map_id in expected_manifest_ids.items():
        audit.require(f'public const int {const_name} = {map_id};' in manifest_text,
                      f'MapPortManifest.{const_name} must match PC MapAliasCatalog mapId {map_id}')
    audit.require('QuangChauId' not in manifest_text and 'Quảng Châu' not in manifest_text,
                  'MapPortManifest must not include synthetic Quảng Châu: scoped PC maplist.ini has no 广州 entry')
    audit.require('public const int BaLangHuyenMapId = 79;' in text,
                  'SandboxManager must keep Ba Lăng Huyện constant at mapId 79')
    audit.require('public int defaultMapId = BaLangHuyenMapId;' in text,
                  'SandboxManager defaultMapId must point to Ba Lăng Huyện map 79')
    enemy_runtime = (root / 'Assets/Scripts/Sandbox/MapEnemySpawnRuntime.cs').read_text(encoding='utf-8', errors='ignore')
    killboss = root / 'Assets/Scripts/Sandbox/VuotAiKillBossMatchSpawns.cs'
    killboss_text = killboss.read_text(encoding='utf-8', errors='ignore') if killboss.is_file() else ''
    audit.require('VuotAiKillBossMatchSpawns.IsMissionMap(mapId)' in enemy_runtime,
                  'MapEnemySpawnRuntime must apply PC killbossmatch ClearMapNpc mission override for map 907..916')
    audit.require('public static readonly int[] BossTemplateIds = { 1481, 1485, 1488, 1483, 1482, 1480, 1489, 1486, 1487, 1484 }' in killboss_text,
                  'Vượt ải killbossmatch boss template roster must match PC class.lua tbNpc order')
    audit.facts['defaultMap'] = {
        'mapId': 79,
        'nameVi': 'Ba Lăng huyện',
    }


def run_audit(args: argparse.Namespace) -> Audit:
    root = args.unity_root.resolve()
    pc_root = args.pc_root.resolve()
    audit = Audit()
    visual_coverage, geometries = verify_visual_catalogs(
        audit,
        root,
        pc_root,
        args.include_missing_spr_region_refs,
    )
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
    parser.add_argument('--include-missing-spr-region-refs', action='store_true',
                        help='Scan generated Region_C files and include per-missing-SPR region file reference counts.')
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
