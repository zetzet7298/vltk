---
name: jx-player-visual
description: >-
  Build, fix, or extend on-screen player avatar/player-like NPC visuals in
  VLTK-mobile from the original JX Online 1 layered SPR character system. Use
  for player/character visual work: female/woman avatar, armor/weapon/hat swaps,
  mount/dismount, invisible player, wrong Z-sort, wrong direction/animation,
  horse spinning idle, joystick move blocked, square overlay, attack/stand/run
  actions, map spawn, or mentions MA_*/WO_* parts, HH/HB/HT horse body,
  RD01/HR01 mounted actions, 男主角/女主角贴图顺序表, npcres\man/woman,
  body/head/hair/hand/weapon layers, 8-way direction, mount/cưỡi ngựa,
  MalePlayerVisual, MalePlayerSpriteCatalog, SandboxPlayerController,
  PlayerMountService, or MountToggleButton. Preserve signed/unsigned hash rules,
  staged SPR pipeline, mounted layered-horse fixes, and sorting/cache/UI/joystick
  bug knowledge.
---

# JX Player Visual (layered SPR avatar)

Render a JX/VLTK player character in the Unity mobile sandbox the way the original PC
client does: **many SPR parts stacked as separate `SpriteRenderer`s** (shadow, body,
head, hair, hands, weapons, horse), animated per-action, ordered per-direction by the
PC draw-order table. Male is the reference implementation; the same model covers the
female avatar and any equipment/mount swap.

## Mental model (read first)

A JX character is NOT one sprite — it is a stack of independent part SPRs (shadow, body, head,
hair, hands, weapons, and when mounted, horse) sharing one canvas and reference pixel. Each part
SPR holds all 8 directions of one action. To draw a frame: pick the part's sprite for
`direction * framesPerDirection + frame`, offset it from the shared reference pixel, and set its
`sortingOrder` from the per-direction draw-order table. Get this model right and male, female,
equipment, and mount all fall out of the same code — only part filenames and variant numbers
change. The detailed version is in the section below.

## Resource/hash guard learned from combat visual port

Before concluding that any PC SPR/icon/effect/NPC/HUD asset is missing, apply `jx-pc-port-rule` → **PC resource resolution doctrine**:

- Read PC TXT/INI tables with the correct encoding. Paths with Chinese resource folders are usually GB2312/GBK; mojibake paths hash to fake UIDs.
- PAK entries named `unknown/<uid>.spr` are valid extracted PC assets, not garbage.
- For PAK lookup use PC signed-byte FileNameHash, not an unsigned-byte/private runtime hash.
- Copy exact PC assets into `Assets/StreamingAssets/...`; never load directly from `/var/www/vltksource_new` at runtime.
- Verify with real file existence/decode/render evidence before claiming parity or missing source.

A JX character is NOT one sprite. It is a stack of independent part SPRs that share a
common canvas and reference pixel. Each part SPR holds **all 8 directions** of one
action (e.g. body-idle, body-run). To draw a frame you pick the part's sprite for the
current `direction * framesPerDirection + frame`, place it by its per-frame offset
relative to a shared reference pixel, and set its `sortingOrder` from the
**per-direction draw-order table** so parts overlap correctly (hair behind/in-front of
head depending on facing, weapon behind body when facing away, etc).

Get this model right and male/female/equipment all fall out of the same code — only
the part filenames and variant numbers change.

## What already exists (reuse, don't rebuild)

The male avatar is fully working. Read these before writing anything new:

| File | Role |
|------|------|
| `Assets/Scripts/Sandbox/MalePlayerSpriteCatalog.cs` | Part enum, part->SPR table (idle/move), 8-way `DirectionFromMove`, per-direction `SortingOffset` from the PC draw-order table. |
| `Assets/Scripts/Sandbox/MalePlayerVisual.cs` | Renderer: one `SpriteRenderer` per part, SPR decode + cache, ref-pixel offset, frame/sort apply. Holds both visibility-bug fixes. |
| `Assets/Scripts/Sandbox/SandboxPlayerController.cs` | Joystick + keyboard input -> world move + camera follow (`followOrthoSize`); forwards move vector to the visual; `Mount`/`ToggleMount`; `defaultHorseId`. |
| `Assets/Scripts/Sandbox/PlayerMountService.cs` | Mount state machine (None→Mounting→Mounted→Dismounting), 0.5s transitions, `IsMounted`, `SpeedMultiplier`. |
| `Assets/Scripts/Sandbox/MountToggleButton.cs` | HUD Lên/Xuống Ngựa button; label auto-flips from `Mount.IsMounted`. |
| `Assets/Scripts/Sandbox/SandboxManager.cs` | Auto-spawns player + joystick + camera on map load; `PlacePlayerOnActiveMap()` centers on `MapRenderer.ContentBounds`. |
| `Assets/Scripts/Sprites/SprRuntimeService.cs` | `ComputePathUidHex` (defaults SIGNED, `signedBytes:false` for legacy unsigned) — runtime SPR file naming + pak-accurate lookup. |
| `Assets/Scripts/Sprites/SprDecoder.cs` | SPR -> texture/frames/offsets. |
| `Assets/StreamingAssets/male_player_sprites.json` | Manifest: name -> uid -> staged file. |
| `Assets/Tests/EditMode/Sandbox/MalePlayerVisualTests.cs` | Catalog/direction/load/move tests. |

### Additional avatars (already in codebase)

| File | Role |
|------|------|
| `Assets/Scripts/Sandbox/FemalePlayerSpriteCatalog.cs` | Female part enum + SPR table, same 8-way draw-order as male. |
| `Assets/Scripts/Sandbox/FemalePlayerVisual.cs` | Female avatar renderer, mirrors MalePlayerVisual structure. |
| `Assets/StreamingAssets/female_player_sprites.json` | Female part manifest. |
| `Assets/StreamingAssets/female_mount_sprites.json` | Female mount SPR manifest. |
| `Assets/StreamingAssets/male_mount_sprites.json` | Male mount SPR manifest. |
| `Assets/StreamingAssets/horse_sprites.json` | Horse mount SPR manifest. |
| `Assets/StreamingAssets/TrainingSprites/` | Training/dummy SPR assets. |
| `Assets/Scripts/Sandbox/EnemyAiService.cs` | Reusable AI service (wander/engagement). |
| `Assets/Scripts/Sandbox/EnemyTemplateFactory.cs` | Enemy template factory from PC NpcS.txt. |

To add a NEW avatar (female, alt class) the cleanest path is to **generalize the male
classes by gender/variant** rather than copy-paste. See `references/extending.md`.

## Source data (PC client: `/var/www/vltksource_new/vl_update_27/`)

Character definitions live in `SourceNew/swrod3/Utility/Run/Settings/NpcRes/` and art in
`.../Run/spr/npcres/`. The master row table is `人物类型.txt` (tab-separated):

- `男主角` (male hero) -> art `spr\npcres\man`, draw-order `男主角贴图顺序表.txt`
- `女主角` (female hero) -> art `spr\npcres\woman`, draw-order `女主角贴图顺序表.txt`

The male and female draw-order tables are **identical for Dir1..Dir8**, so the existing
`SortingOffset` works unchanged for the female avatar.

### SPR filename grammar

`MA_<PART>_<VARIANT>_<ACTION><NN>.spr` (male; female uses its own prefix in `woman/`).

| PART | meaning | part id (draw-order) |
|------|---------|----------------------|
| YY | shadow (影) | -1 |
| HD | head (头) | 0 |
| HR | hair (发) | 1 |
| BD | body/armor (躯体) | 5 |
| LH / RH | left / right hand | 6 / 7 |
| LW / RW | left / right weapon | 8 / 9 |
| HH / HB / HT | **horse** front / mid / rear (马前/马中/马后) | 12 / 13 / 14 |

**Do NOT confuse HH/HB/HT with headgear** — they are the MOUNTED HORSE BODY (front,
middle, rear). When mounted, the horse renders as these three layered parts *inside the
player visual*, frame-synced with the rider (not a separate horse GameObject — see the
Mounted system section).

`VARIANT` is the equipment id: rider body/head `019` on-foot / `050` mounted, horse body
`016`, empty-hand weapon `000`, shadow `999`. Swapping armor/weapon/horse = swap the
variant number for that part only.

### Action codes (`<ACTION>` in the filename)

| code | action | frames (male) | used by |
|------|--------|---------------|---------|
| ST | stand / idle (on-foot) | 120 = 15/dir × 8 | `Idle` |
| RN | run / move (on-foot) | 88 = 11/dir × 8 | `Move` |
| RD01 | RideStand — mounted IDLE | 112 = 14/dir × 8 | `Ride` |
| HR01 | RideRun — mounted GALLOP | 112 = 14/dir × 8 | `RideMove` |
| MG / AT | magic / attack | varies | `Magic` / `Attack` |

Always derive `framesPerDirection = totalFrames / directions` at load time — do not
hard-code 11/14/15; parts/actions differ.

**⚠ SPR header `directions` lies for some files.** HH (horse-front) and HT (horse-rear)
headers report `directions=1` even though the file holds 112 frames = 8 dirs × 14. HB
(horse-mid) reports the correct `8`. If you trust the header you get a 1-dir × 112-frame
clip whose idle animation walks through ALL 8 direction slices — the horse visibly
"spins" while standing still. Fix: pass an `expectedDirections` hint per part (see the
Mounted system section).

## The two hashes (do not mix them up)

There are TWO uses of a path hash in this project. Both lowercase ASCII `A-Z` and run the
same `value` recurrence; they differ ONLY in how a path byte `>= 0x80` is treated (signed
`b-256` vs unsigned 0..255):

1. **Pak lookup hash (`g_FileName2Id`, SIGNED byte).** Used to find an entry inside
   `maps.pak` / `spr.pak`. High bytes (Chinese GBK, `>=0x80`) are treated as signed
   (`b - 256`). This is the `jx-map-port` skill's hash. Use it to read from paks.
2. **Runtime file-naming hash (`ComputePathUid`).** Used by
   `SprRuntimeService.ComputePathUidHex` to name staged files `{uid}.spr`. The C# default
   is now **SIGNED** (`signedBytes:true`), and `ResolveSpr` tries uidFromPath → signed →
   unsigned. ASCII-only player paths (`spr\npcres\man\MA_BD_019_ST01.spr`) have no high
   bytes, so signed == unsigned there — but for any CJK part path you MUST use the signed
   variant or the lookup misses real `unknown/<uid>.spr` assets. The legacy unsigned-named
   staged files still resolve via the final unsigned fallback.

Verified: `spr\npcres\man\MA_BD_019_ST01.spr` -> uid `45488ea8` (signed == unsigned for this
ASCII path), matching the manifest and the file the runtime loads. CJK evidence:
`\spr\Ui\技能图标\icon_sk_ty_at.spr` -> signed `c4454165`, unsigned `bedc5b69`. `scripts/uid.py`
is the reference impl (signed by default; `--unsigned` for the legacy variant).

## Offline/Python Composite Sprite Assembly (for Static Gallery / Tooling)

When generating a static web gallery or preview tool where layered Unity rendering is not available, you can assemble the complete character sprite into a single PNG in Python:
1. **Extract variant & parts**: From the body path (e.g., `ma_bd_001_st01.spr`), parse the gender (`ma`/`fm`) and variant (`001`). Locate corresponding part files: head (`hd`), hair (`hr`), left hand (`lh`), right hand (`rh`).
   - **⚠ Path classification pitfall**: When detecting if a path is male or female, do NOT use `"man/" in path.lower()` directly because `"woman/"` contains `"man/"` as a substring and will cause female characters to be classified as male (resulting in wrong part resolutions). Use `"/man/" in path.lower() or "\\man\\" in path.lower()` instead, or check for `"woman"` first.
2. **Decode frame metadata**: Open each SPR, parse the frame count and offsets. The frame's visual offsets (`offsetX`, `offsetY`) are stored at bytes 4-5 and 6-7 of the frame blob as signed 16-bit integers (`<h`).
3. **Compute global bounding box**:
   - `min_x = min(offset_x)`, `max_x = max(offset_x + width)`
   - `min_y = min(offset_y)`, `max_y = max(offset_y + height)`
   - Composite width = `max_x - min_x`, composite height = `max_y - min_y`.
4. **Z-Order Drawing**: Create a transparent canvas and draw the parts from back to front (e.g., for front-facing idle direction 0):
   - Hair (`hr` - if present) -> Body (`bd`) -> Left Hand (`lh`) -> Right Hand (`rh`) -> Head (`hd`).
   - Draw coordinate: `dest_x = offset_x + local_x - min_x`, `dest_y = offset_y + local_y - min_y`.

## Staging pipeline (get art into the build)

The runtime reads `Assets/StreamingAssets/Sprites/{uid}.spr`. To stage a part set:

1. Collect the source `.spr` files for the avatar (from `npcres/man` or `npcres/woman`,
   or extracted from `spr.pak` via the `jx-map-port` pak reader if not on disk).
2. For each, compute the UNSIGNED uid (`scripts/uid.py` or `ComputePathUid`).
3. Copy to `Assets/StreamingAssets/Sprites/{uid}.spr`.
4. Append `{name, sourcePath, uid, unityPath, bytes}` to the manifest json.
5. `refresh_unity` so Unity imports the new files.

`scripts/stage_player_spr.py` does steps 2-4 for a folder. Keep `sourcePath` in the
exact backslash form the catalog uses (`spr\npcres\man\MA_BD_019_ST01.spr`) — that
string is what the runtime re-hashes, so any mismatch silently breaks the lookup.

## Two visibility bugs already solved (don't relive these)

Both were fixed in `MalePlayerVisual.cs`. If a new avatar goes invisible, it is almost
certainly one of these — check before debugging anything else.

### Bug 1: player drawn UNDER the map (sorting model has CHANGED)

The avatar lands below dense town map-art if its `sortingOrder` is wrong. **The old
“screenY*2 clamped to ±32000” scheme is GONE** — map content orders reach ~100000 so the
16-bit-ish clamp both overflowed and capped. Current model (see `MapRenderer.cs` +
`MalePlayerVisual.PlayerBaseSortingOrder`):

```csharp
public const int PlayerSortingOrder = 5000;   // MapRenderer: actors above static map art
private int PlayerBaseSortingOrder() => MapRenderer.PlayerSortingOrder;
```

The player base order is a **flat 5000** (no screen-Y encoding). Depth among nearby
sprites is handled by the camera's `transparencySortMode = CustomAxis` world-Y sort, NOT
by baking screen-Y into `sortingOrder`. Per-part `SortingOffset` (0..~22) is added on top
so the avatar layers stack correctly. If a new avatar renders under the map, check that
its base order is `MapRenderer.PlayerSortingOrder` and that the camera CustomAxis sort is
still configured — do NOT reintroduce a screen-Y clamp.

### Bug 2: invisible after stop -> play (no recompile)

The SPR clip cache is `static`. Runtime `Sprite`/`Texture2D` objects are destroyed when
play mode stops. With **Domain Reload disabled** (fast enter play mode) the static dict
survives, so the next play gets cache hits that return destroyed (fake-null) sprites ->
`sprite == null` on every part -> invisible, and no "Loaded" log (cache hit skips decode).

Two-layer fix, both required:

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
private static void ResetStaticCaches() { ClipCache.Clear(); MissingLogCache.Clear(); }
```

plus a guard at the cache-hit site that re-decodes if the cached clip's sprites were
destroyed (`IsClipAlive`). `SubsystemRegistration` runs on every play start regardless
of the domain-reload setting, which is why it works where a normal static initializer
would not. Any new static cache holding runtime Unity objects needs the same treatment.

## Mounted system (cưỡi ngựa) — layered horse + rider

The horse is NOT a separate GameObject. When mounted, the avatar renders the **horse body
as three extra parts inside the SAME player visual**, frame-synced and direction-synced
with the rider. The legacy 50x76 single-frame `HorseVisual` GameObject is kept DISABLED
(`horse.gameObject.SetActive(false)` in `OnMountChanged`) to avoid a duplicate mismatched
horse. Don't re-enable it.

### The action mapping

`PlayerVisualAction` has two mounted states:
- `Ride` → suffix `RD01` (RideStand, mounted idle)
- `RideMove` → suffix `HR01` (RideRun, gallop)

`SetAction` remaps on-foot actions when mounted: `Move → RideMove`, everything else
`→ Ride`. `SetMounted(true)` picks `RideMove`/`Ride` from current `LastMoveInput`.
`ApplyFrame`'s rate switch must include `RideMove => moveFrameRate` (else gallop plays at
idle speed).

### The parts (`BuildMountedParts`)

Eight parts, all sharing one `suffix` (RD01 or HR01):
- Horse: `HH` HorseFront (12), `HB` HorseMiddle (13), `HT` HorseRear (14) — variant `016`.
- Rider: `BD` `HD` `HR` `LH` `RH` — variant `050`. No Shadow, no Weapon when mounted.

### Bug 3: horse “spins” while standing still (SPR header lies)

HH and HT SPR headers report `directions=1` (HB reports `8`) even though all three hold
112 frames = 8 dirs × 14. A 1-dir clip makes the idle loop walk frame 0..111 across every
direction slice → the horse rotates through all 8 facings while the rider stands still.

Fix (already in code): a per-part `expectedDirections` hint.

```csharp
// PlayerSpritePartSpec gains: public int expectedDirections;  (0 = trust header)
new(PlayerSpritePartKind.HorseFront, "HorseFront", BuildPath("HH", horseVariant, suffix), true, 8),
// ... HB, HT also pass 8

// LoadClip(sourcePath, expectedDirections): prefer the hint when it divides evenly
int directions = Mathf.Max(1, decoded.header.directions);
if (expectedDirections > 1 && totalFrames % expectedDirections == 0)
    directions = expectedDirections;
```

The `expectedDirections` MUST also go into the clip cache key (`|dir={expectedDirections}`)
or a wrong-dir cached clip wins. If a NEW mounted part spins, dump each part's
`directionCount`/`framesPerDirection` via reflection (see verification) before anything else.

### Mount toggle button

`SandboxPlayerController.ToggleMount()` mounts (`defaultHorseId`) if on-foot, dismounts if
mounted. `MountToggleButton.cs` is the HUD button: calls `ToggleMount`, label auto-flips
"Lên Ngựa"/"Xuống Ngựa" via `Update()` reading `Mount.IsMounted`. Built in
`SandboxManager.EnsureMountToggleButton` on the joystick canvas (right side).

**Mount has a 0.5s transition** (`PlayerMountService`: None→Mounting→Mounted, and
Mounted→Dismounting→None). `IsMounted` is true only in the `Mounted` state. When testing
via `SimulateMove`, tick past 0.5s (≥6 × 0.1s) before asserting `Mounted` — one tick still
reads `Mounting`. `SimulateMove` does NOT call MonoBehaviour `Update()`, so the button
label won't refresh in a code-only test; call `Update` manually or trust real frames.

## Workflow

For a new avatar / equipment swap / new action:

1. **Identify the part set** from `人物类型.txt` + the variant numbers wanted. Note the
   art folder (`man` vs `woman`) and the action codes needed (ST, RN, ...).
2. **Stage the SPRs** (`scripts/stage_player_spr.py`) and refresh Unity.
3. **Wire the catalog** — add the part->SPR rows for each action. Reuse `SortingOffset`
   (draw-order is shared). For female, see `references/extending.md`.
4. **Compile + check console** for the per-part `Loaded ... N frames, 8 dirs` logs.
5. **Verify in Play mode** with the runtime checks below.
6. **Tests**: extend `MalePlayerVisualTests` (or add a parallel test) for the new set.
7. Update `CHANGELOG.md` and the harness story.

## Verification (how to actually prove it works)

Screenshots alone are unreliable here (dense map, hard to eyeball). Use `execute_code`
for ground truth — this is what caught both bugs:

- **Parts loaded**: find the player GameObject, `GetComponentsInChildren<SpriteRenderer>`,
  assert `withSprite == partCount` (NOT zero — zero = Bug 2).
- **Above the map**: compare player `sortingOrder` range vs map art; player base must be
  `MapRenderer.PlayerSortingOrder` (5000), NOT the old 32000 clamp. Depth vs nearby
  sprites is the camera CustomAxis sort, not the order value.
- **Actually visible (A/B diff)**: render the scene to a `RenderTexture` twice — once
  with player `SpriteRenderer`s enabled, once disabled — and diff. Non-zero diff pixels
  = avatar draws on top, not occluded. Occlusion bugs show diff ~ 0 even when sprites
  are assigned. A clean diff bbox shaped like a tall rider silhouette (e.g. 13×36) =
  the avatar adds only itself; a wide filled box = a real overlay quad.
- **8-way move**: drive `SetMoveInput` with the 8 unit vectors, step `SimulateMove`,
  assert `direction` maps E6/NE5/N4/NW3/W2/SW1/S0/SE7 and action toggles Move/Idle.
- **Mounted dir lock**: while mounted + idle, step several `SimulateMove` and assert
  `GetCurrentDirection()` stays constant AND each horse part's clip is `directionCount=8`
  (reflect `_parts`). A horse clip reporting `directionCount=1` is Bug 3.

`scripts/verify_player.cs` is a ready-to-paste `execute_code` body for all six checks
(parts loaded, sorting model, A/B visibility, 8-way move, mounted dir-lock, joystick raycast).

## When the bug is UI, not the avatar (don't waste hours on the player)

This session burned a long time chasing a "dark blurry square / lớp phủ mờ on the
player" that turned out to be NOTHING to do with the avatar. Two separate root causes,
both UI/camera. Recognize these patterns fast:

### "Translucent box on the player" that isn't there in any player render

Symptom: user sees a dim square at the player; every player-isolation render is clean
(magenta-bg render = 0 haze, A/B diff = clean rider silhouette). The gap: `cam.Render()`
to a RenderTexture does NOT capture **ScreenSpaceOverlay** UI, but the composited game
view (ScreenCapture path) and the user's eyes DO. So a UI panel stuck at screen center
looks painted on the player (camera follows player → player always centered).

Root cause here: a panel host GameObject was parented to the canvas with **no
RectTransform setup** → defaulted to 0-size at canvas center → its children (anchored to a
fraction of the host) collapsed to the center too. Fix = force the host RectTransform
full-stretch (`anchorMin=0, anchorMax=1, offsets=0`) before building children.

Diagnostic that finally nailed it: enumerate `UnityEngine.UI.Graphic` with
`raycastTarget`/dark color, `GetWorldCorners`, and flag any that `coversCenter`. Also a
bounds-overlap scan (NOT `transform.position` distance — corner-pivot sprites can overlap
the player while their pivot sits far away).

### "Zoom makes the player a tiny blurry square"

`SandboxPlayerController.followOrthoSize` too large (was 480) renders the player ~34px on
a 360px screen = a blurry dark blob that reads as a "box". 300 is the balanced default
(player ~116px, still some map context). 160 is too close. This is a camera setting, not
art.

### Joystick / button does nothing (input eaten by overlapping UI)

Symptom: joystick visible but player won't move; the signal path works when you call
`joystick.onMove.Invoke(...)` directly. Root cause: another canvas with **higher
`sortingOrder`** has a `raycastTarget` Graphic overlapping the joystick, so its
`GraphicRaycaster` wins the touch. Here the chat panel (PanelCanvas order=200) sat on top
of the joystick (SandboxCanvas order=0) after the chat was moved to the bottom-left.

Diagnostic: build a `PointerEventData` at the joystick center and
`EventSystem.current.RaycastAll` — the TOP hit must be the joystick, not a panel. Fix:
set `raycastTarget=false` on the decorative backdrop/text (keep it on real buttons/inputs
so they still click). Touches then fall through to the joystick.

## Unity MCP quirks (this project)

- Instance `vltk-mobile@...`, Unity 6000.4.7f1, LinuxEditor, scene `Assets/Scenes/Sandbox.unity`.
- Action tools can deregister during `playmode_transition`. The resource
  `mcpforunity://editor/state` always reads — wait for `ready_for_tools==true` before
  calling action tools. `is_changing` may sit true persistently; if `ready_for_tools`
  is true, `execute_code` still works.
- After editing a script: `stop` play -> `refresh_unity(compile=request, force, scripts)`
  -> poll state -> `read_console` -> `play`. Editing while playing does NOT recompile.
- Fallback runtime evidence: `~/.config/unity3d/Editor.log` (grep `[MalePlayer]`/`[Sandbox]`).

## Pointers

- `references/extending.md` — generalize male classes to female / equipment / mount.
- `references/draw-order.md` — the full Dir1..Dir8 part-id tables + how `SortingOffset` reads them.
- `scripts/uid.py` — PC signed-byte uid by default (matches C# `ComputePathUid`); `--unsigned` for the legacy staged-naming variant.
- `scripts/stage_player_spr.py` — stage a folder of part SPRs + update manifest.
- `scripts/verify_player.cs` — execute_code body for the 6 verification checks.
