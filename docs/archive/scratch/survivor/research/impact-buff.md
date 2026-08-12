# Research 07 — ActorAttr / ImpactMgr / Buff parity (structure)

Status: `done` (sub-agent `research/impact-buff`, ticket 07)
Sources (read-only):
`C:/Projects/dhcd/reconstructed-types/BattleCore/*.cs`, `C:/Projects/dhcd/reconstructed/GameProto/ResDef/*.cs`,
`C:/Projects/dhcd/il2cpp/diffable-cs/DiffableCs/BattleCore/BattleCore/*.cs`
Quy ước: mọi thứ dưới đây là **declaration + IL recovery** (không authoritative behavior).
Số = config (ResAttrImpactData / BuffAttrConfig / SkillAttrDamageData) → **own-design**. Structure = parity target.

---

## 1. Attribute fields + impact type enum

### 1.1 ActorAttrData — 87 fields, 1 file
`BattleCore/BattleCore.ActorAttrData.cs` (class `ActorAttrData : FMemPoolObject`).

- Flat scalars: `MaxHP` (int), `Damage`, `HurtThrough`, `JianDamage`, `NuDamage`, `ShanDamage`,
  `FeiBiaoDamage` (weapon-type damage), `CollisionDamageReduce`, `BulletDamageReduce`,
  `DamageReduce`, `RecoveryHpVal`, `CritAtkRatio`, `DodgeRatio`, `HitRatio`, `CritAtkMulti`,
  `HeadShotRatio`, + `*Ratio` family (`CollisionDamageReduceRatio`, `BulletDamageReduceRatio`,
  `DamageReduceRatio`, `HurtThroughRatio`, `RecoveryHpPercent`, `HurtAddRatio`,
  `BlockDamageRatio`, `BlockReduceRatio`, `EquipAttrRatio`, `GuaJiRewardRatio`, `AddExpRatio`,
  `AddGetMoneyRatio`) — tất cả `FP` (fixed-point, `FP` = dhcd numeric type; mobile dùng float per map).
- Movement/skill: `MoveSpeed`, `InitSkillCount` (int), `ZhaoShiCD`, `MaxMP`, `PickUpRange`,
  `SkillAttckRangeEnhance`, `SkillCDReduceRatio`, `SkillKeepTime`, `BallisticVelocityEnhance`,
  `BulletThrough`, `PickUpRangeEnhance`, `DamageFrequencyReduce`, `RepelRatio`, `HpCount` (int),
  `StatusResistance` (int/FP? — declared FP; dùng trong BufferItem.Create), `ExtraMpRatio`,
  `BattleRecoveryHpRatio`, `SkillDamageRatio`, `SelectTargetRangeIncrease`,
  `MeleeAttackDamageRatio`, `RemoteAttackDamageRatio`.
- 5-element (Metal/Wood/Water/Fire): `MetalAtk/WoodAtk/WaterAtk/FireAtk`, `MetalDef..FireDef`,
  `MetalAtkRate..FireAtkRate`, `MetalAtkTriggerRate..FireAtkTriggerRate`, `AllElementAtk`,
  `AllElementDef`, `AllElementRate`, `AllElementBreakRate`, `AllElementBreakResistRate`,
  `MetalToughnessRate..FireToughnessRate`, `MetalToughnessRecoverTime..FireToughnessRecoverTime`.
- Misc: `XiuLianXiuWeiRatio`, `PetFuShenSkillCDReduce`, `HuaShenSkillAddTime`, `ShenJiDamageRatio`,
  `ShenJiDamageReduceRatio`, `SpiritStoneDisCount`, `MythicalAnimalAddTime`.

API (declaration): `CalImpact(ActorAttrImpactData)` — apply 1 impact vào attr (IL lost);
`Set(ActorAttrData src)` — copy toàn bộ field; `SetAttr(ActorAttrDataType, FP)` — set-by-enum;
`InitFromPool()` — zero-init.

### 1.2 ActorAttrDataType enum — 88 values
`C:/Projects/dhcd/reconstructed/GameProto/ResDef/ActorAttrDataType.cs`
`None=0, MaxHp=1, Damage=2, HurtThrough=3, JianDamage=4, NuDamage=5, ShanDamage=6, FeiBiaoDamage=7,
CollisionDamageReduce=8, BulletDamageReduce=9, DamageReduce=10, RecoveryHpVal=11, CritAtkRatio=12,
DodgeRatio=13, HitRatio=14, CritAtkMulti=15, HeadShotRatio=16, ...Ratio=17..28, MoveSpeed=29,
InitSkillCount=30, ZhaoShiCD=31, MaxMp=32, PickUpRange=33, SkillAttckRangeEnhance=34,
SkillCDReduceRatio=35, SkillKeepTime=36, BallisticVelocityEnhance=37, BulletThrough=38,
PickUpRangeEnhance=39, DamageFrequencyReduce=40, RepelRatio=41, HpCount=42, StatusResistance=43,
ExtraMpRatio=44, BattleRecoveryHpRatio=45, XiuLianXiuWeiRatio=46, PetFuShenSkillCDReduce=47,
HuaShenSkillAddTime=48, SkillDamageRatio=49, ShenJiDamageRatio=50, ShenJiDamageReduceRatio=51,
(gap 52,54), SelectTargetRangeIncrease=53, MeleeAttackDamageRatio=55, RemoteAttackDamageRatio=56,
SpiritStoneDisCount=57, MetalAtk=58..FireAtk=61, MetalDef=62..FireDef=65, *AtkRate=66..69,
*AtkTriggerRate=70..73, AllElementAtk=74, AllElementDef=75, AllElementRate=76,
AllElementBreakRate=77, AllElementBreakResistRate=78, *ToughnessRate=79..82,
*ToughnessRecoverTime=83..86, MythicalAnimalAddTime=87` — 1:1 với field list, gap value = enum legacy.

### 1.3 ActorAttrAddType — 4 kiểu phép cộng
`C:/Projects/dhcd/reconstructed/GameProto/ResDef/ActorAttrAddType.cs`
`INVAL_VAL=0, ABSOLUTE_VAL=1, SUM_PERCENT_VAL=2, MUL_PERCENT_VAL=3`.
Khớp 4 bucket list của ActorAttrImpactMgr: `m_listEffect` (add), `m_listAbEffect` (absolute),
`m_listRelEffect` (sum-percent), `m_listMulEffect` (mul-percent).
→ **Công thức attr = base → abs → sum% → mul% → add** (thứ tự bucket trong
`RefreshFinalAttr` IL: Ab → Rel → … → Effect; phần giữa lost, declare-order).

### 1.4 Impact type — KHÔNG có enum "status" riêng
Trả lời câu hỏi ticket (poison/burn/freeze/stun/slow/silence): dhcd **không** có enum
impact-type cho status. Phân loại nằm rải:
- **Stat buff/debuff** = `ResAttrImpactData` (short DataType + byte AddType + FP Value) →
  `ActorAttrImpactMgr` 4 bucket. File: `BattleCore/BattleCore.ResAttrImpactData.cs` (proto wire,
  `{DataType, AddType, Value}`), `BattleCore.ActorAttrImpactData.cs`
  (runtime: `Type m_producer, ActorAttrAddType m_addType, ActorAttrDataType m_dataType,
  FP m_value, int m_priority`; API `UpdateData/CalAddVal/CanMerge/Merge/Clone` — Merge cộng value
  (`value = other + m_value`), CanMerge = điều kiện gộp, priority khai báo nhưng không thấy dùng).
- **Control status** = `BuffStateID` enum (20 state):
  `C:/Projects/dhcd/il2cpp/diffable-cs/DiffableCs/BattleCore/BattleCore/BuffStateID.cs`
  `NONE=0, STUN=1, UNDEAD=2, INVISIBLE=3, BIGGER=4, NO_MOVE=5, NO_SKILL=6, SLEEP=7,
  WALK_WATER=8, WALK_BOX=9, FORCE_COLLIDER=10, NO_TRAP_DAMAGE=11, TRAP_MONSTER=12,
  FULL_INVISIBLE=13, BIANSHENS=14, PLAYER_SHADOW=15, GET_UPSPEED=16, MONSTER_NO_MOVE=17,
  MONSTER_AOE=18, CONFUSION=19, MAX=20`.
  - slow → **không có** state; = MoveSpeed impact (ActorAttrDataType.MoveSpeed=29) hoặc
    `CheckSpeedChangeByScene` / `BuffWindEffectFunc` (wind, speed-ref).
  - silence → `NO_SKILL` (= BuffManger `m_muteSkill`, `CanSkill()` gate).
  - freeze → **không có** state tên freeze; control cứng = STUN / SLEEP / NO_MOVE
    (+ MONSTER_NO_MOVE cho monster). Element ice nằm ở 5-element attr layer, không phải status.
  - poison/burn → **không có** state; = generic `BuffDot` với element qua
    `SkillAttrDamageData.MagicType` + `DamageInfo.magicType` / `m_triggerElements`.
- **Sub-state classes**: `BuffStateStun.cs`, `BuffStateConfusion.cs`, `BuffStateInvisible.cs`,
  `BuffStateBigger.cs` — `BuffState` base
  (`BattleCore/BattleCore.BuffState.cs`: `m_actor, m_buffStateId, m_running, m_param`;
  `Enter(FP param1)`, `Leave()`, virtual `OnEnter/OnLeave/OnDestroy`; Enter/Leave gửi visual event
  qua `SendBuffStateVisualEvent`).

### 1.5 Buff config (data-driven impact nguồn)
- `BattleCore/BattleCore.BuffConfig.cs`: `BuffID, TimeType(byte), ReplaceType(byte), State(BuffTriggleState)`.
  `BuffTimeType` = `BUFF_TIME_DURING=0, BUFF_TIME_INFINIT=1`
  (`il2cpp/diffable-cs/.../BuffTimeType.cs`).
- `BattleCore/BattleCore.BuffAttrConfig.cs`: `BuffID, StackNum(int), DurTime(FP),
  RemoveWhenDie(byte), EffectID, DotDamageData(SkillAttrDamageData), DotTickConfig(BuffDotTickConfig),
  AttrData(ResAttrImpactData[]), FuncData(BuffFuncData)` — **1 buff = 1 stack config**:
  attr impacts array + DOT damage config + optional custom func.
- `BattleCore/BattleCore.BuffConfigClient.cs`: `buffConfig + List<BuffAttrConfig> listAttrConfig;
  FindAttr(int stackNum)` → chọn config theo stack (tăng stack → config khác).
- `BattleCore/BattleCore.BuffDotTickConfig.cs`: `TickTime(FP), TickWhenAdd(byte), RemoveAfterDot(byte)`.
- `BattleCore/BattleCore.SkillAttrDamageData.cs`: `MagicType(int), AttrType(int), Param1/2/3(FP)` —
  DOT damage formula config (AttrType = attr nguồn, Param = hệ số; numeric = own).
- `BattleCore/BattleCore.AttrDataConfigMgr.cs`: `m_baseType (Dictionary<int,bool>)` (attr nào là
  base-type), `ElementBreakConfig m_elementBreak` — gần như opaque, chỉ declaration.

---

## 2. Impact lifecycle

### 2.1 Apply — BuffManger.AddBuff
`BattleCore/BattleCore.BuffManger.cs` (`BuffManger : ActorEntityCmpt`).
- State: `Dictionary<int,BufferItem> m_allBuff`, `List<BufferItem> m_listBuff`,
  `bool[] m_buffState / m_buffStateParam(FP[]) / m_oldBuffState`, `BuffStateMgr m_stateMgr`,
  flags `m_translate, m_muteMove, m_muteSkill, m_muteDamage, m_walkOnWater, m_walkOnBox,
  m_walkOnMapItemObstacle, m_cantBeSelect, m_colliderVisible`,
  `List<AdjustDamageFunc> m_damageAdjustList`, `List<AdjustCriptFunc> m_critRatioAdjustList`,
  `ActorAttrImpactMgr _impactMgr` (lazy, reg vào `ActorData.RegRuntimgAttrImpact`), `m_listToDel`.
- Entry: `AddBuff(int buffId, ActorEntity caster, bool fromRandomSkill, int stackNum = 0, FP promoteVal)`
  → lookup config → check forbid/trigger rule (`BuffConfigMgr.GetBuffForbidRule`, line 521
  `BattleCore/BattleCore.BuffConfigMgr.cs`) → status-resistance check (dưới) → create or stack.
- Callers (buff apply từ đâu): `SkillDamageHelper.AddBuff(target, caster, buffID, skillId, stackCount)`
  (`BattleCore/BattleCore.SkillDamageHelper.cs:1572` — từ skill impact pipeline),
  `SkillBuffHandle.cs`, `SkillRepeatBuffHandle.cs`, `BuffHittedAddBuffFunc.cs` (bị đánh → buff),
  `BuffHpBelowTriggeAddBuffFunc.cs`, `BuffIdleAddBuff.cs`, `BuffMpRangeActiveBuffFunc.cs`,
  `CollectItemBuffEntity.cs`, `TrapEntity.cs:653`, `IceEntity.cs:446`, `BianShenCmpt.cs:207`,
  `ShenShouCmpt.cs:516`.

### 2.2 BufferItem — 1 buff instance
`BattleCore/BattleCore.BufferItem.cs`
- Fields: `BuffStackNum, BuffID, m_timeType, m_clientConfig(BuffConfigClient),
  m_baseConfig(BuffConfig), m_attrConfig(BuffAttrConfig), m_fromRandomSkill, m_buffExpired(Action),
  m_durTimer(FTimer), m_buffMgr, m_dot(BuffDot), m_buffFunc(BuffBaseFunc),
  m_promteVal(FP), m_statusResistanceVal(FP)`.
- `Create(target, caster, buffMgr, buffId, config, fromRandomSkill, stackNum, promoteVal)`:
  `m_statusResistanceVal = target.AttrData.StatusResistance` (status-resist snapshot tại apply);
  `TimeType==DURING` → `SetDurtime(attrConfig.DurTime)` (once-timer → `m_buffExpired`);
  `m_promteVal = promoteVal`; có DotDamageData+DotTickConfig → `m_dot.Init(...)`;
  có FuncData → tạo `BuffBaseFunc` (factory: `BuffFuncFactory.cs`).
- `ReplaceAdd(out needRefreshAttr, stackNum)`: re-apply → **refresh duration**
  (`durtime = remaining + elapsed`, SetDurtime) — không cộng stack theo mặc định; cấu hình
  ReplaceType trong BuffConfig quyết định (declaration).
- Expire: `m_buffExpired` → `DestroyBuffFromTimer` → `m_buffMgr.RmvBuff(BuffID)`.
- Remove: `MarkToFree()` → `CreateOnceFrameTimer(m_buffExpired)` (xóa delayed 1 frame, chống
  mutate-list khi iterate).

### 2.3 Stack model
- Config có `StackNum` per-level; `BuffConfigClient.FindAttr(stackNum)` chọn level config
  (listAttrConfig = danh sách config theo stack).
- Merge data-level: `ActorAttrImpactData.CanMerge(other)` + `Merge(other)` (cộng value) —
  gộp impact cùng loại khi apply.
- `BuffManger.GetBuffStackNum(buffId)` / `GetBuffItem(buffId)`; `LoseBuff(buffID, loseStackNum,
  isRemove=true)` — giảm stack hoặc remove hẳn.
- Stack change → `BuffBaseFunc.OnStackChanged()` (vd `BuffFuncSkillAttrAdd.OnStackChanged` → reset
  dict add-attr; `UpdateAttrOnStackChanged(funcConfig)`).

### 2.4 Attr impact pipe (buff → attr final)
`BuffManger.RefreshBuffAttr()`: `ImpactMgr.ClearImpact()` → iterate `m_listBuff` (mỗi buff ×
stack) → `AddAttrImpact(ResAttrImpactData)` / `AddAttrImpact(resData, additiveVal)` (promoteVal
cộng thêm, IL: `val = resData.Value + additive`) → `ImpactMgr.SetDirty()`.
`ActorAttrImpactMgr`: `MergeAttrImpact(impactData)` (clone + merge), `AddAttrImpact(dataType,
addType, val)`, `ClearImpact()`, `m_changed` (Action) → ActorData.RefreshAttr.
`ActorData` (`BattleCore/BattleCore.ActorData.cs`): `m_baseData → m_attrData` (final);
`RefreshAttr()`: baseChanged → `m_attrData.Set(m_baseData)`; runtimeChanged →
`RefreshRuntimeAttr(m_attrData)` (chạy mọi runtime impact mgr đã `RegRuntimgAttrImpact`, gồm
BuffManger._impactMgr); MoveSpeed đổi → `SendMoveSpeedChanged` event.
`ActorAttrImpactMgr.RefreshFinalAttr(ActorAttrData)`: iterate 4 bucket theo thứ tự
`CalAttrList(Ab) → CalAttrList(Rel) → …` (mul/effect phần IL lost; declare-order Ab→Rel→Mul→Effect).

### 2.5 DOT tick model (poison/burn/heal-over-time)
`BattleCore/BattleCore.BuffDot.cs`:
- Fields: `m_dotVal(int), m_isAtk, m_isHaveDefend, m_isNoAttrImpact, m_dotTimer(FTimer),
  m_dot(Action), m_caster, m_target, m_damageInfo(DamageInfo), m_impactData(SkillImpactData),
  m_source(SkillImpactSource), m_fromRandomSkill, m_buff(BufferItem),
  m_removeAfterDot, m_isHPNaturalRecovery`.
- `Init(buff, target, caster, buffId, damageConfig(SkillAttrDamageData), tickConfig(BuffDotTickConfig),
  fromRandomSkill, promoteVal)`:
  `damageInfo.sourceType = SourceBuffer(4)`; `m_dotVal` = damageConfig (attr+params) + promoteVal;
  `m_isHPNaturalRecovery = TickConfig…` (heal DOT); `m_removeAfterDot = tickConfig.RemoveAfterDot`;
  `tickConfig.TickWhenAdd != 0` → `Dot()` ngay; rồi
  `timerMgr.CreateLoopTimer(ref m_dotTimer, …, tickConfig.TickTime, m_dot)` — **loop timer = tick**.
- `Dot()`: guard caster died / target died / `m_fromRandomSkill` (random-skill DOT không tick —
  visual-only); nếu `m_isHaveDefend` → `damageHelper.CalcBuffDamage(target, impactType, ref dotVal)`
  (defend áp dụng); nếu `m_muteDamage` (buff muffle) → skip; gửi
  `ActorEntityEventHelper.SendSkillImpacted(caster, target, damageInfo, impactData, source)` —
  đi qua pipeline damage đầy đủ (bookkeeping, kill credit); `m_removeAfterDot` → `m_buff.MarkToFree()`
  (single-tick DOT).
- `Destroy()`: `timerMgr.DestroyTimer` — hủy khi buff remove.
- `RefreshAttr(...)`: `ImpactType = HIT_RECOVERY | NORMAL_HIT`; `m_dotVal` recompute (dùng khi
  buff re-apply/promote).

### 2.6 Control (freeze/stun/slow/silence) + ActorStunState + ActorSM
- `BuffStateMgr` (`BattleCore/BattleCore.BuffStateMgr.cs`): `BuffState[] m_allState`,
  `SetState(stateId, param1)`, `ClrState(stateId)`, `InitAllState(actor)` — đăng ký instance
  BuffState (Stun/Confusion/Invisible/Bigger subclasses).
- `BuffManger.UpdateBuffState()`: `CopyBuffState(old, cur)` → `ResetBuffState(cur)` → iterate
  `m_listBuff`, mỗi buff set bit `BuffStateID` tương ứng (+ `m_buffStateParam` FP param, vd
  stun duration / move-speed scale) → so sánh old/cur → `NotifyBuffStateChange(stateId, set)` →
  `ActorEntityEventHelper.SendBuffStateChanged`. Các cờ gate (`m_muteMove/m_muteSkill/m_muteDamage`)
  được recompute từ state; `CanMove()/CanSkill()/CanBeDamaged()/CanBeBackOff()` đọc cờ.
  `RemoveSleepTypeBuff()` — nhận damage xóa buff sleep-type.
- **Stun → SM bridge**: `BuffStateStun.OnEnter(FP param1)` →
  `SendStateEvent(Actor_Enter_Stun, param1)` (duration); `OnLeave()` →
  `SendStateEvent(Actor_Finish_Stun)`.
  `BattleCore/BattleCore.ActorStunState.cs` (`SMBaseState`): `static Dictionary<int,int> s_map`
  + `GetStateMap()` — transition map (event→state, opaque); `InitializeState()` gọi
  `AddTransition(trans, id)` ×2 (vd Finish_Stun→Idle, Die→Die; IL lost, chỉ declaration).
  `BattleCore/BattleCore.ActorSM.cs` (`XStateMachine`): `initializeStateMachine()` tạo 1 state
  per `ActorStateID` (`ActorStateID.cs`: `Null=0, Idle=1, Move=2, Skill=3, Die=4, Stun=5,
  Appear=6, Count=7`), `OnChangeActorStateEvent(ActorStateEvent, object data)` chuyển state.
  `ActorStateEvent.cs`: `…Actor_Enter_Stun=6, Actor_Finish_Stun=7, Actor_Die=8, Actor_Relive=9…`.
  `OnStateChange()` → `actor.CurrState = newVal`.
- **BuffManger ↔ SM coupling**: `OnActorStateChange(ActorStateID oldVal, ActorStateID newVal)`
  (đăng ký event trong Awake) — buff manager phản ứng state đổi (vd die → ClearAllBuff;
  declaration-only).
- Sleep ≠ Stun: state riêng `BUFF_STATE_SLEEP`; bị damage → RemoveSleepTypeBuff.

### 2.7 Remove paths
- `RmvBuff(buffID)` (BuffManger): tìm trong m_allBuff → `RemoveBuffFromList` →
  `RefreshBuffAttr()` → `UpdateBuffState()` → `SendActorBuffRmv` event → VisualInter.OnBuffRmv.
- `LoseBuff(buffID, loseStackNum, isRemove=true)` — stack-down; `MarkBuffToRemove(buffId)` —
  deferred (vd shield hết: `OnShiledValEnd`).
- `ClearAllBuff(fromDestroy)` — die/entity destroy; OnDestroy: `UnRegRuntimeAttrImpact` +
  `m_stateMgr.Destroy()`.
- `ClearImpact()` (ImpactMgr) — xóa toàn bộ impact (dirty → recompute attr).

---

## 3. Owner / source attribution

- **Impact data**: `ActorAttrImpactData.m_producer` (`Type` — producer class) — khai báo, không
  thấy set trong IL recovered.
- **Buff**: `BufferItem` KHÔNG lưu caster (chỉ `m_buffMgr`); caster duy nhất được giữ trong
  `BuffDot.m_caster` (DOT). AddBuff nhận `caster` để: status-resistance check +
  `BuffDot.Init(caster)` + promoteVal từ caster stat.
- **Damage attribution chain**:
  `SkillImpactSource {uint m_skillId; int m_buffId}` (`BattleCore/BattleCore.SkillImpactSource.cs`)
  — skill id + buff id nguồn; được truyền qua `SendSkillImpacted(caster, target, damageInfo,
  impactData, source)` và `SumSkillDamage(SkillImpactSource source, int damage)`.
  `DamageInfo` (`BattleCore/BattleCore.DamageInfo.cs`): `damage, addMP, isCrit, isMiss,
  isHeadShot, isBlockDamage, isDead, magicType(int, element), impactDir, damageToCenterDist,
  sourceType(DamageSourceType), m_runtimeOption, m_damageRatio, m_casterAtk, m_triggerElements,
  m_toughnessDamage`. DOT set `sourceType = SourceBuffer=4`
  (`DamageSourceType.cs`: `SourceNone=0, SourceBullet=1, SourceLightChain=2, SourceShootPoint=3,
  SourceBuffer=4`).
- **Kill/damage bookkeeping**: `ActorEntity.SumSkillDamage(source, damage)` (virtual; base cộng
  `RealTotalDamage`; `PetEntity` override — pet damage riêng, `PlayerEntity.SumSkillDamage` +
  `GetSkillDataByDamageSource(skillID, buffID)` + `m_otherSkillDamages` + `killMonsterNumber /
  killBossNumber` — `il2cpp/diffable-cs/.../PlayerEntity.cs`, body-less diffable).
  `BuffManger.ProcessAtkVal(targetActor)` / `ProcessCritRatio(targetActor)` — damage/crit adjust
  từ buff list (AdjustDamageFunc/AdjustCriptFunc delegates) trước khi dính vào pipeline.
- `SkillImpactData` (`BattleCore/SkillImpactData.cs`): `m_damageType(SkillHitDamageType:
  Bullet/BodyCollider/Machine), m_ImpactType(SkillImpactType: NORMAL_HIT=0, HIT_RECOVERY=1,
  HIT_BACKOFF=2, IMPACT_NONE=3, ADD_HP=4, ADD_MP=5, HIT_NO_IMPACT=6, HIT_MOVE=7,
  HIT_MOVE_NO_IMPACT=8), m_hitType, m_HitNum, m_impcatMinInteravl, m_shootId, m_backDir,
  m_backOffDist/Time/AccTime/Interval, m_moveData, m_damageIndex, m_damageRatio,
  m_fiveEleDamageRatio, m_impactAudio, m_damageWhenCasterDeath, m_shareFireInterval` —
  DOT dùng lại impact data của skill gây buff (element, backoff, damage ratio).

---

## 4. GAP LIST

### 4.1 Structure-parity (phải có, cite declaration)
| # | Hạng mục | Dhcd declaration | Mobile design note |
|---|---|---|---|
| S1 | Attr model 3 lớp base → runtime → final + dirty flag | `ActorData.cs` m_baseData/m_runtimeBase/m_attrData + m_baseChanged/m_runtimeChanged + RefreshAttr | SurvivorActorData: base + runtime impact list, recompute-on-dirty |
| S2 | Impact 4 bucket add/abs/sum%/mul% + order | `ActorAttrImpactMgr.cs` m_listEffect/AbEffect/RelEffect/MulEffect; RefreshFinalAttr; `ActorAttrAddType.cs` | 4 List<Impact>; order Ab→Rel→Mul→Effect |
| S3 | Impact data: dataType+addType+value+merge | `ActorAttrImpactData.cs` (CanMerge/Merge cộng value/Clone) | Impact struct + merge-by-(type,addType) |
| S4 | Buff container: allBuff dict + list + stack | `BuffManger.cs`; `BufferItem.cs` BuffStackNum/ReplaceAdd | BuffInstance {id, stack, remaining, attrConfig} |
| S5 | Buff config: stack-level attr array + DOT + func | `BuffAttrConfig.cs` (StackNum/DurTime/AttrData[]/DotDamageData/DotTickConfig/FuncData); `BuffConfigClient.FindAttr(stack)` | BuffDef {levels[], dot, func} |
| S6 | DOT: loop timer tick + TickWhenAdd + RemoveAfterDot + heal variant | `BuffDot.cs` (CreateLoopTimer/TickWhenAdd/RemoveAfterDot/m_isHPNaturalRecovery); `BuffDotTickConfig.cs` | DotDriver per buff; tick qua game time |
| S7 | Control states bitmap + gate flags | `BuffStateID.cs` (20 state); `BuffManger` m_muteMove/m_muteSkill/m_muteDamage + CanMove/CanSkill/CanBeDamaged | StatusFlags enum + gates; survivor cần subset (Stun, NoMove, NoSkill, Sleep, Confusion, Invisible…) |
| S8 | Stun → SM transition | `ActorStunState.cs` s_map + `ActorSM.cs` OnChangeActorStateEvent; `ActorStateEvent.cs` Enter/Finish_Stun | ActorSM state Stun + event trigger, duration param |
| S9 | Buff expire/remove/clear paths | `BufferItem` m_buffExpired/durTimer/MarkToFree; `BuffManger` RmvBuff/LoseBuff/ClearAllBuff | deferred removal (frame-delay) chống mutate-list |
| S10 | Source attribution | `SkillImpactSource.cs` {skillId, buffId}; `DamageInfo.sourceType=SourceBuffer`; `SumSkillDamage` | DOT damage gắn source (skillId+buffId) + caster; kill credit → XP |
| S11 | Status resistance tại apply | `BufferItem.Create` m_statusResistanceVal = target.StatusResistance | check resist tại AddBuff |
| S12 | Duration refresh trên re-apply | `BufferItem.ReplaceAdd` (remaining + elapsed) | re-apply = refresh duration (ReplaceType config) |
| S13 | Mute-damage gate áp dụng cả DOT | `BuffDot.Dot()` check `BuffManger.m_muteDamage` | gate chung |

### 4.2 Own-design (numeric / nội bộ mobile)
| # | Mục | Lý do |
|---|---|---|
| O1 | Mọi `FP` value trong config (attr value, DurTime, TickTime, DOT Param1/2/3, promoteVal) | dhcd config blocked (FastXXTEA) — map.md Decision 01; build từ JX PcSkills.txt + own balance |
| O2 | 5-element hệ (Metal/Wood/Water/Fire Atk/Def/Rate/Toughness/Break) — 30+ field | dhcd element layer khai báo nhưng không số; survivor MVP: chọn subset (vd Fire=burn, Water=freeze flavor) hoặc bỏ hẳn, skill lib quyết |
| O3 | Priority (`ActorAttrImpactData.m_priority`) | khai báo, không thấy dùng trong IL recovered — mobile bỏ hoặc giữ field 0 |
| O4 | `m_producer` (Type) attribution | khai báo nhưng không set trong recovered code — mobile dùng SkillImpactSource thay |
| O5 | Rpg-only fields (GuaJiRewardRatio, AddExpRatio, SpiritStoneDisCount, pet/faBao/shenShou/xiuLian…) | chế độ offline survivor không có hệ thống đó; chỉ giữ nếu skill lib cần |
| O6 | `AttrDataConfigMgr.m_baseType` / `ElementBreakConfig` | opaque, không đủ declaration → own config schema (SURVIVOR_PLAN data authoring, ticket 02) |
| O7 | Damage formula DOT (AttrType + Param1/2/3) | formula numeric = own; structure (attr-scaled + promote) giữ |
| O8 | Fixed-point `FP` | map quyết: float, không deterministic |
| O9 | Buff caster không lưu trên BufferItem (chỉ BuffDot) | dhcd layout quirk; mobile lưu caster ref trên BuffInstance cho sạch (visual seam + kill credit) |

### 4.3 Không có trong dhcd (survivor tự thêm — ngoài parity)
- freeze/silence/poison/burn **tên riêng**: dhcd dùng generic (attr impact / BuffDot / NO_SKILL /
  STUN). Mobile nên giữ generic model + đặt tên element flavor ở config, KHÔNG tạo enum status riêng
  (tránh fork logic).
- Buff UI icon/stack display: dhcd có BuffMangerVisualInter (event → visual), chi tiết body-less —
  mobile tự design.

---

## Verdict (1 dòng)
Structure cốt lõi: 4-bucket impact mgr (Ab/Rel/Mul/Effect) + stack-level buff config
(AttrData[] + DOT tick loop timer + control states bitmap → SM stun event) + source attribution
(SkillImpactSource{skillId,buffId} + SourceBuffer DOT) — đủ clear để port; mọi số là own.
