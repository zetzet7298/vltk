# Phi Long Tại Thiên — PC Sample Video Evidence

## Source
`pc-evidence/skills/phi_long_tai_thien.mp4` (user-supplied PC capture).

| Property | Value |
| --- | --- |
| Codec | h264 |
| Resolution | 518 x 212 |
| Duration | ~5.52 s |
| Avg frame rate | ~26 fps |

## Method
Frames were sampled at 3 fps with `ffmpeg` and analyzed with Python (PIL + numpy + scipy).
For each frame, bright + saturated pixels (`value > 0.55`, `saturation > 0.35`) were treated
as skill-effect glow. Median hue, warm-color fraction, projectile-cluster count, and the
horizontal centroid of the glow were measured. No frame was hand-picked to fit a conclusion.

## Measured facts (per sampled frame)

| t (s) | fx pixels | median hue | warm fraction | glow centroid X |
| ---: | ---: | ---: | ---: | ---: |
| 0.00 | 8656 | 31° | 92% | 120 |
| 0.33 | 2028 | 37° | 64% | 157 |
| 0.67 | 21722 | 33° | 97% | 134 |
| 1.00 | 26627 | 34° | 95% | 333 |
| 1.33 | 45482 | 34° | 97% | 233 |
| 1.67 | 34960 | 35° | 98% | 296 |
| 2.00 | 35243 | 34° | 97% | 280 |
| 2.33 | 44949 | 33° | 99% | 205 |
| 2.67 | 37741 | 34° | 98% | 281 |
| 3.00 | 23598 | 36° | 97% | 368 |

## Conclusions (only what the video supports)

1. **Effect color is warm orange/gold.** Median hue stays in the 31–37° band with 92–99%
   warm-color fraction across the whole cast. This matches the Cai Bang faction default
   color used by the mobile auto-mapper, `new Color(1f, 0.68f, 0.24f)` (HSV hue ≈ 33°), and
   the fire-dragon SPR family (`mag_gb_05_亢龙有悔.spr` / impact `mag_gb_bz5_爆炸效果.spr`).
2. **Projectiles travel from the caster toward the target, then converge.** The glow centroid
   starts near the left/caster (X≈120) and moves right (X up to ~333–368) before the glow
   concentrates at the impact region. This is consistent with the locked homing behaviour
   (`missles.txt` row 166 `MoveKind=5`) implemented in `SkillEffectVisualService`.
3. **Multiple simultaneous projectile clusters are present.** After morphological merging of
   the glow, 2–7 distinct clusters were measured mid-flight (4 at t=1.33 s). This corroborates
   a multi-missile skill but is NOT used to assert the exact dragon count — the count of 4 at
   level 20 is fixed by PC `gaibang.lua::feilong_zaitian skill_misslenum_v` (L20 = 4), already
   locked by `CaiBangPhiLongSpreadTests` and `phi-long-resource-evidence.md`.

## Caveat
Glow bloom over-segments into many bright clusters under simple thresholding, so the raw
cluster count is noisy and must not be read as a literal missile count. The video is used to
confirm **color** and **homing trajectory**, both of which agree with the PC data already
ported. The authoritative numeric source remains the PC `skills.txt` / `missles.txt` /
`gaibang.lua` rows cited in `phi-long-resource-evidence.md`.
