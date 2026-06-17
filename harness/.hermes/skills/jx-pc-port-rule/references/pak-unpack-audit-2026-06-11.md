# pak_unpacked Audit / Repair Notes — 2026-06-11

> **HISTORICAL SNAPSHOT — superseded.** This file documents an intermediate state where
> `dmjx01.pak` was still partial/segfaulting. A later re-unpack (manifest `updated_at`
> 2026-06-11 23:25) finished the repair: the live `_unpack_summary.json` now reports
> **46 paks, 403560/403560 exported, 0 failed, 0 partial, `dmjx01.pak` ok 1621/1621**
> (verified: 1621 files on disk). Treat the live manifest as truth; keep this note only for
> the methodology and the format-gotcha history below. Do NOT cite the "dmjx01 partial /
> segfault 1621" numbers as current.

Use this when deciding whether to delete/rebuild `/var/www/jx-source/pak_unpacked` or repair it incrementally.

## Verdict

Do **not** delete the whole `pak_unpacked` tree by default. Prefer targeted repair.

A full re-unpack run completed after `unpak_tool.py` learned to treat method `0x11000000` as raw SPR data. The tree is now broadly usable as canonical PC source, with one known focused gap: `dmjx01.pak`.

## Observed full re-unpack output

Command driver used `/tmp/full_reunpack_v3.py`, calling `/var/www/vltktool/unpak_tool.py -f -o /var/www/jx-source/pak_unpacked` with scoped scan dirs from Client/Server settings and script folders.

Final log highlights:

```text
Total time: 2257s
PAKs: 45 OK, 1 partial
Entries: 401939/403560 exported, 0 failed
Failure rate: 0.000%
_unpacked_summary.json updated 2026-06-11 21:44:16 +0700
```

Actual disk counts after the run:

```text
files=302941..302943   # count may differ by summary/log files
spr=74660
size=12G
```

The apparent mismatch between `401939` exported and ~`302k` files on disk is expected because several source PAK paths share the same output stem and overwrite/merge into one directory.

Duplicate output stems found:

```text
maps       x3  (client + two identical server copies)
update3    x2  (two identical server copies)
update_map x2  (two identical server copies)
shenxingfu.lua x2  (not real PACK magic)
```

Server duplicate hashes confirmed identical for the duplicated server pairs (`maps.pak`, `update3.pak`, `update_map.pak`, `shenxingfu.lua.pak`).

## Method counts across 46 paths

```text
0x00000000: 1506
0x01000000: 149697
0x10000000: 5
0x11000000: 352
0x20000000: 252000
```

`0x11000000` is no longer a full-run blocker: save bytes as-is. Those 352 entries are raw SPR files.

## Remaining targeted gap: dmjx01.pak

`dmjx01.pak` still segfaulted in the full subprocess run and was recorded as partial:

```text
summary_nonok: dmjx01.pak total_entries=1621 exported=0 segfaulted=1621
```

But the output directory already contains recovered files from prior/partial attempts:

```text
pak_unpacked/dmjx01 files=837
pak_unpacked/dmjx01 spr=817
unknown=408
```

So the correct operational stance is: `dmjx01.pak` is partially recovered and needs targeted repair for roughly 784 missing entries, not a full tree rebuild.

## Recommended repair workflow

1. Keep the current `pak_unpacked` tree.
2. Audit `dmjx01.pak` index entry-by-entry.
3. Compare each UID to existing `/pak_unpacked/dmjx01/**` output to identify missing entries.
4. Extract missing entries in process isolation or with stronger per-entry guards so libucl segfaults do not kill the whole repair.
5. For method `0x11000000`, copy raw bytes directly.
6. For entries whose decompressed data starts with `SPR\0`, validate with existing SPR parser/preview tooling before assigning purpose.
7. Update `_unpack_summary.json` or create a small audit report that distinguishes:
   - source PAK paths processed,
   - unique output stems/files on disk,
   - duplicate server PAKs,
   - true missing entries.

## Pitfalls

- Do not use total exported rows as final file count; duplicate stems merge output directories.
- Do not classify `shenxingfu.lua.pak` as a real PAK; it has bad PACK magic despite `.pak` suffix.
- Do not re-run broad scans or delete the 12G tree unless the user explicitly asks; full rebuild is slow and mostly redundant.
- Do not conclude assets are missing solely because they are under `unknown/<uid>.*`; UID-only files are still valid PC source pending path resolution.
