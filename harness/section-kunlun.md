# Côn Luân (KunLun) — Gap Analysis

## Scope
- Mobile catalog: `CreateKunLunSkills()` line 2403-2673, ID 167-184 (18 entry)
- Cross-scope: ID 90 Mê Tung Ảo Ảnh (PC: KunLun, mobile: EMei)
- PC source: `kunlun.lua` (GB18030, 408 dòng) + `kunlun/*.lua` (33 file per-skill: 14 pinyin + 19 Chinese GBK)
- C++: `KNpc.cpp::CastMeleeSkill` switch — KunLun KHÔNG có dash skill

## Catalog scan (18 + 1 cross)

| ID | Tên | Style | Child | Vai trò |
|---:|---|---|---|---|
| 90 | Mê Tung Ảo Ảnh (PC: KunLun, mobile: EMei) | Missiles | 20 | **G5 misplaced** |
| 167 | Côn Lôn Đao Pháp | PassivityNpcState | — | Phys dmg + crit mastery |
| 168 | Côn Lôn Kiếm Pháp | PassivityNpcState | — | Lighting magic V mastery |
| 169 | Hô Phong Pháp | Missiles | 14, num=1 | Base lighting |
| 170 | Đại Lãng Thực Không | InitiativeNpcState | — | Fire res P buff |
| 171 | Thanh Phong Phù | Missiles | 19, num=1 | Fastwalkrun buff self |
| **172** | Thiên Tế Tấn Lôi | Missiles | 15, num=1 | **PC: StartEvent=1/399** |
| 173 | Thiên Thanh Địa Trọc | PassivityNpcState | — | 4 res buff self |
| 174 | Ki Bán Phù | Missiles | 20, num=1 | Fastwalkrun - debuff enemy |
| 175 | Khi Hàn Ngạo Tuyết | Missiles | 20, num=1 | Castspeed - debuff enemy |
| **176** | Cuồng Phong Sậu Điện | Missiles | 16, num=1 | Lighting + stun + addskilldamage→373/1108 |
| 177 | Bách Xuyên Nạp Hải | InitiativeNpcState | — | Cold+phys res buff |
| 178 | Nhất Khí Tam Thanh | PassivityNpcState | — | Phys dmg + crit buff |
| 179 | Cuồng Lôi Chấn Địa | Missiles | 17, num=1 | Lighting + addskilldamage→375/182/1109 |
| 180 | Độc Tê Tị Tà | InitiativeNpcState | — | Poison res P buff |
| 181 | Khí Tâm Phù | Missiles | 22, num=1 | Stun |
| 182 | Ngũ Lôi Chánh Pháp | Missiles | 18, num=4 | Big lighting + 4-fan + addskilldamage→375/1109 |
| **183** | Tuế Nguyệt Vô Tình | Missiles | 23, num=1 | **G5 attribute swap (slowmissle_b vs atk/cast speed)** |
| 184 | Kim Thiền Thoát Xác | InitiativeNpcState | — | Phys res P buff |

## Gap findings (risk-ranked)

### Cao (CRITICAL)
| ID | Gap | Severity | Effort |
|---:|---|---|---|
| **90** | G5 — ID 90 misplaced ở EMei (PC `lvlSetScript=kunlun.lua`); `emei.lua` không có `mizhong_huanying`. Mobile `IsEMeiSkill(90)=true`. | Cao | 1 giờ |
| **172** | G6 — PC `StartEvent=1/399` (ModSkills.txt). Mobile runtime không handle. Damage/visual mất. | Cao | nửa ngày |
| **172** | G4 — radius 384 vs PC L20=448 (-14%) | Cao | 15 phút |
| **183** | G5 — attribute class MISMATCH: PC `slowmissle_b` (missile speed slow), mobile `AttackSpeedV+CastSpeedV` (atk/cast speed). Tên "Tuế Nguyệt" = slow years, gợi ý slow effect. `MagicAttributeKind` không có `SlowMissle`. | Cao | 1-2 ngày |
| **183** | G4 — radius 400 vs PC 180 (+122% — sai lớn nhất) | Cao | 15 phút |
| **183** | G4 — waitTime 0 vs PC 5 | Cao | 1 phút |
| **176** | G4 — radius 448 vs PC L20=512 (-13%) | TB-Cao | 15 phút |
| **170/180/184** | G7 — magnitude không có PC anchor chính xác (main file + per-skill không tìm thấy). Mobile dùng per-skill sister schema. | TB-Cao | 2-4 giờ tổng |

### Trung bình
| ID | Gap | Severity | Effort |
|---:|---|---|---|
| **Tuning coverage** | G7 — 4/18 entry trong `PcSkillTuningRegistry.RadiusCurves[KunLunId]`; 3/4 entry SAI: 169=400 (PC 320, +25%), 172=570 (PC 448, +27%), 178=570 (PC 440, +30%). Cần thêm 14 entry + sửa 3 entry. | Cao | 1 ngày |
| **Visual coverage** | G7 — 0% (không có `ConfigureKunLunVisuals` trong `SkillEffectVisualService.cs`). Mọi visual dựa vào data-driven fallback. | TB | 1-2 ngày |
| 171/173/174/175/178 | G4 — radius 400 vs PC 440 (-9%) × 5 skill | Thấp-TB | 15 phút tổng |
| 179/181/182 | G4 — radius lệch -7% đến -9% | Thấp-TB | 15 phút tổng |
| 181 | G4 — waitTime 0 vs PC 2 | Thấp | 1 phút |
| 179/182 | G4 — ModSkills.txt mslsForm=6 (Surround) vs mobile default. Cần check `MslsGenerateData`. | TB | 1 giờ verify |
| 179/181/182/176 | G7 — `addskilldamage` mechanism THIẾU toàn cục (75% damage từ sister skills 373/375/1108/1109/182 mất). Phase 5 toàn dự án. | TB | Phase 5 |
| 167 | G7 — thiếu `AttackRatingEnhanceP` từ per-skill (chung file với 168). Cần verify per-skill áp dụng cho 167. | Thấp-TB | 1 giờ verify |

### Thấp
| ID | Gap | Severity | Effort |
|---:|---|---|---|
| 177 | G4 — radius 400 vs PC 0 (buff, optional fix) | Thấp | 1 phút |
| 168 | OK (match PC main `addlightingmagic_v` via `AddLightingDamageV`) | — | — |
| 169/175/176/181/182/183/184 | CharAnimId match PC (11 cho active, 14 cho passive) | — | — |
| Tất cả 18 entry | `isMelee=0` (no dash) — **G1 N/A** | — | — |
| Tất cả 17 entry (trừ 172) | Không có event chain trong PC range 167-184 — **G6 N/A** (chỉ 172 có StartEvent) | — | — |

## Total summary
- **Ca**: 1 fix G5 (ID 90 misplaced), 1 fix G5 (ID 183 attribute swap), 1 fix G6 (ID 172 StartEvent) — tổng effort ~3-4 ngày
- **TB**: 1 fix G7 (tuning coverage 22%→100%, 1 ngày), 1 fix G7 (visual coverage 0%→100%, 1-2 ngày), 7 fix G4 radius (15 phút tổng)
- **Thấp**: 1 verify G7 (167 AttackRatingEnhanceP, 1 giờ)
- **Total Phase 1 effort**: ~3-4 ngày (excluding Phase 5 addskilldamage toàn cục)

## Key file references
- `Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs:2403-2673` — CreateKunLunSkills
- `Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs:1408-1419` — EMeiMeTungAoAnh (where ID 90 wrongly sits)
- `Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs:34-35,597,602` — `IsEMeiSkill` (line 597), `IsKunLunSkill` (line 602)
- `Assets/Scripts/Sandbox/PcSkillTuningRegistry.cs:108-114` — KunLunId radius curves (only 4/18)
- `Assets/Scripts/Sandbox/SkillEffectVisualService.cs:281,290,455-466,501,625,1036,1079` — no `ConfigureKunLunVisuals`
- `/var/www/vltksource_new/vl_update_27/Server 6.0/server/home_jxser_bachkim_6.0/server1/script/skill/kunlun.lua` — PC main (GB18030)
- `/var/www/vltksource_new/vl_update_27/Server 6.0/server/home_jxser_bachkim_6.0/server1/script/skill/kunlun/*.lua` — 33 per-skill files
- `Assets/StreamingAssets/Reference/ModSkills.txt` — TCVN3 canonical PC data (SkillId 167-184, 90, 372, 373, 375, 376, 386, 387, 1080, 1081, 1108, 1109)
- `Assets/StreamingAssets/Reference/KNpc.cpp:1829-1891` — `CastMeleeSkill` switch (KunLun KHÔNG có dash)

## Status
- [x] Catalog scan xong (18 entry + 1 cross-scope ID 90)
- [ ] Quick-win phase merged (3 CRITICAL: 90 misplaced, 183 attribute swap, 172 StartEvent)
- [ ] Dash phase merged: N/A (KunLun không có dash)
- [ ] Event chain phase merged: 1 item (ID 172 StartEvent → 399)
- [ ] Tuning coverage 22% → 100% (cần thêm/sửa 14 entry trong PcSkillTuningRegistry)
- [ ] Visual coverage 0% → 100% (cần tạo ConfigureKunLunVisuals)
