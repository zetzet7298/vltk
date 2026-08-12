# 28 — Impact/buff system (4-bucket + DOT + stun + stacking)

**What to build:** Generic impact model (KHÔNG enum status tên riêng): `SurvivorActorAttr` + impact mgr **4 bucket** (Absolute/Relative/Multiply/Effect), control qua `BuffStateID` 20-state (stun/no-move/no-skill/sleep/confusion/invisible...), DOT generic `BuffDot` (poison/burn/heal variant, loop-timer tick, `TickWhenAdd`, `RemoveAfterDot`, `sourceType=SourceBuffer`), stun qua state event `Enter/Finish_Stun`, stack/replace/refresh (`FindAttr(stack)` + `ReplaceAdd` shape), attribution `SkillImpactSource{skillId,buffId}` + caster → `SumSkillDamage` kill credit.

**Blocked by:** None — can start immediately.

**Status:** verified

- [x] Stat buff 4 bucket tính đúng khi apply + remove
- [x] Control state chặn hành động đúng (stun chặn move/skill, no-move chỉ chặn move)
- [x] DOT loop tick đúng interval, dmg mang sourceType=SourceBuffer, heal variant hoạt động
- [x] Stack/replace/refresh theo stack level đúng quy tắc
- [x] Attribution → SumSkillDamage kill credit XP về đúng skill/buff + caster
- [x] EditMode self-check xanh: bucket math, DOT tick, stun lifecycle, stacking

**Verification (orchestrator):** EditMode 126/126 PASSED (2026-08-03). Fixes: CollectSettings.Pull thêm epsilon pickup (float drift 5−4.6=0.4000001>0.4); SurvivorI18nTests MakeBundled thêm en entry rỗng + đúng key survivor.only.vi; SurvivorSaveService.TryParse catch JsonUtility ArgumentException (JSON syntax hỏng THROW, không trả null).
