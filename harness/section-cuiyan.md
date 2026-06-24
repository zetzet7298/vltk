## 2.7. Thúy Yên (PC: CuiYan 翠烟, ID range 95-114)

> **Nguồn PC**:
> - `/var/www/vltksource_new/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/Server/script/skill/cuiyan.lua` (GB18030, 23 skills incl. 150-tier + 120-tier)
> - Per-skill files: `cuiyan/{cuiyan-daofa,cuiyan-shuangdao,binggu-xuexin,bingxin-qianying,fengjuan-canxue,muye-liuxing,taxue-wuhen,xueying}.lua` (8 file, mỗi file override 1 attribute)
> - `Assets/StreamingAssets/Reference/ModSkills.txt` (canonical, TCVN3, SkillId 95-114, 19 skills — trừ 96/98 cùng pattern passive mastery)
> - C++: `KNpc.cpp::CastMeleeSkill` switch (line 1829-1891). **CuiYan 100% IsMelee=0** (xác nhận từ ModSkills.txt) → không thuộc nhánh Melee_Jump/JumpAndAttack/RunAndAttack. **G1 (dash) KHÔNG áp dụng** cho CuiYan.
> - Tóm tắt: Thúy Yên là môn phái **băng + song đao ranged** (active skills đều `SkillStyle=0/2/4` + child missile 6-13), đa số là **Missiles + Surround** + childSkillId 6-13 + StartEvent chain (102→398, 111→112). Gap nặng nhất: **G4 (childSkillId sai toàn bộ)**, **G7 (passive 95/97/100/109 sai effect hoàn toàn)**, **G6 (event chain 102→398, 111→112 missing)**, **G7 (PC `addskilldamage` sub-skills 336/337/338/382/398/1063/1064/1065/1093/381 chưa register)**.

### 2.7.1. Catalog scan

| ID | Tên | Loại PC | Vai trò |
|---:|---|---|---|
| 95 | Thúy Yên Đao pháp (passive) | Passive mastery | Buff physics dmg + crit (theo `cuiyan-daofa.lua`) |
| 96 | Thúy Yên Kiếm pháp (passive ##) | Passive mastery | Alt variant — **không catalog mobile** |
| 97 | Thúy Yên Song đao (passive) | Passive mastery | Buff **cold magic** theo PC `cuiyan-shuangdao.lua` (mobile sai: physics + crit) |
| 98 | Bích Yên Kiếm pháp (passive ##) | Passive mastery | Alt variant — **không catalog mobile** |
| 99 | Phong Hoa Tuyết Nguyệt | Ranged + missile 6 | Active — child=6 + 4 addskilldamage (336/108/1063/1064) |
| 100 | Hộ Thể Hàn Băng | Self buff | PC `huti_hanbing`: meleedamagereturn_p + rangedamagereturn_p. Mobile sai: ColdResP + AddDefenseV |
| 101 | Trị liệu thuật | Ranged heal self | PC `bingxin_qianying`: lifereplenish_v 130→700. Mobile sai: ManaReplenishV (sai effect) |
| 102 | Phong Quyển Tàn Tuyết | Ranged + StartEvent=398 | Active — child=7 + StartSkillId 398 (event chain **chưa fire**) |
| 103 | Thiên Lý Băng Phong | Self buff | PC `taxue_wuhen` empty (file per-skill có `fastwalkrun_p`). Mobile sai: ColdResP + AllResP |
| 104 | Băng Hồn | Passive | **không catalog mobile** |
| **105** | **Vũ Đả Lê Hoa** | Ranged + child=8, num=4 | **PC childSkillNum=4 (mất 4-hit). Mobile: 1.** |
| 106 | Băng Tung Vô Ảnh 111 | Teleport/blink (Style=4) | PC MslsGenerate=15, AttackRadius=400. **không catalog mobile** |
| 107 | Nhiếp Tâm Thuật | Ranged self (LRSkill=2) | PC child=6, num=1, AttackRadius=180. **không catalog mobile** |
| 108 | Mục Dã Lưu Tinh | Ranged + child=9 | Active — child=9 + 3 addskilldamage (336/1063/1064). Mobile: radius 420 vs PC 480 |
| 109 | Tuyết Ảnh | Self buff | PC `xueying`: attackspeed_v + fastwalkrun_p. Mobile sai: AllResP + AddDefenseV |
| 110 | Ngũ hành độn | Ranged self | PC child=6, num=1, AttackRadius=180. **không catalog mobile** |
| **111** | **Bích Hải Triều Sinh** | Ranged + StartEvent=112 | **Active — child=10 + StartSkillId 112 (sub-skill 112 missing in mobile catalog)** |
| **112** | **Bích Hải Triều Sinh b** | Ranged AOE (StartSkillId cho 111) | **PC child=11, num=16, MslsGenerate=5. MISSING trong mobile** — event chain dead |
| 113 | Phù Vân Tán Tuyết | Ranged + child=12 | Active — child=12 + 3 addskilldamage (338/1065/1093). Mobile: cost 20 vs PC 50 |
| 114 | Băng Cốt Tuyết Tâm (passive 30) | Passive mastery | PC maxLevel=30; 7 attributes (addcoldmagic_v + addcolddamage_v + addphysicsmagic_v + deadly + fasthitrecover + coldenhance + lifemax). Mobile: 2 attributes, bỏ 5 cái |

**Không catalog trong mobile** (PC `cuiyan.lua` có, mobile thiếu):
- **96, 98** — passive mastery alt-variant "##" (faction-specific, có thể MOD)
- **104** — "Băng Hồn" passive (no LvlSet active)
- **106** — "Băng Tung Vô ảnh" teleport/blink (MslsGenerate=15, AttackRadius=400) — 150-tier-like active
- **107** — "Nhiếp Tâm Thuật" (child=6 self, AttackRadius=180)
- **110** — "Ngũ hành độn" (child=6 self, AttackRadius=180)
- **112** — "Bích Hải Triều Sinh b" (16-missile AOE; **critical: StartSkillId cho event chain 111**)
- 1063, 1064, 1065, 1093, 381 — MOD sub-skills referenced trong `addskilldamage1-4` của 99/102/105/108/111/113 (Băng Tước Hoạt Kú, Băng Ngưng Hàn Yên, Thủy Anh Man Tố, etc.) — **không có catalog nào cho 5 ID này**
- 336, 337, 338, 382, 398 — missile IDs referenced trong `addskilldamage1-2` (Băng Tung Vô Ảnh / Phong Tuyết Băng Thiên / Băng Tâm Ngọc Lăng) — có thể là missle ID, cần check `Missles.txt`
- 150-tier: `bingzong_wuying`, `bingxin_yuling`, `daocuiyan150`, `daocuiyan150_2`, `bingxin_xuelian`, `bingxin_xianzi`, `fengxue_bingtian`, `neicuiyan150`, `neicuiyan150_2` — ngoài scope task 95-114 (Phase 5)
- 120-tier: `cuiyan120` (hide + skill_mintimepercast_v) — ngoài scope (Phase 5)

### 2.7.2. Gap table

> **Chú thích cột "Hành vi mobile"**: lấy từ `CreateCuiYanSkills()` trong `PcCombatCatalogFactory.cs` line 1934-2127.
> **Chú thích cột "Hành vi PC"**: lấy từ `ModSkills.txt` (AttackRadius, ChildSkillId, ChildSkillNum, CharAnimId, MisslesForm, MslsGenerate, StartEvent/StartSkillId, ReqLevel, MaxLevel) + `cuiyan.lua` (damage curves per level) + per-skill file (formula override).

| ID | Tên | Hành vi mobile | Hành vi PC | Gap | Severity | Effort |
|---:|---|---|---|---|---|---|
| 95 | Thúy Yên Đao pháp | `AddPhysicsDamageP(15→215) + DeadlyStrikeEnhanceP(6→25 Conic)` | PC `cuiyan-daofa.lua`: `addphysicsdamage_p=13+7*level` (L1=20, L20=153) + `deadlystrikeenhance_p=5+level` (L20=25). Per-skill file override cuiyan.lua. | **G7 — L1 sai lớn (15 vs 20), L20 sai lớn (215 vs 153, +40%)** | TB (passive mastery sai damage) | 30 phút |
| 96 | Thúy Yên Kiếm pháp (##) | **không catalog** | PC ModSkills.txt: passive mastery (SkillStyle=3, charAnimId=14) — alt-variant | **G — Missing trong mobile catalog** | TB (faction variant, MOD) | 30 phút |
| 97 | Thúy Yên Song đao | `AddPhysicsDamageP(15→215) + DeadlyStrikeEnhanceP(6→25 Conic)` | PC `cuiyan-shuangdao.lua`: `addcoldmagic_v=13+7*level, time=-1,5` (L20=153, time=-1, flag=5) | **G7 — Sai effect hoàn toàn** (physics dmg + crit vs **cold magic**). Also flag 5 ≠ 0. | **Cao** (passive mastery sai school effect — băng thành vật lý) | 30 phút |
| 98 | Bích Yên Kiếm pháp (##) | **không catalog** | PC ModSkills.txt: passive mastery — alt-variant | **G — Missing trong mobile catalog** | TB | 30 phút |
| 99 | Phong Hoa Tuyết Nguyệt | `childSkillId=70, childSkillNum=1, baseSkill=true, charAnimId=2, radius=360, WaitTime=0` | PC: child=**6**, num=1, baseSkill=1, charAnimId=**11**, MisslesForm=1, AttackRadius=360, WaitTime=**5**, LvlSet `fenghua_xueyue`: physicsenhance_p(5→85), seriesdamage_p(1→10), 4×addskilldamage (336/108/1063/1064). | **G4 — childSkillId 70 vs PC 6 (sai hoàn toàn)** + **G4 charAnimId 2 vs PC 11** + **G4 waitTime 0 vs PC 5** + **G7 — mobile bỏ physicsenhance_p(5→85)** + **G — 4 addskilldamage sub-skills 336/108/1063/1064 missing** | **Cao** (multi-gap: child sai + animation sai + bỏ 1 attribute + 4 sub-skills missing) | nửa ngày |
| 100 | Hộ Thể Hàn Băng | `InitiativeNpcState, targetSelf, charAnimId=2, ColdResP(10→50) + AddDefenseV(50→450), 1200+1200*lv, cost 20, AttackRadius=400` | PC `huti_hanbing`: `meleedamagereturn_p(5→20, 18*120 dur) + rangedamagereturn_p(5→20, 18*120 dur) + skill_cost_v(40→60)`. PC AttackRadius=0 (buff, không cần). CharAnimId=11. | **G7 — Sai effect hoàn toàn** (ColdResP + AddDefense vs **damage return shield**) + **G7 cost sai (20 vs PC 40-60)** + **G4 charAnimId 2 vs PC 11** + **G4 AttackRadius 400 vs PC 0** (waste) | **Cao** (passive mastery sai data — băng trở thành tank thuần) | 1 giờ |
| 101 | Trị liệu thuật | `Missiles, childSkillId=5, num=1, baseSkill=true, charAnimId=2, targetSelf, targetAlly, ManaReplenishV(100→450), cost 50, AttackRadius=400` | PC `bingxin_qianying`: `lifereplenish_v(130→700, time=20) + skill_cost_v(20+level)`. PC child=**13**, charAnimId=**11**, WaitTime=**5**, LRSkill=2 (self). | **G7 — Sai effect hoàn toàn** (ManaReplenishV vs **LifeReplenish** = HEAL) + **G4 childSkillId 5 vs PC 13** + **G4 charAnimId 2 vs PC 11** + **G7 cost sai (50 vs PC 21-40)** + **G7 life curve sai (100-450 vs PC 130-700)** | **Cao** (heal thành mana — sai semantics gameplay) | 1 giờ |
| 102 | Phong Quyển Tàn Tuyết | `childSkillId=71, num=1, charAnimId=2, ColdDamageV(30→300, 0, 40→400), cost 15, radius=360, WaitTime=0` | PC `fengjuan_canxue`: `physicsdamage_v(25→235, 0, 25→375) + seriesdamage_p(1→10) + 4×addskilldamage (337/111/1065/1093)`. PC child=**7**, charAnimId=**11**, WaitTime=**5**, StartEvent=**1**, StartSkillId=**398**. | **G4 — childSkillId 71 vs PC 7** + **G4 charAnimId 2 vs PC 11** + **G4 waitTime 0 vs PC 5** + **G7 — Sai damage type (Cold vs Physics)** + **G7 curve sai (cold 30-300 vs PC phys 25-235)** + **G6 — StartEvent=1, StartSkillId=398 chưa fire (sub-skill 398 missing)** + **G — 4 addskilldamage sub-skills missing** | **Cao** (multi-gap: 3 G4 + 2 G7 + 1 G6 + sub-skills) | nửa ngày |
| 103 | Thiên Lý Băng Phong | `InitiativeNpcState, targetSelf, charAnimId=2, ColdResP(15→75) + AllResP(5→25), 1200+1200*lv, cost 25, AttackRadius=400` | PC `taxue_wuhen` empty trong cuiyan.lua, per-skill `taxue-wuhen.lua`: `fastwalkrun_p(15+2*level, time=1080+135*level) + skill_cost_v(20+level*4)`. PC AttackRadius=0, charAnimId=11. | **G7 — Có thể sai effect** (ColdResP + AllResP vs **fastwalkrun_p** = move speed) + **G4 charAnimId 2 vs PC 11** + **G4 AttackRadius 400 vs PC 0** + **G7 cost sai (25 vs PC 20+4*lv=24-100)** | **Cao** (nếu là move speed, gameplay thay đổi hoàn toàn — Thúy Yên thiếu kỹ năng tăng tốc) | 1 giờ verify |
| 104 | Băng Hồn | **không catalog** | PC ModSkills.txt: passive (SkillStyle=3, charAnimId=14) | **G — Missing** | Thấp (no active effect) | 30 phút |
| **105** | **Vũ Đả Lê Hoa** | `childSkillId=72, **num=1**, charAnimId=2, PhysicsEnhanceP(10→100) + ColdDamageV(30→250, 0, 30→250), cost 20, radius=300, WaitTime=0, MslsGenerate=0` | PC `yuda_lihua`: `physicsenhance_p(10→140) + seriesdamage_p(5→30) + 2×addskilldamage (382/1064)`. PC child=**8**, **num=4**, charAnimId=**11**, WaitTime=**5**, MisslesForm=6, MslsGenerate=**3**, MslsGenerateData=**10**, AttackRadius=300, LRSkill=0. | **G4 — childSkillId 72 vs PC 8** + **G4 childSkillNum=1 vs PC=4 — MẤT 4-HIT pattern** + **G4 charAnimId 2 vs PC 11** + **G4 waitTime 0 vs PC 5** + **G4 MslsGenerate=0 vs PC=3 (multi-missile)** + **G7 physicsenhance_p L20 sai (100 vs 140, -29%)** + **G — 2 addskilldamage sub-skills 382/1064 missing** | **Cao** (mất 4-hit + multi-missile — cốt lõi Vũ Đả Lê Hoa) | nửa ngày |
| 106 | Băng Tung Vô ảnh 111 | **không catalog** | PC ModSkills.txt: **SkillStyle=4** (InitiativeNpcState variant), AttackRadius=400, MslsGenerate=**15**, MslsForm=1, LRSkill=2, charAnimId=11 | **G — Missing** (teleport/blink 15-missile AOE) | TB (gap lớn, PC có 15 missile) | 2 giờ |
| 107 | Nhiếp Tâm Thuật | **không catalog** | PC ModSkills.txt: SkillStyle=0, child=6, num=1, baseSkill=1, AttackRadius=180, LRSkill=2 (self), WaitTime=5, charAnimId=11 | **G — Missing** (self-targeted ranged, 180 radius) | Thấp (small effect) | 1 giờ |
| 108 | Mục Dã Lưu Tinh | `childSkillId=73, num=1, charAnimId=2, ColdDamageV(50→385, 0, 50→385) + SeriesDamageP(10→50), cost Link(20→40), radius=420, WaitTime=0, MslsGenerate=1` | PC `muye_liuxing`: `seriesdamage_p(10→50) + physicsenhance_p(30→271) + colddamage_v(20→246, 0, 20→426) + 3×addskilldamage (336/1063/1064)`. PC child=**9**, charAnimId=**11**, AttackRadius=448→**480** (L20), MisslesForm=6, LRSkill=0. PC cost=30→40. | **G4 — childSkillId 73 vs PC 9** + **G4 charAnimId 2 vs PC 11** + **G4 radius 420 vs PC 480 (-12%)** + **G7 — bỏ PhysicsEnhanceP(30→271) hoàn toàn** + **G7 colddamage_v L20 sai (385 vs PC 246, +56%)** + **G7 cost L1 sai (20 vs PC 30, -33%)** + **G — 3 addskilldamage sub-skills missing** | **Cao** (bỏ 1 attribute + sai 2 cái khác + radius sai) | nửa ngày |
| 109 | Tuyết Ảnh | `InitiativeNpcState, targetSelf, charAnimId=2, AllResP(5→25) + AddDefenseV(50→350), 1200+1200*lv, cost 30, AttackRadius=400` | PC `xueying`: `attackspeed_v(12→65, 23,73, 25,90, 28,99, 42,111, 43,119, 44,122) + fastwalkrun_p(17→55) + skill_cost_v(40→140)`. PC AttackRadius=0, charAnimId=11, WaitTime=5, LRSkill=2. | **G7 — Sai effect hoàn toàn** (AllResP + AddDefense vs **attackspeed_v + fastwalkrun_p** = cast/atk/move speed) + **G4 charAnimId 2 vs PC 11** + **G4 waitTime 0 vs PC 5** + **G4 AttackRadius 400 vs PC 0** + **G7 cost sai (30 vs PC 40-140)** | **Cao** (passive sai data — mất cảm giác "tuyết ảnh" = move speed) | 1 giờ |
| 110 | Ngũ hành độn | **không catalog** | PC ModSkills.txt: SkillStyle=0, child=6, num=1, baseSkill=1, AttackRadius=180, LRSkill=2 (self), WaitTime=5, charAnimId=11 | **G — Missing** (self-targeted ranged, 180 radius) | Thấp | 1 giờ |
| **111** | **Bích Hải Triều Sinh** | `childSkillId=74, num=1, charAnimId=2, ColdDamageV(40→350, 0, 40→350) + SeriesDamageP(10→50), cost 25, radius=72, WaitTime=0` | PC `bihai_chaosheng`: `seriesdamage_p(10→50) + physicsdamage_v(20→200, 0, 20→200) + colddamage_v(43→704, 0, 43→1214) + 4×addskilldamage (337/338/1065/1093)`. PC child=**10**, charAnimId=**11**, AttackRadius=72, MisslesForm=7, LRSkill=0, StartEvent=**1**, StartSkillId=**112**. PC cost=65. | **G4 — childSkillId 74 vs PC 10** + **G4 charAnimId 2 vs PC 11** + **G4 waitTime 0 vs PC 1** (StartEvent) + **G7 colddamage_v L20 SAI LỚN (350 vs PC 704, -50%)** + **G7 — bỏ PhysicsDamageV(20→200)** + **G7 cost sai (25 vs PC 65, -62%)** + **G6 — StartEvent=1, StartSkillId=112 chưa fire** + **G — 4 addskilldamage sub-skills missing** | **Cao** (damage sai 50% + bỏ physics + event chain dead) | nửa ngày |
| **112** | **Bích Hải Triều Sinh b** | **không catalog** | PC ModSkills.txt: SkillStyle=0, child=**11**, num=**16**, baseSkill=1, charAnimId=11, MslsGenerate=**5**, MslsGenerateData=**1**, LRSkill=2. **Đây là StartSkillId của 111 — event chain** | **G — Missing critical** (16-missile AOE sub-skill for 111) | **Cao** (event chain dead nếu thiếu) | 1 giờ |
| 113 | Phù Vân Tán Tuyết | `childSkillId=75, num=1, charAnimId=2, PhysicsEnhanceP(40→200) + ColdDamageV(20→200, 0, 20→200) + SeriesDamageP(5→25), cost 20, radius=400, WaitTime=0` | PC `fuyun_sanxue`: `colddamage_v(40→375, 0, 40→575) + seriesdamage_p(5→30) + 3×addskilldamage (338/1065/1093)`. PC child=**12**, charAnimId=**11**, WaitTime=**5**, AttackRadius=384→**416** (L20), MisslesForm=6, LRSkill=0. PC cost=50. | **G4 — childSkillId 75 vs PC 12** + **G4 charAnimId 2 vs PC 11** + **G4 waitTime 0 vs PC 5** + **G4 radius 400 vs PC 416 (-4%)** + **G7 — Thừa PhysicsEnhanceP(40→200)** (PC không có) + **G7 colddamage_v L20 sai (200 vs PC 375, -47%)** + **G7 cost sai (20 vs PC 50, -60%)** + **G — 3 addskilldamage sub-skills missing** | **Cao** (thừa attribute + 2 cái sai + radius lệch) | nửa ngày |
| 114 | Băng Cốt Tuyết Tâm (passive 30) | `PassivityNpcState, charAnimId=14, AddColdDamageV(20→200) + CastSpeedV(5→30), MaxLevel=30` | PC `binggu_xuexin` (maxLevel=**30**): `addcoldmagic_v(60→315) + addcolddamage_v(30→275) + addphysicsmagic_v(30→275) + deadlystrikeenhance_p(5→45 Conic) + fasthitrecover_yan_v(5→49) + coldenhance_p(8→80) + lifemax_yan_p(21→20)`. | **G7 — Mobile chỉ có 2 attributes, PC có 7**: bỏ `addcoldmagic_v` (cold magic dmg), `addphysicsmagic_v` (physics magic), `deadlystrikeenhance_p` (crit), `fasthitrecover_yan_v` (smoke), `coldenhance_p` (cold enhance), `lifemax_yan_p` (life max smoke) + **G7 AddColdDamageV L30 sai (200 vs 275, -27%)** | **Cao** (mastery passive mất 5/7 attribute — game-breaking) | nửa ngày |
| **All 6 active** (99, 102, 105, 108, 111, 113) | — | `childSkillId` dùng 70-75 (mobile) | PC dùng 6, 7, 8, 9, 10, 12 | **G4 — childSkillId sai toàn bộ** (mobile internal ID ≠ PC child missile ID — visual/runtime reference sai) | **Cao** (cốt lõi missile chain) | 1 giờ (sửa 6 dòng) |
| **All 6 active** | — | `charAnimId=2` (mobile default) | PC yêu cầu **11** (tất cả) | **G4 charAnimId sai toàn bộ** | TB (animation) | 30 phút |
| **All buff** (100, 103, 109) | — | `AttackRadius=400` (mobile default) | PC AttackRadius=**0** (buff, không cần) | **G4 radius waste** (không ảnh hưởng gameplay nhưng sai data) | Thấp | 15 phút |
| **All 6 active** | — | `WaitTime=0` (mobile default) | PC WaitTime=5 (cho 99/102/105/107/108/109/110/113) | **G4 waitTime sai toàn bộ** | TB (multi-frame timing) | 30 phút |
| 102, 111 | — | `StartEvent=0` (mobile default) | PC `StartEvent=1, StartSkillId=398/112` (event chain) | **G6 — event chain chưa fire** | **Cao** (102→398 + 111→112) | 2-3 giờ |
| **Tất cả active 6** | — | `PcSkillTuningRegistry.CuiYan[ID]` cover 99/102/105/108/111/113 (6/13 active catalog) | PC có 7 active attack cần tuning (thiếu 101) | **G7 — Tuning coverage 86%** (thiếu ID 101, 15 phút) | Thấp | 15 phút |
| **Tất cả sub-skill reference** | — | Mobile **không register** 1063, 1064, 1065, 1093, 381, 336, 337, 338, 382, 398 | PC `addskilldamage1-4` references tất cả 10 ID này | **G7 — sub-skill 10 ID missing** (cần register + runtime support cho addskilldamage) | **Cao** (gameplay mất damage bonus) | 1-2 ngày |

### 2.7.3. Phase 1 quick wins (CRITICAL — childSkillId + passives)

> **Đây là phase quan trọng nhất** cho Thúy Yên. Sai childSkillId toàn bộ = 6 skill active (99, 102, 105, 108, 111, 113) không spawn đúng missile. 4 passive (95, 97, 100, 109) sai effect hoàn toàn = mastery băng → vật lý/tank.

- [ ] **ID 99** Phong Hoa Tuyết Nguyệt: sửa `childSkillId=70` → `6`; `charAnimId=2` → `11`; `waitTime=0` → `5`. (G4, 15 phút)
- [ ] **ID 101** Trị Liệu Thuật: sửa `childSkillId=5` → `13`; `charAnimId=2` → `11`; `waitTime=0` → `5`; `ManaReplenishV(100→450)` → `LifeReplenishV(130→700)` (theo PC `bingxin_qianying`); `cost 50` → `Link(lv,(1,21,""),(20,40,""))`. (G4 + G7, 1 giờ)
- [ ] **ID 102** Phong Quyển Tàn Tuyết: sửa `childSkillId=71` → `7`; `charAnimId=2` → `11`; `waitTime=0` → `5`; `ColdDamageV(30→300, 0, 40→400)` → `PhysicsDamageV(25→235, 0, 25→375)` (theo PC `fengjuan_canxue`); `cost 15` → `Link(lv,(1,20,""),(20,20,""))`. (G4 + G7, 1 giờ)
- [ ] **ID 105** Vũ Đả Lê Hoa: sửa `childSkillId=72` → `8`; `childSkillNum=1` → `4`; `charAnimId=2` → `11`; `waitTime=0` → `5`; `MslsGenerate=0` → `3`; `MslsGenerateData=0` → `10`; `PhysicsEnhanceP(10→100)` → `Link(lv,(1,10,""),(20,140,""))`. (G4, 30 phút)
- [ ] **ID 108** Mục Dã Lưu Tinh: sửa `childSkillId=73` → `9`; `charAnimId=2` → `11`; `radius=420` → `480`; thêm `PhysicsEnhanceP Link(lv,(1,30,""),(20,271,""))`; sửa `ColdDamageV(50→385)` → `Link(lv,(1,20,""),(20,246,""))` min, `Link(lv,(1,20,""),(20,426,""))` max; sửa `cost Link((1,20),(20,40))` → `Link((1,30),(20,40))`. (G4 + G7, 1 giờ)
- [ ] **ID 111** Bích Hải Triều Sinh: sửa `childSkillId=74` → `10`; `charAnimId=2` → `11`; `waitTime=0` → `1`; `ColdDamageV(40→350)` → `Link(lv,(1,43,""),(20,704,""))` min, `Link(lv,(1,43,""),(20,1214,""))` max; thêm `PhysicsDamageV Link(lv,(1,20,""),(20,200,""))`; sửa `cost 25` → `65`. (G4 + G7, 1 giờ)
- [ ] **ID 113** Phù Vân Tán Tuyết: sửa `childSkillId=75` → `12`; `charAnimId=2` → `11`; `waitTime=0` → `5`; **XÓA** `PhysicsEnhanceP(40→200)` (PC không có); sửa `ColdDamageV(20→200)` → `Link(lv,(1,40,""),(20,375,""))` min, `Link(lv,(1,40,""),(20,575,""))` max; sửa `cost 20` → `50`. (G4 + G7, 1 giờ)
- [ ] **ID 95** Thúy Yên Đao pháp: sửa `AddPhysicsDamageP(15→215)` → `13+7*level` (formula theo `cuiyan-daofa.lua::Getaddphysicsdamage_p`). (G7, 30 phút)
- [ ] **ID 97** Thúy Yên Song đao: thay toàn bộ — `AddPhysicsDamageP + DeadlyStrikeEnhanceP` → `AddColdMagicV Link(lv, (1, 13+7*level, ""), (20, 153, ""))` (theo `cuiyan-shuangdao.lua::Getaddphysicsdamage_p` trả về Param2String(13+7*level, -1, 5) — magic_v với time=-1, flag=5). **Critical: băng thành vật lý**. (G7, 1 giờ)
- [ ] **ID 100** Hộ Thể Hàn Băng: thay toàn bộ — `ColdResP + AddDefenseV` → `MeleeDamageReturnP(5→20, 18*120 dur) + RangeDamageReturnP(5→20, 18*120 dur)` (theo PC `huti_hanbing`); `cost 20` → `Link(lv,(1,40,""),(20,60,""))`; `charAnimId=2` → `11`; `radius=400` → `0`. (G4 + G7, 1 giờ)
- [ ] **ID 103** Thiên Lý Băng Phong: **VERIFY FIRST** — PC `taxue_wuhen` empty trong cuiyan.lua nhưng `taxue-wuhen.lua` có `fastwalkrun_p(15+2*level, time=1080+135*level)`. Nếu 103 thực sự là move speed → thay toàn bộ. Có thể khác 109 (cũng có buff). Ưu tiên check với `PcSkills.txt` column `SkillDesc`. (G7, 1 giờ verify + 30 phút fix)
- [ ] **ID 109** Tuyết Ảnh: thay toàn bộ — `AllResP + AddDefenseV` → `AttackSpeedV Link(lv,(1,12,""),(20,65,""))` + `FastWalkRunP Link(lv,(1,17,""),(20,55,""))` (theo PC `xueying`); `cost 30` → `Link(lv,(1,40,""),(20,140,""))`; `charAnimId=2` → `11`; `waitTime=0` → `5`; `radius=400` → `0`. (G4 + G7, 1 giờ)
- [ ] **ID 114** Băng Cốt Tuyết Tâm: thêm 5 attributes bị thiếu — `AddColdMagicV(60→315)` + `AddPhysicsMagicV(30→275)` + `DeadlyStrikeEnhanceP(5→45 Conic)` + `FastHitRecover(5→49)` + `ColdEnhanceP(8→80)` + `LifeMaxP(21→20)`; sửa `AddColdDamageV(20→200)` → `(30→275)`. (G7, 1 giờ)
- [ ] **ID 101 Tuning** (registry thiếu): thêm `[101] = new[] { (1, 400), (20, 400) }` vào `PcSkillTuningRegistry.CuiYanId`. (G7, 5 phút)

### 2.7.4. Phase 3 dash (G1)

- [ ] **Không áp dụng** — Thúy Yên không có skill dash/melee-jump. Cả `KNpc.cpp::CastMeleeSkill` switch (line 1834) và `ModSkills.txt` (IsMelee=0 cho toàn bộ ID 95-114) đều không tham chiếu `Melee_Jump/JumpAndAttack/RunAndAttack`. Toàn bộ active skill là Ranged + Missiles, không phải dash. Phase 3 bỏ qua cho Thúy Yên.

### 2.7.5. Phase 4 event chain (G6) + Missing catalog (G)

- [ ] **G6 — ID 102 → 398 StartEvent**: trong `SkillEffectVisualService.PlaySkillCast` cho 102, sau khi missile spawn, gọi `SpawnStartEvent(fx, 398)` — fire sub-skill 398 ngay khi cast. PC `fengjuan_canxue` không có `skill_startevent` trong cuiyan.lua, nhưng ModSkills.txt cột StartEvent=1, StartSkillId=398. Cần verify sub-skill 398 có tồn tại trong `Missles.txt` (có thể là sub-missile). Effort: 2 giờ
- [ ] **G6 — ID 111 → 112 StartEvent + Add to catalog**: tạo `BichHaiTrieuSinhB(112)` (child=11, num=16, charAnimId=11, MslsGenerate=5, MslsGenerateData=1, SkillMissileForm.Surround, targetEnemy). Trong `PlaySkillCast` cho 111, gọi `SpawnStartEvent(fx, 112)` ngay khi cast — fire 16-missile AOE. PC `bihai_chaosheng` không có `skill_startevent` trong cuiyan.lua, nhưng ModSkills.txt cột StartEvent=1, StartSkillId=112. Effort: 2-3 giờ (cần test 16-missile AOE pattern)
- [ ] **G — ID 96, 98**: tạo 2 passive mastery alt-variant (theo schema SkillStyle=3, charAnimId=14, không LvlSetting — chỉ metadata). Có thể map AddPhysicsDamageP hoặc AddColdMagicV tùy variant. Effort: 1 giờ
- [ ] **G — ID 104 Băng Hồn**: tạo passive (SkillStyle=3, charAnimId=14). PC không có LvlSetting active (chỉ metadata). Effort: 30 phút
- [ ] **G — ID 106 Băng Tung Vô Ảnh 111**: tạo active (SkillStyle=4, AttackRadius=400, MslsGenerate=15, MslsForm=1, childSkillId=chưa biết, charAnimId=11). PC `taxue_wuhen` empty trong cuiyan.lua — cần check `cuiyan-150/120.lua` hoặc tương đương. Nhiều khả năng đây là teleport/blink 150-tier (Phase 5) chứ không thuộc range 95-114. Effort: 2 giờ (nếu Phase 5 → skip)
- [ ] **G — ID 107 Nhiếp Tâm Thuật**: tạo active (child=6, num=1, AttackRadius=180, LRSkill=2, WaitTime=5, charAnimId=11). Effort: 1 giờ
- [ ] **G — ID 110 Ngũ hành độn**: tạo active (child=6, num=1, AttackRadius=180, LRSkill=2, WaitTime=5, charAnimId=11). Effort: 1 giờ
- [ ] **G — sub-skills 1063, 1064, 1065, 1093, 381**: tạo 5 sub-skill entry (Băng Tước Hoạt Kú, Băng Ngưng Hàn Yên, Thủy Anh Man Tố, etc.) trong catalog. Cần check Vietnamese MOD source để biết chính xác LvlSetting. Effort: nửa ngày (5 skill + verify với `ModSkills.txt` 1000+ range)
- [ ] **G — missile refs 336, 337, 338, 382, 398**: register missile trong `Missles.txt` reference + `SkillEffectVisualService` nếu cần visual riêng. Effort: 2-3 giờ (5 missile + visual + test addskilldamage runtime)

### 2.7.6. Trạng thái

- [x] Catalog scan xong (19 skill PC: 4 passive mastery + 2 passive ## + 1 heal + 1 state buff + 6 active attack + 1 teleport + 2 ranged self + 1 ranged AOE 16-missile + 1 ranged 13-skill; mobile: 13 skills — thiếu 96/98/104/106/107/110/112 = 7 IDs)
- [ ] Quick-win phase merged (Phase 1 — 13 items, priority: childSkillId sửa 6 skill + passive 95/97/100/109/114 + tuning coverage)
- [ ] Dash phase merged: **không áp dụng** (Thúy Yên không có dash)
- [ ] Event chain phase merged (Phase 4 — 2 items, 102→398 + 111→112)
- [ ] Missing catalog merged (7 IDs + 5 sub-skills + 5 missile refs) (Phase 4)
- [ ] Tuning coverage 86% → 100% (chỉ thiếu ID 101, fix 5 phút)

### 2.7.7. Tổng kết

- **Skill chính ưu tiên sửa** (theo thứ tự):
  1. **99, 102, 105, 108, 111, 113** (6 active attack) — sửa childSkillId 70-75 → 6-12 (PC). Một sửa mở ra toàn bộ missile chain. **Effort: 1 giờ tổng (6 dòng)**
  2. **105 Vũ Đả Lê Hoa** — sửa childSkillNum 1→4 + MslsGenerate 0→3 (mất 4-hit + multi-missile). **Effort: 30 phút**
  3. **95, 97, 100, 109, 114** (5 passive mastery) — sửa effect: cold magic / damage return / attackspeed / 7-attribute. **Effort: 4 giờ tổng**
  4. **101 Trị liệu thuật** — sửa ManaReplenish → LifeReplenish (heal sai thành mana regen). **Effort: 1 giờ**
  5. **108, 111, 113** (damage curve) — sửa colddamage_v L20 sai lớn + bỏ physicsenhance_p. **Effort: 2 giờ tổng**
- **Skill event chain** (G6 — quan trọng): 111→112 missing catalog → event chain dead. Cần tạo sub-skill 112 trước (16-missile AOE), rồi wire StartEvent vào `PlaySkillCast` cho 111. Tương tự 102→398 (sub-skill 398, 1-missile sub). **Effort: nửa ngày**
- **Skill passive sai data nghiêm trọng**: 97 (cold magic → physics dmg), 100 (damage return → cold res + def), 109 (atk speed + move speed → all res + def). Fix để Thúy Yên cảm giác đúng là "song đao băng" thay vì "tank vật lý".
- **Sub-skills MOD missing** (5 IDs: 1063, 1064, 1065, 1093, 381): referenced trong 6 active skill `addskilldamage1-4`. Cần register catalog + runtime support. Nếu runtime không hỗ trợ addskilldamage pattern, gameplay sẽ thiếu damage bonus từ 5 sub-skill này.
- **Test plan**: unit test cho childSkillId resolution (cast 99 → expect missile ID 6 spawn); visual test cast 105 ở close range (expect 4 hit pattern); regression test cast 111 với StartSkillId 112 (expect 16 AOE missile + 1 main); verify passive 100 (cast, take melee/range damage, expect 5-20% return).
- **Không cần làm dash** (Thúy Yên 100% IsMelee=0, không có Melee_Jump).
- **Phase 5 (future)**: port 150-tier sub-form (9 entries: bingzong_wuying, bingxin_yuling, daocuiyan150, daocuiyan150_2, bingxin_xuelian, bingxin_xianzi, fengxue_bingtian, neicuiyan150, neicuiyan150_2) + 120-tier (cuiyan120 hide skill). Ngoài range 95-114, cần check ID trong `PcSkills.txt` (có thể 700+ hoặc 1500+).
