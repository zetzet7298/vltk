# Đả Cẩu / Dagou Version Priority Evidence

## Decision
Newest PC sources make skill `124` a passive staff mastery (`dagou_zhen`), not an active ally aura. The actual state/aura projectile row is `209`.

This corrects an older mobile interpretation that treated `124` as `SkillStyle=2` aura with `AttackRadius=180` and `StateSpecialId=44`.

## Compared PC sources
All checked newest PC skill tables agree for row `124`:

- `/var/www/jx-pc/Client 6.0/settings/skills.txt`
- `/var/www/jx-pc/Server 6.0/server/home_jxser/server1/settings/skills.txt`
- `/var/www/jx-pc/Server 6.0/server/home_jxser/server1/script/skill2/skills.txt`
- `/var/www/jx-pc/Server 6.0/server/home_jxser_bachkim_6.0/server1/settings/skills.txt`
- `/var/www/jx-pc/Server 6.0/server/home_jxser_bachkim_6.0/server1/script/skill2/skills.txt`

Row `124` values:

```text
SkillName=Đả Cẩu Bổng
SkillStyle=3
SkillIcon=\spr\Ui\技能图标\icon_sk_gb_23.spr
AttackRadius=0
IsAura=0
TargetAlly=0
TargetSelf=0
StateSpecialId=0
ChildSkillId=0
ChildSkillNum=1
MisslesForm=7
CharAnimId=11
```

Row `209` values:

```text
SkillName=Đả Cẩu Bổng
SkillStyle=0
StateSpecialId=44
AttackRadius=180
TargetAlly=1
TargetSelf=1
ChildSkillId=92
ChildSkillNum=1
```

## Lua evidence
Reference Lua: `Assets/StreamingAssets/Reference/gaibang.lua`, decoded with GBK.

`dagou_zhen`:

```lua
dagou_zhen={ --打狗阵
    addphysicsdamage_p={{{1,10},{20,175}},{{1,-1},{30,-1}},{{1,2},{2,2}}},
    --skill_cost_v={{{1,24},{20,50}}}
},
```

So mobile skill `124` should expose a permanent passive `AddPhysicsDamageP` curve:

```text
L1  = AddPhysicsDamageP=10,-1,2
L20 = AddPhysicsDamageP=175,-1,2
```

## Mobile changes
- `PcCombatCatalogFactory` now defines `124` with `PassiveMastery(...)` using PC newest row values and Lua curve.
- `CombatRuntimeService` no longer special-cases `124` as an aura; aura propagation is guarded generically by `skill.isAura && skill.targetAlly && skill.stateSpecialId != 0`.
- `CaiBangDogArrayTests` now tests:
  - `124` newest passive config;
  - `124` Lua `addphysicsdamage_p` L1/L20 values;
  - `209` remains the actual state projectile/aura row with `StateSpecialId=44`.
