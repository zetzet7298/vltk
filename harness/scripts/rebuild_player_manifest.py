#!/usr/bin/env python3
"""
Rebuild male_player_sprites.json and female_player_sprites.json manifests
from the actual files on disk (Assets/StreamingAssets/Sprites/).

Also stages missing SPR variants from PC source (spr.pak or loose folder).
"""
from __future__ import annotations
import sys, os, json, shutil, argparse
from pathlib import Path

sys.path.insert(0, '/var/www/vltk-mobile/harness/.agents/skills/jx-player-visual/scripts')
from uid import uid_hex

SPRITES_DIR = Path('/var/www/vltk-mobile/Assets/StreamingAssets/Sprites')
MALE_MANIFEST = Path('/var/www/vltk-mobile/Assets/StreamingAssets/male_player_sprites.json')
FEMALE_MANIFEST = Path('/var/www/vltk-mobile/Assets/StreamingAssets/female_player_sprites.json')

# PC loose folder for man/woman
PC_MAN_DIR = Path("/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/spr/npcres/man")
PC_WOMAN_DIR = Path("/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/spr/npcres/woman")

# Actions we need for the game (idle, run, attack, mount)
ACTIONS = ['ST01', 'ST02', 'ST04', 'ST05', 'ST06',
           'RN01', 'RN02', 'RN03', 'RN04',
           'AT01', 'AT03', 'AT05', 'AT07',
           'MG01', 'MG02', 'MG03', 'MG04', 'MG05',
           'RD01', 'HR01']

# Parts for male
MALE_PARTS = ['YY', 'BD', 'HD', 'HR', 'LH', 'RH', 'LW', 'RW', 'HH', 'HB', 'HT']
FEMALE_PARTS = ['YY', 'BD', 'HD', 'HR', 'LH', 'RH', 'LW', 'RW', 'HH', 'HB', 'HT']

# Mapper variants from PlayerAppearanceMapper (what variants runtime actually needs)
# Expanded to all variants actually on disk from spr.pak + loose PC source
MALE_BODY_VARIANTS = [1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,
                      25,26,27,28,29,30,31,32,33,34,37,38,39,40,50,
                      70,72,74,75,76,77,80,81,82,83,84,85,86,87,88,89,90,
                      93,94,95,96,97,98,99,100,101,104,105,106,107,108,
                      109,110,111,113,114,115,116,117,118]
MALE_HEAD_VARIANTS = MALE_BODY_VARIANTS  # same range
MALE_HAIR_VARIANTS = MALE_BODY_VARIANTS  # same range

FEMALE_BODY_VARIANTS = [1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,
                        22,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,50,
                        70,72,73,74,75,77,78,79,80,81,82,83,84,85,86,88,89,90,91,92,
                        93,94,95,96,97,99,100,101,102,103,104,105,106,107,108]

# Shadow, mounted, horse variants
EXTRA_VARIANTS = {
    'YY': [999],
    'HH': [16, 18],
    'HB': [16, 18],
    'HT': [16, 18],
}
MOUNT_VARIANTS = [50, 72]  # BD/HD/HR/LH/RH mounted rider variants
WEAPON_VARIANTS_RW = list(range(0, 130))   # 0-129 RW variants
WEAPON_VARIANTS_LW = list(range(0, 60))    # 0-59 LW variants


def build_expected_paths_male() -> list[tuple[str, str]]:
    """Returns list of (name, sourcePath) for all expected male SPR files."""
    paths = []
    prefix = 'spr\\npcres\\man'
    
    # Body, Head, Hair, LH, RH with armor variants
    for part, variants in [('BD', MALE_BODY_VARIANTS), ('HD', MALE_HEAD_VARIANTS),
                            ('HR', MALE_HAIR_VARIANTS),
                            ('LH', MALE_BODY_VARIANTS), ('RH', MALE_BODY_VARIANTS)]:
        for v in variants:
            for action in ACTIONS[:10]:  # ST + RN + AT actions
                name = f'MA_{part}_{v:03d}_{action}.spr'
                paths.append((name, f'{prefix}\\{name}'))
        # Mount rider variants
        for v in MOUNT_VARIANTS:
            for action in ['RD01', 'HR01']:
                name = f'MA_{part}_{v:03d}_{action}.spr'
                paths.append((name, f'{prefix}\\{name}'))

    # Weapons RW
    for v in WEAPON_VARIANTS_RW:
        for action in ACTIONS:
            name = f'MA_RW_{v:03d}_{action}.spr'
            paths.append((name, f'{prefix}\\{name}'))
    
    # Weapons LW
    for v in WEAPON_VARIANTS_LW:
        for action in ACTIONS:
            name = f'MA_LW_{v:03d}_{action}.spr'
            paths.append((name, f'{prefix}\\{name}'))
    
    # Shadow
    for action in ACTIONS:
        name = f'MA_YY_999_{action}.spr'
        paths.append((name, f'{prefix}\\{name}'))
    
    # Horse
    for part in ['HH', 'HB', 'HT']:
        for v in [16, 18]:
            for action in ['RD01', 'HR01']:
                name = f'MA_{part}_{v:03d}_{action}.spr'
                paths.append((name, f'{prefix}\\{name}'))
    
    return paths


def scan_staged_and_build_manifest(gender: str, manifest_path: Path):
    """Scan staged Sprites dir and build manifest entries for given gender prefix (MA_/WO_)."""
    prefix = 'MA_' if gender == 'man' else 'WO_'
    source_root = f'spr\\npcres\\{gender}'
    
    # Load current manifest
    with open(manifest_path) as f:
        manifest_data = json.load(f)
    
    existing = {e['uid']: e for e in manifest_data.get('sprites', [])}
    new_entries = []
    
    # Scan the Sprites directory for files
    total_checked = 0
    total_found = 0
    
    # Generate all paths we expect and check if the uid is on disk
    actions_on_foot = ['ST01', 'ST02', 'ST04', 'ST05', 'ST06',
                       'RN01', 'RN02', 'RN03', 'RN04',
                       'AT01', 'AT03', 'AT05', 'AT07',
                       'MG01', 'MG02', 'MG03', 'MG04', 'MG05']
    actions_mounted = ['RD01', 'HR01']
    all_actions = actions_on_foot + actions_mounted
    
    if gender == 'man':
        body_variants = MALE_BODY_VARIANTS
        parts_with_body_variant = ['BD', 'HD', 'HR', 'LH', 'RH']
    else:
        body_variants = FEMALE_BODY_VARIANTS
        parts_with_body_variant = ['BD', 'HD', 'HR', 'LH', 'RH']
    
    # Body parts with armor/head/hair variants  
    for part in parts_with_body_variant:
        for v in body_variants:
            for action in (actions_on_foot if v not in MOUNT_VARIANTS else []):
                name = f'{prefix}{part}_{v:03d}_{action}.spr'
                sp = f'{source_root}\\{name}'
                u = uid_hex(sp)
                total_checked += 1
                if u and (SPRITES_DIR / f'{u}.spr').exists():
                    total_found += 1
                    if u not in existing:
                        new_entries.append({
                            'name': name,
                            'sourcePath': sp,
                            'uid': u,
                            'unityPath': f'Assets/StreamingAssets/Sprites/{u}.spr',
                            'bytes': (SPRITES_DIR / f'{u}.spr').stat().st_size
                        })
            # Mounted variants for BD/HD/HR/LH/RH
            for action in actions_mounted:
                mount_v = MOUNT_VARIANTS[0] if v not in MOUNT_VARIANTS else v
                if v in MOUNT_VARIANTS:
                    name = f'{prefix}{part}_{v:03d}_{action}.spr'
                    sp = f'{source_root}\\{name}'
                    u = uid_hex(sp)
                    total_checked += 1
                    if u and (SPRITES_DIR / f'{u}.spr').exists():
                        total_found += 1
                        if u not in existing:
                            new_entries.append({
                                'name': name,
                                'sourcePath': sp,
                                'uid': u,
                                'unityPath': f'Assets/StreamingAssets/Sprites/{u}.spr',
                                'bytes': (SPRITES_DIR / f'{u}.spr').stat().st_size
                            })
    
    # Weapon RW
    for v in range(0, 160):
        for action in all_actions:
            name = f'{prefix}RW_{v:03d}_{action}.spr'
            sp = f'{source_root}\\{name}'
            u = uid_hex(sp)
            total_checked += 1
            if u and (SPRITES_DIR / f'{u}.spr').exists():
                total_found += 1
                if u not in existing:
                    new_entries.append({
                        'name': name,
                        'sourcePath': sp,
                        'uid': u,
                        'unityPath': f'Assets/StreamingAssets/Sprites/{u}.spr',
                        'bytes': (SPRITES_DIR / f'{u}.spr').stat().st_size
                    })
    
    # Weapon LW
    for v in range(0, 80):
        for action in all_actions:
            name = f'{prefix}LW_{v:03d}_{action}.spr'
            sp = f'{source_root}\\{name}'
            u = uid_hex(sp)
            total_checked += 1
            if u and (SPRITES_DIR / f'{u}.spr').exists():
                total_found += 1
                if u not in existing:
                    new_entries.append({
                        'name': name,
                        'sourcePath': sp,
                        'uid': u,
                        'unityPath': f'Assets/StreamingAssets/Sprites/{u}.spr',
                        'bytes': (SPRITES_DIR / f'{u}.spr').stat().st_size
                    })
    
    # Shadow
    for action in all_actions:
        name = f'{prefix}YY_999_{action}.spr'
        sp = f'{source_root}\\{name}'
        u = uid_hex(sp)
        total_checked += 1
        if u and (SPRITES_DIR / f'{u}.spr').exists():
            total_found += 1
            if u not in existing:
                new_entries.append({
                    'name': name,
                    'sourcePath': sp,
                    'uid': u,
                    'unityPath': f'Assets/StreamingAssets/Sprites/{u}.spr',
                    'bytes': (SPRITES_DIR / f'{u}.spr').stat().st_size
                })
    
    # Horse body: all PC variants from horseres.txt (variants 2-35, excl. 34)
    # Plus 016/018 from loose folder (already included as body mount variants above)
    horse_prefix = 'MA_' if gender == 'man' else 'WO_'
    all_horse_variants = [2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,
                          20,21,22,23,24,25,26,27,28,29,30,31,32,33,35,
                          110,111,112,113]
    horse_actions = ['RD01', 'HR01', 'HA01', 'HA02', 'HD01', 'HI01', 'HM01', 'HW01',
                     'RD02', 'RD03']  # All horse movement/idle/attack actions
    for part in ['HH', 'HB', 'HT']:
        for v in all_horse_variants:
            for action in horse_actions:
                name = f'{horse_prefix}{part}_{v:03d}_{action}.spr'
                sp = f'{source_root}\\{name}'
                u = uid_hex(sp)
                total_checked += 1
                if u and (SPRITES_DIR / f'{u}.spr').exists():
                    total_found += 1
                    if u not in existing:
                        new_entries.append({
                            'name': name,
                            'sourcePath': sp,
                            'uid': u,
                            'unityPath': f'Assets/StreamingAssets/Sprites/{u}.spr',
                            'bytes': (SPRITES_DIR / f'{u}.spr').stat().st_size
                        })
    
    print(f'{gender}: checked {total_checked}, found on disk {total_found}, new entries {len(new_entries)}')
    
    # Merge with existing entries
    all_entries = list(existing.values()) + new_entries
    
    # Write updated manifest
    from datetime import datetime, timezone
    manifest_data['sprites'] = all_entries
    manifest_data['count'] = len(all_entries)
    manifest_data['generatedAt'] = datetime.now(timezone.utc).isoformat()
    
    with open(manifest_path, 'w', encoding='utf-8') as f:
        json.dump(manifest_data, f, indent=2, ensure_ascii=False)
    
    print(f'  -> Updated manifest with {len(all_entries)} total entries')
    return new_entries


def stage_missing_from_pc(gender: str, manifest_path: Path):
    """Stage SPR files that are missing from disk but exist in PC loose folder."""
    prefix = 'MA_' if gender == 'man' else 'WO_'
    pc_dir = PC_MAN_DIR if gender == 'man' else PC_WOMAN_DIR
    source_root = f'spr\\npcres\\{gender}'
    
    staged = 0
    missing = 0
    
    # Only stage files that exist in PC loose folder
    if not pc_dir.exists():
        print(f'PC dir not found: {pc_dir}')
        return
    
    for spr_file in sorted(pc_dir.glob('*.spr')):
        name = spr_file.name
        sp = f'{source_root}\\{name}'
        u = uid_hex(sp)
        if u and not (SPRITES_DIR / f'{u}.spr').exists():
            dest = SPRITES_DIR / f'{u}.spr'
            shutil.copy2(spr_file, dest)
            staged += 1
    
    print(f'{gender}: staged {staged} new SPRs from PC loose folder')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description='Rebuild player sprite manifests')
    parser.add_argument('--stage-missing', action='store_true', 
                        help='Also stage missing SPRs from PC loose folder')
    args = parser.parse_args()
    
    if args.stage_missing:
        print('Staging missing SPRs from PC loose folder...')
        stage_missing_from_pc('man', MALE_MANIFEST)
        stage_missing_from_pc('woman', FEMALE_MANIFEST)
    
    print('\nRebuilding manifests from disk...')
    scan_staged_and_build_manifest('man', MALE_MANIFEST)
    scan_staged_and_build_manifest('woman', FEMALE_MANIFEST)
    
    print('\nDone! Restart Unity to pick up manifest changes.')
