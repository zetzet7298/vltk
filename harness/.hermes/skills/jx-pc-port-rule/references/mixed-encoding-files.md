# Mixed-Encoding Files in PC Source (TCVN3 + GBK)

## The problem

Several PC data files under `/var/www/vltksource_new/vl_update_27` are **partially Việt-hoá**: some columns contain Vietnamese text (TCVN3-encoded) while others retain original Chinese (GBK-encoded). A single tab-separated row can mix both encodings.

**Known mixed-encoding files:**
- `Client 6.0/settings/thiefskill.txt` — skill names are TCVN3 Vietnamese (e.g. "Kiếp Phá Thiên Tôn"), SPR/sound/icon paths are GBK Chinese (e.g. `劫富济贫1.spr`)

**Fully GBK files** (no Vietnamese, decode entirely as GBK):
- `skills.txt`, `skills1.txt`, `npcs.txt`, `missles1.txt`, most `Reference/PcNpc/*`, most `Reference/PcSkill/*`

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
