# Research 05 — Wave system parity (LevelWave / WaveRefresh / MonsterCfg)

Status: `done` (sub-agent `research/wave-system`) — Decision 01: parity = structure/lifecycle/loop-shape, KHÔNG numeric.

Sources (read-only):
- `C:/Projects/dhcd/reconstructed-types/BattleCore/BattleCore.LevelWave.cs`
- `.../BattleCore.WaveRefresh.cs`
- `.../BattleCore.LevelMonsterMgr.cs`
- `.../BattleCore.LevelWaveConfig.cs`, `.../BattleCore.WavePoolConfig.cs`, `.../BattleCore.WaveMonsterConfig.cs`
- `.../BattleCore.MonsterCfg.cs`, `.../BattleCore.ActorWaveCmpt.cs`, `.../BattleCore.LevelBronHelp.cs`
- `.../BattleCore.WaveFuncBase.cs`, `.../BattleCore.WaveFuncCtr.cs`, `.../BattleCore.WaveFuncByTime.cs`, `.../BattleCore.WaveFuncByMonsterCount.cs`, `.../BattleCore.WaveFuncByMonsterHP.cs`
- `.../BattleCore.LevelCfgMgr.cs`, `.../BattleCore.LevelMonsterWaveParam.cs`, `.../BattleCore.BossBronData.cs`
- `C:/Projects/dhcd/il2cpp/diffable-cs/DiffableCs/BattleCore/BattleCore/WaveEventFuncType.cs` (enum sạch), `.../MonsterType.cs`, `.../WaveRefresh.cs` (field offsets)

---

## 1. Wave-type phân biệt

**Không có enum wave-type normal/elite/boss/timed/swarm ở wave level.** Phân biệt bằng 2 lớp riêng:

### 1a. Trigger type — "khi nào wave bắt đầu" (`WaveFuncCtr.CreateWaveFunc` cast `(WaveEventFuncType)cfg`)
`LevelWaveConfig.WaveType` (uint, `BattleCore.LevelWaveConfig.cs` field `public uint WaveType;`) → cast enum `WaveEventFuncType` (`BattleCore.WaveFuncCtr.cs`: `waveFuncCtr.CreteFunc((WaveEventFuncType)cfg)`).

Enum (`il2cpp/diffable-cs/.../WaveEventFuncType.cs`, đầy đủ 9 giá trị):
```csharp
WAVE_TIME_TRIGGER = 1,                 // timed: TriggerTime đạt → start
WAVE_MONSTER_COUNT_PERCENT = 2,        // kill % (TriggerParams)
WAVE_MONSTER_HP_PERCENT = 3,           // boss HP % (TriggerParams)
WAVE_MONSTER_PLAY_SKILL = 4,           // monster cast skill → start
WAVE_TYPE_KILL_ALL_MONSTER_THISID = 5, // kill-all theo MonsterID (TriggerParams)
WAVE_TYPE_KILLALL_AND_TIMEOVER_THISID = 6, // kill-all + timeover
WAVE_TYPE_OCCUPY_START = 7,  WAVE_TYPE_OCCUPY_END = 8,  WAVE_TYPE_OCCUPY_ALLEND = 9, // điểm chiếm đóng
```
Mỗi type có `WaveFuncByXxx : WaveFuncBase` override `Trigger(FP time)` (`WaveFuncByTime.cs` so `TriggerTime`, `WaveFuncByMonsterCount.cs` dùng `m_cfg.TriggerParams` + percent qua `LevelWave.GetCurWaveDieMonstePercent()` — `LevelWave.cs` method `GetCurWaveDieMonstePercent()` → `m_wave.GetCurWaveDieMonstePercent()`).

### 1b. Boss/loại monster — `MonsterCfg` (`BattleCore.MonsterCfg.cs`)
- `public byte IsBoss;` `public byte IsWorldBoss;` `public byte Type;` (= `MonsterType` enum: `MonsterNormalType=1, MonsterBossType=2, MonsterFenceType=3, MonsterDestructibleType=4, MonsterSpecialFenceType=5` — `diffable-cs/.../MonsterType.cs`).
- **Boss là flag binary trên monster config, không có tier "elite"** — elite không tồn tại trong dhcd declaration → own-design (xem Gap 4).
- WaveRefresh có luồng boss riêng: `private bool m_IsBronBoss; private BossBronData m_CurBronBossData;` (`WaveRefresh.cs` fields) — `OnUpdate` nhánh `if (m_IsBronBoss) { ... waveRefresh.BronMonster(m_data, pos, m_targer); }` = boss spawn qua `BossBronData`, ngoài monster-list thường.

### 1c. Swarm/dynamic — không có tên "swarm", có dynamic-monster cơ chế
`WavePoolConfig.DynamicMonsterTime` + `DynamicLoopNum` + `DynamicMonsterMaxNum` (`WavePoolConfig.cs` fields) → `WaveRefresh.OnUpdate`: khi số monster vượt ngưỡng + hết `DynamicMonsterTime` → **đổi `m_intelval = DynamicMonsterTime`** (interval co lại) và spawn-count tỉ lệ nghịch số monster thiếu (`m_intelval` swap + `TSMath.Min(...)` — `WaveRefresh.cs` OnUpdate IL). `Isloop` (int) → `m_isLoop` = wave tự loop sau khi hết list. `LevelMonsterMgr.GetEndlessWaveCount()` tồn tại — endless counter (logic thân ẩn).

**Kết luận: timed=type 1 + `TriggerTime`; swarm=dynamic fields + `Isloop`; boss=`MonsterCfg.IsBoss`/`m_IsBronBoss`; normal=mọi thứ còn lại; elite=KHÔNG có → own.**

---

## 2. Spawn config fields + `SpawnMonsterNormal` flow

### 2a. `LevelWaveConfig` (per-wave, `LevelWaveConfig.cs`)
`LevelID, StageID, WaveID, WaveType, WavePoolID, TriggerType(byte), EndType(byte), EndWin(byte), IsDeleteAllMonster(byte), TriggerTime(FP), TriggerParams(int[]), EndParam(int), ViewChgID(uint), IsReposeWave(byte)`.

### 2b. `WavePoolConfig` (per-pool — toàn bộ nhịp spawn, `WavePoolConfig.cs`)
`WavePoolID, Time(FP), MonsterPoolID, MonsterNum(int), SheerMonsterNum(int), MonsterBornPos, BornPosID, TeamDisX(FP), TeamDisY(FP), TeamBornPosID, Interval(FP), SingleNum(int), DynamicMonsterTime(FP), DynamicLoopNum(FP), DynamicMonsterMaxNum(int), Isloop(int), MonsterLifeTime(FP), OutPlayerByDis(FP), MoveBornPosID, OutEyeOutTimeDestroy(byte), OutEyeOutDis(FP), AtkRatio(FP), DefRatio(FP), HpRatio(FP), DropItemPoolID, DropItemCount, DropItemRatio(FP)`.

### 2c. `WaveMonsterConfig` (entry trong pool list, `WaveMonsterConfig.cs`)
`PoolID, MonsterID, DropItemPoolID, DropItemCount, DropItemRatio(FP)` — pool = danh sách monster cấu hình, spawn round-robin theo index.

### 2d. WaveRefresh runtime counters (`WaveRefresh.cs` fields)
`m_startTime, m_lifeTime(=Time), m_intelval(=Interval), m_refreshMonsterTime, m_curMonsterIndex, m_listMonster(List<WaveMonsterConfig>), m_monster(List<ActorEntity> spawned), m_monsterNumMax, m_hesmonsterNumMax (đang sống), m_oneFrameCreateNum, m_dynamicMonsterMaxNum, m_dynamicLoopNum, m_dieMonsterCount, m_isKillMonsterById, m_isShowSkillShop, m_roleSpawnMonsterCache, const OneFrameMaxCreateNum=100`.

### 2e. `SpawnMonsterNormal` flow (tổng hợp từ `WaveRefresh.cs` Init/Start/OnUpdate/SpawnMonster/SpawnMonsterNormal/BronMonster)
1. `Init(cfg, poolId)`: `LevelCfgMgr.GetWavePoolConfig` → `m_curWaveConfig`; `GetWaveMonsterList(pool)` → `m_listMonster`; copy `Time/Interval/DynamicMonsterTime/Isloop/MonsterNum/TeamDisX/Y`; `m_oneFrameCreateNum = min(oneFrame, monsterNum)`; `m_dynamicMonsterMaxNum = min(..., val2)`; `m_bornHelp.Init(BornPosID, monsterNum, ...)` (spawn-point helper, `LevelBronHelp.cs`).
2. `Start()`: `m_startRuning=true; m_startTime=now; m_refreshMonsterTime = m_intelval; m_curMonsterIndex=0`.
3. `OnUpdate()`: nếu `m_IsBronBoss` → spawn boss qua `BossBronData`; else nếu `m_monster.Count >= m_monsterNumMax` và `!m_isLoop` → `m_startRuning=false` (wave hết quota); nếu `time >= m_refreshMonsterTime` → batch spawn: `m_refreshMonsterTime += m_intelval`, spawn-count = `min(m_oneFrameCreateNum, ...)` (dynamic nhánh tính theo `(time-dynamicMonsterTime)/(monsterNumMax-count)`), loop spawn từng con qua `m_curMonsterIndex++` (round-robin `m_listMonster`).
4. `SpawnMonsterNormal(monster, pos, centerPos, targer, monsterCfg)`: build `MonsterCreateParam` — `m_monsterID, m_wavePoolID, m_wavePoolGID`; drop group/count/ratio ưu tiên từ WavePoolConfig (nếu `DropItemPoolID!=0`) else từ WaveMonsterConfig; `m_lifeTIme=MonsterLifeTime; m_outDisByPlayer=OutPlayerByDis; m_moveBornPosID=MoveBornPosID; m_hpAddRate=HpRatio; m_atkAddRate=AtkRatio; m_damageReduceRate=DefRatio; m_ownerActorID=targer.ActorID` → `ActorEntityCreateData.CreateMonsterCreateData` → `BronMonster` → `ActorEntityMgr.CreateActorEntity(data, isMonster)` → thành công thì `m_hesmonsterNumMax++` + thêm `m_monster`; thất bại (null) → skip.
5. Monster chết: `OnActorDie` (subscribe trong `Create`) → `m_dieMonsterCount++`.

### 2f. Per-monster wave runtime — `ActorWaveCmpt.cs` fields
`m_isLifeTimeLimit, m_lifeTime, m_bronTime, m_isNeedMovePos, m_checkDis, m_movePos, m_disBronPos, m_runTime, FTimer m_delayTimer` — monster có lifetime (hết → HP=0), tự di chuyển về born-pos khi xa quá `m_checkDis`, delay-move sau sinh. Đây là cơ chế "monster sống có hạn trong wave" (chống tích tụ).

---

## 3. Wave lifecycle (trigger → start → end)

### 3a. Khởi tạo (`LevelMonsterMgr.cs`)
`Init(levelID)` → `LevelCfgMgr.GetLevelWaves(levelID)` → `m_levelWaveConfigs` (list); `CreteWave(cfg)` → `m_waveFuncCtr.CreateWaveFunc(cfg)` (chọn func theo WaveType) → `LevelWave.Init(cfg, waveFunc)`; `StartSpawn(startWave=1)` → `m_startSpawn=true; m_runTime=0`. Có `InitByDiyLevelWave(levelID, List<LevelWaveConfig>)` — **config list nạp từ ngoài (diy) = hook cho own wave authoring** (02 decision).

### 3b. Trigger → Start (`LevelWave.cs`)
`LevelWave.OnUpdate(time)`: nếu `!m_start` → gọi `m_waveFunc.Trigger(time)` (subclass theo WaveEventFuncType); trigger đúng → `m_isTrigger=true` → `CreateCurWave(time)` → `CreteWave(poolId, time)` (Gid++ qua `LevelMonsterMgr.Gid`) → `WaveRefresh.Create(logic, gid)` + `Init(cfg, time)` + `Start()` → `m_monsterMgr.TriggerWaveEvent(time, waveType, waveID)` (visual event). State fields LevelWave: `m_isTrigger, m_start, m_end, m_curWaveConfig, m_waveFunc, m_wave`.

### 3c. Run
`LevelWave.OnUpdate` tiếp: `m_wave.OnUpdate()` (nhịp spawn §2e); `m_wave.Finish()` = `TimeOver()` (`now - m_startTime >= m_lifeTime`) hoặc `CheckTriggerTimeOverEvent()` (trigger 6).

### 3d. End
`Finish()==true` → `m_wave.Stop()` + `m_wave.TimeOverCheckDestoryMonster()` (xóa monster nếu `IsDeleteAllMonster`) → `m_wave=null`. `LevelMonsterMgr.FreeWave(wave)` → `RerfreshLevelPlayWaveCount()`: `m_runWaveCnt++`, gửi visual event, wave cuối → `Battle.BattleFinsh(BattleEndReason)` = level win. `EndType/EndWin/EndParam` (LevelWaveConfig) = điều kiện thắng wave. Destroy: `LevelMonsterMgr.DestroyAll()` → `m_isFnish=true`, `FreeFrontWaves()`.

### 3e. Timeline tổng
`LevelMonsterMgr.StartSpawn → LevelWave (wait trigger) → WaveFuncByX.Trigger → LevelWave.CreateCurWave → WaveRefresh.Start → [spawn batch theo Interval/SingleNum/dynamic] → TimeOver/Finish → Stop + TimeOverCheckDestoryMonster → FreeWave → runWaveCnt++ → BattleFinsh`.

---

## 4. GAP LIST — structure-parity vs own-design

### Structure-parity (bắt buộc giữ shape, cite declaration)
1. **Config schema 3 lớp**: LevelWaveConfig → WavePoolConfig → WaveMonsterConfig (field lists §2a-c) — `BattleCore.LevelWaveConfig.cs`, `BattleCore.WavePoolConfig.cs`, `BattleCore.WaveMonsterConfig.cs`. (Schema reference — value tự author per 02.)
2. **Trigger enum 9 type** `WaveEventFuncType` — `diffable-cs/.../WaveEventFuncType.cs` + `WaveFuncCtr.CreateWaveFunc` dispatch.
3. **Lifecycle state machine** LevelWave (`m_isTrigger/m_start/m_end`, `CreateCurWave`) — `BattleCore.LevelWave.cs`.
4. **WaveRefresh spawn nhịp**: `Start()` set `m_startTime`+`m_refreshMonsterTime=m_intelval`; `OnUpdate` batch spawn khi `time>=m_refreshMonsterTime`, `m_refreshMonsterTime+=m_intelval`; quota `m_monsterNumMax` stop; round-robin `m_curMonsterIndex` qua `m_listMonster`; `OneFrameMaxCreateNum=100` const — `BattleCore.WaveRefresh.cs`.
5. **MonsterCreateParam mapping** từ WavePoolConfig (lifeTime/outDis/moveBornPos/HpRatio/AtkRatio/DefRatio/drop) — `WaveRefresh.SpawnMonsterNormal` IL.
6. **Boss path riêng**: `MonsterCfg.IsBoss/IsWorldBoss/Type(MonsterType)` + `WaveRefresh.m_IsBronBoss/m_CurBronBossData` — `BattleCore.MonsterCfg.cs`, `BattleCore.WaveRefresh.cs`.
7. **Dynamic/swarm cơ chế**: `DynamicMonsterTime/DynamicLoopNum/DynamicMonsterMaxNum` + interval swap + count-tỉ-lệ spawn — `BattleCore.WavePoolConfig.cs` fields, `WaveRefresh.OnUpdate` IL.
8. **Wave-end cleanup + win**: `IsDeleteAllMonster` + `TimeOverCheckDestoryMonster` + `EndType/EndWin/EndParam` + `RerfreshLevelPlayWaveCount→BattleFinsh` — `LevelWaveConfig.cs`, `WaveRefresh.cs`, `LevelMonsterMgr.cs`.
9. **Monster lifetime/return**: `ActorWaveCmpt` (m_isLifeTimeLimit/m_lifeTime/m_isNeedMovePos/m_checkDis/m_movePos) — `BattleCore.ActorWaveCmpt.cs`.
10. **DIY wave list hook**: `InitByDiyLevelWave(levelID, List<LevelWaveConfig>)` — own authoring nạp trực tiếp, không cần binary cfg — `LevelMonsterMgr.cs`.

### Own-design (numeric blocked / không tồn tại trong dhcd declaration)
1. **Elite tier**: KHÔNG có trong dhcd — chỉ `IsBoss` binary (+IsWorldBoss). Elite cần own: hoặc flag mới trên own MonsterCfg (thêm byte), hoặc model như pool riêng với HpRatio/AtkRatio cao. → **decision cần chốt khi to-spec** (candidate: thêm `IsElite` mirror `IsBoss`).
2. **Endless ramp curve**: `GetEndlessWaveCount()` tồn tại nhưng body ẩn; map.md đã ghi graduate sau research này — curve family (linear/expo/stair) = own (ticket sau).
3. **Mọi numeric**: Time/Interval/MonsterNum/SingleNum/DynamicMonsterTime/DynamicLoopNum/DynamicMonsterMaxNum/MonsterLifeTime/OutPlayerByDis + AtkRatio/DefRatio/HpRatio + TriggerTime/TriggerParams/EndParam + drop (r-dhcd-006) = own tuning.
4. **`m_oneFrameCreateNum` / spawn-count formula** (`min(oneFrame, dynamic-proportional)`) — shape giữ, hệ số own.
5. **Wave→wave chaining rule** (wave kế tiếp trigger từ đâu — config-driven qua TriggerType/TriggerTime, không có "next index" field; `m_curStartIndex` + `GetLastWaveConfig` gợi ý sequence theo list order) — confirm bằng own wave table.
6. **Boss phase trigger model**: `WaveFuncByMonsterHP` (type 3) = HP% trigger **start wave** — phase-switch trong boss fight nằm AI task (ngoài scope declaration) — map.md Not-yet-specified giữ nguyên.

### Mapped recommendation (Survivor wave types → dhcd shape)
- normal wave = `WaveEventFuncType.WAVE_TIME_TRIGGER` + pool Interval/Time/MonsterNum.
- timed wave = `TriggerTime` (delay/đếm ngược) — cùng type 1, khác TriggerTime.
- swarm = dynamic fields (`DynamicMonsterTime` nhỏ + `DynamicLoopNum` cao + `Isloop`) — "swarm" là pool-config khác, KHÔNG phải enum mới.
- boss wave = monster trong pool có `IsBoss=1` (hoặc `m_IsBronBoss` path nếu cần boss spawn riêng tại điểm cố định) + trigger type 3 (HP%) cho phase.
- elite = own flag (`IsElite` hoặc pool ratio) — cần decision ticket khi to-spec.
