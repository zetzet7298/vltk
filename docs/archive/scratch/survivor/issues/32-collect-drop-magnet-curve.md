# 32 — Collect/drop/magnet/level curve (drop table SO)

**What to build:** `SurvivorCollectItemMgr` hoàn chỉnh: drop table ScriptableObject (PoolID/ItemID/OutputType/Param1/Param2/BronID shape, giá trị own), drop khi die (`OnActorDie`/`TriggerWave`/`TestRate` parity-shape) + drop bonus đợt, merge gem, magnet pickup (radius/speed own), level curve từ config (`LevelExpCalc.AddExp` parity-shape; default giữ `5+(L-1)*3`).

**Blocked by:** None — can start immediately.

**Status:** verified

- [x] Drop table config: die → roll đúng rate/output theo item type, wave trigger drop bonus
- [x] Gem merge: gem gần nhau gộp thành gem lớn (hoặc bỏ merge — ghi rationale nếu bỏ)
- [x] Magnet: gem trong radius → hút về player, speed own; pickup tăng XP
- [x] Level curve đọc từ config (không hardcode), AddExp đúng shape
- [x] EditMode self-check xanh: rate roll (seed fixed), curve, magnet math

**Verification (orchestrator):** EditMode 126/126 PASSED. Magnet epsilon fix pickup distance (float drift).
