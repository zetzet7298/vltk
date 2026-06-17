# Mixed-Encoding Files in PC Source (TCVN3 + GBK)

## The problem

Several PC data files under `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem` are **partially Việt-hoá**: some columns contain Vietnamese text (TCVN3-encoded) while others retain original Chinese (GBK-encoded). A single tab-separated row can mix both encodings.

**Known mixed-encoding files:**
- `Client 6.0/settings/thiefskill.txt` — skill names are TCVN3 Vietnamese (e.g. "Kiếp Phá Thiên Tôn"), SPR/sound/icon paths are GBK Chinese (e.g. `劫富济贫1.spr`)
- `Client 6.0/settings/npcs.txt` — contains a complex mix of:
  1. Vietnamese names in TCVN3 (e.g. `Giới Luật Viện đầu tọa`, `Bắp cải 1`).
  2. Chinese names in GBK (e.g. `事件总管` = Event Manager, `灶台` = Stove, `武林盟主` = Martial Arts Alliance Leader).
  3. Vietnamese names double-encoded in GBK resulting in Mojibake (e.g. `A S琻` = `A Sơn`, `Bao c竧` = `Bao cát`, `ti觰` = `tiểu`).

**Fully GBK files** (no Vietnamese, decode entirely as GBK):
- `skills.txt`, `skills1.txt`, `missles1.txt`, most `Reference/PcNpc/*`, most `Reference/PcSkill/*`

**Fully TCVN3 files** (already Việt-hoá, decode as TCVN3):
- Some `PcTask/*`, `PcMap/*` files that were specifically localised

## Detection heuristic

1. Read raw bytes of the first data row.
2. If columns 0–4 are pure ASCII (numeric IDs) but later columns contain bytes in range 0x80–0xFF, check whether the high-byte sequences decode cleanly as GBK or TCVN3.
3. **If name-like columns decode as readable Vietnamese but path-like columns (`\spr\`, `\sound\`) produce mojibake** → you have a mixed file.
4. Verify by checking a known Chinese SPR filename (e.g. `劫富济贫`) against GBK decode of the raw bytes.

## Correct parse strategy: raw-byte split per column

**Do NOT decode the whole line as one encoding.** Instead:

```python
# 1. Read raw bytes
raw = open(path, 'rb').read()

# 2. Split into raw-byte columns (tab-delimited)
raw_cols = raw_line.split(b'\t')

# 3. Decode text columns as TCVN3 (or PcText.ReadLinesTcvn3 for the line)
text_cols = PcText.ReadLinesTcvn3(path)  # gives decoded string lines

# 4. Decode SPR/sound/icon columns from raw bytes as GBK
gbk = Encoding.GetEncoding("GB18030")  # via reflection for CodePagesEncodingProvider
spr_path = gbk.GetString(raw_cols[10])  # col index depends on file schema
```

### C# implementation pattern (PcThiefSkillParser example)

```csharp
RegisterCodePages();  // reflection-based CodePagesEncodingProvider registration
var gbk = Encoding.GetEncoding("GB18030");
var lines = PcText.ReadLinesTcvn3(path);       // text columns (Vietnamese)
var rawLines = File.ReadAllBytes(path);          // raw bytes for path columns

// Walk both in parallel — same row count, same tab structure
var rawCols = SplitRawLine(rawLines, ref rawOffset);  // byte[][] per row
var cols = line.Split('\t');                            // string[] per row

// Use string cols for text fields, raw cols for path fields
movie = DecodeGbkCol(rawCols, MovieCol, gbk) ?? PcItemCommon.Str(cols, MovieCol);
```

## Common mistakes

1. **Using `PcText.ReadLinesTcvn3` for the whole line then trying to reverse-map path columns.** The TCVN3→byte→GBK reverse mapping is ambiguous for many characters (multiple TCVN3 bytes map to the same Unicode codepoint). This produces *almost* correct but subtly wrong Chinese (e.g. `劫峄济贫` instead of `劫富济贫` — only 1 character off).

2. **Using `PcText.DecodeBest` (auto-detect) for mixed files.** `DecodeBest` scores the whole file and picks one winner. A mixed file scores high on both Vietnamese and Chinese, and the winner may be wrong for half the columns.

3. **Assuming all files in a directory share the same encoding.** The `PcSkill/` directory contains both fully-GBK files (`skills.txt`, `skills1.txt`) and mixed files (`thiefskill.txt`). Each file must be tested independently.

## RegisterCodePages helper (for Unity/IL2CPP)

`System.Text.Encoding.GetEncoding("GB18030")` requires the `System.Text.Encoding.CodePages` NuGet package. In Unity, register via reflection:

```csharp
private static void RegisterCodePages()
{
    try
    {
        var pt = Type.GetType("System.Text.CodePagesEncodingProvider, System.Text.Encoding.CodePages");
        var prov = pt?.GetProperty("Instance")?.GetValue(null, null) as EncodingProvider;
        if (prov != null) Encoding.RegisterProvider(prov);
    }
    catch { }
}
```

## Evidence

- `thiefskill.txt` row 1 raw hex at SPR column: `bd d9 b8 bb bc c3 c6 b6` → GBK = `劫富济贫` ✓
- Same bytes decoded as TCVN3: `劫峄济贫` ✗ (char `富` U+5BCC → `峄` U+5CC4, off by 1 byte in reverse mapping)
- Name column same row: `4b 69 d5 70 20 50 68 f3 20 54 d5 20 42 c7 6e` → TCVN3 = `Kiếp Phá Thiên Tôn` ✓

## Sino-Vietnamese (Hán Việt) Transliteration & Mojibake Resolution (Proved 2026-06-13)

When porting assets (such as items, equipment, and NPCs) from settings files that contain both TCVN3 and untranslated Chinese, use the following advanced transliteration and decoding techniques:

### 1. Chinese/Mojibake Detection Heuristics
To separate actual Vietnamese (TCVN3) from untranslated Chinese that was garbled into Mojibake:

- **For NPCs (`npcs.txt`)**: 
  Compare the client settings file (`Client 6.0/settings/npcs.txt`) and the purely Chinese unpack file (`dmjx06/settings/npcs.txt`) byte-for-byte.
  - If `raw_cl == raw_zh` (bytes are identical) and the string contains CJK characters (`\\u4e00 <= ch <= \\u9fff`) when decoded as GB18030, it is **untranslated Chinese** and needs Hán Việt translation.
  - Otherwise, it is TCVN3 Vietnamese.

- **For Equipment (`goldequip.txt`)**:
  Check the TCVN3-decoded string for specific Mojibake signatures. If any signature is found, decode the raw bytes as GB18030 and translate:
  ```python
  def is_mojibake_tcvn3(text: str) -> bool:
      mojibake_signatures = [
          "ẵð", "ệự", "ềứ", "Íư", "Äắ", "ẵả", "ẵô", "ấữ", "ểà", "ệú", 
          "ẫò", "ẻã", "ưề", "ềở", "ẵủ", "ẽủ", "ẽọ", "ìể", "èỡ", "ìụ",
          "ºẻ", "ẩậ", "ẻề", "ảậ", "Äắ", "ợÊ", "ầồ", "Áô", "àÀ", "ấ³", 
          "ổẩ", "ằã", "ũỵ", "ãò", "âò", "àủ", "éô", "ạỳ", "á±", "ạư",
          "±ứ", "ầạ", "àỉ", "Äá", "ãũ", "ệơ", "ãồ", "ºÅ", "ãð", "ẽủ"
      ]
      return any(sig in text for sig in mojibake_signatures)
  ```

### 2. Transliteration pipeline (Hán Việt Map + Custom Word Map)
We maintain a canonical character mapping at `/var/www/vltktool/hanviet_dict.json` containing Hán Việt transliterations for all 1,000+ CJK characters found in JX1 settings. 

The correct translation pipeline is:
1. **Apply custom word-level mappings** first for gaming-specific terms (e.g. `金箱子` -> `Rương Vàng`, `事件总管` -> `Tổng Quản Sự Kiện`, `门派祈福香炉` -> `Lư Hương Cầu Phúc Môn Phái`).
2. **Translate remaining CJK characters** character-by-character using `/var/www/vltktool/hanviet_dict.json`.
3. **Normalize spacing and capitalization** so it formats as standard Vietnamese proper nouns.

#### Python Implementation Example:
```python
import json
import re

with open("/var/www/vltktool/hanviet_dict.json", "r", encoding="utf-8") as f:
    hanviet_dict = json.load(f)

word_map = {
    "金箱子": "Rương Vàng",
    "银箱子": "Rương Bạc",
    "铜箱子": "Rương Đồng",
    "木箱子": "Rương Gỗ",
    "事件总管": "Tổng Quản Sự Kiện",
    "门派祈福香炉": "Lư Hương Cầu Phúc Môn Phái",
    "宋金运粮车": "Xe Vận Lương Tống Kim",
    "运粮车": "Xe Vận Lương",
    "运粮士兵": "Binh Sĩ Vận Lương",
    "金军": "Quân Kim",
    "宋军": "Quân Tống",
    "大将军": "Đại Tướng Quân",
    "武林盟主": "Võ Lâm Minh Chủ",
    "服务器": "Máy Chủ",
    "白虎": "Bạch Hổ",
    "青龙": "Thanh Long",
    "玄武": "Huyền Vũ",
    "朱雀": "Chu Tước",
}

def translate_cjk_to_vietnamese(text: str) -> str:
    # 1. Word-level mappings
    for cn_w, vi_w in word_map.items():
        text = text.replace(cn_w, " " + vi_w + " ")
        
    # 2. Character-by-character Hán Việt translation
    parts = []
    for ch in text:
        if "\\u4e00" <= ch <= "\\u9fff":
            trans = hanviet_dict.get(ch, "")
            if trans:
                parts.append(" " + trans.capitalize() + " ")
            else:
                parts.append(ch)
        else:
            parts.append(ch)
            
    # 3. Clean spaces
    joined = "".join(parts)
    return re.sub(r'\\s+', ' ', joined).strip()
```
Using this pipeline, names like `b'\\\\xbd\\\\xf0\\\\xcf\\\\xe4\\\\xd7\\\\xd301'` decode correctly as `金箱子01` and translate naturally to `Rương Vàng 01` instead of garbled Mojibake `ẵðẽọìể01`.

