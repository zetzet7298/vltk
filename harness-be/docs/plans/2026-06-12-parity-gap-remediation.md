# Kế hoạch xử lý Parity Gap — VLTK Backend (giai đoạn 2)

> **For Hermes:** Dùng skill `subagent-driven-development` để thực thi plan theo từng task.
> Mỗi task port phải tuân `jx-pc-port-rule`: source-of-truth = PC Lua + binary
> `jx_linux_y`, KHÔNG đoán. Mỗi task có TDD + commit riêng.

**Goal:** Đưa BE từ "khung CRUD + catalog" lên "parity behavior" với PC server,
xử lý ~80 gap (backlog #1-#12) theo thứ tự rủi ro/ROI.

**Giả định nền (đã chốt theo memory dự án):** BE là **game server độc lập,
authoritative**, port 100% logic. Không có game-logic layer khác gánh phần
mission/combat → mission engine + combat đầy đủ PHẢI port vào BE. Nếu chủ dự án
thay đổi giả định này, Phase 4 (mission/activity engine) cần scope lại.

**Architecture:** Giữ kiến trúc module hiện tại (domain/application/infrastructure
/api per module, UoW, tt-fw cores). Thêm các domain-service + bảng mới khi cần.
Reverse engine bằng `objdump -d -l` line-mapping (DWARF còn nguyên) thay decompile.

**Tech Stack:** FastAPI + tt-fw cores + PostgreSQL + pytest. RE: objdump/nm/gdb/radare2.

---

## Nguyên tắc điều phối multi-subagent (đọc trước khi thực thi)

Rút từ kinh nghiệm 11 story đã port + audit vừa rồi:

1. **Subagent CHỈ build trong phạm vi 1 module + unit test của nó.** Main agent
   tích hợp wiring (UoW/dependencies/conftest/pyproject) TUẦN TỰ để tránh
   conflict. Subagent có thể timeout — main agent verify output + làm bù.
2. **Fan-out chỉ cho task ĐỘC LẬP** (khác module, không share file). Task cùng
   chạm `unit_of_work.py`/`dependencies.py`/`conftest.py` → main agent làm tuần tự.
3. **Mỗi subagent nhận:** đường dẫn PC source cụ thể (file:line hoặc symbol@addr
   từ audit), đường dẫn BE module, format báo cáo, lệnh venv. Yêu cầu trả về
   handle verify được (test pass count, file path).
4. **Ngôn ngữ:** context phải ghi "trả lời tiếng Việt".
5. **Verify gate:** sau mỗi task — ruff sạch + pytest module pass + (nếu sửa
   wiring) full suite pass. Chưa pass thì không commit.
6. **Trace harness** sau mỗi nhóm: `harness-cli trace` + `backlog close`.

Concurrency tối đa 8 subagent/lần; thực tế batch 3-5 để dễ verify.

---

## PHASE 0 — Sửa 5 lỗi SAI (ưu tiên cao nhất, rẻ, chặn data hỏng)

Đây là logic ĐÃ port nhưng SAI, đang tạo data/giá sai. Sửa trước khi port mới.
Các task này chạm nhiều module khác nhau → **fan-out 5 subagent độc lập được**,
nhưng task 0.1/0.2 cùng chạm shop nên gộp 1 subagent.

### Task 0.1 — Sửa shop sell-price + buysell.txt semantics (backlog #4)

**Module:** `app/modules/shop/` (+ đọc lại parser `item/domain/goods.py`)

**PC source:** `second_hand_store/itemdef.lua:23` (GetItemPrice), `buysell.txt`
(cell = chỉ số DÒNG goods.txt), `global_tiejiang.lua:93-98`/`hangrong.lua:56`
(AddShop2Stores(goodsIdx, name, currencyType, pct, cb)).

**Việc:**
- Sửa `shop/domain/buysell.py`: parse cell là 1-based row-index vào goods.txt,
  KHÔNG phải template ID. Catalog = các goodsIdx từ lệnh AddShop2Stores.
- Sửa `shop/domain/pricing.py`: BỎ hệ số hardcode 0.5. Sell-price cần dựa trên
  item value thật. Tạm thời (chờ RE GetItemPrice ở Phase 2): tách hàm
  `sell_price()` ra interface riêng, đánh dấu `# TODO Phase2: reverse GetItemPrice`,
  dùng giá trị từ goods.txt cột đúng thay vì buy×0.5. Ghi rõ đây là placeholder.
- Load đủ cột tiền tệ trong `item/domain/goods.py` (bạc/đồng/tích phân/vinh dự/
  phúc duyên), không chỉ cột 银两.

**Verify:** `pytest tests/unit/modules/shop tests/integration/modules/shop -v`
+ test mới khẳng định catalog map đúng goods-row-index. Commit.

### Task 0.2 — Sửa translife rules (backlog #2)

**Module:** `app/modules/player/`

**PC source:** `task_head.lua:52` TB_LEVEL_LIMIT={160,170,180,200,200},
`task_func.lua:107-127` check_zhuansheng_level, `:110` transcount>=5,
`task_head.lua:13-48` TB_LEVEL_REMAIN_PROP, `:53` TB_TRANSTIME_LIMIT.

**Việc:**
- `player/domain/constants.py`: TRANSLIFE level-limit thành mảng theo transcount
  [160,170,180,200,200], MAX_TRANS_LIFE=5.
- `player/domain/progression.py`: `can_translife()` dùng mốc theo transcount;
  `translife()` KHÔNG reset level về 150, GIỮ cấp + apply remain-prop từ bảng;
  thêm check khoảng cách thời gian TB_TRANSTIME_LIMIT (nếu BE lưu last-translife-time).
- Sửa `translife.py` parser range z=1..5 (không 1..7) cho khớp dữ liệu lv200.

**Verify:** `pytest tests/unit/modules/player tests/integration/modules/player -v`.
Commit.

### Task 0.3 — Sửa item stack chỉ cho stackable (backlog #3, phần stack)

**Module:** `app/modules/item/`

**PC source:** `itemdef.lua:26-34` IsItemStackable.

**Việc:** thêm cờ `stackable` vào item template (parse từ item_detail.txt nếu có,
hoặc theo genre); `item/application/service.py:55-62` `find_stack` chỉ gộp khi
template stackable. Trang bị mỗi cái là instance riêng.

**Verify:** `pytest tests/unit/modules/item tests/integration/modules/item -v`. Commit.

### Task 0.4 — Sửa skill faction-gate semantics (backlog #5, phần gate)

**Module:** `app/modules/skill/`

**PC source:** `server_playerlevelup.lua:8-86` — FACTION_SKILLTAB chỉ dùng
`levelup_check150skillmission()` để NHẮC nhiệm vụ, KHÔNG chặn học.

**Việc:** bỏ `faction_skill_unlocked()` chặn học theo char_level 90/120/150.
Gate học thực = ReqLevel (skills.txt) + BaseSkill prerequisite (xem Phase 1 Task
1.2). Giữ FACTION_SKILLTAB như metadata nhắc nhiệm vụ.

**Verify:** `pytest tests/unit/modules/skill tests/integration/modules/skill -v`. Commit.

**Điều phối Phase 0:** fan-out 3 subagent độc lập [0.1+đọc goods, 0.2, 0.3];
task 0.4 main agent làm (nhỏ, chạm logic gate). Verify từng cái. ~30 phút.

---

## PHASE 1 — Port-ngay-từ-Lua (ROI cao, KHÔNG cần binary)

Logic nằm thuần trong Lua, port trực tiếp. Fan-out tốt vì khác module.

### Task 1.1 — Player level-up + exp curve + base attrib theo phái (backlog #1)

**Module:** `app/modules/player/`

**PC source:** `server_playerlevelup.lua` (level-up hook, mốc skill 90/120/150,
~5 prop/level), `task_head.lua:79-82` TB_BASE_STRG/DEX/VIT/ENG theo 5 series.
**LƯU Ý:** đường cong exp chính có thể engine-internal → nếu Lua không đủ, đánh
dấu phần exp-curve chuyển Phase 2 (reverse), port phần còn lại (free-point, base attrib).

**Việc:** `add_exp()` → auto level-up khi đủ exp; cộng free_point mỗi cấp;
`create_state()` gán base attrib theo faction/series; trigger mốc skill.

**Verify:** unit + integration player. Commit.

### Task 1.2 — Skill level-up prerequisite + skill-point + 36 magic attribute (backlog #5)

**Module:** `app/modules/skill/` (+ có thể thêm field magic_point ở role/player)

**PC source:** `skilllvlup.lua:9-192` (prerequisite HaveMagic>=5, cap 20,
AddMagicPoint(-1)), `skill1/special/skillstate.lua:6-153` (36 attribute, công
thức theo cấp: addphysicsdamage_p=lvl*20, lifemax_p=lvl*5, allres_p=lvl*2.5...).

**Việc:** `level_up_skill()` check prerequisite + trừ skill-point + cap đặc thù 20;
thêm module/parser `skillstate` map StateSpecialId → công thức attribute theo level
(port bảng skillstate.lua). Việc ÁP attribute lên stat phối hợp Phase 2 combat.

**Verify:** unit + integration skill (test công thức attribute theo cấp). Commit.

### Task 1.3 — Map: enforce map_type restrictions + entry gate + forbidmap (backlog #7)

**Module:** `app/modules/map/`

**PC source:** `map_type.txt` FORBIT_ITEM_TYPE, `forbidmap.lua:5-115`
(__SJMAPS/__BWMAPS/__ZQMAPS/__TONGMAPS ranges), `newworldscript_h.lua:16-77`
(SetPKFlag/ForbidChangePK/SetCreateTeam/DisabledUseTownP), entry gate level/
faction/transcript (`server_playerlevelup.lua:56,75`).

**Việc:** parse map_type.txt; `enter_map()` enforce entry gate + trả về cờ
restriction (cấm PK/tổ đội/hồi thành/dịch chuyển) theo map_type; port forbidmap
classification ranges.

**Verify:** unit + integration map. Commit.

### Task 1.4 — Account: logout flow + IP/session limit + OTP (backlog #11)

**Module:** `app/modules/account/`

**PC source:** `logout.lua:15-35` (logout flow + logout_date), `limitaccount_ip.lua:40-67`
(LimitAccountPerIP), `login.lua` (bIsUseOTP check).

**Việc:** thêm endpoint `POST /v1/account/logout` (ghi logout_date, dọn phiên);
IP/session limit (nếu mobile cần); OTP check khi is_use_otp=True.

**Verify:** unit + integration account. Commit.

### Task 1.5 — Role: init attrib + name filter (backlog #12, phần Lua)

**Module:** `app/modules/role/`

**PC source:** `task_head.lua:79-82` TB_BASE theo series; name filter (charset/từ cấm).

**Việc:** `create_role()` init RoleData base attrib theo faction; thêm name
validation (charset, độ dài, từ cấm). Phần RoleData blob serialization → Phase 2.

**Verify:** unit + integration role. Commit.

**Điều phối Phase 1:** fan-out 5 subagent [1.1, 1.2, 1.3, 1.4, 1.5] — khác
module, độc lập. Main agent wiring nếu task thêm field cross-module (1.1/1.2 có
thể thêm field ở player/role → main agent merge migration tuần tự). Verify full
suite sau khi gộp. ~1-2h.

---

## PHASE 2 — Reverse binary (engine-internal, DWARF line-mapping)

`jx_linux_y` không stripped + DWARF + tên C++. Reverse qua `objdump -d -l`.
Các task này CẦN đọc binary — giao subagent có toolset terminal, mỗi con 1 cụm symbol.

### Task 2.1 — Combat damage: phần còn lại 3692-3900 + DAMAGE_TYPE + crit/lucky (backlog #8)

**PC source/binary:** `KNpc::CalcDamage@0x809e790` KNpc.cpp:3692-3900
(state-skill-effect loop 3692-3712, đệ quy elemental 3765), DAMAGE_TYPE switch
→ `Calc{Physics,Cold,Fire,Light,Poison}AttribDamage@0x80925f0-0x8092c10`,
lucky/crit (`GetLucky/LuckyRandom/CurrentPiercePercent`).

**Việc:** reverse tiếp 5 hàm Calc*AttribDamage + dispatch DAMAGE_TYPE; port
state-skill-effect loop + lucky/crit/pierce vào `combat/domain/damage.py`. Mỗi
hàm có provenance comment (KNpc.cpp:line). Mở rộng test parity damage.

**Verify:** unit combat (test từng nhánh elemental + crit). Commit.

### Task 2.2 — Skill cast/cost/cooldown (backlog #5, phần engine)

**PC source/binary:** `KSkill::CanCastSkill@0x08101010` KSkills.cpp:300-421,
`Cast@0x08105b70`, `GetSkillCost@0x08107e70` (vtable offset 0x98),
`GetSkillCostType` (0x9c), `GetDelayPerCast` (0xa4), `GetNextCastTime@0x0819dc50`.

**Việc:** reverse cost/cooldown offset (đơn giản, đọc field struct) → port cast
validation (mana/cost, cooldown, target/range/weapon) vào skill domain. Thêm
endpoint cast hoặc service method `can_cast`/`apply_cost`.

**Verify:** unit skill cast. Commit.

### Task 2.3 — Item GetItemPrice + EnchaseItem + FoundryItem (backlog #3/#4, phần engine)

**PC source/binary:** locate symbol `GetItemPrice`, `EnchaseItem`, `FoundryItem`
trong jx_linux_y (nm | grep -i). Reverse công thức sell-price (thay placeholder
Phase 0.1), khảm ngọc, chế tạo/cường hóa.

**Việc:** port công thức sell-price thật vào `shop/domain/pricing.py` (gỡ
placeholder); port enchase/foundry vào item domain (+ bảng/cột cần thiết).

**Verify:** unit + integration item/shop. Commit.

### Task 2.4 — Social TongUnion + TongZhaoMu + Tong economy (backlog #9)

**PC source/binary:** `KTongLogic::Union_Add/AddTong/Join_Apply/Join_Refuse`,
`KTongZhaoMuServer`, `KServerCore::AddTongZhaoMuInfo/ProcessApplyJoinTongFromZhaoMu`,
`tong.lua` (BuildFund/WarFund/Maintain_R/TongLevelUp/Maintain_Stunt),
`tong_setting.lua` (MAX_ELDER=7/MAX_MANAGER=56). DB: TongUnion, TongZhaoMu.

**Việc:** dựng model+service TongUnion (apply/refuse/join), TongZhaoMu (đăng
tuyển/nộp đơn/duyệt), Tong economy (quỹ + level-up + bảo trì tuần + stunt);
enforce rank quota. Đây là task lớn nhất Phase 2 — có thể tách 2 subagent
(Union+ZhaoMu | economy+quota).

**Verify:** unit + integration social. Commit.

### Task 2.5 — Role RoleData blob serialization (backlog #12, phần engine)

**PC source/binary:** reverse cấu trúc RoleData serialize (C++ engine) để
create_role tạo blob engine load được. **Đánh giá khả thi trước** — nếu blob
quá phức tạp và mobile client không dùng engine gốc, có thể GIỮ field-level model
(quyết định hiện tại) và đóng gap này là "won't-fix có chủ đích".

**Verify:** nếu port — round-trip test serialize/deserialize. Commit hoặc ghi
quyết định won't-fix.

**Điều phối Phase 2:** fan-out tối đa — [2.1, 2.2, 2.3] độc lập module; [2.4]
tách 2 con; [2.5] làm cuối (đánh giá trước). Main agent wiring migration tuần tự.
Mỗi subagent toolset=[terminal,file,search], context kèm symbol@addr + cảnh báo
read-only binary. ~2-4h.

---

## PHASE 3 — Item use-effect / equip (backlog #3, phần gameplay)

### Task 3.1 — Equip/unequip + equip slots

**PC source:** `equip_system.lua`, item equip slot taxonomy. Thêm equip slot
model + luồng mặc/cởi + check class/level.

### Task 3.2 — Use-item dispatcher

**PC source:** `itemscript.lua:26-44` (tbCondition/tbAction), 629 lua item. Port
theo lô loại item (hồi HP/MP, mở rương, buff, lệnh bài). Engine UseItem → reverse
phần engine, dispatcher → port Lua.

### Task 3.3 — Durability + bind + expire + random-prop

Thêm các cột + logic vào item model. random-prop (`itemhead.lua` RndItemProp)
có thể cần reverse seed/series/luck.

**Điều phối Phase 3:** tuần tự trong module item (cùng file model/service) →
KHÔNG fan-out song song trong cùng module; 1 subagent làm tuần tự hoặc main agent.

---

## PHASE 4 — Mission engine + Activity engine (lớn nhất, scope chọn lọc)

**Quyết định scope:** KHÔNG tái dựng toàn bộ 942+494+427 lua. Port theo
event/quest ưu tiên kinh doanh. Cần chủ dự án xác nhận danh sách quest/event
launch trước khi làm.

### Task 4.1 — Mission framework tối thiểu (backlog #6)

Dựng module `mission` riêng: state machine quest-chain (port `tasklink_head.lua`),
lifecycle trigger (StartMission/EndMission/Timer/OnDeath), reward grant (tích hợp
inventory/role), điều kiện hoàn thành server-authoritative. Port 3-5 quest chain
mẫu để validate framework.

### Task 4.2 — Activity engine tối thiểu (backlog #10)

Dựng engine rule-based (tbConfig/tbCondition/tbAction message dispatch). Port
1-2 event mẫu (giao nộp + ranking + reward). Mở rộng dần.

**Điều phối Phase 4:** đây là thiết kế lớn → viết design doc riêng trước khi
code; 1-2 subagent orchestrator cho framework, fan-out cho từng quest/event sau.

---

## Thứ tự thực thi tổng thể

1. **Phase 0** (sửa SAI) — làm NGAY, rẻ, chặn data hỏng. ~30 phút.
2. **Phase 1** (port Lua) — ROI cao, fan-out 5 con. ~1-2h.
3. **Phase 2** (reverse binary) — core gameplay. ~2-4h.
4. **Phase 3** (item gameplay) — tuần tự. ~1-2h.
5. **Phase 4** (mission/activity) — cần scope từ chủ dự án; design-first.

Sau mỗi phase: full suite pass + ruff clean + `harness-cli trace` + `backlog close`
các item đã xong + cập nhật story coverage.

## Câu hỏi cần chốt trước Phase 4

- Danh sách quest-chain + event ưu tiên cho bản launch (để scope Phase 4)?
- RoleData blob (Task 2.5): port serialize hay giữ field-level model + won't-fix?
