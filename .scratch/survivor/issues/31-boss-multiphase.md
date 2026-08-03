# 31 — Boss multi-phase (damage-window + AI + skill pool + booty)

**What to build:** Boss spawn qua wave boss-type, phase-switch **damage-window keyed** (KHÔNG timer): `BossChangeBehaviorCmpt.OnHpChg → GetJiangHuBossPhaseConfig(lossHp)` → phase table `{BossDamageMin/Max, MonsterAI, Skill[], BootyID}`. Boss skill pool = subset skill library (npc/boss pool từ 26), cast qua runtime 27. Chết → booty lớn (nhiều gem + hòm). Own: số phase, HP window, thưởng.

**Blocked by:** 27 (Skill cast runtime), 30 (Wave breadth)

**Status:** ready-for-agent

- [x] Boss spawn qua wave boss-type, HP/atk từ MonsterDef riêng
- [x] Phase switch đúng HP window (lossHp) → đổi AI + skill pool, KHÔNG theo timer
- [x] Phase cuối → chết → drop booty (nhiều gem + hòm)
- [x] EditMode self-check xanh: phase table lookup boundary (min/max window)
- [x] PlayMode manual: boss 3 phase nhìn thấy rõ đổi hành vi

## Verified

- Orchestrator: 195/195 EditMode PASSED (job d96397529afb4ec597883f7f605dceea). Fixes applied:
  - [29] SurvivorSkillChoiceTests.cs:77 CS8978 `gold?.TrySpend` method-group nullable → explicit `Func<ulong,int,bool>` cast.
  - [31] SurvivorBoss.cs `CurrentPhaseIndex` — gap giữa 2 window trả −1; fix: phase = row cuối đã MỞ (lossHp ≥ Min), gap → giữ phase trước.
  - [33] Heal test target Hp=0 — TickNow chặn Hp≤0 (coi chết); đổi target (2,8) → expect 6 (heal 4).
