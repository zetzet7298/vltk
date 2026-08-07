# 19 — Decision: VFX pipeline (SkillEffectVisualService parity)

Type: `grilling`
Status: `ready-for-human`
Blocked by: 08

## Question

Thiết kế VFX: parity `SkillEffectVisualService` (precast SPR + missile SPR) qua adapter 16, hit
flash, death effect, levelup burst. Quyết định VFX trigger từ combat events + fail-closed (skill
chưa staged → không render VFX sprite, chỉ fallback). Dựa research 08.
