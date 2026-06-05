---
name: jx-player-visual
description: >-
  Build, fix, or extend the on-screen PLAYER AVATAR (and player-like NPC) of the
  VLTK-mobile Unity client using the original JX Online 1 / Võ Lâm Truyền Kỳ layered
  SPR character system. Use this skill WHENEVER the user works on a player/character
  VISUAL — e.g. "port the female player", "add the woman avatar", "đổi giáp/vũ khí/mũ
  cho nhân vật", "thêm cưỡi ngựa", "nhân vật bị vô hình / vẽ dưới map", "animation sai
  hướng", "thêm action chém/đứng/chạy", "render player nữ", "spawn player vào map",
  or mentions MA_*/WO_* SPR parts, 男主角/女主角贴图顺序表, npcres\man / npcres\woman,
  body/head/hair/hand/weapon layers, 8-way direction, or MalePlayerVisual /
  MalePlayerSpriteCatalog / SandboxPlayerController. This skill encodes the hard-won
  layered-part model, the signed/unsigned hash split, the SPR staging pipeline, and
  the two visibility bugs (sorting ceiling + static-cache-on-replay) already solved —
  reuse it instead of re-deriving.
---

# JX Player Visual (layered SPR avatar)

Render a JX/VLTK player character in the Unity mobile sandbox the way the original PC
client does: **many SPR parts stacked as separate `SpriteRenderer`s** (shadow, body,
head, hair, hands, weapons, horse), animated per-action, ordered per-direction by the
PC draw-order table. Male is the reference implementation; the same model covers the
female avatar and any equipment/mount swap.

## Mental model (read first)

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
| `Assets/Scripts/Sandbox/SandboxPlayerController.cs` | Joystick + keyboard input -> world move + camera follow; forwards move vector to the visual. |
| `Assets/Scripts/Sandbox/SandboxManager.cs` | Auto-spawns player + joystick + camera on map load; `PlacePlayerOnActiveMap()` centers on `MapRenderer.ContentBounds`. |
| `Assets/Scripts/Sprites/SprRuntimeService.cs` | `ComputePathUidHex` (UNSIGNED variant) — runtime SPR file naming. |
| `Assets/Scripts/Sprites/SprDecoder.cs` | SPR -> texture/frames/offsets. |
| `Assets/StreamingAssets/male_player_sprites.json` | Manifest: name -> uid -> staged file. |
| `Assets/Tests/EditMode/Sandbox/MalePlayerVisualTests.cs` | Catalog/direction/load/move tests. |

To add a NEW avatar (female, alt class) the cleanest path is to **generalize the male
classes by gender/variant** rather than copy-paste. See `references/extending.md`.

## Source data (jxwin-kinnox PC client)

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
| HT/HB/HH | hat/headgear variants | shoulder/headwear region |
| BD | body/armor (躯体) | 5 |
| LH / RH | left / right hand | 6 / 7 |
| LW / RW | left / right weapon | 8 / 9 |
| (horse) | mount front/mid/rear | 12 / 13 / 14 |

`VARIANT` is the equipment id: body/head `019`, empty-hand weapon `000`, shadow `999`.
Swapping armor/weapon = swap the variant number for that part only.

### Action codes (`<ACTION>` in the filename)

| code | action | frames (male 019) |
|------|--------|-------------------|
| ST | stand / idle | 120 = 15/dir x 8 |
| RN | run / move | 88 = 11/dir x 8 |
| RD | ride (mount) | varies |
| ZZ | special/emote | varies |

Always derive `framesPerDirection = totalFrames / directions` at load time — do not
hard-code 11 or 15; different parts/actions differ.

## The two hashes (do not mix them up)

There are TWO different path-hash functions in this project. Mixing them = 0 matches
or wrong files. Both lowercase ASCII `A-Z` and run the same `value` recurrence; they
differ ONLY in how each path byte is treated:

1. **Pak lookup hash (`g_FileName2Id`, SIGNED byte).** Used to find an entry inside
   `maps.pak` / `spr.pak`. High bytes (Chinese GBK, >=0x80) are treated as signed
   (`b - 256`). This is the `jx-map-port` skill's hash. Use it ONLY to read from paks.
2. **Runtime file-naming hash (`ComputePathUid`, UNSIGNED byte).** Used by
   `SprRuntimeService.ComputePathUidHex` to name staged files `{uid}.spr`. ASCII-only
   player paths (`spr\npcres\man\MA_BD_019_ST01.spr`) contain no high bytes, so signed
   vs unsigned is irrelevant here — but the staging script MUST use the SAME unsigned
   function as the C# runtime so the names line up.

Verified: `spr\npcres\man\MA_BD_019_ST01.spr` -> unsigned uid `45488ea8`, which matches
the manifest and the file the runtime loads. `scripts/uid.py` is the reference impl.

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

### Bug 1: player drawn UNDER the map

Map ground-cover/builtin sprites (`MapRenderer.cs`) clamp `sortingOrder` to a ceiling of
**32000**. A naive screen-Y order for the player lands below that in dense town centers,
so the map paints over the avatar. Fix: the player's base order is forced above the map
ceiling.

```
int screenOrder = Mathf.RoundToInt(-transform.position.y) * 2 + 2;
return Mathf.Clamp(Mathf.Max(screenOrder, 32200), 32200, 32700);
```

Per-part `SortingOffset` (0..~14) is added on top, so the whole avatar sits in
[32200..~32714], always above the map. Used by both `ApplyFrame` and `ApplySorting`.

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
- **Above the map**: compare player `sortingOrder` range vs `MapRenderer` max order;
  player min must exceed 32000.
- **Actually visible (A/B diff)**: render the scene to a `RenderTexture` twice — once
  with player `SpriteRenderer`s enabled, once disabled — and diff. Non-zero diff pixels
  = avatar draws on top, not occluded. Occlusion bugs show diff ~ 0 even when sprites
  are assigned.
- **8-way move**: drive `SetMoveInput` with the 8 unit vectors, step `SimulateMove`,
  assert `direction` maps E6/NE5/N4/NW3/W2/SW1/S0/SE7 and action toggles Move/Idle.

`scripts/verify_player.cs` is a ready-to-paste `execute_code` body for all four checks.

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
- `scripts/uid.py` — unsigned runtime uid (matches C#).
- `scripts/stage_player_spr.py` — stage a folder of part SPRs + update manifest.
- `scripts/verify_player.cs` — execute_code body for the 4 verification checks.
