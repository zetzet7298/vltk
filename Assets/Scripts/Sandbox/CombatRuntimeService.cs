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
        public bool fightMode = true;
        public bool rideHorse;
        public int currentMana = 100;
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
            var report = new CombatCastReport { reason = CombatCastRejectReason.None };
            if (caster == null) return Reject(report, CombatCastRejectReason.InvalidTarget, "missing caster");
            if (!caster.fightMode) return Reject(report, CombatCastRejectReason.NotInFightMode, "PC KNpc::DoSkill returns when player not in fight mode");

            var skill = _catalog.Resolve(skillId);
            if (skill == null) return Reject(report, CombatCastRejectReason.NoSkill, "skill missing from catalog");
            report.skill = skill;

            if (!caster.knownSkills.Contains(skillId)) return Reject(report, CombatCastRejectReason.SkillNotKnown, "KSkillList::FindSame missing skill");
            if (NextCastTime(caster.actorId, skillId) > CurrentTime) return Reject(report, CombatCastRejectReason.OnCooldown, "KSkillList::NextCastTime > current time");
            if (skill.faction != CombatFaction.None && caster.faction != skill.faction) return Reject(report, CombatCastRejectReason.FactionMismatch, "CharClass faction mismatch");
            if (caster.level < skill.reqLevel) return Reject(report, CombatCastRejectReason.InsufficientLevel, "ReqLevel not met");
            if (skill.horseLimit == 1 && caster.rideHorse) return Reject(report, CombatCastRejectReason.HorseRestricted, "HorseLimit=1 forbids riding");
            if (skill.horseLimit == 2 && !caster.rideHorse) return Reject(report, CombatCastRejectReason.HorseRestricted, "HorseLimit=2 requires riding");
            if (skill.isPhysical && skill.weaponSkill && caster.currentWeaponSkillId != skill.skillId) return Reject(report, CombatCastRejectReason.WeaponSkillMismatch, "PC physical player skill must equal current weapon skill");
            if (!ValidateTarget(skill, target, relation)) return Reject(report, CombatCastRejectReason.InvalidTarget, "target relation rejected by PC flags");

            var castPoint = target != null ? target.position : targetPoint;
            if (skill.targetSelf && !skill.targetEnemy && relation == CombatRelation.Self)
                castPoint = caster.position;
            var dist = Vector2.Distance(caster.position, castPoint);
            if (skill.attackRadius > 0 && dist > skill.attackRadius * RangeWorldPerPcUnit)
                return Reject(report, CombatCastRejectReason.OutOfRange, $"{dist:F1}>{skill.attackRadius}");

            int level = ResolveLevel(caster, skill);
            var levelData = skill.GetPcLevelData(level);
            report.skillLevel = level;
            report.levelData = levelData;
            report.manaCost = GetCost(skill, levelData, caster);
            if (caster.currentMana < report.manaCost) return Reject(report, CombatCastRejectReason.InsufficientResource, "mana cost check failed");

            caster.currentMana -= report.manaCost;
            ApplyActionState(caster, skill, report);
            ApplyStates(caster, target, relation, levelData, report);
            ApplyDamage(caster, target, levelData, report);
            SpawnProjectiles(skill, caster, castPoint, grid, report);

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
            if (caster.skillLevels.TryGetValue(skill.skillId, out var level)) return Mathf.Max(1, level);
            return 1;
        }

        private int GetCost(SkillDefinition skill, SkillLevelData levelData, CombatActorState caster)
        {
            var cost = levelData?.First(MagicAttributeKind.SkillCostV)?.value1 ?? skill.cost;
            return Mathf.Max(0, cost);
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
            if (target == null || data == null) return;
            foreach (var attr in data.damage)
            {
                if (attr.kind != MagicAttributeKind.PhysicsDamageV && attr.kind != MagicAttributeKind.FireDamageV && attr.kind != MagicAttributeKind.PoisonDamageV)
                    continue;
                var type = attr.kind == MagicAttributeKind.FireDamageV ? DamageType.Fire : attr.kind == MagicAttributeKind.PoisonDamageV ? DamageType.Poison : DamageType.Physics;
                int min = attr.value1;
                int max = attr.value3 != 0 ? attr.value3 : attr.value1;
                var result = _damage.Compute(new AttackerStats { minDamage = min, maxDamage = max, type = type, isMelee = false }, new DefenderStats(), rolledOverride: min);
                target.currentLife = Mathf.Max(0, target.currentLife - result.finalDamage);
                report.damageResults.Add(result);
            }
        }

        private void SpawnProjectiles(SkillDefinition skill, CombatActorState caster, Vector2 targetPoint, ObstacleGrid grid, CombatCastReport report)
        {
            if (skill.skillStyle != PcSkillStyle.Missiles || skill.childSkillNum <= 0) return;
            int count = Mathf.Max(1, skill.childSkillNum);
            report.childProjectileCount = count;
            for (int i = 0; i < count; i++)
            {
                var child = new SkillDefinition
                {
                    skillId = skill.childSkillId != 0 ? skill.childSkillId : skill.skillId,
                    nameNormalized = skill.DisplayName + " child",
                    attackRadius = skill.attackRadius,
                    missileForm = skill.missileForm,
                    effectResolved = skill.effectResolved,
                    effectSourceId = skill.effectSourceId,
                };
                var result = _projectiles.Cast(child, caster.position, targetPoint, grid);
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
