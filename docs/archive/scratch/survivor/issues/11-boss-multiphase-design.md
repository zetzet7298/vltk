# 11 — Decision: boss + multi-phase + boss skill library

Type: `grilling`
Status: `ready-for-human`
Blocked by: 06, 04

## Question

Thiết kế boss: flag boss trong MonsterDef, multi-phase (phase-switch trigger: HP%/timer/skill-
count), boss skill pool (subset từ skill library 04), boss spawn từ wave boss-type. Quyết định
phase state machine + boss skill queue. Structure-parity từ 06 (MonsterCfg.boss, AI tasks).
