# Section 2.5. Ngũ Độc (PC: WuDu, ID range 60-76)

## Nguồn PC đã rà

- `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/Server/script/skill/wudu.lua` (GB2312, 613 dòng, 25+ skills)
- `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/Server/script/skill/wudu/*.lua` (26 file per-skill; 9 pinyin + 17 Chinese GBK)
- `Assets/StreamingAssets/Reference/ModSkills.txt` (canonical TCVN3, ID 60-76 trừ 61, attackradius + isMelee + charAnimId)
- `Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs` line 1704-1932 (`CreateWuDuSkills` 16 skill)

## Tóm tắt sect

Ngũ Độc = môn phái **poison ranged** thuần. 16 skill:
- 5 passive mastery/buff (60, 62, 66, 75 = mastery; 76 = self buff)
- 3 ranged missile độc (63, 65, 68) + 3 ranged surround độc (69, 71, 74) + 4 ranged AOE state (64, 67, 70, 72, 73)
- **Không có melee nào** → `IsMelee=1` chỉ áp dụng cho 63/65/68/74 (gán `skillStyle=Missiles` chứ không phải Melee)
- `KNpc.cpp::CastMeleeSkill` switch (line 1829-1891) — **không có** WuDu skill nào → **G1 (dash) N/A**
- Range 60-76 **không có** `skill_startevent` / `skill_flyevent` / `skill_collideevent` / `skill_showevent` (event chain chỉ có ở sub-form 150-tier `yinfeng_shigu` (1094), `xuanyin_zhan` (1096), `zhangwudu150`, `daowudu150` — ngoài scope) → **G6 N/A**
- Tất cả 5 skill missile độc chính (63/65/68/69/71/74) trong PC có `addskilldamage*` tham chiếu 353/354/355/71/1066/1094/1095/1096 (skill-interaction multiplier) — **mobile KHÔNG có cơ chế `addskilldamage`** (G6/G7 toàn cục)

## 2.5.1. Catalog scan

| ID | Tên (suy ra) | Loại PC | Vai trò |
|---:|---|---|---|
| 60 | 五毒刀法 Ngũ Độc Đao Pháp | Passive mastery | Buff physics dmg + crit |
| 62 | 五毒掌法 Ngũ Độc Chưởng Pháp | Passive mastery | Buff poison magic V |
| 63 | 毒砂掌 Độc Sa Chưởng | Ranged missile độc | Active — base poison missile (ReqLv 10) |
| 64 | 冰蓝玄晶 Băng Lam Huyền Tinh | Ranged AOE state (cold) | Active — debuff self cold res (ReqLv 10) |
| 65 | 血刀毒杀 Huyết Đao Độc Sát | Ranged missile độc | Active — phys + poison (ReqLv 10) |
| 66 | 杂难药经 Tạp Nan Dược Kinh | Passive mastery | Buff poison res P |
| 67 | 九霄狂雷 Cửu Thiên Cuồng Lôi | Ranged AOE state (lighting) | Active — debuff self lighting res (ReqLv 20) |
| 68 | 幽冥骷髅 U Minh Khô Lâu | Ranged missile độc | Active — poison + series (ReqLv 30) |
| 69 | 无形蛊 Vô Hình Độc | Ranged surround độc | Active — poison + movement speed (ReqLv 30) |
| 70 | 赤焰蚀天 Chích Dương Thệ Thiên | Ranged AOE state (fire) | Active — debuff self fire res (ReqLv 30) |
| 71 | 天罡地煞 Thiên Cương Địa Sát | Ranged surround độc | Active — big poison + series (ReqLv 60) |
| 72 | 穿心毒刺 Xuyên Tâm Độc Thích | Ranged AOE state (poison) | Active — debuff self poison res (ReqLv 20) |
| 73 | 万毒蚀心 Vạn Độc Thực Tâm | Ranged AOE state (poison) | Active — **extend poison duration on target** (ReqLv 20) |
| 74 | 朱蛤清鸣 Chu Cáp Thanh Minh | Ranged missile độc | Active — phys+poison+series (ReqLv 60) |
| 75 | 五毒奇经 Ngũ Độc Kỳ Kinh | Passive mastery | Buff poison magic V + cast speed V (ReqLv 60) |
| 76 | 移花接木 Di Hoa Tiếp Ngọc | Self buff | Active — return ranged damage (ReqLv 50) |

**Không catalog trong mobile** (PC `wudu.lua` có, mobile thiếu — ngoài range 60-76): 26 per-skill files nằm ngoài scope, bao gồm sub-form 150-tier `zhangwudu150` / `daowudu150` / `xuanyin_zhan` (1096) / `yinfeng_shigu` (1094) / `baidu_chuanxin` (1095) / `xueshou_dusha` (sister 65) và 120-tier `wudu120` (có `autoattackskill` = 719*256+1 → caster buff). Phase 5.

**ID 61** (PC ModSkills.txt có tên "Ngũ Độc Bổng Pháp ##" — disabled/bỏ). Mobile đã exclude đúng (line 599 `IsWuDuSkill id != 61`).

## 2.5.2. Gap table

> **Cột "Hành vi mobile"**: `CreateWuDuSkills` line 1704-1932.
> **Cột "Hành vi PC"**: `wudu.lua` (curves per level) + per-skill files (`*.lua` subdir) + `ModSkills.txt` (radius/IsMelee/anim) + `wudu.lua::SKILLS.<key>.skill_attackradius` (radius theo level).

| ID | Tên | Hành vi mobile | Hành vi PC | Gap | Severity | Effort |
|---:|---|---|---|---|---|---|
| 63 | Độc Sa Chưởng | `radius=180, child=5, num=1, PoisonDamageV 15/150 (P1+P3), SeriesDamageP 1/10, cost=10` | PC wudu.lua: `poisondamage_v={{1,2},{20,40}},{{1,60},{20,60}},{{1,10},{20,10}}` (3-element per-tick model), `seriesdamage_p={{1,1},{20,10}}`, `skill_attackradius={{1,320},{20,384}}` → L20=**384** | **G4 radius 180 vs PC 384 (53% off — RẤT LỚN)** + **G7 PoisonDamageV 15/150 vs PC 2/40 (PC max per-tick 60+60+10 = 130, mobile 150 tổng ≈ OK, nhưng schema 3-element bị nén thành 2-element sai)** | Cao | 1 giờ |
| 65 | Huyết Đao Độc Sát | `radius=400 ✓, PhysicsEnhanceP 10/100, PoisonDamageV 15/150, SeriesDamageP 1/10, cost=20` | PC wudu.lua: `physicsenhance_p={{1,15},{20,65}}`, `poisondamage_v={{1,4},{20,11}},{{1,60},{20,60}},{{1,10},{20,10}}`, `skill_attackradius={{1,320},{20,384}}` | **G7 PhysicsEnhanceP 10/100 vs PC 15/65 (L20 1.5× off)** + **G7 PoisonDamageV 15/150 vs PC 4/11 (L20 14× off — SCHEMA 3-ELEMENT bị nén sai)** + **G7 mất `physicsdamage_v` raw 30→70 (per-skill `xueshou-dusha.lua`)** | Cao | 2 giờ |
| 68 | U Minh Khô Lâu | `radius=400, PoisonDamageV 30/250, SeriesDamageP 5/30, cost=15` | PC wudu.lua: `poisondamage_v={{1,11},{20,40}},{{1,60},{20,60}},{{1,10},{20,10}}`, `seriesdamage_p={{1,5},{20,30}}`, `skill_attackradius={{1,384},{20,448}}` → L20=**448** | **G4 radius 400 vs PC 448 (11% off)** + **G7 PoisonDamageV 30/250 vs PC 11/40 (L20 6× off — schema 3-element nén sai)** | Cao | 2 giờ |
| 69 | Vô Hình Độc | `radius=100, PoisonDamageV 25/220, AttackSpeedV 5/30, cost=15` | PC wudu.lua: `poisondamage_v={{1,5},{20,25}}` (L20 max 25) + `fastwalkrun_p={{1,-10},{25,-50}}` (movement speed buff). Per-skill `wuxing-gu.lua`: `Getfastwalkrun_p` = `-5-level, -20, -25` (L1=-6, L20=-25). `skill_attackradius` không set. | **G7 AttackSpeedV 5/30 ≠ PC fastwalkrun_p -10/-50 (MOVEMENT speed ≠ ATTACK speed — đổi nhầm class attribute)** + **G7 PoisonDamageV 25/220 vs PC 5/25 (L20 9× off)** + **G4 radius 100 không có PC anchor** | **Cao** (di chuyển ≠ tấn công — gameplay cốt lõi "tàng hình lao tới") | 2 giờ |
| 71 | Thiên Cương Địa Sát | `radius=420, PoisonDamageV 50/385, SeriesDamageP 10/50, cost=20→40` | PC wudu.lua: `poisondamage_v={{1,50},{20,135}}` (L20 max 135), `seriesdamage_p={{1,10},{20,50}}` ✓, `skill_attackradius={{1,448},{20,480}}` → L20=**480** | **G4 radius 420 vs PC 480 (12% off)** + **G7 PoisonDamageV 50/385 vs PC 50/135 (L20 ~3× off — magnitude lớn nhất trong range)** | Cao | 1 giờ |
| 74 | Chu Cáp Thanh Minh | `radius=400, PhysicsEnhanceP 80/385, PoisonDamageV 50/385, SeriesDamageP 10/50, cost=25` | PC wudu.lua: `physicsenhance_p={{1,30},{20,392}}` (L20 close 392 ✓), `poisondamage_v={{1,16},{20,53}}` (L20=53, mobile 385 = **7× off**), `seriesdamage_p={{1,10},{20,50},{21,52}}` ✓, `skill_attackradius={{1,448},{20,512},{21,512}}` → L20=**512** | **G4 radius 400 vs PC 512 (22% off)** + **G7 PoisonDamageV 50/385 vs PC 16/53 (7× off — magnitude lớn)** | Cao | 2 giờ |
| 62 | Ngũ Độc Chưởng Pháp (passive) | `AddPoisonDamageV 15/515, baseLv=10` | PC wudu.lua: `addpoisonmagic_v={{1,15},{20,45}},{{1,-1},{2,-1}},{{1,10},{2,10}}` (P1=15/45, P3=duration=10). Per-skill `wudu-zhangfa.lua` `Getaddpoisonmagic_v = 5+level*2` (L1=7, L20=45). | **G7 AddPoisonDamageV L20=515 vs PC 45 (11× off — magnitude cực lớn)** + **G7 mất `addphysicsdamage_p` raw 13+7*lv (per-skill)** | **Cao** (passive mastery bị sai 11× → 1 môn phái cả đời tăng sai damage) | 1 giờ |
| 75 | Ngũ Độc Kỳ Kinh (passive) | `AddPoisonDamageV 20/200, CastSpeedV 5/30, baseLv=60, maxLv=30` | PC wudu.lua: `addpoisonmagic_v={{1,5},{30,45}}` (P1 only, L30=45) + `poisonenhance_p={{1,12},{30,50},{33,50},{35,50},{38,50}}` (poison damage % enhance) + `castspeed_v={{1,1},{30,25},{38,25},{39,26}}` (L30=25). Per-skill `wudu-qijing.lua` chỉ set `poisonenhance_p = 10+2*level`. | **G7 AddPoisonDamageV 20/200 vs PC 5/45 (L30 4.4× off)** + **G7 mất `poisonenhance_p` 12→50 (poison damage %)** + **G7 CastSpeedV 5/30 vs PC 1/25 (L30 sai số 1.2× nhưng L1 5× off)** | Trung bình-cao | 2 giờ |
| 73 | Vạn Độc Thực Tâm | `radius=440, PoisonResP -10/-40 (debuff self poison res), cost=20` | PC wudu.lua: `poisontimereduce_p={{1,-200},{20,-300}},{{1,18*45},{20,18*120}}` — **EXTEND poison duration on target** (theo comment trong wudu.lua line 132-135: "âm càng nhiều = thời gian dính độc càng lâu"). Per-skill `wangu-shixin.lua` THÊM `poisonres_p` với `result1 = -floor(log10(level+1)/2*60)` (L1=-9, L20=-23). | **G5 — ĐỔI NHẦM ATTRIBUTE CLASS: PC là `poisontimereduce_p` (kéo dài độc trên target), mobile là `PoisonResP` (debuff self res độc). Tên "Vạn Độc Thực Tâm" = mục tiêu ăn độc lâu hơn, KHÔNG phải debuff res.** + **G7 mất `poisontimereduce_p` HOÀN TOÀN** + **G7 poisonres_p value -10/-40 vs per-skill -9/-23 (L20 sai số 2×)** | **Cao (G5 đổi class gameplay)** | 2 giờ (cần thêm `PoisonTimeReduceP` attribute kind hoặc reuse `PoisonEnhanceP` với sign) |
| 60 | Ngũ Độc Đao Pháp (passive) | `AddPhysicsDamageP 15/215, DeadlyStrikeEnhanceP 6/25, baseLv=10` | PC wudu.lua: `addphysicsdamage_p={{1,20},{20,180}},{{1,-1},{2,-1}},{{1,1},{2,1}}` + `deadlystrikeenhance_p={{1,6},{20,25}},{{1,-1},{2,-1}}` | **G7 AddPhysicsDamageP 15/215 vs PC 20/180 (mobile L1 sai 25%, L20 sai 19%)** + **G7 thiếu curve giữa các cấp (hard-code 1→20)** | Thấp-TB | 30 phút |
| 64 | Băng Lam Huyền Tinh | `radius=440, ColdResP -5/-25, cost=20, baseLv=10` | PC wudu.lua: `coldres_p={{1,-9},{20,-49}},{{1,18*20},{20,18*90}}` | **G7 ColdResP -5/-25 vs PC -9/-49 (L20 2× weaker debuff)** | Thấp | 15 phút |
| 66 | Tạp Nan Dược Kinh (passive) | `PoisonResP 10/60, baseLv=20` | PC wudu.lua: `poisonres_p={{1,9},{20,39}},{{1,-1},{2,-1}}` | **G7 PoisonResP 10/60 vs PC 9/39 (L20 1.5× off)** | Thấp | 15 phút |
| 67 | Cửu Thiên Cuồng Lôi | `radius=440, LightingResP -5/-25, cost=20, baseLv=20` | PC wudu.lua: `lightingres_p={{1,-9},{20,-49}},{{1,18*20},{20,18*90}}` | **G7 LightingResP -5/-25 vs PC -9/-49 (L20 2× weaker debuff)** | Thấp | 15 phút |
| 70 | Chích Dương Thệ Thiên | `radius=440, FireResP -5/-25, cost=20, baseLv=30` | PC wudu.lua: `fireres_p={{1,-9},{20,-49}},{{1,18*20},{20,18*90}}` | **G7 FireResP -5/-25 vs PC -9/-49 (L20 2× weaker debuff)** | Thấp | 15 phút |
| 72 | Xuyên Tâm Độc Thích | `radius=440, PoisonResP -5/-25, cost=20, baseLv=20` | PC wudu.lua: `poisonres_p={{1,-29},{20,-49}},{{1,18*20},{20,18*90}}` | **G7 PoisonResP -5/-25 vs PC -29/-49 (L1 sai 5.8×, L20 sai 2× — magnitude lệch LỚN nhất trong 4 state debuff)** | Thấp-TB | 15 phút |
| 76 | Di Hoa Tiếp Ngọc | `radius=400, RangeDamageReturnP 10/50, cost=30, targetSelf` | PC ModSkills.txt: `radius=0, isMelee=7 (state buff)`. PC wudu.lua không có entry cho 76 (ngoài range `wudu.lua::SKILLS`); per-skill `rangedamagereturn_p` trong `/wudu/` (filename GBK) cho thấy effect đúng. | **G4 radius 400 vs PC 0 (buff không cần radius — nhưng mobile dùng để AOE state)** + **G7 magnitude chưa verify (PC per-skill cần đọc để so sánh)** | Thấp | 30 phút |
| 63, 65, 68, 71, 74 | (ranged missile) | mobile dùng `childSkillId=5, childSkillNum=1, baseSkill=true` (template default) | PC 63 có `addskilldamage1-5` referencing 353/71/1066/1094/1096 (skill-interaction multiplier). PC 65/68/71/74 tương tự. | **G6/G7 — `addskilldamage` mechanism MISSING (không chỉ WuDu, là toàn cục mobile). Nếu caster có skill 353/71/1066/1094/1096 đồng thời → damage nhân hệ số 1.5-2×, mobile không apply → 63/65/68/71/74 damage thiếu.** | TB (gameplay) | Phase 5 (cần thêm `addskilldamage` engine support) |
| **Tất cả 7 ranged AOE (63/64/65/67/68/70/71/72/73/74)** | `PcSkillTuningRegistry.RadiusCurves[WuDuId]` | KHÔNG CÓ (line 1704-1932 không gọi registry; `CreateWuDuSkills` 16 skill, registry `RadiusCurves[WuDuId]` = 0 entries) | PC có 9 active attack cần curve | **G7 — Tuning coverage 0% (0/9 active attack trong registry)** | TB (runtime radius sai nếu dùng default thay vì curve) | 1 ngày (thêm 9-10 entries vào `PcSkillTuningRegistry.RadiusCurves[WuDuId]`) |
| 63/65/68/71/74 | `ConfigureWuDuVisuals` | KHÔNG CÓ (chỉ có `CreateWuDuSkills`, không có visual config function) | PC có pre-cast SPR / missile SPR riêng cho mỗi skill wudu (5 missile + 4 AOE state) | **G7 — Visual coverage 0%** (chỉ dùng default SPR, không có wudu-specific pre-cast/missile SPR) | TB (visual thô, mất cảm giác Ngũ Độc) | 1-2 ngày (cần tham chiếu wudu sub-spr pack + `ConfigureWuDuVisuals` switch) |
| 1066 / 1067 | (PC sub-form 150-tier referenced as `addskilldamage` source in wudu.lua) | KHÔNG CÓ trong mobile catalog (line 1704-1932 + line 1934+ = CuiYan, không phải 1066/1067) | PC wudu.lua line 24/71/94/141/162/220: `addskilldamage3 = [1]={{1,1066},{2,1066}}, [3]={{1,1},{20,80}}` và `addskilldamage3 = [1]={{1,1067},{2,1067}}`. 1066 = "Hình Tiêu Cốt Lập", 1067 = "U Hồn Phệ Ảnh" (per task brief) — sub-form 150-tier, ngoài scope 60-76. | **Không phải gap trong range 60-76** — chỉ thiếu 1066/1067 là vì ngoài range, không ảnh hưởng catalog 60-76 hiện tại. Phase 5. | N/A (scope) | Phase 5 |
| 71/73/67/70 (task brief focus) | (dash/event chain check) | N/A — không có dash; không có event chain trong range 60-76 | PC 71/73/67/70 không có `skill_startevent/flyevent/collideevent/showevent`; 71/74/68/65/63 missile không có sub-event (sub-form 150-tier mới có) | **G1 N/A** + **G6 N/A** cho range 60-76 | N/A | — |

## 2.5.3. Phase 1 quick wins (G4 radius + G7 magnitude critical bugs)

> **Highest priority**: 73 (G5 attribute class swap), 62 (passive 11× off), 69 (movement ≠ attack), 71/74/65/68 (poison damage 3-14× off do schema 3-element nén sai).

- [ ] **ID 73 Vạn Độc Thực Tâm — G5 attribute swap (CRITICAL)**: thay `PoisonResP -10/-40` → `PoisonTimeReduceP -200/-300` (PC `poisontimereduce_p`, âm = kéo dài độc trên target). Cần verify `MagicAttributeKind` có `PoisonTimeReduceP` chưa; nếu chưa, thêm enum + runtime. (G5, 2 giờ — bao gồm cả test "độc trên target kéo dài từ 10s lên ~30s")
- [ ] **ID 62 Ngũ Độc Chưởng Pháp — passive damage sai 11×**: sửa `AddPoisonDamageV 15/515` → `Link(lv, (1, 5, ""), (20, 45, ""))` (per-skill `wudu-zhangfa.lua` = `5+level*2`). Đồng thời thêm `addphysicsdamage_p 13+7*lv` raw (per-skill line 19) — hiện mobile thiếu hẳn attribute này. (G7, 1 giờ)
- [ ] **ID 69 Vô Hình Độc — đổi nhầm class attribute (CRITICAL gameplay)**: thay `AttackSpeedV 5/30` → `FastWalkRunP -10/-50` (PC `fastwalkrun_p` line 121-122, âm = tăng movement speed). Cần verify `MagicAttributeKind.FastWalkRunP` đã có; nếu chưa thêm enum. Sửa `PoisonDamageV 25/220` → `Link(lv, (1, 5, ""), (20, 25, ""))` (PC). (G7, 2 giờ — gameplay core: tàng hình + lao tới)
- [ ] **ID 71 Thiên Cương Địa Sát — radius + poison damage**: sửa `radius=420` → `480` (PC L20). Sửa `PoisonDamageV 50/385` → `Link(lv, (1, 50, ""), (20, 135, ""))` (PC). (G4+G7, 1 giờ)
- [ ] **ID 74 Chu Cáp Thanh Minh — radius + poison damage**: sửa `radius=400` → `512` (PC L20). Sửa `PoisonDamageV 50/385` → `Link(lv, (1, 16, ""), (20, 53, ""))` (PC). (G4+G7, 2 giờ — magnitude lệnh 7×)
- [ ] **ID 65 Huyết Đao Độc Sát — poison damage schema 3-element nén sai**: sửa `PoisonDamageV 15/150` → dùng `Link(lv, (1, 4, ""), (20, 11, ""))` cho P1, P3=duration=10. Nếu schema magic-attribute hiện tại không hỗ trợ 3-element, dùng `Link(lv, (1, 4, ""), (20, 11, ""))` cho P1, `Link(lv, (1, 60, ""), (20, 60, ""))` cho P2 (per-tick max), duration = 10. Sửa `PhysicsEnhanceP 10/100` → `15/65` (PC). (G7, 2 giờ)
- [ ] **ID 68 U Minh Khô Lâu — radius + poison damage**: sửa `radius=400` → `448` (PC L20). Sửa `PoisonDamageV 30/250` → `Link(lv, (1, 11, ""), (20, 40, ""))` (PC P1). (G4+G7, 2 giờ)
- [ ] **ID 63 Độc Sa Chưởng — radius sai 53% (CRITICAL)**: sửa `radius=180` → `384` (PC L20). (G4 Cao, 30 phút)
- [ ] **ID 60 Ngũ Độc Đao Pháp — passive physics damage**: sửa `AddPhysicsDamageP 15/215` → `Link(lv, (1, 20, ""), (20, 180, ""))` (PC). (G7 thấp, 30 phút)
- [ ] **ID 75 Ngũ Độc Kỳ Kinh — passive missing 2 attributes**: thêm `PoisonEnhanceP 12/50` (PC `poisonenhance_p`); sửa `AddPoisonDamageV 20/200` → `Link(lv, (1, 5, ""), (30, 45, ""))` (PC); sửa `CastSpeedV 5/30` → `Link(lv, (1, 1, ""), (30, 25, ""))` (PC). (G7, 2 giờ)
- [ ] **ID 64/67/70/72 — magnitude sai 2× debuff**: sửa 4 state debuff `ResP -5/-25` → `Link(lv, (1, -9, ""), (20, -49, ""))` (PC). Riêng 72: `Link(lv, (1, -29, ""), (20, -49, ""))` (L1 khởi đầu -29 mạnh hơn). (G7 thấp-TB, 30 phút tổng)
- [ ] **ID 66 Tạp Nan Dược Kinh — passive magnitude**: sửa `PoisonResP 10/60` → `Link(lv, (1, 9, ""), (20, 39, ""))` (PC). (G7 thấp, 15 phút)
- [ ] **ID 76 Di Hoa Tiếp Ngọc — radius 0**: cân nhắc sửa `radius=400` → `0` (PC state buff không cần AOE). Effort: 5 phút nếu agree.

## 2.5.4. Phase 2 tuning (G7 — 0% coverage)

- [ ] **Tuning coverage 0% → 100%**: thêm 9-10 entries vào `PcSkillTuningRegistry.RadiusCurves[WuDuId]`:
  - `[63] = new[] { (1, 320), (20, 384) }` (PC L1=320, L20=384)
  - `[64] = new[] { (1, 320), (20, 384) }` (PC coldres_aoe)
  - `[65] = new[] { (1, 320), (20, 384) }` (PC)
  - `[67] = new[] { (1, 320), (20, 384) }` (PC lightingres_aoe)
  - `[68] = new[] { (1, 384), (20, 448) }` (PC)
  - `[69] = new[] { (1, 0), (20, 0) }` (PC không set, default 0; mobile hiện 100 sai)
  - `[70] = new[] { (1, 320), (20, 384) }` (PC fireres_aoe)
  - `[71] = new[] { (1, 448), (20, 480) }` (PC)
  - `[72] = new[] { (1, 320), (20, 384) }` (PC poisonres_aoe)
  - `[73] = new[] { (1, 320), (20, 384) }` (PC)
  - `[74] = new[] { (1, 448), (20, 512), (21, 512) }` (PC)
  - (G7, 1 ngày — cần tham chiếu cách WuDangId / TianWangId đã làm)

## 2.5.5. Phase 3 dash (G1)

- [ ] **Không áp dụng** — Ngũ Độc không có skill dash/melee-jump. `KNpc.cpp::CastMeleeSkill` switch (line 1829-1891) và `wudu.lua` (range 60-76) đều không tham chiếu `Melee_Jump/JumpAndAttack/RunAndAttack`. Tất cả 16 skill là passive/ranged, không có dash.

## 2.5.6. Phase 4 event chain (G6)

- [ ] **Không áp dụng** cho range 60-76 — PC `wudu.lua` không có `skill_startevent/flyevent/collideevent/showevent` cho 60-76. Chỉ sub-form 150-tier `yinfeng_shigu` (1094) + `xuanyin_zhan` (1096) + `zhangwudu150` + `daowudu150` mới có event chain — ngoài scope task này. Phase 5.
- [ ] **Tuy nhiên**: cả 5 skill missile độc (63/65/68/71/74) đều có `addskilldamage*` referencing 353/354/355/71/1066/1094/1096. Cơ chế `addskilldamage` MISSING toàn cục mobile (không riêng WuDu). Nếu mobile muốn đúng PC behavior, cần implement `addskilldamage` engine. Effort: Phase 5 (toàn dự án, không chỉ WuDu).

## 2.5.7. Trạng thái

- [x] Catalog scan xong (16 skill: 4 passive mastery + 1 self buff + 5 ranged missile + 4 ranged surround + 4 ranged AOE state debuff)
- [ ] Quick-win phase merged (Phase 1 — 13 items, priority: 73 G5 attribute swap + 69 G7 class swap + 62 passive 11× off + 71/74/65/68 magnitude + 63 radius 53%)
- [ ] Dash phase merged: **không áp dụng** (Ngũ Độc không có dash)
- [ ] Event chain phase merged: **không áp dụng** cho range 60-76 (sub-form 150-tier Phase 5)
- [ ] Tuning coverage 0% → 100% (cần thêm 10 entries vào `PcSkillTuningRegistry.RadiusCurves[WuDuId]`)
- [ ] Visual coverage 0% → có `ConfigureWuDuVisuals` (5 missile + 4 AOE state debuff)

## 2.5.8. Tổng kết

- **3 gap nghiêm trọng nhất (Cao + đổi class gameplay)**:
  1. **ID 73 G5** — "Vạn Độc Thực Tâm" đổi nhầm `PoisonTimeReduceP` (kéo dài độc trên target) → `PoisonResP` (debuff self res). Tên kỹ năng gợi "target ăn độc lâu hơn" — implement hiện tại hoàn toàn sai class. Effort: 2 giờ.
  2. **ID 69 G7** — "Vô Hình Độc" đổi `FastWalkRunP` (movement speed buff = "tàng hình lao tới") → `AttackSpeedV` (attack speed). Gameplay cốt lõi mất. Effort: 2 giờ.
  3. **ID 62 G7** — Passive `Ngũ Độc Chưởng Pháp` `AddPoisonDamageV 15/515` vs PC `5/45` (L20 sai 11×). Cả đời nhân vật tăng sai damage gấp 11. Effort: 1 giờ.
- **5 gap magnitude poison damage** (63/65/68/71/74): schema 3-element `{{1,2},{20,40}},{{1,60},{20,60}},{{1,10},{20,10}}` (per-tick min, per-tick max, duration) bị nén thành 2-element `(1,X,20,Y)` sai semantics. Cần verify mobile có schema 3-element chưa; nếu chưa thì 5 skill này cần refactor.
- **G4 radius lệch**: 63 (180 vs 384 = -53%), 74 (400 vs 512 = -22%), 71 (420 vs 480 = -12%), 68 (400 vs 448 = -11%). Cao nhất là 63.
- **G7 state debuff magnitude** (64/67/70/72): -5/-25 vs PC -9/-49. L20 sai 2×, không ảnh hưởng gameplay lớn nhưng nhất quán.
- **G7 passive 60/75/66**: 1.5-4.4× off magnitude. Fix dễ.
- **G7 tuning coverage 0%**: 10 entries radius thiếu trong `PcSkillTuningRegistry`. Cần thêm function `ConfigureWuDuTuning` (theo template `ConfigureWuDangTuning`).
- **G7 visual coverage 0%**: không có `ConfigureWuDuVisuals`. Cần thêm function này, tham chiếu wudu sub-spr pack.
- **Phase 5 (future)**: port sub-form 150-tier `wuxing-gu` (sister 69) / `xueshou-dusha` (sister 65) / `xuanyin_zhan` (1096) / `yinfeng_shigu` (1094) / `baidu_chuanxin` (1095) / `zhangwudu150` / `daowudu150` / `wudu120` từ PC `wudu.lua` line 142-401. Có event chain (yinfeng_shigu có `skill_flyevent`+`skill_vanishedevent`+`skill_showevent`; xuanyin_zhan có `skill_collideevent`+`skill_showevent`; zhangwudu150/daowudu150 có `skill_collideevent`+`skill_showevent`). Cần check ID trong `PcSkills.txt` (ngoài range 60-76 — có thể 700+).
