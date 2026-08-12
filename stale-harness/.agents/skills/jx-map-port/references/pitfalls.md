# Dead-ends already explored — do NOT repeat these

Porting balang took many sessions. The hash was the first wall; the Z-projection, sorting,
and pivots were the second. Everything below was tried and confirmed; the table saves you
from re-walking it.

## The root cause of almost all "it doesn't work"

**Wrong:** unsigned-byte path hash (what the project's C# `ComputePathUid` and the old
vltktool used). For ASCII-only paths (`\spr\npcres\...`, `\settings\maplist.ini`) it
*happens* to match because all bytes are <128, so people "validate" the hash and trust it.
But every terrain/building path contains GBK Chinese (bytes ≥0x80) and silently misses.

**Right:** `g_FileName2Id` treats bytes as **signed char** (movsx). Use the version in
`scripts/jx_map_port.py::g_filename2id`. Validate it on a Chinese path, e.g.
`\maps\两湖区\巴陵县\v_106\106_Region_C.dat` should be present in `maps.pak`. If a known
Chinese path resolves, the hash is correct.

To re-derive the hash from a different `engine.dll`: disassemble the export
`?g_FileName2Id@@YAKPAD@Z` (RVA ~0x10799 in the 2021 build). Look for `movsx` (signed) vs
`movzx` (unsigned) on the path byte, the `% 0x8000000B`, the `imul …, -0x11` (=0xFFFFFFEF),
and the final `xor 0x12345678`. capstone/pefile do this in ~30 lines.

## Things that look promising but are WRONG / wasteful

| Approach | Why it fails / wastes time |
|---|---|
| Obstacle-hash matching loose `_Region_S` ↔ `maps.pak` | The loose `Utility/Run/maps` *server* tree is a **different map revision** than `bin/Client/data/maps.pak`. Only ~78–98 of 580 balang cells match. Don't bridge versions this way. |
| Brute-forcing path templates with the unsigned hash | 0 matches even with 630 known (X,Y,UID) Chengdu pairs. The hash, not the template, was wrong. |
| Assuming "terrain art isn't shipped in the client" | False. It's in the paks; it just wouldn't resolve under the wrong hash. With the signed hash, all 198 balang art names resolve. |
| Object-coordinate filtering to isolate a map from maps.pak | Scene coords are reused across maps, so you catch 35k yellow-palette regions from many maps. You can't isolate one map by content/coords. Use the path hash. |
| Relying on a sibling project's pre-converted regions (e.g. an h5 export) | It can be a different/incomplete revision (missing town-center buildings, only 59 vs 198 art names). Extract from the pak yourself. |
| Trusting the in-repo `SprRuntimeService.ComputePathUid` for pak lookups | It's unsigned — correct ONLY as the extractor↔runtime filename scheme, never for reading paks. |
| Downloading whole 3–7 GB clients to "find the art" | Clients are pak-only; the art was always in the local VMDK pak. Crack the hash first; download only if a genuinely different map revision is needed. |

## SPR gotchas

- **Per-frame compressed SPRs** (`桃树`, `竹子`, large trees, `大碧波_*` water blends):
  stored as `head+palette + frame_info[Frames]{compress_size:i32, size:i32} + blobs`.
  Copying the raw bytes yields garbage frame dims (you'll see Unity
  `Texture has out of range height (got 25601 max 16384)`). The script's
  `spr_rebuild_perframe` decompresses each frame and rewrites a flat SPR. If a NEW shape
  appears, detect it by validating frame0's width/height ≤ 16384 and rebuild.
- **Whole-UCL SPRs**: entry flag's high byte is the UCL method; decompress the whole entry.
- **Loose-art 0xCD-filled files**: some loose `游戏资源` SPRs are MSVC-uninitialized
  placeholders (start `CD CD …`). Ignore them; the pak copy is the real one. The script
  prefers pak over loose for exactly this reason.

## Rendering gotchas (each took significant effort to diagnose)

### Ignoring the Z coordinate in builtin projection

**Symptom:** Dark vertical gaps through gate archways, house roofs detached from walls,
multi-piece structures appearing broken/scattered.

**Root cause:** `KBuildinObj` has 4 ImgPos corners each with (x, y, z). The Z coordinate
represents height above ground. The engine's `CoordinateTransform` projects Z into screen-Y:
`screenY = sceneY/2 - sceneZ*(887/1024)`. Ignoring Z makes tall objects (Z=100-630) render
hundreds of pixels too low.

**Fix:** `screenY = imgY1 * 0.5f - imgZ1 * (887f/1024f)` in `MapRenderer.RenderBuiltinObjects`.

**Why it's tempting to skip Z:** Cover objects (KSPRCoverGroundObj) have NO Z field, and
ground tiles don't either. It's easy to assume builtins are the same. But builtins are
3D scene objects with full (x,y,z) positioning — the SPR art is mapped onto a 3D quad
defined by ImgPos1-4.

### Using int16 sortingOrder overflow (screenY * 2 encoding)

**Symptom:** Map looks "99% complete" but structures in dense areas (town centers) have
incorrect layering — pieces of different buildings draw in wrong order, gates look scattered.

**Root cause:** Unity's `sortingOrder` is internally **int16** (range -32768..32767). The old
approach encoded `sortingOrder = screenY * 2`, but screenY values for balang objects reach
~50000. `Mathf.Clamp(±32000)` pinned 3580 objects at the same ceiling value → undefined
draw order among them → random occlusion.

**Fix:** Don't encode screenY into sortingOrder at all. Use CustomAxis world-Y sort
(`transparencySortMode = CustomAxis`, `transparencySortAxis = (0,1,0)`) as the depth
mechanism, and use sortingOrder only for coarse layer separation (ground=-1000, cover=0,
builtin=1000+fileIndex, player=5000).

### Wrong sprite pivot for builtin objects

**Symptom:** Structures appear shifted — half a sprite width to one side, offset upward or
downward from where they should be. Gaps between adjacent building pieces.

**Root cause:** Using bottom-center pivot `(0.5, 0)` for builtins when the data positions
them at ImgPos1 = top-left corner. Bottom-center shifts every sprite by half its width
rightward and its full height downward.

**Fix:** Builtin objects use top-left pivot `(0, 1)`. Cover objects use bottom-center
`(0.5, 0)` — they position at the base/foot. Ground tiles also use `(0, 1)`. Each object
type's pivot matches how the original engine positions the sprite relative to its anchor.

### Using pure Y-sorting for multi-piece structures

**Symptom:** Gate crossbeams draw in front of near pillars (should be behind), or complex
structures like archways have pieces in wrong order despite being at similar Y positions.

**Root cause:** The original engine uses a spatial binary tree (KIpoTree) that traverses
in-order to produce correct draw order. Pure Y-sorting cannot handle interlocking pieces
where a crossbeam must draw behind one pillar but in front of another at a similar Y.

**Fix:** Use a monotonically-increasing file-order counter as sortingOrder for builtins.
The data files already store objects in the KIpoTree's traversal order within each region.
Regions are iterated col-by-row (back-to-front). This preserves the authored draw order
without needing to reconstruct the spatial tree.

### Cover objects drawing on top of buildings (grass on rooftops)

**Symptom:** Green grass sprites appear on top of house roofs, road decals float above walls.

**Root cause:** Cover and builtin objects had the same `sortingOrder` (or cover was higher).
With CustomAxis Y-sort, a cover sprite at a lower Y (higher on screen) would draw on top
of a building piece at a higher Y.

**Fix:** Strict layer separation: cover at `sortingOrder=0`, builtin at `sortingOrder≥1000`.
Cover objects are flat ground decals that must NEVER draw above any structure.

### Player visual "ghost" / duplicate character

**Symptom:** Player appears twice at the same position — one animated correctly, one frozen
in idle pose.

**Root cause:** `MalePlayerVisual.RefreshActionParts` disables old parts' renderers but
doesn't destroy the underlying GameObjects. When switching from Idle to Move (or vice versa),
`GetOrCreatePart` finds the dictionary entry for each `kind`, updates the runtime (new clip),
but the OLD GameObject (created during the first action) is still a child with `enabled=True`.
The dictionary only tracks one runtime per kind, so the old GameObject becomes an orphan
that renders on top of the active one.

**Fix:** Before loading new action parts, disable ALL children (including orphans). After
loading, destroy any child GameObject not tracked in the `_parts` dictionary:
```csharp
// Disable all children first
for (int i = transform.childCount - 1; i >= 0; i--)
    transform.GetChild(i).gameObject.SetActive(false);
// ... load new parts (reuses tracked GameObjects, re-enables them) ...
// Destroy orphans
var tracked = new HashSet<GameObject>();
foreach (var part in _parts.Values)
    if (part.renderer != null) tracked.Add(part.renderer.gameObject);
for (int i = transform.childCount - 1; i >= 0; i--)
    if (!tracked.Contains(transform.GetChild(i).gameObject))
        Destroy(transform.GetChild(i).gameObject);
```

## General Unity/MCP gotchas

- `manage_camera screenshot` only captures real content **while in play mode**. A shot
  after `manage_editor stop` shows just the skybox gradient. Always screenshot during play.
- The Sandbox scene is empty at edit time; `SandboxManager` builds everything at runtime
  under `DontDestroyOnLoad`, so `find_gameobjects` in edit mode returns nothing — that's normal.
- The town-focus heuristic weights named structures (房屋/house/桥/墙/牌坊/井) far above
  trees so the camera frames the settlement, not a forest patch. If a new map's center is
  off, check that its buildings carry those substrings, or adjust the weight list.
- Unity's MCP bridge can drop/restart during long region loads (618 regions × many sprites).
  If `read_console`/screenshot returns "session not ready", wait ~10s and retry; re-enter
  play mode cleanly if the watchdog restarted.
- `CameraRigService` and `SandboxPlayerController.FollowCamera` reassert camera position/zoom
  every frame. To take custom-position screenshots, either execute code to reposition the
  camera immediately before the screenshot call, or adjust `followOrthoSize` in
  `SandboxPlayerController` (default=480 for a wide view).

## Verification quicklist

1. `extracted regions: N` with N ≈ cells in the `.wor` rect (618 for balang's 33×20 minus gaps).
2. `staged art: M/M failed=0`.
3. Unity console: `Rendered N regions; SPR stats: 0 missing`.
4. Play-mode reflection check: `proceduralFallbackTiles=0`, `nullDecor=0`, real art sprites > 0.
5. No dark gaps through structures (Z-projection working).
6. No grass/roads on rooftops (cover < builtin layer separation).
7. No duplicate player character (orphan cleanup working).
8. Multi-piece structures (牌坊 gate) render with correct piece ordering.
9. `run_tests EditMode` all pass (410 as of balang port).
