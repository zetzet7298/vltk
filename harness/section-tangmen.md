## 2.4. Đường Môn (PC: TangMen 唐门, ID range 43-58)

> **Nguồn PC**:
> - `Assets/StreamingAssets/Reference/PcSkills.txt` (canonical, TCVN3, 8 active + 2 buff + 1 mastery + 1 resist + 1 passive = 10 skill ID 43-58)
> - `/var/www/vltksource_new/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/.../script/skill/tangmen.lua` (GB18030, 749 dòng, 25 skills kể cả 80/150-tier)
> - Per-skill files: `script/skill/tangmen/*.lua` (2 file pinyin) + `script/skill/tangmeng/*.lua` (19 file GB2312 bao gồm trùng lặp)
> - C++: `KNpc.cpp::CastMeleeSkill` switch (line 1834) — **không có** TangMen skill nào thuộc `Melee_Jump/JumpAndAttack/RunAndAttack` (tất cả `IsMelee=0`, thuần ranged). Do đó **G1 (dash) không áp dụng** cho Đường Môn.
> - Tóm tắt: Đường Môn là môn phái **ranged ám khí / poison thuần**, 5 active attack (45/47/50/54/58) đều dùng `PcSkillStyle.Missiles` + `childSkillId/childSkillNum` cho sub-missile. Gap nặng nhất: **G4 (waitTime=5 bị bỏ ở 5/5 attack)** + **G4 (req level sai ID 47/54)** + **G4 (ID 54 missile form sai Single vs Fan)** + **G4 (ID 50 MslsGenData=4 bị bỏ)** + **G6 (ID 58 CollideEvent 1→227 thiếu)** + **G7 (radius flat curve sai PC, lighting res formula sai PC cho 51)**.

### 2.4.1. Catalog scan

| ID | Tên (mobile + PC) | PC Skills.txt cols | PC tangmen.lua | Vai trò |
|---:|---|---|---|---|
| 43 | Đường Môn Ám Khí (唐门暗器) | `ReqLevel=10, MaxLevel=20, IsMelee=0, CharAnimId=0, Form=7, ChildId=0` | `tangmen_anqi` (passive) | Buff `addphysicsdamage_p` 25→215 + atk rating 7 |
| 45 | Tích Lịch Đơn (霹雳弹) | `ReqLevel=10, MaxLevel=20, IsMelee=0, CharAnimId=11, Form=1, ChildId=35, ChildNum=1, Wait=5, Cost=0, Time=0, EqtLimit=-2, HorseLimit=0` | `pili_dan` (VanishEvent 1113 L20, ShowEvent 8) | Active missile + poison V 1→5 + 9 addskilldamage |
| 47 | Đoạt Hồn Tiêu (夺魂镖) | **`ReqLevel=30` (mobile: 10), MaxLevel=20, IsMelee=0, CharAnimId=11, Form=1, ChildId=116, ChildNum=1, Wait=5, EqtLimit=100 (Phi tiêu), HorseLimit=1** | `duohun_biao` (rad 384→448, speed 24→28, cost 5→16) | Active missile + poison V 3→8 + deadly 2→12 |
| 48 | Tâm Nhãn (心眼) | `ReqLevel=60, MaxLevel=30, IsMelee=0, CharAnimId=14, Form=7, ChildId=0` | `xinyan` (addcold 10→110, addpoison 1→10, addphys 15→115, deadly 8→26, atkspd 29→106) | Passive mastery (5 buff gộp) |
| 50 | Truy Tâm Tiễn (追心箭) | `ReqLevel=30, MaxLevel=20, IsMelee=0, CharAnimId=11, Form=1, ChildId=37, ChildNum=2, **MslsGen=2, MslsGenData=4**, Wait=5, EqtLimit=101 (Phi đao), HorseLimit=1` | `zhuixin_jian` (rad 384→448, speed 24→28, cost 20) | Active 2 base + 2 gen + 2 gen = 6 missile, poison 3→8 |
| 51 | Thanh Mộc (青木) | `ReqLevel=30, MaxLevel=20, IsMelee=0, CharAnimId=14, Form=7, ChildId=0` | `青木功.lua` `lightingres_p = floor(log10(level+1)/2*60)` | Passive resist lightning |
| 54 | Mạn Thiên Hoa Vũ (漫天花雨) | **`ReqLevel=30` (mobile: 50), MaxLevel=20, IsMelee=0, CharAnimId=11, Form=6 (Fan), ChildId=38, ChildNum=1, Wait=5, EqtLimit=102, HorseLimit=1** | `mantian_huayu` (rad 384→416, cost 40 flat) | Active Fan missile + poison 3→8 |
| 55 | Thối Độc Thuật (淬毒术) | `ReqLevel=40, MaxLevel=20, IsMelee=0, CharAnimId=11, Form=7, ChildId=0` | per-skill file `tangmeng/淬毒术.lua` (addpoison 2→25, dur 1200-15600) | Self/ally poison buff (no missile) |
| 57 | Băng Phách Hàn Quang (冰魄寒光) | `ReqLevel=50, MaxLevel=20, IsMelee=0, CharAnimId=11, Form=7, ChildId=0` | per-skill file `tangmeng/冰附加.lua` (addcold 2→25, dur 1200-15600) | Self/ally cold buff (no missile) |
| 58 | Thiên La Địa Võng (天罗地网) | `ReqLevel=60, MaxLevel=20, IsMelee=0, CharAnimId=11, Form=1, ChildId=67, ChildNum=1, Wait=5, EqtLimit=102, HorseLimit=0`, **`CollideEvent=1, CollidSkillId=227` (PC: Vạn Lý Truy Tâm tiểu phi đao)**, `Param1=0` | `tianluo_diwang` + `tianluo_diwang1` (rad 448→512, cost 45→65, speed 26→28) | Active single missile + collide sub 227 |

**Không catalog trong mobile** (PC tangmen.lua có, mobile thiếu — Phase 5):
- **80-tier sub-skill (player)**:
  - 249 (Tiểu Lý Phi Đao 小李飞刀) — `ReqLevel=60, EqtLimit=101, Cost=50, rad=350, ChildId=106, Wait=5, CharAnim=11, Attrib=302, Icon=\spr\Ui\技能图标\icon_sk_小李飞刀.spr`
  - 250 (Tiểu Lý Phi Đao pc variant) — same icon
  - 302 (Bão Vũ Lê Hoa 暴雨梨花) — `ReqLevel=80, EqtLimit=102, Cost=0, rad=470, ChildId=96, FlyEvent=301 L20, FlyEventTime=30, Form=6, CharAnim=11` — có `skill_flyevent` chain
  - 340 (Ngân Đao Xạ Nguyệt 银刀射月) — `ReqLevel=80, EqtLimit=101, rad=400, ChildId=150`
  - 341 (Tản Hoa Tiêu 散花镖) — `ReqLevel=60, EqtLimit=100, ChildNum=5, rad=400, Form=2 (Fan)`
  - 342 (Cửu Cung Phi Tinh 九宫飞星) — `ReqLevel=80, EqtLimit=100, ChildNum=5, rad=360, Form=2 (Fan), CharAnim=11`
- **150-tier sub-skill (player)**:
  - 1069 (Vô Ảnh Xuyên 无影穿) — `ReqLevel=150, EqtLimit=101, rad=360, ChildId=331, ChildNum=1, Wait=3, CollidSkillId=1097` (sub 1097 = Truy Tâm Tọa Mệnh)
  - 1070 (Thiết Liên Tứ Sát 铁链四杀) — `ReqLevel=150, EqtLimit=102, rad=470, ChildId=332, ChildNum=1, Wait=5, **FlyEvent=1, FlySkillId=1098, FlyEventTime=18**, Form=6` (sub 1098 = Thiết Sa Xạ Tinh, fire mỗi 18 tick bay)
  - 1071 (Càn Khôn Nhất Trích 乾坤一掷) — `ReqLevel=150, EqtLimit=100, rad=360, ChildId=333, ChildNum=5, Wait=2, Form=2 (Fan), MslsGen=5`
  - 1097, 1098, 1099, 1100 (sub-evt visual / chain)

**IsTangMenSkill exclude**: 44, 46, 49, 52, 53, 56 không thuộc Đường Môn (ModSkills.txt confirm các ID này là shared/common skill khác). Không phải gap.

### 2.4.2. Gap table

> **Chú thích cột "Hành vi mobile"**: lấy từ `CreateTangMenSkills` trong `PcCombatCatalogFactory.cs` line 1073-1230. Mọi skill attack Đường Môn dùng `PcSkillStyle.Missiles` + `childSkillId` (sub-missile sprite) + `childSkillNum` (số lượng missile).
>
> **Chú thích cột "Hành vi PC"**: lấy từ `PcSkills.txt` (ReqLevel, ChildId, ChildNum, MslsGen, MslsGenData, CharAnimId, WaitTime, CostValue, EqtLimit, HorseLimit, CollideEvent) + `tangmen.lua` (damage curves, radius) + per-skill files trong `tangmen/` + `tangmeng/`.

| ID | Tên | Hành vi mobile | Hành vi PC | Gap | Severity | Effort |
|---:|---|---|---|---|---|---|
| 43 | Đường Môn Ám Khí | `BaseSkill(43, ..., 10, 20, 0, ...)` + `AddPhysicsDamageP Link(lv,(1,25,""),(20,215,""))` + atk rating 7 | `tangmen_anqi` — `addphysicsdamage_p={{1,25},{20,215}},{{1,-1},{2,-1}},{{1,7},{2,7}}` | **OK** (mobile set `d.state.Add(...,AddPhysicsDamageP,...,7)` đúng cả 3 tuple `[1][2][3]`) | — | — |
| **45** | Tích Lịch Đơn | `BaseSkill(45, ..., 10, 20, 400, Single)`; `childSkillId=35, childSkillNum=1`; `charAnim=11`; `phys 20→80, poison V 1→5/60s/10, deadly 1→8, series 1→10, cost=12`; **`s.waitTime` KHÔNG set** (default 0) | `pili_dan` — `physicsenhance_p 20→80, poisondamage_v 1→5 (60s dur 10), deadlystrike_p 1→8, seriesdamage_p 1→10, skill_cost_v 12 flat` + **`WaitTime=5`** + `VanishedEvent 1,1113` (vanish sub-skill 1113 = Tích Lịch Loạn Hoàn Hãm Tĩnh, level-gated) + `ShowEvent 8` | **G4 waitTime=5 bị bỏ** (cast cadence sai PC: mỗi missile fire sau 5 PC tick thay vì instant). **`G6 VanishedEvent 1113 bị bỏ`** (missile 45 fire sub-skill 1113 khi vanish — visual explosion chain) + `ShowEvent 8` bị bỏ. | Cao (cadence + visual chain mất) | 2 giờ |
| **47** | Đoạt Hồn Tiêu | `BaseSkill(47, ..., 10, 20, 450, Single)`; `childSkillId=116, childSkillNum=1`; `phys 25→115, poison 3→8, deadly 2→12, series 5→30, cost Link(1,5,20,16)`; **`s.waitTime` KHÔNG set**; `req=10`, `horseLimit=0` (mặc định) | `duohun_biao` — `physicsenhance_p 25→115, poisondamage_v 3→8, deadlystrike_p 2→12, seriesdamage_p 5→30, skill_cost_v 5→16, missle_speed_v 24→28, skill_attackradius 384→448` + **`ReqLevel=30` (mobile sai: 10)** + **`WaitTime=5`** + **`EqtLimit=100 (Phi tiêu)`** (mobile không set) + **`HorseLimit=1`** (mobile không set) + `CharAnimId=11` ✓ | **G4 req 10 vs PC 30** (cao hơn 20 level sai, gameplay progression sai) + **G4 waitTime=5 bị bỏ** + **G4 horseLimit=0 vs PC 1** (player cưỡi ngựa dùng được Đoạt Hồn Tiêu trên PC, mobile cấm) + **G7 EqtLimit 100 (Phi tiêu) bị bỏ** (không enforce vũ khí) + radius hardcode 450 vs PC curve 384→448 | Cao (req sai = progression broken + weapon check miss) | 1 giờ |
| 48 | Tâm Nhãn | `BaseSkill(48, ..., 30, 30, 0, ...)`; passive 5-attr: addcold 10→110 (∞), addpoison 1→10/10t (∞), addphys 15→115/7 (∞), deadly 8→26 (∞), atkspd 29→106 (∞) | `xinyan` — `addcolddamage_v 10→110, addpoisondamage_v 1→10 (10 dur), addphysicsdamage_p 15→115 (7), deadlystrikeenhance_p 8→26, attackspeed_v 29→106 → 33→113 → 35→149 → 38→156 → 39→159 → 40→162` + **`lifemax_yan_p 21→20 (L35-36)`** (bị bỏ) | **G4 attackspeed_v curve thiếu điểm giữa 30/35/38/40** (PC có nhiều breakpoint) + **G4 lifemax_yan_p 21→20 L35-36 bị bỏ** (smoke bonus) | Trung bình | 1 giờ |
| **50** | Truy Tâm Tiễn | `BaseSkill(50, ..., 30, 20, 360, Single)`; `childSkillId=37, childSkillNum=2`; `missilesGenerateData=0` (mặc định) | `zhuixin_jian` — `ChildNum=2, MslsGen=2, MslsGenData=4` (PC: 2 base missiles + 2 generated missiles × 4 = 8 sub-missile slots, runtime sinh 2-4 thêm) + `WaitTime=5` + `EqtLimit=101` + `HorseLimit=1` | **G4 MslsGenData=4 bị bỏ** (PC: missile 50 generate thêm 4 sub-missile sau khi va chạm — mobile chỉ fire 2 base) + **G4 waitTime=5 bị bỏ** + **G7 EqtLimit 101 (Phi đao) bị bỏ** + **G4 horseLimit=0 vs PC 1** | Cao (mất cảm giác 6-8 phi tiêu "truy tâm") | 2 giờ |
| **51** | Thanh Mộc | `PassiveResist(51, ..., 30, LightingResP)` — formula `Floor(Log10(lv+5)/2*50)` | `tangmeng/青木功.lua` — formula `floor(log10(level+1)/2*60)` | **G7 lighting res formula sai PC** (mobile L1: `Floor(Log10(6)/2*50)=19`, PC L1: `floor(log10(2)/2*60)=9` → 2.1× sai; mobile L20: `Floor(Log10(25)/2*50)=34`, PC L20: `floor(log10(21)/2*60)=39` → 13% sai). Sai vì dùng generic `PassiveResist` helper thay vì PC per-skill formula. | Trung bình (resistance sai) | 1 giờ (override formula cho ID 51) |
| **54** | Mạn Thiên Hoa Vũ | `BaseSkill(54, ..., 50, 20, 400, Single)`; `childSkillId=38, childSkillNum=1` | `mantian_huayu` — `Form=6 (Fan)`, `ReqLevel=30`, `EqtLimit=102`, `HorseLimit=1`, `WaitTime=5`, `skill_attackradius 384→416` | **G4 req 50 vs PC 30** (cao hơn 20 level sai) + **G4 missile form Single vs Fan (6)** (mất hiệu ứng "mưa hoa rải đều") + **G4 waitTime=5 bị bỏ** + **G7 EqtLimit 102 bị bỏ** + **G4 horseLimit=0 vs PC 1** + **G4 EventSkillLevel=1 bị bỏ** (PC dùng `EvtLvl=1`, mobile không set) | Cao (form Fan mất = cốt lõi "mạn thiên hoa vũ" tan tành) | 2 giờ |
| 55 | Thối Độc Thuật | `BaseSkill(55, ..., 30, 20, 400, Surround)`; `targetSelf=true, targetAlly=true`; `effectSourceId=\spr\skill\天忍\mag_tr_16_施魔法.spr`; `addpoison 2→25 dur 1200-15600` | per-skill `tangmeng/淬毒术.lua` — `addpoisondamage_v 2→25, dur 1200 + 1200*lv, time 10` + `skill_cost_v 20` | **OK** (mobile matches PC formula) | — | — |
| 57 | Băng Phách Hàn Quang | `BaseSkill(57, ..., 30, 20, 400, Surround)`; `addcold 2→25 dur 1200-15600` | per-skill `tangmeng/冰附加.lua` — `addcolddamage_v 2→25, dur 1200 + 1200*lv, time 0` + `skill_cost_v 20` | **OK** (mobile matches PC formula) | — | — |
| **58** | Thiên La Địa Võng | `BaseSkill(58, ..., 50, 20, 520, Single)`; `childSkillId=67, childSkillNum=1`; `cost Link(1,45,20,65)` | `tianluo_diwang` — `ReqLevel=60, EqtLimit=102, HorseLimit=0, WaitTime=5, EventSkillLevel=1, **CollideEvent=1, CollidSkillId=227** (PC: Vạn Lý Truy Tâm tiểu phi đao — sub-skill 227 fire khi missile 58 va chạm NPC) | **G4 req 50 vs PC 60** (thấp hơn 10 level sai) + **G4 waitTime=5 bị bỏ** + **G4 EventSkillLevel=1 bị bỏ** + **G7 EqtLimit 102 bị bỏ** + **`G6 CollideEvent 1→227 bị bỏ`** (mất sub-skill 227 = Vạn Lý Truy Tâm visual) | Cao (mất CollideEvent chain + req sai + weapon check miss) | 2 giờ |
| **Tất cả 5 attack (45/47/50/54/58)** | — | `s.waitTime` KHÔNG set (default 0). So sánh `DamageSkillNew` factory (line 523-528) có set `s.waitTime = 5; s.timePerCast = 2;` | PC: tất cả 5 attack đều `WaitTime=5` (5 PC tick ~ 280ms giữa mỗi missile trong cùng cast). PC: `TimePerCast=0` cho ranged (không set ở ranged, mobile 0 ok) | **G4 waitTime=5 bị bỏ ở 5/5 attack skills** — cadence sai PC. `DamageSkillNew` factory đã set, nhưng `TangMenXxx()` factories không set. Pattern tương tự `TianWang` (cùng thiếu waitTime). | Cao (cast cadence sai toàn bộ Đường Môn) | 30 phút (sửa 5 dòng) |
| **Tất cả 5 attack (45/47/50/54/58)** | — | `s.horseLimit` KHÔNG set (default 0) | PC: 45=0, 47=1, 50=1, 54=1, 58=0 | **G4 horseLimit thiếu 3/5 attack** (47/50/54 sai — player cưỡi ngựa sẽ không dùng được skill trên mobile, dù PC cho phép) | Trung bình | 15 phút |
| **Tất cả 5 attack (45/47/50/54/58)** | — | `s.missilesGenerateData` KHÔNG set (default 0) | PC: 45=0, 47=0, **50=4 (MslsGenData=4)**, 54=0, 58=0 | **G4 missilesGenerateData thiếu 1/5** (ID 50 quan trọng nhất) | Trung bình (50 only) | 15 phút |
| **Tất cả 5 attack (45/47/50/54/58)** | — | Radius hardcode 400/450/360/400/520, registry flat `(1,X),(20,X)` | PC curves: 45 default 400, 47 `{{1,384},{20,448}}`, 50 `{{1,384},{20,448}}`, 54 `{{1,384},{20,416}}`, 58 `{{1,448},{20,512}}` | **G7 — RadiusCurves[TangMenId] flat 5/5 sai PC** (PC có curve, registry hardcode 1 mức). L1 sai 4-17% tuỳ skill. | Trung bình (radius lệch nhẹ) | 30 phút (sửa registry 5 dòng) |
| **Tất cả 5 attack** | — | `PcSkillTuningRegistry.RadiusCurves[TangMenId]` cover 5/5 active (100%) | PC có 5 active cần tuning | **OK coverage 100%** — chỉ vấn đề là curves flat, không phải coverage | — | — |
| **Tất cả 5 attack** | — | `s.ChildSkillLevel` KHÔNG set (default 0) | PC: 45/47/50/54/58 đều `ChildSkillLevel=-1` (lấy level của parent). Mobile mặc định 0 → có thể gây sai khi tính damage sub-missile | **G7 — ChildSkillLevel=-1 bị bỏ 5/5** (sub-missile damage sẽ dùng L1 thay vì L_parent — sai damage) | Trung bình (sub-damage sai) | 15 phút (set `s.childSkillLevel = -1` cho 5 skill) |
| **ID 50 mobile `radius=360`** | `BaseSkill(50, ..., 30, 20, 360, Single)` | PC: `skill_attackradius={{1,384},{20,448}}` (L1=384, L20=448) | **G4 radius 360 flat vs PC 384→448** (sai 6.3% L1, 19.6% L20) | Trung bình | (covered ở row radius curves) |
| **ID 50 mobile `MslsGenData=0`** | (default) | PC: `MslsGen=2, MslsGenData=4` | **G4 MslsGenData=4 bị bỏ** (mobile chỉ spawn 2 base, PC spawn 2 base + 2 gen + 2 gen = 6) | (covered ở row missilesGenerateData) |
| **ID 58 mobile `ColEvt` missing** | (không set) | PC: `CollideEvent=1, CollidSkillId=227` (Vạn Lý Truy Tâm tiểu phi đao — sub-skill 227 với 1 sub-missile `MslsGen=0, ChildId=36, ChildNum=1` fire khi missile 58 va chạm NPC) | **G6 — CollideEvent chain 58→227 bị bỏ** (mất visual + damage 2nd wave "Vạn Lý Truy Tâm") | (covered ở row ID 58) |

### 2.4.3. Phase 1 quick wins (5 attack skills — casting cadence + weapon check + req level)

> **Phase này ưu tiên sửa 5 attack skills (45/47/50/54/58) cùng lúc** — pattern giống nhau. Toàn bộ là edit trong `TangMenPiLiDan()` / `TangMenDuoHunBiao()` / `TangMenZhuiXinJian()` / `TangMenManThienHoaVu()` / `TangMenThienLaDiaVong()` factories.

- [ ] **Tất cả 5 attack — set `s.waitTime = 5`**: sửa 5 factory methods thêm `s.waitTime = 5;` sau `s.targetEnemy = true;` (PC `WaitTime=5` cho 5/5 attack — cadence sai toàn bộ Đường Môn). (G4, 15 phút)
- [ ] **Tất cả 5 attack — set `s.childSkillLevel = -1`**: thêm `s.childSkillLevel = -1;` để sub-missile damage lấy level của parent (PC `ChildSkillLevel=-1`). (G7, 15 phút)
- [ ] **ID 47**: sửa `BaseSkill(47, ..., 10, ...)` → `..., 30, ...` (PC ReqLevel=30). Thêm `s.horseLimit = 1;` (PC HorseLimit=1). Effort 15 phút. (G4)
- [ ] **ID 47 — EqtLimit 100 (Phi tiêu)**: thêm field `s.weaponType = 100;` hoặc tương đương vào `SkillDefinition` (cần xem `SkillDefinition.cs` field schema — hiện chưa thấy field EqtLimit). Nếu không có field, đăng ký vào `PcSkillTuningRegistry.WeaponRequirements[TangMenId][47] = 100`. (G7, 1 giờ nếu cần refactor schema)
- [ ] **ID 50**: thêm `s.missilesGenerateData = 4;` (PC MslsGenData=4). Thêm `s.horseLimit = 1;`. Effort 15 phút. (G4)
- [ ] **ID 50 — EqtLimit 101 (Phi đao)**: tương tự ID 47. (G7, 30 phút)
- [ ] **ID 54**: sửa `BaseSkill(54, ..., 50, 20, 400, SkillMissileForm.Single)` → `..., 30, 20, 400, SkillMissileForm.Fan` (PC ReqLevel=30, Form=6 Fan). Thêm `s.horseLimit = 1;` + `s.eventSkillLevel = 1;`. Effort 15 phút. (G4)
- [ ] **ID 54 — EqtLimit 102**: tương tự ID 47. (G7, 30 phút)
- [ ] **ID 58**: sửa `BaseSkill(58, ..., 50, ...)` → `..., 60, ...` (PC ReqLevel=60). Thêm `s.eventSkillLevel = 1;`. Effort 15 phút. (G4)
- [ ] **ID 58 — EqtLimit 102**: tương tự ID 47. (G7, 30 phút)
- [ ] **Tất cả radius curves** (registry `PcSkillTuningRegistry.cs` line 53-60): sửa curves flat thành PC curves:
  - 45: `[45] = new[] { (1, 400), (20, 400) }` ✓ (PC default, OK)
  - 47: `[47] = new[] { (1, 384), (20, 448) }` (PC 384→448)
  - 50: `[50] = new[] { (1, 384), (20, 448) }` (PC 384→448)
  - 54: `[54] = new[] { (1, 384), (20, 416) }` (PC 384→416)
  - 58: `[58] = new[] { (1, 448), (20, 512) }` (PC 448→512)
  Effort 15 phút. (G7)
- [ ] **ID 51 Thanh Mộc formula override**: thêm helper mới hoặc override trong `TangMenThanhMoc()` (line 1163-1166) dùng PC formula `floor(log10(level+1)/2*60)` thay vì generic `PassiveResist`. Effort 1 giờ. (G7)
- [ ] **ID 48 Tâm Nhãn — bổ sung attackspeed_v curve 30/35/38/40 + lifemax_yan_p 21→20**: sửa `AddLevels` thêm breakpoints `(33, 113, ""), (35, 149, ""), (38, 156, ""), (40, 162, "")` cho `MagicAttributeKind.AttackSpeedV`. Thêm `MagicAttributeKind.LifeMaxYanP` với `Link(lv, (1, 21, ""), (35, 20, ""), (36, 20, ""))`. Effort 1 giờ. (G4)

### 2.4.4. Phase 3 dash (G1)

- [ ] **Không áp dụng** — Đường Môn không có skill dash/melee-jump. Cả `KNpc.cpp::CastMeleeSkill` switch (line 1834), `PcSkills.txt` (`IsMelee=0` cho tất cả 43-58), và `tangmen.lua` đều không tham chiếu `Melee_Jump/JumpAndAttack/RunAndAttack` cho ID 43-58. Toàn bộ 5 active skill là ranged pure missile, không phải dash. Phase 3 bỏ qua cho Đường Môn. **Tương tự Võ Đang / Thiên Vương.**

### 2.4.5. Phase 4 event chain (G6)

- [ ] **ID 45 Tích Lịch Đơn — VanishedEvent 1→1113** (PC `skill_vanishedevent [1]={{1,1},{20,1}}, [3]={{1,1113},{20,1113}}`): thêm `s.vanishedSkillId = 1113; s.vanishedEvent = 1;` vào `TangMenPiLiDan()`. Trong `SkillEffectVisualService.ConfigureTangMenVisuals` (line 585), thêm handler `case 45:` → `SetupPcVanishSubEffect(fx, 1113)` (fire sub-skill 1113 = Tích Lịch Loạn Hoàn Hãm Tĩnh visual khi missile 45 vanish). ShowEvent 8 (animation id 8) cũng cần map. Effort 2 giờ.
- [ ] **ID 58 Thiên La Địa Võng — CollideEvent 1→227** (PC `skill_collideevent [1]={{1,0},{10,0},{10,1},{20,1}}, [3]={{1,227},{20,227}}`): thêm `s.collideSkillId = 227; s.collideEvent = 1;` vào `TangMenThienLaDiaVong()`. Trong `SkillEffectVisualService.ConfigureTangMenVisuals`, thêm handler `case 58:` → `SpawnCollideSubEffect(fx, mp)` cho sub-skill 227 (Vạn Lý Truy Tâm — `physicsenhance_p=tianluo_diwang1, missle_speed_v=tianluo_diwang1` = `physicsenhance_p={{1,40},{20,120}}` half-damage từ parent). Effort 2 giờ.
- [ ] **Verify** CollideEvent fire đúng level: PC `skill_collideevent [1]={{1,0},{10,0},{10,1},{20,1}}` — fire L11+ only. Nếu cần gate, dùng pattern tương tự 357/389 (line 146-155 trong `CombatRuntimeService`). Effort 30 phút verify.

### 2.4.6. Trạng thái

- [x] Catalog scan xong (10 skill: 4 passive/buff/resist, 6 active 5 missile + 1 passive 5-attr)
- [ ] Quick-win phase merged (Phase 1 — 14 items, priority: `waitTime=5` 5/5 + req level 47/54/58 + `MslsGenData=4` ID 50 + Fan form ID 54 + EqtLimit 4/5 + radius curves 5/5 + ID 51 formula + ID 48 curve bổ sung)
- [ ] Dash phase merged: **không áp dụng** (Đường Môn không có dash)
- [ ] Event chain phase merged (Phase 4 — 2 items, ID 45 VanishedEvent→1113 + ID 58 CollideEvent→227)
- [ ] Tuning coverage 100% (5/5 radius curves) — chỉ thiếu chất lượng curves (flat → PC curve)

### 2.4.7. Tổng kết

- **Skill chính ưu tiên sửa** (cao nhất):
  1. **ID 54 Mạn Thiên Hoa Vũ** — sai MissileForm (Single vs Fan) là mất cốt lõi "mưa hoa rải đều" + sai req level. 15 phút fix.
  2. **ID 50 Truy Tâm Tiễn** — mất `MslsGenData=4` (chỉ spawn 2 base thay vì 6). 15 phút fix.
  3. **ID 58 Thiên La Địa Võng** — mất CollideEvent→227 (Vạn Lý Truy Tâm visual) + sai req level. 30 phút fix (event chain cần `SkillEffectVisualService` integration).
- **Skill ưu tiên #2** (cadence + weapon):
  1. **5/5 attack missing waitTime=5** — toàn bộ Đường Môn fire missile quá nhanh so với PC. 15 phút fix (5 dòng).
  2. **3/5 attack missing horseLimit=1** (47/50/54) — sai cưỡi ngựa behavior. 15 phút fix.
  3. **4/5 attack missing EqtLimit** (47/50/54/58) — không enforce vũ khí (Phi tiêu / Phi đao). Effort phụ thuộc schema field có sẵn.
- **Skill passive/buff cần chỉnh formula**:
  - **ID 51 Thanh Mộc** — formula sai 2.1× ở L1 (mobile 19 vs PC 9). 1 giờ fix (override `PassiveResist`).
  - **ID 48 Tâm Nhãn** — thiếu 5 breakpoint `attackspeed_v` (PC có 30/35/38/40) + `lifemax_yan_p` L35-36. 1 giờ fix.
- **Skill ưu tiên #3** (radius tuning):
  - **5/5 radius curves flat sai PC** — registry dùng hardcode 1 mức, PC có curve `{{1,X},{20,Y}}`. 30 phút fix 5 dòng.
- **Test plan**:
  - Unit test: cast 50 ở L1 vs L20, expect 6 missiles thay vì 2.
  - Unit test: cast 54 ở L30, expect Fan form (8 fan missiles) thay vì Single.
  - Unit test: cast 58 ở L60, expect 2nd-wave missile 227 (Vạn Lý Truy Tâm) sau khi missile 58 va chạm NPC.
  - Unit test: cast 51 ở L1, expect lighting res = 9 (PC) thay vì 19 (mobile bug).
  - Visual test: cast 45 ở L20, expect vanish visual 1113 (Tích Lịch Loạn Hoàn Hãm Tĩnh) khi missile 45 biến mất.
  - Regression test: cast 47/50/54 ở trên ngựa, expect vẫn cast được (mobile hiện cấm do `horseLimit=0` default).
- **Không cần làm dash** cho Đường Môn (ranged pure missile, không có dash).
- **Phase 5 (future)**: port 80-tier sub-skills (249, 250, 302, 340, 341, 342) + 150-tier sub-skills (1069, 1070, 1071, 1097, 1098, 1099, 1100) + per-skill files `feidaotang150` / `nutang150` / `biaotang150` / `xiaoli_feidao` / `baoyu_lihua` / `shehun_yueying` / `tangmen120` / `tangmen150` (8 sub-form) vào mobile catalog. Effort: 1-2 tuần. Cần check ID trong `PcSkills.txt` đã có sẵn, chỉ cần thêm `CreateTangMenXxx()` factory.
