# 06 — Research: boss multi-phase + shop/box + endless scaling

Type: `research`
Status: ``resolved``
Blocked by: 01

## Question

3 mảng giao nhau: (a) boss multi-phase + boss skill lib, (b) 3-mode skill choice (levelup/box/
shop) + reroll + per-roleId queue, (c) endless + difficulty scaling. Cần:

1. `LevelRandomSkillCtrl`: request/reroll/box/shop command shape; `PlayerRandomSkillData`
   (`m_playerEventWaitingList` Queue @+0x38, `m_beginWaitingLearnTime` @+0x40, roleId @+0x10);
   `Dictionary<ulong,PlayerRandomSkillData>` @+0x38. Parity r-dhcd-002 (role-keyed pending state,
   FIFO structure proved; semantics unresolved).
2. Entry box/shop: `BattleCmd` SelectBoxSkill / ReRandomSkill / SelectRandomSkill / SkillRun...
3. Boss: `MonsterCfg` boss flag; phase-switch surface (AI tasks `AIPlaySkillByDistanceTask`,
   `AISequeneceTask`, script hook); boss skill pool.
4. Endless: own mode (dhcd declaration chỉ có wave refresh) → surface scaling cần tự thiết kế.

## Output

Ghi `research/boss-shop-box-endless.md`. Đọc: `BattleCore.LevelRandomSkillCtrl.cs`,
`PlayerRandomSkillData.cs`, `NpcEntity.cs`, `AI*Task.cs` (đặc biệt AIPlaySkillByDistance,
AISequeneceTask, AISelectTargetTask), `BattleSys.cs`. Evidence r-dhcd-002/003 làm tham chiếu
structure, KHÔNG clone số.

## Answer

- **3-mode** = `RandomSkillParam.Type {1 levelup, 2 box, 3 shop}` + `boxParam.learnNum`; `RequestRandomSkill` enqueue-if-waiting/trigger-ngay.
- **Per-role queue** = `Dictionary<ulong,PlayerRandomSkillData>` + per-role `Queue<RandomSkillParam>` + `FP beginWaitingLearnTime` (time-predicate) — parity r-dhcd-002 (structure proven, FIFO semantics unresolved).
- **Reroll** = 2 cmd riêng: `FrameCmdRerandomSkill{EventGID}` (levelup) + `FrameCmdReSelectRandomSkill{RollCnt}` (shop, fixed price).
- **Boss phase** = damage-window keyed: `BossChangeBehaviorCmpt.OnHpChg → GetJiangHuBossPhaseConfig(lossHp) → phase table {BossDamageMin/Max, MonsterAI, Skill[], BootyID}` — KHÔNG timer/cast-count. → resolves map fog "Boss phase trigger model".
- **Endless** = chỉ wave-loop skeleton (`IsReposeWave` + WaveRefresh dynamic caps + `GetEndlessWaveCount()`); ramp curve family = own.
13 structure-parity (S1-S13) + 6 own-design (O1-O6). Full: research/boss-shop-box-endless.md
