#!/usr/bin/env python3
import sys
import os
import struct
import uuid
from pathlib import Path

# Add vltktool directory to path so we can import unpak_tool
sys.path.insert(0, '/var/www/vltktool')
try:
    import unpak_tool
except ImportError:
    print("Failed to import unpak_tool. Check path /var/www/vltktool")
    sys.exit(1)

# Target horse variants
VARIANTS = [1, 4, 16, 18, 19] # Variant 1, 4, 16 (previous default), 18, 19 (Siêu Quang)
PARTS = ['HH', 'HB', 'HT']
SUFFIXES = ['RD01', 'HR01', 'HW01']

# Generate target paths
TARGET_PATHS = []
for var in VARIANTS:
    for part in PARTS:
        for suffix in SUFFIXES:
            # Male paths (female mounts also reuse these male horse assets)
            TARGET_PATHS.append(f"spr\\npcres\\man\\MA_{part}_{var:03d}_{suffix}.spr")

# Output directory
DEST_DIR = Path('/var/www/vltk-mobile/Assets/StreamingAssets/Sprites')
DEST_DIR.mkdir(parents=True, exist_ok=True)

# Add uid.py directory to path
sys.path.insert(0, '/var/www/vltk-mobile/harness/.agents/skills/jx-player-visual/scripts')
from uid import uid_hex

# Build UID map for PAK hashing (signed byte, requires leading backslash)
# and DEST name hashing (unsigned byte, does NOT use leading backslash)
path_meta = {}
for p in TARGET_PATHS:
    # 1. PAK path starts with a backslash
    pak_path = "\\" + p
    uid_pak = unpak_tool.file_id_from_bytes(pak_path.encode('gbk'))
    
    # 2. Local destination path does NOT start with a backslash
    uid_dest = uid_hex(p)
    
    path_meta[uid_pak] = {
        'path': p,
        'uid_dest': uid_dest
    }

print(f"Generated {len(path_meta)} target UIDs to find in JX PAKs.")

# Find JX PAK files
PAK_DIRS = [
    Path('/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/SwordOnline/Sources/S3Client/Debug/data'),
    Path('/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/data'),
]

paks = []
for d in PAK_DIRS:
    if d.exists():
        paks.extend(d.glob('*.pak'))
        paks.extend(d.glob('*.PAK'))

# Dedup paks by name
paks = list({p.name.lower(): p for p in paks}.values())
print(f"Found {len(paks)} unique JX PAK files to scan.")

# Scan paks
extracted_count = 0
for pak_path in paks:
    print(f"Scanning {pak_path.name}...")
    try:
        with pak_path.open("rb") as f:
            header = f.read(32)
            if len(header) != 32 or header[:4] != b"PACK":
                continue
            count, index_offset = struct.unpack_from("<II", header, 4)
            f.seek(index_offset)
            
            entries = []
            for _ in range(count):
                record = f.read(16)
                if len(record) != 16:
                    break
                uid, offset, size, flag = struct.unpack("<IIii", record)
                entries.append((uid, offset, size, flag))
                
            for uid, offset, size, flag in entries:
                if uid in path_meta:
                    meta = path_meta[uid]
                    path_str = meta['path']
                    uid_dest = meta['uid_dest']
                    dest_file = DEST_DIR / f"{uid_dest}.spr"
                    
                    print(f"Found {path_str} in {pak_path.name}! Extracting...")
                    try:
                        data = unpak_tool.decompress_entry(pak_path, offset, size, flag)
                        dest_file.write_bytes(data)
                        
                        # Write meta file to keep Unity happy
                        meta_file = DEST_DIR / f"{uid_dest}.spr.meta"
                        if not meta_file.exists():
                            guid = uuid.uuid4().hex
                            meta_file.write_text(f"fileFormatVersion: 2\nguid: {guid}\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
                            
                        print(f"Extracted -> {dest_file.name}")
                        extracted_count += 1
                    except Exception as e:
                        print(f"Failed to extract {path_str}: {e}")
    except Exception as e:
        print(f"Error reading {pak_path.name}: {e}")

print(f"\nDone! Extracted {extracted_count} horse sprites directly to Unity StreamingAssets/Sprites.")
