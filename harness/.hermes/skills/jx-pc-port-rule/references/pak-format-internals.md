# JX1 PAK Format Internals

Reverse-engineered from `engine.dll` (PE i386, MSVC 8, ImageBase `0x10000000`) in
`/var/www/vltksource_new/vl_update_27/Client 6.0/engine.dll`.

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

| Method         | Value       | Decompressor            | Notes                         |
|----------------|-------------|-------------------------|-------------------------------|
| TYPE_NONE      | `0x00000000`| None (stored as-is)     |                               |
| TYPE_UCL_OLD   | `0x01000000`| `ucl_nrv2b_decompress_8`| Most common (~90% of entries) |
| TYPE_UCL       | `0x10000000`| `ucl_nrv2b_decompress_8`| New variant                   |
| TYPE_BZIP2_OLD | `0x02000000`| `ucl_nrv2b_decompress_8`| Misnamed; still NRV2B         |
| TYPE_BZIP2     | `0x20000000`| `ucl_nrv2b_decompress_8`| Misnamed; still NRV2B         |
| TYPE_FRAGMENT_OLD | `0x03000000`| `ucl_nrv2d_decompress_8`| NRV2D variant              |
| TYPE_FRAGMENT  | `0x30000000`| `ucl_nrv2d_decompress_8`| NRV2D variant                 |
| TYPE_FRAGMENTA_OLD | `0x04000000`| `ucl_nrv2e_decompress_8`| NRV2E variant             |
| TYPE_FRAGMENTA | `0x40000000`| `ucl_nrv2e_decompress_8`| NRV2E variant                 |
| **0x11_RAW**   | `0x11000000`| **None — save as-is**   | SPR files stored uncompressed |

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
