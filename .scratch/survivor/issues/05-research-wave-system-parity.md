# 05 — Research: wave system parity (LevelWave / WaveRefresh / MonsterCfg)

Type: `research`
Status: ``resolved``
Blocked by: 01

## Question

Cấu trúc wave dhcd → all wave types (normal/elite/boss/timed/swarm). Cần:

1. Wave-type enum/flag từ declaration (LevelWave, WaveRefresh, LevelMonsterMgr) — normal/elite/
   boss/timed/swarm phân biệt bằng gì (flag? trigger? MonsterCfg.boss?).
2. Spawn config fields: spawn time, interval, limit/batch, pool, deaths counter,
   `SpawnMonsterNormal` flow.
3. Wave lifecycle: trigger/start/end state transitions.
4. Gap list: phần nào own-design (vì numeric blocked) vs structure-parity.

## Output

Ghi `research/wave-system.md`. Đọc: `BattleCore.LevelWave.cs`, `BattleCore.WaveRefresh.cs`,
`BattleCore.LevelMonsterMgr.cs`, `BattleCore.MonsterCfg.cs`, `BattleCore.ActorWaveCmpt.cs`.
Schema-parity cite declaration path; numeric = own.

## Answer

Wave-type KHÔNG phải enum normal/elite/boss/... — phân biệt 2 lớp:
- **Trigger** = enum `WaveEventFuncType` 9 giá trị (1=time, 2=kill%, 3=HP%, 4=skill-cast, 5/6=kill-all, 7-9=occupy) → "khi nào wave start".
- **Boss** = flag `MonsterCfg.IsBoss/IsWorldBoss/Type(MonsterType)` + path riêng `WaveRefresh.m_IsBronBoss/BossBronData`.
- **Swarm** = dynamic fields `DynamicMonsterTime/DynamicLoopNum/DynamicMonsterMaxNum` + interval swap; `Isloop`.
- **Elite KHÔNG tồn tại** trong dhcd → own-design (flag `IsElite` hoặc pool ratio).
- Lifecycle: `LevelMonsterMgr.StartSpawn → WaveFuncByX.Trigger → LevelWave.CreateCurWave → WaveRefresh.Start → [batch spawn theo Interval/SingleNum/dynamic] → TimeOver/Finish → BattleFinsh`.
- DIY hook: `InitByDiyLevelWave(levelID, List<LevelWaveConfig>)` — author wave table trực tiếp, không cần binary cfg.
10 structure-parity + 6 own-design gap list. Full: research/wave-system.md
