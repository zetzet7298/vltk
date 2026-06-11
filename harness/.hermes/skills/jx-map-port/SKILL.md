---
name: jx-map-port
description: >-
  Port any JX Online 1 / Võ Lâm Truyền Kỳ PC map into the VLTK-mobile Unity client
  with real PC region geometry and SPR art from maps.pak/spr.pak. Use whenever the
  user wants to port/render/extract/rebuild a JX/VLTK map, mentions mapId/.wor/
  Region_C.dat/巴陵县, or reports map fidelity bugs (gray placeholders, missing terrain,
  dark gaps, grass on rooftops, wrong heights, duplicate/hidden player). Also use for
  minimap parity bugs: gray/not zoomed minimap, preview missing, wrong coordinates,
  player dot not following movement, minimap/preview click-to-move broken, player/camera
  blocked before minimap edge, Chinese map names, preview not closing outside, or default
  boot-map changes. Covers mission maps with Lua NewWorld entry scripts, map-specific NPC
  boss SPR staging, signed-byte hash, Z-projection, spatial-tree sorting, active-map bounds,
  PC coordinate labels, click-to-move, Unity serialized-field gotchas.
---

# JX Online 1 / VLTK Map Porting

Port a complete PC map (巴陵县/balang style) into the Unity mobile sandbox with the
real game art, by extracting region geometry and SPR sprites directly from the
client paks using the **correct** filename hash and the **correct** isometric projection.

## The things that make this work (read first)


## Resource/hash guard learned from combat visual port

Before concluding that any PC SPR/icon/effect/NPC/HUD asset is missing, apply `jx-pc-port-rule` → **PC resource resolution doctrine**:

- Read PC TXT/INI tables with the correct encoding. Paths with Chinese resource folders are usually GB2312/GBK; mojibake paths hash to fake UIDs.
- PAK entries named `unknown/<uid>.spr` are valid extracted PC assets, not garbage.
- For PAK lookup use PC signed-byte FileNameHash, not an unsigned-byte/private runtime hash.
- Copy exact PC assets into `Assets/StreamingAssets/...`; never load directly from `/var/www/vltksource_new` at runtime.
- Verify with real file existence/decode/render evidence before claiming parity or missing source.

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

> Update: `SprRuntimeService.ComputePathUid` now defaults to the PC signed-byte
> variant and can still compute the old unsigned variant via `signedBytes:false`. Do
> not reintroduce unsigned-only lookup for CJK paths; it makes real PAK assets look missing.
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

### 5. Minimap/HUD parity is part of map porting

A ported map is not complete until the HUD minimap and preview behave like PC enough for
navigation. The hard-won Unity implementation lives across:

| File | Minimap responsibility |
|------|------------------------|
| `Assets/Scripts/Sandbox/MinimapService.cs` | bidirectional world ↔ minimap coordinate transforms |
| `Assets/Scripts/Sandbox/MapManager.cs` | builds `MapDefinition.sourceBoundsRect` in Unity world coords |
| `Assets/Scripts/Sandbox/MapRenderer.cs` | exposes full active-map `ContentBounds` for minimap capture |
| `Assets/Scripts/Sandbox/SandboxRuntimeState.cs` | live active map + player position/name for HUD |
| `Assets/Scripts/Sandbox/SandboxPlayerController.cs` | movement/camera clamp must use active map bounds |
| `Assets/Scripts/UI/GameHudController.cs` | minimap texture, preview overlay, click-to-move |
| `Assets/Scripts/UI/PcHudVietnameseTextOverlay.cs` | Vietnamese map name + coordinate labels over UI |
| `Assets/UI/HUD/GameHud.uxml/.uss` | small minimap, preview frame, dots, close button |

Important behavior:

- Small minimap is **zoomed around the player**, not full-map-scaled. Full-map scale makes
  player movement invisible and looks like a gray/flat placeholder.
- Large preview uses the **full active map texture**. It opens when the user taps the
  minimap itself (mobile usability), not only the PC magnifier/world-map button.
- Tapping inside preview converts UI top-left pixel → normalized map → Unity world target
  and calls `SandboxPlayerController.MoveTo(target)`. Do not teleport unless debugging.
- Tapping outside the preview map frame closes the preview.
- Player dot follows `SandboxPlayerController.transform.position` every frame.
- Coordinate text mirrors PC `UiMiniMap.cpp`: PC displays `nScenePos0 / 8` and
  `nScenePos1 / 8`. In Unity world, because map Y is negative screen-space, display:
  `floor(world.x / 8) / floor(-world.y / 8)`.
- User-facing map names must be Vietnamese. Known mappings:
  - `巴陵县` / `Map_79` → `Ba Lăng huyện`
  - `风之骑` / `Map_389` / mojibake variants → `Phong Kỳ (Vượt ải 120+)`
  - `沙漠山洞1` / `Map_907` → `Vượt ải Nhiếp Thí Trần`
- A map is not truly navigable until player/camera clamp uses the active map bounds, not
  the old Ba Lăng defaults.

## When to use which reference

- **Porting a new map, or fixing a broken/incomplete one** → run the pipeline below.
- **Projection math / coordinate systems** → `references/projection.md` (full details).
- **Sorting / rendering bugs** → `references/sorting.md` (the complete sorting model).
- **Hitting a wall** (0 matches, gray tiles, corrupt sprites, wrong positions) →
  `references/pitfalls.md` lists every dead-end already explored so you don't repeat them.

## Prerequisites

Primary PC source: `/var/www/vltksource_new/vl_update_27/` (always available).

Optional: VMDK with original pak files. Check:

```bash
ls /mnt/jxwin/SourceNew/swrod3/bin/Client/data/maps.pak   # mounted?
```

If not mounted, the extractor falls back to the PC source tree. Only mount the VMDK
if you need raw `.pak` files not yet extracted — see `references/environment.md`.

Also needs `libucl.so` (UCL decompression) and Python 3 with `pefile`/`capstone`
(only for re-deriving the hash from a different engine.dll; not needed normally).

## Pipeline

### 1. Identify the map

First prove the PC source of truth. For normal maps, use `maplist.ini`/`.wor`; for
mission/default-map requests, also inspect the Lua/C++ entry script that sends the
player there and any auto-spawn script for NPCs. Example from Tín sứ Vượt ải 120+:
`maplist.ini` says map `389=...\信使任务\风之骑`, `wagoner.lua` sends `NewWorld(389,1582,3137)`,
and `global/autoexec.lua` spawns templates `822`/`377`. Preserve PC IDs and Việt hoá
only user-facing names.

For multi-instance mission maps, choose the representative map ID from the PC script, not
the display name alone. Example from Vượt ải Nhiếp Thí Trần:
`script/missions/killbossmatch/class.lua` has `tbMapId={907..916}`, `tbNpc={1480..1489}`,
entry `NewWorld(nMapId,1476,3274)` / `NewWorld(nMapId,1579,3186)`, and `maplist.ini`
maps `907=西北北区\沙漠迷宫\沙漠山洞1`; use project map id `907` unless the user asks for
another instance. Read `NpcS.txt`/`Reference/PcNpc/npcs.txt` for template stats and
`NpcResType` before creating any enemy fallback.

Use the helper to find the map name and bounds in one shot when `.wor` is available:

```bash
python3 scripts/list_maps.py 巴陵        # search by substring
python3 scripts/list_maps.py --id 53     # show one entry
```

It prints `id=<MapList index>  <region\name>  rect=minX,minY,maxX,maxY`. The value
(`region\name`) is what you pass to `--map-name`; the `rect` is the grid bounds
(read from the map's `.wor`). If a `.wor` is missing it says so — pass `--bounds`
explicitly (or `--scan-pad` to widen and keep only cells that resolve).

The project's own `mapId` (used for the StreamingAssets folder `Map_{id}_C`) may differ
from the MapList index — check `Assets/StreamingAssets/MapCatalog.json` and PC
`settings/maplist.ini`. For balang the MapList index is 53 but project default used 79;
for Phong Kỳ Vượt ải both PC/default map id is 389. The script takes project id explicitly.

### 2. Run the extractor

```bash
python3 scripts/jx_map_port.py \
  --map-name '两湖区\巴陵县' \
  --project-map-id 79 \
  --unity-root /var/www/vltk-mobile

# If the .wor/bounds path is missing or a mission map needs a verified window, pass explicit PC grid bounds.
python3 scripts/jx_map_port.py \
  --map-name '特殊用地\任务用地\信使任务\风之骑' \
  --project-map-id 389 \
  --unity-root /var/www/vltk-mobile \
  --data-dir '/var/www/vltksource_new/vl_update_27/Client 6.0/data' \
  --bounds 79 78 101 100

# Mission maps with map-specific NPC/boss visuals: stage extra SPRs from NpcResType too.
cat >/tmp/map907_extra_spr.txt <<'EOF'
spr\npcres\boss\boss018\boss018_wlk.spr
spr\npcres\boss\boss019\boss019_wlk.spr
EOF
python3 scripts/jx_map_port.py \
  --map-name '西北北区\沙漠迷宫\沙漠山洞1' \
  --project-map-id 907 \
  --unity-root /var/www/vltk-mobile \
  --bounds 77 96 105 109 \
  --extra-spr-file /tmp/map907_extra_spr.txt
```

It will, in one pass:
- index every pak under `bin/Client/data`,
- for each grid cell hash `\maps\{map-name}\v_{Y:03d}\{X:03d}_Region_C.dat`, decompress
  the entry (UCL), and write `Assets/StreamingAssets/TestData/Regions/Map_{id}_C/{X}_{Y}_Region_C.dat`
  plus `manifest.json` + `image_names.json`,
- collect every ground / cover / builtin `imageName`, resolve each with the same hash,
  extract + rebuild the SPR (handling per-frame-compressed sprites), and stage it under
  `Assets/StreamingAssets/Sprites/{ComputePathUid}.spr` so the runtime finds it by name.
- stage any `--extra-spr` / `--extra-spr-file` path and write `extra_spr_names.json`; use
  this for mission NPC/boss visuals because they are not referenced by Region_C art names.

Expected healthy output: `extracted regions: N`, `staged art: M/M failed=0`.
Then verify `manifest.json` and `image_names.json`; use `image_names.json` for the
referenced-art count because shared SPRs may appear as modified existing files in git,
not only new files. When extras are staged, `M` equals map art plus extra SPRs; verify
`extra_spr_names.json` separately. If `failed>0`, read `references/pitfalls.md` — usually
a loose-art fallback or a new per-frame SPR shape.

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

Do not treat `SprRuntimeService.CacheCount == 0` as a map failure: `MapRenderer` uses
`ResolveTexture`, not the sprite cache. Treat `MissCount == 0`, visible real terrain, and
`Rendered N regions` as the map-art signal.

### 4. Runtime glue checklist for new/default maps

If the task sets a map as default or makes it playable, update all runtime surfaces:

- `MapPortManifest`: add id, Vietnamese name, PC name hint, status.
- `SandboxManager.defaultMapId` **and** serialized `Assets/Scenes/Sandbox.unity`
  `defaultMapId`; Unity serialized scene values override C# field initializers.
- `MapManager`: when PC maplist entries have zero/missing rect, derive `sourceBoundsRect`
  from `Map_{id}_C/manifest.json` min/max `col`/`row`.
- `MapEnemyDatabase`: add PC template ids and default spawn only from PC scripts/data.
- `SandboxRuntimeState`, `GameHudController`, `PcHudVietnameseTextOverlay`: show Vietnamese
  names even when PC text arrives as Chinese or mojibake.
- Prefer `MapPortManifest.TryGet(activeMapId)` for HUD/overlay map names before raw PC text;
  raw names can arrive as mojibake (`M颽 an...`) even when the manifest name is correct.
- NPC SPR path folders are data-dependent: `ani*` → `spr\npcres\animal`, `boss*` →
  `spr\npcres\boss`, otherwise `spr\npcres\enemy`. Do not route `boss018` through
  `enemy` or visuals will spawn with nameplates but no body.
- `SandboxPlayerController`: call/apply active map bounds after `MapManager.OnMapLoaded`;
  never leave Ba Lăng `mapBoundsMin/Max` as movement/camera clamp for another map.
- Serialized prefabs can also stale public fields (example: `Assets/Prefabs/Player.prefab`
  `moveSpeed`). If a public field affects map play, inspect scene/prefab YAML too.

### 5. Visual verification checklist

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
- **Mission NPC visuals load**: For boss/mission maps, console should show `PcNpcVisual Loaded
  spr\npcres\boss\...` and a play probe should report `visualsWithClip == visuals.Length`.
  Nameplates without bodies mean either extra SPRs were not staged or the NPC folder mapping
  (`boss` vs `enemy`) is wrong.

### 6. Keep the tests green

Prefer targeted Unity tests for touched systems (`MinimapTests`, `HudDataBridgeTests`,
`SandboxBootE2ETests`, map flow tests). Some repo test assemblies are gated by
`VLTK_ENABLE_TESTS`; if enabling that define exposes unrelated stale compile errors, record
that fact and fall back to `validate_script` plus Play Mode probes for the touched runtime.
Unity MCP `run_tests` may sometimes return `summary.total=0` for a specific test name; do
not count that as real coverage. Add/adjust an EditMode assertion anyway, validate scripts,
then use Play Mode probes for default map, bounds, renderer, enemies, and visual clips.
Always clear/read console after refresh; known Addressables GUID conflicts can be ignored
per AGENTS, but new compile/runtime errors cannot.

### 7. Minimap / preview / click-to-move verification

After porting or changing a map, validate the HUD map surface too. This catches subtle
coordinate bugs that normal map screenshots miss.

PC references:

- `/var/www/vltksource_new/vl_update_27/Utility/Run/Ui/ui3/小地图_小.ini`
  - `[MiniMap] Left=670 Top=0 Width=130 Height=130`
  - `[MapRect] Left=1 Top=1 Width=128 Height=128`
  - `[SwitchBtn] Left=101 Top=115 Width=14 Height=14`
  - `[WorldMapBtn] Left=115 Top=115 Width=14 Height=14`
- `/var/www/vltksource_new/vl_update_27/SwordOnline/Sources/S3Client/Ui/UiMiniMap.cpp`
  - left-click on minimap forwards to game space (`Wnd_TransmitInputToGameSpace`)
  - PC scene label uses `Set2IntText(nScenePos0 / 8, nScenePos1 / 8, '/')`
  - world map button switches to `MINIMAP_M_WORLD_MAP`
- `/var/www/vltksource_new/vl_update_27/SwordOnline/Sources/S3Client/Ui/UiWorldMap.cpp`
  - PC world map closes on click/key; mobile keeps click-inside for move, click-outside for close.

Unity implementation rules:

1. **Active bounds source**
   - `MapManager.BuildRuntimeDefinition()` should set `sourceBoundsRect` in Unity world coords:
     - `x = rectLeft * 512`
     - `y = -(rectTop * 512) - (regionHeight * 512)`
     - `width = regionWidth * 512`
     - `height = regionHeight * 512`
   - `MapRenderer.ApplyFullMapBounds()` should expose full active-map bounds, not only the town
     focus crop, so full preview coordinate mapping is stable.
   - `SandboxPlayerController.mapBoundsMin/Max` must equal `sourceBoundsRect` min/max after
     map load; otherwise manual movement can stop before the minimap edge.

2. **Bidirectional conversion**
   - `MinimapService.WorldToMinimapPixel(map, world, size)` returns top-left-origin pixel for dots.
   - `MinimapService.MinimapPixelToWorld(map, pixel, size)` is exact inverse for click-to-move.
   - `MinimapService.MinimapNormalizedToWorld(map, normalizedTopLeft)` is useful for tests/probes.
   - Unit tests must cover offset bounds, clamping, y flip, and inverse pixel → world.

3. **Small minimap texture**
   - Render a **zoomed square** around player. Current proven span: `2048` world units.
   - Clamp zoom window inside active map bounds.
   - Re-render small minimap when player moves enough (current threshold: `128` world units) or map changes.
   - Dot mapping for small minimap must use the same zoomed bounds, not full map bounds.

4. **Preview texture**
   - Render full active map once per map using an offscreen camera / `RenderTexture`.
   - Use an explicit orthographic projection matching map aspect:
     `Matrix4x4.Ortho(-bounds.size.x/2, bounds.size.x/2, -bounds.size.y/2, bounds.size.y/2, near, far)`.
     Do not rely on `Camera.orthographicSize` alone for square render targets or the preview will crop/letterbox wrong.

5. **Opening/closing interaction**
   - Register preview-open on `MinimapPanel`, `MinimapFrame`, `MinimapContent`, `PlayerDot`, and both minimap buttons.
     This avoids “sometimes tap does nothing” caused by child elements catching events.
   - Use `StopImmediatePropagation()` when opening.
   - Overlay should close on pointer down when `!MapPreviewFrame.worldBound.Contains(evt.position)`.
   - Pointer inside `MapPreviewFrame` should move player and close preview after target is set.

6. **Coordinate/name labels**
   - UI Toolkit labels may not render reliably in this project; `PcHudVietnameseTextOverlay.cs` draws IMGUI text over the HUD.
   - Prefer manifest ID lookup for the overlay title; only fall back to raw-name switch mappings.
   - Small minimap label positions in 1280x720 reference coords:
     - map name: `(1144, 4, 112, 14)`
     - coords: `(1146, 18, 112, 14)`
   - Preview labels:
     - header: `(394, 82, 492, 20)`
     - footer: `(394, 596, 492, 20)`
   - Convert Chinese/raw names to Vietnamese before display. Known mappings:
     - `巴陵县` / `Map_79` → `Ba Lăng huyện`
     - `风之骑` / `Map_389` / mojibake variants → `Phong Kỳ (Vượt ải 120+)`

7. **Proof checklist**
   - Unity compile: `0` new errors; ignore only documented Addressables GUID conflicts.
   - Prefer targeted tests: `VLTK.Tests.Sandbox.MinimapTests`, `VLTK.Tests.Sandbox.HudDataBridgeTests`.
   - PlayMode probe should show:
     - Vietnamese `SceneName` / overlay map name
     - minimap background texture present
     - preview background texture present
     - preview opens from minimap tap path
     - outside-preview click closes overlay
     - target movement changes player position + coordinate readout
   - Movement-bound regression: place or move the player near every active-map edge. For map 389,
     `x=41000` must remain inside (old Ba Lăng clamp snapped it to `48128`); out-of-bounds
     `x=39000,y=-53000` should clamp to `(40448,-51712)`.
   - Screenshot evidence: include small minimap zoom, preview map, visible coordinates, and joystick not blocked.

## Output contract

Per ported map, under `Assets/StreamingAssets/`:
- `TestData/Regions/Map_{id}_C/{col}_{row}_Region_C.dat` — one per occupied cell
- `TestData/Regions/Map_{id}_C/manifest.json` — bounds, cells, hasGround/hasBuiltin
- `TestData/Regions/Map_{id}_C/image_names.json` — every art path referenced
- `TestData/Regions/Map_{id}_C/extra_spr_names.json` — optional mission/NPC SPRs staged with `--extra-spr*`
- `Sprites/{ComputePathUid}.spr` — every resolved art asset (shared across maps)

The renderer, projection math, and SPR decoder are map-agnostic for plain extraction, but
playable/default maps still need runtime glue: manifest entry, scene default, spawn/NPC data,
Vietnamese HUD names, active bounds clamp, and validation probes.

## Key files in the project

| File | Purpose |
|------|---------|
| `Assets/Scripts/Sandbox/MapRenderer.cs` | Rendering pipeline: ground, cover, builtin layers, Z-projection, sorting, full-map bounds for preview |
| `Assets/Scripts/Sandbox/MapManager.cs` | Runtime map definition + manifest-derived `sourceBoundsRect` fallback |
| `Assets/Scripts/Sandbox/MinimapService.cs` | world↔minimap coordinate conversion and preview click target math |
| `Assets/Scripts/Sandbox/SandboxManager.cs` | Scene setup, default map, active-map bounds application, camera CustomAxis sort config |
| `Assets/Scripts/Sandbox/SandboxPlayerController.cs` | Player movement, active map clamp, camera follow, zoom level, preview target movement |
| `Assets/Scenes/Sandbox.unity` | Serialized default map id; update this when changing boot map |
| `Assets/Prefabs/Player.prefab` | Serialized player fields; inspect when movement/camera values seem stale |
| `Assets/Scripts/Sandbox/MalePlayerVisual.cs` | 8-direction SPR layered character rendering |
| `Assets/Scripts/Sandbox/BuildinObjParser.cs` | Parses KBuildinObj with ImgPos1-4 (x,y,z quads) |
| `Assets/Scripts/Sandbox/GroundLayerParser.cs` | Parses ground tiles + KSPRCoverGroundObj |
| `Assets/Scripts/Sandbox/RegionParser.cs` | Region_C.dat section table dispatch |
| `Assets/Scripts/Sandbox/CameraRigService.cs` | Pure C# camera rig (no MonoBehaviour) |
| `Assets/Scripts/UI/GameHudController.cs` | HUD minimap, map preview, active-map render texture, click-to-move |
| `Assets/Scripts/UI/PcHudVietnameseTextOverlay.cs` | Vietnamese minimap/preview name + coordinate overlays |
| `Assets/UI/HUD/GameHud.uxml/.uss` | Minimap and map preview visual tree/style |
| `Assets/Tests/EditMode/Sandbox/MinimapTests.cs` | Coordinate transform and inverse click mapping tests |
