## 2.8. Thiên Nhẫn (PC: TianRen 天忍, ID range 131-150 + 361-364 + 1075-1076)

> **Nguồn PC**:
> - `/var/www/vltksource_new/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/.../script/skill/tianren.lua` (GBK, 247 dòng, 16 skill + 150-tier `zhanren150` có `randmove` + `missle_missrate` → dash-pattern)
> - `/var/www/vltksource_new/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/.../script/skill/tianren/*.lua` (26 file per-skill; 6 pinyin ASCII: `limo-duohun`, `sanmei-zhenhuo`, `shigu-xueren`, `tianmo-jieti`, `wuxing-zhen`, `zhiyan`; 20 GBK TCVN3)
> - `Assets/StreamingAssets/Reference/ModSkills.txt` (TCVN3, canonical SkillId 131-150 + 361-364 + 1075-1076, full columns)
> - `Assets/StreamingAssets/Reference/ModMissles.txt` (child missile 54-58, 69-71, 169-171, 226, 337, 363, 366, 192 — `mv=0` stationary fire, `mv=1` homing)
> - C++: `KNpc.cpp::CastMeleeSkill` switch (line 1829-1891) — **TianRen KHÔNG có** skill nào thuộc `Melee_Jump/JumpAndAttack/RunAndAttack` (PC `IsMelee=1` cho 139/142/147/361/1075 nhưng chỉ là melee-missile, không phải dash). Tuy nhiên `zhanren150` có `randmove` → missile có thể "lunge" (chưa thấy trong PC C++ reference, có thể nằm ngoài scope task).
> - Tóm tắt: Thiên Nhẫn là môn phái **fire damage + debuff ranged/missile** thuần, **không có dash** (G1 N/A). Gap nặng nhất: **G6 event chain (sub-skill 361/362/363/364/1075/1076 MISSING toàn bộ)** + **G4 attackRadius sai nhiều skill** + **G7 lifemax_p dấu ngược (ID 150)** + **G7 PcSkillTuningRegistry thiếu 136/137/139/140/142/143/147/149/150**.

### 2.8.1. Catalog scan

| ID | Tên (Việt / Chinese) | Loại PC | Style PC | IsMelee | AttackRadius PC / Mobile | childId/N | Vai trò |
|---:|---|---|---|---:|---|---|---|
| 131 | Thiên Nhẫn Đao Pháp / 天忍刀法 | Passive mastery | 3 (passive) | 0 | 0 / 0 | 0/0 | Buff addfiremagic_v (L1: 15 → L20: 215) |
| 132 | Thiên Nhẫn Thương Pháp / 天忍矛法 | Passive mastery | 3 (passive) | 0 | 0 / 0 | 0/0 | Buff addphysicsdamage_p + attackrating + deadly |
| 133 | Thiên Nhẫn Phủ Pháp (##) | Passive mastery | 3 (passive) | 0 | 0 / — | — | **THIẾU mobile catalog** (placeholder `##`) |
| 134 | Thiên Nhẫn Chùy Pháp | Passive mastery | 3 (passive) | 0 | 0 / — | — | **THIẾU mobile catalog** |
| 135 | Tàn Dương Như Huyết / 残阳如血 | Active ranged | 0 (Missiles) | 0 | 270 / 320 | 54/1 | Base fire skill, addskilldamage1→361, addskilldamage2→142, addskilldamage3→1075 |
| 136 | Hỏa Liên Phần Hoa / 火莲焚华 | Active state | 0 (Missiles) | 0 | 440 / 400 | 20/1 | Buff: meleedamagereturn_p (âm), stateSpecialId=22 |
| 137 | Ảo Ảnh Phi Hồ / 幻影飞狐 | Active state | 0 (Missiles) | 0 | 440 / 400 | 20/1 | Buff: attackratingenhance_p (âm), stateSpecialId=26 |
| 138 | Thôi Sơn Điền Hải / 推山填海 | Active ranged | 0 (Missiles) | 0 | 400 / 350 | 55/10 | **MISSILES STAGGER** — PC timePerCast=40 + MslsGen=5/5 (mobile mất) |
| 139 | Hỗn Thủy Mạc Ngư / ?水?鱼 | Active melee-missile | 0 | **1 (melee)** | 60 / 320 | 70/1 | **RADIUS SAI LỚN** (5×) + melee bị Missiles |
| 140 | Phi Hồng Vô Tích / 飞鸿无迹 | Active state | 0 (Missiles) | 0 | 440 / 400 | 20/1 | Buff: adddefense_v (âm), stateSpecialId=27 |
| 141 | Liệt Hỏa Tình Thiên / 烈火情天 | Active ranged | 0 (Missiles) | 0 | 72 / 384 | 56/16 | **RADIUS SAI 5×** (72 vs 384), MissilesForm=3 (Surround) nhưng mobile=Single |
| 142 | Thâu Thiên Hoán Nhật / 偷天换日 | Active melee-missile | 0 | **1 (melee)** | 60 / 78 | 69/1 | Melee bị Missiles; steal life/mana; missle_lifetime_v=4 (mobile không có) |
| 143 | Lịch Ma Đoạt Hồn / 厉魔夺魄 | Active state | 0 (Missiles) | 0 | 440 / 400 | 20/1 | Buff: addphysicsdamage_p (âm), stateSpecialId=28 |
| 144 | Minh Tôn Bản Sinh (##) | Passive | 3 (passive) | 0 | 0 / 0 | 0/0 | Buff: fireres_p (Log10 công thức có thể sai) — placeholder `##` |
| 145 | Đơn Chỉ Liệt Diệm / 单指烈焰 | Active ranged | 0 (Missiles) | 0 | 280 / 320 | 57/1 | Fire damage pure |
| 146 | Ngũ Hành Trận / 五行阵 | Active aura | 2 (Initiative) | 0 | 180 / 180 | 226/1 | Aura stateSpecialId=29; mobile đã có ✓ |
| 147 | Huyền Minh Hấp Tinh / ?冥?星 | Active melee-missile | 0 | **1 (melee)** | 60 / 320 | 71/1 | **RADIUS SAI 5×** (60 vs 320) + melee bị Missiles |
| 148 | Ma Diệm Thất Sát / 魔炎七杀 | Active ranged | 0 (Missiles) | 0 | 570 / 320 | 58/1 | **RADIUS SAI LỚN** (320 vs 570); **EVENT CHAIN startSkill=192 (Ngự Phong thuật) MISSING** |
| 149 | Thực Cốt Huyết Nhận / ?骨?刃 | Active state | 2 (Initiative) | 0 | 0 / 0 | 0/0 | Buff: addfiredamage_v; stateSpecialId=25 (mobile dùng 11 cho animId, state id riêng) |
| 150 | Thiên Ma Giải Thể / 天魔解体 | Active state | 2 (Initiative) | 0 | 0 / 0 | 0/0 | Buff multi-attr; **lifemax_p DẤU NGƯỢC** (mobile +21%/+20% vs PC -11%/-20%) |
| 361 | Vân Long Kích / ?龙? | Sub-skill (added dmg from 135/141/142) | 0 | 1 (melee) | 60 / — | 169/1 | **MISSING MOBILE** — child missile 169 mv=1 life=6 speed=32 (homing dash?) |
| 362 | Thiên Ngoại Lưu Tinh / 天外流星 | Sub-skill (from 138/145) | 0 | 0 | 420 / — | 171/1 | **MISSING MOBILE** — child 171 mv=0 stationary life=14; **EVENT CHAIN vanishSkill=363 MISSING** |
| 363 | Nghiệp Hỏa Phần Thành / 业火焚城 | Sub-skill (from 148) | 0 | 0 | 570 / — | 170/1 | **MISSING MOBILE** — child 170 mv=0 stationary life=54 speed=2 (fire spread!) |
| 364 | Bi Tô Thanh Phong / 碧?清风 | Sub-skill (from ?) | 0 | 0 | 440 / — | 20/1 | **MISSING MOBILE** — stateSpecialId=58 |
| 1075 | Giang Hải Não Lan / 江海?兰 | Sub-skill 150-tier (from 135/141) | 0 | 1 (melee) | 60 / — | 337/1 | **MISSING MOBILE** — child 337 mv=1 life=6 speed=36; **EVENT CHAIN startSkill=1131 MISSING** |
| 1076 | Tật Hỏa Liệu Nguyên / 疾火燎原 | Sub-skill 150-tier (from 138/145/148) | 0 | 0 | 570 / — | 366/1 | **MISSING MOBILE** — child 366 mv=0 stationary life=37 (fire storm) |

**Sub-skill damage chain trong tianren.lua** (event chain G6 hoàn toàn MISSING):
- 135 (Tàn Dương) L1-2: cast 361 + cast 142 + cast 1075 (sub-skill)
- 138 (Thôi Sơn) L1-2: cast 362 + cast 1076
- 141 (Liệt Hỏa) L1-2: cast 361 + cast 1075
- 142 (Thâu Thiên) L1-2: cast 361 + cast 1075
- 145 (Đơn Chỉ) L1-2: cast 362 + cast 148 + cast 1076
- 148 (Ma Diệm) L1-2: cast 363 + cast 1076
- 1075 (Giang Hải) startSkill=1131 — event chain chưa rõ target
- 362 (Thiên Ngoại) vanishSkill=363 — fire spread khi missile bay xong

**Tuning coverage** (`PcSkillTuningRegistry.RadiusCurves[TianRenId]` line 77-84): chỉ cover 5/14 active (35%); missing 136/137/139/140/142/143/147/149/150.

### 2.8.2. Gap table

| ID | Tên | Hành vi mobile | Hành vi PC | Gap | Severity | Effort |
|---:|---|---|---|---|---|---|
| **361** | Vân Long Kích | **KHÔNG CÓ TRONG CATALOG** | PC ModSkills.txt: child=169, anim=10, isMelee=1, arc=60, mv=1 homing | **G6 — sub-skill hoàn toàn missing** — referenced as addskilldamage1 cho 135/141/142 (chained damage). L1-2 cast nên trigger cast 361 | **Cao** (cốt lõi chain damage) | 1 giờ (BaseSkill + child config) |
| **362** | Thiên Ngoại Lưu Tinh | **KHÔNG CÓ TRONG CATALOG** | PC: child=171, anim=11, arc=420, mv=0 stationary, **vanishSkill=363** (event chain fire spread) | **G6 — sub-skill missing + G6 event chain vanish→363 missing**. Mobile không có cách nào fire chain. | **Cao** | 1 giờ + thêm logic vanish event |
| **363** | Nghiệp Hỏa Phần Thành | **KHÔNG CÓ TRONG CATALOG** | PC: child=170, anim=11, arc=570, mv=0 stationary life=54 speed=2 (fire spread AoE); referenced by 148 chain | **G6 — sub-skill missing** (mất fire spread visual cốt lõi của Thiên Nhẫn) | **Cao** | 1 giờ |
| **364** | Bi Tô Thanh Phong | **KHÔNG CÓ TRONG CATALOG** | PC: child=20, arc=440, stateSpecialId=58 | **G7 — sub-skill missing** (chưa rõ reference nào dùng) | Trung bình | 30 phút |
| **1075** | Giang Hải Não Lan | **KHÔNG CÓ TRONG CATALOG** | PC: child=337, anim=10, isMelee=1, arc=60, mv=1 life=6 speed=36; **startSkill=1131** | **G6 — sub-skill 150-tier missing + G6 event chain startSkill=1131 missing**. Mobile không cast được. | **Cao** | 1 giờ + start event |
| **1076** | Tật Hỏa Liệu Nguyên | **KHÔNG CÓ TRONG CATALOG** | PC: child=366, anim=11, arc=570, mv=0 stationary life=37; referenced by 138/145/148 chain | **G6 — sub-skill 150-tier missing** (mất fire storm cuối game) | **Cao** | 1 giờ |
| **135** | Tàn Dương Như Huyết | `radius=320, child=54/1, charAnim=11, timePerCast=0, waitTime=0, addskilldamage1→361, addskilldamage2→142, addskilldamage3→1075` (declarations only, no runtime cast) | PC: radius=270, waitTime=5, addskilldamage1[1]={{1,361},{2,361}} L1-2 cast 361; [3]={{1,1},{20,45}} L3-20 dmg scales | **G4 radius sai (320 vs 270)** + **G4 waitTime=0 vs PC=5** (multi-cast no delay) + **G6 addskilldamage1/2/3 không fire sub-skill runtime** (chỉ là damage field, không cast sub-skill) | **Cao** (mất chain pattern L1-2 + radius sai) | 2 giờ |
| **136** | Hỏa Liên Phần Hoa | `radius=400, child=20/1, addfiredamage_v` (NOTE: code ghi `MeleeDamageReturnP` không phải AddFireDamageV) | PC: radius=440, stateSpecialId=22 | **G4 radius sai (400 vs 440)** + **G7 stateSpecialId=22 bị bỏ** + **G7 PcSkillTuningRegistry thiếu ID 136** | Trung bình | 1 giờ |
| **137** | Ảo Ảnh Phi Hồ | `radius=400, child=20/1, AttackRatingEnhanceP` | PC: radius=440, stateSpecialId=26 | **G4 radius sai (400 vs 440)** + **G7 stateSpecialId=26 bị bỏ** + **G7 Tuning thiếu** | Trung bình | 1 giờ |
| **138** | Thôi Sơn Điền Hải | `radius=350, child=55/10, timePerCast=0, waitTime=0` | PC: radius=400, child=55/10, **timePerCast=40**, **waitTime=10**, **MslsGenerate=5**, **MslsGenerateData=5** | **G4 radius sai (350 vs 400)** + **G4 timePerCast=0 vs PC=40** (missile bắn hết 1 phát thay vì 10 missiles staggered qua 40 ticks) + **G4 waitTime=0 vs PC=10** + **G7 MslsGenerate/MslsGenerateData bị bỏ** (cơ chế staggered spawn mất) | **Cao** (mất cảm giác "10 lưỡi cày theo đường" — đây là tên skill "Thôi Sơn Điền Hải") | 2-3 giờ (cần thêm logic spawn theo tick) |
| **139** | Hỗn Thủy Mạc Ngư | `radius=320, child=70/1, PhysicsEnhanceP 9+lv*10, StealStaminaP 2+lv` (L1=11, L20=219) | PC: **isMelee=1 (melee)**, **radius=60**, waitTime=0, IsPhysical=1. PC `physicsenhance_p` (chưa tìm thấy file per-skill trong scope, có thể tianren.lua định nghĩa) | **G4 radius SAI 5×** (320 vs 60 — overcast, player cast xa hơn PC 5×) + **G4 isMelee bị bỏ** (mobile Missiles, PC melee) + **G7 Tuning thiếu ID 139** + **G7 PhysicsEnhanceP sai công thức (11→219 vs PC ?)** | **Cao** (radius 5× sai = balance break + melee bị missile) | 1 giờ |
| **140** | Phi Hồng Vô Tích | `radius=400, AddDefenseV (âm)` | PC: radius=440, stateSpecialId=27 | **G4 radius sai (400 vs 440)** + **G7 stateSpecialId=27 bị bỏ** + **G7 Tuning thiếu** | Trung bình | 1 giờ |
| **141** | Liệt Hỏa Tình Thiên | `radius=384, child=56/16, SkillMissileForm.Single` | PC: **radius=72** (cast range), MisslesForm=**3 (Surround)**, MslsGenerate=1/5, waitTime=5 | **G4 radius SAI 5×** (384 vs 72 — cast xa hơn PC 5×) + **G4 MissilesForm sai** (Single vs PC Surround=3) + **G4 waitTime=0 vs PC=5** + **G7 MslsGenerateData=5 bị bỏ** | **Cao** (radius sai + hình dạng missile sai: 16 tia tỏa tròn vs mobile 16 missile thẳng hàng) | 1 giờ |
| **142** | Thâu Thiên Hoán Nhật | `radius=78, child=69/1, PhysicsEnhanceP 25+lv*231, StealLife/Mana, charAnim=9, timePerCast=0, waitTime=0` | PC: **isMelee=1 (melee)**, radius=60, IsPhysical=1, child=69/1 (mv=1 life=3 speed=24), addskilldamage1→361, addskilldamage2→1075 (chain) | **G4 radius sai (78 vs 60)** + **G4 isMelee bị bỏ** (mobile Missiles, PC melee) + **G4 MissilesForm=Single ✓ (PC=1)** + **G6 addskilldamage chain MISSING** + **G7 missle_lifetime_v=4 (mobile missile 69) chưa thấy khai báo** + **G7 Tuning thiếu ID 142** | **Cao** (chain damage mất + melee→missile) | 2 giờ |
| **143** | Lịch Ma Đoạt Hồn | `radius=400, AddPhysicsDamageP (âm)` | PC: radius=440, stateSpecialId=28 | **G4 radius sai (400 vs 440)** + **G7 stateSpecialId=28 bị bỏ** + **G7 Tuning thiếu** | Trung bình | 1 giờ |
| **145** | Đơn Chỉ Liệt Diệm | `radius=320, child=57/1, waitTime=0` | PC: radius=280, waitTime=5, IsPhysical=0 | **G4 radius sai (320 vs 280)** + **G4 waitTime=0 vs PC=5** | Thấp (15% sai) | 30 phút |
| **147** | Huyền Minh Hấp Tinh | `radius=320, child=71/1, PhysicsEnhanceP 25+lv*10, StealManaP 2+lv/2` | PC: **isMelee=1 (melee)**, **radius=60**, IsPhysical=1, child=71/1 (mv=1 life=3 speed=24) | **G4 radius SAI 5×** (320 vs 60 — overcast 5×) + **G4 isMelee bị bỏ** (mobile Missiles, PC melee) + **G7 Tuning thiếu ID 147** + **G7 missle_lifetime_v chưa khai báo** | **Cao** (radius 5× + melee bị missile) | 1 giờ |
| **148** | Ma Diệm Thất Sát | `radius=320, child=58/1, DeadlyStrikeP, charAnim=11, timePerCast=0, waitTime=0` | PC: **radius=570**, **startSkill=192** (Ngự Phong thuật — fireball start event), addskilldamage1→363, addskilldamage2→1076 | **G4 radius SAI LỚN** (320 vs 570, undercast 44%) + **G6 EVENT CHAIN startSkill=192 MISSING** (mất hiệu ứng "gió bùng lên" trước khi bắn) + **G6 addskilldamage chain MISSING** + **G4 timePerCast=0 vs PC=0 ✓** | **Cao** (event chain mất + radius sai lớn) | 2 giờ (cần thêm start event hook) |
| **149** | Thực Cốt Huyết Nhận | `AddFireDamageV 40+10*lv (L1=50, L20=240), 1080+162*lv (L1=1242, L20=4320), charAnim=11, targetEnemy` | PC: stateSpecialId=25, InitiativeNpcState, addfiredamage_v theo per-skill `shigu-xueren.lua` | **G7 stateSpecialId=25 bị bỏ** (mobile dùng 11 charAnimId, state id riêng) + **G7 AddFireDamageV 2 tham số (val1+val2) là per-skill `shigu-xueren.lua` (L1=50, L20=240) + duration 1080+162*lv (per-skill); PC tianren.lua ĐÃ COMMENT OUT** (xem line 199-204 `skill_startevent/showevent` commented). G7 verify cần check 150-tier data | Trung bình | 1 giờ |
| **150** | Thiên Ma Giải Thể | `LifeMaxP Link(lv,(1,21,""),(30,20,"")) dur=18*45→18*180` — **L1=+21%, L30=+20%** (positive, slight decrease) | PC tianren.lua line 170 (commented): `lifemax_p={{{1,-11},{20,-30},{30,-20},{40,10},{41,10}}}`. Per-skill `tianmo-jieti.lua::Getlifemax_p`: `result1 = -10-level; result2 = 600+level*200` → L1=-11, L20=-30, L30=-40 | **G7 lifemax_p DẤU NGƯỢC** (mobile +21→+20 vs PC -11→-40) — buff HP thay vì giảm HP (mobile mất cảm giác "giải thể" — tự hủy HP để tăng dame) | **Cao** (cốt lõi cảm giác "tự thiêu" của skill) | 30 phút (đổi dấu + curve) |
| **131** | Thiên Nhẫn Đao Pháp | `AddFireDamageV Link(1,15)→(20,215), skillStyle=PassivityNpcState, charAnim=14` | PC: style=3 (passive), addfiremagic_v tianren.lua line 26 | **G7** MagicAttributeKind.AddFireDamageV vs PC tianren.lua addfiremagic_v (MagicAttr addFireMagic vs addFireDamage) | Thấp (verify: AddFireDamageV + value ổn) | 30 phút verify |
| **132** | Thiên Nhẫn Thương Pháp | `AddPhysicsDamageP (15→215) -1 3, AttackRatingEnhanceP (35→272) -1 0, DeadlyStrikeEnhanceP (6→35) -1 0` | PC tianren.lua: `addphysicsdamage_p (15→215), attackratingenhance_p (35→272), deadlystrikeenhance_p (6→35)` | OK (match) | — | — |
| **133** | Thiên Nhẫn Phủ Pháp | **THIẾU mobile catalog** | PC ModSkills.txt: style=3 passive mastery, icon `\spr\Ui\...µ¶·¨.spr` | **G7 — missing** (có trong PC, mobile chưa catalog) | Thấp (passive mastery) | 30 phút (copy 131/132) |
| **134** | Thiên Nhẫn Chùy Pháp | **THIẾU mobile catalog** | PC ModSkills.txt: style=3 passive mastery, icon `\spr\Ui\...\Ë«´¸Ì×Â·.spr` | **G7 — missing** (có trong PC, mobile chưa catalog) | Thấp (passive mastery) | 30 phút (copy 131/132) |
| **144** | Minh Tôn Bản Sinh | `FireResP = Log10(lv+1)/2 * 70` (L1=10, L20=48) | PC ModSkills.txt: style=3 passive, isMelee=0 | **G7** công thức FireResP có thể sai (PC tianren.lua không định nghĩa `minhton_bansinh` per-skill) | Thấp | 1 giờ verify |
| **146** | Ngũ Hành Trận | `AddDefenseV (75→550) dur=18, stateSpecialId=226, targetSelf, isAura` | PC: stateSpecialId=**29** (mobile=226), charAnim=14, isAura=1 ✓ | **G5** stateSpecialId confusion (mobile=226 vs PC=29 — có thể là ID khác mapping) | Trung bình | 30 phút verify |
| **All 6 melee-missile** (139/142/147/361/1075 + isMelee=1) | `skillStyle=Missiles` (mobile default) | PC: IsMelee=1 (melee mode) | **G1 lite — melee mode MISSING** (PC melee-missile pattern, mobile map sang Missiles). Ở close range <60, PC chém cận chiến; mobile bắn missile xa hơn. Không phải dash, nhưng có melee range check. | Trung bình (feel khác PC) | 2 giờ (thêm IsMelee branch trong runtime) |
| **All 5 active attack** | `PcSkillTuningRegistry.RadiusCurves[TianRenId]` cover 5/14 (35%) | PC có 14 active skill cần tuning | **G7 — Tuning coverage 35%** (thiếu 9 IDs: 136/137/139/140/142/143/147/149/150) | Trung bình | 1 giờ (thêm 9 entries) |
| **All TianRen** | `ConfigureTianRenVisuals` **KHÔNG CÓ** trong SkillEffectVisualService | PC có data-driven visuals OK, không có switch-case | **G7 — không có visual case** (rely on ConfigureDataDrivenVisuals auto-resolve). OK cho hiện tại nhưng rủi ro nếu auto-mapper miss | Thấp | 1 giờ verify |
| **135 + 148** | tianren.lua line 199-204 commented: `skill_eventskilllevel/skill_startevent/skill_showevent` | PC có event chain tiềm năng cho 147/150 (`zhanren150.randmove`) | **G6 — showevent/startevent commented** (PC code path mất; nếu uncomment sẽ cần thêm 378, 1101 logic) | Thấp (đã commented) | nửa ngày |

### 2.8.3. Phase 1 quick wins

- [ ] **ID 361-364 + 1075-1076 — thêm 6 sub-skill vào catalog** (CRITICAL — chain damage G6). Copy từ `DamageSkillNew` template với child missile 169/170/171/337/366. Riêng 362 cần thêm `vanishSkill=363` event hook, 1075 cần `startSkill=1131` event hook. (G6, 1 giờ + 1 giờ event)
- [ ] **ID 148 Ma Diệm Thất Sát** — sửa `radius=320` → `570` (PC), thêm event hook `startSkill=192` (Ngự Phong thuật pre-cast fire effect). (G4+G6, 1 giờ)
- [ ] **ID 141 Liệt Hỏa Tình Thiên** — sửa `radius=384` → `72`, đổi `SkillMissileForm.Single` → `Surround`, thêm `waitTime=5`. (G4, 1 giờ)
- [ ] **ID 139, 142, 147, 361, 1075 melee-mode** — sửa `radius=320/78/320/60/60` → `60/60/60/60/60` (PC melee range). (G4, 1 giờ tổng 5 skill)
- [ ] **ID 150 Thiên Ma Giải Thể lifemax_p DẤU NGƯỢC** — đổi `Link(lv,(1,21,""),(30,20,""))` → `Link(lv,(1,-11,""),(30,-40,""))` (PC per-skill tianmo-jieti.lua line 47-51). Đây là bug gameplay lớn nhất — buff HP thay vì tự hủy. (G7, 30 phút)
- [ ] **ID 135 Tàn Dương** — sửa `radius=320` → `270`, `waitTime=0` → `5`. (G4, 15 phút)
- [ ] **ID 136, 140, 143 Hỏa Liên / Phi Hồng / Lịch Ma** — sửa `radius=400` → `440` (3 skill). (G4, 30 phút)
- [ ] **ID 145 Đơn Chỉ** — sửa `radius=320` → `280`, `waitTime=0` → `5`. (G4, 15 phút)
- [ ] **ID 138 Thôi Sơn** — sửa `radius=350` → `400`. (G4, 15 phút) **[DEFER — staggered spawn fix xem G7]**
- [ ] **PcSkillTuningRegistry.RadiusCurves[TianRenId]** — thêm 9 entries: 136, 137, 139, 140, 142, 143, 147, 149, 150 (mỗi entry flat 1 dòng). (G7, 1 giờ)
- [ ] **ID 133 + 134 Phủ Pháp + Chùy Pháp** — thêm 2 passive mastery tương tự 131/132 (copy từ `TianRenPassiveDaofa`). (G7, 30 phút)
- [ ] **ID 149 Thực Cốt Huyết Nhận** — thêm `stateSpecialId=25` để runtime bind buff state. (G7, 15 phút)

### 2.8.4. Phase 3 dash (G1)

- [ ] **N/A — Thiên Nhẫn không có dash/lunge**. `KNpc.cpp::CastMeleeSkill` switch (line 1829) + `tianren.lua` (không thấy Melee_Jump/JumpAndAttack/RunAndAttack) đều không tham chiếu. PC `zhanren150.randmove` ở dòng 242 có `randmove={{1,1},{20,1}},{{1,1},{20,5}}` → missile 197 có random scatter path (chưa rõ PC C++ impl); ngoài range 131-150 task. Phase 3 bỏ qua cho Thiên Nhẫn.

### 2.8.5. Phase 4 event chain (G6)

- [ ] **Sub-skill 361-364, 1075-1076 catalog (G6 critical)** — Phase 1 đã cover (6 entries, mỗi cái BaseSkill + child config)
- [ ] **ID 148 startSkill=192** (Gió bùng lên trước khi bắn Ma Diệm) — Phase 1 đã cover (1 hook)
- [ ] **ID 362 vanishSkill=363** (fire spread khi Thiên Ngoại Lưu Tinh missile bay xong) — Phase 1 đã cover (1 hook)
- [ ] **ID 1075 startSkill=1131** (Giang Hải Não Lan pre-cast event — chưa rõ ID 1131 là gì, cần check `ModSkills.txt` ID 1131) — Phase 1 đã cover (1 hook, 30 phút verify 1131)
- [ ] **ID 135/138/141/142/145/148 addskilldamage1/2/3 chain** (chained damage L1-2 cast sub-skill 361/362/363/1075/1076) — cần thêm runtime path: nếu `skillLevel <= 2` thì gọi `ApplySkillCast(caster, subSkillId)` thay vì chỉ add damage field. Effort: nửa ngày (cần uncomment path trong CombatRuntimeService + test không infinite-loop)

### 2.8.6. Trạng thái

- [x] Catalog scan xong (18 skill: 4 passive mastery, 1 passive (144), 1 aura (146), 12 active attack)
- [x] Per-skill files: 6 pinyin ASCII decoded (`limo-duohun`, `sanmei-zhenhuo`, `shigu-xueren`, `tianmo-jieti`, `wuxing-zhen`, `zhiyan`) + 20 GBK TC names decoded
- [ ] **Quick-win phase merged** (Phase 1 — 12 items; ưu tiên: 361-364/1075-1076 sub-skill + 148 event + 141 radius + 150 lifemax_p)
- [ ] Dash phase merged: **không áp dụng** (Thiên Nhẫn không có dash)
- [ ] Event chain phase merged (Phase 4 — 5 hooks: 148/362/1075/1131 + addskilldamage chain runtime)
- [ ] Tuning coverage 35% → 100% (cần thêm 9 entries: 136/137/139/140/142/143/147/149/150)

### 2.8.7. Tổng kết

- **Skill chính ưu tiên sửa**: **148 Ma Diệm Thất Sát** (event chain mất + radius sai 44%) → fix 1 giờ → mở ra hiệu ứng gió bùng lên đặc trưng Thiên Nhẫn.
- **Skill sub-form ưu tiên**: **361-364, 1075-1076** (toàn bộ MISSING trong mobile) → 1 giờ/cái = 6 giờ tổng → mở ra chain damage L1-2 pattern (cốt lõi Thiên Nhẫn).
- **Bug gameplay nghiêm trọng #1**: **150 Thiên Ma Giải Thể lifemax_p DẤU NGƯỢC** — mobile buff HP, PC tự hủy HP. Fix 30 phút, ảnh hưởng lớn đến cảm giác "giải thể" (đây là tên skill).
- **Bug gameplay nghiêm trọng #2**: **141 Liệt Hỏa Tình Thiên** radius 384 vs PC 72 (5× overcast) — cast xa hơn PC 5×, balance break.
- **Bug gameplay #3**: **139, 147** radius 320 vs PC 60 (5× overcast) — same as 141.
- **Skill passive**: 133, 134 missing (chỉ là copy từ 131/132, 30 phút).
- **G1 dash**: **không áp dụng** (Thiên Nhẫn melee-missile thuần, không dash).
- **G6 event chain**: 5 hooks cần thêm (148 startSkill=192, 362 vanishSkill=363, 1075 startSkill=1131, addskilldamage1/2/3 chain runtime cho 6 skill) — tổng effort ~nửa ngày.
- **Test plan**: unit test cho sub-skill 361-364/1075-1076 cast chain; visual test cast 148 (expect gió bùng 192 → 363 fire spread); regression test cast 150 xem HP giảm (-10/L1) thay vì tăng; radius test cast 141 ở khoảng cách 100 (>72 PC) — expect fail cast ở PC, success ở mobile.
- **Phase 5 (future)**: port `zhanren150` 150-tier (có `randmove`+`missle_missrate`); port 4 missing per-skill skill (`huanying_feihu`/`feihong_wuji`/`huolian_fenhua`/`limo_duopo` đã có trong catalog rồi; còn lại `canyang_ruxue`/`tuishan_tianhai`/`liehuo_qingtian`/`toutian_huanri`/`tanzhi_lieyan`/`moyan_qisha`/`wuxing_zhen`/`beisu_qingfeng` per-skill cần verify).

---

## Tổng kết gap (ID, gap, severity, effort) — yêu cầu format

| ID | Gap | Severity | Effort |
|---:|---|---|---|
| 361 | Sub-skill MISSING (chain damage cốt lõi) | Cao | 1 giờ |
| 362 | Sub-skill MISSING + event vanish→363 | Cao | 1 giờ + event |
| 363 | Sub-skill MISSING (fire spread visual) | Cao | 1 giờ |
| 364 | Sub-skill MISSING | Trung bình | 30 phút |
| 1075 | Sub-skill 150-tier MISSING + event startSkill=1131 | Cao | 1 giờ + event |
| 1076 | Sub-skill 150-tier MISSING | Cao | 1 giờ |
| 135 | radius sai 320 vs 270, waitTime 0 vs 5, addskilldamage chain MISSING | Cao | 2 giờ |
| 136 | radius sai 400 vs 440, stateSpecialId=22 bị bỏ, Tuning thiếu | Trung bình | 1 giờ |
| 137 | radius sai 400 vs 440, stateSpecialId=26 bị bỏ, Tuning thiếu | Trung bình | 1 giờ |
| 138 | radius sai 350 vs 400, timePerCast=0 vs 40, MslsGenerate staggered MISSING | Cao | 2-3 giờ |
| 139 | radius SAI 5× (320 vs 60), isMelee bị bỏ, Tuning thiếu | Cao | 1 giờ |
| 140 | radius sai 400 vs 440, stateSpecialId=27 bị bỏ, Tuning thiếu | Trung bình | 1 giờ |
| 141 | radius SAI 5× (384 vs 72), MissilesForm Single vs Surround, waitTime 0 vs 5 | Cao | 1 giờ |
| 142 | radius sai 78 vs 60, isMelee bị bỏ, addskilldamage chain MISSING, Tuning thiếu | Cao | 2 giờ |
| 143 | radius sai 400 vs 440, stateSpecialId=28 bị bỏ, Tuning thiếu | Trung bình | 1 giờ |
| 145 | radius sai 320 vs 280, waitTime 0 vs 5 | Thấp | 30 phút |
| 147 | radius SAI 5× (320 vs 60), isMelee bị bỏ, Tuning thiếu | Cao | 1 giờ |
| 148 | radius SAI LỚN (320 vs 570), event startSkill=192 MISSING, addskilldamage chain MISSING | Cao | 2 giờ |
| 149 | stateSpecialId=25 bị bỏ | Trung bình | 15 phút |
| **150** | **lifemax_p DẤU NGƯỢC (+21% vs -11%/-40%)** | **Cao** | **30 phút** |
| 131 | AddFireDamageV vs PC addfiremagic_v (verify) | Thấp | 30 phút |
| 133 | Passive mastery MISSING | Thấp | 30 phút |
| 134 | Passive mastery MISSING | Thấp | 30 phút |
| 144 | FireResP công thức cần verify | Thấp | 1 giờ |
| 146 | stateSpecialId=29 vs mobile=226 confusion | Trung bình | 30 phút |
| All 6 melee-missile (139/142/147/361/1075) | isMelee mode MISSING (melee→Missiles sai pattern) | Trung bình | 2 giờ |
| All 5 active | Tuning coverage 35% (thiếu 9 entries) | Trung bình | 1 giờ |
| All 14 active | ConfigureTianRenVisuals MISSING (rely on auto-mapper) | Thấp | 1 giờ verify |
