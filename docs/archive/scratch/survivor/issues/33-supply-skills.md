# 33 — Supply skills (heal/bomb/magnet/full-clear + cooldown)

**What to build:** Supply skill subset dùng được từ HUD: heal (lifereplenish_v/lifemax_v qua impact 28), bomb (dmg vùng từ SkillDef `special/bomb.lua`), magnet (kích hoạt hút toàn màn qua collect 32), full-clear (dmg tất cả quái hiện tại). Slot UI + cooldown riêng từng supply; fail-closed khi chưa staged.

**Blocked by:** 27 (Skill cast runtime), 28 (Impact/buff system), 32 (Collect/drop/magnet/level curve)

**Status:** done — implement P2 core (0a649b663) + verified (SupplyDefs/SupplyBar, tests xanh)

- [x] Heal: hồi đúng lượng qua impact; bomb: dmg vùng đúng; magnet: hút toàn màn; full-clear: dmg toàn quái
- [x] Slot UI + cooldown riêng từng supply, dùng lại được sau cooldown
- [x] Fail-closed: supply chưa staged visual → vẫn dùng được, proxy/không VFX, không crash
- [x] EditMode self-check xanh: cooldown FSM + effect mapping 4 loại
- [x] PlayMode manual: bấm supply thấy effect đúng

## Verified

- Orchestrator: 195/195 EditMode PASSED (job d96397529afb4ec597883f7f605dceea). Fixes applied:
  - [29] SurvivorSkillChoiceTests.cs:77 CS8978 `gold?.TrySpend` method-group nullable → explicit `Func<ulong,int,bool>` cast.
  - [31] SurvivorBoss.cs `CurrentPhaseIndex` — gap giữa 2 window trả −1; fix: phase = row cuối đã MỞ (lossHp ≥ Min), gap → giữ phase trước.
  - [33] Heal test target Hp=0 — TickNow chặn Hp≤0 (coi chết); đổi target (2,8) → expect 6 (heal 4).
