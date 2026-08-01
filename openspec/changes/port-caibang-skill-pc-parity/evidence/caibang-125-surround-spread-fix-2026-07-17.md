# Cái Bang 125 "Bổng Đả Ác Cẩu" — fix missile spread (2026-07-17)

## Hiện tượng
Cast 125 trong game: 16 tia bổng **chụm vào 1 chỗ** thay vì tỏa đều từ tâm player.

## Root cause (2 chỗ)

### 1. Visual — `SkillEffectVisualService.SetupSurroundMissiles`
`PlaySkillCast` (đường cast người chơi thấy) dùng `SetupSurroundMissiles(fx, 16)`
với **radius cố định 1.5** → 16 tia bổng chỉ bay 1.5 đơn vị quanh player rồi
chạm đích → trông như chụm 1 chỗ tại player.

### 2. Runtime — `CombatRuntimeService.SpawnProjectiles`
Vòng spawn 16 child-missile truyền **cùng 1 `targetPoint`** (vị trí địch) cho mọi
missile → 16 projectile chồng lên nhau bay 1 hướng (nguồn cho authoritative
presentation + collision khi có backend).

## PC truth — `KSkills.cpp` `CastCircle` (SKILL_MF_Circle)
- `Value1 == 0` → circle tâm **caster** (row 125: Param1=0) ✓
- `nFirstStep = m_nValue2` = spawn offset; row 125 Param2=0 → spawn ngay tại caster
- `nDirPerNum = MaxMissleDir / ChildSkillNum` = 360°/16 = **22.5°**
- hướng missile i = castDir + 22.5°·i → phủ đều 360°
- missile bay full lifetime range

Row 125 (PcCaiBangSkills.txt): MisslesForm=3 (Circle), ChildSkillId=47,
ChildSkillNum=16, Param1=0, Param2=0. Lua `bangda_egou`:
`skill_attackradius L20=512`, `missle_speed_v L20=32`.

## Fix
1. `SetupSurroundMissiles`: thay radius 1.5 bằng **PC circle spread** — missile 0
   theo hướng caster→target, các missile khác `Rotate(baseDir, i*360/count)`,
   spawn tại caster, bay `distance = max(speed*duration, targetDist)`.
   (Cùng pattern đã dùng cho `SetupPcKangLongSpread`/`SetupPcCircleOutwardMissiles`.)
2. `SpawnProjectiles`: khi `form == Surround && count > 1 && attackRadius > 0`,
   missile 0 giữ nguyên `targetPoint` (va chạm địch tại đích như PC), missile i>0
   có target riêng `caster + dir(baseAngle + 360°/count·i) * attackRadius`
   (vòng tròn bán kính attackRadius — Lua L20 = 512).

## Verify
- Test mới `CaiBang_125_SurroundMissilesSpreadAroundCasterPcCastCircle`: missile 0
  target = enemy.position; missile 1–15 nằm trên vòng bán kính 512 quanh caster,
  cách đều 22.5°, missile 1 lệch +22.5° so với castDir. **Pass.**
- Group "CaiBang" EditMode: **131/131 pass** (130 cũ + 1 mới, 0 regression).
- Play-mode verify (`SandboxManager.Instance.SkillEffectVisual.PlaySkillCast(125, (0,0)→(300,0), L20)`):
  ```
  missileCount=16
  0: t=(512,0) mag=512   ← hướng cast
  1: t=(473,196) mag=512 ← +22.5°
  ... 16 tia phủ đều 360°, tất cả mag=512
  angle0=0.0 angle1=22.5
  ```
  → 16 tia bổng tỏa đều quanh player, missile 0 về địch, đúng `CastCircle` PC.
- Full EditMode suite (4872 tests): không failure mới liên quan spread/125/Cái Bang;
  các failure còn lại đều pre-existing ngoài scope (Backend cần server,
  WuDang 165 data gap, missing story fixtures, perf benchmark...).
