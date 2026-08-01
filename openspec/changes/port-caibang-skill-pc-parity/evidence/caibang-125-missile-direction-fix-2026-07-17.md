# 125 Bổng Đả Ác Cẩu — missile SPR frame chọn sai hướng ("tia xoay loạn xạ")

Date: 2026-07-17. Commits: `43de246a6` (direction fix), `b2445d3fe` (range boundary, cùng ngày).

## User report

Sau fix spread (commit `9adc53b`), 16 tia bổng tỏa đều quanh người nhưng **xoay loạn xạ,
không đúng phương** như ảnh PC (tia phải thẳng theo phương xuyên tâm).

## Root cause (2 lớp)

### 1. Y-flip thiếu — PC `g_GetDirIndex` là screen space y-DOWN

`SkillEffectRenderer.ComputePcDirection64` port nguyên PC `g_GetDirIndex` (KMath.h:195):

```cpp
int nSin = (nYLength << 10) / nDistance;   // +Y = DOWN (màn hình PC)
for (int i = 0; i < 32; i++) { if (nSin > g_nSin[i]) break; nRet = i; }
if ((nX2 - nX1) > 0) nRet = 63 - nRet;
```

Bảng `PcScanSin` (32 mục, giảm dần sin 270°→90°) và vòng scan giữ nguyên 100% — nhưng
Unity world **+Y = UP**. Truyền thẳng tọa độ world vào hàm PC-space làm tia chéo bị
**mirror dọc**: tia bay lên-phải (world dy>0 → PC sin>0) bị map thành hướng xuống-phải.
Cardinal (dọc/ngang) tình cờ đúng (art dọc/ngang đối xứng) → chỉ thấy sai ở tia chéo,
trông như tia quay loạn xạ.

Probe trước fix (play mode, cast 125, enemy 300,0): tia 45° → frame 0 (art DỌC);
tia 0° → frame 48 (art Ngang nhưng bucket của hướng TRÁI); tia -135° → frame 32 (DỌC).

### 2. Precision — round từng endpoint trên world float lớn

`ComputePcDirection64` round từng điểm: `Round(from.x)`, `Round(to.x)` với from/to là
world coords (hàng trăm px) và hiệu `to-from` chỉ ~1px (hướng bay đã normalize) →
`Round(a) - Round(b) ≠ Round(a-b)` → hướng bay bị hủy theo phần thập phân vị trí.
PC không gặp vì PC dùng int MPS từ đầu.

## Fix

`Assets/Scripts/UI/SkillEffectRenderer.cs` — `ComputePcDirection64`:

```csharp
// PC g_GetDirIndex (KMath.h) is defined in screen space where +Y is DOWN.
// Unity world is +Y UP, so flip Y before the int-based PC scan...
// Round the DELTA, not each endpoint: world coords are large floats...
=> ComputePcDirection64FromInts(0, 0,
    Mathf.RoundToInt(to.x - from.x), -Mathf.RoundToInt(to.y - from.y));
```

Caller `SelectPcMissileFrame` (overlay + IMGUI renderer) scale direction ×4096 trước khi
truyền để hiệu 1px qua được int rounding:

```csharp
renderer.sprite = SelectPcMissileFrame(fx, mp, mp + direction * 4096f);
```

`FromInts` giữ nguyên 100% PC (test table 64 row không đổi). `SkillEffectWorldOverlay`
delegate sang renderer nên tự được fix.

## Verify

### EditMode
- `SkillVisualDataDrivenParityTests` 12/12 — test `DirectionMapper_MatchesPcCardinalAndStrictScanBoundaries`
  cập nhật: Unity up → dir31 (PC up), up-right → dir40 (qsqrt exact-45), mirror rows mới
  (Unity (1024,-1024) down-right → 55, (-1024,1024) up-left → 23).
- `CaiBangCombatParityTests` 48/48.
- Full EditMode 4873 tests: failures = pre-existing (Backend `invalid_arg` vs `validation_error`,
  thiếu story fixture SKL-EM-PROOF-001, weapon-thief, perf benchmark 645ms, WuDang 165,
  Shaolin 10 radius, TianRen 364) — không failure mới từ direction fix.

### Play mode (production runtime)
Cast 125 @20, enemy (300,0), dump `SelectPcMissileFrame` per stick (frame = bucket·4 + anim):

```
stick0  dirDeg=0    bucket=12  (art ngang, hướng PHẢI)   ✓
stick1  dirDeg=23   bucket=11  (chéo lên-phải)           ✓
stick2  dirDeg=45   bucket=10  ✓
stick4  dirDeg=90   bucket=8   (art dọc, hướng LÊN)      ✓
stick8  dirDeg=-180 bucket=4   (art ngang, hướng TRÁI)   ✓
stick12 dirDeg=-90  bucket=0   (art dọc, hướng XUỐNG)    ✓
stick14 dirDeg=-45  bucket=14  (chéo xuống-phải)         ✓
```

Bucket tuần tự theo chiều kim đồng hồ 12→11→10→9→8→7→6→5→4→3→2→1→0→15→14→13 —
mỗi tia khớp đúng hướng bay xuyên tâm, đúng ảnh mẫu PC (16 tia starburst radial).

## Scope / ảnh hưởng

- Mọi missile dùng `ComputePcDirection64` (KangLong 128, Thiên Hạ Vô Cẩu 359, Đường Môn...)
  giờ chọn frame đúng hướng — fireball tròn trước đó sai frame nhưng không nhìn thấy.
- SPR `mag_gb_04_天下无狗.spr` 64 frame = 16 dir × 4 anim, dir-major (f16 ngang = dir4,
  f32 dọc = dir8) — khớp convention PC dir0=down.
