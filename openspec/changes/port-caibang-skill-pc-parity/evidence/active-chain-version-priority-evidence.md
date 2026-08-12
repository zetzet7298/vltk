# Active Chain Version Priority Evidence

## Rule applied
User clarified that when multiple PC versions exist, the port must prefer the newest PC version. For this slice, the newest PC `skills.txt`/`gaibang.lua` pairing wins over stale comments/mobile assumptions.

## Compared sources
- `Assets/StreamingAssets/Reference/PcSkill/skills.txt`
- `Assets/StreamingAssets/Reference/gaibang.lua`
- `/var/www/jx-pc/pak_unpacked/_slistcache/script/skill/gaibang.lua`
- `/var/www/jx-pc/pak_unpacked/vltkdata/script/skill/gaibang.lua`
- `/var/www/jx-pc/Client 6.0/file/skill/gaibang.lua`
- `/var/www/jx-pc/Server 6.0/server/home_jxser/server1/script/skill-goc/gaibang.lua`

For the checked attributes, mobile reference, `_slistcache`, `vltkdata`, Client 6.0, and Server skill-goc agree.

## Correct newest PC skill id mapping
| SkillId | PC `skills.txt` name | Lua key | Important chains |
| ---: | --- | --- | --- |
| `125` | `Bổng Đả Ác Cẩu` (`Bæng §¶ ¸c CÈu`) | `bangda_egou` | `addskilldamage1 -> 359` chance `60`, `addskilldamage2 -> 1074` chance `50` at L20 |
| `359` | `Thiên Hạ Vô Cẩu` (`Thiªn H¹ V« CÈu`) | `tianxia_wugou` | `addskilldamage1 -> 1074` chance `25` at L20 |
| `1539` | `Thiên Hạ Vô Cẩu` NPC variant | `tianxia_wugou` | NPC variant uses the same Thiên Hạ Lua table |

## Why this mattered
Previous mobile code mapped `125 -> tianxia_wugou`, which treated `Bổng Đả Ác Cẩu` as `Thiên Hạ Vô Cẩu`. That lost the newest PC `bangda_egou` double-chain behavior:

```lua
bangda_egou={
    addskilldamage1={ [1]={{1,359},{2,359}}, [3]={{1,1},{20,60}} },
    addskilldamage2={ [1]={{1,1074},{2,1074}}, [3]={{1,1},{20,50}} },
    skill_attackradius={{{1,448},{20,512}}},
    skill_cost_v={{{1,28},{20,48}}}
}
```

`Thiên Hạ Vô Cẩu` remains:

```lua
tianxia_wugou={
    skill_misslenum_v={{{1,1},{20,3},{21,3}}},
    addskilldamage1={ [1]={{1,1074},{2,1074}}, [3]={{1,1},{20,25}} },
    missle_speed_v={{{1,20},{20,24},{21,24}}},
    skill_attackradius={{{1,448},{20,512},{21,512}}},
    skill_cost_v={{{1,20},{20,50}}}
}
```

## Mobile implementation
- `PcCaiBangLuaLevelService`: `125 -> bangda_egou`, `359/1539 -> tianxia_wugou`.
- `CombatRuntimeService`: supports multiple `addskilldamageN` slots for a single parent; `125` can fire both `359` and `1074` chains.
- `PcCombatCatalogFactory`: 1539 now matches newest PC Thiên Hạ variant shape (child missile `168`, single/homing form with Lua count override) rather than stale Bổng Đả/NPC shape.
- Chain randomness now uses injected `DamageFormulaService.RollPercent`, so Cai Bang tests can deterministically force chain hit/miss.

## Test evidence
- `CaiBangAddSkillDamageChainTests.BangDaEgou_L20Chances60And50_Target359And1074` verifies both newest PC chances for 125.
- `CaiBangAddSkillDamageChainTests.TianxiaWugou_L20Chance25_Target1074` verifies 359 remains the Thiên Hạ chain.
- `CaiBang_Cast_AppliesCostCooldownProjectileCountDamageAndHorseRestriction` with deterministic `RollPercent=true` now expects `8` chain projectiles (`359` L20 count `3` + `1074` L20 count `5`).
- `CaiBang_1539_VisualServiceUsesPcMissile168HomingSpeed` verifies 1539 uses missile `168`, Lua speed `24`, lifetime `32`, and count `3` like 359.
