# 10 — Decision: wave types + spawn model design

Type: `grilling`
Status: `ready-for-human`
Blocked by: 01, 05

## Question

Thiết kế wave system: normal/elite/boss/timed/swarm + spawn (time/interval/limit/batch/pool) +
`WaveRefresh.SpawnMonsterNormal`-parity flow. Dựa research 05. Quyết định: enum wave-type own,
MonsterDef pool, spawn ramp, elite/boss/timed/swarm trigger model (HP%/timer/count). Tách
structure-parity (cite declaration) vs own-values (wave timing, batch size, ramp).
