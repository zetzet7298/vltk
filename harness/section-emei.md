# Section 2.6. Nga My (PC: EMei) — Gap Summary

> Source: section 2.6 of `/var/www/vltk-mobile.worktrees/all-sect-dash/.harness/baocao-all-sect-skills.md`

## Scope
- Mobile: `CreateEMeiSkills()` 16 entries (ID 77, 79-93, thiếu 78 + 94)
- PC: `emei.lua` 18 skills + 19 per-skill files `emei/*.lua`
- C++: `KNpc.cpp::CastMeleeSkill` (1829-1891) — không có Nga My skill nào thuộc nhánh dash/jump
- **Kết luận**: Nga My 100% ranged magic (no melee, no dash) — G1 (dash) **N/A**

## Gaps by ID, severity, effort

| # | ID | Tên | Gap | Severity | Effort |
|---|---|---|---|---|---|
| 1 | **80** | Phiêu Tuyết Xuyên Vân | Registry radius sai (240 vs PC 320-384) + thiếu addskilldamage1-3 chain (91/380/1062) | Cao | 1 giờ + nửa ngày |
| 2 | 81 | Thu Phong Diệp | Sai `StaminaMaxP` → `StaminaReplenishV` (rate vs max) | TB | 1 giờ |
| 3 | **82** | Tứ Tượng Đồng Quy | Registry radius sai (570 vs PC 384-416) + thiếu addskilldamage1-2 (331/1062) | Cao | 1 giờ + nửa ngày |
| 4 | 83 | Vọng Nguyệt | Sai schema `ManaReplenishV` → `physicsenhance_p+colddamage_v+deadlystrike_p` (3 attribute) | TB | 1 giờ |
| 5 | **84** | Phong Vũ Phiêu Hương | Sai schema nghiêm trọng `AddDefenseV` (defense buff) → `SlowMissleB` (anti-missile debuff) + `targetSelf` sai | TB | 2 giờ |
| 6 | **85** | Nhất Diệp Tri Thu | Registry radius sai (180 vs PC 320-384) + thiếu addskilldamage1-4 (328/88/1061/1091) | Cao | 1 giờ + 1 ngày |
| 7 | 86 | Lưu Thủy | Sai `AttackSpeedV` → `FastWalkRunP` (move speed) | TB | 1 giờ |
| 8 | **88** | Bất Diệt Bất Tuyệt | Registry radius sai (360 vs PC 448-512) + thiếu addskilldamage1-2 (328/1061) | Cao | 1 giờ + nửa ngày |
| 9 | 89 | Mộng Điệp | Sai `AddDefenseV` → `LifeReplenishV` (mất 50% heal HP) | TB | 1 giờ |
| 10 | 90 | Mê Tung Ảo Ảnh | Schema drift 5-field → 1-field (mất 4 attribute: fasthitrecover_v, fatallystrikeres_p, 3 timer reduce). G5 cross-faction `kunlun.lua` | Thấp-TB | 2 giờ |
| 11 | 91 | Phật Quang Phổ Chiếu | Thiếu addskilldamage2-3 (380/1062) | TB | nửa ngày |
| 12 | 92 | Phật Tâm Từ Hữu | Sai `AllResP` → `LifeMaxP + LifeMaxYanP` (HP max, không phải all res) | TB | 1 giờ |
| 13 | **93** | Từ Hàng Phổ Độ | Sai schema `ManaReplenishV` → `LifeReplenishV` (HP heal, không phải mana) | **Cao** (skill chữa trị chính) | 30 phút |
| 14 | **ALL 5 attack registry** | 80/82/85/88/91 | Registry radius sai 5/5 (240/570/180/360/400) | **Cao** (runtime AOE sai 38-62%) | 30 phút (1 lần sửa) |
| 15 | **All 8 attack** | 80/82/85/88/91 | Thiếu addskilldamage1-4 chain 0/8 | Cao (mất 25-50% damage) | 2-3 ngày |
| 16 | Tuning coverage | — | 5/13 active trong registry, thiếu 90/93/94 | Thấp | 15 phút |

## Phase plan
- **Phase 1 quick wins**: 10 items, ~1-2 ngày. Priority: registry radius (1 lần sửa) + 93 heal + 84 anti-missile + 5 schema drift
- **Phase 3 dash**: **N/A** (Nga My không có dash)
- **Phase 4 event chain**: 6 items, 2-3 ngày cho addskilldamage chain + visual coverage
- **Phase 5**: 6 active 150-tier sub-form (`sane_jixue` / `fengshuang_suiying` / `qianfo_qianye` / `jianemei150` / `zhangemei150` / `jinding_foguang`) + 3 passive (`yuquan_xichen` / `emei120` / `foxin_ciyou` nếu tách riêng) — 1-2 tuần effort

## Key observations
- **Nga My là môn phái ranged thuần**, không có melee/dash (PC IsMelee=0 toàn bộ 77-94)
- **Tuning registry 5/5 entries sai** (PcSkillTuningRegistry.cs line 87-91) — đây là bug nghiêm trọng nhất, runtime sẽ dùng radius sai 38-62% so với PC
- **Schema drift 5/7 surround self-buff** (81/83/86/89/92) — sai `MagicAttributeKind`, mất cảm giác team buff combat stats đặc trưng Nga My
- **Skill 93 sai concept** — đây là heal HP mạnh nhất Nga My (LifeReplenish 750), mobile hiện cho mana regen
- **G6 addskilldamage chain 0/8 fire** — mỗi attack skill mất 25-50% damage do không fire sub-missile 91/328/331/380/1061/1062/1089/1091/88/329
- **G5 cross-faction script** — ID 90 dùng `kunlun.lua` (ModSkills.txt LvlSetScript) thay vì `emei.lua` — cần verify Phase 5
