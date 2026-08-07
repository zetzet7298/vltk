# 06-research — Boss multi-phase / 3-mode skill choice / endless scaling

Status: `research` (sub-agent `research/boss-shop-box-endless`). Sources read-only.
Confidence: declaration-level (diffable-cs) + partial IL recovery (reconstructed-types) + r-dhcd-002/003 native evidence. KHÔNG có numeric parity (dhcd data blocked).

## 1. LevelRandomSkillCtrl — command shape + per-role queue

### Event types & command payloads (diffable-cs, field offsets)

`RandomSkillParam` = { `RandomSkillEventType Type` (+0x10), `RandomSkillBoxParam boxParam` (+0x18) }.
`RandomSkillBoxParam` = { `int learnNum` (+0x10) }.
`RandomSkillEventType`: `RandomSkillNormal = 1`, `RandomSkillBox = 2`, `RandomSkillShop = 3`.
→ Path: `C:/Projects/dhcd/il2cpp/diffable-cs/DiffableCs/BattleCore/BattleCore/RandomSkillParam.cs` (cùng thư mục: RandomSkillBoxParam.cs, RandomSkillEventType.cs).

FrameCmd (client→server wire shape, BProtoBaseStruct):
- `FrameCmdSelectRandomSkill` = { `uint EventGID` (+0x10), `int SkillID` (+0x14) } — levelup: chọn 1 skill.
- `FrameCmdSelectBoxSkill` = { `uint BoxGID` (+0x10), `int LearnCount` (+0x14) } — box: chọn nhiều (learnCount).
- `FrameCmdRerandomSkill` = { `uint EventGID` (+0x10) } — reroll 1 event (levelup modal).
- `FrameCmdReSelectRandomSkill` = { `int RollCnt` (+0x10) } — shop reroll, roll count.
Cùng thư mục: FrameCmdSelectRandomSkill.cs, FrameCmdSelectBoxSkill.cs, FrameCmdRerandomSkill.cs, FrameCmdReSelectRandomSkill.cs.

Event params (server→client, RoleId + GID + list):
- `LevelEventParam` = { `ulong RoleId` (+0x10), `uint GID` (+0x18), `List SkillList` (+0x20), `int skillCount` (+0x28), `uint canLearnNum` (+0x2C), `uint stageId` (+0x30) } — levelup modal.
- `LevelBoxEventParam` = { `RoleId`, `GID`, `List WillLearnSkillList` } — box modal (pre-rolled skill list).
- `LevelSkillShopEventParam` = { `RoleId`, `GID`, `List SkillList`, `int skillCount` } — shop modal.
- `LevelSkillShopParam` = { `int SkillId` (+0x10), `bool IsBuy` (+0x14), `int Price` (+0x18) } — mỗi entry shop có giá riêng.
- `LevelBoxSkillData` = { `int SkillID`, `int SkillLv` } — box sẽ học.
Cùng thư mục: LevelEventParam.cs, LevelBoxEventParam.cs, LevelSkillShopEventParam.cs, LevelSkillShopParam.cs, LevelBoxSkillData.cs.

### Per-role pending state (khớp r-dhcd-002 @+0x38/@+0x40, giờ có tên method)

`PlayerRandomSkillData` (declaration, cùng thư mục PlayerRandomSkillData.cs):
- `ulong roleId` (+0x10), `List<RandomSkillConfig> PlayerSkillLibrary` (public), `List<LevelEventParam> m_playerSkillCache`, `List<LevelBoxEventParam> m_playerBoxSkillCache`, `List<LevelSkillShopEventParam> m_playerSkillShopCache`, `Queue<RandomSkillParam> m_playerEventWaitingList` (+0x38), `FP m_beginWaitingLearnTime` (+0x40), `StringBuilder m_sb`.
- `IsPlayerWaitingLearn()`: reconstruction = `now >= m_beginWaitingLearnTime` (time-based predicate — waiting hết hạn theo thời gian, không phải cờ). `SetPlayerWaiting(FP time)`, `SetPlayerNotWaiting()` (= −inf).
- Ctor: `m_beginWaitingLearnTime = -<something>` → mặc định "không waiting".
- Records: `RecordLvUpSkill/RecordBoxSkill/RecordSkillShop` → 3 cache list riêng; `GetLastLvUpSkillParam/GetLastBoxSkillParam` → resend path.

`LevelRandomSkillCtrl` (LevelRandomSkillCtrl.cs): fields `m_idCnt`, `m_owner (BattleLevelLogic)`, `List<RandomSkillConfig> m_baseLibrary`, `m_baseSupplyLibrary`, `List<ulong> m_playerIdCache`, `Dictionary<ulong,PlayerRandomSkillData> m_playerRandomSkillData` (+0x38 khớp r-dhcd-002), `m_tempCfgs`, `m_sendLogicEventFunc`.

Flow (proven native order r-dhcd-002 + caller IL now visible):
- `RequestRandomSkill(roleId, param)`: resolve data → `IsPlayerWaitingLearn()` true ⇒ `EnqueueWaitingEvent(param)` + `CheckPlayerResend(data)`; false ⇒ `DoTriggerEvent(data, param)`.
- `CheckWaitingList(roleId)`: waiting ⇒ `CheckPlayerResend`; else `DequeueWaitingEvent()` → nếu non-null ⇒ `DoTriggerEvent(data, param)`.
- `DoTriggerEvent`: switch `param.Type`; **shop branch gọi `GetRandomSkillShopParam(roleId, count)` + `RecordSkillShop`** rồi send event qua `m_sendLogicEventFunc` (native: `m_sendLogicEventFunc` = `Action<EntityVisualEvent,object,bool,bool>`; emit-before-return proven r-dhcd-002). Levelup/box tương tự (GetLvUpRandomSkillParam / GetBoxSkillParam) + `m_beginWaitingLearnTime` được set tại đây → waiting window bắt đầu khi event hiện lên.
- `ResendLastSkillEvent(data)`: re-send `GetLastLvUpSkillParam()` hoặc `GetLastBoxSkillParam()` (last-param cache, không phải queue) — nền tảng cho reconnect/UI-miss.
- Selection boundary: `CheckSkillEvent(roleId,gid,skillId,remove)` / `CheckBoxSkillEvent(roleId,gid,remove,ref skillIdList)` / `CheckSkillShopEvent(roleId,gid,skillId,ref skillList)` → thin bridge vào PlayerRandomSkillData.
- `CheckReMoveRandomSkillShopData(roleId, forceRemove)` — dọn shop (hết giờ/leave), tạo data entry mới nếu chưa có; `CheckReMoveRandomSkillEventCompeleted(roleId)`.

Selection→pump (NormalLevelLogic, reconstructed-types/BattleCore/BattleCore.NormalLevelLogic.cs):
- `TriggerRandomSkillEvent(roleID)` → `new RandomSkillParam { Type = RandomSkillNormal }` → `m_randomSkillCtrl.RequestRandomSkill(roleID, param)` (line ~1651).
- `TriggerBoxRandomSkillEvent(roleID, learnNum)` → `Type = RandomSkillBox` + `boxParam.learnNum = learnNum` (line ~1691).
- `SelectClientRandomSkill(actor, gid, skillId)` → `CheckSkillEvent(roleId, gid, skillId, remove:false)` → true ⇒ `CheckWaitingList(roleId)` (line ~1749) — **selection thành công = pump queue ngay**.
- `SelectClientBoxRandomSkill(actor, gid, skillCount)` (~1788), `ClientReRandomSkill(actor, gid)` (~1993).
- Base virtuals: `BattleLevelLogic.cs` line ~222/232/234: `TriggerBoxRandomSkillEvent`, `TriggerRandomSkillEvent`, `TriggerRandomSkillShopEvent` (override chỉ ở mode có shop — xem §2).

### Weight / pool surface (own tuning zone)

`RandomSkillConfig` = { `RandomSkiilPoolID` (+0x10), `ID`, `int RandomSkillLibraryId`, `int LevelUpRandomWeight`, `int FirstLevelRandomSkillWeight`, `byte CanRepeatSelect`, `byte IsDependHandbook` } — **2 trọng số riêng: lần đầu vs level-up**.
`RandomSkillLibraryConfig` = { `ID`, `int Level`, `IsMaxLevel`, `FuncType`, `ClasifyType` (None/ATK/Def/Supply — enum RandomSkillClasifyType), `IsPetUse`, `DependSkills[]`, `SkillID`, `RewardID`, `BuffID`, `IsSuperWeapon`, `EffectID` } — mỗi level của 1 library; super-weapon = evolution surface.
`SkillBoxRandomWidgetConfig` = { `ItemID`, `SkillNum`, `Weight` } — widget box.
`RandomSkillData` (player runtime, RandomSkillCmpt): `m_skillIdsUnlock`, `m_banSkillIds`, `Dictionary m_learnSkills`, `m_learnSkillsList`, `m_deathLearnSkillsList`, `m_removeSkillsList`, `m_randomSkillDamgeCaches`; `ProcessLibrarySkill(libSkillId, level)` — apply card; `GetMaxLearnSkillCount(type)` ở NormalLevelLogic.
Nguồn: RandomSkillConfig.cs, RandomSkillLibraryConfig.cs, RandomSkillClasifyType.cs, SkillBoxRandomWidgetConfig.cs, RandomSkillCmpt.cs (cùng thư mục diffable-cs/BattleCore/BattleCore/).
`LevelRandomSkillCtrl.Init(poolID, supplyPool, sendLogicEventFunc)`; `HandleEndlessSkillPool` (reconstructed-types LevelRandomSkillCtrl.cs) — endless dùng pool riêng.

## 2. Entry box/shop BattleCmd

Client UI (GameLogic/A5Game/BattleLearnSkillCtrl.cs) mirror server queues: `Queue m_waitingLvSkillParam` (+0x10), `m_lvSkillCmdCaches`, `m_reLvSkillCmdCaches`, `Queue m_waitingBoxSkillParam` (+0x28), `m_boxSkillCmdCaches`, `m_curLvSkillParam` (+0x38), `m_curSkillShopParam` (+0x40), `m_curBoxSkillGid`, `m_isShowingLvSkillUI`/`m_isShowingBoxSkillUI`, `m_delayShowLvSkillUIState`, `GameTimer m_delayShowLvSkillTimer`, `float m_delayShowTime`. Handlers: `OnLearnRandomSkillEvent(LevelEventParam)` / `OnLearnBoxSkillEvent` / `OnRandomSkillShopEvent`; senders: `OnSendRandomSkillCmd(gid, skillId)`, `OnSendBoxSkillCmd(gid)`, `OnSendReRandomSkillCmd(gid)`; `OnMiJiResultUIClose` → `CheckWaitingList` (gọi lại server). → r-dhcd-002 "unresolved semantics" giờ có caller-level shape.

BattleCmd layer (BattleCore/BattleCore/):
- `BattleCmdSelectRandomSkill : BattleCmdRunTemplate<BattleCmdSelectRandomSkill, FrameCmdSelectRandomSkill>` — OnRun: null-check context/actor (`this+0x10` cmd data, `+0x20` gid), indirect call @+0x3C8.
- `BattleCmdSelectBoxSkill : ...<FrameCmdSelectBoxSkill>` — indirect call @+0x3D8.
- `BattleCmdReRandomSkill : ...<FrameCmdRerandomSkill>` — indirect call @+0x3E8.
- `BattleCmdReSelectRandomSkill : ...<FrameCmdReSelectRandomSkill>`.
- `BattleCmdRunTemplate<T, FrameCmd>`: bind FrameCmd → OnRun(entity); `BattleCmdMgr` (m_listCmd + m_moveCmd riêng, `AddCmdData`, `RunFrameCmd`, `EndFrame`); `BattleCmdRunFactory.CreateRunData(BattleFrameCmd)`.
- `BattleCmdSkillRun` + `FrameCmdSkill { uint SkillID }` — skill cast command (boss dùng cast qua command path).

Shop entry: `BattleLevelLogic.TriggerRandomSkillShopEvent()` virtual; override `BattleXianDaoLevelLogic` (line 50) — stage-scoped shop (mode XianDao). `XianDaoShopConfig` = { `LevelID`, `StageID`, `RandomSkiilPoolID`, `FP BuyPriceWeight`, `uint RefreshPrice` } — giá mua = weight, reroll giá cố định. `WaveRefresh.m_isShowSkillShop` (+0xC1) — wave config bật shop trong wave. `SendSkillShopEvent` → `SendVisualEvent((EntityVisualEvent)eventParam, eventParam, canMerged, syncSend)`.
Nguồn: BattleCmd*.cs, BattleCmdRun.cs, BattleCmdRunFactory.cs, BattleCmdMgr.cs, BattleCmdSkillRun.cs, FrameCmdSkill.cs, BattleXianDaoLevelLogic.cs, XianDaoShopConfig.cs, WaveRefresh.cs (cùng thư mục diffable-cs/BattleCore/BattleCore/).

Pause (r-dhcd-003, giờ có field): `BattleSys.m_twiceSpeed` (+0xD8), `m_isPause` (+0xD9), `int m_pauseCount` (+0xDC), `m_isQuickSelecPanel` (+0xE2) — signed counter, `set_IsPause` tăng/giảm counter, `ReCalcTimeScale` chọn timeScale {0,1,1.5,2}. Card UI OnVisible/OnHidden gọi setter (native proven). Vẫn KHÔNG có bằng chứng input lock/timer suspension → fail-closed như r-dhcd-002/003.

## 3. Boss flag + phase-switch surface

`MonsterCfg` (MonsterCfg.cs): `IsBoss` (+0x21), `IsWorldBoss` (+0x28), `MonsterKind` (+0x29), `IsRoyal` (+0x2A), `uint[] Skills` (+0x50), `AttachSkill` (+0x58), `AITaskID` (+0x5C), `DefaultAttr/AddAttr`, `BootyId`, `skillScore`. Monster + AI config cùng 1 manager: `MonsterCfgMgr` có `m_dictMonsters`, `m_dictAIConfigs` (AITaskConfig), hatred configs.

AI task tree (AITaskConfig.cs, AITaskType.cs): `AITaskConfig` = { `uint ID`, `uint Type`, `uint[] SubAIList`, `FP[] Param` } — declarative, composable. `AIBaseTask` = `List m_taskList` (+0x10), `AITaskConfig m_cfg` (+0x18), `ActorEntity m_owner` (+0x20), `AITaskStatus TaskStatus` (+0x28); factory `CreateAIProcesser(cfg, actor)`.

Type enum (AITaskType.cs): MoveToPos=1, Attack=2, Wait=3, MoveFollow=4, AttackSelectPos=5, AttackLockDir=6, PetAttack=7, PetFollow=8, MoveByDir=9, MoveReoundDir=10, PlayerAttackTask=11, **SelectTarget=12**, ShadowPlayer=13, MoveFixDirDistance=14, CheckBattleIsEnd=15, PlayerNoTargetAttack=16, ChargeToPlayerAI=17, **BasedPlayerDistance=18**, **PlaySkillByDistance=19**, MoveByAreaPath=20, MenKeFollow=21, ReturnBirthPos=23, **SequeneceType=101**, **RandomSelectType=102**.

Boss-relevant tasks (fields, diffable-cs):
- `AIPlaySkillByDistanceTask` = { `uint m_skillIdx` (+0x2C), `FP m_minDistance` (+0x30), `FP m_maxDistance` (+0x38), `bool m_keepCurrent` (+0x40), `bool m_curPlayNoTargetNeedKeep` (+0x41), `ActorEntitySideFilter m_enemySideFilter` (+0x44) }; methods `CheckCanPlaySkill()`, `PlaySkill()` — **skill theo dải khoảng cách** (boss dùng kỹ năng tầm xa/gần theo khoảng player).
- `AIAttackTask` = { `uint m_skillIdx` (+0x2C) } — attack cơ bản, `GetSkillByIdx(idx)` → `NpcEntity.SkillList`.
- `AISequeneceTask` = { `int m_index` (+0x2C), `bool m_isLoop` (+0x30) } — chạy SubAIList tuần tự, có loop.
- `AIRandomSelectTask` — chọn ngẫu nhiên trong SubAIList (102).
- `AISelectTargetTask` = { `FMultiList m_listResult` } — chọn target theo filter.
- `AIBasedPlayerDistanceTask` = { `FP m_Distance`, `int m_index` } — chọn sub-task theo khoảng cách (mini phase tree).
- `NpcEntity` (NpcEntity.cs): `m_aiTaskId` (+0x114), `SkillList` (+0x120), `AICmpt` (+0x160), `m_lifeTime`, `GetSkillByIdx`, `HaveSkill` — monster runtime = NpcEntity + AICmpt + AI task tree.

Phase-switch — HP-keyed, skill swap:
- `BattleCore.src.Actor.BossChangeBehaviorCmpt` (BossChangeBehaviorCmpt.cs): `int m_curPhaseId` (+0x1C, init −1), `uint[] m_replaceSkills` (+0x20); `OnHpChg(FP percent, bool isDecrease)` → `GetJiangHuBossPhaseConfig(ulong lossHp)` → `OnChangePhaseId(phaseConfig, damage)`; `GetReplaceSkills()`.
- `JiangHuBossPhaseConfig` = { `uint Phase` (+0x10), `uint MonsterAI` (+0x14), `ulong BossDamageMin` (+0x18), `ulong BossDamageMax` (+0x20), `uint BootyID` (+0x28), `uint[] Skill` (+0x30) } — **phase = cửa sổ damage tích lũy [Min,Max]**, mỗi phase có AI tree riêng (MonsterAI) + skill set riêng (Skill[]) + booty.
- `JiangHuBossCfgMgr.m_dictJiangHuBossPhaseConfig` (tra cứu theo phase/damage).
- Wave spawn boss: `WaveRefresh.m_IsBronBoss` (+0xC0), `m_CurBronBossData (BossBronData)` (+0xC8); `BossBronData` = { `ActorEntityCreateData m_data`, `TSVector m_pos`, `ActorEntity m_targer` }.
→ Kết luận surface: **boss = NpcEntity + AITask tree (MonsterCfg.AITaskID) + BossChangeBehaviorCmpt (HP% hook → phase table damage-window → replace skill set + MonsterAI)**. Phase trigger model resolved về phía dhcd: damage-window table, không phải timer/cast-count (map.md "Not yet specified" — có thể đóng với ticket quyết định).

## 4. Endless — own mode, surface cần thiết kế

dhcd endless surface (đúng như map.md — chỉ wave refresh, không có endless game-mode declaration):
- `LevelMonsterMgr.GetEndlessWaveCount()` (+ `m_runWaveCnt`, `m_waveID`, `m_stageID`) — đếm wave đã chạy (LevelMonsterMgr.cs line 50).
- `LevelWaveConfig` (LevelWaveConfig.cs): `WaveType`, `WavePoolID`, `TriggerType` (enum WaveEventFuncType: TIME=1, MONSTER_COUNT_PERCENT=2, MONSTER_HP_PERCENT=3, MONSTER_PLAY_SKILL=4, KILL_ALL_THISID=5, KILLALL_AND_TIMEOVER=6, OCCUPY_START=7, OCCUPY_END=8, OCCUPY_ALLEND=9), `EndType`, `EndParam`, `IsDeleteAllMonster`, `TriggerTime`, `TriggerParams`, **`IsReposeWave` (+0x40) — wave lặp lại**; `LevelWave` wrapper (m_waveFunc, WaveRefresh m_wave).
- `WaveRefresh` (WaveRefresh.cs): `m_isLoop`, `m_intelval` (+0x30), `m_refreshMonsterTime`, `m_lifeTime`, `m_dynamicMonsterMaxNum` (+0x88), `m_dynamicLoopNum` (+0xA0), `m_dieMonsterCount`, `m_IsBronBoss`, `m_isShowSkillShop`, `OneFrameMaxCreateNum=100`, `SpawnMonsterNormal`, `TimeOver`, `OnActorDie` — **loop-wave với interval + dynamic cap + shop flag + boss flag** = skeleton endless.
- `WavePoolConfig` = { `WavePoolID`, `Time`, `MonsterPoolID`, `MonsterNum`, `SheerMonsterNum`, `Interval`, `SingleNum`, `DynamicMonsterTime`, `DynamicLoopNum`, `DynamicMonsterMaxNum`, `Isloop`, `MonsterLifeTime` }.
- `LevelRandomSkillCtrl.HandleEndlessSkillPool` — endless skill pool riêng (pool đổi theo wave).
- `FuncIdDef` (BattleCore): `EndlessCountTime = 268`, `EndlessNewWaveTipTime = 275` — func-id cho UI tip/time.
→ dhcd KHÔNG có difficulty-ramp declaration (không có scaling curve). Toàn bộ ramp (monster HP/ATK theo wave, pool mở rộng, tốc độ spawn, boss định kỳ) là own design. Surface nên vay: LevelWaveConfig loop + WaveRefresh dynamic caps + HandleEndlessSkillPool pattern.

## 5. GAP LIST — structure-parity vs own-design

### Structure-parity (bắt buộc mirror, theo Decision 01/02)

| # | Surface | Parity (dhcd) | Ghi chú |
|---|---|---|---|
| S1 | Per-role pending state | `Dictionary<ulong,PlayerRandomSkillData>` + per-role `Queue<RandomSkillParam>` + `FP beginWaitingLearnTime` + `IsPlayerWaitingLearn()` time-predicate | Survivor P1 đã có match brain levelup → cần nâng thành per-role data (mobile single-player: roleId cố định, vẫn giữ cấu trúc để P3 multiplayer) |
| S2 | 3-mode request | `RandomSkillParam.Type {1 normal, 2 box, 3 shop}` + `boxParam.learnNum`; `RequestRandomSkill` = enqueue-nếu-waiting / trigger-ngay-nếu-không | Levelup đã có (P1 OverlayPanel 3-card) |
| S3 | Selection→pump | `Check*SkillEvent(roleId, gid, ...)` → success ⇒ `CheckWaitingList(roleId)` | GID = `m_idCnt++` global counter; event param chứa RoleId+GID+SkillList |
| S4 | Event payload | `LevelEventParam` (skillCount, canLearnNum, stageId) / `LevelBoxEventParam` (WillLearnSkillList) / `LevelSkillShopEventParam` (SkillList + `LevelSkillShopParam{SkillId,IsBuy,Price}`) | Shop entry có giá per-skill |
| S5 | Reroll | `FrameCmdRerandomSkill{EventGID}` (levelup reroll) + `FrameCmdReSelectRandomSkill{RollCnt}` (shop reroll, `XianDaoShopConfig.RefreshPrice` cố định) | 2 reroll riêng biệt, không gộp |
| S6 | Pool/weight | `RandomSkillConfig{LevelUpRandomWeight, FirstLevelRandomSkillWeight, CanRepeatSelect}` + `RandomSkillLibraryConfig{Level,IsMaxLevel,ClasifyType,SkillID,BuffID,RewardID,IsSuperWeapon,DependSkills}` + `GetMaxLearnSkillCount(type)` | Own weights; clasify ATK/Def/Supply = loại card |
| S7 | Box source | `LevelBoxSkillData{SkillID,SkillLv}` + `SkillBoxRandomWidgetConfig{ItemID,SkillNum,Weight}`; box trigger `TriggerBoxRandomSkillEvent(roleId, learnNum)` | Box = pre-roll sẽ học (multi-learn), khác levelup shape |
| S8 | Shop scope | `XianDaoShopConfig{LevelID,StageID,PoolID,BuyPriceWeight,RefreshPrice}` + `WaveRefresh.m_isShowSkillShop` | Shop = stage/wave-scoped, không phải global |
| S9 | Client mirror | `BattleLearnSkillCtrl` mirror queue + `m_isShowing*UI` + delay timer + `IsShowing()` | Survivor OverlayPanel cần lock queue tương đương (timescale pause parity r-dhcd-003: pause counter, KHÔNG claim input lock) |
| S10 | Boss flag | `MonsterCfg.IsBoss/IsWorldBoss`, `Skills[]`, `AITaskID` | Boss = NpcEntity + AI tree |
| S11 | Boss phase | `BossChangeBehaviorCmpt.OnHpChg → GetJiangHuBossPhaseConfig(lossHp) → OnChangePhaseId` + phase table `{BossDamageMin,Max, MonsterAI, Skill[], BootyID}` | **damage-window keyed**, skill set + AI tree swap; KHÔNG timer/cast-count |
| S12 | AI task tree | `AITaskConfig{Type,SubAIList,Param[]}` + Sequence(101)/RandomSelect(102)/PlaySkillByDistance(19)/Attack(2)/BasedPlayerDistance(18)/SelectTarget(12) | PlaySkillByDistance = skill theo dải khoảng cách (dùng được cho melee/ranged boss pattern) |
| S13 | Wave loop | `LevelWaveConfig.IsReposeWave` + `WaveRefresh{m_isLoop, m_intelval, m_dynamicMonsterMaxNum, m_dynamicLoopNum, m_IsBronBoss, m_isShowSkillShop}` + `WaveEventFuncType` trigger/end | Endless skeleton đã tồn tại trong dhcd wave system |

### Own-design (dhcd không có declaration → tự thiết kế, không phải gap)

| # | Mục | Quyết định cần |
|---|---|---|
| O1 | Endless difficulty ramp | Hệ số HP/ATK/speed monster theo `GetEndlessWaveCount()`; family curve (linear/exponential/stair) — map.md chưa chốt |
| O2 | Endless pool mở rộng | `HandleEndlessSkillPool` pattern: pool đổi theo wave — thời điểm mở card mới |
| O3 | Boss định kỳ + loot | Wave N boss → booty; boss HP scale theo wave |
| O4 | Shop currency | dhcd không có currency trong slice (chỉ Price); Survivor cần chọn: coin drop riêng vs dùng XP — ngoài phạm vi dhcd |
| O5 | Phases cho boss thường (không phải JiangHu) | Phase table chỉ tồn tại ở JiangHu boss; boss thường chỉ có AI tree — quyết định phase cho survivor boss (dùng damage-window table làm chuẩn chung) |
| O6 | `m_beginWaitingLearnTime` | FP time-window: thời gian tối đa player giữ modal; hết → auto/skip — số cần own tuning |

### Fail-closed / unresolved (KHÔNG port như parity)

- Input lock/global pause khi modal mở: KHÔNG proven (r-dhcd-002/003). Survivor dùng timescale pause counter (P1 đã có) — là own decision, không claim dhcd parity.
- FIFO queue semantics: Queue type + enqueue/dequeue calls proven, method bodies absent → vẫn "high-confidence" (r-dhcd-002). Cấu trúc mirror OK, không suy diễn thêm.
- `FrameCmdSelectRandomSkill`/`SelectBoxSkill` IL bị lỗi decompile (reconstructed-types/_logs/*.error.log) — chỉ dùng diffable-cs declaration.

## Sources

- `C:/Projects/dhcd/il2cpp/diffable-cs/DiffableCs/BattleCore/BattleCore/` (LevelRandomSkillCtrl.cs, PlayerRandomSkillData.cs, RandomSkillParam.cs, RandomSkillBoxParam.cs, RandomSkillEventType.cs, LevelEventParam.cs, LevelBoxEventParam.cs, LevelSkillShopEventParam.cs, LevelSkillShopParam.cs, LevelBoxSkillData.cs, RandomSkillConfig.cs, RandomSkillLibraryConfig.cs, RandomSkillClasifyType.cs, SkillBoxRandomWidgetConfig.cs, RandomSkillCmpt.cs, RandomSkillData.cs, BattleCmd*.cs, BattleCmdRun.cs, BattleCmdRunFactory.cs, BattleCmdMgr.cs, FrameCmdSkill.cs, BattleCmdSkillRun.cs, MonsterCfg.cs, MonsterCfgMgr.cs, NpcEntity.cs, AIBaseTask.cs, AITaskConfig.cs, AITaskType.cs, AIPlaySkillByDistanceTask.cs, AIAttackTask.cs, AISequeneceTask.cs, AIRandomSelectTask.cs, AISelectTargetTask.cs, AIBasedPlayerDistanceTask.cs, BossChangeBehaviorCmpt.cs, JiangHuBossPhaseConfig.cs, JiangHuBossCfgMgr.cs, JiangHuBossDifficultConfig.cs, LevelWave.cs, LevelWaveConfig.cs, WaveRefresh.cs, WavePoolConfig.cs, WaveFuncCtr.cs, WaveFuncBase.cs, WaveFuncBy*.cs, WaveEventFuncType.cs, LevelMonsterMgr.cs, NormalLevelLogic.cs, BattleLevelLogic.cs, BattleXianDaoLevelLogic.cs, XianDaoShopConfig.cs, EndlessSkillData.cs, FuncIdDef.cs, BattleSys.cs)
- `C:/Projects/dhcd/il2cpp/diffable-cs/DiffableCs/GameLogic/A5Game/BattleLearnSkillCtrl.cs`
- `C:/Projects/dhcd/reconstructed-types/BattleCore/` (BattleCore.LevelRandomSkillCtrl.cs, BattleCore.PlayerRandomSkillData.cs, BattleCore.NormalLevelLogic.cs, BattleCore.BattleXianDaoLevelLogic.cs, BattleCore.src.Actor.BossChangeBehaviorCmpt.cs, _logs/BattleCore.FrameCmdSelectRandomSkill.error.log, _logs/BattleCore.FrameCmdSelectBoxSkill.error.log)
- `C:/Projects/dhcd/docs/evidence/r-dhcd-002-modal-queue.md`, `r-dhcd-003-pause-timescale.md`

## Verdict cho map.md

- "Boss phase trigger model" (Not yet specified): **resolved** — HP/damage-window phase table (JiangHuBossPhaseConfig) + BossChangeBehaviorCmpt skill swap. Ticket quyết định có thể chốt damage-window chuẩn chung.
- "Endless ramp curve family": vẫn own design (O1) — dhcd chỉ cho wave-loop skeleton.
- "Card pool composition law": RandomSkillConfig 2-weight + clasify 3 loại — cấu trúc có sẵn, số liệu own (đã biết từ 02).
