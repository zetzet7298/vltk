# 29 — Skill choice 3-mode + queue + reroll (levelup/box/shop)

**What to build:** `SurvivorRandomSkillCtrl` parity: 3 mode `RandomSkillParam.Type {1 levelup, 2 box, 3 shop}` + `boxParam.learnNum` (box chọn nhiều card), per-role queue (`Dictionary<ulong,PlayerRandomSkillData>` + `Queue<RandomSkillParam>` + `beginWaitingLearnTime`, enqueue-if-waiting/trigger-ngay, FIFO), reroll 2 cmd riêng (`FrameCmdRerandomSkill` levelup + `FrameCmdReSelectRandomSkill{RollCnt}` shop giá cố định trừ vàng), card pool composition theo weight own, pick → skill vào roster (từ 27), `SurvivorPause` card scope (ref-count, timescale ∈ {0,1}).

**Blocked by:** 26 (SkillDef data pipeline), 27 (Skill cast runtime)

**Status:** ready-for-agent

- [x] Levelup trigger (XP đủ) → 3 card từ pool weight, pause timescale 0, pick → skill vào roster + resume
- [x] Queue: request khi panel đang mở → enqueue; dequeue FIFO sau khi đóng
- [x] Box mode: learnNum > 1 card chọn nhiều lần; shop mode: giá cố định + reroll riêng trừ vàng
- [x] Reroll levelup: đổi 3 card, số lần giới hạn own
- [x] Card hiển thị tên/desc/icon JX (fail-closed: proxy icon khi chưa staged)
- [x] EditMode self-check xanh: queue FSM (enqueue/wait/dequeue), pool weight, 3 mode
- [x] PlayMode manual: levelup pick card → skill cast được trong arena

## Verified

- Orchestrator: 195/195 EditMode PASSED (job d96397529afb4ec597883f7f605dceea). Fixes applied:
  - [29] SurvivorSkillChoiceTests.cs:77 CS8978 `gold?.TrySpend` method-group nullable → explicit `Func<ulong,int,bool>` cast.
  - [31] SurvivorBoss.cs `CurrentPhaseIndex` — gap giữa 2 window trả −1; fix: phase = row cuối đã MỞ (lossHp ≥ Min), gap → giữ phase trước.
  - [33] Heal test target Hp=0 — TickNow chặn Hp≤0 (coi chết); đổi target (2,8) → expect 6 (heal 4).
