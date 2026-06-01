---
name: jx-map-port
description: >-
  Port any JX Online 1 / Võ Lâm Truyền Kỳ PC map (terrain, buildings, decor, NPCs)
  into the VLTK-mobile Unity client with 100% visual fidelity by extracting the real
  region geometry and SPR art straight from the game's maps.pak / spr.pak. Use this
  skill WHENEVER the user wants to port, render, extract, rebuild, or "make look like
  the original" ANY JX/VLTK map — e.g. "port map Tương Dương", "render 成都 in Unity",
  "lấy map X từ game gốc", "thêm map mới vào sandbox", "extract regions/sprites from
  maps.pak", or mentions a map name/mapId, a .wor file, Region_C.dat, 游戏资源 art, or
  the balang/巴陵县 workflow. Also trigger when terrain/buildings/trees are missing,
  showing as gray/procedural placeholders, scattered, or "không giống map gốc". Also
  trigger for rendering bugs: dark gaps through structures, grass on rooftops, duplicate
  player visuals, objects at wrong heights, or "phân thân"/"khe tối"/"thiếu mảnh".
  This skill encodes the cracked g_FileName2Id hash, the Z-projection formula, the
  spatial-tree sorting model, and the full extraction pipeline that took many sessions
  to discover — do not re-derive any of it, reuse this.
---

# JX Online 1 / VLTK Map Porting

Port a complete PC map (巴陵县/balang style) into the Unity mobile sandbox with the
real game art, by extracting region geometry and SPR sprites directly from the
client paks using the **correct** filename hash and the **correct** isometric projection.

## The things that make this work (read first)

### 1. Signed-byte path hash

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

### 2. The Z-projection formula (critical for tall structures)

The original engine's `KRepresentShell3::CoordinateTransform` converts scene coords to
screen coords with this exact formula (found at `Represent3/KRepresentShell3.cpp:2157`):

```cpp
void CoordinateTransform(int& nX, int& nY, int nZ)
{
    nX = nX - m_nLeft;
    nY = nY / 2 - m_nTop - ((nZ * 887) >> 10);
}
```

In Unity (no viewport offset): `screenY = sceneY/2 - sceneZ*(887/1024)`

The `sceneZ * 0.866` term is **not optional**. Built-in objects (houses, trees, gates)
have Z values of 0–630. Ignoring Z causes:
- Gate crossbeams (Z=441-628) to render 380-544 pixels too low → dark gaps through structures
- Tall buildings to appear squashed/misaligned with their bases
- Multi-piece structures (like the 牌坊/paifang gate) to be completely broken

The C# implementation in `MapRenderer.cs`:
```csharp
private const float ZScreenScale = 887f / 1024f; // ≈0.866
float screenY = obj.imgY1 * 0.5f - obj.imgZ1 * ZScreenScale;
```

### 3. The sorting model (critical for correct layering)

The original engine uses a **spatial binary tree** (`KIpoTree` / `KIpotBranch`) to order
object rendering. Objects are inserted into the tree by their position relative to split
lines, then the tree is traversed in-order (left=UP/far, then root, then right=DOWN/near)
to produce correct back-to-front draw order. This is NOT simple Y-sorting.

For the Unity port, the sorting system uses three mechanisms working together:

**A. Layer separation by sortingOrder:**
- Ground tiles: `sortingOrder = -1000` (always beneath everything)
- Cover objects (grass, roads): `sortingOrder = 0` (flat ground decals)
- Builtin objects (houses, trees, gates): `sortingOrder = 1000 + fileIndex` (monotonically
  increasing counter preserving the original engine's spatial-tree draw order)
- Player: `sortingOrder = 5000` (always above map art)

**B. File-order counter for builtins (NOT Y-sort):**
Within each region, builtin objects are stored in the same order as the KIpoTree's
in-order traversal. A global counter (`_builtinSortCounter`) assigns each builtin a
unique `sortingOrder` (1000, 1001, 1002, …). Regions are iterated col-by-row (back-to-front
in the isometric view), so the counter naturally produces correct inter-region ordering.
This preserves the authored draw order for complex multi-piece structures like the
牌坊 gate where a crossbeam must draw behind the near pillar but in front of the far pillar
— something pure Y-sorting cannot achieve.

**C. CustomAxis world-Y sort as tiebreaker:**
The camera uses `transparencySortMode = CustomAxis` with `transparencySortAxis = (0,1,0)`.
This sorts sprites at the same `sortingOrder` by their world Y position (higher Y = drawn
later = in front). This handles cover-vs-cover and provides a safety net, but the primary
ordering is the file-order counter.

### 4. Sprite pivots differ between object types

- **Ground tiles**: pivot `(0, 1)` = top-left. Placed at world `(screenX, -screenY)`.
  The sprite extends right and downward from the placement point.
- **Cover objects** (KSPRCoverGroundObj): pivot `(0.5, 0)` = bottom-center. The `posX/posY`
  is the base/foot position. Cover objects have NO Z coordinate (they are flat ground decals).
- **Builtin objects** (KBuildinObj): pivot `(0, 1)` = top-left. `ImgPos1` is the quad's
  top-left corner in scene space. The sprite extends right and downward to approximately
  `ImgPos3` (bottom-right). Using bottom-center pivot for builtins causes them to shift
  by half their width and their full height, leaving gaps.

## When to use which reference

- **Porting a new map, or fixing a broken/incomplete one** → run the pipeline below.
- **Projection math / coordinate systems** → `references/projection.md` (full details).
- **Sorting / rendering bugs** → `references/sorting.md` (the complete sorting model).
- **Hitting a wall** (0 matches, gray tiles, corrupt sprites, wrong positions) →
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

### 4. Visual verification checklist

After rendering, verify these specific things (each corresponds to a bug that took
significant effort to diagnose and fix):

- **No dark gaps through structures**: Gate crossbeams should connect to pillars, house
  roofs should sit flush on walls. Dark gaps mean Z-projection is broken.
- **No grass/road decals on top of buildings**: Cover objects (sortingOrder=0) must draw
  below all builtin objects (sortingOrder≥1000). If you see grass on rooftops, the cover/
  builtin layer separation is broken.
- **Player renders as a single clean sprite**: If you see a "ghost" or duplicate character,
  the `MalePlayerVisual.RefreshActionParts` orphan cleanup is broken — orphan GameObjects
  from prior actions are still enabled.
- **Multi-piece structures render correctly**: The 牌坊 gate (12 pieces from b013_v2_*.spr)
  should show pillars behind crossbeams. If pieces overlap wrong, the file-order sorting
  counter or builtin pivot is broken.

### 5. Keep the tests green

`unityMCP run_tests (mode=EditMode)` — the EditMode tests must all pass (410 as of the
balang port). The extractor only writes data files, so they should be unaffected, but
verify after any renderer changes.

## Output contract

Per ported map, under `Assets/StreamingAssets/`:
- `TestData/Regions/Map_{id}_C/{col}_{row}_Region_C.dat` — one per occupied cell
- `TestData/Regions/Map_{id}_C/manifest.json` — bounds, cells, hasGround/hasBuiltin
- `TestData/Regions/Map_{id}_C/image_names.json` — every art path referenced
- `Sprites/{ComputePathUid}.spr` — every resolved art asset (shared across maps)

The renderer, projection math, and SPR decoder are already in the project and map-agnostic;
porting a new map is purely a data-extraction step once this skill's hash is used.

## Key files in the project

| File | Purpose |
|------|---------|
| `Assets/Scripts/Sandbox/MapRenderer.cs` | Rendering pipeline: ground, cover, builtin layers, Z-projection, sorting |
| `Assets/Scripts/Sandbox/SandboxManager.cs` | Scene setup, camera CustomAxis sort config |
| `Assets/Scripts/Sandbox/SandboxPlayerController.cs` | Player movement, camera follow, zoom level |
| `Assets/Scripts/Sandbox/MalePlayerVisual.cs` | 8-direction SPR layered character rendering |
| `Assets/Scripts/Sandbox/BuildinObjParser.cs` | Parses KBuildinObj with ImgPos1-4 (x,y,z quads) |
| `Assets/Scripts/Sandbox/GroundLayerParser.cs` | Parses ground tiles + KSPRCoverGroundObj |
| `Assets/Scripts/Sandbox/RegionParser.cs` | Region_C.dat section table dispatch |
| `Assets/Scripts/Sandbox/CameraRigService.cs` | Pure C# camera rig (no MonoBehaviour) |
