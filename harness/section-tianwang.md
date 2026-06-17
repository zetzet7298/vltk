# 2.2. Thiên Vương (PC: 天王帮 TiānWángBāng, ID range 23-42)

> **Nguồn PC verified**:
> - `Assets/StreamingAssets/Reference/ModSkills.txt` (canonical, TCVN3, SkillId 23-42 trừ 25/27/28/38/39)
> - `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/.../script/skill/tianwang.lua` (GB18030, 763 dòng)
> - `Assets/StreamingAssets/Reference/KNpc.cpp` (`CastMeleeSkill` switch line 1829-1891, `DoSkill`/`OnSkill` line 1937-1968, `NewJump` line 2883)
> - `Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs` (line 1461-1702 `CreateTianWangSkills`)
> - `Assets/Scripts/Sandbox/PcSkillTuningRegistry.cs` (line 42-52 TianWangId radius curves)
> - `Assets/Scripts/Sandbox/SkillEffectVisualService.cs` (line 1079-1115 `ConfigureTianWangVisuals`)
> - `Assets/Scripts/Sandbox/CombatRuntimeService.cs` (line 247-283 `SpawnProjectiles`)
> - `Assets/Scripts/Model/SkillDefinition.cs` (line 13-50 enum + struct, không có `meleeType`)

> **Verified conclusion**: Thiên Vương là **melee cận chiến thuần**. **KHÔNG có dash** cho bất kỳ skill 23-42 nào. KNpc.cpp `CastMeleeSkill` switch xác nhận: 9 active skill đều `IsMelee=1` + `MslsGenerate=1`, không có `MeleeType` (mặc định `Melee_AttackWithBlur` cho cả 9). Multi-hit pattern thông qua `ChildSkillNum` (1-4 hit) + `MaxShadowNum` (5 shadow) + `WaitTime/TimePerCast` (skill 40 only).

---

## 2.2.1. Catalog scan (verified từ ModSkills.txt + tianwang.lua)

| ID | Tên VN | SkillStyle | IsMelee | AR | MaxShadow | MslsGen | MslsForm | ChildSkillId | ChildNum | BaseSk | CharAnim | Wait | TPC | ReqL | Horse | Param1 | Param2 | Vai trò |
|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| 23 | Thiên Vương Thương Pháp | 3 passive | 0 | 0 | 0 | 0 | 7 | 0 | 0 | 0 | 14 | 0 | 0 | 10 | 0 | 0 | 0 | Buff mastery thương |
| 24 | Thiên Vương Đao Pháp | 3 passive | 0 | 0 | 0 | 0 | 7 | 0 | 0 | 0 | 14 | 0 | 0 | 10 | 0 | 0 | 0 | Buff mastery đao |
| 26 | Thiên Vương Chùy Pháp | 3 passive | 0 | 0 | 0 | 0 | 7 | 0 | 0 | 0 | 14 | 0 | 0 | 10 | 0 | 0 | 0 | Buff mastery chùy |
| **29** | Trảm Long Quyết | 1 melee | 1 | 72 | 0 | 1 | 12 | 405 | 1 | 0 | 9 | 0 | 0 | 10 | 1 | 0 | 0 | Melee 1-hit (đơn chiêu) |
| **30** | Hồi Phong Lạc Nhạn | 1 melee | 1 | 90 | 0 | 1 | 12 | 219 | **2** | 0 | 9 | 0 | 0 | 10 | 1 | 10 | 0 | **Melee 2-hit liên tiếp** |
| **31** | Hành Vân Quyết | 1 melee | 1 | 72 | 0 | 1 | 12 | 406 | 1 | 0 | 9 | 0 | 0 | 30 | 1 | 0 | 0 | Melee 1-hit + cold |
| **32** | Vô Tâm Trảm | 1 melee | 1 | 90 | **5** | 1 | 12 | 220 | 1 | 0 | 9 | 0 | 0 | 60 | 0 | 0 | 0 | Melee 1-hit + 5 shadow |
| 33 | Tĩnh Tâm Quyết | 2 buff | 0 | 0 | 0 | 0 | 7 | 0 | 0 | 0 | 11 | 0 | 0 | 20 | 0 | 0 | 0 | Self buff attack rating |
| **34** | Kinh Lôi Trảm | 1 melee | 1 | 72 | 0 | 1 | 12 | 404 | 1 | 0 | 9 | 0 | 0 | 10 | 0 | 0 | 0 | Melee 1-hit |
| **35** | Dương Quan Tam Điệp | 1 melee | 1 | 90 | 0 | 1 | 12 | 221 | **3** | 0 | **10** | 0 | 0 | 30 | 1 | 10 | 0 | **Melee 3-hit liên tiếp** |
| 36 | Thiên Vương Chiến Ý | 3 passive | 0 | 0 | 0 | 0 | 7 | 0 | 0 | 0 | 11 | 0 | 0 | 60 | 0 | 0 | 0 | Passive HP/deadly/atk speed |
| **37** | Bát Phong Trảm | 1 melee | 1 | 90 | **5** | 1 | 12 | 222 | 1 | 0 | 9 | 0 | 0 | 30 | 0 | 0 | 0 | Melee 1-hit + 5 shadow |
| **40** | Đoạn Hồn Thích | 1 melee | 1 | 200 | **5** | 1 | **11** | 224 | 1 | 0 | 9 | **5** | **27** | 40 | 0 | **32** | **5** | **Multi-thrust 5×5 + 5 shadow** |
| **41** | Huyết Chiến Bát Phương | 1 melee | 1 | 90 | **5** | 1 | 12 | 225 | **4** | 0 | 9 | 0 | 0 | 60 | 1 | 9 | 0 | **Melee 4-hit + 5 shadow** |
| 42 | Kim Chung Tráo | 2 buff | 0 | 0 | 0 | 0 | 7 | 0 | 0 | 0 | 11 | 0 | 0 | 50 | 0 | 0 | 0 | Self buff phys/cold/fire/poison res |

> **Skill 40 verified detail** (MOST COMPLEX): `MslsForm=11` (khác 12 — "thrust" form), `WaitTime=5, TimePerCast=27` (= 5 thrust × 5 wait + 2 land), `Param1=32, Param2=5`. PC `tianwang.lua` `duanhun_ci` có `skill_param1_v={{1,4},{5,12},{20,24},{28,31},{31,31}}` (số thrust theo level) + `skill_param2_v={{1,18},{20,1},{21,1}}` (timing). Per-skill file `tianwang/duanhun-ci.lua` có `Getstun_p(level) → 15+level, time=9` (= 9/18=0.5s stun duration).
>
> **Skill 41 verified detail**: `MslsForm=12` (standard swing), `ChildNum=4` (4-hit), `MaxShadowNum=5` (5 shadow), `MslsGenerateData=4` (4 MslsGenerate count). PC `tianwang.lua` `xuezhan_bafang` có `physicsenhance_p={{1,60},{20,723}}` (mobile 80→385 SAI lớn) + `attackrating_p={{1,75},{20,320}}` (mobile 10→60 sai lớn).

**Skill 40 và 41 KHÔNG có dash** (xác nhận từ `KNpc.cpp`):
- `IsMelee=1, MslsGenerate=1` → vào `CastMeleeSkill` switch
- `MeleeType` field KHÔNG tồn tại trong `SkillDefinition.cs` enum `PcSkillStyle` (chỉ có `Melee=1` đơn, không phân biệt `Melee_AttackWithBlur` / `Melee_Jump` / `Melee_JumpAndAttack` / `Melee_RunAndAttack` / `Melee_ManyAttack`)
- PC mặc định `MeleeType=0` = `Melee_AttackWithBlur` cho cả 9 active skill → instant swing, không có `NewJump`/`DoJumpAttack`/`DoRunAttack`
- Skill 40 multi-thrust = `MslsForm=11` + `WaitTime/TimePerCast` timing, là **multi-step pattern** chứ KHÔNG phải dash player
- Skill 41 multi-hit = `ChildNum=4` + 5 shadow, là **child-skill chain** chứ KHÔNG phải dash player

---

## 2.2.2. Gap table (verified + new)

> **Chú thích cột "Hành vi mobile"**: lấy từ `CreateTianWangSkills` (PcCombatCatalogFactory line 1461-1702) + `PcSkillTuningRegistry` + `ConfigureTianWangVisuals` + `CombatRuntimeService.SpawnProjectiles`.
>
> **Chú thích cột "Hành vi PC"**: từ ModSkills.txt (verified bằng awk tab-split) + tianwang.lua (GB18030 decode verified).

| ID | Tên | Hành vi mobile | Hành vi PC (verified) | Gap | Severity | Effort |
|---:|---|---|---|---|---|---|
| **29** | Trảm Long Quyết | `radius=90, childSkillId=0, childSkillNum=0, charAnimId=2, cost=10, baseSkill=0`. Visual: case 29 stationary effect | `radius=72 ✓ registry, childSkillId=405, childSkillNum=1, charAnimId=9, baseSkill=0, isMelee=1, missleForm=12`. PC `zhanlong_jue`: physenhance_p {{1,80},{20,185}} (mobile 30→150) | **G4 charAnimId 2 vs PC 9** + **G4 childSkillId/Num bỏ** + **G7 physenhance_p sai lớn (80/185 vs 30/150)** + **G4 radius 90 catalog vs 72 registry inconsistency** | Cao | 1 giờ |
| **30** | Hồi Phong Lạc Nhạn | `radius=90, childSkillNum=0, charAnimId=2, cost=10, baseSkill=0` | `radius=90 ✓, childSkillId=219, childSkillNum=2, charAnimId=9`. PC `huifeng_luoyan`: physenhance_p {{1,80},{20,215}} (mobile 20→120) | **G4 childSkillNum=0 vs PC=2 (MẤT 2-HIT PATTERN)** + **G4 charAnimId 2 vs 9** + **G7 physenhance_p sai (80/215 vs 20/120)** | Cao (cốt lõi "2 chiêu liên tiếp") | 2 giờ |
| **31** | Hành Vân Quyết | `radius=80, childSkillNum=0, charAnimId=2, cost=10, no visual case` | `radius=72, childSkillId=406, childSkillNum=1, charAnimId=9`. PC `xingyun_jue`: physenhance_p {{1,30},{20,255}} (mobile 30→150), colddamage_v {{1,5},{20,50}} (mobile 0) | **G4 radius 80 vs 72** + **G4 childSkillNum=0 vs 1** + **G4 charAnimId 2 vs 9** + **G7 physenhance_p sai lớn (30/255 vs 30/150)** + **G7 mất colddamage_v** + **G7 visual missing (no case 31)** + **G7 PcSkillTuningRegistry thiếu ID 31** | Cao (mất cold + 1-hit) | 2 giờ |
| **32** | Vô Tâm Trảm | `radius=90, childSkillNum=0, charAnimId=2, cost=20→40, baseSkill=0` | `radius=90 ✓, childSkillId=220, childSkillNum=1, charAnimId=9, MaxShadowNum=5`. PC `wuxin_zhan`: physenhance_p {{1,65},{20,453}} (mobile 80→385) | **G4 childSkillNum=0 vs 1** + **G4 MaxShadowNum=5 bỏ** + **G4 charAnimId 2 vs 9** + **G7 physenhance_p sai (65/453 vs 80/385)** + **G7 deadlystrike_p sai (PC {{1,4},{20,25}} vs mobile 15/54)** | Cao | 2 giờ |
| 33 | Tĩnh Tâm Quyết (buff) | `radius=400, skillStyle=InitiativeNpcState, targetSelf, charAnimId=2, cost=20, duration=1200+1200*lv ms` | `radius=0 (buff), isMelee=0, charAnimId=11, ReqLevel=20, MaxLevel=20`. PC `jingxin_jue`: attackratingenhance_p {{1,45},{20,400}}, **duration 18×120→18×180 = 120s→180s** | **G4 radius 400 vs PC 0** + **G4 charAnimId 2 vs 11** + **G7 attackratingenhance_p sai lớn 4× (45/400 vs 10/100)** + **G7 DURATION sai 50× (PC 120s vs mobile 2.4s L1; 180s vs 25.2s L20)** + **G7 stateSpecialId=46 bị bỏ** | **Cao** (buff expire sau 2-25s thay vì 2-3 phút) | 1 giờ |
| **34** | Kinh Lôi Trảm | `radius=72 ✓, childSkillNum=0, charAnimId=2, cost=10, baseSkill=0` | `radius=72 ✓, childSkillId=404, childSkillNum=1, charAnimId=9`. PC `jinglei_zhan`: physenhance_p {{1,40},{20,200}} (mobile 20→120) | **G4 childSkillNum=0 vs 1** + **G4 charAnimId 2 vs 9** + **G7 physenhance_p sai 2× (40/200 vs 20/120)** | Trung bình | 1 giờ |
| **35** | Dương Quan Tam Điệp | `radius=90, childSkillNum=0, charAnimId=2, cost=15, baseSkill=0` | `radius=90 ✓, childSkillId=221, childSkillNum=3, charAnimId=**10**, ReqLevel=30`. PC `yangguan_sandie`: physenhance_p {{1,130},{20,375}} (mobile 35→221) | **G4 childSkillNum=0 vs PC=3 (MẤT 3-HIT PATTERN)** + **G4 charAnimId 2 vs 10** + **G7 physenhance_p sai lớn (130/375 vs 35/221)** | **Cao** (3 chiêu liên tiếp = cốt lõi) | 2 giờ |
| 36 | Thiên Vương Chiến Ý (passive) | `skillStyle=PassivityNpcState, charAnimId=14, ManaMaxP {{1,5},{30,60}}, DeadlyStrikeEnhanceP {{1,2},{30,20}}` | `skillStyle=3 ✓, charAnimId=11, isMelee=0, ReqLevel=60, MaxLevel=30`. PC `tianwang_zhanyi`: **lifemax_p {{1,21},{30,185}}** + **lifemax_yan_p {{1,21},{35,160},{36,160}}** + deadlystrikeenhance_p {{1,5},{30,45}} + **attackspeed_v {{1,5},{30,65}}** | **G7 bỏ 4 attribute: lifemax_p (HP+185% L30) + lifemax_yan_p (smoke) + attackspeed_v (atk speed +65) + sai deadlystrike (2/20 vs 5/45 sai 2.25×)** + **G4 charAnimId 14 vs PC 11** | **Cao** (mất 3 buff chính) | 2 giờ |
| **37** | Bát Phong Trảm | `radius=90, childSkillNum=0, charAnimId=2, cost=15, baseSkill=0` | `radius=90 ✓, childSkillId=222, childSkillNum=1, charAnimId=9, MaxShadowNum=5, MslsGenerateData=8`. PC `pofeng_zhan`: physenhance_p {{1,120},{20,275}} (mobile 30→222) | **G4 childSkillNum=0 vs 1** + **G4 MaxShadowNum=5 bỏ** + **G4 charAnimId 2 vs 9** + **G7 physenhance_p sai lớn (120/275 vs 30/222, L1 sai 4×)** + **G5 Chinese name "八风斩" (8-wind) vs PC `pofeng_zhan` "泼风斩" (splash-wind) — name drift** | Cao | 2 giờ |
| **40** | Đoạn Hồn Thích | `radius=200 ✓, childSkillNum=0, charAnimId=2, timePerCast=0, waitTime=0, cost=20, baseSkill=0, no Param1/Param2 curves` | `radius=200 ✓, childSkillId=224, childSkillNum=1, charAnimId=9, MaxShadowNum=5, MslsGenerateData=5, MisslesForm=**11** (vs 12 khác), WaitTime=**5**, TimePerCast=**27**, Param1=**32**, Param2=**5**`. PC `duanhun_ci`: stun_p {{1,16},{20,35}} (mobile 10→50), deadlystrike_p {{1,4},{20,80}} (mobile 5→25), physenhance_p {{1,25},{20,215}} (mobile 50→250, sai lớn), **`skill_param1_v={{1,4},{5,12},{20,24},{28,31},{31,31}}` (số thrust theo level — BỎ HOÀN TOÀN)**, **`skill_param2_v={{1,18},{20,1},{21,1}}` (timing — BỎ)** | **G4 childSkillId/Num=0** + **G4 MaxShadowNum=5 bỏ** + **G4 charAnimId 2 vs 9** + **G4 waitTime 0 vs 5 (multi-thrust timing)** + **G4 timePerCast 0 vs 27** + **G4 Param1=32 (multi-missile) bỏ** + **G4 Param2=5 bỏ** + **G4 MisslesForm=11 (special thrust) bỏ** + **G4 PC tianwang.lua skill_param1_v/param2_v level curves bỏ** + **G7 stun_p sai (16/35 vs 10/50)** + **G7 deadlystrike_p sai lớn (4/80 vs 5/25)** + **G7 physenhance_p sai (25/215 vs 50/250)** | **Cao** (Đoạn Hồn Thích = multi-thrust 5-step, mất hoàn toàn) | nửa ngày |
| **41** | Huyết Chiến Bát Phương | `radius=90, childSkillNum=0, charAnimId=2, cost=25, baseSkill=0` | `radius=90 ✓, childSkillId=225, childSkillNum=4, charAnimId=9, MaxShadowNum=5, MslsGenerateData=4`. PC `xuezhan_bafang`: physenhance_p {{1,60},{20,723}} (mobile 80→385 sai lớn), attackrating_p {{1,75},{20,320}} (mobile 10→60 sai lớn), deadlystrike_p {{1,4},{20,25}} (mobile 10→30 sai) | **G4 childSkillNum=0 vs PC=4 (MẤT 4-HIT PATTERN)** + **G4 MaxShadowNum=5 bỏ** + **G4 charAnimId 2 vs 9** + **G7 physenhance_p sai lớn (60/723 vs 80/385)** + **G7 attackrating_p sai lớn (75/320 vs 10/60 sai 7.5×)** + **G7 deadlystrike_p sai** | **Cao** (4 chiêu liên tiếp = cốt lõi) | 2 giờ |
| 42 | Kim Chung Tráo (buff) | `radius=400, skillStyle=InitiativeNpcState, targetSelf, charAnimId=2, cost=30, duration=1200+1200*lv ms` | `radius=0 (buff), isMelee=0, charAnimId=11, ReqLevel=50, MaxLevel=20`. PC `jinzhong_zhao`: physicsres_p {{1,12},{20,50}} (mobile 10→40), coldres_p {{1,7},{20,45}} (mobile 5→25), **fireres_p {{1,-5},{20,-15}}** (mobile +5→+25, **DẤU SAI**), poisonres_p {{1,12},{20,49}} (mobile 0), **duration 18×120→18×180 = 120s→180s** | **G4 radius 400 vs 0** + **G4 charAnimId 2 vs 11** + **G7 physicsres_p sai** + **G7 coldres_p sai** + **G7 fireres_p DẤU SAI (âm ↔ dương)** + **G7 mất poisonres_p** + **G7 DURATION sai 50× (PC 120s vs mobile 2.4s L1; 180s vs 25.2s L20)** + **G7 stateSpecialId=49 bị bỏ** | **Cao** (fireres âm vs dương + duration sai 50×) | nửa ngày |
| 23, 24, 26 | (passive mastery) | match: addphysicsdamage_p, attackratingenhance_p, deadlystrikeenhance_p; charAnimId=14 ✓; cost=0 ✓ (CostValue=0); state duration=-1 (permanent) | PC `tianwang_qiangfa/daofa/chuifa` schema match. PC tianwang.lua: addphysicsdamage_p {{1,25},{20,315}} cho thương (mobile dùng 15→215 cho cả 3, sai 1.5×). PC đao/chuỳ tương tự. | **G7 thương physics sai (25/315 vs 15/215)**; đao 50/300 vs 15/215; chuỳ 25/275 vs 15/215. Mobile 3 skill giống hệt nhau (copy/paste) nhưng PC 3 khác nhau | Thấp | 1 giờ |
| 32, 37, 40, 41 | (MaxShadowNum=5) | mobile không xử lý MaxShadowNum | PC có MaxShadowNum=5 + MslsGenerate=5/8/5/4 | **G4 MaxShadowNum bỏ hoàn toàn** — `DamageSkillNew` không cover Melee, runtime `SpawnProjectiles` line 249 chỉ fire cho `skillStyle==Missiles` | Trung bình (visual feel) | nửa ngày (refactor SpawnProjectiles cho Melee hoặc thêm `MeleeType` field) |
| **All 9 active** | `charAnimId=2` (mobile default) | PC yêu cầu **9** (cho 8 skill) và **10** (riêng 35) | **G4 charAnimId sai toàn bộ** | Trung bình | 1 giờ (sửa 9 dòng) |
| **All 9 active** | `cost` hard-coded flat (10/15/20/25/30) | PC `CostValue=0` (dùng `skill_cost_v` từ tianwang.lua) | **G4 cost sai schema** — PC để CostValue=0 rồi `skill_cost_v` curve; mobile hard-code gần đúng nhưng không phải curve | Thấp | 1 giờ verify |
| **All 9 active** | `timePerCast=0, waitTime=0` | PC `WaitTime`+`TimePerCast` riêng (40=5/27, các khác 0/0) | **G4 timing sai** — multi-thrust skill 40 mất timing | Trung bình | 1 giờ (set waitTime=5, timePerCast=27 cho 40) |
| **All 9 active** | `horseLimit=0` (default) | PC: 30/31/35/41 HorseLimit=1 (cưỡi ngựa OK); 32/34/37/40 HorseLimit=0 (không cưỡi) | **G4 horseLimit sai 4 skill** (30/31/35/41 mất cưỡi-OK) | Thấp | 15 phút |
| **All 9 active** | `baseSkill=0` | PC: cả 9 = 0 (không chain base) | OK (đúng) | — | — |
| **All 9 active** | `stateSpecialId=0` (mobile) | PC: 33=46, 36=49, 42=49 (mod), 29/30/31/32/34/35/37/40/41=0 | **G4 stateSpecialId bỏ cho 3 buff/pasive** | Trung bình | 15 phút |
| **Tất cả 8 attack** | `PcSkillTuningRegistry.RadiusCurves[TianWangId]` cover 29/30/32/34/35/37/40/41 (8/9) | PC có 9 active | **G7 Tuning coverage 89%** (thiếu ID 31) | Thấp | 15 phút |
| **Tất cả 8 attack** | Registry dùng mobile radius 90/72/200 | PC: 29=72, 30=90, 31=72, 32=90, 34=72, 35=90, 37=90, 40=200, 41=90 | **G4 ID 29 catalog 90 vs registry 72 inconsistency** (7/9 match) | Thấp | 30 phút |

### NEW gaps (chưa có trong sơ bộ)

| ID | Gap mới | Severity | Effort | Phát hiện từ |
|---:|---|---|---|---|
| **G2** (sub-skill gate) | `CombatRuntimeService.SpawnProjectiles` line 249: `if (skill.skillStyle != PcSkillStyle.Missiles || skill.childSkillNum <= 0) return;` → **childSkillNum hoàn toàn bị bỏ cho Melee style**. Tất cả 9 tianwang active là `Melee` → `childSkillId`/`childSkillNum` vô hiệu. Multi-hit 30/35/41 (2/3/4 hit) + child skill 219/220/221/222/224/225/404/405/406 đều không trong catalog (chưa register) | **Cao** (chặn toàn bộ Phase 1 multi-hit restoration) | 1-2 ngày (refactor) | `CombatRuntimeService.cs:249` |
| **MeleeType** | `SkillDefinition.cs` không có `meleeType` field; `PcSkillStyle` enum chỉ có `Melee=1` đơn. PC `Melee_AttackWithBlur`/`Melee_Jump`/`Melee_JumpAndAttack`/`Melee_RunAndAttack`/`Melee_ManyAttack` không phân biệt được. Mặc định `Melee_AttackWithBlur` cho cả 9 → instant swing 1 hit, không thể represent multi-hit pattern. | **Cao** (kiến trúc) | 1-2 ngày (thêm enum + field + dispatcher) | `SkillDefinition.cs:13-50` + `CombatDefinition.cs:25-32` |
| **G7 duration bug** (33 + 42) | PC tianwang.lua dùng `{{1,18*120},{20,18*180}}` = 120s/180s; mobile dùng `1200+1200*lv` ms = 2.4s/25.2s. **SAI 50× ở L1, sai 7× ở L20**. Buff 33 (Tĩnh Tâm Quyết) + 42 (Kim Chung Tráo) hết hiệu lực sau vài giây thay vì vài phút. | **Cao** (gameplay) | 30 phút (sửa formula duration cho 33 + 42) | `tianwang.lua:153-155 (jingxin_jue)` + `tianwang.lua:273-281 (jinzhong_zhao)` |
| **G7 ID 40 tianwang.lua param curves** | `skill_param1_v={{1,4},{5,12},{20,24},{28,31},{31,31}}` + `skill_param2_v={{1,18},{20,1},{21,1}}` — level curve cho số thrust và timing, bị bỏ hoàn toàn trong mobile | Cao | 2 giờ (thêm `param1`/`param2`/level curves cho 40) | `tianwang.lua:262-264 (duanhun_ci)` |
| **G4 MisslesForm=11** (skill 40) | PC `MisslesForm=11` cho 40 (vs 12 cho tất cả skill khác) — form "thrust" đặc biệt, khác form 12 "swing". Mobile dùng `SkillMissileForm.Single` (giá trị=1) cho hầu hết Melee; thiếu form 11 | TB | 1 giờ (thêm `Thrust=11` enum + dispatch) | `ModSkills.txt` col 20 cho ID 40 |
| **G4 baseSkill / stateSpecialId** | PC: 33=stateSpecial 46, 36/42=49. Mobile: tất cả 0. State priority/buff type không khớp. | TB | 15 phút | `ModSkills.txt` col 10 (StateSpecialId) |
| **G4 horseLimit** | PC: 30/31/35/41=1 (cưỡi OK), 32/34/37/40=0. Mobile: tất cả 0 | Thấp | 15 phút | `ModSkills.txt` col 57 (HorseLimit) |
| **G4 timing waitTime/TimePerCast** | PC: 40=WaitTime 5, TimePerCast 27. Mobile: tất cả 0 | TB | 1 giờ (chỉ sửa 40) | `ModSkills.txt` col 28+33 |
| **G5 tên Chinese ID 37** | Mobile: `"八风斩"` (8-wind) vs PC Lua `pofeng_zhan` = "泼风斩" (splash-wind). Vietnamese "Bát Phong Trảm" dù chính xác hơn, nhưng cần thống nhất với PC Chinese | Thấp (cosmetic) | 5 phút | so sánh `PcCombatCatalogFactory.cs:1643` vs `tianwang.lua:229` |

---

## 2.2.3. Phase 1 quick wins (CRITICAL — multi-hit restoration + duration bug)

> **2 gap nghiêm trọng nhất cần fix Phase 1**:
> 1. **G2 (sub-skill gate)**: chặn 100% multi-hit. Phải refactor runtime trước khi set `childSkillNum` cho từng skill.
> 2. **G7 duration bug 33/42**: sai 50× → buff vô dụng.

- [ ] **G2 root cause fix** (PREREQUISITE cho Phase 1): `CombatRuntimeService.SpawnProjectiles` line 249 thêm nhánh `Melee` — thay vì skip, dispatch sang `Melee_ManyAttack`/`Melee_AttackWithBlur` dựa trên `childSkillNum`/`MaxShadowNum`. Cần thêm `MeleeType` enum + field `meleeType` vào `SkillDefinition`. Effort 1-2 ngày, chặn mọi multi-hit của tất cả môn phái melee (Cái Bang, Võ Đang, Côn Lôn, Ngũ Độc).
- [ ] **G2 register child skills** 219, 220, 221, 222, 224, 225, 404, 405, 406 vào catalog (hoặc dùng sub-skill ID mapping từ PC engine). Các child skill này là PC-internal multi-hit engine, không phải skill player thấy. (G2, 2-3 giờ)
- [ ] **G7 duration fix ID 33**: sửa `d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AttackRatingEnhanceP, ..., 1200 + 1200 * lv, 0))` → `... , (int)(18f * 120 * 1000 / 18f) = 120000 + 60000*lv` ms. PC: 18×120=2160 unit × (1s/18 unit) = 120s at L1, 18×180=3240 → 180s at L20. Mobile cần `120000 + (lv-1)*60000/19*...` hoặc dùng `Link(lv, (1, 120000, ""), (20, 180000, ""))`. Đồng thời sửa `attackratingenhance_p` 10→100 thành 45→400. Sửa `charAnimId=2`→11, `radius=400`→0, thêm `stateSpecialId=46`. (G7, 1 giờ)
- [ ] **G7 duration fix ID 42**: tương tự 33 — duration 120s→180s cho cả 4 state (physics/cold/fire/poison res). Sửa `fireres_p` từ `+5→+25` → `-5→-15` (PC âm). Sửa `physicsres_p 10/40 → 12/50`, `coldres_p 5/25 → 7/45`, thêm `poisonres_p {{1,12},{20,49}}`. Sửa `charAnimId=2`→11, `radius=400`→0, thêm `stateSpecialId=49`. (G7, 2 giờ)
- [ ] **ID 30 Hồi Phong Lạc Nhạn** (2-hit, sau G2): thêm `s.childSkillId=219; s.childSkillNum=2; s.baseSkill=false;` (PC BaseSkill=0). (G4, 30 phút sau G2)
- [ ] **ID 35 Dương Quan Tam Điệp** (3-hit, sau G2): thêm `s.childSkillId=221; s.childSkillNum=3; s.charAnimId=10;`. (G4, 30 phút sau G2)
- [ ] **ID 41 Huyết Chiến Bát Phương** (4-hit + 5 shadow, sau G2): thêm `s.childSkillId=225; s.childSkillNum=4; s.maxShadowNum=5;`. (G4, 30 phút sau G2)
- [ ] **ID 40 Đoạn Hồn Thích** (multi-thrust + 5 shadow + param curves): thêm `s.childSkillId=224; s.childSkillNum=1; s.maxShadowNum=5; s.waitTime=5; s.timePerCast=27; s.param1=32; s.param2=5; s.missileForm=Thrust(=11);`. Thêm `skill_param1_v={{1,4},{5,12},{20,24},{28,31},{31,31}}` + `skill_param2_v={{1,18},{20,1},{21,1}}` curves. (G4, nửa ngày)
- [ ] **ID 29, 31, 32, 34, 37** (sau G2): thêm `s.childSkillId = 405/406/220/404/222; s.childSkillNum=1;` cho 5 skill còn lại. ID 32/37 thêm `s.maxShadowNum=5;`. (G4, 1 giờ tổng sau G2)
- [ ] **All 9 active charAnimId**: sửa `charAnimId=2` → `9` (29/30/31/32/34/37/40/41), `10` (35), `11` (33/42), `14` (23/24/26/36 — đã đúng). (G4, 1 giờ)
- [ ] **ID 36 Thiên Vương Chiến Ý** (passive): thêm 3 attribute bị thiếu — `lifemax_p {{1,21},{30,185}}` + `lifemax_yan_p {{1,21},{35,160}}` + `attackspeed_v {{1,5},{30,65}}`. Sửa `deadlystrikeenhance_p` 2/20 → 5/45. Sửa `charAnimId=14`→11. Thêm `stateSpecialId=49`. (G7, 2 giờ)
- [ ] **G7 mastery passive** (23/24/26): sửa copy/paste thành 3 giá trị riêng: thương `addphysicsdamage_p {{1,25},{20,315}}` (mobile 15→215), đao `{{1,50},{20,300}}`, chuỳ `{{1,25},{20,275}}`. (G7, 1 giờ)
- [ ] **G4 horseLimit**: sửa 30/31/35/41 → `s.horseLimit=1`; 32/34/37/40 giữ 0. (G4, 15 phút)
- [ ] **G4 waitTime/timePerCast**: thêm `s.waitTime=5; s.timePerCast=27;` cho skill 40. (G4, 5 phút)
- [ ] **ID 31 visual missing**: thêm `case 31: // Hành Vân Quyết` vào `ConfigureTianWangVisuals` (line 1079). PC `xingyun_jue` không có missile riêng, dùng chung `42ed0184` preCast + 1 stationary effect (giống 29/35). (G7, 1 giờ)
- [ ] **ID 31 PcSkillTuningRegistry**: thêm `[31] = new[] { (1, 72), (20, 72) }` vào `RadiusCurves[TianWangId]` (line 42). (G7, 15 phút)
- [ ] **ID 29 radius catalog vs registry**: catalog hardcode 90, registry 72. Sửa catalog `radius=90` → `72` cho khớp PC. (G4, 15 phút)
- [ ] **ID 31 radius catalog**: sửa 80 → 72 (PC). (G4, 15 phút)
- [ ] **G5 ID 37 Chinese name**: thay `"八风斩"` → `"泼风斩"` (mobile) cho khớp PC `pofeng_zhan`. Vietnamese "Bát Phong Trảm" giữ nguyên. (G5, 5 phút)

---

## 2.2.4. Phase 3 dash (G1) — N/A

- [ ] **Không áp dụng** — KNpc.cpp `CastMeleeSkill` switch (line 1829-1891) xác nhận: **0/9 skill Thiên Vương (ID 23-42) thuộc nhánh `Melee_Jump`/`Melee_JumpAndAttack`/`Melee_RunAndAttack`**. Tất cả 9 active dùng `Melee_AttackWithBlur` (mặc định khi không set `MeleeType`). ModSkills.txt confirm: `IsMelee=1` + `MslsGenerate=1` cho cả 9, không có flag dash. `tianwang.lua` không tham chiếu `NewJump`/`DoJumpAttack`/`DoRunAttack`. Phase 3 bỏ qua cho toàn bộ Thiên Vương.

---

## 2.2.5. Phase 4 event chain (G6) — N/A cho range 23-42

- [ ] **Không áp dụng** — ModSkills.txt verified:
  - `StartEvent` (col 45): 0 cho tất cả 15 skill tianwang
  - `FlyEvent` (col 47): 1 cho 29/30/31/32/34/35/37/41; 0 cho 40; 0 cho 33/36/42 (buff/passive)
  - `CollideEvent` (col 50): 0 cho tất cả 15 skill tianwang
  - `ShowEvent` (col 68): empty cho tất cả
  - 150-tier sub-form `daotianwang150` (line 384-389 tianwang.lua) mới có `skill_collideevent` + `skill_showevent` — ngoài scope task này
- [ ] **Chú ý FlyEvent=1 cho 8 active skills**: PC `FlyEvent=1` nghĩa là "phát event khi missile bay tới giữa đường" — nhưng mobile không đọc `FlyEvent` từ catalog. Tạm thời OK vì visual cố định trong `ConfigureTianWangVisuals`. Phase 5 mới cần event chain.

---

## 2.2.6. Trạng thái

- [x] Catalog scan xong (15 skill: 4 passive, 2 self-buff, 9 active melee)
- [x] **Verified sơ bộ** từ sơ bộ trước: 13/13 gap confirmed (29/30/31/32/34/35/36/37/40/41/42/3 mastery/all 9 active). Severity + effort cập nhật từ data verified.
- [x] **G6 event chain** verified: FlyEvent=1 cho 8 skills, CollideEvent=0, ShowEvent empty → Phase 4 bỏ qua range 23-42
- [x] **Skill 40 + 41 dash check** verified: **KHÔNG dash** (cả 2 là `Melee_AttackWithBlur` mặc định + multi-hit pattern, không phải player-jump). Skill 40 = multi-thrust 5×5 timing, Skill 41 = 4-hit child + 5 shadow
- [ ] **G2 (sub-skill gate)** — chưa fix, chặn mọi multi-hit. **PREREQUISITE** cho Phase 1
- [ ] Quick-win phase merged (Phase 1 — 15 items, priority: G2 root cause → 33/42 duration bug → 30/35/40/41 multi-hit + 36/42 attribute fix)
- [ ] Dash phase merged: **không áp dụng** (Thiên Vương không có dash, đã verified từ KNpc.cpp)
- [ ] Event chain phase merged: **không áp dụng** (range 23-42 không có CollideEvent/ShowEvent)
- [ ] Tuning coverage 89% → 100% (chỉ thiếu ID 31, fix 15 phút)

---

## 2.2.7. Verified findings tóm tắt

### Confirmed sơ bộ (13 gaps)
| ID | Gap | Severity | Effort | Status |
|---:|---|---|---|---|
| 30 | G4 childSkillNum=0 vs PC=2 | Cao | 2 giờ | ✓ confirmed (PC ModSkills.txt childSkillId=219, childSkillNum=2) |
| 35 | G4 childSkillNum=0 vs PC=3 | Cao | 2 giờ | ✓ confirmed (PC childSkillId=221, childSkillNum=3, charAnim=10) |
| 41 | G4 childSkillNum=0 vs PC=4 | Cao | 2 giờ | ✓ confirmed (PC childSkillId=225, childSkillNum=4, maxShadow=5) |
| 40 | G4 childSkillNum=0 + waitTime/TimePerCast bỏ + param1/param2 bỏ + MslsForm=11 | Cao | nửa ngày | ✓ confirmed (PC childSkillId=224, waitTime=5, timePerCast=27, param1=32, param2=5, MslsForm=11) |
| 29,31,32,34,37 | G4 childSkillNum=0 vs PC=1 (5 skill) + MaxShadowNum bỏ (32/37) | Cao | 2 giờ tổng | ✓ confirmed |
| 36 | G7 mất lifemax/lifemax_yan/attackspeed, sai deadlystrike | Cao | 2 giờ | ✓ confirmed (PC tianwang.lua 222-228) |
| 42 | G7 fireres_p dấu sai (+ ↔ -) + duration sai 50× + mất poisonres | Cao | nửa ngày | ✓ confirmed + duration bug (PC 120s/180s vs mobile 2.4s/25.2s) |
| 33 | G7 attackratingenhance_p sai 4× + charAnimId sai + duration sai 50× | Cao | 1 giờ | ✓ confirmed + duration bug |
| All 9 active | G4 charAnimId=2 vs PC 9/10 | TB | 1 giờ | ✓ confirmed |
| 32,37,40,41 | G4 MaxShadowNum=5 bỏ | TB | nửa ngày | ✓ confirmed |
| 31 | G7 visual case thiếu + tuning thiếu + mất colddamage_v + radius 80 vs 72 | Cao | 2 giờ | ✓ confirmed |
| 29 | G4 radius 90 vs 72 (catalog/registry inconsistency) | Thấp | 30 phút | ✓ confirmed |

### NEW gaps (4 phát hiện mới)
| ID | Gap mới | Severity | Effort |
|---:|---|---|---|
| **G2** (sub-skill gate) | `SpawnProjectiles` line 249 skip Melee style → `childSkillId`/`childSkillNum` vô hiệu cho cả 9 tianwang melee + mọi melee môn phái khác | **Cao** | 1-2 ngày |
| **MeleeType** | `SkillDefinition` không có `meleeType` field → 5 loại MeleeType của PC (`AttackWithBlur`/`Jump`/`JumpAndAttack`/`RunAndAttack`/`ManyAttack`) không phân biệt được | **Cao** | 1-2 ngày |
| **G7 duration bug 33/42** | 120s/180s PC vs 2.4s/25.2s mobile → sai 50× ở L1 | **Cao** | 30 phút |
| **G7 40 param curves** | `skill_param1_v`/`skill_param2_v` (số thrust + timing) bỏ hoàn toàn | Cao | 2 giờ |
| **G4 40 MslsForm=11** | Thrust form (vs 12 swing) — chưa có enum value | TB | 1 giờ |
| **G4 stateSpecialId/horseLimit** | 6 skill thiếu stateSpecialId (33/36/42) + 4 skill sai horseLimit (30/31/35/41) | TB | 15 phút |
| **G5 tên Chinese ID 37** | "八风斩" mobile vs "泼风斩" PC | Thấp | 5 phút |
| **G7 mastery 23/24/26 copy/paste** | 3 mastery passive cùng giá trị 15/215 nhưng PC khác (25/315, 50/300, 25/275) | Thấp | 1 giờ |

### Skill 40 + 41 dash check (kết quả cuối)
- **Skill 40 Đoạn Hồn Thích**: `MslsForm=11` + `WaitTime=5, TimePerCast=27` + `Param1=32, Param2=5` → **multi-thrust 5 lần**, mỗi lần 5 ticks wait. KHÔNG có `NewJump`/`DoJumpAttack` → không dash. Vẫn là instant melee (caster chém 5 lần tại chỗ với 5 tia missile).
- **Skill 41 Huyết Chiến Bát Phương**: `MslsForm=12, childSkillNum=4, MaxShadowNum=5, MslsGenerateData=4` → **4-hit + 5 shadow**. KHÔNG có dash flag. Vẫn là instant melee (caster chém 4 lần + 5 bóng).
- **Kết luận**: KNpc.cpp `CastMeleeSkill` switch line 1829-1891, default branch (line 1885-1887) `m_ProcessAI = 1` chỉ xảy ra khi `MeleeType` không khớp case nào → fallback là instant 1 hit. Cả 9 tianwang active dùng default. Multi-hit thông qua `childSkillNum` (engine-side child skill chain) chứ không phải MeleeType.

### Phase 5 (future) — out of scope cho task này
- Port 150-tier sub-form `daotianwang150` / `qiangtianwang150` / `chuitianwang150` (có `skill_collideevent` / `skill_showevent`)
- Port mastery `daoxutian` (passive 20-level, có `allres_p`, `lifereplenish_p`, `ignoreskill_p`)
- Port 4 active skills còn thiếu trong catalog: `chenglong_jue` (Thừa Long Quyết, MaxShadow=5, MslsGenerate=8), `potian_zhan` (Phá Thiên Trảm), `zhuixing_zhuyue` (Truy Tinh Trục Nguyệt), `zhuifeng_jue` (Truy Phong Quyết)
- ID range ngoài 23-42 (PC skills.txt có thể ở 700+ range)

---

## Test plan (verify sau khi fix)
- Unit test `childSkillNum` sequence: cast 30 ở level 10 → expect 2 hit mỗi 1 PC tick (sau khi G2 fix)
- Visual test cast 35 ở close range: expect 3 swing animation liên tiếp (charAnimId=10) + 1 missile impact
- Regression test cast 41: 4 hit tập trung vào target, 5 shadow quanh caster
- Buff duration test 33/42: cast ở level 10, expect buff còn hiệu lực sau 60s, hết sau ~170s (PC duration), KHÔNG phải sau 12s (mobile cũ)
- Cast 40: 5 tia missile liên tiếp cách nhau 5 ticks, tổng TimePerCast 27
- Cast 36 Thiên Vương Chiến Ý: passive → HP max tăng 185% (L30), atk speed +65, deadly +45%
- Cast 42 Kim Chung Tráo: fire res **GIẢM** 5-15% (PC âm, gây thiệt hại khi dùng buff — phải verify visual)

---

## Tổng kết
- **13 gap từ sơ bộ**: tất cả CONFIRMED, severity/effort giữ nguyên hoặc điều chỉnh theo data verified
- **4 gap mới phát hiện**:
  1. **G2 sub-skill gate** (Cao, 1-2 ngày) — chặn multi-hit, cần fix root cause
  2. **MeleeType missing** (Cao, 1-2 ngày) — kiến trúc, cần thêm enum
  3. **G7 duration bug 33/42** (Cao, 30 phút) — sai 50×, fix ngay
  4. **G7 40 param curves** (Cao, 2 giờ) — bỏ `skill_param1_v`/`skill_param2_v`
- **Skill 40 + 41 dash check**: KHÔNG dash (verified từ KNpc.cpp), là multi-hit/multi-thrust pattern
- **Priority Phase 1**: G2 root cause → 33/42 duration → 30/35/40/41 multi-hit → 36/42 attribute fix
