# JX1 PAK Format Internals

Reverse-engineered from `engine.dll` (PE i386, MSVC 8, ImageBase `0x10000000`) in
`/var/www/vltksource_new/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/engine.dll`.

## Verified symbol RVAs (radare2 on the real engine.dll, 2026-06-12)

`engine.dll` ships with full MSVC-mangled export symbols (1362 exports), so these are exact,
not guesses. Confirmed with `rabin2 -E` + `r2 -c "s <rva>; af; pdf"`:

| Symbol | RVA (file) | VA (baddr 0x10000000) | What it proves |
|---|---|---|---|
| `?g_FileName2Id@@YAKPBD@Z` | `0x25c60` | `0x10025c60` | the PAK path-hash; `movsx edx,dl` = **signed byte** |
| `?g_InitCodec@@YAXPAPAVKCodec@@H@Z` | `0x5b40` | `0x10005b40` | only ever instantiates `KCodecLzo` (NRV2B) |
| `?Decode@KCodec@@UAEHPAUTCodeInfo@@@Z` | `0x5b10` | `0x10005b10` | base codec virtual Decode |
| `?Decode@KCodecLzo@@UAEHPAUTCodeInfo@@@Z` | `0x60f0` | `0x100060f0` | NRV2B decode; `cmp cl,0x11` is the LZO literal-run marker on the **data stream**, NOT a PAK flag |
| `?Open@KPakData@@QAEHPAD@Z` | `0x279d0` | `0x100279d0` | PAK open |
| `?Search@KPakData@@QAEHPADPAK1@Z` | `0x27870` | `0x10027870` | UID lookup in index |
| `?Decode@KPakData@@QAEHPAUTCodeInfo@@@Z` | `0x27950` | `0x10027950` | thunk → KCodec vtable slot 2 |
| `?Pack@KPakTool@@QAEHPAD0H@Z` | `0x28250` | `0x10028250` | single-file pack |
| `?UnPack@KPakTool@@QAEHPAD0@Z` | `0x284a0` | `0x100284a0` | single-file unpack |

**`g_FileName2Id` disassembly (the signed-byte proof).** The exact instruction stream at
`0x10025c60`: lowercase `A-Z` (`sub al,0x41; cmp al,0x19; ja; add dl,0x20`), `/`→`\`
(`cmp dl,0x2f; mov dl,0x5c`), then **`movsx edx, dl`** (sign-extend → bytes ≥0x80 become
negative, i.e. `b-256`), `imul` by 1-based index, `add` accumulator, `div 0x8000000B` (mod),
`neg/shl 4/sub` (= `rem * 0xFFFFFFEF`), final `xor 0x12345678`. This is byte-for-byte the
algorithm in `jx_map_port.py` / `uid.py`. Cross-checked: a Python reimpl of this exact asm
and `uid.py` both yield `c4454165` for `\spr\Ui\技能图标\icon_sk_ty_at.spr` and `45488ea8`
for `\spr\npcres\man\MA_BD_019_ST01.spr` (after both normalize a leading `\`).

**`g_InitCodec`** at `0x10005b40` only `new`s a `KCodecLzo` (12-byte object, vtable
`0x1003f510`) when its selector is set; there is no second codec class. So every *compressed*
PAK entry is decompressed by NRV2B regardless of the method label — which is why the
"BZIP2/FRAGMENT" rows below are noted as misnamed.

## PAK File Layout

```
Offset  Size  Description
0x00    4     Magic: "PACK"
0x04    4     Entry count (uint32 LE)
0x08    4     Index table offset (uint32 LE)
0x0C    4     Header size (always 0x20 = 32)
0x10    16    Padding (zeros)
--- header ends at 0x20 ---
...     ...   Data blobs (offsets from index table point here)
--- index table starts at specified offset ---
0x00+N  16×N  Index entries (see below)
```

### Index Entry (16 bytes each)

```
Offset  Size  Type   Description
0x00    4     uint32  UID (FileName2Id hash of original path)
0x04    4     uint32  Offset (absolute byte offset in PAK file)
0x08    4     int32   Decompressed size
0x0C    4     int32   Flags (compression method + compressed size)
```

### Flag Field Decomposition

```
method        = flags & 0xFF000000
compressed_sz = flags & 0x00FFFFFF
```

## Compression Methods

`method = flags & 0xFF000000`. Table below; the **Count** column is the measured distribution
across all 46 source paks (403,560 entries total, verified 2026-06-12 — matches the manifest).

| Method         | Value       | Decompressor            | Count   | Notes                         |
|----------------|-------------|-------------------------|---------|-------------------------------|
| TYPE_BZIP2     | `0x20000000`| `ucl_nrv2b_decompress_8`| 252,000 | Misnamed; still NRV2B. Most common |
| TYPE_UCL_OLD   | `0x01000000`| `ucl_nrv2b_decompress_8`| 149,697 | NRV2B                         |
| TYPE_NONE      | `0x00000000`| None (stored as-is)     | 1,506   | Stored                        |
| **0x11_RAW**   | `0x11000000`| **None — save as-is**   | 352     | Raw SPR stored uncompressed   |
| **0x10_FRAG**  | `0x10000000`| **fragment table** (see below) | 5 | dmjx01.pak only; NOT a plain NRV2B stream |
| TYPE_UCL       | `0x10000000`| `ucl_nrv2b_decompress_8`| (0 plain)| Label collides with 0x10_FRAG; in this dataset every 0x10 entry is a fragment table, so try the fragment parse first, then fall back to NRV2B |
| TYPE_BZIP2_OLD | `0x02000000`| `ucl_nrv2b_decompress_8`| 0       | Not present in this dataset   |
| TYPE_FRAGMENT_OLD | `0x03000000`| `ucl_nrv2d_decompress_8`| 0    | Not present; would be NRV2D if it appeared |
| TYPE_FRAGMENT  | `0x30000000`| `ucl_nrv2d_decompress_8`| 0       | Not present                   |
| TYPE_FRAGMENTA_OLD | `0x04000000`| `ucl_nrv2e_decompress_8`| 0   | Not present; NRV2E            |
| TYPE_FRAGMENTA | `0x40000000`| `ucl_nrv2e_decompress_8`| 0       | Not present                   |

Measured reality: only **5 distinct methods actually occur** (`0x20`, `0x01`, `0x00`, `0x11`,
`0x10`). The NRV2D/NRV2E rows are kept for completeness but are dead in this client — and note
`g_InitCodec` only builds `KCodecLzo` (NRV2B), so if a `0x30/0x40` entry ever turned up, the
NRV2D/NRV2E mapping would need re-confirming against the binary rather than trusted blindly.

### Method 0x11000000 — Raw SPR (critical discovery)

352 entries across 11 valid PACK archives use method `0x11000000`. These are **raw SPR byte
streams stored directly without UCL compression**. The `compressed_sz` bytes at the entry
offset form a self-contained SPR:

- Starts with `SPR\x00` magic
- Standard SPR0 header (32 bytes): magic(4) + width(2) + height(2) + ... + frame_count(2@0x0C) + color_count(2@0x0E)
- Palette: `color_count × 3` bytes after header
- Frame table: `frame_count × 8` bytes (rel_offset + size per frame)
- Frame data: RLE-encoded sprite pixels

**Do NOT call libucl on these entries — it will segfault.** Just save `compressed_sz` bytes
directly. The `decompressed_size` field is the in-memory size when decoded to RGBA, not the
actual data size.

Extraction verified by byte-preserving raw copy: 352/352 start with `SPR\x00`. The live manifest
reports all of them exported. A *simple* `parse_frames()` validator may not fully parse every
large/edge SPR, but the raw byte-copy is correct regardless — never run libucl on them.

### Method 0x10000000 — KCodec fragment table (dmjx01.pak edge case)

5 entries in `Client 6.0/data/dmjx01.pak` advertise top-level method `0x10000000`, but the
payload is not one NRV2B stream. It is a fragment container:

```
0x00  u32 fragment_count
0x04  u32 table_offset
0x08  payload chunks ...
table_offset:
      fragment_count × (u32 offset, u32 decompressed_size, u32 flag)
```

Each chunk must be decompressed using its own `flag` method/size and then concatenated. Calling
libucl/NRV2B on the whole top-level payload segfaults. Verified fixed UIDs:
`8ced40ec`, `9514cffa`, `a4728732`, `c99c13bd`, `e53792c4`.

**Binary-verified (2026-06-12)** by parsing `dmjx01.pak` directly: all 5 UIDs are present in the
index with method `0x10000000`. Cracking `8ced40ec` (csize 92097, dsize 132112): header reads
`fragment_count=17`, `table_offset=0x166f5`; the 17 tail records sum `out_size` to exactly
`132112 == dsize`, and the first record's `flag` carries its own per-chunk method (`0x0` stored,
`0x20000000` NRV2B, …). This confirms the container layout and the "decompress-per-chunk-then-
concat" rule, not a single NRV2B stream. `e53792c4` parses identically (same 17-record shape).

## Engine Key Functions (radare2 symbols)

| RVA      | Symbol                                  | Purpose                                   |
|----------|-----------------------------------------|-------------------------------------------|
| `0x05B40`| `g_InitCodec(KCodec**, int)`            | Creates codec instance (arg=2 → KCodecLzo)|
| `0x060F0`| `KCodecLzo::Decode(TCodeInfo*)`         | LZO1X decompressor (checks first byte)    |
| `0x05B10`| `KCodec::Decode(TCodeInfo*)`            | Base codec (no-op fallback)               |
| `0x27950`| `KPakData::Decode(TCodeInfo*)`          | Virtual dispatch to codec                 |
| `0x279D0`| `KPakData::Open(char*)`                 | Opens PAK, reads header+index             |
| `0x27870`| `KPakData::Search(char*, DWORD*, DWORD*)`| Binary search by UID in index            |
| `0x284A0`| `KPakTool::UnPack(char*, char*)`        | Full unpack tool                          |
| `0x28250`| `KPakTool::Pack(char*, char*, int)`     | Pack tool                                 |

### Engine Architecture Notes

- `KPakData::Open` allocates `count × 12` bytes for index, reads same from file.
  But actual entries are 16 bytes each. The engine only reads 3 of 4 fields per entry
  (uid, offset, size), not the flag field. Compression is handled at the data level,
  not the index level.
- `g_InitCodec` is called with `header[12:16]` value (always 0x20 = 32 for these PAKs).
  With arg ≠ 0 and ≠ 2, it falls through without creating a codec. This suggests the
  engine uses a different code path for runtime sprite loading than `KPakTool::UnPack`.
- `KCodecLzo::Decode` at `0x060FA` checks `cmp cl, 0x11` — this is the LZO literal marker,
  **not** the PAK compression method 0x11000000. They are coincidentally the same byte value
  but exist at different levels (data stream vs. PAK entry flags).

## UID Hash Algorithm (from unpak_tool.py + engine.dll `g_FileName2Id`)

```python
def file_id_from_bytes(path_bytes: bytes) -> int:
    value = 0
    for index, byte in enumerate(path_bytes, 1):
        if 65 <= byte <= 90:  # A-Z → a-z
            byte += 32
        c = byte - 256 if byte >= 128 else byte  # signed-byte for GBK paths
        value = ((value + index * c) % 0x8000000B) * 0xFFFFFFEF
        value &= 0xFFFFFFFF
    return value ^ 0x12345678
```

Evidence check: `\\spr\\Ui\\技能图标\\icon_sk_ty_at.spr` encoded as GBK hashes to `c4454165`.

## pak_unpacked Status (as of 2026-06-11)

| Metric | Value |
|--------|-------|
| Total `.pak` extension files | 46 |
| Valid `PACK` archives | 44 (2 `shenxingfu.lua.pak` files are Lua text, 0 PACK entries) |
| Total index entries | 403,560 |
| Successfully extracted | 403,560 (100%) |
| Failed | 0 |
| Method `0x11000000` raw SPR | 352 entries — raw byte-copy, save as-is |
| Method `0x10000000` fragmented wrapper | 5 `dmjx01.pak` entries — decompress sub-chunks then concatenate |
| Manifest | `_unpack_summary.json` |

### Current action decision

Do **not** delete the whole `pak_unpacked` tree. The correct fix was targeted update: patch
`unpak_tool.py` for `0x11000000` raw SPR plus `dmjx01.pak` KCodec fragment-table entries, then
repair only affected entries. Full delete + re-unpack is unnecessary unless source PAK files change
or the desired layout changes to a per-origin mirrored forensic tree.

## RE Methodology Used

1. `strings engine.dll | grep -i "KPakData\|KCodec\|Decode"` → found class names
2. `r2 -c "is~Decode"` → list all Decode symbols with RVAs
3. `r2 -c "aaa; s <rva>; pdf"` → decompile each function
4. Traced `g_InitCodec` to understand codec dispatch (only KCodecLzo exists)
5. Compared OK entries (compressed, starts with UCL data) vs FAIL entries (starts with SPR\x00)
6. Verified FAIL entries are complete SPRs using `parse_frames()` validation
7. Confirmed all 352 entries across 12 PAKs are 100% valid SPR files
