
## Verified

- Orchestrator: 233/233 EditMode PASSED (job 9b594d4461cd4e3fac461874cb675951). Fixes applied:
  - [41] WaveManager.RampCopy: DynamicMonsterMaxNum 0 + CapAdd → cap Alive=1 chặn spawn; fix: cap chỉ áp khi base>0.
  - [41] RunKillChain test sim: snapshot kill tĩnh → con spawn trễ (interval) sống vĩnh viễn → kẹt wave; fix: drain tick + kill ngay + guard WaveIndex (chống cascade).
  - [41] Boss test: wave 12 chaos (×1.5) làm expected sai → ChaosAtWave=999 cô lập boss scale.
  - [37] Director thêm WaveIndex + Kills (wire WaveIndexSource + gameover stats).
