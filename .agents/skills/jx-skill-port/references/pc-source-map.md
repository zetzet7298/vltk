# PC Skill Source Map

This map is navigation guidance. Re-run `srcwalk show` before citing line numbers because the PC corpus can be updated.

## Source priority

1. `/var/www/jx-source/pak_unpacked/` is the canonical extracted runtime corpus.
2. Determine the active package winner from client PAK order with `jx-pc-resource-resolver`.
3. Use the audited C++ source under `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/` to interpret table enums, dispatch, timing, and event order.
4. Use `/var/www/jx-source/docs/SOURCE_INDEX.md`, scan reports, and backend/client port docs as indexes, not substitutes for the underlying source.
5. Treat `Assets/StreamingAssets/Reference/`, Unity parsers, tests, comments, screenshots, and old reports as comparison material only.

The bundled helper defaults to `pak_unpacked/slistcache`. That is a selected package for reconnaissance, not proof that `slistcache` wins the active PAK load order.

## Core runtime paths

Within a selected extracted package:

```text
settings/skills.txt
settings/missles.txt
script/skill/**/*.lua
```

Core C++ anchors:

```text
SwordOnline/Sources/Core/Src/SkillDef.h
SwordOnline/Sources/Core/Src/KSkills.cpp
SwordOnline/Sources/Core/Src/KMissle.cpp
SwordOnline/Sources/Core/Src/KMissle.h
SwordOnline/Sources/Core/Src/KMissleRes.cpp
SwordOnline/Sources/Core/Src/KMissleSet.cpp
SwordOnline/Sources/Core/Src/KSkillList.cpp
SwordOnline/Sources/Core/Src/KNpc.cpp
SwordOnline/Sources/Core/Src/CoreShell.cpp
```

Use `srcwalk discover` to locate the current copy and `srcwalk context/show/trace` to bound claims. Do not infer a call path from filename presence.

## Encoding

PC tables can mix:

- ASCII column names and identifiers.
- TCVN3 Vietnamese bytes in names and descriptions.
- GBK/GB18030 Chinese bytes in SPR/WAV/Lua paths.

Decode by evidence and field type. A whole-file decode can make Vietnamese readable while corrupting Chinese resource paths, or vice versa. Preserve original source bytes/path for hashing and use `jx-pc-resource-resolver`.

Never rewrite or re-encode PC `.txt`, `.lua`, `.ini`, or `.cfg` files.

## Skills.txt groups

### Identity and UI

- `SkillName`, `Property`, `SkillId`, `Attrib`, `SkillStyle`
- `SkillIcon`, `PreCastSpr`, `ManCastSnd`, `FMCastSnd`
- `ReqLevel`, `MaxLevel`, `SkillDesc`

### State and target behavior

- `StateSpecialId`, `StatePriority`, `IsAura`, `LRSkill`, `NeedShadow`
- `TargetOnly`, `TargetEnemy`, `TargetAlly`, `TargetSelf`
- `TargetOther`, `TargetObj`, `TargetNoNpc`
- `PeaceCanUse`, `HorseLimit`, `StopWhenMove`

### Missile and formation

- `AttackRadius`, `MaxShadowNum`
- `MslsGenerate`, `MslsGenerateData`
- `MisslesForm`
- `ChildSkillId`, `ChildSkillLevel`, `ChildSkillNum`, `BaseSkill`
- `WaitTime`, `Param1`, `Param2`, `HeelAtParent`, `RelativePosType`

`BaseSkill` determines whether `ChildSkillId` creates a missile resource directly or dispatches another skill. Confirm in `KSkills.cpp`; do not decide from the child ID range.

### Cast, cost, and damage timing

- `CharClass`, `CharAnimId`, `IsMelee`, `DoHurt`, `ByMissle`
- `SkillCostType`, `CostValue`
- `TimePerCast`, `TimePerCastOnHorse`
- `IsPhysical`, `WeaponSkill`, `Series`

### Events

- `StartEvent`, `StartSkillId`
- `FlyEvent`, `FlySkillId`, `FlyEventTime`
- `CollideEvent`, `CollidSkillId`
- `VanishedEvent`, `VanishedSkillId`
- `ShowEvent`, `EventSkillLevel`, `ShowAddition`

Lua level data can enable an event or replace its destination even when the static row is zero. Inspect both.

### Level bindings

- `LvlSetScript`
- `LvlSetting1..20`
- `LvlData1..20`
- `LevelUpScript`

For each non-empty binding, inspect the exact Lua table and the C++ consumer. JX tables can contain multiple channels (`[1]`, `[2]`, `[3]`), duplicate anchors, `Conic`, helper functions, and missing-table behavior.

## Missles.txt groups

### Movement and following

- `MoveKind`, `FollowKind`, `ColFollowTarget`
- `LifeTime`, `Speed`, `Zspeed`, `Zacc`
- `Param1`, `Param2`, `Param3`

### Collision and lifecycle

- `MissleHeight`, `CollidRange`
- `IsRangeDmg`, `DmgRange`, `DmgInterval`
- `ResponseSkill`, `CanDestroy`, `ColVanish`, `CanSlow`
- `CanColFriend`, `AutoExplode`, `MissRate`

### Visual and sound slots

- `LoopPlay`, `SubLoop`, `SubStart`, `SubStop`, `MultiShow`
- `AnimFile1..4`, `AnimFileInfo1..4`, `SndFile1..4`
- `AnimFileB1..B4`, `AnimFileInfoB1..B4`, `SndFileB1..B4`
- `RedLum`, `GreenLum`, `BlueLum`, `LightRadius`

Do not collapse flight and collision slots into one visual or sound. Interpret slot state transitions from `KMissleRes.cpp`/`KMissle.cpp`.

## Required enum evidence

`SkillDef.h` currently defines:

- `eMissleMoveKind`: `0 Stand`, `1 Line`, `2 Random`, `3 Circle`, `4 Helix`, `5 Follow`, `6 Motion`, `7 Parabola`, `8 SingleLine`, `100 RollBack`, `101 Toss`.
- `eMisslesForm`: `0 Wall`, `1 Line`, `2 Spread`, `3 Circle`, `4 Random`, `5 Zone`, `6 AtTarget`, `7 AtFirer`.
- `eSKillStyle`: `0 Missles`, `1 Melee`, `2 InitiativeNpcState`, `3 PassivityNpcState`, followed by specialized styles.

Always read the current enum and the selected `switch`/cast function. A Unity enum with different names or numbering is not proof of equivalence.

## Phi Long Tai Thien evidence example

Selected package evidence:

- `pak_unpacked/slistcache/settings/skills.txt:358`: skill `357`.
- `pak_unpacked/slistcache/settings/missles.txt:167`: missile `166`.
- `pak_unpacked/slistcache/script/skill/gaibang.lua:156-185`: `feilong_zaitian`.

C++ mechanism evidence:

- `KSkills.cpp:697-725`: wall dispatch, target direction, and caster/target origin selection.
- `KSkills.cpp:1497-1608`: `KSkill::CastWall`.
- `KMissle.cpp:784-817`: `MISSLE_MMK_Follow` update.
- `SkillDef.h:20-33`: movement enum.
- `SkillDef.h:120-130`: missile-form enum.

At level 20:

- Root `MisslesForm=0`, so dispatch is `SKILL_MF_Wall`.
- `Param1=32`.
- Lua `skill_misslenum_v=4`.
- Wall dispatch rotates the target direction by a quarter turn to build the perpendicular formation axis; non-zero `Param2` rotates each missile back to the original target direction.
- `CastWall` starts at `-32 * 4 / 2`, then increments by `32`: `-64, -32, 0, 32`.
- Lua `missle_speed_v=24`, overriding missile row speed `30`.
- Missile `166`: `MoveKind=5`, `LifeTime=24`.
- `CastWall` stores `nTargetId` and stable NPC ID on every spawned missile.
- Follow movement refreshes direction when `m_nTempParam1++ >= 8`, which is the ninth update from zero.

This example also demonstrates why existing Unity terminology can mislead: a current Unity test/comment may call form `0` "Single", but PC `SkillDef.h` and `KSkill::CastWall` prove wall formation.

## Audit helper

```bash
python3 scripts/audit_pc_skill.py \
  --skill-id 357 \
  --package slistcache \
  --level 20
```

Use its JSON for fast row discovery. Then:

1. Verify package/load-order winner.
2. Read the enum and C++ consumer.
3. Resolve every listed resource.
4. Evaluate Lua through the real PC semantics.
5. Recurse through child and event skills.
