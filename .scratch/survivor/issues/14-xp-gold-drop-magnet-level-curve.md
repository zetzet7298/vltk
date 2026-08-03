# 14 — Decision: XP/gold drop + magnet + level curve

Type: `grilling`
Status: `ready-for-human`
Blocked by: 05

## Question

Thiết kế `SurvivorCollectItemMgr`: XP/gem drop (`OnActorDie`/`TriggerWave`/`TestRate`-parity từ
05), merge, magnet pickup, level curve (`LevelExpCalc.AddExp`-parity shape, own numbers vì
r-dhcd-006 blocked). Quyết định drop table ScriptableObject + level curve + magnet radius/speed.
Numeric = own (ghi rationale).
