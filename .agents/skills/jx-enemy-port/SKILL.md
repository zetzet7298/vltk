---
name: jx-enemy-port
version: 1.2.0
description: >-
  Port, fix, or verify JX Online 1 / Võ Lâm Truyền Kỳ map enemies and trainer NPC/object spawns in the VLTK-mobile Unity client using PC Region_S data, NpcS.txt templates, real SPR visuals, 8-way animation, reusable player-style shadows, HP/nameplate overlays, default spawn checks, and Unity runtime validation. Use whenever the user asks for map enemies/mobs/monsters/NPC/object spawns, PC-accurate coordinates, võ sư/cọc gỗ/mộc nhân/bao cát, Region_S.dat, NpcS.txt, enemy visual placeholders, wrong enemy names, missing enemy shadows/đổ bóng, 8-direction enemy animation, HP bars/nameplates, ngũ hành labels, or says enemies must look/behave like PC. Also trigger this skill when handling special decoding issues like upside-down training NPCs (vertical row flipping, bottom-left (0,0) pivot in PcNpcVisual) or size table decompression (1-byte header shift, non-monotonic offsets) in SprDecoder.
---

# JX Enemy Port Pattern

Use this skill to port or fix enemies and PC trainer/object spawns for a map using the same pattern proven on **Ba Lăng huyện / 巴陵县 (Map_79)**. Goal: PC-derived data, PC coordinates, real SPR visuals when available, reusable player-style enemy shadows, readable PC-style overhead name/HP UI, and deterministic Unity validation.

## Core rule

Prefer PC data over guesses. Do not invent sprites or spawn positions if the PC source is available. If art/data is missing, skip or report that exact missing asset rather than showing placeholders as final output.

## Key files in current implementation

| File | Role |
| --- | --- |
| `Assets/Scripts/Sandbox/BaLangEnemyRegionScanner.cs` | Parses PC `Region_S.dat` NPC section (`KSPNpc`) and extracts map spawn entries. |
| `Assets/Scripts/Sandbox/BaLangEnemyDatabase.cs` | Map-specific template DB: template id → Vietnamese name, PC res type, AI fields, MPS→world transform. |
| `Assets/Scripts/Sandbox/BaLangEnemyRuntime.cs` | Runtime spawn setup, trainer markers, AI, HP state, nameplate anchor, scene object construction. |
| `Assets/Scripts/Sandbox/BaLangPcSpawnMarker.cs` | PC Region_S marker for NPC/object entries whose exact visual asset is missing; preserves authoritative coordinates without placeholders. |
| `Assets/Scripts/Sandbox/PcNpcVisual.cs` | PC NPC SPR renderer: staged SPR → uid lookup → `SprDecoder` → 8-direction frames + reusable player-style shadow layer. |
| `Assets/Scripts/Sandbox/BaLangEnemyNameplateOverlay.cs` | Screen-space PC-style name/HP overlay anchored to sprite head. |
| `Assets/Scripts/Sandbox/SandboxManager.cs` | Calls enemy spawn runtime for active map. |
| `Assets/Tests/EditMode/Sandbox/BaLangEnemyTests.cs` | Parser/database/nameplate/AI validation. |
| `Assets/StreamingAssets/Sprites/{uid}.spr` | Staged runtime PC SPRs, named by `SprRuntimeService.ComputePathUidHex`. |
| `Assets/StreamingAssets/TestData/Regions/Map_XX/` | Server-side `*_Region_S.dat` files, authoritative map enemy spawns. |

## Source data workflow

### 1. Find PC map spawn files

Use server region data, not client decoration data:

- Correct: `Assets/StreamingAssets/TestData/Regions/Map_<id>/*_Region_S.dat`
- Usually wrong for real enemies: `Map_<id>_C/*_Region_C.dat` (client-side critters/decor/NPCs only)

If `Region_S` is not staged, search/extract from PC server map folder:

- PC reference repo: `jxwin-kinnox/SourceNew/swrod3/bin/Server/maps/...`
- Ba Lăng example path decoded from GBK: `maps/两湖区/巴陵县/...`

### 2. Parse `Region_S.dat`

PC format references:

- `jxwin-kinnox/SourceNew/swrod3/SwordOnline/Sources/Core/Src/Scene/SceneDataDef.h`
- `KRegion::LoadServerNpc()` in `KRegion.cpp`
- `KNpcSet::Add(int nSubWorld, void* pNpcInfo)` in `KNpcSet.cpp`

Combined file format:

1. `DWORD sectionCount`
2. `sectionCount * KCombinFileSection { uint offset, uint length }`
3. section data, offsets relative to data start
4. NPC section index = `2` (`REGION_NPC_FILE_INDEX`)
5. NPC section contains `KNpcFileHead` then variable-length `KSPNpc` entries

`KSPNpc` fixed prefix is 60 bytes:

```text
int templateId
int nPositionX   // PC MPS X
int nPositionY   // PC MPS Y
bool specialNpc
char reserved[3]
char name[32]    // GBK
short level
short curFrame   // facing/start frame
short headImageNo
short kind        // 0 = normal/enemy/animal, 3 = town NPC
byte camp
byte series       // ngũ hành per spawn
ushort scriptNameLen
char script[scriptNameLen]
```

Important: `nPositionX/Y` are **MPS** coordinates. PC path: `KRegion::LoadServerNpc()` → `NpcSet.Add(nSubWorld, &sNpcCell)` → `KSubWorld::Mps2Map()`.

### 3. Map template ids to `NpcS.txt`

Use `NpcS.txt` as authoritative for stats, AI, and visual resource:

- `jxwin-kinnox/SourceNew/swrod3/bin/Server/Settings/NpcS.txt`
- Has header row, so current Ba Lăng fix used `rowIndex = templateId + 1`.
- Confirm by matching spawn `nameRaw` from `Region_S` against `NpcS.txt` row name.

Fields used by current code:

| Field | Meaning |
| --- | --- |
| `Name` | PC raw name, translate to Vietnamese. |
| `Kind` | enemy/NPC type. |
| `Series` | template default ngũ hành; Region_S per-spawn `series` can override display. |
| `NpcResType` | SPR resource folder/key, e.g. `ani063`. |
| `Life` | max HP baseline. |
| `WalkSpeed`, `RunSpeed` | AI/runtime movement. |
| `VisionRadius`, `ActiveRadius` | wander/engagement radius. |
| `AIMode`, `AIParam1..9` | PC AI behavior inputs. |

Ba Lăng example:

| Template | PC name | Resource | Vietnamese | Count |
| --- | --- | --- | --- | --- |
| `31` | `金猫` | `ani049` | `Mèo vàng` | 102 |
| `42` | `梅花鹿` | `ani061` | `Hươu đốm` | 193 |
| `43` | `白猪` | `ani063` | `Heo trắng` | 189 |

Training objects and trainer NPCs are PC `Region_S` entries too. For Ba Lăng:

| Template | PC name | Resource | Vietnamese | Count | Handling |
| --- | --- | --- | --- | ---: | --- |
| `311` | `武师` | `passerby097` | `Võ sư` | 1 | marker + default player spawn coordinate; render only when exact visual asset is staged |
| `413` | `木桩` | `enemy178` | `Cọc gỗ` | 10 | marker until exact stand visual asset is staged |
| `414` | `木人` | `enemy179` | `Mộc nhân` | 10 | marker until exact stand visual asset is staged |
| `415` | `沙袋` | `enemy180` | `Bao cát` | 10 | marker until exact stand visual asset is staged |

When exact visual assets are missing, create `BaLangPcSpawnMarker` objects at PC coordinates without any renderer. This makes coordinates auditable and usable (for default player spawn, quests, interactions later) while honoring the no-placeholder rule.

For Ba Lăng default play-start spawn, use the PC trainer coordinate:

```text
Region_S: template=311 name=武师 mps=(53493, 95313)
Unity world via MpsToWorld: (53493, -47697)
```

If player does not spawn there after stop/play, check for Fast Enter Play state leaks:

- reset `SandboxManager.Instance` in `RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)`
- clear player movement state, target, joystick state, and last delta before `PlaceAt()`
- probe `playerDist` against `(53493,-47697)` after entering Play Mode

## Coordinate conversion pattern

Do not place enemies by region file origin unless Region_S has local coords only. For server enemies, use MPS.

Current MapRenderer uses:

- X region stride: `512`
- Y render stride: `512`
- PC MPS Y region stride: `1024` because PC region grid is `16 x 32` cells, `32px` per cell

Current conversion in `BaLangEnemyDatabase.MpsToWorld()`:

```csharp
int regionRow = mpsY / 1024;
float worldX = mpsX;
float worldY = -(mpsY - regionRow * 512);
```

Before applying this to another map, verify against that map's renderer/bounds and a known PC coordinate. Do not change root coordinates after spawn; attach visuals/nameplates as children or overlays.

## Runtime setup pattern

Per map, create a small DB class first (Ba Lăng example: `BaLangEnemyDatabase.cs`):

1. `MapId`
2. supported enemy template IDs
3. `CreateTemplates()` from PC `NpcS.txt`
4. Vietnamese names
5. `BuildNpcSprPath(resType, action)`
6. coordinate conversion

Spawn flow:

1. `SandboxManager.SpawnEnemiesForActiveMap()` picks `StreamingAssets/TestData/Regions/Map_<id>`.
2. Runtime scans `Region_S` entries.
3. Filter:
   - `kind == 0`
   - template exists in current map DB
   - real visual SPR staged (or else skip/report; do not final-show placeholder)
4. Create root object at PC world coordinate.
5. Add `PcNpcVisual` for real 8-way SPR.
6. Add `BaLangEnemyAi` using PC AI fields.
7. Add `EnemyHealthBar` data model.
8. Add `EnemyNameplateAnchor` anchored to actual sprite bounds.

## Visual/animation pattern

Use `PcNpcVisual.cs` rather than placeholder `SpriteRenderer`.

Path convention for normal animal/enemy resources:

```csharp
spr\npcres\animal\<resType>\<resType>_wlk.spr
spr\npcres\animal\<resType>\<resType>_st.spr
spr\npcres\enemy\<resType>\<resType>_wlk.spr
```

Current Ba Lăng staged assets use walk clips only:

| Resource | Runtime uid | Source |
| --- | --- | --- |
| `ani049_wlk.spr` | `8cdfcb84.spr` | Mèo vàng |
| `ani061_wlk.spr` | `f4f4d150.spr` | Hươu đốm |
| `ani063_wlk.spr` | `bb10ad7c.spr` | Heo trắng |

Stage SPRs by computing `SprRuntimeService.ComputePathUidHex(sourcePath)` and copying to:

```text
Assets/StreamingAssets/Sprites/{uid}.spr
```

`PcNpcVisual` follows the male player renderer principles:

- Reset static SPR cache via `SubsystemRegistration`.
- Decode with `SprDecoder`.
- Use PC header `directions` and `framesPerDirection`.
- Direction mapping reuses `MalePlayerSpriteCatalog.DirectionFromMove()`:
  - `0=S`, `1=SW`, `2=W`, `3=NW`, `4=N`, `5=NE`, `6=E`, `7=SE`
- Sprite is a child (`NpcSprite`) so frame offsets do **not** move root/spawn position.

## Enemy shadow pattern

Player shadow is not a Unity light/shader shadow. It is a normal PC SPR layer in `MalePlayerSpriteCatalog`:

```text
Idle shadow: spr\npcres\man\MA_YY_999_ST01.spr
Move shadow: spr\npcres\man\MA_YY_999_RN01.spr
```

For enemies, reuse this implementation style instead of inventing a new shadow system:

1. In `PcNpcVisual`, create a child `NpcShadow` beside `NpcSprite`.
2. Load the same staged player shadow SPRs through `SprRuntimeService.ComputePathUidHex()` and `SprDecoder`, using the same cache strategy as NPC body clips.
3. Animate shadow frame by current direction and moving/standing state, matching enemy movement state.
4. Apply SPR offsets to `NpcShadow`, never to the enemy root.
5. Set shadow sorting below enemy body (`PlayerSortingOrder - 20` vs body `PlayerSortingOrder - 10`).
6. Keep player behavior unchanged; this is an extra render layer on enemies only.

Verification probes should include:

```csharp
var visuals = Object.FindObjectsOfType<PcNpcVisual>();
int shadows=0, shadowBelow=0, movingShadow=0, standingShadow=0;
foreach (var v in visuals) {
  var body = v.transform.Find("NpcSprite")?.GetComponent<SpriteRenderer>();
  var shadow = v.transform.Find("NpcShadow")?.GetComponent<SpriteRenderer>();
  if (shadow != null && shadow.sprite != null) shadows++;
  if (body != null && shadow != null && shadow.sprite != null && shadow.sortingOrder < body.sortingOrder) shadowBelow++;
  if (v.HasShadow && v.moving) movingShadow++;
  if (v.HasShadow && !v.moving) standingShadow++;
}
return $"visuals={visuals.Length} shadows={shadows} shadowBelow={shadowBelow} movingShadow={movingShadow} standingShadow={standingShadow}";
```

Expected Ba Lăng result: `visuals=484`, `shadows=484`, `shadowBelow=484`, with both moving and standing shadow counts > 0.

## Nameplate / HP overlay pattern

PC sample style for enemy nameplates:

- Compact white text with black outline.
- Name above enemy head.
- Thin green HP bar directly below name.
- No large background panel.
- No numeric HP in PC-style overlay unless the target UI needs it.
- Keep HP data in `EnemyHealthBar`; render style can hide numeric text.

Current implementation:

- `EnemyNameplateAnchor` computes anchor from `spriteRenderer.bounds.max.y + offset`.
- World data model at `z=-10f` to avoid map/sprite cover.
- `BaLangEnemyNameplateOverlay` is screen-space and uses the same anchor. This is the main readable layer.
- World `TextMesh`/bar renderers are disabled to avoid duplicate/cluttered labels; the data model remains for tests and HP updates.

Checklist:

- Anchor to actual sprite bounds, not root feet.
- Probe `hp.transform.position.y > spriteRenderer.bounds.max.y` for all visible enemies.
- Probe root position does not shift when anchor applies.
- Probe sprite still has 8 directions and assigned sprite.

## AI pattern

Use PC `AIMode`, `WalkSpeed`, and `AIParam` fields. Current sandbox behavior is practical parity, not full combat AI:

- Static/no walk speed: stays still.
- Moving AI: choose deterministic wander targets from AIParam distance/angle values.
- Clamp to `ActiveRadius` around origin.
- Feed movement vector into `PcNpcVisual.SetMoveInput()` so direction changes with movement.

This gives stable tests and visibly correct 8-way facing.

## Verification checklist

Always run these after map enemy work:

1. Compile/import via Unity MCP:
   - `unityMCP_refresh_unity(scope='scripts' or 'all', compile='request')`
   - `unityMCP_read_console(types='["error"]')` → 0 errors
2. EditMode tests:
   - targeted enemy tests for parser/template/nameplate/AI
3. PlayMode probe:
   - active map id
   - live enemy count
   - visual count
   - assigned sprite count
   - direction count = 8
   - shadow count equals visual count when shadow assets are staged
   - shadow sorting below enemy body count equals visual count
   - both moving and standing enemies can show shadows
   - trainer/object marker counts if porting town training area
   - default player spawn distance to PC trainer coordinate if requested
   - nameplates above head count
   - root shift = 0
   - labels have no Chinese chars
4. Screenshot evidence:
   - save to `Assets/Screenshots/<map>-enemy-<purpose>.png`

Useful PlayMode probe shape:

```csharp
var ais = Object.FindObjectsOfType<BaLangEnemyAi>();
int anchored=0, above=0, sprite=0, visual8=0, shadows=0, shadowBelow=0;
float maxRootShift=0f;
foreach (var ai in ais) {
  var anchor = ai.GetComponent<EnemyNameplateAnchor>();
  var visual = ai.GetComponent<PcNpcVisual>();
  var sr = ai.transform.Find("NpcSprite")?.GetComponent<SpriteRenderer>();
  var sh = ai.transform.Find("NpcShadow")?.GetComponent<SpriteRenderer>();
  var hp = ai.GetComponentInChildren<EnemyHealthBar>();
  if (anchor != null) anchored++;
  if (sr != null && sr.sprite != null) sprite++;
  if (visual != null && visual.DirectionCount == 8) visual8++;
  if (sh != null && sh.sprite != null) shadows++;
  if (sr != null && sh != null && sh.sprite != null && sh.sortingOrder < sr.sortingOrder) shadowBelow++;
  if (anchor != null && sr != null && hp != null) {
    var before = ai.transform.position;
    anchor.Apply();
    maxRootShift = Mathf.Max(maxRootShift, Vector3.Distance(before, ai.transform.position));
    if (hp.transform.position.y > sr.bounds.max.y) above++;
  }
}
return $"enemies={ais.Length} anchored={anchored} above={above} sprite={sprite} visual8={visual8} shadows={shadows} shadowBelow={shadowBelow} maxRootShift={maxRootShift:F4}";
```

## Porting another map: minimal steps

1. Stage or locate `Map_<id>/*_Region_S.dat`.
2. Parse all spawns and count template IDs/names.
3. Confirm `templateId + 1` vs `NpcS.txt`; do not assume blindly.
4. Translate enemy names to Vietnamese.
5. Stage real SPRs for each supported enemy (`*_wlk`, ideally `*_st`).
6. If `Region_S` includes trainer/NPC/object entries needed by the task, create marker objects at exact PC coordinates even if visual art is missing; do not render fake placeholders.
7. Create a map DB class following `BaLangEnemyDatabase` or generalize only when the second map proves the abstraction.
8. Wire `SandboxManager`/runtime to use the map DB.
9. Reuse player-style shadow layer for `PcNpcVisual` when shadow SPR assets are staged.
10. Skip unsupported/missing-asset templates rather than showing placeholders.
11. Add/extend EditMode tests.
12. Run compile, tests, PlayMode probe, screenshot.

## Common traps

- Using `Region_C` instead of `Region_S` gives critters/decor, not real PC server enemies.
- Forgetting `NpcS.txt` header row can shift resource mapping by one. Cross-check spawn raw name.
- Applying SPR frame offsets to the root moves the spawn position. Put sprite in a child.
- Rendering nameplate from root places label at feet. Anchor to `spriteRenderer.bounds.max.y`.
- Showing both world TextMesh and screen overlay creates unreadable duplicate labels.
- Leaving placeholders for missing art violates PC fidelity. Stage exact SPR or report missing.
- Creating invisible marker objects is acceptable for authoritative coordinates; make it explicit they are markers, not final visual parity.
- Enemy shadows are SPR layers, not lighting. Reuse player shadow assets and sorting rather than adding Unity lights/shaders.
- Fast Enter Play can preserve static singletons and movement state. Reset statics via `SubsystemRegistration` and clear player input/target before default spawn probes.
- **Adding an extra Y-flip in `PcNpcVisual` for "special" NPC types will render them upside down** — `SprDecoder` already maps PC row 0 to Unity texture top via `rowBase = (h - 1 - row) * w`, so any subsequent flip inverts the image. See "SPR orientation rule" below.

## SPR orientation rule (PC → Unity)

**PC SPR frame storage convention:**

PC stores SPR rows **top-down** (row 0 is the topmost visual row, confirmed by `KDrawSprite` advancing the source pointer past the first `Clipper.top` rows from the start of the buffer — see `jx-source/KDrawSprite.cpp` ~line 1469).

**Unity texture storage convention:**

Unity stores `Texture2D.GetPixels32()`/backing arrays **bottom-up** — array index `(h - 1) * w` is the topmost row in Unity's UV/screen coordinates. Feeding a top-down pixel buffer directly to `SetPixels32()` would render the image upside down.

**Decoder handles this correctly:**

```csharp
// Top-down row order (matching PC source payload ordering decoded to Unity bottom-up SetPixels32 format)
for (int row = 0; row < frame.height; row++)
{
    int rowBase = (frame.height - 1 - row) * frame.width;
    // ... copy pixels to rowBase ...
}
```

After this loop, **PC row 0 (top) ends up at array position `(h-1)*w`, which Unity renders as the topmost visible row.** The decoder output is already "right-side up" for any PC SPR.

**The "training NPC" trap (PcNpcVisual.cs):**

An earlier version of `PcNpcVisual.LoadClip` had a `shouldFlipY` branch for paths containing `enemy178/179/180` (training NPCs: cọc gỗ, mộc nhân, bao cát):

```csharp
// BUG: flipping the already-correct decoder output + setting pivot to (0,0) bottom-left
//   → PC row 0 (top) ended up at Unity BOTTOM → training NPCs rendered upside down.
bool shouldFlipY = path.Contains("enemy178") || path.Contains("enemy179") || path.Contains("enemy180");
if (shouldFlipY) {
    // flip pixel rows: row r → row h-1-r
}
var pivot = shouldFlipY ? new Vector2(0f, 0f) : new Vector2(0f, 1f);
clip.sprites[i] = Sprite.Create(tex, rect, pivot, ...);
```

**Fix:** remove the entire flip branch and use a single standard top-left pivot `(0f, 1f)` for all NPCs. The decoder is already correct; do not second-guess it.

```csharp
// CORRECT: no extra flip, top-left pivot (matches all other NPC types in the project).
clip.sprites[i] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
    new Vector2(0f, 1f), pixelsPerUnit, 0, SpriteMeshType.FullRect);
```

**When the same trap recurs:**

- Symptom: a specific NPC type (or set of `enemyXXX` / `aniXXX` resources) renders upside down while other NPCs render correctly.
- Verify by inspecting `PcNpcVisual.LoadClip` for path-specific flip/rotation logic. If present, remove it.
- Don't "fix" `SprDecoder` to compensate for a `PcNpcVisual` flip — the decoder logic is intentional and shared by all other SPR consumers (player, mount, items).

**Pivot convention reminder:**

For ground-standing NPCs and the player, the standard pivot is `(0, 1)` (top-left of the sprite rect) so the foot of the character is at the world anchor. PC frame `offsetX/offsetY` then shift the sprite relative to that anchor. If a sprite "floats above" or "sinks into" the ground after a port, check pivot first before changing offsets.

## Historical context — the training NPC flip saga

This trap has been re-introduced multiple times. The full commit history (most recent first) tells the story:

| Commit | Date | Change | Effect |
| --- | --- | --- | --- |
| `8f0e681d3` | 2026-06-19 | Removed `shouldFlipY` from `PcNpcVisual` | **Fixed**: training NPCs render right-side up. |
| `2c158959a` | 2026-06-18 14:38 | Changed `SprDecoder` rowBase to `(h-1-row)*w` ("correct JX SPR vertical flip") | Fixed upside-down player/parts. |
| `5d2f74df9` | 2026-06-18 08:09 | Added `shouldFlipY` to `PcNpcVisual` for `enemy178/179/180` paths | **Bug introduced**: combined with `2c158959a`, training NPCs became upside down. |
| `b3830ea92` | 2026-06-18 07:59 | Decoder with `rowBase = row*w` + staged `enemy178/179/180_st.spr` (switched from corpse to stand sprites) | Decoder was wrong at this point — anything rendered upside down. |

**Timeline lesson:** the `shouldFlipY` in `5d2f74df9` was the right *idea* at the time (decoder was indeed upside-down), but a later decoder fix (`2c158959a`) made the PcNpcVisual flip redundant and ultimately wrong. **Two fixes for the same problem in different files cancel each other out.** When debugging upside-down sprites, fix the *root cause* (the decoder or the SPR source itself) and remove all downstream patches.

If you ever find yourself adding a path-specific flip/rotation to `PcNpcVisual`, **first check `git log` on `SprDecoder.cs`**. The decoder has been "fixed" at least twice in the project's history. Adding a downstream patch on top of an evolving decoder is a recipe for stale code.

## Training NPC asset selection (corpse vs stand vs walk)

The training NPC templates map to PC SPRs in `Assets/StreamingAssets/Sprites/`. The project has gone through three asset choices for these:

| Asset | Source | Used in | Visual |
| --- | --- | --- | --- |
| `enemy{178,179,180}_corpse.spr` | `pak_unpacked/Client 6.0/data/spr/spr/obj/corpse/` | early commit `bf677785e` | corpse/destroyed form (lying down) — wrong for live training dummies |
| `enemy{178,179,180}_st.spr` (stand) | PC `npcres\enemy\enemyXXX\` | `b3830ea92` onwards, current | upright stand pose — correct for training dummies |
| `enemy{178,179,180}_wlk.spr` (walk) | PC `npcres\enemy\enemyXXX\` | not currently used | walking pose — wrong, training dummies don't walk |

**Rule:** use the `_st.spr` (stand) variant. The path is constructed in `TrainingNpcSpawner.SpawnSingleNpc()` as:

```csharp
string standPath = $@"spr\npcres\enemy\{clipRef}\{clipRef}_st.spr";
```

The double backslash is a verbatim string literal (escaped backslash for Windows-style PC path). The actual runtime path is `spr\npcres\enemy\enemy178\enemy178_st.spr` etc.

The same path pattern (`{clipRef}_st.spr`) is used by `MapEnemySpawnRuntime.SpawnTrainerMarkers()` for the Ba Lăng training area (templates 413/414/415). Always use the stand variant — never the corpse variant for live training NPCs.

## Visual verification (mandatory for any sprite-related change)

The existing EditMode test `TrainingNpcSpawner_UsesPcStandSpritesForTrainingObjects` only checks SPR path and HP — it does **not** verify visual orientation. The same blind spot exists for `BaLangEnemyTests` (no visual assertions). That means a flipped sprite will pass all tests.

When you change anything in `SprDecoder`, `PcNpcVisual`, or the training NPC staging, do one of these:

1. **PlayMode probe** — Enter Play Mode, navigate to Ba Lăng or any map with `enemy178/179/180` spawns, take a screenshot of at least one of each training NPC variant. Visually confirm head is up, feet are down.
2. **Editor screenshot via Unity MCP** — `unityMCP_manage_camera(action="capture_screenshot")` after positioning camera on the training NPC spawner. Compare against a known-good reference (the original PC client, or a previous good frame in the project's git history).
3. **Pixel-orientation unit test** — for catchable cases, add a test that decodes a known-correct SPR and asserts the top row contains expected opaque pixels (e.g. sample a sprite where the head is on the top row and assert pixel 0,0 is non-transparent). This catches regressions of the decoder flip.

Recommended screenshot path: `Assets/Screenshots/<map>-enemy-<purpose>.png`.

## Trainer NPC (template 311) marker pattern

The Ba Lăng `võ sư` (template 311, `passerby097` resType) is the **default player spawn coordinate** for entering the map. The PC Region_S entry is:

```text
Region_S: template=311 name=武师 mps=(53493, 95313)
Unity world via MpsToWorld: (53493, -47697)
```

When the player enters Ba Lăng in PlayMode, they should spawn at this coordinate. If they don't:

1. Check `SandboxManager.SpawnEnemiesForActiveMap()` is called.
2. Verify `MpsToWorld()` formula: `worldX = mpsX`, `worldY = -(mpsY - regionRow * 512)` with `regionRow = mpsY / 1024`.
3. Reset static singletons in `RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)` — `SandboxManager.Instance`, player movement state, target, joystick state, last delta.
4. Probe `playerDist` against `(53493, -47697)` after `EnterPlayMode` — should be < 1 world unit.

The marker object for template 311 follows the same `BaLangPcSpawnMarker` pattern as missing-asset NPCs (no visual render, just the coordinate). If `passerby097` SPR is later staged, the marker is upgraded to a real `PcNpcVisual`.
