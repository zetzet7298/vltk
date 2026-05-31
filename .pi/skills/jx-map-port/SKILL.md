---
name: jx-map-port
description: >-
  Port any JX Online 1 / Võ Lâm Truyền Kỳ PC map (terrain, buildings, decor, NPCs)
  into the VLTK-mobile Unity client with 99% visual fidelity by extracting the real
  region geometry and SPR art straight from the game's maps.pak / spr.pak. Use this
  skill WHENEVER the user wants to port, render, extract, rebuild, or "make look like
  the original" ANY JX/VLTK map — e.g. "port map Tương Dương", "render 成都 in Unity",
  "lấy map X từ game gốc", "thêm map mới vào sandbox", "extract regions/sprites from
  maps.pak", or mentions a map name/mapId, a .wor file, Region_C.dat, 游戏资源 art, or
  the balang/巴陵县 workflow. Also trigger when terrain/buildings/trees are missing,
  showing as gray/procedural placeholders, scattered, or "không giống map gốc".
  This skill encodes the cracked g_FileName2Id hash and the full extraction pipeline
  that took many sessions to discover — do not re-derive it, reuse this.
---

# JX Online 1 / VLTK Map Porting

Port a complete PC map (巴陵县/balang style) into the Unity mobile sandbox with the
real game art, by extracting region geometry and SPR sprites directly from the
client paks using the **correct** filename hash.

## The one thing that makes this work (read first)

Every JX pak (`maps.pak`, `spr.pak`, `update*.pak`, …) keys its entries by a hash of
the resource path, `g_FileName2Id` (exported from `engine.dll`). The hash treats each
path byte as a **signed char** (`movsx`) after lowercasing ASCII `A–Z`. Earlier
attempts used *unsigned* bytes, so any path containing Chinese (GBK high bytes, ≥0x80)
hashed wrong and resolved **nothing** — which is why people wrongly conclude "the data
isn't in the pak" or "the UID is unrecoverable". It IS recoverable. The signed hash:

```
value = 0; index = 0
for each byte b of the GBK path:
    if 'A' <= b <= 'Z': b += 0x20          # lowercase ASCII only
    c = b - 256 if b >= 128 else b          # SIGNED char — THIS is the fix
    index += 1
    value = ((value + index*c) % 0x8000000B) * 0xFFFFFFEF   # keep 32-bit
value ^= 0x12345678
```

With this hash, **every** region path and **every** referenced art name resolves.
`scripts/jx_map_port.py` already implements it — you almost never need to touch it.

> Note: the C# `SprRuntimeService.ComputePathUid` in the project uses the *unsigned*
> variant. That is fine and intentional — it's only used as a private naming scheme
> between the extractor and the runtime (extractor writes `{ComputePathUid}.spr`, C#
> re-derives the same name from the imageName). It must NOT be used to look inside paks.

## When to use which path

- **Porting a new map, or fixing a broken/incomplete one** → run the pipeline below.
- **Just understanding the render math / projection** → `references/projection.md`.
- **Hitting a wall (0 matches, gray tiles, corrupt sprites, wrong positions)** →
  `references/pitfalls.md` lists every dead-end already explored so you don't repeat them.

## Prerequisites

The original game art lives on a VMDK that must be mounted read-only. Check first:

```bash
ls /mnt/jxwin/SourceNew/swrod3/bin/Client/data/maps.pak   # mounted?
```

If not mounted, see `references/environment.md` (qemu-nbd mount commands) and confirm
with the user before mounting — it touches `/dev` and `sudo`.

Also needs `libucl.so` (UCL decompression) and Python 3 with `pefile`/`capstone`
(only for re-deriving the hash from a different engine.dll; not needed normally).

## Pipeline

### 1. Identify the map

Use the helper to find the map name and bounds in one shot:

```bash
python3 scripts/list_maps.py 巴陵        # search by substring
python3 scripts/list_maps.py --id 53     # show one entry
```

It prints `id=<MapList index>  <region\name>  rect=minX,minY,maxX,maxY`. The value
(`region\name`) is what you pass to `--map-name`; the `rect` is the grid bounds
(read from the map's `.wor`). If a `.wor` is missing it says so — pass `--bounds`
explicitly (or `--scan-pad` to widen and keep only cells that resolve).

The project's own `mapId` (used for the StreamingAssets folder `Map_{id}_C`) may differ
from the MapList index — check `Assets/StreamingAssets/MapCatalog.json`. For balang the
MapList index is 53 but the project mapId is 79; the script takes both as arguments.

### 2. Run the extractor

```bash
python3 scripts/jx_map_port.py \
  --map-name '两湖区\巴陵县' \
  --project-map-id 79 \
  --unity-root /var/www/vltk-mobile
```

It will, in one pass:
- index every pak under `bin/Client/data`,
- for each grid cell hash `\maps\{map-name}\v_{Y:03d}\{X:03d}_Region_C.dat`, decompress
  the entry (UCL), and write `Assets/StreamingAssets/TestData/Regions/Map_{id}_C/{X}_{Y}_Region_C.dat`
  plus `manifest.json` + `image_names.json`,
- collect every ground / cover / builtin `imageName`, resolve each with the same hash,
  extract + rebuild the SPR (handling per-frame-compressed sprites), and stage it under
  `Assets/StreamingAssets/Sprites/{ComputePathUid}.spr` so the runtime finds it by name.

Expected healthy output: `extracted regions: N`, `staged art: M/M failed=0`.
If `failed>0`, read `references/pitfalls.md` — usually a loose-art fallback or a new
per-frame SPR shape.

### 3. Render & verify in Unity

The runtime (`MapRenderer.cs`) already loads `Map_{id}_C` and projects regions with the
JX screen-space formula. To see it:

```
unityMCP refresh_unity (scope=assets)        # import the new .spr/.dat
unityMCP manage_editor play
unityMCP read_console  -> expect "Rendered N regions; SPR stats: 0 missing"
unityMCP manage_camera screenshot (capture_source=game_view, include_image)
```

The renderer auto-frames the densest building cluster (the town core). Confirm against
the original: stone plaza (青砖), Jiangnan houses, trees, water edges should all appear
with real art. `manage_camera` screenshots only work **while in play mode** — a shot
taken after stop shows only the skybox.

### 4. Keep the tests green

`unityMCP run_tests (mode=EditMode)` — the 406 EditMode tests must stay at 406/406.
The extractor only writes data files, so they should be unaffected, but verify.

## Output contract

Per ported map, under `Assets/StreamingAssets/`:
- `TestData/Regions/Map_{id}_C/{col}_{row}_Region_C.dat` — one per occupied cell
- `TestData/Regions/Map_{id}_C/manifest.json` — bounds, cells, hasGround/hasBuiltin
- `TestData/Regions/Map_{id}_C/image_names.json` — every art path referenced
- `Sprites/{ComputePathUid}.spr` — every resolved art asset (shared across maps)

The renderer, projection math, and SPR decoder are already in the project and map-agnostic;
porting a new map is purely a data-extraction step once this skill's hash is used.
