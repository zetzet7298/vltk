import os
import sys
import json
import struct
import shutil
from pathlib import Path
from collections import defaultdict

# Add vltktool folder to path to import decode_best and extract_item_spr
sys.path.append("/var/www/vltktool")
from decode_item_texts_vi import decode_best
import extract_item_spr as ex
import unpak_tool as u

HTML_DIR = Path("/var/www/vltk-mobile/html")
IMAGES_DIR = HTML_DIR / "images"
ITEMS_IMG_DIR = IMAGES_DIR / "items"
NPCS_IMG_DIR = IMAGES_DIR / "npcs"
CHARS_IMG_DIR = IMAGES_DIR / "characters"

# Clean up existing images folder to remove old non-ascii names
if IMAGES_DIR.exists():
    shutil.rmtree(IMAGES_DIR)

# Re-create directories
ITEMS_IMG_DIR.mkdir(parents=True, exist_ok=True)
NPCS_IMG_DIR.mkdir(parents=True, exist_ok=True)
CHARS_IMG_DIR.mkdir(parents=True, exist_ok=True)

UNPACKED_ROOT = Path("/var/www/vltksource_new/vl_update_27/pak_unpacked")
CLIENT_SETTINGS_DIR = Path("/var/www/vltksource_new/vl_update_27/Client 6.0/settings")

print("Pre-scanning unpacked_root for .spr files...")
real_paths = {}
unknown_files = {}

for p in UNPACKED_ROOT.rglob("*"):
    if not p.is_file():
        continue
    
    # 1. Index real path relative to its pak folder
    try:
        parts = p.parts
        pak_idx = len(UNPACKED_ROOT.parts)
        if len(parts) > pak_idx + 1:
            rel_parts = parts[pak_idx+1:]
            rel_path = "/".join(rel_parts).lower()
            real_paths[rel_path] = p
            # Also index lowercase file name
            real_paths[p.name.lower()] = p
    except Exception:
        pass

    # 2. Index unknown files by their hex uid (stem)
    if "unknown" in str(p).lower() and p.suffix.lower() in (".spr", ".dat"):
        stem = p.stem.lower()
        unknown_files[stem] = p

print(f"Index built: {len(real_paths)} real paths, {len(unknown_files)} unknown files.")

def resolve_spr_path(logical_path):
    if not logical_path:
        return None
    norm = u.normalize_resource_path(logical_path)
    rel = norm.replace("\\", "/").strip("/").lower()
    
    # Direct match
    if rel in real_paths:
        return real_paths[rel]
        
    # Basename match
    base = Path(rel).name
    if base in real_paths:
        return real_paths[base]
        
    # Hashing matching
    for enc in ["utf-8", "gbk", "cp1258", "latin1"]:
        try:
            b = norm.encode(enc)
            uid = u.file_id_from_bytes(b)
            uid_hex = f"{uid:08x}"
            if uid_hex in unknown_files:
                return unknown_files[uid_hex]
        except Exception:
            pass
            
    return None

def extract_frame_0(spr_path_str, out_png_path_str):
    spr_path = Path(spr_path_str)
    out_png_path = Path(out_png_path_str)
    
    # If already extracted, skip
    if out_png_path.exists():
        return True
        
    try:
        data = spr_path.read_bytes()
        frame_count, color_count, frames, frame_data_off = ex.parse_frames(data)
        if frame_count == 0 or not frames:
            return False
        palette = data[0x20 : 0x20 + color_count * 3]
        
        # Get frame 0
        frame = frames[0]
        blob_start = frame_data_off + frame.rel_offset
        blob_end = blob_start + frame.size
        blob = data[blob_start:blob_end]
        
        # Decode to RGBA
        width, height, rgba = ex.decode_frame_rgba(data, palette, blob, flip_v=False)
        if width == 0 or height == 0:
            return False
            
        ex.write_png_rgba(out_png_path, width, height, rgba)
        return True
    except Exception as e:
        return False

# Category to ASCII Slug mapping to avoid URL encoding issues on web servers
category_slugs = {
    "Áo giáp": "ao_giap",
    "Nón": "non",
    "Giày": "giay",
    "Bao tay": "bao_tay",
    "Đai lưng": "dai_lung",
    "Dây chuyền": "day_chuyen",
    "Nhẫn": "nhan",
    "Ngọc bội": "ngoc_boi",
    "Vũ khí cận chiến": "vu_khi_can_chien",
    "Vũ khí tầm xa": "vu_khi_tam_xa",
    "Quest Items": "quest_items",
    "Magic Items": "magic_items",
}

# ==================== STEP 1: PARSE CONFIGS ====================
print("Parsing equipment configurations...")
equip_files = {
    "Áo giáp": "item/004/armor.txt",
    "Nón": "item/004/helm.txt",
    "Giày": "item/004/boot.txt",
    "Bao tay": "item/004/cuff.txt",
    "Đai lưng": "item/004/belt.txt",
    "Dây chuyền": "item/004/amulet.txt",
    "Nhẫn": "item/004/ring.txt",
    "Ngọc bội": "item/004/pendant.txt",
    "Vũ khí cận chiến": "item/004/meleeweapon.txt",
    "Vũ khí tầm xa": "item/004/rangeweapon.txt",
}

equip_data = defaultdict(list)

for cat_name, rel in equip_files.items():
    file_path = CLIENT_SETTINGS_DIR / rel
    if not file_path.exists():
        print(f"Skipping missing file: {rel}")
        continue
    with open(file_path, "rb") as f:
        text, _, _ = decode_best(f.read())
    lines = text.splitlines()
    header = lines[0].split("\t")
    
    # level column index (usually 11)
    # spr column index (usually 4)
    # desc column index (usually 8)
    for idx, line in enumerate(lines[1:]):
        cols = line.split("\t")
        if len(cols) < 12:
            continue
        name = cols[0].strip()
        spr_logical = cols[4].strip()
        desc = cols[8].strip()
        level = cols[11].strip()
        
        if not name or not spr_logical:
            continue
            
        # Try resolving and extracting
        spr_disk = resolve_spr_path(spr_logical)
        slug = category_slugs.get(cat_name, "item")
        png_rel_path = f"items/{slug}_L{level}_{idx}.png"
        png_abs_path = IMAGES_DIR / png_rel_path
        
        success = False
        if spr_disk:
            success = extract_frame_0(spr_disk, png_abs_path)
            
        if success:
            equip_data[cat_name].append({
                "name": name,
                "desc": desc,
                "level": level,
                "img_path": f"./images/{png_rel_path}"
            })

print(f"Parsed equipment categories: { {k: len(v) for k, v in equip_data.items()} }")

# Parse Quest Items
print("Parsing quest items...")
quest_items = []
questkey_path = CLIENT_SETTINGS_DIR / "item/004/questkey.txt"
if questkey_path.exists():
    with open(questkey_path, "rb") as f:
        text, _, _ = decode_best(f.read())
    lines = text.splitlines()
    for idx, line in enumerate(lines[1:]):
        cols = line.split("\t")
        if len(cols) < 8:
            continue
        name = cols[0].strip()
        spr_logical = cols[3].strip() # Quest items has SPR at column index 3
        desc = cols[7].strip()
        if not name or not spr_logical:
            continue
        
        spr_disk = resolve_spr_path(spr_logical)
        png_rel_path = f"items/quest_{idx}.png"
        png_abs_path = IMAGES_DIR / png_rel_path
        
        success = False
        if spr_disk:
            success = extract_frame_0(spr_disk, png_abs_path)
            
        if success:
            quest_items.append({
                "name": name,
                "desc": desc,
                "img_path": f"./images/{png_rel_path}"
            })
            if len(quest_items) >= 100:  # Representative sample
                break

# Parse Magic/Gold Items
print("Parsing magic / gold items...")
magic_items = []
goldequip_path = CLIENT_SETTINGS_DIR / "item/004/goldequip.txt"
if goldequip_path.exists():
    with open(goldequip_path, "rb") as f:
        text, _, _ = decode_best(f.read())
    lines = text.splitlines()
    for idx, line in enumerate(lines[1:]):
        cols = line.split("\t")
        if len(cols) < 9:
            continue
        name = cols[0].strip()
        spr_logical = cols[4].strip()
        desc = cols[8].strip()
        if not name or not spr_logical:
            continue
            
        spr_disk = resolve_spr_path(spr_logical)
        png_rel_path = f"items/magic_{idx}.png"
        png_abs_path = IMAGES_DIR / png_rel_path
        
        success = False
        if spr_disk:
            success = extract_frame_0(spr_disk, png_abs_path)
            
        if success:
            magic_items.append({
                "name": name,
                "desc": desc,
                "img_path": f"./images/{png_rel_path}"
            })
            if len(magic_items) >= 100:  # Representative sample
                break

# ==================== STEP 2: PARSE NPCS & MONSTERS ====================
print("Parsing NPCres and NPC settings...")
npcres_dir = CLIENT_SETTINGS_DIR / "npcres"
char_types = {}
char_types_path = npcres_dir / "ÈËÎïÀàÐÍ.txt"
if char_types_path.exists():
    with open(char_types_path, "rb") as f:
        text, _, _ = decode_best(f.read())
    lines = text.splitlines()
    for line in lines[1:]:
        cols = line.split("\t")
        if len(cols) > 2:
            char_name = cols[0]
            char_type = cols[1]
            res_path = cols[2].replace("\\", "/").strip("/")
            char_types[char_name] = {
                "type": char_type,
                "res_path": res_path
            }

common_npc_res = {}
common_res_path = npcres_dir / "ÆÕÍ¨npc×ÊÔ´.txt"
if common_res_path.exists():
    with open(common_res_path, "rb") as f:
        text, _, _ = decode_best(f.read())
    lines = text.splitlines()
    header = lines[0].split("\t")
    for line in lines[1:]:
        cols = line.split("\t")
        if not cols:
            continue
        npc_name = cols[0]
        actions = {}
        for i, col in enumerate(cols[1:]):
            if i + 1 < len(header):
                action_name = header[i+1]
                actions[action_name] = col.strip()
        common_npc_res[npc_name] = actions

# Parse NPCS list
npcs_path = CLIENT_SETTINGS_DIR / "npcs.txt"
npc_list = []
boss_list = []
monster_list = []

if npcs_path.exists():
    with open(npcs_path, "rb") as f:
        text, _, _ = decode_best(f.read())
    lines = text.splitlines()
    for idx, line in enumerate(lines[1:]):
        cols = line.split("\t")
        if len(cols) < 12:
            continue
        name = cols[0].strip()
        kind = cols[1].strip()
        series = cols[3].strip() # 五行
        is_boss = cols[4].strip() # Treasure/Boss
        res_type = cols[11].strip()
        
        if not name or not res_type:
            continue
            
        # Resolve SPR path
        spr_filename = None
        # Try finding in common_npc_res
        actions = common_npc_res.get(res_type, {})
        for act in ["NormalStand1", "FightStand", "NormalWalk", "FightWalk"]:
            if actions.get(act):
                spr_filename = actions[act]
                break
        if not spr_filename and actions:
            # Get any non-empty action
            for act, val in actions.items():
                if val:
                    spr_filename = val
                    break
                    
        if not spr_filename:
            # Try setting directory directly
            res_dir = char_types.get(res_type, {}).get("res_path", "")
            if res_dir:
                spr_filename = f"{res_type}.spr"
                
        if not spr_filename:
            continue
            
        # Resolve path
        spr_logical = None
        res_dir = char_types.get(res_type, {}).get("res_path") if res_type in char_types else ""
        if res_dir:
            spr_logical = f"{res_dir}/{spr_filename}"
        else:
            spr_logical = spr_filename
            
        spr_disk = resolve_spr_path(spr_logical)
        png_rel_path = f"npcs/{res_type}_{idx}.png"
        png_abs_path = IMAGES_DIR / png_rel_path
        
        success = False
        if spr_disk:
            success = extract_frame_0(spr_disk, png_abs_path)
            
        if success:
            series_names = {"0": "Kim", "1": "Mộc", "2": "Thủy", "3": "Hỏa", "4": "Thổ", "5": "Vô"}
            series_vi = series_names.get(series, "Không")
            
            item_info = {
                "name": name,
                "res": res_type,
                "series": series_vi,
                "img_path": f"./images/{png_rel_path}"
            }
            
            # Categorize
            name_lower = name.lower()
            is_boss_flag = (
                "boss" in name_lower or 
                name_lower.startswith("boss") or 
                is_boss in ["8", "12", "25", "30"] or 
                "đại vương" in name_lower
            )
            
            if is_boss_flag:
                boss_list.append(item_info)
            elif kind in ["0", "4", "5"]:
                monster_list.append(item_info)
            else:
                npc_list.append(item_info)

# De-duplicate lists by name to keep visual list clean
def deduplicate_by_name(items):
    seen = set()
    dedup = []
    for item in items:
        cleaned_name = item["name"].strip()
        if cleaned_name not in seen:
            seen.add(cleaned_name)
            dedup.append(item)
    return dedup

npc_list = deduplicate_by_name(npc_list)
boss_list = deduplicate_by_name(boss_list)
monster_list = deduplicate_by_name(monster_list)

print(f"Loaded: NPCs={len(npc_list)}, Bosses={len(boss_list)}, Monsters={len(monster_list)}")

# ==================== STEP 3: PLAYER CHARACTERS ====================
print("Processing player characters...")
player_chars = []
char_sprites = {
    "Nam Tân Thủ (Novice)": ("spr/npcres/man/ma_bd_001_st01.spr", "Tân Thủ"),
    "Nữ Tân Thủ (Novice)": ("spr/npcres/woman/fm_bd_001_st01.spr", "Tân Thủ"),
    "Nam hệ Kim (Thiếu Lâm/Thiên Vương)": ("spr/npcres/man/ma_bd_002_st01.spr", "Hệ Kim"),
    "Nữ hệ Kim": ("spr/npcres/woman/fm_bd_002_st01.spr", "Hệ Kim"),
    "Nam hệ Mộc (Đường Môn/Ngũ Độc)": ("spr/npcres/man/ma_bd_004_st01.spr", "Hệ Mộc"),
    "Nữ hệ Mộc": ("spr/npcres/woman/fm_bd_004_st01.spr", "Hệ Mộc"),
    "Nam hệ Thủy": ("spr/npcres/man/ma_bd_006_st01.spr", "Hệ Thủy"),
    "Nữ hệ Thủy (Nga My/Thúy Yên)": ("spr/npcres/woman/fm_bd_006_st01.spr", "Hệ Thủy"),
    "Nam hệ Hỏa (Cái Bang/Thiên Nhẫn)": ("spr/npcres/man/ma_bd_008_st01.spr", "Hệ Hỏa"),
    "Nữ hệ Hỏa": ("spr/npcres/woman/fm_bd_008_st01.spr", "Hệ Hỏa"),
    "Nam hệ Thổ (Võ Đang/Côn Lôn)": ("spr/npcres/man/ma_bd_010_st01.spr", "Hệ Thổ"),
    "Nữ hệ Thổ": ("spr/npcres/woman/fm_bd_010_st01.spr", "Hệ Thổ"),
}

for char_desc, (spr_logical, element) in char_sprites.items():
    spr_disk = resolve_spr_path(spr_logical)
    filename = Path(spr_logical).stem
    png_rel_path = f"characters/{filename}.png"
    png_abs_path = IMAGES_DIR / png_rel_path
    
    success = False
    if spr_disk:
        success = extract_frame_0(spr_disk, png_abs_path)
        
    if success:
        player_chars.append({
            "name": char_desc,
            "element": element,
            "img_path": f"./images/{png_rel_path}"
        })

print(f"Processed {len(player_chars)} player characters.")

# ==================== STEP 4: GENERATE HTML ====================
print("Generating static HTML...")

html_content = """<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Thư Viện Vật Phẩm & NPC Võ Lâm Truyền Kỳ</title>
    <script src="https://cdn.tailwindcss.com"></script>
    <style>
        .tab-content { display: none; }
        .tab-content.active { display: block; }
        .tab-btn.active { border-bottom-width: 4px; border-color: #f59e0b; color: #f59e0b; font-weight: 700; }
    </style>
</head>
<body class="bg-gray-900 text-gray-100 font-sans min-h-screen">

    <!-- Header -->
    <header class="bg-gray-800 border-b border-gray-700 py-6 px-8 shadow-md">
        <div class="max-w-7xl mx-auto flex flex-col md:flex-row justify-between items-center gap-4">
            <div>
                <h1 class="text-3xl font-extrabold text-amber-500 tracking-wide">VÕ LÂM TRUYỀN KỲ PC</h1>
                <p class="text-gray-400 mt-1">Trang thông tin và hình ảnh các đối tượng (Vật phẩm, Trang bị, NPC, Quái, Boss, Nhân vật)</p>
            </div>
            <!-- Search Bar -->
            <div class="w-full md:w-80">
                <input type="text" id="searchInput" oninput="filterContent()" placeholder="Tìm kiếm đối tượng..." 
                       class="w-full bg-gray-950 border border-gray-600 rounded-lg py-2 px-4 text-white focus:outline-none focus:border-amber-500 transition">
            </div>
        </div>
    </header>

    <!-- Main Tabs -->
    <div class="max-w-7xl mx-auto px-4 py-8">
        <div class="flex border-b border-gray-700 mb-8 overflow-x-auto whitespace-nowrap scrollbar-hide">
            <button onclick="switchTab('tab-items')" id="btn-tab-items" class="tab-btn active px-6 py-3 text-lg text-gray-400 hover:text-white transition">Mục 1: Vật Phẩm & Trang Bị</button>
            <button onclick="switchTab('tab-npcs')" id="btn-tab-npcs" class="tab-btn px-6 py-3 text-lg text-gray-400 hover:text-white transition">Mục 2: NPC, Boss & Quái</button>
            <button onclick="switchTab('tab-characters')" id="btn-tab-characters" class="tab-btn px-6 py-3 text-lg text-gray-400 hover:text-white transition">Mục 3: Nhân Vật & Tân Thủ</button>
        </div>

        <!-- TAB 1: ITEMS & EQUIPMENT -->
        <div id="tab-items" class="tab-content active">
            <!-- Inner tabs for item categories -->
            <div class="flex flex-wrap gap-2 mb-6">
"""

# Generate buttons for equip/item subcategories
categories = list(equip_data.keys()) + ["Quest Items", "Magic Items"]
for idx, cat in enumerate(categories):
    active_classes = "bg-amber-600 text-white" if idx == 0 else "bg-gray-800 hover:bg-gray-700 text-gray-400"
    html_content += f"""
                <button onclick="filterItems('{cat}')" class="item-cat-btn {active_classes} px-4 py-2 rounded-full text-sm font-semibold transition">{cat}</button>"""

html_content += """
            </div>

            <!-- Items Grid -->
            <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6" id="itemsGrid">
"""

# Append equipments to grid
for cat, items in equip_data.items():
    for item in items:
        html_content += f"""
                <div class="item-card bg-gray-800 rounded-xl p-6 border border-gray-700 flex gap-4 items-center shadow-lg transition hover:scale-102 hover:border-amber-500" data-category="{cat}" data-name="{item['name']}">
                    <div class="bg-gray-950 p-2 rounded-lg border border-gray-600 flex-shrink-0 flex items-center justify-center w-16 h-20">
                        <img src="{item['img_path']}" alt="{item['name']}" class="max-h-full max-w-full object-contain">
                    </div>
                    <div>
                        <span class="text-xs font-bold text-amber-500 uppercase tracking-wider">{cat} (Cấp {item['level']})</span>
                        <h3 class="text-lg font-bold text-white mt-1 item-name">{item['name']}</h3>
                        <p class="text-sm text-gray-400 mt-2 line-clamp-2">{item['desc']}</p>
                    </div>
                </div>"""

# Append quest items to grid
for item in quest_items:
    html_content += f"""
                <div class="item-card bg-gray-800 rounded-xl p-6 border border-gray-700 flex gap-4 items-center shadow-lg transition hover:scale-102 hover:border-amber-500" data-category="Quest Items" data-name="{item['name']}">
                    <div class="bg-gray-950 p-2 rounded-lg border border-gray-600 flex-shrink-0 flex items-center justify-center w-16 h-20">
                        <img src="{item['img_path']}" alt="{item['name']}" class="max-h-full max-w-full object-contain">
                    </div>
                    <div>
                        <span class="text-xs font-bold text-teal-400 uppercase tracking-wider">Quest Items</span>
                        <h3 class="text-lg font-bold text-white mt-1 item-name">{item['name']}</h3>
                        <p class="text-sm text-gray-400 mt-2 line-clamp-2">{item['desc']}</p>
                    </div>
                </div>"""

# Append magic items to grid
for item in magic_items:
    html_content += f"""
                <div class="item-card bg-gray-800 rounded-xl p-6 border border-gray-700 flex gap-4 items-center shadow-lg transition hover:scale-102 hover:border-amber-500" data-category="Magic Items" data-name="{item['name']}">
                    <div class="bg-gray-950 p-2 rounded-lg border border-gray-600 flex-shrink-0 flex items-center justify-center w-16 h-20">
                        <img src="{item['img_path']}" alt="{item['name']}" class="max-h-full max-w-full object-contain">
                    </div>
                    <div>
                        <span class="text-xs font-bold text-purple-400 uppercase tracking-wider">Magic Items</span>
                        <h3 class="text-lg font-bold text-white mt-1 item-name">{item['name']}</h3>
                        <p class="text-sm text-gray-400 mt-2 line-clamp-2">{item['desc']}</p>
                    </div>
                </div>"""

html_content += """
            </div>
        </div>

        <!-- TAB 2: NPCS, BOSSES & MONSTERS -->
        <div id="tab-npcs" class="tab-content">
            <!-- Inner tabs for npc categories -->
            <div class="flex gap-2 mb-6">
                <button onclick="filterNpcs('NPC')" class="npc-cat-btn bg-amber-600 text-white px-4 py-2 rounded-full text-sm font-semibold transition">Thương nhân / NPC</button>
                <button onclick="filterNpcs('Boss')" class="npc-cat-btn bg-gray-800 hover:bg-gray-700 text-gray-400 px-4 py-2 rounded-full text-sm font-semibold transition">Boss Hoàng Kim</button>
                <button onclick="filterNpcs('Monster')" class="npc-cat-btn bg-gray-800 hover:bg-gray-700 text-gray-400 px-4 py-2 rounded-full text-sm font-semibold transition">Quái vật / Thú dữ</button>
            </div>

            <!-- NPCs Grid -->
            <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6" id="npcsGrid">
"""

# Append NPCs
for item in npc_list[:120]: # Show representative list
    html_content += f"""
                <div class="npc-card bg-gray-800 rounded-xl p-6 border border-gray-700 flex gap-4 items-center shadow-lg transition hover:scale-102 hover:border-amber-500" data-category="NPC" data-name="{item['name']}">
                    <div class="bg-gray-950 p-2 rounded-lg border border-gray-600 flex-shrink-0 flex items-center justify-center w-20 h-24">
                        <img src="{item['img_path']}" alt="{item['name']}" class="max-h-full max-w-full object-contain">
                    </div>
                    <div>
                        <span class="text-xs font-bold text-emerald-400 uppercase tracking-wider">NPC / Người dân</span>
                        <h3 class="text-lg font-bold text-white mt-1 npc-name">{item['name']}</h3>
                        <p class="text-sm text-gray-400 mt-2">Dân thường hoặc thương nhân trong giang hồ.</p>
                    </div>
                </div>"""

# Append Bosses
for item in boss_list[:120]:
    html_content += f"""
                <div class="npc-card bg-gray-800 rounded-xl p-6 border border-gray-700 flex gap-4 items-center shadow-lg transition hover:scale-102 hover:border-amber-500" data-category="Boss" data-name="{item['name']}">
                    <div class="bg-gray-950 p-2 rounded-lg border border-gray-600 flex-shrink-0 flex items-center justify-center w-20 h-24">
                        <img src="{item['img_path']}" alt="{item['name']}" class="max-h-full max-w-full object-contain">
                    </div>
                    <div>
                        <span class="text-xs font-bold text-rose-500 uppercase tracking-wider">Boss Hoàng Kim - Ngũ Hành: {item['series']}</span>
                        <h3 class="text-lg font-bold text-white mt-1 npc-name">{item['name']}</h3>
                        <p class="text-sm text-gray-400 mt-2">Thủ lĩnh võ lâm khét tiếng, mang lượng kho báu khổng lồ.</p>
                    </div>
                </div>"""

# Append Monsters
for item in monster_list[:120]:
    html_content += f"""
                <div class="npc-card bg-gray-800 rounded-xl p-6 border border-gray-700 flex gap-4 items-center shadow-lg transition hover:scale-102 hover:border-amber-500" data-category="Monster" data-name="{item['name']}">
                    <div class="bg-gray-950 p-2 rounded-lg border border-gray-600 flex-shrink-0 flex items-center justify-center w-20 h-24">
                        <img src="{item['img_path']}" alt="{item['name']}" class="max-h-full max-w-full object-contain">
                    </div>
                    <div>
                        <span class="text-xs font-bold text-amber-400 uppercase tracking-wider">Quái Vật - Ngũ Hành: {item['series']}</span>
                        <h3 class="text-lg font-bold text-white mt-1 npc-name">{item['name']}</h3>
                        <p class="text-sm text-gray-400 mt-2">Dã thú hoặc lâu la trên các bản đồ luyện công.</p>
                    </div>
                </div>"""

html_content += """
            </div>
        </div>

        <!-- TAB 3: CHARACTERS & NOVICES -->
        <div id="tab-characters" class="tab-content">
            <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
"""

# Append player characters
for item in player_chars:
    html_content += f"""
                <div class="char-card bg-gray-800 rounded-xl p-6 border border-gray-700 flex gap-4 items-center shadow-lg transition hover:scale-102 hover:border-amber-500" data-name="{item['name']}">
                    <div class="bg-gray-950 p-2 rounded-lg border border-gray-600 flex-shrink-0 flex items-center justify-center w-24 h-32">
                        <img src="{item['img_path']}" alt="{item['name']}" class="max-h-full max-w-full object-contain">
                    </div>
                    <div>
                        <span class="text-xs font-bold text-indigo-400 uppercase tracking-wider">Nhân vật {item['element']}</span>
                        <h3 class="text-lg font-bold text-white mt-1 char-name">{item['name']}</h3>
                        <p class="text-sm text-gray-400 mt-2">Tạo hình nhân vật phái võ lâm tương ứng từ game PC.</p>
                    </div>
                </div>"""

html_content += """
            </div>
        </div>
    </div>

    <!-- Footer -->
    <footer class="bg-gray-800 border-t border-gray-700 py-6 text-center text-gray-400 text-sm mt-12">
        <div class="max-w-7xl mx-auto px-4">
            <p>&copy; 2026 Võ Lâm Truyền Kỳ PC - Thư Viện Đối Tượng. Sinh tự động bởi VLTK Mobile Harness Tool.</p>
        </div>
    </footer>

    <!-- Script for interaction -->
    <script>
        function switchTab(tabId) {
            // Hide all tab contents
            document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));
            // Remove active style from buttons
            document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
            
            // Show active tab
            document.getElementById(tabId).classList.add('active');
            document.getElementById('btn-' + tabId).classList.add('active');
            
            // Trigger filter update to respect the newly active tab
            filterContent();
        }

        function getActiveItemCategory() {
            let activeBtn = document.querySelector('.item-cat-btn.bg-amber-600');
            return activeBtn ? activeBtn.innerText : 'Áo giáp';
        }

        function getActiveNpcCategory() {
            let activeBtn = document.querySelector('.npc-cat-btn.bg-amber-600');
            if (activeBtn) {
                let text = activeBtn.innerText;
                if (text.includes('NPC') || text.includes('Thương nhân')) return 'NPC';
                if (text.includes('Boss')) return 'Boss';
                if (text.includes('Quái vật') || text.includes('Thú dữ')) return 'Monster';
            }
            return 'NPC';
        }

        function filterItems(category) {
            // Update button styles
            document.querySelectorAll('.item-cat-btn').forEach(btn => {
                if (btn.innerText === category) {
                    btn.classList.add('bg-amber-600', 'text-white');
                    btn.classList.remove('bg-gray-800', 'text-gray-400');
                } else {
                    btn.classList.remove('bg-amber-600', 'text-white');
                    btn.classList.add('bg-gray-800', 'text-gray-400');
                }
            });
            // Apply filtering
            filterContent();
        }

        function filterNpcs(category) {
            // Update button styles
            document.querySelectorAll('.npc-cat-btn').forEach(btn => {
                let isMatch = false;
                if (category === 'NPC' && (btn.innerText.includes('NPC') || btn.innerText.includes('Thương nhân'))) isMatch = true;
                if (category === 'Boss' && btn.innerText.includes('Boss')) isMatch = true;
                if (category === 'Monster' && (btn.innerText.includes('Quái vật') || btn.innerText.includes('Thú dữ'))) isMatch = true;
                
                if (isMatch) {
                    btn.classList.add('bg-amber-600', 'text-white');
                    btn.classList.remove('bg-gray-800', 'text-gray-400');
                } else {
                    btn.classList.remove('bg-amber-600', 'text-white');
                    btn.classList.add('bg-gray-800', 'text-gray-400');
                }
            });
            // Apply filtering
            filterContent();
        }

        function filterContent() {
            let input = document.getElementById('searchInput').value.toLowerCase();
            let activeTabBtn = document.querySelector('.tab-btn.active');
            if (!activeTabBtn) return;
            
            let activeTab = activeTabBtn.id;
            
            if (activeTab === 'btn-tab-items') {
                let activeCat = getActiveItemCategory();
                document.querySelectorAll('.item-card').forEach(c => {
                    let name = c.getAttribute('data-name').toLowerCase();
                    let cat = c.getAttribute('data-category');
                    if (cat === activeCat && (input === '' || name.includes(input))) {
                        c.style.display = 'flex';
                    } else {
                        c.style.display = 'none';
                    }
                });
            } else if (activeTab === 'btn-tab-npcs') {
                let activeCat = getActiveNpcCategory();
                document.querySelectorAll('.npc-card').forEach(c => {
                    let name = c.getAttribute('data-name').toLowerCase();
                    let cat = c.getAttribute('data-category');
                    if (cat === activeCat && (input === '' || name.includes(input))) {
                        c.style.display = 'flex';
                    } else {
                        c.style.display = 'none';
                    }
                });
            } else if (activeTab === 'btn-tab-characters') {
                document.querySelectorAll('.char-card').forEach(c => {
                    let name = c.getAttribute('data-name').toLowerCase();
                    if (input === '' || name.includes(input)) {
                        c.style.display = 'flex';
                    } else {
                        c.style.display = 'none';
                    }
                });
            }
        }

        // Initialize defaults
        window.onload = function() {
            filterItems('Áo giáp');
            filterNpcs('NPC');
        }
    </script>
</body>
</html>
"""

# Write HTML file
index_html_path = HTML_DIR / "index.html"
index_html_path.write_text(html_content, encoding="utf-8")
print(f"index.html generated successfully at {index_html_path}")
