# Section 2.3 — Thiếu Lâm (PC: Shaolin 少林, ID range 3-21, loại trừ 5, 7)

> Gap-analysis subagent output. Scope: **Thiếu Lâm** (ID 3-21, loại 5/7).
> Nguồn: `Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs::CreateShaolinSkills()` +
> `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/Server/script/skill/shaolin.lua`
> + `script/skill1/shaolin.lua` (saolin cập nhật, pinyin TCVN3) +
> `script/skill/shaolin/*.lua` (11 per-skill pinyin) +
> `script/skill/saolin/*.lua` (19 per-skill GB2312, không có `mohe-wuliang.lua`) +
> `KNpc.cpp::CastMeleeSkill` switch line 1834-1891.

---

## Tóm tắt executive

- **17 skill** trong catalog mobile (3, 4, 6, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21)
- **7 passive mastery** (3, 4, 6, 8, 9, 12, 21) + **4 self buff/state** (13, 15, 16, 18) + **6 ranged active** (10, 11, 14, 17, 19, 20)
- **G1 (dash) N/A** — không có Shaolin skill 3-21 nào là `Melee + Jump` theo `KNpc.cpp::CastMeleeSkill` switch
- **G2 (sub-skill gated)** — không phát hiện (per-skill `luohan-zhen.lua` có L10+ gate, nhưng main file thắng)
- **G4 (childSkillNum)** — **CỰC KỲ NGHIÊM TRỌNG**: 4/6 active skill mất 1-5 sub-skill `addskilldamage*` (10 mất 5/6, 19 childSkillId sai, 14 mất 6/6, 17 mất 3/4, 11 mất 1/2)
- **G5 (id↔name swap)** — nghi ngờ ID 9 (Hỗn Nguyên Nhất Khí Công vs PC `luohan_fumo`) và ID 13 (Lập Địa Thành Phật vs PC `清心梵音` Thanh Tâm Phạn Âm)
- **G6 (event chain)** — 11 (`hengsao_liuhe`) và 20 (`shizi_hou`) có `skill_eventskilllevel` trong per-skill file, mobile không handle
- **G7 (Tuning)** — `PcSkillTuningRegistry.ShaolinId` 5 entry (10, 11, 14, 16, 19), 3 entry sai: [10]=90 vs PC 54 (off 67%), [11]=90 vs PC 96 (off 6%), [19]=200 vs PC L20=512 (off 156%); thiếu 17, 20 (coverage 83%)

## Files retrieved

1. `Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs` (line 814-1071) — `CreateShaolinSkills` + 17 method
2. `Assets/Scripts/Sandbox/PcSkillTuningRegistry.cs` (line 34-41) — Shaolin radius curves
3. `Assets/Scripts/Sandbox/SkillEffectVisualService.cs` (line 535-583) — `ConfigureShaolinVisuals` (6/6 active + 4 buff)
4. `Assets/Scripts/Sandbox/SkillEffectVisualService.cs` (line 887) — `SetupPcMissile` signature (`speedPerTick`, `lifeTicks` từ PC `missle_lifetime_v`)
5. `Assets/StreamingAssets/Reference/KNpc.cpp` (line 1829-1891) — `CastMeleeSkill` switch (5 melee types, không có Shaolin)
6. `Assets/Tests/EditMode/Sandbox/ShaolinSkillPanelTests.cs` — 17 skill test fixture
7. `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/.../script/skill/shaolin.lua` — main PC gốc (GB2312, 450 dòng)
8. `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/.../script/skill1/shaolin.lua` — saolin cập nhật (TCVN3 pinyin comments)
9. `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/.../script/skill/shaolin/*.lua` — 11 per-skill pinyin (TCVN3)
10. `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/.../script/skill/saolin/*.lua` — 19 per-skill GB2312

## Key code references

- `PcCombatCatalogFactory.cs:814-833` — `CreateShaolinSkills()` (17 entries)
- `PcCombatCatalogFactory.cs:887-891` — `ShaolinHunyuanYiqi` (ID 9)
- `PcCombatCatalogFactory.cs:893-907` — `ShaolinJingangFumo` (ID 10, child=1056)
- `PcCombatCatalogFactory.cs:909-926` — `ShaolinHengsaoLiuhe` (ID 11, child=319)
- `PcCombatCatalogFactory.cs:947-963` — `ShaolinHanglongBayu` (ID 14, child=66)
- `PcCombatCatalogFactory.cs:995-1012` — `ShaolinLongzhaoHuzhua` (ID 17, child=218)
- `PcCombatCatalogFactory.cs:1028-1042` — `ShaolinMoheWuliang` (ID 19, child=61)
- `PcCombatCatalogFactory.cs:1044-1057` — `ShaolinShiziHou` (ID 20, child=77)
- `PcSkillTuningRegistry.cs:34-41` — Shaolin radius curves (5 entry, 3 sai)

## Gap list (ID, gap, severity, effort)

| # | ID | Gap | Severity | Effort |
|---|---|---|---|---|
| 1 | 10 | childSkillId=1056 chỉ fire 1/6 sub-skill (321, 319, 11, 19, 1057 miss) | Cao | 2 giờ |
| 2 | 10 | radius 400 vs PC 54 (sai 7.4×) | Cao | 5 phút |
| 3 | 10 | registry [10]=90 vs PC 54 (off 67%) | TB | 5 phút |
| 4 | 11 | childSkillId=319 chỉ fire 1/2 sub-skill (1056 miss) | TB | 1 giờ |
| 5 | 11 | registry [11]=90 vs PC 96 (off 6%) | Thấp | 5 phút |
| 6 | 11 | `skill_eventskilllevel` không handle (G6) | Thấp-TB | 1-2 giờ |
| 7 | 14 | childSkillId=66 có thể sai (không khớp PC addskilldamage pattern) | TB | 1 giờ verify |
| 8 | 14 | 6/6 sub-skill MISSING (318, 317, 271, 272, 1083, 1055) | Cao | 2-3 giờ |
| 9 | 17 | childSkillId=218 chỉ fire 1/4 sub-skill (318, 317, 1083, 1055 miss) | TB | 1 giờ |
| 10 | 17 | registry thiếu entry 17 (mặc dù PC có data) | Thấp | 15 phút |
| 11 | 19 | childSkillId=61 sai (PC sub-damages là 321 và 1057) | Cao | 1 giờ |
| 12 | 19 | registry [19]=200 vs PC L20=512 (off 156%) | Cao | 5 phút |
| 13 | 20 | `skill_eventskilllevel` không handle (G6) | Thấp-TB | 1-2 giờ |
| 14 | 20 | registry thiếu entry 20 | Thấp | 15 phút |
| 15 | 9 | name "Hỗn Nguyên Nhất Khí Công" không có trong PC (G5 — `luohan_fumo`?) | TB | 30 phút verify |
| 16 | 13 | name "Lập Địa Thành Phật" không có trong PC (G5 — `清心梵音` Thanh Tâm Phạn Âm) | Thấp | 30 phút verify |
| 17 | 3 | slot verify — mobile dùng values per-skill quyền cho kiếm | TB | 30 phút verify |
| 18 | 16 | per-skill `luohan-zhen.lua` L10+ gate conflict vs main (mobile match main, OK) | Thấp | 1 giờ verify |
| 19 | 18 | per-skill `huiyan-zhou.lua` (pinyin) conflict vs `skill/saolin/慧眼咒.lua` (mobile dùng saolin = đúng) | Thấp | 1 giờ verify |
| 20 | All | registry-catalog-PC mismatch 2/5 (10 sai 7.4×, 19 sai 156%) | TB | 30 phút |
| 21 | All | Tuning coverage 83% (thiếu 17, 20 + sửa 10, 11, 19) | Thấp | 30 phút |

## Tổng kết

- **Top 3 priority (Cao)**: #1 (10/6 sub-skill), #8 (14/6 sub-skill), #11+12 (19/childSkillId+registry)
- **Effort tổng Phase 1**: ~10-12 giờ (fix 12 quick-win items)
- **Effort tổng Phase 4 (event chain)**: ~3-6 giờ
- **Phase 3 dash**: N/A
- **Phase 5 (future)**: 8 sub-form 150-tier (damo_dujiang, rulai_qianye, quanshaolin150, hengsao_qianjun, gunshaolin150, wuxiang_zhan, daoshaolin150, dachengrulaizhou) — trong đó `daoshaolin150` có event chain (priority Phase 5 cao nhất)

## Trạng thái

- [x] Catalog scan xong
- [x] Gap table viết xong
- [x] Section appended to `baocao-all-sect-skills.md`
- [ ] Quick-win phase merged
- [ ] Dash phase merged: N/A
- [ ] Event chain phase merged

---

**Section appended to**: `/var/www/vltk-mobile.worktrees/all-sect-dash/.harness/baocao-all-sect-skills.md` (line 460+)
