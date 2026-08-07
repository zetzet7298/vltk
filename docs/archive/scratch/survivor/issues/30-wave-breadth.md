# 30 — Wave breadth (9 trigger + elite + swarm + DIY wave table)

**What to build:** Wave config table tự author (DIY hook `InitByDiyLevelWave`), trigger theo `WaveEventFuncType` 9 giá trị (1=time, 2=kill%, 3=HP%, 4=skill-cast, 5/6=kill-all, 7-9=occupy), boss-flag wave, swarm dynamic fields (`DynamicMonsterTime/LoopNum/MaxNum`, `Isloop`), elite own-design (`IsElite` flag + ratio), ramp interval/count own. Lifecycle parity: StartSpawn → WaveFuncByX.Trigger → CreateCurWave → WaveRefresh.Start → batch spawn (Interval/SingleNum) → TimeOver/Finish → BattleFinsh.

**Blocked by:** None — can start immediately.

**Status:** verified

- [x] Wave table từ config (SO/text): trigger 9 loại đánh giá đúng điều kiện start
- [x] Batch spawn theo Interval/SingleNum + dynamic caps swarm
- [x] Boss-type wave spawn quái flag boss; elite ratio own từ config
- [x] Ramp interval giảm / count tăng theo wave index
- [x] EditMode self-check xanh: trigger eval 9 loại + batch math
- [x] PlayMode manual: ≥3 wave type khác nhau chạy đúng
