# US-M44-001 Novice + Cái Bang Combat PC Parity

## Status

implemented

## Lane

normal

## Intake

Change request / spec slice. Flags: Existing behavior, Weak proof. Scope bounded to combat novice + Cái Bang.

## Product Contract

Mobile combat must match PC behavior for:

- novice/no-faction physical attacks before joining sect;
- Cái Bang faction skill availability;
- all Cái Bang skills from PC `Skills.txt` ids 115–130 plus aura child 209;
- skill-level formulas from corresponding PC Lua scripts;
- PC cast gates: fight mode, known skill, cooldown, required level, faction, target relation, range, horse limit, weapon-skill match, mana cost;
- action state/frame choice for attack/magic/melee and projectile child count.

## PC Evidence

- `jxwin-kinnox/SourceNew/swrod3/bin/Server/Settings/Skills.txt`
  - novice: ids 1,2,53,196,199;
  - Cái Bang: ids 115–130, child 209;
  - fields used: `SkillStyle`, `CharClass`, `AttackRadius`, `MisslesForm`, `ChildSkillId`, `ChildSkillLevel`, `ChildSkillNum`, `BaseSkill`, `CharAnimId`, `IsMelee`, `WaitTime`, `TimePerCast`, `IsPhysical`, target flags, `ReqLevel`, `MaxLevel`, `HorseLimit`, `DoHurt`, `WeaponSkill`, `StateSpecialId`, `IsAura`.
- `jxwin-kinnox/SourceNew/swrod3/Utility/Run/Script/skill/special/*.lua`
  - novice attack formulas: `physicsenhance_p=0`, `attackrating_p=0`, `skill_cost_v=0`; poison attack formula.
- `jxwin-kinnox/SourceNew/swrod3/Utility/Run/Script/skill/gaibang/*.lua`
  - all Cái Bang level formulas.
- `jxwin-kinnox/SourceNew/swrod3/SwordOnline/Sources/Core/Src/KNpc.cpp:1580-1801`
  - `DoSkill`/`DoOrdinSkill`: fight mode, `CanCast`, `CanCastSkill`, cost, action state/frame flow.
- `jxwin-kinnox/SourceNew/swrod3/SwordOnline/Sources/Core/Src/KSkills.cpp:130-255`
  - target relation, physical weapon-skill, equip/horse/range gates.
- `jxwin-kinnox/SourceNew/swrod3/SwordOnline/Sources/Core/Src/Skill/KSkillList.cpp:208-229`
  - `CanCast`/`SetNextCastTime` cooldown gate.

## Implementation

- `Assets/Scripts/Model/CombatDefinition.cs`
  - PC enums and magic attribute triples.
- `Assets/Scripts/Model/SkillDefinition.cs`
  - extended skill model with PC combat fields and full level data.
- `Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs`
  - PC-derived novice + Cái Bang catalog seed.
- `Assets/Scripts/Sandbox/CombatRuntimeService.cs`
  - pure C# cast gate/runtime flow for novice + Cái Bang combat.
- `Assets/Scripts/Sandbox/SandboxManager.cs`
  - boots the PC-derived combat catalog/runtime into the sandbox services root.
- `Assets/Tests/EditMode/Sandbox/CaiBangCombatParityTests.cs`
  - PC fixture tests for catalog, formulas, cast gates, cooldown, projectile count, damage, buff/aura, faction lock.

## Acceptance Criteria

- AC1: Novice no-faction attack uses PC skill ids and gates.
- AC2: Cái Bang skill catalog contains ids 115–130 plus child 209 with PC fields.
- AC3: Cái Bang level formulas match PC Lua for damage, costs, passives, resists, aura.
- AC4: Cast runtime preserves PC gates/cooldown/action/targeting/projectile/damage flow.
- AC5: Verification runs through Harness/Unity checks.

## Validation

Expected proof:

- EditMode `CaiBangCombatParityTests` pass: 9/9, job `1e89149127824fd6bf569d6a56e25dee`.
- Existing M4 combat tests still pass: 39/39, job `39d3446d09c24f749c81d842533f949b`.
- Full EditMode suite still passes: 430/430, job `7d692e518b1d45e9bcc6b5dcf978964f`.
- Harness story status/proof updated (`US-M44-001` implemented; trace #17 recorded before final full rerun, final job ids above are authoritative).
