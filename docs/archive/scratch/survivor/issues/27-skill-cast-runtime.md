# 27 — Skill cast runtime (form 7 + fan + precast + attribution)

**What to build:** Player cast skill từ roster SkillDef: form 7 đạn thẳng + fan spread (dir = castDir + Param1×(i−half), đơn vị 1/64 vòng, offset spawn = Param2 px — KHÔNG chia 360°), melee form 12 (IsMelee=1, visual qua child missile, không cần PreCastSpr), precast SPR fail-closed (proxy màu khi chưa staged). Hit → damage đúng LevelScaling, attribution `SkillImpactSource{skillId}` → kill credit XP. Roster = danh sách SkillDef học được (debug seed 1 skill cho tới ticket 29).

**Blocked by:** 26 (SkillDef data pipeline)

**Status:** verified

- [x] Cast form 7: đạn bay hướng cast, hit monster → dmg đúng scaling, kill → XP credit qua attribution
- [x] Fan spread parity đúng công thức (1/64 vòng, offset Param2 px), không chia 360° quanh caster
- [x] Melee form 12: hit qua child missile visual; fail-closed nếu isMelee sai → không visual
- [x] Precast SPR hiển thị khi cast (staged); chưa staged → proxy màu, không crash
- [x] EditMode self-check xanh: fan math, scaling, attribution, melee fail-closed
- [x] PlayMode manual: cast 1 skill thấy đạn + dmg + XP

**Verification (orchestrator):** EditMode 142/142 PASSED. Fix: SurvivorMonster.Die out param property → local var (CS0206).
