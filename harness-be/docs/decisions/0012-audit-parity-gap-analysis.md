# 0012 — Audit parity đa subagent: BE port vs PC server (gap analysis)

- **Status:** accepted
- **Date:** 2026-06-12
- **Scope:** toàn bộ 11 domain US-001..US-011

## Bối cảnh

Sau khi 11 story báo `implemented` + 228 test pass, chạy review đa subagent
(5 con, read-only) so behavior thực của BE FastAPI với PC game server (Lua
scripts + binary `jx_linux_y`). Mục tiêu: phát hiện logic/behavior THIẾU hoặc
SAI so với PC, áp dụng reverse engineering khi logic nằm trong engine.

Nguyên tắc: "implemented/pass là tuyên bố, không phải bằng chứng" — mỗi claim
phải trace tới code/data PC thật.

## Phát hiện cốt lõi

**BE hiện port đúng lớp PERSISTENCE/CRUD + catalog data, nhưng phần lớn
BEHAVIOR động của game chưa port.** Đây không phải lỗi che giấu — các story
được scope ở mức "khung dữ liệu + CRUD", còn game logic sống động phần nhiều
nằm trong engine C++ (jx_linux_y) và hệ script lớn (missions/activitysys).

Tỷ lệ phủ behavior ước lượng theo domain:

| Domain | Phủ behavior | Gap nổi bật |
|---|---:|---|
| account | ~70% | OTP, IP/session limit, logout flow |
| role | ~50% | init attrib theo phái, RoleData blob, name filter |
| player | ~20% | exp curve, auto level-up, free-point, translife rules SAI |
| item | ~25% | use/equip/enchase/crafting/durability/random-prop |
| shop | ~20% | sell-price 50% SAI, catalog semantics SAI |
| skill | ~15-20% | cast/cooldown/effect(36 attrib)/prereq/skill-point |
| map | ~40% | NewWorldScript, map_type restrict, entry gate |
| task | ~5% | mission engine 942 lua, reward, quest-chain |
| combat | ~25% | damage 3692-3900, DAMAGE_TYPE, crit; battle balance |
| social | ~15% | TongUnion, TongZhaoMu, Tong economy, tong war |
| activity | ~5% | rule-based event engine 494+427 lua |

## Tổng gap

~80 gap qua 11 domain. Phân loại severity (gộp): **HIGH ~31, MED ~28,
LOW ~16**. 12 nhóm gap lớn đã ghi vào Harness backlog (#1-#12).

## Lỗi SAI (không chỉ thiếu) cần sửa sớm

1. **shop sell = buy×0.5**: PC không có hệ số 50%; giá bán qua engine
   `GetItemPrice(instance)`. Base đang lấy nhầm giá MUA. (backlog #4)
2. **buysell.txt semantics**: cell = chỉ số DÒNG trong goods.txt, BE hiểu là
   template ID. Catalog trả về sẽ sai. (backlog #4)
3. **translife reset level**: PC GIỮ cấp + cộng remain-prop; BE RESET về 150.
   Mốc cấp phải 160/170/180/200 tăng dần, trần 5 lần (BE=4). (backlog #2)
4. **skill faction gate**: `faction_skill_unlocked` chặn học theo char_level
   90/120/150; PC chỉ dùng để NHẮC nhiệm vụ, không chặn học. (backlog #5)
5. **item stack**: BE gộp stack cho mọi item kể cả trang bị; PC chỉ stack khi
   `IsItemStackable==1`. (backlog #3)

## Khả thi reverse engineering

`jx_linux_y` là ELF 32-bit, NOT stripped, có DWARF + tên C++ demangle được,
source path lộ `swordonline/gameworld/core/src/`. Reverse RẤT khả thi qua
`objdump -d -l` (line-mapping) thay vì decompile mù. Symbol đã xác định cho:

- Damage: `KNpc::CalcDamage`, `Calc{Physics,Cold,Fire,Light,Poison}AttribDamage`,
  `CalcCurRes` (đã port 1/3; còn 3692-3900 + DAMAGE_TYPE + crit/lucky/pierce)
- Skill: `KSkill::CanCastSkill@0x08101010`, `Cast@0x08105b70`,
  `GetSkillCost@0x08107e70` (offset 0x98), `GetNextCastTime@0x0819dc50`
- Social: `KTongLogic::Union_*`, `KTongZhaoMuServer`, `KServerCore::*ZhaoMu*`
- Item: `GetItemPrice`, `EnchaseItem`, `FoundryItem` (cần locate symbol)

## Quyết định / khuyến nghị

1. **KHÔNG flip story status** — 11 story vẫn `implemented` ở mức scope ban đầu
   (khung CRUD + data). Audit này định nghĩa lại "parity 100% behavior" là mục
   tiêu giai đoạn 2, tách khỏi "khung đã dựng".
2. **Ưu tiên sửa 5 lỗi SAI** trước (rủi ro tạo data/giá sai), rẻ hơn port mới.
3. **Port-ngay-từ-Lua** (ROI cao, không cần binary): skillstate.lua (36 attrib),
   skilllvlup.lua (prereq+point), translife rules, map_type enforce.
4. **Reverse binary** cho: damage còn lại, skill cast/cost/cooldown, social
   Union/ZhaoMu, item GetItemPrice/Enchase.
5. **Scope lại** task-mission và activity engine — port chọn lọc theo event/
   quest ưu tiên thay vì tái dựng toàn bộ engine 942+494+427 lua.

## Hệ quả

- Backlog #1-#12 là bản đồ công việc giai đoạn 2.
- Phương pháp reverse (DWARF line-mapping) đã chứng minh trên damage — tái dùng.
- Cần làm rõ với chủ dự án ranh giới authoritative: BE có phải nguồn chân lý cho
  mission/combat real-time không, hay một game-logic layer khác đảm nhận.
