using System;
using System.Collections.Generic;
using UnityEngine;
using VLTK.Model;

namespace VLTK.Sandbox
{
    public class CombatActorState
    {
        public int actorId;
        public CombatFaction faction;
        public int level = 1;
        public int partyId = 0;
        public bool fightMode = true;
        public bool rideHorse;
        public int currentMana = 100;
        public int maxMana = 100;
        public int currentLife = 100;
        public int maxLife = 100;
        public int minDamage = 1;
        public int maxDamage = 3;
        public int attackSpeed;
        public int castSpeed;
        public int attackFrame = 18;
        public int castFrame = 18;
        public int activeSkillId;
        public int currentWeaponSkillId;
        public Vector2 position;
        public HashSet<int> knownSkills = new();
        public Dictionary<int, int> skillLevels = new();
        public Dictionary<MagicAttributeKind, SkillMagicAttribute> states = new();
    }

    public class CombatCastReport
    {
        public bool success;
        public CombatCastRejectReason reason;
        public SkillDefinition skill;
        public int skillLevel;
        public SkillLevelData levelData;
        public CombatActionState actionState;
        public int totalFrames;
        public int manaCost;
        public int childProjectileCount;
        public List<ProjectileInstance> projectiles = new();
        public List<SkillMagicAttribute> appliedState = new();
        public List<DamageResult> damageResults = new();
        public string detail;
    }

    /// <summary>
    /// PC combat cast gate and flow for novice attacks + Cái Bang skills.
    /// Mirrors KNpc::DoSkill/DoOrdinSkill, KSkill::CanCastSkill, KSkillList::CanCast.
    /// </summary>
    public class CombatRuntimeService
    {
        private readonly SkillCatalog _catalog;
        private readonly ProjectileService _projectiles;
        private readonly DamageFormulaService _damage;
        private readonly Dictionary<(int actorId, int skillId), int> _nextCastTime = new();

        public int CurrentTime { get; private set; }
        public float RangeWorldPerPcUnit { get; set; } = 1f;

        public CombatRuntimeService(SkillCatalog catalog, ProjectileService projectiles = null, DamageFormulaService damage = null)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _projectiles = projectiles ?? new ProjectileService();
            _damage = damage ?? new DamageFormulaService();
        }

        public void AdvanceTime(int ticks) => CurrentTime += Mathf.Max(0, ticks);
        public int NextCastTime(int actorId, int skillId) => _nextCastTime.TryGetValue((actorId, skillId), out var t) ? t : 0;

        public CombatCastReport Cast(CombatActorState caster, CombatActorState target, int skillId, Vector2 targetPoint, CombatRelation relation, ObstacleGrid grid = null)
        {
            return CastInternal(caster, target, skillId, targetPoint, relation, grid, bypassKnownSkillGate: false, forcedSkillLevel: 0);
        }

        public CombatCastReport CastNpcPlan(CombatActorState caster, CombatActorState target, NpcBossSkillCastPlan plan, Vector2 targetPoint, CombatRelation relation, ObstacleGrid grid = null)
        {
            if (plan.missingScriptGuard)
                return Reject(new CombatCastReport { reason = CombatCastRejectReason.None }, CombatCastRejectReason.NoSkill, plan.guardReason ?? "npc skill plan guarded");
            if (!_catalog.Contains(plan.skillId)) _catalog.Register(plan.ToSkillDefinition());
            return CastInternal(caster, target, plan.skillId, targetPoint, relation, grid, bypassKnownSkillGate: true, forcedSkillLevel: plan.maxLevel > 0 ? plan.maxLevel : 1);
        }

        private CombatCastReport CastInternal(CombatActorState caster, CombatActorState target, int skillId, Vector2 targetPoint, CombatRelation relation, ObstacleGrid grid, bool bypassKnownSkillGate, int forcedSkillLevel)
        {
            var report = new CombatCastReport { reason = CombatCastRejectReason.None };
            if (caster == null) return Reject(report, CombatCastRejectReason.InvalidTarget, "missing caster");
            if (!caster.fightMode) return Reject(report, CombatCastRejectReason.NotInFightMode, "PC KNpc::DoSkill returns when player not in fight mode");

            var skill = _catalog.Resolve(skillId);
            if (skill == null) return Reject(report, CombatCastRejectReason.NoSkill, "skill missing from catalog");
            report.skill = skill;

            // --- KSkillList::CanCast gates ---
            // PC: FindSame returns 0 if skill not in list
            if (!bypassKnownSkillGate && !caster.knownSkills.Contains(skillId)) return Reject(report, CombatCastRejectReason.SkillNotKnown, "KSkillList::FindSame: skill not in list");
            // PC: CurrentSkillLevel <= 0 → cannot cast (skill must be at least level 1)
            int skillLevel = forcedSkillLevel > 0 ? forcedSkillLevel : ResolveLevel(caster, skill);
            if (!bypassKnownSkillGate && skillLevel <= 0) return Reject(report, CombatCastRejectReason.SkillNotKnown, "KSkillList::CanCast: CurrentSkillLevel <= 0");
            // PC: NextCastTime > dwCurrentTime → on cooldown
            if (NextCastTime(caster.actorId, skillId) > CurrentTime) return Reject(report, CombatCastRejectReason.OnCooldown, "KSkillList::CanCast: NextCastTime > CurrentTime");

            // --- KSkill::CanCastSkill gates ---
            // PC: targetSelf overrides target to self
            // PC: targetOnly + param1 != -1 → reject (must have specific target)
            // PC: targetEnemy/Ally/Self checks via NPC_RELATION
            if (skill.targetSelf && !skill.targetEnemy && !skill.targetAlly && !skill.targetOnly)
            {
                // Pure self-buff (TargetSelf=1, no other target flags).
                // PC: KSkill forces target=caster regardless of current selection.
                target = caster;
                relation = CombatRelation.Self;
            }
            if (skill.faction != CombatFaction.None && caster.faction != skill.faction) return Reject(report, CombatCastRejectReason.FactionMismatch, "CharClass faction mismatch");
            if (!ValidateTarget(skill, target, relation)) return Reject(report, CombatCastRejectReason.InvalidTarget, "KSkill::CanCastSkill: target relation rejected");
            // PC: IsPhysical + weaponSkill → must match current weapon skill id
            if (skill.isPhysical && skill.weaponSkill && caster.currentWeaponSkillId != skill.skillId) return Reject(report, CombatCastRejectReason.WeaponSkillMismatch, "KSkill::CanCastSkill: physical weapon skill mismatch");
            // PC: EquipLimit check (skip for sandbox — no weapon system yet)
            // PC: HorseLimit: 1=forbid riding, 2=require riding
            if (skill.horseLimit == 1 && caster.rideHorse) return Reject(report, CombatCastRejectReason.HorseRestricted, "KSkill::CanCastSkill: HorseLimit=1 forbids riding");
            if (skill.horseLimit == 2 && !caster.rideHorse) return Reject(report, CombatCastRejectReason.HorseRestricted, "KSkill::CanCastSkill: HorseLimit=2 requires riding");
            // PC: targetOnly + no target → range check on NPC target

            // --- KNpc::DoSkill range check ---
            var castPoint = target != null ? target.position : targetPoint;
            if (skill.targetSelf && !skill.targetEnemy && relation == CombatRelation.Self)
                castPoint = caster.position;
            var dist = Vector2.Distance(caster.position, castPoint);
            int attackRadius = ResolveAttackRadius(skill, skillLevel);
            if (attackRadius > 0 && dist > attackRadius * RangeWorldPerPcUnit)
                return Reject(report, CombatCastRejectReason.OutOfRange, $"KNpc::DoSkill: distance {dist:F1} > AttackRadius {attackRadius}");

            // [SECT-DASH] §2.1 G1 + §2.4.2 G1: PC Melee_Jump / Melee_JumpAndAttack (KNpc.cpp line 1834-1873)
            //   cho skill Melee có meleeType ∈ {Jump, JumpAndAttack}. Player JUMP tới target trước khi attack.
            // PC NewJump: nếu dist > MIN_JUMP_RANGE (64 PC pixel), nhảy tới castPoint + clamp obstacle.
            //   Ở close range (< MIN_JUMP_RANGE), PC KNpc.cpp vẫn gọi DoJump với min dist 8 PC pixel
            //   (LUNGE_STEP 16 PC pixel) — player tiến 1 bước nhỏ để "chém" target.
            //   Nếu dist <= 8 (quá sát), skip lunge — chỉ swing melee bình thường.
            // Mobile MVP (đợt 1 Phase 3): snap caster.position tới castPoint (lerp đầy đủ là Phase 4 follow-up).
            //   Đợt 2 (Phase 3.2 — user report): thêm close-range LUNGE_STEP=16 để visual feel đúng PC.
            if (skill.meleeType == PcMeleeType.Jump || skill.meleeType == PcMeleeType.JumpAndAttack)
            {
                float minJumpRange = 64f * RangeWorldPerPcUnit;   // PC MIN_JUMP_RANGE = 64 PC pixel
                float minLungeRange = 8f * RangeWorldPerPcUnit;   // PC MIN_LUNGE_RANGE = 8 PC pixel (bỏ qua nếu dist quá gần)
                float maxLungeStep = 16f * RangeWorldPerPcUnit;   // PC LUNGE_STEP = 16 PC pixel (bước tiến tối đa ở close range)
                if (dist > minJumpRange)
                {
                    // Long range: full dash snap tới castPoint.
                    skill.dashOrigin = caster.position;
                    skill.dashVisualsEnabled = true;
                    caster.position = castPoint;
                }
                else if (dist > minLungeRange)
                {
                    // Close range: small lunge forward (PC: KNpc::DoJump bước tiến tối đa 16 PC pixel)
                    //   Player "sâu xé" tới target — visual feel giống dash rút gọn.
                    Vector2 toTarget = castPoint - caster.position;
                    Vector2 dir = toTarget.normalized;
                    float lunge = Mathf.Min(maxLungeStep, dist - minLungeRange);
                    skill.dashOrigin = caster.position;
                    skill.dashVisualsEnabled = true;
                    caster.position += dir * lunge;
                }
                // Quá gần (dist <= 8): giữ caster.position nguyên, chỉ swing melee (PC behavior).
                // Cập nhật castPoint = caster.position mới (attack happens at new pos).
                castPoint = caster.position;
            }

            // --- KNpc::Cost gate ---
            // PC: Cost(attrib_mana, GetSkillCost()) — checks & deducts mana
            var levelData = skill.GetPcLevelData(skillLevel);
            report.manaCost = GetCost(skill, levelData, caster);
            if (caster.actorId == 1) // Player actor in sandbox
            {
                report.manaCost = 0;
            }
            if (caster.currentMana < report.manaCost) return Reject(report, CombatCastRejectReason.InsufficientResource, "KNpc::Cost: insufficient mana");

            // PC: deduct mana (KNpc::Cost already checked above, now subtract)
            report.skillLevel = skillLevel;
            report.levelData = levelData;
            caster.currentMana -= report.manaCost;

            ApplyActionState(caster, skill, report);
            ApplyStates(caster, target, relation, levelData, report);
            ApplyDamage(caster, target, levelData, report);
            SpawnProjectiles(skill, caster, castPoint, grid, report, forcedSkillLevel);

            // [SECT-QUICKWIN] Gap report baocao-all-sect-skills.md §2.1 G2:
            // PC Phi Long 357 → Long Chiến Ư Dật 389 (sub-skill slash) fires
            // ở MỌI level khi missile collide (skill_collideevent[3]={{1,0},{10,0},{10,1},{20,1}}}).
            // Trước fix: chỉ fire khi L>=11 — mất slash ở L1-10 → user nói
            // "phi long tới mục tiêu ở cự ly gần, không sâu xé".
            // Sau fix: fire mọi level — sâu xé luôn damage + projectile.
            if (skillId == 357 && _catalog.Resolve(389) is { } subSkill)
            {
                var subLevelData = subSkill.GetPcLevelData(skillLevel);
                ApplyDamage(caster, target, subLevelData, report);
                SpawnProjectiles(subSkill, caster, castPoint, grid, report);
            }

            // [SECT-QUICKWIN] Gap report baocao-all-sect-skills.md §2.4-2.9 G6: event chain generalizer.
            // PC pattern: skill_startevent / skill_collideevent / skill_vanishedevent / skill_flyevent.
            //   Sau cast thành công, fire start sub-skill (nếu có). Sau missile va chạm, fire collide (đã có 1073→1072).
            //   Sau missile vanish, fire vanish (chưa có). Giữa đường bay, fire fly (chưa có).
            // Mobile MVP (đợt này): chỉ fire StartEvent inline. CollideEvent/VanishEvent cần missile runtime hook
            //   (Phase 4 follow-up — SpawnProjectiles trả child SkillDefinition, cần wrap thành ActiveSkillEffect để có
            //   lifecycle callback).
            // Sau fix: catalog đã set startSkillId cho TangMen 58 / TianRen 148 / KunLun 172 / CuiYan 102, 111.
            //   Phase 4 runtime: chỉ fire khi startSkillId > 0.
            if (skill.startSkillId > 0 && _catalog.Resolve(skill.startSkillId) is { } startSubSkill)
            {
                var startLevel = skill.startSkillLevel > 0 ? skill.startSkillLevel : skillLevel;
                var startLevelData = startSubSkill.GetPcLevelData(startLevel);
                ApplyStates(caster, caster, CombatRelation.Self, startLevelData, report);
                SpawnProjectiles(startSubSkill, caster, castPoint, grid, report, startLevel);
            }

            _nextCastTime[(caster.actorId, skillId)] = CurrentTime + Mathf.Max(0, skill.timePerCast);
            report.success = true;
            report.detail = "cast ok";
            return report;
        }

        private bool ValidateTarget(SkillDefinition skill, CombatActorState target, CombatRelation relation)
        {
            if (skill.targetOnly && target == null) return false;
            if (target == null) return !skill.targetOnly;
            if (skill.targetEnemy && relation == CombatRelation.Enemy) return true;
            if (skill.targetAlly && relation == CombatRelation.Ally) return true;
            if (skill.targetSelf && relation == CombatRelation.Self) return true;
            return !(skill.targetEnemy || skill.targetAlly || skill.targetSelf || skill.targetOnly);
        }

        private int ResolveLevel(CombatActorState caster, SkillDefinition skill)
        {
            if (skill.skillId == 389 && caster.skillLevels.TryGetValue(357, out var mainLevel))
                return mainLevel;
            if (caster.skillLevels.TryGetValue(skill.skillId, out var level))
                return level; // May be 0 = unlearned, matching PC CurrentSkillLevel
            return 0; // Not in skillLevels dict = not learned = level 0
        }

        private int GetCost(SkillDefinition skill, SkillLevelData levelData, CombatActorState caster)
        {
            if (PcKangLongYouHuiTuning.Applies(skill.skillId))
                return Mathf.Max(0, PcKangLongYouHuiTuning.AtLevel(ResolveLevel(caster, skill)).manaCost);
            var cost = levelData?.First(MagicAttributeKind.SkillCostV)?.value1 ?? skill.cost;
            return Mathf.Max(0, cost);
        }

        private static int ResolveAttackRadius(SkillDefinition skill, int skillLevel)
        {
            if (PcKangLongYouHuiTuning.Applies(skill.skillId))
                return PcKangLongYouHuiTuning.AtLevel(skillLevel).attackRadius;
            if (PcSkillTuningRegistry.HasTuning(skill.skillId, (int)skill.faction))
                return PcSkillTuningRegistry.GetSkillSpec(skill.skillId, skillLevel, (int)skill.faction).attackRadius;
            return skill.attackRadius;
        }

        private void ApplyActionState(CombatActorState caster, SkillDefinition skill, CombatCastReport report)
        {
            if (skill.skillStyle == PcSkillStyle.Melee || skill.isMelee)
                report.actionState = CombatActionState.Melee;
            else if (skill.isPhysical)
                report.actionState = CombatActionState.Attack;
            else
                report.actionState = CombatActionState.Magic;

            if (skill.skillStyle == PcSkillStyle.Melee || skill.isMelee || skill.isPhysical)
                report.totalFrames = caster.attackFrame * 100 / (100 + Mathf.Max(0, caster.attackSpeed));
            else
                report.totalFrames = caster.castFrame * 100 / (100 + Mathf.Max(0, caster.castSpeed));
        }

        private void ApplyStates(CombatActorState caster, CombatActorState target, CombatRelation relation, SkillLevelData data, CombatCastReport report)
        {
            if (data == null) return;
            CombatActorState receiver = relation == CombatRelation.Enemy ? target : caster;
            if (receiver == null) receiver = caster;
            foreach (var attr in data.state)
            {
                receiver.states[attr.kind] = attr;
                report.appliedState.Add(attr);
            }
            foreach (var attr in data.immediate)
            {
                receiver.states[attr.kind] = attr;
                report.appliedState.Add(attr);
            }
        }

        private void ApplyDamage(CombatActorState caster, CombatActorState target, SkillLevelData data, CombatCastReport report)
        {
            // [DMG-PORT-100] Port 100% từ PC KNpc::ReceiveDamage+CalcDamage.
            // PC source: KNpc.cpp:2842-2941 (ReceiveDamage) + 2445-2732 (CalcDamage).
            //
            // PC ReceiveDamage extract 14 attribs theo thứ tự cố định:
            // 1. AttackRatingP (AR) → hit/miss check
            // 2. IgnoreDefenseP (IgnoreAR) → ignore defend chance
            // 3. MagicDamage → unused (legacy)
            // 4. SeriesDamageP → ngũ hành damage bonus
            // 5. DeadlyStrikeP (DS%) → crit roll
            // 6. (reserved FS% - chưa implement)
            // 7. StealLifeP% → steal life percentage
            // 8. StealManaP% → steal mana percentage
            // 9. StealStaminaP% → steal stamina percentage
            // 10-14. PhysicsDamageV, ColdDamageV, FireDamageV, LightDamageV, PoisonDamageV
            //
            // Mỗi damage type được tính độc lập qua DamageFormulaService.Compute.
            if (target == null || data == null) return;

            // --- Pre-pass: extract combat meta from data.damage (PC ReceiveDamage:2849-2881) ---
            int attackRating = 0;
            int ignoreDefense = 0;
            int deadlyStrikePercent = 0;
            int stealLifePercent = 0;
            int stealManaPercent = 0;
            int stealStaminaPercent = 0;
            int fiveElementsDamageP = 0;

            foreach (var attr in data.damage)
            {
                switch (attr.kind)
                {
                    case MagicAttributeKind.AttackRatingP:
                        attackRating = attr.value1;
                        break;
                    case MagicAttributeKind.IgnoreDefenseP:
                        ignoreDefense = attr.value1;
                        break;
                    case MagicAttributeKind.DeadlyStrikeP:
                        deadlyStrikePercent = attr.value1;
                        break;
                    case MagicAttributeKind.StealLifeP:
                        stealLifePercent = attr.value1;
                        break;
                    case MagicAttributeKind.StealManaP:
                        stealManaPercent = attr.value1;
                        break;
                    case MagicAttributeKind.StealStaminaP:
                        stealStaminaPercent = attr.value1;
                        break;
                    case MagicAttributeKind.SeriesDamageP:
                        fiveElementsDamageP = attr.value1;
                        break;
                }
            }

            // --- Hit/Miss check (PC ReceiveDamage:2855 CheckHitTarget) ---
            // Defender value: m_CurrentDefend. Target defend từ state (AddDefenseV) hoặc level-based.
            int targetDefend = 0;
            if (target.states != null && target.states.TryGetValue(MagicAttributeKind.AddDefenseV, out var def))
                targetDefend = def.value1;
            // PC: defend mặc định dựa trên level. Mvp: dùng targetDefend (hoặc default 0).
            if (targetDefend <= 0) targetDefend = target.level * 5; // level-based fallback

            bool hit = _damage.CheckHitTarget(attackRating, targetDefend, ignoreDefense);
            if (!hit)
            {
                // Miss: thêm result hit=false, KHÔNG tính damage.
                report.damageResults.Add(new DamageResult { hit = false, type = DamageType.Physics });
                return; // PC: return FALSE → damage pipeline stops
            }

            // --- Crit roll (PC ReceiveDamage:2867 g_RandPercent) ---
            bool isDeadlyStrike = deadlyStrikePercent > 0 && _damage.RollPercent != null && _damage.RollPercent(deadlyStrikePercent);

            // --- Loop 6 damage types (PC ReceiveDamage:2884-2940) ---
            bool isMelee = report.skill != null && report.skill.meleeType != PcMeleeType.None;
            Series skillSeries = report.skill != null && report.skill.series != Series.Nil
                ? report.skill.series
                : caster.faction.GetFactionSeries();

            foreach (var attr in data.damage)
            {
                DamageType type;
                // Map PC magic attribute → DamageType (PC KNpc.cpp:2500-2580 switch).
                switch (attr.kind)
                {
                    case MagicAttributeKind.PhysicsDamageV:
                        type = DamageType.Physics; break;
                    case MagicAttributeKind.ColdDamageV:
                        type = DamageType.Cold; break;
                    case MagicAttributeKind.FireDamageV:
                        type = DamageType.Fire; break;
                    case MagicAttributeKind.LightingDamageV:
                        type = DamageType.Light; break;
                    case MagicAttributeKind.PoisonDamageV:
                        type = DamageType.Poison; break;
                    default:
                        // Non-damage attr (enhance/AR/steal/...) — không phải hit component.
                        continue;
                }

                // Extract min/max từ DamageV (value1 = min, value3 = max, hoặc value1 nếu value3=0)
                int min = attr.value1;
                int max = attr.value3 != 0 ? attr.value3 : attr.value1;
                if (max < min) max = min;

                // Add state buff damage (caster có thể cộng thêm damage từ buff)
                int extraDamageMin = 0;
                int extraDamageMax = 0;
                if (caster != null && caster.states != null)
                {
                    // Base type buff (PC m_Current*Damage.nValue)
                    AddStateDamage(caster.states, attr.kind, ref extraDamageMin, ref extraDamageMax);
                    // Add-type buff (AddPhysicsDamageP, AddFireDamageV, ...)
                    MagicAttributeKind addKind = type switch
                    {
                        DamageType.Physics => MagicAttributeKind.AddPhysicsDamageP,
                        DamageType.Fire => MagicAttributeKind.AddFireDamageV,
                        DamageType.Poison => MagicAttributeKind.AddPoisonDamageV,
                        DamageType.Cold => MagicAttributeKind.AddColdDamageV,
                        DamageType.Light => MagicAttributeKind.AddLightingDamageV,
                        _ => (MagicAttributeKind)(-1),
                    };
                    if ((int)addKind >= 0)
                        AddStateDamage(caster.states, addKind, ref extraDamageMin, ref extraDamageMax);
                }

                min += extraDamageMin;
                max += extraDamageMax;
                if (max < min) max = min;

                // Extract defender resist + armor (PC m_Current*Resist + m_*Armor.nValue[0])
                int targetResist = 0;
                int targetResistMax = 100;
                int targetArmor = 0;
                if (target.states != null)
                {
                    if (target.states.TryGetValue(MagicAttributeKind.AllResP, out var allRes))
                        targetResist += allRes.value1;
                    MagicAttributeKind resKind = type switch
                    {
                        DamageType.Physics => MagicAttributeKind.PhysicsResP,
                        DamageType.Fire => MagicAttributeKind.FireResP,
                        DamageType.Poison => MagicAttributeKind.PoisonResP,
                        DamageType.Cold => MagicAttributeKind.ColdResP,
                        DamageType.Light => MagicAttributeKind.LightingResP,
                        _ => MagicAttributeKind.AllResP,
                    };
                    if (resKind != MagicAttributeKind.AllResP && target.states.TryGetValue(resKind, out var specRes))
                        targetResist += specRes.value1;
                    // Armor pool (PC m_*Armor.nValue[0]). Map AddDefenseV → physics armor alias.
                    if (type == DamageType.Physics && target.states.TryGetValue(MagicAttributeKind.AddDefenseV, out var def))
                        targetArmor = def.value1;
                }

                // PC: KHÔNG pin rolledOverride → để DamageFormulaService random roll (KNpc.cpp:2466).
                var result = _damage.Compute(
                    new AttackerStats
                    {
                        minDamage = min,
                        maxDamage = max,
                        type = type,
                        isMelee = isMelee,
                        series = skillSeries,
                        fiveElementsDamageP = fiveElementsDamageP,
                        fiveElementsEnhance = 0,
                        // Crit visual: isDeadlyStrike → visual highlight (PC: crit KHÔNG nhân damage khi bReturn=FALSE)
                        isDeadlyStrike = isDeadlyStrike,
                        // Steal percentages (PC ReceiveDamage:2875-2881)
                        stolenLifePercent = stealLifePercent,
                        stolenManaPercent = stealManaPercent,
                        stolenStaminaPercent = stealStaminaPercent,
                    },
                    new DefenderStats
                    {
                        resist = targetResist,
                        resistMax = targetResistMax,
                        armor = targetArmor,
                        currentMana = target.currentMana,
                        series = target.faction.GetFactionSeries(),
                        fiveElementsResist = 0
                    });
                target.currentLife = Mathf.Max(0, target.currentLife - result.finalDamage);
                // Reflect damage về caster (PC KNpc.cpp:2648-2679) — áp ngay lên caster HP.
                if (isMelee && result.meleeReturnDamage > 0 && caster != null)
                    caster.currentLife = Mathf.Max(0, caster.currentLife - result.meleeReturnDamage);
                else if (!isMelee && result.rangeReturnDamage > 0 && caster != null)
                    caster.currentLife = Mathf.Max(0, caster.currentLife - result.rangeReturnDamage);
                // Steal life/mana (PC KNpc.cpp:2692-2700) — damage result đã tính stolenLife/StolenMana
                // Cần apply lên caster.
                if (caster != null && result.finalDamage > 0)
                {
                    if (result.stolenLife > 0)
                        caster.currentLife = Mathf.Min(caster.maxLife, caster.currentLife + result.stolenLife);
                    if (result.stolenMana > 0)
                        caster.currentMana = Mathf.Min(caster.maxMana, caster.currentMana + result.stolenMana);
                }
                report.damageResults.Add(result);
            }
        }

        // PC: cộng state buff vào min/max (PC KNpcAttribModify::Add*DamageV + EnhanceP).
        private static void AddStateDamage(Dictionary<MagicAttributeKind, SkillMagicAttribute> states, MagicAttributeKind kind, ref int min, ref int max)
        {
            if (states != null && states.TryGetValue(kind, out var st))
            {
                min += st.value1;
                max += st.value3 != 0 ? st.value3 : st.value1;
            }
        }

        // [SECT-QUICKWIN] Gap report baocao-all-sect-skills.md §2.2.2 G2 (TianWang multi-hit root cause):
        // Trước fix: `if (skillStyle != Missiles) return` → chặn 9 Melee skill của Thiên Vương (29/30/31/32/34/35/37/40/41)
        //   + Mọi Melee khác (Võ Đang, Côn Luân) spawn child projectile. Đây là root cause chặn:
        //   - TianWang 30 (PC childSkillNum=2, Hồi Phong Lạc Nhạn 2-hit)
        //   - TianWang 35 (PC childSkillNum=3, Dương Quan Tam Điệp 3-hit)
        //   - TianWang 41 (PC childSkillNum=4, Huyết Chiến Bát Phương 4-hit)
        //   - TianWang 40 (PC MslsForm=11 thrust + multi-thrust)
        // Sau fix: cho phép cả Missiles và Melee spawn child. Mỗi Melee skill vẫn cần set childSkillId/childSkillNum
        //   riêng (xem catalog fix đợt này cho 9 TianWang active).
        private void SpawnProjectiles(SkillDefinition skill, CombatActorState caster, Vector2 targetPoint, ObstacleGrid grid, CombatCastReport report, int forcedSkillLevel = 0)
        {
            // [SECT-QUICKWIN] §2.2.2 G2: allow cả Missiles và Melee (TianWang multi-hit pattern).
            if (skill.childSkillNum <= 0) return;
            if (skill.skillStyle != PcSkillStyle.Missiles && skill.skillStyle != PcSkillStyle.Melee) return;
            int skillLevel = forcedSkillLevel > 0 ? forcedSkillLevel : ResolveLevel(caster, skill);
            var kangLong = PcKangLongYouHuiTuning.Applies(skill.skillId) ? PcKangLongYouHuiTuning.AtLevel(skillLevel) : default;
            bool useKangLong = PcKangLongYouHuiTuning.Applies(skill.skillId);
            // [CaiBang-LuaPort 2026-06-17] PcCaiBangModTuning (stale hardcoded tables) replaced
            // by PcCaiBangLuaLevelService, which reads từ Assets/StreamingAssets/Reference/gaibang.lua
            // SKILLS dict. 357/359/1073/1074 (MOD Vietnam Cái Bang) lấy count/form/radius từ đây.
            bool useLua = PcCaiBangLuaLevelService.Applies(skill.skillId);
            // Lua returns 0 khi skill không có skill_misslenum_v/attackradius/misslesform_v
            // trong SKILLS dict → caller fall through về catalog values (skill.childSkillNum, v.v.).
            int luaCountRaw = useLua ? PcCaiBangLuaLevelService.GetMissileCount(skill.skillId, skillLevel) : 0;
            int luaAttackRadiusRaw = useLua ? PcCaiBangLuaLevelService.GetAttackRadius(skill.skillId, skillLevel) : 0;
            int luaFormInt = useLua ? PcCaiBangLuaLevelService.GetMissileForm(skill.skillId, skillLevel) : -1;
            SkillMissileForm luaForm = luaFormInt <= 0 ? skill.missileForm : (luaFormInt == 2 ? SkillMissileForm.Fan : SkillMissileForm.Single);
            int count = luaCountRaw > 0 ? luaCountRaw : (useKangLong ? Mathf.Max(1, kangLong.missileCount) : Mathf.Max(1, skill.childSkillNum));
            SkillMissileForm form = luaFormInt > 0 ? luaForm : (useKangLong ? kangLong.missileForm : skill.missileForm);
            int attackRadius = luaAttackRadiusRaw > 0 ? luaAttackRadiusRaw : (useKangLong ? kangLong.attackRadius : skill.attackRadius);
            report.childProjectileCount += count;
            for (int i = 0; i < count; i++)
            {
                int childId = skill.childSkillId != 0 ? skill.childSkillId : skill.skillId;
                var child = new SkillDefinition
                {
                    skillId = childId,
                    nameNormalized = skill.DisplayName + " child",
                    attackRadius = attackRadius,
                    missileForm = childId == 195 ? SkillMissileForm.Single : form,
                    effectResolved = skill.effectResolved,
                    effectSourceId = skill.effectSourceId,
                    // [SECT-QUICKWIN] §2.4-2.9 G6: propagate event chain anchors từ parent → child.
                    // CollideEvent/VanishedEvent/FlyEvent trên parent sẽ fire khi child missile va chạm/vanish.
                    // Phase 4 runtime cần wire projectile lifecycle callback để fire các event này.
                    collideSkillId = skill.collideSkillId,
                    collideSkillLevel = skill.collideSkillLevel,
                    vanishSkillId = skill.vanishSkillId,
                    vanishSkillLevel = skill.vanishSkillLevel,
                    flySkillId = skill.flySkillId,
                    flySkillLevel = skill.flySkillLevel,
                    flyEventTime = skill.flyEventTime,
                    startSkillId = skill.startSkillId,  // propagate để mỗi child trigger start
                    startSkillLevel = skill.startSkillLevel,
                };
                var origin = child.skillId == 195 ? targetPoint : caster.position;
                var result = _projectiles.Cast(child, origin, targetPoint, grid);
                if (!result.success)
                {
                    report.success = false;
                    report.reason = result.reason == CastRejectReason.TargetBlocked ? CombatCastRejectReason.TargetBlocked : CombatCastRejectReason.OutOfRange;
                    report.detail = result.detail;
                    return;
                }
                if (result.projectile != null)
                    report.projectiles.Add(result.projectile);
            }
        }

        private CombatCastReport Reject(CombatCastReport report, CombatCastRejectReason reason, string detail)
        {
            report.success = false;
            report.reason = reason;
            report.detail = detail;
            report.actionState = CombatActionState.Stand;
            return report;
        }
    }
}
