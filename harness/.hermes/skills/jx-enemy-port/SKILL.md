---
name: jx-enemy-port
description: >-
  Port, fix, or verify JX Online 1 / Võ Lâm Truyền Kỳ map enemies and trainer NPC/object spawns in the VLTK-mobile Unity client using PC Region_S data, NpcS.txt templates, real SPR visuals, 8-way animation, reusable player-style shadows, HP/nameplate overlays, default spawn checks, and Unity runtime validation. Use whenever the user asks for map enemies/mobs/monsters/NPC/object spawns, PC-accurate coordinates, võ sư/cọc gỗ/mộc nhân/bao cát, Region_S.dat, NpcS.txt, enemy visual placeholders, wrong enemy names, missing enemy shadows/đổ bóng, 8-direction enemy animation, HP bars/nameplates, ngũ hành labels, or says enemies must look/behave like PC.
---

# JX Enemy Port Pattern

Use this skill to port or fix enemies and PC trainer/object spawns for a map using the same pattern proven on **Ba Lăng huyện / 巴陵县 (Map_79)**. Goal: PC-derived data, PC coordinates, real SPR visuals when available, reusable player-style enemy shadows, readable PC-style overhead name/HP UI, and deterministic Unity validation.

## Reference files

- [`references/gameplay-combat-bridge.md`](references/gameplay-combat-bridge.md) — Full session evidence: bridge pattern (both EnemyRuntime→GameplayLoop AND TriggerSkillSlot→GameplayLoop), field name traps, HP scale mismatch fix, Thread.Sleep coroutine trap, verified combat results (skill slot kills → EXP/silver), debug probe code, Cái Bang skill reference table.

## Core rule

Enemies and trainer/object spawns come from **PC server `Region_S.dat` + `NpcS.txt`**, not
guesses. Authoritative chain: `Region_S.dat` (spawn template id + MPS coordinates + per-spawn
series) → `NpcS.txt` row (name, `NpcResType`, `Life`, AI fields) → staged real SPR visual.
Preserve PC coordinates and template ids exactly; localize names to Vietnamese; never invent
a sprite or a spawn position. If the exact visual asset is missing, place an invisible PC-coord
marker rather than a placeholder, or skip and report — do not show fake art as final output.

## Resource/hash guard learned from combat visual port

Before concluding that any PC SPR/icon/effect/NPC/HUD asset is missing, apply `jx-pc-port-rule` → **PC resource resolution doctrine**:

- Read PC TXT/INI tables with the correct encoding. Paths with Chinese resource folders are usually GB2312/GBK; mojibake paths hash to fake UIDs.
- PAK entries named `unknown/<uid>.spr` are valid extracted PC assets, not garbage.
- For PAK lookup use PC signed-byte FileNameHash, not an unsigned-byte/private runtime hash.
- Copy exact PC assets into `Assets/StreamingAssets/...`; never load directly from `/var/www/jx-source` at runtime.
- Verify with real file existence/decode/render evidence before claiming parity or missing source.

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
| `Assets/Scripts/Sandbox/EnemyAiService.cs` | Reusable AI service: wander/engagement radius logic. |
| `Assets/Scripts/Sandbox/EnemyTemplateFactory.cs` | Factory for enemy templates from PC NpcS.txt data. |

## Source data workflow

### 1. Find PC map spawn files

Use server region data, not client decoration data:

- Correct: `Assets/StreamingAssets/TestData/Regions/Map_<id>/*_Region_S.dat`
- Usually wrong for real enemies: `Map_<id>_C/*_Region_C.dat` (client-side critters/decor/NPCs only)

If `Region_S` is not staged, search/extract from PC server map folder:

- PC reference repo: `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/maps/...`
- Ba Lăng example path decoded from GBK: `maps/两湖区/巴陵县/...`

### 2. Parse `Region_S.dat`

PC format references:

- `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/SwordOnline/Sources/Core/Src/Scene/SceneDataDef.h`
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

- `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/Settings/NpcS.txt`
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

## Combat/GameplayLoop bridge — connecting visual enemies to combat engine

This is a critical architectural gap: `MapEnemySpawnRuntime` spawns 812 visual enemies on the scene but `GameplayLoopService` knows nothing about them until you explicitly bridge them.

### The problem

`MapEnemySpawnRuntime.SpawnForMap()` creates GameObjects with SPR visuals, AI, nameplates etc.
`GameplayLoopService` maintains its own `_actors` dictionary and `_enemies` list used by combat.
These two systems are **completely parallel** — visual enemies are NOT in the combat engine by default.

`gl.Enemies.Count == 0` even when 812 monsters are visible on screen.

### The fix: bridge in `SpawnEnemiesForActiveMap()`

After `EnemyRuntime.SpawnForMap()` completes, loop through `EnemyRuntime.Entries` and register each in GameplayLoop:

```csharp
private void SpawnEnemiesForActiveMap()
{
    if (EnemyRuntime == null || MapManager?.ActiveMap == null) return;
    var regionSFolder = ResolveRegionSFolderForActiveMap();
    EnemyRuntime.SpawnForMap(MapManager.ActiveMapId, regionSFolder);

    // Bridge spawned enemies into GameplayLoop for combat/AI
    if (GameplayLoop != null)
    {
        int bridged = 0;
        foreach (var entry in EnemyRuntime.Entries)
        {
            int templateId = entry.template?.templateId ?? 0;   // NOT .id
            string nameVi  = entry.template?.DisplayName ?? "Quái";  // NOT .nameVi
            int level      = Mathf.Max(1, entry.level);
            var pos        = entry.worldPosition;
            int actorId    = 10000 + entry.instanceId;  // offset to avoid collision with player (id=1)
            GameplayLoop.RegisterEnemy(actorId, nameVi, templateId, level, pos);
            bridged++;
        }
        SubsystemLog.Info("MapEnemy", $"GameplayLoop: bridged {bridged} enemies từ EnemyRuntime.");
    }
}
```

**Key field name traps:**
- `NpcTemplate` uses `templateId` (NOT `id`) and `DisplayName` property (NOT `nameVi` field)
- `BaLangNpcEntry` has `worldPosition`, `level`, `instanceId`, `template`, `series`, `facing`

### Player position sync — range check requires real-time position

`GameplayLoopService` stores `_player.combat.position` but this is set once at spawn and never updated. All skill range checks use `combat.position`, so player can't hit anything unless they're still at the spawn point.

**Fix: sync every frame in `SandboxManager.Update()`:**

```csharp
// Right before GameplayLoop.Tick():
if (GameplayLoop?.Player != null && PlayerController != null)
{
    var wpos = (Vector2)PlayerController.transform.position;
    GameplayLoop.Player.worldPos = wpos;
    GameplayLoop.Player.combat.position = wpos;
}
GameplayLoop?.Tick(Time.deltaTime);
```

When teleporting player via `execute_code` for combat tests, also manually sync:
```csharp
sm.PlayerController?.PlaceAt(newPos, snapCamera: false);
var player = gl.Player;
if (player != null) { player.worldPos = newPos; player.combat.position = newPos; }
// THEN cast skill immediately — don't wait for next frame
```

### Skill selection for Cái Bang

Skill catalog has 511 entries but not all are attack skills. Key lookup:
- Skill ids 115/116 = passive stance skills (`skillStyle=PassivityNpcState`, `targetEnemy=False`) — cast succeeds but `damageResults=0`
- Skill 117 = "Ném Đá Hỏi Đường" (`targetEnemy=True`, `skillStyle=Missiles`, `attackRadius=384`) — first real attack skill
- `success=False, reason=SkillNotKnown` → skill id not in player's `knownSkills`
- `success=False, reason=OutOfRange` → player too far, need dist < attackRadius (384 for sk117)
- `success=False, reason=OnCooldown` → wait or `gl.Combat?.AdvanceTime(100)` to skip
- `success=True, dmgResults=0` → skill cast OK but no damage (passive/buff/self-target)

### TriggerSkillSlot → GameplayLoop HP bridge

`CombatSkillSlotController.TriggerSkillSlot` is a third parallel system with its own HP. When a skill button is tapped:

1. `TriggerSkillSlot` → `CombatRuntime.Cast` → `CombatCastReport`
2. A coroutine `ApplyLiveEnemyHpAtImpact` fires after FX impact
3. `target.enemyBehaviour.SetLife(hp)` updates visual HP only
4. `GameplayActor.combat.currentLife` is untouched → EXP/silver/respawn never fire

**actorId mapping:** `GameplayActor.actorId = 10000 + EnemyRuntimeInfo.enemyId` (where `enemyId == BaLangNpcEntry.instanceId`)

**Fix:** in `ApplyLiveEnemyHpAtImpact`, after `SetLife(hp)`:

```csharp
var glEnemy = SandboxManager.Instance?.GameplayLoop?.GetActor(10000 + target.enemyId);
if (glEnemy != null && !glEnemy.isDead)
{
    int visualDmg = target.maxLife > 0 ? target.currentLife - hp : 0;
    if (visualDmg > 0)
    {
        int glDmg = target.maxLife > 0
            ? Mathf.RoundToInt((float)visualDmg / target.maxLife * glEnemy.combat.maxLife)
            : visualDmg;
        glEnemy.combat.currentLife = Mathf.Max(0, glEnemy.combat.currentLife - glDmg);
        if (glEnemy.combat.currentLife <= 0)
            SandboxManager.Instance?.GameplayLoop?.ProcessActorDeathPublic(
                glEnemy, SandboxManager.Instance?.GameplayLoop?.Player);
    }
}
```

Also expose `ProcessActorDeath` as public wrapper in `GameplayLoopService`:
```csharp
public void ProcessActorDeathPublic(GameplayActor victim, GameplayActor killer)
    => ProcessActorDeath(victim, killer);
```

**HP scale:** visual MaxLife ≠ GL MaxLife. Use ratio: `glDmg = visualDmg / visualMaxLife * glMaxLife`. Never directly assign.

**Verified:** TriggerSkillSlot(2, 117) on Hươu đốm → visual HP: 100→0, GL HP: 140→0, silver +25, EXP +250, Lv1→Lv2.



```
PlayerCastSkill(117, meoVangId) → success=True, hp 280→180, dmg=100
Kill 5/5: Mèo vàng (hp=280), Heo trắng (hp=160/240), Hươu đốm (hp=120)
Post-kill economy: silver=1150, EXP=940, level up to Lv3
Respawn: 812/812 live after 30s (respawnDelay field on GameplayActor)
```

### Debug probe pattern for combat verification

```csharp
// Paste into execute_code:
var sm = VLTK.Sandbox.SandboxManager.Instance;
var gl = sm?.GameplayLoop;
var player = gl?.Player;
var playerPos = sm.PlayerController != null ? (Vector2)sm.PlayerController.transform.position : Vector2.zero;
// Sync position manually for test:
if (player != null) { player.worldPos = playerPos; player.combat.position = playerPos; }
var nearest = gl.FindNearestEnemy(playerPos, 9999f);
int hpB = nearest?.combat?.currentLife ?? 0;
var r = gl.PlayerCastSkill(117, nearest?.actorId ?? 0);
int hpA = nearest?.combat?.currentLife ?? 0;
return $"enemies={gl.Enemies.Count} fightMode={player?.combat?.fightMode} " +
       $"skill117: success={r?.success} reason={r?.reason} " +
       $"{nearest?.nameVi} hp={hpB}→{hpA} dmg={hpB-hpA} dead={nearest?.isDead}\n" +
       gl.GetStatusSummary();
```

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
- **Visual enemies ≠ combat enemies.** `MapEnemySpawnRuntime` and `GameplayLoopService` are completely separate. Always bridge them in `SpawnEnemiesForActiveMap()` or `Enemies.Count` stays 0 even with 812 visible monsters.
- **`NpcTemplate.id` doesn't exist** — use `templateId`. `nameVi` doesn't exist — use `DisplayName`.
- **`player.combat.position` never updates automatically.** Must sync from `PlayerController.transform.position` every frame in `Update()`, otherwise all skill range checks fail even at dist=150.
- **Recompile during PlayMode = broken state.** Domain reload destroys singletons. `SandboxManager.Instance` returns null even though FindObjectsByType finds it. Always Stop → patch → play fresh.
- **Skill 0 / non-Cái Bang skill ids** always fail with `SkillNotKnown` or `FactionMismatch`. Check `player.combat.knownSkills` list and use actual faction skill ids (Cái Bang starts at 115).
- `PlaceAt()` + `PlayerCastSkill()` in the same frame: `combat.position` hasn't synced yet (sync happens next `Update()`). Manually set `player.worldPos` and `player.combat.position` after `PlaceAt()` when testing from `execute_code`.
- **`TriggerSkillSlot` and `GameplayLoopService` are ALSO separate HP systems.** `CombatSkillSlotController.TriggerSkillSlot` uses its own `CombatRuntime.Cast` and applies damage to visual `BaLangNpcEntry` HP — but `GameplayActor.combat.currentLife` is untouched. Kill a visual enemy and EXP/silver don't fire. Fix: in `ApplyLiveEnemyHpAtImpact`, look up `GameplayLoop.GetActor(10000 + target.enemyId)` and apply ratio-scaled damage. actorId mapping: `GameplayActor.actorId = 10000 + BaLangNpcEntry.instanceId = 10000 + EnemyRuntimeInfo.enemyId`.
- **HP scale mismatch between visual and GL.** Visual `maxLife` ≠ GL `maxLife` (different formulas). Never directly assign `gl.combat.currentLife = visualHp`. Use: `glDmg = Mathf.RoundToInt((float)visualDmg / visualMaxLife * glMaxLife)`.
- **`Thread.Sleep` blocks Unity coroutines.** `ApplyLiveEnemyHpAtImpact` is a Coroutine. Calling `Thread.Sleep(1000)` in `execute_code` freezes the main thread — coroutines cannot tick, so HP reads immediately after sleep still show the old value. Use two separate `execute_code` calls instead: trigger in call 1, read results in call 2.
- **`EnemyRuntime.GetActiveEnemies()` allocates a new `List<>` every call.** Don't call it per-frame in `Update()`. Use `EnemyRuntime.Entries` (returns `IReadOnlyList<BaLangNpcEntry>`) for no-alloc iteration, or call `GetActiveEnemies()` only when needed (test probes, one-off queries).
