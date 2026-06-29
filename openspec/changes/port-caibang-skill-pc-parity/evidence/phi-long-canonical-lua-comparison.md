# Phi Long Canonical Lua Comparison

## Question
The old mobile test ignored level-20 Phi Long lane assertions because `gaibang.lua::feilong_zaitian` has no `skill_param1_v`. We needed to determine whether mobile reference data was stale or whether lane spacing must come from another PC source.

## Compared files
- `Assets/StreamingAssets/Reference/gaibang.lua`
- `/var/www/vltksource_new/vl_update_27/Client 6.0/file/skill/gaibang.lua`
- `/var/www/vltksource_new/vl_update_27/Client 6.0/script/skill/gaibang.lua`
- `/var/www/vltksource_new/vl_update_27/Server 6.0/server/home_jxser/server1/script/skill/gaibang.lua`
- `/var/www/vltksource_new/vl_update_27/pak_unpacked/update03/script/skill/gaibang.lua`
- `/var/www/vltksource_new/vl_update_27/pak_unpacked/vltkdata/script/skill/gaibang.lua`

## Common canonical block
All checked canonical Lua sources include:

```lua
feilong_zaitian={ -- Phi Long Tại Thiên
    skill_misslesform_v={{{1,1},{11,1},{11,0},{20,0}}},
    skill_misslenum_v={{{1,1},{11,1},{12,2},{15,2},{16,3},{20,4},{21,4}}},
}
```

No checked canonical `feilong_zaitian` block has `skill_param1_v`.

## Skill/missile row evidence
From `Assets/StreamingAssets/Reference/PcSkill/skills.txt` row `SkillId=357`:

| Field | Value |
| --- | --- |
| `Param1` | `32` |
| `ChildSkillId` | `166` |
| `ChildSkillNum` | `3` in row; runtime Lua count overrides to L20 `4` |
| `MisslesForm` | `0` |

From `Assets/StreamingAssets/Reference/PcAttrib/missles.txt` row `MissleId=166`:

| Field | Value |
| --- | --- |
| `MoveKind` | `5` |
| `Speed` | `30` |
| `LifeTime` | `24` |
| `AnimFile2` | dragon SPR path, resolved as `a31b9f04.spr` |
| `AnimFile4` | impact SPR path, resolved as `c33e96c2.spr` |

## Decision
For `Phi Long Tại Thiên`:
- Use `gaibang.lua skill_misslenum_v` for per-level missile count (`L20=4`).
- Use `skills.txt Param1=32` as the fallback parallel lane gap when Lua has no `skill_param1_v`.
- Use missile row `166 MoveKind=5` as direct PC evidence for live-target homing.

## Mobile implementation implication
When `PcCaiBangLuaLevelService.GetMissileCount(357, 20) == 4` and `skill_param1_v` is absent, `SkillEffectVisualService` must still call `SetupPcPhiLongSpread(fx, 4, 32)` so `missileTargetOffsets` are configured. The four dragons then resolve targets as `liveTarget + offset[i]` through `ActiveSkillEffect.ResolveMissileTarget(i)`.
