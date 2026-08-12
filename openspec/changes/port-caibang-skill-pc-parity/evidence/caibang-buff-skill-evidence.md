# Cai Bang Buff Skill Evidence: Hoạt Bất Lưu Thủ and Túy Điệp Cuồng Vũ

## Hoạt Bất Lưu Thủ (`SkillId=127`)

### PC Lua source
Source: `Assets/StreamingAssets/Reference/gaibang.lua`, key `huabu_liushou`:

```lua
huabu_liushou={
    fastwalkrun_p={{{1,9},{20,66}},{{1,18*120},{20,18*180}}},
    skill_cost_v={{{1,24},{20,50}}}
}
```

### L20 expected values
| Attribute | L20 value | Notes |
| --- | ---: | --- |
| `FastWalkRunP.value1` | `66` | movement speed +66% |
| `FastWalkRunP.value2` | `3240` | `18*180` PC ticks |
| `SkillCostV.value1` | `50` | mana cost |

### Mobile verification
- `PcCombatCatalogFactory` builds skill `127` as `PassivityNpcState`, stateSpecialId `17`, with `FastWalkRunP` and PC duration.
- `SandboxPlayerController` consumes active `MagicAttributeKind.FastWalkRunP` from player combat states and multiplies movement speed by `1 + value1/100`.
- `GameplayLoopService.Tick` decrements positive `SkillMagicAttribute.value2` durations and removes expired states.
- Test `CaiBang_127_HoatBatLuuThu_AppliesPcFastWalkRunDuration` verifies runtime cast state/cost values.

## Túy Điệp Cuồng Vũ (`SkillId=130`)

### PC Lua source
Source: `Assets/StreamingAssets/Reference/gaibang.lua`, key `zuidie_kuangwu`:

```lua
zuidie_kuangwu={
    allres_p={{{1,1},{30,30}},{{1,18*120},{30,18*180}}},
    addfiremagic_v={{{1,10},{30,215}},{{1,18*120},{30,18*180}}},
    addfiredamage_v={{{1,10},{30,175}},{{1,18*120},{30,18*180}}},
    deadlystrikeenhance_p={{{1,5},{20,30,Conic}},{{1,18*120},{30,18*180}}},
    lifemax_yan_p={{{1,21},{35,20},{36,20}},{{1,-1},{30,-1}}},
    skill_cost_v={{{1,50},{20,100}}}
}
```

### L20 expected values
| Attribute | L20 value | Duration | Notes |
| --- | ---: | ---: | --- |
| `AllResP.value1` | `20` | `2867` | duration interpolates L1 `2160` to L30 `3240` |
| `AddFireDamageV.value1` | `144` | `2867` | mobile maps PC `addfiremagic_v` to `AddFireDamageV`; floors interpolation |
| `FireDamageV.value1` | `118` | `2867` | mobile maps PC `addfiredamage_v` to `FireDamageV`; floors interpolation |
| `DeadlyStrikeEnhanceP.value1` | `30` | `2867` | Conic reaches 30 at L20 |
| `LifeMaxYanP.value1` | `20` | `-1` | PC duration slot is -1 sentinel |
| `SkillCostV.value1` | `100` | n/a | mana cost |

### Mobile verification
- `PcCombatCatalogFactory` now applies finite PC duration to all active Túy Điệp buff states except `LifeMaxYanP`, which preserves PC `-1` sentinel.
- Test `CaiBang_130_TuyDiepCuongVu_AppliesPcBuffDurations` verifies runtime cast state/cost values.
