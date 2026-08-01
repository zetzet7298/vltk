# All-Faction Skill Parity — Verification & Evidence (2026-07-18)

## Vấn đề user báo
Các skill môn phái khác (ngoài Cái Bang) cũng bị: quạt quay vòng quanh caster,
tia bắn chụm 1 chỗ, skill không hiển thị visual, thiếu skill trong catalog.

## Root cause tìm được
1. **Fan spread sai PC** — `SetupPcCircleOutwardMissiles` cũ chia full 360° quanh
   caster không xoay theo hướng cast → "quạt quay/spinning". PC KSkills.cpp
   `CastSpread` (SKILL_MF_Spread): dir_i = nDir + Param1×(i−half) (đơn vị 1/64
   vòng), spawn offset = Param2 px. → thay bằng `SetupPcFanMissiles` (SkillEffectVisualService),
   bắt buộc set `fx.missileDirections[i]` (thiếu → missile đứng yên).
2. **FAIL-CLOSED khi cast** — skill không có preCast/missile/impact sprite
   resolve được → Finished ngay, không visual. 6 skill Cái Bang đã fix trước;
   lần này 13 stub skill được gán `effectSourceId` từ PC PreCastSpr **chỉ khi
   SPR staged** (fail-closed, không tham chiếu sprite không tồn tại).
3. **Thiếu 47 learned-display skills** — có trong
   `PcAllFactionLearnedDisplaySkills.txt` (242 rows) nhưng không có builder
   faction nào đăng ký → `MissingLearnedSkillStubs` + overlay
   `ApplyAllFactionPcStaticRows` (faction theo PC LvlSetScript).

## Fix (commits fd78c44cc, 67c1f5993)
- `SkillDefinition.missileDirStep/missileFirstStep` (PC m_nValue1/m_nValue2)
- `PcFanSpreadParity` 22 rows từ PcSkills.txt Param1/Param2 (audit json)
- `SetupPcFanMissiles`: baseDir = caster→target, half = count/2,
  dir_i = baseDir + step×(i−half) đơn vị 64-dir, offset = firstStep px
- `RegisterMissingLearnedSkillStubs` gating theo include* faction flags
- `ApplyPcFanSpreadParity` sau relationship targets (1064 = CY_Rel_1064)
- 361/362/364/1075/1076 → faction TianRen (PC tianren.lua)

## Verify runtime (play mode probes, 2026-07-18)
- 47/47 stub ids resolve trong catalog; 30 cast PreCast bình thường;
  17 Finished-ngay = **đúng PC** (missile 20/408/274/1083/1084/1087/1088/718
  không có sprite trong missles.txt + preCast rỗng → PC cũng không visual)
- preCast=Y đúng 13 skill có SPR staged (em_13/tr_16/kl_16/sl_150_gunshao)
- Fan parity: 165 (1,0), 336 (2,24), 341/342 (1,0), 1057 (8,0), 128 (3,1),
  1064 (4,64), 1071 (1,0) — khớp PcSkills.txt Param1/Param2
- Fan flight xoay theo castDir: 336 cast (300,0) → dirs quanh +X;
  cast (0,300) → dirs quanh +Y ✓; 1057 tương tự ✓
- Catalog tổng 641 skills (full factions)

## Regression
- CaiBangCombatParityTests: 48/48 pass
- Full suite EditMode 4,264 tests: 15 failures — TẤT CẢ pre-existing
  (SKL-EM-PROOF-001 fixture path, weapon-thief source, perf 928ms,
  Shaolin 10 radius, TianRen 364, WuDang 165 childNum ×2, CombatStateSource
  720 ×2, BaLangEnemy damage number, WuDang precast hash stale ×1),
  không failure mới từ change này

## Ghi chú
- 17 fail-closed còn lại: chấp nhận (PC không có visual cho chúng; SPR thật
  không có trong repo staged để vendor)
- Lua level data 8 phái: audit xong → gap report riêng
  `lua-level-data-audit-8-factions-2026-07-18.md` (heuristic Estimate, port sau)
