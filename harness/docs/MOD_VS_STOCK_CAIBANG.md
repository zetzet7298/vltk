# MOD vs Stock: Cái Bang Skill Source Comparison

**Date**: 2026-06-16
**Source 1 (Stock)**: `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/` — PC original
**Source 2 (MOD)**: Google Drive `1Click Mel 7 Jun 2026` — Hội Quán Võ Lâm Offline
**Purpose**: Document which skill formulas in MOD differ from stock PC original

## File comparison

| File | Stock (PC gốc) | MOD (VN community) | Diff |
|---|---|---|---|
| `gaibang.lua` size | 18088 bytes | 16741 bytes | MOD **1347 bytes NHỎ HƠN** |
| `gaibang.lua` timestamp | 2011-11-02 | 2026-05-31 | MOD mới hơn 15 năm |
| Encoding | GBK | GBK | Same |
| Comments | Tiếng Trung | Tiếng Việt | Translated |
| Skill count | 15 | 22 | MOD +7 skills (endgame/150) |

## Skill 127 (Hoạt Bất Lưu Thủ / huabu_liushou)

**GIỐNG HỆT 100%** — không có sự khác biệt nào.

| Attribute | Stock 2011 | MOD 2026 |
|---|---|---|
| `fastwalkrun_p[1]` | L1=9, L20=66 | L1=9, L20=66 |
| `fastwalkrun_p[2]` | L1=18×120, L20=18×180 | L1=18×120, L20=18×180 |
| `skill_cost_v[1]` | L1=24, L20=50 | L1=24, L20=50 |

**Verdict**: Unity code 100% correct for this skill in BOTH sources.

## Skill 130 (Túy Điệp Cuồng Vũ / zuidie_kuangwu) — MODIFIED

| Attribute | Stock 2011 | MOD 2026 | Change |
|---|---|---|---|
| `allres_p[1]` | L1=1, L30=30 | L1=1, L30=30 | ✅ same |
| `allres_p[2]` (duration) | L1=18×120, L30=18×180 | L1=18×120, L30=18×180 | ✅ same |
| `addfiremagic_v[1]` | L1=10, L30=**215** | L1=10, L30=**315** | 🔴 **+100 buff** |
| `addfiremagic_v[2]` (duration) | L1=18×120, L30=18×180 | L1=18×120, L30=18×180 | ✅ same |
| `addfiredamage_v[1]` | L1=10, L30=175 | L1=10, L30=175 | ✅ same |
| `addfiredamage_v[2]` (duration) | L1=18×120, L30=18×180 | L1=18×120, L30=18×180 | ✅ same |
| `deadlystrikeenhance_p[1]` | L1=5, L20=30, Conic | L1=5, L20=30, Conic | ✅ same |
| `deadlystrikeenhance_p[2]` (duration) | L1=18×120, L30=18×180 | L1=18×120, L30=18×180 | ✅ same |
| `lifemax_yan_p[1]` | L1=21, L35=20, L36=20 | **REMOVED** | 🔴 removed |
| `lifemax_yan_p[2]` (duration) | L1=-1, L30=-1 | n/a | n/a |
| `returnres_p[1]` | n/a | L1=5, L30=**30** | 🟢 **+new attribute** |
| `returnres_p[2]` (duration) | n/a | L1=18×120, L30=18×180 | 🟢 new |
| `skill_cost_v[1]` | L1=50, L20=100 | L1=50, L20=100 | ✅ same |

**Verdict**: Unity code (current) uses **Stock 2011 values**, which is the PC original.
If porting MOD instead: would need to change `addfiremagic_v` max from 215→315,
remove `lifemax_yan_p`, add `returnres_p` 5→30.

## Other Cái Bang skills — MOD buffs damage

Most damage skills in MOD have higher `addskilldamage1[3]` trigger chance:

| Skill | Stock trigger | MOD trigger | Change |
|---|---|---|---|
| `yanmen_tuobo` (122) | 1→40 | 1→80 | +100% |
| `jianren_shenshou` (121) | 1→50 | 1→150 | +200% |
| `bangda_egou` (117) | 1→60 | 1→120 | +100% |
| `kanglong_youhui` (128) | 1→55 | 1→105 | +91% |

Also `gaibang_zhangfa` (skill 116): `addfiremagic_v` 25→275 (stock) vs 25→**475** (MOD).

## Skills exclusive to MOD (not in stock)

These 7 skills only exist in MOD 2026, not in stock 2011:

1. `philongtaithien_new` (line 127) — Phi Long Tại Thiên (New) — Chưởng 90
   - `seriesdamage_p`, `firedamage_v[1]=10→15→1050`, `missle_speed_v`,
     `skill_misslenum_v`, `skill_attackradius`, `skill_cost_v`, `addskillexp1`
2. `tuyetdinhngudieu` (line 174) — Tuyệt Đỉnh Ngũ Điếu
   - `seriesdamage_p`, `firedamage_v[1]=20→400`
3. `tuyetdinhphilong` (line 182) — Tuyệt Đỉnh Phi Long
   - `seriesdamage_p`, `firedamage_v[1]=10→300→850`, missle/num/radius
4. `zhanggaibang150` (line 193) — Skill 150 Cái Bang (chưởng) — endgame
5. `zhanggaibang150_2` (line 243) — Skill 150 Cái Bang (chưởng) v2
6. `gungaibang150` (line 295) — Skill 150 Cái Bang (bổng) — endgame
7. `gaibang120` (line 332) — Hắc Thiên Khí Công
8. `gaibang120zuzhou` (line 371) — Hắc Thiên Khí Công (chú thú)

## MOD identity (from Google Doc)

- Project name: "Hội Quán Võ Lâm Offline" (bản Linux 6.0)
- Author: "BachKim" base + community modifications
- Released: 2026-06-07
- Acknowledgments (from `1QTpUYqW6K-WGTAu9Izf5rmIQadjQ4U17Y932x9L3Kxc`):
  - Vinh Ttn: 1ClickVMFull VM image
  - Tiêu Quyền: "BachKim" Linux 6.0 base
  - Kei Ushiro: VLTK 6 DLL mods
  - Võ Khánh: miniskill DLL mods + CentOS/WinXP VM
  - Kiều Tôn Sơn: font + VSCode setup
  - HNT from Voz: server script features
  - Ricckyy from Voz: 1ClickVMFull app
  - Cong Le: map "Kiếm Thế" → "Hội Quán Võ Lâm" conversion
  - Điền Cnc: horse SPRs + skill making tutorial
  - Dev Cùi Bắp: SPRs (not yet added)

## Decision: Unity uses Stock 2011

The Unity implementation at
`/var/www/vltk-mobile/Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs` (lines 155-164, 188-199)
uses **Stock 2011 values** which match PC original 100%.

This is the correct choice for "100% PC-accurate" port:
- Stock 2011 = PC original source (the goal)
- MOD 2026 = community-modified offline server (not the source of truth)

If a future decision is made to port MOD values instead, the changes needed are:
1. Skill 130: `addfiremagic_v` 215→315
2. Skill 130: remove `lifemax_yan_p`, add `returnres_p` 5→30
3. Skills 116, 121, 122, 128: update trigger chances and damage values
4. Add 7 new MOD-exclusive skills (150, 120 endgame)

## File paths

- Stock: `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/Server/script/skill/gaibang.lua`
- MOD: `/var/www/vltk-mobile/cache/server_offline/jxser/server1/script/skill/gaibang.lua`
- Google Drive source folder: `1x24JOS_RcK3bo1cfp5hqBHThybDCZuHu` (1Click Mel 7 Jun 2026)
- MOD download URL: `https://drive.usercontent.google.com/download?id=1Utsd5fqpOdBbZzxhw60kfFSiEtFgfZMH&export=download&confirm=t`
- Password: `123456` (encoded in filename as `-123456`)
