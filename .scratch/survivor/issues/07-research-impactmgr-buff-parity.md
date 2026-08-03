# 07 — Research: ActorAttr / ImpactMgr full buff-debuff parity

Type: `research`
Status: ``resolved``
Blocked by: 01

## Question

ActorAttrData + ActorAttrImpactData + ActorAttrImpactMgr → full buff/debuff/poison/freeze/burn
parity. Cần:

1. Attribute fields + impact type enum (poison/burn/freeze/stun/slow/silence/...).
2. Impact lifecycle: apply / stack / tick / remove / expire; DOT (poison/burn) tick model; control
   (freeze/stun) + `ActorStunState` interaction với ActorSM.
3. Owner/source attribution (impact từ ai, damage bookkeeping).
4. Gap list: phần own-design (numeric) vs structure-parity.

## Output

Ghi `research/impact-buff.md`. Đọc: `BattleCore.ActorAttrData.cs`, `ActorAttrImpactData.cs`,
`ActorAttrImpactMgr.cs`, `ActorStunState.cs`, `ActorSM.cs`, `AttrDataConfigMgr.cs`,
`PlayerEntity.cs` (buff bookkeeping). Cite declaration; numeric = own.

## Answer

dhcd KHÔNG có enum status tên poison/freeze/silence riêng — model generic:
- **Stat buff** = `ActorAttrImpactData` → `ActorAttrImpactMgr` **4 bucket** (Absolute/Relative/Multiply/Effect, `ActorAttrAddType`).
- **Control** = `BuffStateID` bitmap 20 state (stun/no-move/no-skill/sleep/confusion/invisible...); freeze/slow/silence = attr-flavor không tên riêng.
- **DOT** (poison/burn) = generic `BuffDot` (loop-timer tick, `TickWhenAdd`, `RemoveAfterDot`, heal variant); `DamageInfo.sourceType=SourceBuffer`.
- **Stun** → `ActorSM` qua `ActorStateEvent.Enter/Finish_Stun` + `ActorStunState.s_map`.
- **Attribution** = `SkillImpactSource{skillId,buffId}` + caster ref → `SumSkillDamage` (kill credit → XP).
- **Buff config** = stack-level attr array + DOT + func (`BuffAttrConfig.FindAttr(stack)`); stack/replace/refresh (`BufferItem.ReplaceAdd`).
13 structure-parity (S1-S13) + 9 own-design (O1-O9). Khuyến nghị: giữ generic model + element flavor ở config, KHÔNG tạo enum status riêng. Full: research/impact-buff.md
