import os
import sys
import shutil
from pathlib import Path

# Add uid.py directory to path
sys.path.insert(0, '/var/www/vltk-mobile/harness/.agents/skills/jx-player-visual/scripts')
from uid import uid_hex

SPRITES_DIR = Path('/var/www/vltk-mobile/Assets/StreamingAssets/Sprites')
SPRITES_DIR.mkdir(parents=True, exist_ok=True)

SPRITES_RUNTIME_DIR = Path('/var/www/vltk-mobile/SpritesRuntime')
PC_MAN_DIR = Path("/var/www/vltksource_new/01_tinh_kiem_source/source/00.src-tinh-kiem/Utility/Run/spr/npcres/man")

# Actions & Suffixes
ACTIONS = ['ST01', 'ST02', 'ST04', 'ST05', 'ST06',
           'RN01', 'RN02', 'RN03', 'RN04',
           'AT01', 'AT03', 'AT05', 'AT07',
           'MG01', 'MG02', 'MG03', 'MG04', 'MG05',
           'RD01', 'HR01']

def stage_path(source_path):
    uid = uid_hex(source_path)
    if not uid:
        print(f"Failed to hash: {source_path}")
        return False
    
    dest_path = SPRITES_DIR / f"{uid}.spr"
    if dest_path.exists():
        return True # Already staged
    
    # 1. Try to copy from SpritesRuntime
    runtime_src = SPRITES_RUNTIME_DIR / f"{uid}.spr"
    if runtime_src.exists():
        shutil.copy(runtime_src, dest_path)
        print(f"Staged {source_path} -> {uid}.spr (from SpritesRuntime)")
        return True
    
    # 2. Try to copy from loose PC files if it is a man file
    if source_path.startswith("spr\\npcres\\man\\"):
        filename = source_path.split("\\")[-1]
        pc_src = PC_MAN_DIR / filename
        if pc_src.exists():
            shutil.copy(pc_src, dest_path)
            print(f"Staged {source_path} -> {uid}.spr (from PC loose man)")
            return True
            
    print(f"Missing: {source_path} ({uid}.spr)")
    return False

def main():
    staged_count = 0
    missing_count = 0
    
    # 1. MALE PLAYER
    print("--- Staging Male Player ---")
    male_parts = ['YY', 'BD', 'HD', 'HR', 'LH', 'RH', 'LW', 'RW']
    for part in male_parts:
        variant = 999 if part == 'YY' else (0 if part in ['LW', 'RW'] else 19)
        # Try both variant 0 and other weapon/outfit variants
        variants = [variant]
        if part in ['LW', 'RW']:
            variants.extend([1, 2, 10]) # Short, Dual, Long weapon
            
        for v in variants:
            for suffix in ACTIONS:
                p = f"spr\\npcres\\man\\MA_{part}_{v:03d}_{suffix}.spr"
                if stage_path(p):
                    staged_count += 1
                else:
                    missing_count += 1
                    
    # Horse parts
    for part in ['HH', 'HB', 'HT']:
        for suffix in ['RD01', 'HR01', 'HW01']:
            for var in [1, 4, 18, 19]:
                p = f"spr\\npcres\\man\\MA_{part}_{var:03d}_{suffix}.spr"
                if stage_path(p):
                    staged_count += 1
                else:
                    missing_count += 1
                
    # Extra horse items
    for horse in ['horse001.spr', 'horse005.spr']:
        p = f"spr\\item\\equip\\horse\\{horse}"
        if stage_path(p):
            staged_count += 1
        else:
            missing_count += 1

    # 2. FEMALE PLAYER
    print("\n--- Staging Female Player ---")
    female_parts = ['BD', 'HD', 'HR', 'LH', 'RH']
    for part in female_parts:
        for suffix in ACTIONS:
            p = f"spr\\npcres\\woman\\FM_{part}_050_{suffix}.spr"
            if stage_path(p):
                staged_count += 1
            else:
                missing_count += 1
                
    print(f"\nDone! Staged: {staged_count}, Missing/Unresolved: {missing_count}")

if __name__ == "__main__":
    main()
