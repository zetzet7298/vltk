# 34 — VFX (SkillEffectVisualService parity + hit/death/levelup)

**What to build:** Cast skill → precast + missile SPR render qua adapter (data-driven `missles1.txt`, `SkillEffectVisualService` fail-closed sẵn, read-only). Hit flash trên monster bị dmg, death effect khi die, levelup burst. VFX sprite chỉ render khi staged; chưa staged → không render, không crash.

**Blocked by:** 27 (Skill cast runtime)

**Status:** done — implement P2 core (0a649b663) + verified (SurvivorVfxService, tests xanh)

- [x] Cast → precast SPR + missile SPR hiển thị đúng vị trí/hướng (data từ missles1.txt)
- [x] Hit flash khi monster nhận dmg; death effect khi die; levelup burst quanh player
- [x] Chưa staged → không render VFX, không crash, không log lỗi
- [x] PlayMode manual: 3 skill khác nhau nhìn rõ hiệu ứng cast

## Verified

- Orchestrator: 195/195 EditMode PASSED (job d96397529afb4ec597883f7f605dceea). Fixes applied:
  - [29] SurvivorSkillChoiceTests.cs:77 CS8978 `gold?.TrySpend` method-group nullable → explicit `Func<ulong,int,bool>` cast.
  - [31] SurvivorBoss.cs `CurrentPhaseIndex` — gap giữa 2 window trả −1; fix: phase = row cuối đã MỞ (lossHp ≥ Min), gap → giữ phase trước.
  - [33] Heal test target Hp=0 — TickNow chặn Hp≤0 (coi chết); đổi target (2,8) → expect 6 (heal 4).
