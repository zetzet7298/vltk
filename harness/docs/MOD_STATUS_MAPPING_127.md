# MOD Client Status Mapping — Hoạt Bất Lưu Thủ Discovery

**Date**: 2026-06-16
**Source**: `Client_Offline-123456.rar` from Google Drive `1x24JOS_RcK3bo1cfp5hqBHThybDCZuHu`
**Extracted file**: `×´Ì¬Óë¹âÐ§Í¼ÐÎ¶ÔÕÕ±í.txt` (20817 bytes, 249 lines, GBK encoded)
**Password**: `123456`

## Critical finding: MOD adds visual state for Skill 127

The PC stock `×´Ì¬Óë¹âÐ§Í¼ÐÎ¶ÔÕÕ±í.txt` (in `/var/www/vltksource_new/vl_update_27/Client 6.0/settings/`)
has NO status entry for skill 127 (Hoạt Bất Lưu Thủ). The skill is cast with
brief PreCast animation only.

The MOD version adds **3 status entries** that reference Hoạt Bất Lưu Thủ:

### Status17 — kl_10 variant
```
Status17  \spr\skill\昆仑\kl_10_滑不留手.spr  Body  Loop  4  12  15  1  12  1  Hoạt Bất Lưu Thủ
```
- **SPR**: `kl_10_滑不留手.spr` (from Côn Lôn path)
- **Type**: Body
- **PlayMode**: Loop
- **Frame range**: 4-12
- **Total frames**: 15
- **Direction**: 1
- **Interval**: 12 ticks
- **Description**: Hoạt Bất Lưu Thủ

### Status43 — zuidie_kuangwu
```
Status43  \spr\skill\丐帮\mag_gb_11_醉蝶狂舞.spr  Body  Loop  4  12  16  1  19  1  Túy Điệp Cuồng Vũ
```
- **SPR**: `mag_gb_11_醉蝶狂舞.spr` (UID 7d34af1d, verified byte-identical to stock)
- **Type**: Body
- **Frame range**: 4-12
- **Total frames**: 16
- **Interval**: 19 ticks
- **Note**: Identical to stock

### Status57 — gb static variant
```
Status57  \spr\skill\gb\滑不留手.spr  Body  Loop  0  0  15  1  12  1  Hoạt Bất Lưu Thủ
```
- **SPR**: `gb\滑不留手.spr` (from Cái Bang path)
- **Type**: Body
- **Frame range**: 0-0 (static, single frame)
- **Total frames**: 15
- **Interval**: 12 ticks

## Interpretation

The MOD has 3 different visual states for "Hoạt Bất Lưu Thủ":
1. **Status17**: Uses Côn Lôn sprite (likely shared/cloned from Côn Lôn skill)
2. **Status43**: Same as Túy Điệp (probably a copy-paste error or alternate alias)
3. **Status57**: Cái Bang's own sprite, static

The stock PC original (2011) has **none of these** — confirming that the original
game had no body aura for skill 127. The MOD community added visual polish.

## Implications for Unity port

| Source | Skill 127 visual | Skill 130 visual |
|---|---|---|
| Stock 2011 (PC original) | No state, brief PreCast only | Status43 body aura, frames 4-12, interval 19 |
| MOD 2026 (community) | Status17/43/57 body aura (added) | Status43 body aura (same as stock) |

**Current Unity implementation** (in `SkillEffectVisualService.cs` case 127):
- Brief PreCast animation with `3cae8f47` (`mag_tr_16_施魔法.spr`)
- No body aura
- Status43 (case 130): body aura with `7d34af1d` (`mag_gb_11_醉蝶狂舞.spr`)

This matches **stock PC behavior** 100% — which is the correct interpretation
of "100% PC-accurate". The MOD's added aura for skill 127 is community polish,
not PC original.

## Additional MOD script files

The MOD client has files NOT in stock PC client:

| File | Purpose |
|---|---|
| `gaibang-zhangfa.lua` | Extra Cái Bang Chưởng Pháp (not in stock 2011) |
| `gaibangboss.txt` | Cái Bang boss data (not in stock) |
| `newskilldesc.txt` | MOD skill descriptions |
| `newskill_explimit.txt` | MOD skill exp limits |
| `skill120exp.lua` | Skill level 120 exp |
| `skillwood.lua` | Skill wood system |
| `skillstate.lua` | Skill state tracking |
| `init_skill.ini` | Skill init config |
| `MiniSkill.ini` | Mini skill bar config |
| `skilltemplate.txt` | Skill template |

## Client vs Server MOD consistency

Both MOD client and MOD server have **identical** `gaibang.lua`:
- Same file size: 16741 bytes
- Same line count: 467
- Same content: skills 127/130 with same MOD values
- Comments in Vietnamese

This confirms MOD client and server are from the same release.

## Decision (unchanged)

Unity code uses **stock 2011** values (PC original), which is the correct
choice for "100% PC-accurate" port. MOD values are community modifications,
not PC source of truth.

If a future decision is made to port MOD values:
- Skill 127 visual: add body aura with Status17 SPR (`kl_10_滑不留手.spr`)
  or Status57 SPR (`gb\滑不留手.spr`)
- Skill 130: update `addfiremagic_v` 215→315, remove `lifemax_yan_p`,
  add `returnres_p` 5→30

## Files referenced

- MOD status mapping: `/var/www/vltk-mobile/cache/client_offline_extracted/×´Ì¬Óë¹âÐ§Í¼ÐÎ¶ÔÕÕ±í.txt`
- MOD skill formulas: `/var/www/vltk-mobile/cache/client_offline_extracted/gaibang.lua`
- Stock status mapping: `/var/www/vltksource_new/vl_update_27/Client 6.0/settings/×´Ì¬Óë¹âÐ§Í¼ÐÎ¶ÔÕÕ±í.txt`
- Stock skill formulas: `/var/www/vltksource_new/vl_update_27/Client 6.0/script/skill/gaibang.lua`
