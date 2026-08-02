# SURVIVOR_PLAN — vltk-mobile × DHCD gameplay parity

> Mục tiêu: game survivor roguelike mới trong vltk-mobile. Gameplay parity
> **DHCD (Đại Hiệp Chế Đạo / 墨迹大侠)**. Visual / UI / nhân vật / skill dùng
> asset JX (jx-source) đã port trong Sandbox. **Offline single-player trước**.
> Mode/scene MỚI song song Sandbox (giữ Sandbox làm reference + nguồn asset).
> Orientation **portrait**.

## Nguồn sự thật

- Gameplay loop (declarations): `C:/Projects/dhcd/reconstructed-types/BattleCore/`
- Evidence chưng cất: `C:/Projects/dhcd/docs/evidence/r-dhcd-*.md`, `docs/gameplay-evidence-map.md`
- Server: KHÔNG reverse (xem `C:/Projects/dhcd/docs/server-reverse-decision.md`). Build backend mới sau.
- Visual/content JX: `Assets/Scripts/Sandbox/` (PcXxxParser/Service, Male/FemalePlayerVisual,
  SkillEffectVisualService, MissileSpawner, PcNpcVisual, v.v.).
- RandomSkillConfig bundle dhcd **mã hóa FastXXTEA, key blocked** (`r-dhcd-001-build-key`).
  → KHÔNG port data skill dhcd. Tự định nghĩa library từ skill JX.

## Kiến trúc

```
Assets/Scripts/Survivor/            (asmdef: VLTK.Survivor.Runtime)
  ├─ Director/        SurvivorBattleDirector (lifecycle), BattleContext
  ├─ Level/           NormalLevelLogic (survivor override), LevelWave, WaveRefresh
  ├─ Actor/           SurvivorPlayer, SurvivorMonster, ActorAttrData
  ├─ AI/              AITask base + derived (port BattleCore.AI*)
  ├─ Skill/           LevelRandomSkillCtrl + RandomSkillConfig (data từ JX)
  ├─ Collect/         LevelCollectItemMgr (XP/gem drops)
  ├─ Combat/          ColliderDamageCmpt, hit processing
  ├─ Bridge/          JxContentBridge (faction skill→RandomSkillConfig, NPC visual→monster)
  └─ UI/              Portrait HUD: joystick, skill bar, levelup card panel (3 mode)
Assets/Scenes/Survivor.unity         (scene mới, portrait)
```

ref asmdef: `VLTK.Sandbox.Runtime` (+ Core). Không sửa code Sandbox đang chạy.

## DHCD system → port map

| DHCD (BattleCore) | Vai trò | Port target | Ghi chú |
|---|---|---|---|
| `BattleLevelLogic` | lifecycle: Init/Start/GameStart/Update/GameEnd/Destroy | `SurvivorBattleDirector` | virtual hooks OnInit/OnStart/OnGameStart/OnUpdate/OnDestroy/OnAfterBattleEnd |
| `NormalLevelLogic` | survivor mode override | `NormalLevelLogic` (Survivor) | wave jump, random skill, collect |
| `LevelMonsterMgr` + `LevelWave` + `WaveRefresh` | wave orchestration + spawn pool | `SurvivorWaveSystem` | spawn time/interval/limit/boss, SpawnMonsterNormal |
| `MonsterCfg` | monster data (hp/atk/speed/skill/loot/boss/collision) | `SurvivorMonsterCfg` | data nạp từ JX NPC |
| `LevelCollectItemMgr` | XP/gem drop + merge + collect-on-death | `SurvivorCollectItemMgr` | r-dhcd-006-drop-xp |
| `LevelRandomSkillCtrl` | roguelike skill choice | `SurvivorRandomSkillCtrl` | 3 mode: levelup/box/shop; reroll; per-roleId queue (r-dhcd-002) |
| `PlayerRandomSkillData` | per-role pending-event queue + waiting time | nằm trong ctrl | m_playerEventWaitingList (Queue), m_beginWaitingLearnTime |
| `PlayerEntity` | player skills/buffs/weapon/pet/damage | `SurvivorPlayer` | visual = Male/FemalePlayerVisual |
| `NpcEntity` | monster actor: AI/lifecycle/collider/owner | `SurvivorMonster` | visual = PcNpcVisual |
| `ActorAttrData` + `ActorAttrImpactData/Mgr` | attributes + impact | `SurvivorActorAttr` | port trực tiếp |
| `ColliderDamageCmpt` | hit processing (owner/other) | `SurvivorHitProcessor` | port trực tiếp |
| `AIBaseTask` + AI*Task | monster AI (move/attack/charge/follow/...) | `SurvivorAITask` family | port theo nhu cầu mode |
| `BattleSys.set_IsPause` + `ReCalcTimeScale` | card-choice pause (timescale) | HUD levelup panel | r-dhcd-003: acquire OnVisible, release OnHidden; timescale ∈ {0,1,1.5,2} |
| BattleCmd* (SelectRandomSkill, ReRandomSkill, SelectBoxSkill, SkillRun, ...) | client→logic commands | `SurvivorBattleCmd` | port command shape |

## Content mapping (JX → dhcd slot)

| dhcd slot | Nguồn JX (đã port) | Bridge |
|---|---|---|
| Player visual | `MalePlayerVisual` / `FemalePlayerVisual` | spawn thay cho avatar dhcd |
| Monster visual | `PcNpcVisual` + NpcTemplateRegistry | map monster cfg → NPC res |
| Skill library (RandomSkillConfig) | `PcSkill*` (faction skill) qua `SkillEffectVisualService` + `MissileSpawner` | mỗi faction skill = 1 RandomSkillConfig entry; weight/level scaling tự định nghĩa |
| Skill visual | `SkillEffectVisualService` (SPR precast/missile) | dùng khi cast |
| Map/arena | map JX (MapManager) hoặc arena đơn giản tự vẽ | MVP: arena trống |

## Phase / milestone

**P0 — Foundation (session này):**
1. Plan doc (file này) ✅
2. Migrate portrait (ProjectSettings + Canvas)
3. Survivor namespace + asmdef + scene rỗng chạy được
4. SurvivorBattleDirector lifecycle skeleton (Init/Start/GameStart/Update/GameEnd/Destroy rỗng)

**P1 — Core loop playable (vertical slice):**
- Arena + player (joystick, 1 auto-attack + 1 skill)
- Wave spawn liên tục (WaveRefresh tối giản: time/interval/limit)
- Monster AI tối giản (move-to-player)
- Hit processing + die
- XP drop + levelup → card panel 1/3 (LevelRandomSkillCtrl mode levelup)
- Die → restart

**P2 — Parity depth:**
- 3 mode skill choice (levelup/box/shop) + reroll (BattleCmd*)
- Wave types + boss spawn + endless
- Supply skill / super skill / weight library
- ActorAttr impact (buff/debuff)
- Pause timescale khi card mở (r-dhcd-003)
- Meta progression (offline save)

**P3 — Backend mới** (khi loop ổn + cần cloud save/multiplayer).

## Quy tắc port

- Dùng **declaration + evidence** dhcd làm spec, KHÔNG copy IL recovery (gần như garbage).
- FP (fixed-point) dhcd → dùng `float` (ponytail: Unity đơn giản hóa, chưa cần deterministic).
- FTimer/FTask → Coroutine/async Unity.
- Fail-closed: skill/missile chưa có visual JX staged → không gán (theo AGENTS.md SPR parity).
- Mỗi system port xong để lại 1 EditMode self-check nhỏ.
