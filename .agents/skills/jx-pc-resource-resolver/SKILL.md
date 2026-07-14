---
name: jx-pc-resource-resolver
description: >-
  Resolve, lookup, and translate JX Online 1 / Võ Lâm Truyền Kỳ PC resource paths
  (such as SPR, INI, WAV, TXT, LUA) into their decrypted Vietnamese/Chinese meanings,
  and map them to their actual hashed filenames (Pack Hash UIDs) in the unpacked
  jx-source client directory. Use this skill whenever the user asks to look up,
  locate, port, or analyze a feature, skill, map, enemy, item, or asset from JX PC to Mobile,
  or needs to find a specific `.spr` file, client layout, sound, or script within
  `/var/www/jx-source/`.
---

# JX PC Resource Resolver Skill

Use this skill to systematically find and translate Chinese/GBK resource paths from the PC client source/configs to their raw, hashed file names on disk under the unpacked `/var/www/jx-source/pak_unpacked/` directories.

---

## 1. Core JX Pack Hash UID Algorithm

In the JX PC client (and the unpacked folders), files are stored by their **lowercase, backslash-normalized JX Pack Hash UID**. You must compute this hash to find the physical file on disk.

Write a python script or execute a command using this exact hashing logic:

```python
def file_id_from_bytes(path_bytes: bytes) -> int:
    """Computes the JX1/VLTK Pack Hash UID from path bytes."""
    value = 0
    for index, byte in enumerate(path_bytes, 1):
        if 65 <= byte <= 90:  # A-Z -> a-z (normalize to lowercase)
            byte += 32
        c = byte - 256 if byte >= 128 else byte
        value = ((value + index * c) % 0x8000000B) * 0xFFFFFFEF
        value &= 0xFFFFFFFF
    return value ^ 0x12345678

def normalize_resource_path(path: str) -> str:
    """Normalize a path to the JX1 standard: starting with backslash and backslash-separated."""
    path = path.strip().replace("/", "\\")
    if not path:
        return ""
    if not path.startswith("\\"):
        path = "\\" + path
    return path
```

To search for a path like `\Spr\Ui3\主界面\新血条面板.spr`:
1. Normalize to lowercase: `\spr\ui3\主界面\新血条面板.spr` (with backslashes).
2. Encode path to target byte encodings (usually `gbk` or `latin1`/`cp1258`).
3. Run `file_id_from_bytes(encoded_bytes)` -> returns e.g. `0x973816f3`.
4. The file will be named `973816f3.spr` in the unpacked directories (e.g. `unknown/` folders of various PAKs).

---

## 2. Config File Encoding & Decoding (Vietnamese CP1258)

Many project config files (such as `skills.txt`, `npcs.txt`, `missles.txt`) in `/var/www/vltk-mobile/Assets/StreamingAssets/Reference/` contain text stored with **CP1258 / Latin1** Vietnamese encoding.
When read with CP1258, text like:
*   `C玭g k輈h v藅 l` $\rightarrow$ `Công kích vật lý`
*   `Ki課 Nh﹏ Th莕 Th` $\rightarrow$ `Kiên Nhẫn Thần Thông`
*   `Phi Long T筰 Thi猲` $\rightarrow$ `Phi Long Tại Thiên`
*   `B鎛g Ф 竎 C萿` $\rightarrow$ `Bổng Đả Ác Cẩu`

If you are matching skill or item names, write a script to decode these strings using `cp1258` encoding, or perform fuzzy matching to map Vietnamese input names to these values.

---

## 3. Step-by-Step Lookup Workflow

### Step A: Identify the Resource Path in PC Configs
1. **Skills**: Look in `Assets/StreamingAssets/Reference/PcSkill/skills.txt`. Find the row using the Skill ID or Vietnamese/Chinese name.
   *   Read `SkillIcon` for the UI icon sprite.
   *   Read `PreCastSpr` for the casting preparation effect.
   *   Read `MslsGenerateData` / `ChildSkillId` for skill missile properties.
2. **Missiles/Projectiles**: If a skill uses missiles, look up the missile ID in `Assets/StreamingAssets/Reference/PcAttrib/missles.txt`.
   *   Read `AnimFile1`, `AnimFile2`, `AnimFile3`, `AnimFile4` for the projectile animation `.spr` paths.
3. **NPCs/Monsters**: Look in `Assets/StreamingAssets/Reference/PcAttrib/npcs.txt` (or `NpcS.txt` if available).
   *   Read sprite folders and animation paths.

### Step B: Compute the Hashed File Name
Encode the normalized lowercase path to `gbk`, `latin1`, or `utf-8` and calculate the Pack Hash UID.
Example snippet for automatic command execution:
```bash
python3 -c "
def file_id(b):
    val = 0
    for i, byte in enumerate(b, 1):
        if 65 <= byte <= 90: byte += 32
        c = byte - 256 if byte >= 128 else byte
        val = ((val + i * c) % 0x8000000B) * 0xFFFFFFEF
        val &= 0xFFFFFFFF
    return val ^ 0x12345678

path = r'\Spr\Ui\技能图标\icon_sk_gb_21.spr'.lower().replace('/', '\\\\')
if not path.startswith('\\\\'): path = '\\\\' + path
print(f'gbk: {file_id(path.encode(\"gbk\")):08x}')
print(f'latin1: {file_id(path.encode(\"latin1\")):08x}')
"
```

### Step C: Search in `jx-source`
Search for the generated 8-digit hex filename under:
`/var/www/jx-source/pak_unpacked/`

For example:
```bash
find /var/www/jx-source/pak_unpacked/ -name "973816f3.spr"
```

---

## 4. Resource Database Fallbacks
If you cannot calculate the hash or need a fast lookup, inspect:
*   `~/Projects/vltktool/out/label_map_raw.json` (maps many `.spr` files to Vietnamese item/visual names).
*   `~/Projects/vltktool/out/resolved_uids.json` (maps hashes to original paths if resolved).

Use this workflow to reliably locate PC resources and maintain 100% fidelity without guessing file mappings.
