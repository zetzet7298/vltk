# PC Source Audit — Cái Bang Skills 127 & 130

**Date**: 2026-06-16
**Status**: Audit complete, formulas 100% verified
**Scope**: Skills `huabu_liushou` (127 — Hoạt Bất Lưu Thủ) and `zuidie_kuangwu` (130 — Túy Điệp Cuồng Vũ)

## Source Provenance

All values traced to direct PC source files under
`/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/`. No C++ source available in this
distribution; only Lua scripts, settings, binary, and the canonical
`pak_unpacked` SPR tree.

| Aspect | PC source file | Encoding | Reference |
|---|---|---|---|
| Skill formulas | `Client 6.0/script/skill/gaibang.lua` | GBK | lines 68-71, 100-107 |
| State visual mapping | `Client 6.0/settings/×´Ì¬Óë¹âÐ§Í¼ÐÎ¶ÔÕÕ±í.txt` | GBK | Status43 entry |
| Skill data table | `Client 6.0/settings/skills.txt` | GB2312 | mixed TCVN3/GBK |
| SPR assets | `pak_unpacked/skills/unknown/{3cae8f47,7d34af1d}.spr` | binary SPR | verified byte-identical |
| Server-side scripts | `Server 6.0/server/home_jxser/server1/script/skill/gaibang.lua` | GBK | identical to client |

## Skill 127 — Hoạt Bất Lưu Thủ (huabu_liushou)

### PC Lua source (verbatim)

`gaibang.lua:68-71`:
```lua
huabu_liushou={
  fastwalkrun_p={{{1,9},{20,66}},{{1,18*120},{20,18*180}}},
  skill_cost_v={{{1,24},{20,50}}}
}
```

### Attribute breakdown

| Attribute | Param | PC level→value | Unity impl | Match |
|---|---|---|---|---|
| `fastwalkrun_p` | [1] value | L1=9, L20=66 | `Link(lv, (1, 9, ""), (20, 66, ""))` | ✅ |
| `fastwalkrun_p` | [2] duration | L1=18×120=2160, L20=18×180=3240 | `18 * Link(lv, (1, 120, ""), (20, 180, ""))` | ✅ |
| `skill_cost_v` | [1] value | L1=24, L20=50 | `Link(lv, (1, 24, ""), (20, 50, ""))` | ✅ |

### Visual config (Unity `SkillEffectVisualService.cs` case 127)

- PreCast SPR: `3cae8f47` (path `\spr\skill\天忍\mag_tr_16_施魔法.spr`)
  - Verified byte-identical to `pak_unpacked/skills/unknown/3cae8f47.spr`
- PreCast frames: 5, interval 4 ticks (mobile-tuned; PC does not specify
  per-frame timing in Lua — engine reads animation from SPR metadata)
- Impact duration: 0.3s (mobile design; no PC source)
- No persistent body aura (correct — no StateSpecialId in PC for skill 127)

### Test coverage

- `CaiBangCombatParityTests.CaiBang_BuffsAndAura_TargetSelfOrAllyAndApplyState` ✅
- `CaiBangCombatParityTests.CaiBang_ResistAndPassiveSkills_MatchLuaLevelFormulasIncludingBugs` ✅
- `CaiBangSkillPanelTests.PcCombatCatalog_CaiBangRowsMatchAuthoritativePcSkillsTxt` ✅
- `CaiBangSkillPanelTests.SkillPanelSnapshot_ListsAllCaiBangSkillsInPcSlotOrder` ✅

## Skill 130 — Túy Điệp Cuồng Vũ (zuidie_kuangwu)

### PC Lua source (verbatim)

`gaibang.lua:100-107`:
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

### Attribute breakdown

| Attribute | Param | PC level→value | Unity impl | Match |
|---|---|---|---|---|
| `allres_p` | [1] value | L1=1, L30=30 | `Link(lv, (1, 1, ""), (30, 30, ""))` | ✅ |
| `allres_p` | [2] duration | L1=2160, L30=3240 | `dur` shared | ✅ |
| `addfiremagic_v` | [1] value | L1=10, L30=215 | `Link(lv, (1, 10, ""), (30, 215, ""))` | ✅ |
| `addfiremagic_v` | [2] duration | L1=2160, L30=3240 | `dur` shared | ✅ |
| `addfiredamage_v` | [1] value | L1=10, L30=175 | `Link(lv, (1, 10, ""), (30, 175, ""))` | ✅ |
| `addfiredamage_v` | [2] duration | L1=2160, L30=3240 | `dur` shared | ✅ |
| `deadlystrikeenhance_p` | [1] value | L1=5, L20=30, mode=Conic | `Link(lv, (1, 5, ""), (20, 30, "Conic"))` | ✅ |
| `deadlystrikeenhance_p` | [2] duration | L1=2160, L30=3240 | `dur` shared | ✅ |
| `lifemax_yan_p` | [1] value | L1=21, L35=20, L36=20 | `Link(lv, (1, 21, ""), (35, 20, ""))` | ✅ |
| `lifemax_yan_p` | [2] duration | L1=-1, L30=-1 (= use main) | `dur` shared (equivalent) | ✅ |
| `skill_cost_v` | [1] value | L1=50, L20=100 | `Link(lv, (1, 50, ""), (20, 100, ""))` | ✅ |
| `skill_cost_v` | [2] duration | absent (=0) | `0` | ✅ |

### Visual config (Unity `SkillEffectVisualService.cs` case 130)

From PC status mapping table Status43 (verbatim, GBK decoded):
```
Status43  \spr\skill\丐帮\mag_gb_11_醉蝶狂舞.spr  Body  Loop  4  12  16  1  19  1
```

| Field | PC value | Unity impl | Match |
|---|---|---|---|
| SPR UID | `7d34af1d` | `pcPreCastSpriteKey = "7d34af1d"` | ✅ (byte-identical to PC) |
| Total frames | 16 | `pcPreCastTotalFrames = 16` | ✅ |
| Directions | 1 | `pcPreCastDirections = 1` | ✅ |
| Frame interval | 19 ticks (~1.05s/frame) | `pcPreCastIntervalTicks = 19` | ✅ (fixed from 4 in 2026-06-16 audit) |
| Aura frame range | start=4, end=12 | `pcAuraFrameStart = 4`, `pcAuraFrameEnd = 12` | ✅ |
| Body type | Body | `isAura = true` | ✅ |
| Play mode | Loop | default Loop | ✅ |

### Mobile adaptations (explicitly marked, NOT fabricated)

- `auraDuration = 4f` — PC buff lasts 120-180s with continuous body aura;
  mobile shows 4s visual flash. Buff mechanics (AllResP, AddFireDamageV,
  etc.) still full 120-180s via state system. Only the visual animation is
  shortened for UX/battery.

### Version-mismatch note

PC `skills.txt` line 1053 has skill 130 mapped to `xiaopiaosan` (Phiến Tán)
with `meleedamagereturn_p` and `rangedamagereturn_p`. This contradicts the
PC `gaibang.lua` which defines skill 130 as `zuidie_kuangwu` with the
butterfly buff. Authoritative source: `gaibang.lua` (matches the Cái Bang
skill tree expected by the rest of the PC client).

## Unity files modified in this audit

- `Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs` (lines 155-164, 188-199)
  - Skill 127 formula, Skill 130 formula (5 attributes verified)
- `Assets/Scripts/Sandbox/SkillEffectVisualService.cs` (case 130)
  - Changed `pcPreCastIntervalTicks` from 4 to 19 (PC value)
  - Added full PC source trace comments
- `Assets/Scripts/Sandbox/SkillEffectVisualService.cs` (case 127)
  - Brief PreCast animation (no body aura)

## Test verification

- 24/24 `CaiBangCombatParityTests` passed (2026-06-16)
- 12/12 `CaiBangSkillPanelTests` passed (2026-06-16)
- 0 compile errors
- SPR files verified byte-identical to PC `pak_unpacked`

## Related limitations

See `docs/PC_SOURCE_LIMITATIONS.md` for engine-level values that are
not in the available PC source dump (cast time formula, network opcodes,
dash duration).
