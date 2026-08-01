# Evidence: 3 Runtime Bugs + Lua Parser Gap — Fixed 2026-07-17

Audit lại skill Cái Bang theo PC source sau khi verify thực tế (xem `caibang-parity-audit-2026-06-29.md`
là audit cũ — mục 117/714/357 bên đó đã lỗi thời). Mọi giá trị dưới đây đối chiếu 3 nguồn PC:
client_offline, server_offline, và slistcache mới nhất (`PcCaiBangSkills.txt`, bundled
`Assets/Resources/Reference/PcCaiBangSkills.bytes`).

## Bug #1 — Thiếu `357` trong AddSkillDamageGrants → chuỗi sát thương phụ 1073/1101 = 0% (PC 25%)

- PC `gaibang.lua::feilong_zaitian` (skill 357):
  `addskilldamage1={{{1,1073},{2,1073}},{{1,1},{20,25}}}` → target 1073, +25% tại L20
  `addskilldamage2={{{1,1101},{2,1101}},{{1,1},{20,25}}}` → target 1101, +25% tại L20
- Ma trận addskilldamage L20 (đã verify đủ nguồn):
  - 119 → 359 +40 / 125 +35 / 1074 +32
  - 122 → 357 +50 / 1073 +40 / 1101 +40
  - 125 → 359 +60 / 1074 +50
  - 128 → 357 +55 / 1073 +45 / 1101 +45
  - **357 → 1073 +25 / 1101 +25** (thiếu trước fix)
  - 359 → 1074 +25
- Trước fix: `AddSkillDamageGrants` không có 357 → caster học 357 cast 1073/1101 ra `addSkillDamagePercent=0` (PC 25).
- Fix: `CombatRuntimeService.AddSkillDamageGrants` thêm `(357, new[]{"addskilldamage1","addskilldamage2"})`.
- Verify: caster 122+128+357 @20 → cast 1073 = **110%** (40+45+25), 1101 = **110%** (EditMode test
  `Runtime_Learned357_Grants25PercentTo1073And1101` + play-mode production `SandboxManager.CombatRuntime`).

## Bug #2 — Proc rate Hỗn Thiên Khí Công (714) sai: L20=6% (PC 10%), CD sai

- PC `gaibang.lua::gaibang120.autoattackskill` slot[3] (3 nguồn đồng ý):
  `{{1,12*18*256 + 1},{20,12*18*256 + 10},{21,12*18*256 + 10}}`
  → low byte = proc%: L1=1, L15=floor(1+14/19·9)=**7**, L20=**10**, L21=10; `/256 = 12*18 = 216 ticks` CD.
- Trước fix: `AutoAttackRatePoints` = `{1,1},{15,5},{20,6},{21,6}` — fabricated, không có trong PC Lua.
- Fix: `CombatRuntimeService.AutoAttackRatePoints` = `{1,1},{20,10},{21,10}` (PC citation comment kèm).
- Verify: EditMode decode test (L1=1/L15=7/L20=10, CD=216) + play-mode runtime roll pct = **10**, nextCastTime = **216**.

## Bug #3 — Đầu Thạch Vấn Lộ (117) gây sát thương ảo (mượn data 119); PC: 0 damage, 0 mana, radius 280

- PC `skills.txt` row 117: `IsUseAR=0`, `AttackRadius=280`; `LvlData1..3 = physicsdamage_v/firedamage_v/skill_cost_v`
  nhưng **LvlData1..3 RỖNG ở MỌI nguồn PC** (client_offline, server_offline, slistcache) → engine
  `KSkills::GetSkillLevelData` fail-closed → 0 attribs, cost 0. PC SkillDesc: chỉ "thăm dò hư thực đối phương".
- Trước fix: catalog mượn `yanmen_tuobo` (119): phys 10→55, fire 10→100/150, cost 10, radius 384 → 117
  gây sát thương ảo 125+.
- Fix: `PcCombatCatalogFactory` 117 → `phys 0, fire (0,0,0), cost (0,0,0)`, radius **280**, kèm comment PC.
  `PcCaiBangLuaLevelService` không map 117 (fail-closed, đúng PC — 117 không có LvlData).
- Verify: EditMode `CaiBang_117_NoFabricatedDamage_MatchesPcEmptyLvlData` (zero-value attribs) + play-mode
  production cast: manaCost=**0**, tổng damage sau missile impact=**0**, missile 44 vẫn bay (parabola).

## Gap phụ — Lua parser drop toán tử `+`/`-` trong level table (chỉ ảnh hưởng autoattackskill)

- PC Lua: `autoattackskill` slot[1] `720*256 + 20`, slot[3] `12*18*256 + 10`.
- `PcCaiBangLuaLevelService` chỉ evaluate `*`/`/` → đọc `12*18*256` (low byte 0) → `GetSingleValue` trả
  55296 thay vì 55306. Runtime không bị ảnh hưởng (pin points slistcache), nhưng service đọc sai PC data.
- Fix: parser thêm vòng lặp `+`/`-` sau chuỗi `*`/`/` (đúng precedence Lua, left-to-right). Trong cả file
  `gaibang.lua` chỉ có autoattackskill dùng `+` → không ảnh hưởng attrib khác.
- Verify: `GetSingleValue(714,20,"autoattackskill",3)` = **55306** → 216 ticks / 10%; slot[1] = **184340**
  (720*256+20); L1 = 55297 → 1%. EditMode `CaiBang_714_LuaServiceDecodesAutoattackskillFullPcExpression`.

## Test regression

- EditMode group "CaiBang": 147 total / 146 passed / 1 failed — fail duy nhất là
  `WuDangCombatCatalogTests.CoreSectCatalog_IncludesCaiBangAndWuDangRuntimeSkills` (skill 165
  misslenum 16 vs 8) **pre-existing, ngoài phạm vi Cái Bang**, không liên quan diff.
- Các test cũ bị ảnh hưởng bởi 117=0 đã chuyển attacker sang 122 (jianren_shenshou, IsPhysical=0,
  missile 46) — 117 giờ chỉ còn dùng làm probe (0 dmg).

## Files changed

- `Assets/Scripts/Sandbox/CombatRuntimeService.cs` (357 grants, 714 rate points)
- `Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs` (117 zero damage/cost, radius 280; comment 125 form)
- `Assets/Scripts/Sandbox/PcCaiBangLuaLevelService.cs` (+/- trong parser)
- `Assets/Tests/EditMode/Sandbox/CaiBangCombatParityTests.cs`
- `Assets/Tests/EditMode/Sandbox/CaiBangFirePoolParityTests.cs`
- `Assets/Tests/EditMode/Sandbox/CaiBangAddSkillDamageChainTests.cs`
- `Assets/Tests/EditMode/Sandbox/CombatSkillSlotTests.cs`
