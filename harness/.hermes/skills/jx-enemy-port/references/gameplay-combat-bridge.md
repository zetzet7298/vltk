# Gameplay Combat Bridge — Session Evidence 2026-06-13

## Context

Map 53 (Ba Lăng Huyện), Full profile boot, Unity 6 Editor PlayMode.

## Root problem

After `MapEnemySpawnRuntime.SpawnForMap()` runs:
- Visual: 812 enemies visible on scene ✅
- Combat: `gl.Enemies.Count == 0` ❌

Two systems are completely parallel with no auto-wire.

## Fix 1: EnemyRuntime → GameplayLoop bridge

**File:** `Assets/Scripts/Sandbox/SandboxManager.cs`  
**Method:** `SpawnEnemiesForActiveMap()` — add bridge loop after `EnemyRuntime.SpawnForMap()`.

```csharp
// Bridge spawned enemies into GameplayLoop so combat/AI can interact with them.
if (GameplayLoop != null)
{
    int bridged = 0;
    foreach (var entry in EnemyRuntime.Entries)
    {
        int templateId = entry.template?.templateId ?? 0;
        string nameVi  = entry.template?.DisplayName ?? "Quái";
        int level      = Mathf.Max(1, entry.level);
        var pos        = entry.worldPosition;
        int actorId    = 10000 + entry.instanceId; // offset from player id=1
        GameplayLoop.RegisterEnemy(actorId, nameVi, templateId, level, pos);
        bridged++;
    }
    SubsystemLog.Info("MapEnemy", $"GameplayLoop: bridged {bridged} enemies từ EnemyRuntime.");
}
```

**Also required:** Add position sync in `Update()` before `GameplayLoop.Tick()`:

```csharp
if (GameplayLoop?.Player != null && PlayerController != null)
{
    var wpos = (Vector2)PlayerController.transform.position;
    GameplayLoop.Player.worldPos = wpos;
    GameplayLoop.Player.combat.position = wpos;
}
GameplayLoop?.Tick(Time.deltaTime);
```

## Fix 2: TriggerSkillSlot → GameplayLoop HP bridge

**Problem:** `CombatSkillSlotController.TriggerSkillSlot` applies damage to `BaLangNpcEntry` visual HP via its own `CombatRuntime.Cast` system. `GameplayLoopService` has its own actor HP. These are **two separate HP pools** — killing an enemy visually does NOT trigger EXP/silver/respawn in GameplayLoop.

**Architecture:**  
- `TriggerSkillSlot` → `CombatRuntime.Cast` → `ApplyLiveEnemyHpAtImpact` (Coroutine) → `target.enemyBehaviour.SetLife(hp)`
- `GameplayLoopService` → separate HP on `GameplayActor.combat.currentLife`
- actorId mapping: `GameplayActor.actorId = 10000 + BaLangNpcEntry.instanceId = 10000 + EnemyRuntimeInfo.enemyId`

**Fix in `ApplyLiveEnemyHpAtImpact`** (after `target.enemyBehaviour.SetLife(hp, showDamage: true)`):

```csharp
// Bridge damage into GameplayLoop — ratio-based to account for different HP scales
var glEnemy = SandboxManager.Instance?.GameplayLoop?.GetActor(10000 + target.enemyId);
if (glEnemy != null && !glEnemy.isDead)
{
    // Visual damage = target.currentLife - hp (hp = new remaining visual HP)
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

**Also add `ProcessActorDeathPublic` wrapper to `GameplayLoopService`:**
```csharp
public void ProcessActorDeathPublic(GameplayActor victim, GameplayActor killer)
    => ProcessActorDeath(victim, killer);
```

## Field name traps (verified against source)

| Wrong | Correct | Type |
|-------|---------|------|
| `entry.template.id` | `entry.template.templateId` | `NpcTemplate` |
| `entry.template.nameVi` | `entry.template.DisplayName` | `NpcTemplate` property |
| `gl.Enemies.Count` == 812 | starts at 0 until bridge runs | architecture gap |

## Compile errors encountered

```
error CS1061: 'NpcTemplate' does not contain a definition for 'id'
error CS1061: 'NpcTemplate' does not contain a definition for 'nameVi'
```

Fixed by using correct field names above.

## HP scale mismatch between visual and GL systems

Visual HP (`BaLangEnemyAi.MaxLife`) comes from `NpcTemplate.maxLife` (e.g. 100 for animals).  
GL HP (`GameplayActor.combat.maxLife`) comes from `CalculateMaxLife(level)` formula (e.g. 140 for lv2 Hươu đốm).

These are **different scales**. Do NOT directly assign `gl.combat.currentLife = visualHp`. Use the ratio formula:
```
glDmg = visualDmg / visualMaxLife * glMaxLife
```

## Thread.Sleep trap in execute_code

`Thread.Sleep()` inside `execute_code` **blocks the Unity main thread**, which means Unity Coroutines cannot tick during the sleep. `ApplyLiveEnemyHpAtImpact` is a Coroutine — it won't run while the main thread is sleeping.

**Pattern that doesn't work:**
```csharp
slots.TriggerSkillSlot(2, 117);
System.Threading.Thread.Sleep(1000);  // ← blocks main thread, coroutine doesn't run!
int hpAfter = enemy.currentLife;      // ← still old value
```

**Correct pattern — separate execute_code calls:**
```csharp
// Call 1: trigger
slots.TriggerSkillSlot(2, 117);
return "triggered";

// Call 2 (a few real seconds later): read results
return $"hp={enemy.currentLife} alive={enemy.alive} silver={gl.Economy.Wallet.silver}";
```

Between the two calls, Unity's normal Update/Coroutine loop runs, so FX and HP are applied.

## PlayMode verification results

```
Fix 1 result (EnemyRuntime → GameplayLoop):
  Enemies registered: 812
  synced=True (player.combat.position == PlayerController.transform.position)

Attack test (PlayerCastSkill directly):
  Skill 115 "Cái Bang Bổng Pháp": success=True, dmg=0 (passive skill)
  Skill 116 "Cái Bang Chưởng Pháp": success=True, dmg=0 (passive skill)
  Skill 117 "Ném Đá Hỏi Đường": success=True, hp 280→180, dmg=100 ✅

Kill 5/5 test enemies:
  Mèo vàng: dead=True ✅
  Heo trắng: dead=True ✅ (×2)
  Hươu đốm: dead=True ✅ (×2)

Economy after kills:
  silver=1150, EXP=940, level up Lv1→Lv3
  
Respawn: 812/812 live after 30s (respawnDelay=30f)

Fix 2 result (TriggerSkillSlot bridge):
  [Combat] Cast Ném Đá Hỏi Đường [A-3] → Hươu đốm (dmg=1, pendingHp=0, range=200)
  Visual HP: 100→0, alive=False ✅
  GL HP: 140→0, dead=True ✅ (bridge works across HP scale difference)
  Silver: 1025→1050 (+25 reward) ✅
  EXP: 150→400 (+250), PlayerLv: 1→2 (level up triggered) ✅
```

## Cái Bang skill reference (from catalog, id=skill_id, 511 total)

| id | Name | targetEnemy | attackRadius | style | Notes |
|----|------|-------------|--------------|-------|-------|
| 115 | Cái Bang Bổng Pháp | False | 0 | PassivityNpcState | Passive, no dmg |
| 116 | Cái Bang Chưởng Pháp | False | 0 | PassivityNpcState | Passive, no dmg |
| 117 | Ném Đá Hỏi Đường | True | 384 | Missiles/Single | **First real attack** |
| 118 | Cô Mộc Độn Lôi | False (self) | 400 | Missiles/Surround | AoE self |
| 119–130 | various | mixed | varies | — | Check targetEnemy before using |

**Skill 117 `attackRadius=384` in PC units** — player must be within 384 of target. At spawn, player is at ~53246,-52041 and nearest enemy is at dist=542 (too far). Teleport player to within 300 before testing.

## Debug probe (copy-paste to execute_code — Call 1)

```csharp
if (!UnityEngine.Application.isPlaying) return "NOT_PLAYING";
var sm = VLTK.Sandbox.SandboxManager.Instance;
var gl = sm?.GameplayLoop;
if (gl == null) return "ERROR: GameplayLoop=null";

var playerPos = sm.PlayerController != null 
    ? (Vector2)sm.PlayerController.transform.position 
    : Vector2.zero;

// Find nearest enemy
var nearest = gl.FindNearestEnemy(playerPos, 9999f);
if (nearest == null) return $"No enemies. Count={gl.Enemies.Count}";

// Teleport to within range + manual sync
var newPos = nearest.worldPos + new Vector2(150, 0);
sm.PlayerController?.PlaceAt(newPos, snapCamera: false);
var player = gl.Player;
if (player != null) { player.worldPos = newPos; player.combat.position = newPos; }

int hpB = nearest.combat?.currentLife ?? 0;
var r = gl.PlayerCastSkill(117, nearest.actorId);
int hpA = nearest.combat?.currentLife ?? 0;

return $"enemies={gl.Enemies.Count}\n" +
       $"{nearest.nameVi}: hp={hpB}→{hpA} dmg={hpB-hpA}\n" +
       $"cast117: success={r?.success} reason={r?.reason} dead={nearest.isDead}\n" +
       gl.GetStatusSummary();
```

## TriggerSkillSlot end-to-end probe (two separate execute_code calls)

**Call 1 — trigger:**
```csharp
var sm = VLTK.Sandbox.SandboxManager.Instance; var gl = sm?.GameplayLoop;
var slots = UnityEngine.Object.FindFirstObjectByType<VLTK.UI.CombatSkillSlotController>();
var rtAll = sm.EnemyRuntime?.GetActiveEnemies();
VLTK.Sandbox.EnemyRuntimeInfo t1 = null;
foreach (var e in rtAll) { if (e.alive) { t1 = e; break; } }
var np = t1.position + new Vector2(150, 0);
sm.PlayerController?.PlaceAt(np, snapCamera: false);
if (gl.Player != null) { gl.Player.worldPos = np; gl.Player.combat.position = np; }
slots.TriggerSkillSlot(2, 117);  // slot 2 = skill 117
return $"triggered on {t1.displayName} eid={t1.enemyId}";
```

**Call 2 — read results (a few seconds later):**
```csharp
var sm = VLTK.Sandbox.SandboxManager.Instance; var gl = sm?.GameplayLoop;
var glA = gl?.GetActor(10001); // 10000 + enemyId=1
var rtAll = sm?.EnemyRuntime?.GetActiveEnemies();
int rtHp = -1; bool alive = true;
foreach (var e in rtAll) if (e.enemyId == 1) { rtHp = e.currentLife; alive = e.alive; break; }
return $"Visual: hp={rtHp} alive={alive}\nGL: hp={glA?.combat?.currentLife}/{glA?.combat?.maxLife} dead={glA?.isDead}\nSilver={gl?.Economy?.Wallet.silver} EXP={gl?.LevelService?.CurrentExp}";
```

## Commits

- `93d704a42` — `combat: bridge EnemyRuntime→GameplayLoop + sync player combat.position`
- `e095c9c6d` — `combat: full gameplay loop integration - skill slot → EXP/silver sync`
