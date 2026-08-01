using System;
using System.Collections.Generic;
using UnityEngine;
using VLTK.Model;

namespace VLTK.Sandbox
{
    public readonly struct CombatStateSourceKey : IEquatable<CombatStateSourceKey>
    {
        public readonly int actorId;
        public readonly int skillId;

        public CombatStateSourceKey(int actorId, int skillId)
        {
            this.actorId = actorId;
            this.skillId = skillId;
        }

        public bool Equals(CombatStateSourceKey other) => actorId == other.actorId && skillId == other.skillId;
        public override bool Equals(object obj) => obj is CombatStateSourceKey other && Equals(other);
        public override int GetHashCode() => (actorId * 397) ^ skillId;
    }

    public sealed class CombatStateSourceNode
    {
        public int sourceLevel;
        public bool isPermanentPassive;
        public Dictionary<MagicAttributeKind, SkillMagicAttribute> attributes = new();
    }

    public class CombatActorState
    {
        // Skill id 0 reserves an explicit compatibility source for legacy direct `states` writes,
        // persisted flattened state, and immediate attributes.
        public const int CompatibilityStateSourceSkillId = 0;

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

        // Compatibility projection for existing combat consumers. Do not write runtime skill state here.
        public Dictionary<MagicAttributeKind, SkillMagicAttribute> states = new();
        public Dictionary<CombatStateSourceKey, CombatStateSourceNode> stateSources = new();
        private readonly Dictionary<MagicAttributeKind, SkillMagicAttribute> _lastStateProjection = new();

        public void ImportLegacyStates()
        {
            _lastStateProjection.Clear();
            SynchronizeCompatibilityStates();
            RebuildStateProjection();
        }

        /// <summary>Clear skill-owned transient state during an explicit GM runtime faction switch.</summary>
        public void ClearSkillStateSources()
        {
            states.Clear();
            stateSources.Clear();
            _lastStateProjection.Clear();
        }

        /// <summary>Materialize learned passive skills as permanent, source-owned nodes.</summary>
        public bool MaterializeLearnedPassiveStates(SkillCatalog catalog)
        {
            if (catalog == null) return false;
            SynchronizeCompatibilityStates();

            bool changed = false;
            var learnedPassives = new HashSet<int>();
            foreach (int knownId in knownSkills)
            {
                SkillDefinition passive = catalog.Resolve(knownId);
                if (passive == null || passive.skillStyle != PcSkillStyle.PassivityNpcState) continue;
                learnedPassives.Add(knownId);
                if (!skillLevels.TryGetValue(knownId, out int level) || level <= 0) level = 1;
                SkillLevelData data = passive.GetPcLevelData(level);
                var key = new CombatStateSourceKey(actorId, knownId);
                var state = data?.state;
                if (state == null || state.Count == 0)
                {
                    changed |= stateSources.Remove(key);
                    continue;
                }

                if (stateSources.TryGetValue(key, out var node) &&
                    node.isPermanentPassive &&
                    node.sourceLevel == level &&
                    SameAttributes(node.attributes, state))
                {
                    continue;
                }

                node ??= new CombatStateSourceNode();
                node.sourceLevel = level;
                node.isPermanentPassive = true;
                node.attributes.Clear();
                foreach (var attr in state)
                    node.attributes[attr.kind] = Copy(attr);
                stateSources[key] = node;
                changed = true;
            }

            var removed = new List<CombatStateSourceKey>();
            foreach (var source in stateSources)
                if (source.Value.isPermanentPassive && source.Key.actorId == actorId && !learnedPassives.Contains(source.Key.skillId))
                    removed.Add(source.Key);
            foreach (var key in removed)
                changed |= stateSources.Remove(key);

            if (changed) RebuildStateProjection();
            return changed;
        }

        public bool ApplySkillStateSource(int ownerActorId, int sourceSkillId, int sourceLevel, IEnumerable<SkillMagicAttribute> attributes, bool isPermanentPassive = false, bool forceReplace = false)
        {
            SynchronizeCompatibilityStates();
            // PC state nodes live on the receiver and deduplicate by skill id. Two casters
            // applying the same skill must therefore refresh/replace one receiver-owned node.
            var key = new CombatStateSourceKey(ownerActorId, sourceSkillId);
            if (stateSources.TryGetValue(key, out var existing) && !forceReplace && existing.sourceLevel > sourceLevel)
                return false;

            var node = existing ?? new CombatStateSourceNode();
            node.sourceLevel = sourceLevel;
            node.isPermanentPassive = isPermanentPassive;
            node.attributes.Clear();
            if (attributes != null)
            {
                foreach (var attr in attributes)
                    node.attributes[attr.kind] = Copy(attr);
            }
            if (node.attributes.Count == 0) stateSources.Remove(key);
            else stateSources[key] = node;
            RebuildStateProjection();
            return true;
        }

        public void ApplyCompatibilityState(SkillMagicAttribute attribute)
        {
            if (attribute == null) return;
            SynchronizeCompatibilityStates();
            var key = new CombatStateSourceKey(0, CompatibilityStateSourceSkillId);
            if (!stateSources.TryGetValue(key, out var node))
                stateSources[key] = node = new CombatStateSourceNode();
            node.attributes[attribute.kind] = Copy(attribute);
            RebuildStateProjection();
        }

        public void RemoveMissingPassiveSources(ISet<int> learnedPassiveSkillIds)
        {
            var removed = new List<CombatStateSourceKey>();
            foreach (var source in stateSources)
            {
                if (source.Value.isPermanentPassive && source.Key.actorId == actorId && !learnedPassiveSkillIds.Contains(source.Key.skillId))
                    removed.Add(source.Key);
            }
            foreach (var key in removed) stateSources.Remove(key);
            if (removed.Count > 0) RebuildStateProjection();
        }

        public void ExpireStateSources(int ticks)
        {
            if (ticks <= 0) return;
            SynchronizeCompatibilityStates();
            var emptySources = new List<CombatStateSourceKey>();
            foreach (var source in stateSources)
            {
                if (source.Value.isPermanentPassive) continue;
                var expired = new List<MagicAttributeKind>();
                foreach (var attribute in source.Value.attributes)
                {
                    if (attribute.Value.value2 <= 0) continue;
                    attribute.Value.value2 -= ticks;
                    if (attribute.Value.value2 <= 0) expired.Add(attribute.Key);
                }
                foreach (var kind in expired) source.Value.attributes.Remove(kind);
                if (source.Value.attributes.Count == 0) emptySources.Add(source.Key);
            }
            foreach (var key in emptySources) stateSources.Remove(key);
            RebuildStateProjection();
        }

        public void CopyNonPassiveStateProjectionTo(Dictionary<MagicAttributeKind, SkillMagicAttribute> destination)
        {
            if (destination == null) return;
            SynchronizeCompatibilityStates();
            destination.Clear();
            foreach (var source in stateSources)
            {
                if (source.Value.isPermanentPassive) continue;
                foreach (var attribute in source.Value.attributes)
                    AddToProjection(destination, attribute.Value);
            }
        }

        public int GetStateValue(MagicAttributeKind kind)
        {
            SynchronizeCompatibilityStates();
            int value = 0;
            foreach (var source in stateSources)
                if (source.Value.attributes.TryGetValue(kind, out var attribute))
                    value += attribute.value1;
            return value;
        }

        public void CopyStateSourcesFrom(CombatActorState source)
        {
            if (source == null) return;
            source.SynchronizeCompatibilityStates();
            source.RebuildStateProjection();
            stateSources.Clear();
            foreach (var sourcePair in source.stateSources)
            {
                var copy = new CombatStateSourceNode
                {
                    sourceLevel = sourcePair.Value.sourceLevel,
                    isPermanentPassive = sourcePair.Value.isPermanentPassive,
                };
                foreach (var attribute in sourcePair.Value.attributes)
                    copy.attributes[attribute.Key] = Copy(attribute.Value);
                var destinationKey = sourcePair.Key.skillId == CompatibilityStateSourceSkillId
                    ? sourcePair.Key
                    : new CombatStateSourceKey(actorId, sourcePair.Key.skillId);
                stateSources[destinationKey] = copy;
            }
            RebuildStateProjection();
        }

        public void SynchronizeCompatibilityStates()
        {
            states ??= new Dictionary<MagicAttributeKind, SkillMagicAttribute>();
            stateSources ??= new Dictionary<CombatStateSourceKey, CombatStateSourceNode>();
            var changed = new HashSet<MagicAttributeKind>(states.Keys);
            changed.UnionWith(_lastStateProjection.Keys);
            var compatibility = new CombatStateSourceKey(0, CompatibilityStateSourceSkillId);
            stateSources.TryGetValue(compatibility, out var compatibilityNode);
            foreach (var kind in changed)
            {
                states.TryGetValue(kind, out var visible);
                _lastStateProjection.TryGetValue(kind, out var projected);
                if (Same(visible, projected)) continue;
                if (visible == null)
                {
                    compatibilityNode?.attributes.Remove(kind);
                    continue;
                }
                var withoutCompatibility = BuildProjection(includePermanent: true, includeCompatibility: false);
                withoutCompatibility.TryGetValue(kind, out var sourced);
                if (compatibilityNode == null)
                    stateSources[compatibility] = compatibilityNode = new CombatStateSourceNode();
                compatibilityNode.attributes[kind] = new SkillMagicAttribute(
                    kind,
                    visible.value1 - (sourced?.value1 ?? 0),
                    visible.value2,
                    visible.value3 - (sourced?.value3 ?? 0));
            }
            if (compatibilityNode != null && compatibilityNode.attributes.Count == 0)
                stateSources.Remove(compatibility);
        }

        public void RebuildStateProjection()
        {
            var projection = BuildProjection(includePermanent: true, includeCompatibility: true);
            states.Clear();
            foreach (var pair in projection) states[pair.Key] = pair.Value;
            _lastStateProjection.Clear();
            foreach (var pair in projection) _lastStateProjection[pair.Key] = Copy(pair.Value);
        }

        private Dictionary<MagicAttributeKind, SkillMagicAttribute> BuildProjection(bool includePermanent, bool includeCompatibility)
        {
            var projection = new Dictionary<MagicAttributeKind, SkillMagicAttribute>();
            foreach (var source in stateSources)
            {
                if (!includePermanent && source.Value.isPermanentPassive) continue;
                if (!includeCompatibility && source.Key.skillId == CompatibilityStateSourceSkillId) continue;
                foreach (var attribute in source.Value.attributes)
                    AddToProjection(projection, attribute.Value);
            }
            return projection;
        }

        private static void AddToProjection(Dictionary<MagicAttributeKind, SkillMagicAttribute> projection, SkillMagicAttribute attribute)
        {
            if (!projection.TryGetValue(attribute.kind, out var aggregate))
            {
                projection[attribute.kind] = Copy(attribute);
                return;
            }
            aggregate.value1 += attribute.value1;
            aggregate.value3 += attribute.value3;
            aggregate.value2 = AggregateDuration(aggregate.value2, attribute.value2);
        }

        private static int AggregateDuration(int left, int right)
        {
            if (left < 0 || right < 0) return -1;
            return Mathf.Max(left, right);
        }

        private static SkillMagicAttribute Copy(SkillMagicAttribute attribute) =>
            attribute == null ? null : new SkillMagicAttribute(attribute.kind, attribute.value1, attribute.value2, attribute.value3);

        private static bool SameAttributes(Dictionary<MagicAttributeKind, SkillMagicAttribute> left, IReadOnlyCollection<SkillMagicAttribute> right)
        {
            if (left == null || right == null || left.Count != right.Count) return false;
            foreach (var attribute in right)
                if (attribute == null || !left.TryGetValue(attribute.kind, out var existing) || !Same(existing, attribute))
                    return false;
            return true;
        }

        private static bool Same(SkillMagicAttribute left, SkillMagicAttribute right) =>
            left == null ? right == null : right != null && left.kind == right.kind && left.value1 == right.value1 && left.value2 == right.value2 && left.value3 == right.value3;
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
        // PC addskilldamage flat %-damage bonus applied to this cast (KSkillList::GetAddSkillDamage).
        public int addSkillDamagePercent;
        public List<ProjectileInstance> projectiles = new();
        // Missile collision events are idempotent per projectile within this cast.
        public HashSet<int> resolvedCollisionProjectileIds = new();
          // PC FlyEvent repeats at each Lua interval; key by projectile + interval ordinal.
          public HashSet<(int projectileId, int eventOrdinal)> resolvedFlyEventKeys = new();
          // PC VanishEvent fires once when a projectile lifetime ends.
          public HashSet<int> resolvedVanishProjectileIds = new();
          // Ordered lifecycle child identities for deterministic runtime assertions/diagnostics.
          public List<int> resolvedLifecycleSkillIds = new();
        public Dictionary<int, int> projectileImpactSkillIds = new();
        public Dictionary<int, int> projectileImpactSkillLevels = new();
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

        // PC 打狗阵 stance ally chain: truyền callback từ GameplayLoopService để enumerate
        // ally actors trong radius. Cho phép CombatRuntimeService không phụ thuộc GameplayLoopService.
        // Delegate signature: nhận (center, radiusWu) → trả về IEnumerable<CombatActorState> trong range.
        public System.Func<Vector2, float, System.Collections.Generic.IEnumerable<CombatActorState>> AllyFinder;

        public CombatRuntimeService(SkillCatalog catalog, ProjectileService projectiles = null, DamageFormulaService damage = null)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _projectiles = projectiles ?? new ProjectileService();
            _damage = damage ?? new DamageFormulaService();
        }

        public void AdvanceTime(int ticks) => CurrentTime += Mathf.Max(0, ticks);
        public int NextCastTime(int actorId, int skillId) => _nextCastTime.TryGetValue((actorId, skillId), out var t) ? t : 0;

        /// <summary>Remove every armed recast gate owned by one actor.</summary>
        public int ResetActorCooldowns(int actorId)
        {
            var keys = new List<(int actorId, int skillId)>();
            foreach (var pair in _nextCastTime)
                if (pair.Key.actorId == actorId)
                    keys.Add(pair.Key);
            foreach (var key in keys)
                _nextCastTime.Remove(key);
            return keys.Count;
        }

        public CombatCastReport Cast(CombatActorState caster, CombatActorState target, int skillId, Vector2 targetPoint, CombatRelation relation, ObstacleGrid grid = null)
        {
            return CastInternal(caster, target, skillId, targetPoint, relation, grid, bypassKnownSkillGate: false, forcedSkillLevel: 0);
        }

        /// <summary>
        /// Resolves one PC projectile collision. The spawning skill's damage and optional
        /// collide-event sub-skill execute once per projectile instance.
        /// </summary>
        public bool TryResolveProjectileCollision(
            CombatActorState caster,
            CombatActorState target,
            CombatCastReport parentCast,
            ProjectileInstance missile,
            Vector2 collisionPoint,
            ObstacleGrid grid = null)
        {
            if (caster == null || target == null || parentCast?.skill == null || missile == null ||
                parentCast.projectiles == null || !parentCast.projectiles.Contains(missile))
                return false;

            var impactSkill = parentCast.skill;
            int impactLevel = parentCast.skillLevel;
            if (parentCast.projectileImpactSkillIds.TryGetValue(missile.instanceId, out int impactSkillId))
            {
                impactSkill = _catalog.Resolve(impactSkillId) ?? impactSkill;
                if (parentCast.projectileImpactSkillLevels.TryGetValue(missile.instanceId, out int mappedLevel))
                    impactLevel = mappedLevel;
            }
            if (!parentCast.resolvedCollisionProjectileIds.Add(missile.instanceId))
                return false;

            int addSkillDamagePercent = impactSkill.skillId == parentCast.skill.skillId ? parentCast.addSkillDamagePercent : 0;
            ApplyDamage(caster, target, impactSkill.GetPcLevelData(impactLevel), parentCast, impactSkill.isPhysical, addSkillDamagePercent);
            ProcessAutoAttackProcs(caster, target, parentCast);

            int collideSkillId = ResolveCollideSkillId(impactSkill, impactLevel);
              if (collideSkillId > 0 && ShouldTriggerCollideEvent(impactSkill, impactLevel) &&
                  _catalog.Resolve(collideSkillId) is { } collideSub)
              {
                  int collideLevel = ResolveEventSkillLevel(impactSkill, impactLevel, impactSkill.collideSkillLevel);
                  parentCast.resolvedLifecycleSkillIds.Add(collideSkillId);

                // PC KSkill::OnMissleEvent dispatches an event skill only through CastMissles.
                // ByMissle belongs to the parent impact skill and selects missile-vs-NPC launcher.
                Vector2 launcherPoint = ResolveMissileEventLauncherPoint(impactSkill, caster, collisionPoint);
                SpawnProjectiles(collideSub, caster, launcherPoint, grid, parentCast, collideLevel, launcherPoint);
            }

            return true;
        }

          /// <summary>Resolves one PC VanishEvent when a projectile expires, idempotently.</summary>
          public bool TryResolveProjectileVanish(
              CombatActorState caster,
              CombatActorState target,
              CombatCastReport parentCast,
              ProjectileInstance missile,
              Vector2 eventPoint,
              ObstacleGrid grid = null)
          {
              if (caster == null || target == null || parentCast?.skill == null || missile == null ||
                  parentCast.projectiles == null || !parentCast.projectiles.Contains(missile) ||
                  !parentCast.resolvedVanishProjectileIds.Add(missile.instanceId))
                  return false;

              var impactSkill = parentCast.skill;
              int impactLevel = parentCast.skillLevel;
              if (parentCast.projectileImpactSkillIds.TryGetValue(missile.instanceId, out int impactSkillId))
              {
                  impactSkill = _catalog.Resolve(impactSkillId) ?? impactSkill;
                  if (parentCast.projectileImpactSkillLevels.TryGetValue(missile.instanceId, out int mappedLevel))
                      impactLevel = mappedLevel;
              }

              int vanishSkillId = ResolveVanishSkillId(impactSkill, impactLevel);
              if (vanishSkillId <= 0 || !ShouldTriggerVanishEvent(impactSkill, impactLevel)) return false;
              var vanishSub = _catalog.Resolve(vanishSkillId);
              if (vanishSub == null) return false;
              int vanishLevel = ResolveEventSkillLevel(impactSkill, impactLevel, impactSkill.vanishSkillLevel);
            parentCast.resolvedLifecycleSkillIds.Add(vanishSkillId);
            Vector2 launcherPoint = ResolveMissileEventLauncherPoint(impactSkill, caster, eventPoint);
            SpawnProjectiles(vanishSub, caster, launcherPoint, grid, parentCast, vanishLevel, launcherPoint);
            return true;
          }

        /// <summary>Resolve one PC FlyEvent tick for a projectile, idempotently.</summary>
        public bool TryResolveProjectileFly(
            CombatActorState caster,
            CombatActorState target,
            CombatCastReport parentCast,
            ProjectileInstance missile,
            int eventOrdinal,
            Vector2 eventPoint,
            ObstacleGrid grid = null)
        {
            if (caster == null || target == null || parentCast?.skill == null || missile == null || eventOrdinal <= 0 ||
                parentCast.projectiles == null || !parentCast.projectiles.Contains(missile))
                return false;

            var impactSkill = parentCast.skill;
            int impactLevel = parentCast.skillLevel;
            if (parentCast.projectileImpactSkillIds.TryGetValue(missile.instanceId, out int impactSkillId))
            {
                impactSkill = _catalog.Resolve(impactSkillId) ?? impactSkill;
                if (parentCast.projectileImpactSkillLevels.TryGetValue(missile.instanceId, out int mappedLevel))
                    impactLevel = mappedLevel;
            }

            // PC slistcache/settings/skills.txt row 1073: FlyEvent=1, FlySkillId=1103,
            // FlyEventTime=1. KMissle fires this only while flying; caller supplies ordinal ticks.
            bool hasTangMenFlyOverride = PcTangMenLuaLevelService.Applies(impactSkill.skillId) &&
                                         PcTangMenLuaLevelService.HasAttribute(impactSkill.skillId, "skill_flyevent");
            bool flyEnabled = hasTangMenFlyOverride
                ? PcTangMenLuaLevelService.FlyEnabled(impactSkill.skillId, impactLevel)
                : impactSkill.flySkillId > 0 && impactSkill.flyEventTime > 0;
            if (!flyEnabled || !parentCast.resolvedFlyEventKeys.Add((missile.instanceId, eventOrdinal)))
                return false;

              var flySub = _catalog.Resolve(impactSkill.flySkillId);
              if (flySub == null) return false;
              int flyLevel = ResolveEventSkillLevel(impactSkill, impactLevel, impactSkill.flySkillLevel);
              parentCast.resolvedLifecycleSkillIds.Add(impactSkill.flySkillId);

            Vector2 launcherPoint = ResolveMissileEventLauncherPoint(impactSkill, caster, eventPoint);
            SpawnProjectiles(flySub, caster, launcherPoint, grid, parentCast, flyLevel, launcherPoint);
            return true;
        }

        public bool TryResolvePhiLongCollision(
            CombatActorState caster,
            CombatActorState target,
            CombatCastReport parentCast,
            ProjectileInstance missile,
            Vector2 collisionPoint,
            ObstacleGrid grid = null)
        {
            return parentCast?.skill?.skillId == 357 &&
                   TryResolveProjectileCollision(caster, target, parentCast, missile, collisionPoint, grid);
        }

          private static bool ShouldTriggerCollideEvent(SkillDefinition skill, int skillLevel)
          {
              if (skill == null || skill.collideSkillId <= 0) return false;
              if (PcCaiBangLuaLevelService.Applies(skill.skillId))
                  return PcCaiBangLuaLevelService.GetSingleValue(skill.skillId, skillLevel, "skill_collideevent", 1) > 0;
              if (PcTangMenLuaLevelService.Applies(skill.skillId) &&
                  PcTangMenLuaLevelService.HasAttribute(skill.skillId, "skill_collideevent"))
                  return PcTangMenLuaLevelService.CollideEnabled(skill.skillId, skillLevel) > 0;
              return true;
          }

          private static bool ShouldTriggerVanishEvent(SkillDefinition skill, int skillLevel)
          {
              if (skill == null || skill.vanishSkillId <= 0) return false;
              if (PcCaiBangLuaLevelService.Applies(skill.skillId))
                  return PcCaiBangLuaLevelService.GetSingleValue(skill.skillId, skillLevel, "skill_vanishedevent", 1) > 0;
              if (PcTangMenLuaLevelService.Applies(skill.skillId) &&
                  PcTangMenLuaLevelService.HasAttribute(skill.skillId, "skill_vanishedevent"))
                  return PcTangMenLuaLevelService.VanishEnabled(skill.skillId, skillLevel) > 0;
              return true;
          }

          private static int ResolveCollideSkillId(SkillDefinition skill, int skillLevel)
          {
              if (skill == null) return 0;
              if (PcTangMenLuaLevelService.Applies(skill.skillId))
              {
                  int luaSkillId = PcTangMenLuaLevelService.CollideSkillId(skill.skillId, skillLevel);
                  if (luaSkillId > 0)
                      return luaSkillId;
              }
              return skill.collideSkillId;
          }

          private static int ResolveVanishSkillId(SkillDefinition skill, int skillLevel)
          {
              if (skill == null) return 0;
              if (PcTangMenLuaLevelService.Applies(skill.skillId))
              {
                  int luaSkillId = PcTangMenLuaLevelService.VanishSkillId(skill.skillId, skillLevel);
                  if (luaSkillId > 0)
                      return luaSkillId;
              }
              return skill.vanishSkillId;
          }

        private static int ResolveEventSkillLevel(SkillDefinition skill, int skillLevel, int fallback)
          {
              int luaLevel = 0;
              if (PcCaiBangLuaLevelService.Applies(skill?.skillId ?? 0))
                  luaLevel = PcCaiBangLuaLevelService.GetSingleValue(skill.skillId, skillLevel, "skill_eventskilllevel", 1);
              else if (PcTangMenLuaLevelService.Applies(skill?.skillId ?? 0))
                  luaLevel = PcTangMenLuaLevelService.EventSkillLevel(skill.skillId, skillLevel);
              if (luaLevel > 0) return luaLevel;
            return fallback > 0 ? fallback : skillLevel;
        }

        /// <summary>
        /// PC KSkill::OnMissleEvent: the parent skill's ByMissle flag selects the launcher.
        /// Missile launcher uses the event point; NPC launcher uses the caster position.
        /// </summary>
        private static Vector2 ResolveMissileEventLauncherPoint(
            SkillDefinition parentSkill, CombatActorState caster, Vector2 missileEventPoint)
        {
            return parentSkill != null && parentSkill.byMissile
                ? missileEventPoint
                : caster.position;
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
            ApplyStates(caster, target, relation, skill, skillLevel, levelData, report);
            // PC KNpc::AppendSkillEffect: addskilldamage is a passive flat %-damage amplifier
            // applied to THIS cast skill's own damage, summed from learned skills whose
            // addskilldamage entries target this skill. No proc chance, no sub-skill spawn.
            int addSkillDamageP = ComputeAddSkillDamagePercent(caster, skill.skillId);
            report.addSkillDamagePercent = addSkillDamageP;
            // PC KSkill::Cast (KSkills.cpp:348) dispatches purely on SkillStyle:
            //   SKILL_SS_Missles -> CastMissles() only, NEVER applies direct damage.
            // Damage resolves later in TryResolveProjectileCollision (OnMissleEvent).
            // Non-Missile skills that do not spawn a projectile resolve damage now.
            // ByMissle selects the event launcher point (missile vs NPC), not the damage
            // gate. Event children (301/352/1098) route through the lifecycle handlers
            // below, never this direct-cast damage gate.
            int projectileCountBeforeCast = report.projectiles.Count;
            SpawnProjectiles(skill, caster, castPoint, grid, report, forcedSkillLevel);
            if (report.reason != CombatCastRejectReason.None)
                return report;
            if (report.projectiles.Count == projectileCountBeforeCast &&
                skill.skillStyle != PcSkillStyle.Missiles)
            {
                ApplyDamage(caster, target, levelData, report, skill.isPhysical, addSkillDamageP);
                ProcessAutoAttackProcs(caster, target, report);
            }

            // [CaiBang-VersionPriority 2026-06-29] Newest PC skill 124 is passive dagou_zhen
            // (SkillStyle=3, no aura propagation). Keep ally aura propagation only for actual aura skills.
            if (skill.isAura && skill.targetAlly && skill.stateSpecialId != 0)
            {
                PropagateAllyAura(caster, skill, levelData, report);
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
                ApplyStates(caster, caster, CombatRelation.Self, startSubSkill, startLevel, startLevelData, report);
                SpawnProjectiles(startSubSkill, caster, castPoint, grid, report, startLevel);
            }

            // [CaiBang-AddSkillDamage 2026-06-29] PC gaibang.lua::addskilldamageN is NOT a
            // proc that casts/spawns the listed sub-skill. PC engine KSkillList::GetAddSkillDamage
            // (KSkillList.cpp:895) + KNpc::AppendSkillEffect (KNpc.cpp:3017/3045/3119) treat it as a
            // passive flat %-damage bonus: learning skill G adds G's addskilldamageN[3]% to the
            // damage of the skill G's addskilldamageN[1] points at, WHEN that target skill is cast.
            // It is applied above via ComputeAddSkillDamagePercent → ApplyDamage. No missiles, no RNG.

            if (!skill.isAura)
                _nextCastTime[(caster.actorId, skillId)] = CurrentTime + ResolveRecastTicks(skill, caster);
            report.success = true;
            report.detail = "cast ok";
            return report;
        }

        private static int ResolveRecastTicks(SkillDefinition skill, CombatActorState caster) =>
            caster != null && caster.rideHorse ? skill.timePerCastOnHorse : skill.timePerCast;

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

        private void ApplyStates(CombatActorState caster, CombatActorState target, CombatRelation relation, SkillDefinition sourceSkill, int sourceLevel, SkillLevelData data, CombatCastReport report)
        {
            if (data == null) return;
            CombatActorState receiver = relation == CombatRelation.Self ? caster : target ?? caster;
            if (receiver == null) receiver = caster;

            // Mobile migration: a higher source level atomically replaces a lower node; a lower
            // recast is ignored. PC inspected replacement branch omits an explicit level/timer write.
            // This normalized rule is mobile-only, needed for deterministic source-node expiry.
            if (sourceSkill != null && receiver.ApplySkillStateSource(receiver.actorId, sourceSkill.skillId, sourceLevel, data.state))
            {
                foreach (var attr in data.state)
                    report.appliedState.Add(attr);
            }
            foreach (var attr in data.immediate)
            {
                // Immediate attributes historically write the public dictionary. Keep that behavior
                // through explicit compatibility ownership rather than pretending they are skill states.
                receiver.ApplyCompatibilityState(attr);
                report.appliedState.Add(attr);
            }
        }

        private void ApplyDamage(CombatActorState caster, CombatActorState target, SkillLevelData data, CombatCastReport report, bool skillIsPhysical, int addSkillDamagePercent = 0)
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
            if (target.states != null && target.states.TryGetValue(MagicAttributeKind.AddDefenseV, out var defendAttr))
                targetDefend = defendAttr.value1;
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

                // PC KNpc::AppendSkillEffect: addskilldamage + skill enhance scale the skill's OWN
                // damage component: nValue + nValue * nAddDamageP / MAX_PERCENT (MAX_PERCENT=100).
                // Applied to the skill's base value before external state buffs are added.
                if (addSkillDamagePercent != 0)
                {
                    min = min * (DamageFormulaService.MaxPercent + addSkillDamagePercent) / DamageFormulaService.MaxPercent;
                    max = max * (DamageFormulaService.MaxPercent + addSkillDamagePercent) / DamageFormulaService.MaxPercent;
                }

                // Add state buff damage (caster có thể cộng thêm damage từ buff)
                int extraDamageMin = 0;
                int extraDamageMax = 0;
                if (caster != null && caster.states != null)
                {
                    // Base type buff (PC m_Current*Damage.nValue)
                    AddStateDamage(caster.states, attr.kind, ref extraDamageMin, ref extraDamageMax);
                    // Add-type buff (AddPhysicsDamageP, AddFireDamageV, AddFireMagicV, ...).
                    // [CaiBang-FirePool 2026-07-17] PC splits the fire-add buff into two pools:
                    //   bIsPhysical  -> m_CurrentFireDamage (AddFireDamageV)
                    //   !bIsPhysical -> m_CurrentFireMagic  (AddFireMagicV)
                    // selected by the SOURCE skill's IsPhysical (PC KNpc.cpp bIsPhysical = pOrdinSkill->IsPhysical()).
                    MagicAttributeKind fireAddKind = skillIsPhysical ? MagicAttributeKind.AddFireDamageV : MagicAttributeKind.AddFireMagicV;
                    MagicAttributeKind addKind = type switch
                    {
                        DamageType.Physics => MagicAttributeKind.AddPhysicsDamageP,
                        DamageType.Fire => fireAddKind,
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
                    // PC player base caps are 75 for all five damage types (KPlayer.cpp
                    // BASE_*_RESIST_MAX). NPC-specific caps are not modeled in this actor slice.
                    int targetResistMax = target.faction == CombatFaction.None ? 100 : 75;
                  int targetArmor = 0;
                  int meleeReturnPercent = 0;
                  int rangeReturnPercent = 0;
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
                    // [CaiBang-slistcache 2026-07-15] PC yang (阳) resistance variants stack cùng base res
                    // (KMagicDesc.cpp:241/230/231). gaibang120zuzhou slistcache dùng physicsres_yan_p/fireres_yan_p.
                    if (target.states.TryGetValue(MagicAttributeKind.AllResYanP, out var allResYan))
                        targetResist += allResYan.value1;
                    MagicAttributeKind resYanKind = type switch
                    {
                        DamageType.Physics => MagicAttributeKind.PhysicsResYanP,
                        DamageType.Fire => MagicAttributeKind.FireResYanP,
                        _ => MagicAttributeKind.AllResYanP,
                    };
                      if (resYanKind != MagicAttributeKind.AllResYanP && target.states.TryGetValue(resYanKind, out var specResYan))
                          targetResist += specResYan.value1;
                      MagicAttributeKind resMaxKind = type switch
                      {
                          DamageType.Physics => MagicAttributeKind.PhysicsResMaxP,
                          DamageType.Fire => MagicAttributeKind.FireResMaxP,
                          _ => (MagicAttributeKind)(-1),
                      };
                      if ((int)resMaxKind >= 0 && target.states.TryGetValue(resMaxKind, out var resMax))
                          targetResistMax += resMax.value1;
                    // Armor pool (PC m_*Armor.nValue[0]). Map AddDefenseV → physics armor alias.
                      if (type == DamageType.Physics && target.states.TryGetValue(MagicAttributeKind.AddDefenseV, out var armorDef))
                          targetArmor = armorDef.value1;
                  }
                  // PC KNpcAttribModify::{Melee,Range}DamageReturnP accumulate into the
                  // corresponding percent pools consumed by KNpc::CalcDamage. Read nodes, not
                  // stale flattened compatibility state after source-aware save hydration.
                  meleeReturnPercent = target.GetStateValue(MagicAttributeKind.MeleeDamageReturnP);
                  rangeReturnPercent = target.GetStateValue(MagicAttributeKind.RangeDamageReturnP);

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
                          fiveElementsResist = 0,
                          meleeDmgRetPercent = meleeReturnPercent,
                          rangeDmgRetPercent = rangeReturnPercent
                      });
                  // PC KNpc.cpp:2669-2678 applies the ATTACKER's returnres_p after the
                  // defender computes reflected melee/range damage.
                  if (caster?.states != null &&
                      caster.states.TryGetValue(MagicAttributeKind.ReturnResP, out var returnRes) &&
                      returnRes.value1 != 0)
                  {
                      if (result.meleeReturnDamage != 0)
                          result.meleeReturnDamage -= result.meleeReturnDamage * returnRes.value1 / DamageFormulaService.MaxPercent;
                      if (result.rangeReturnDamage != 0)
                          result.rangeReturnDamage -= result.rangeReturnDamage * returnRes.value1 / DamageFormulaService.MaxPercent;
                  }
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

        // [CaiBang-slistcache 2026-07-15] PC gaibang.lua::gaibang120 (skill 714) 'autoattackskill'
        // passive proc config: when bearer is hit, roll proc% → cast targetSkill on attacker + cooldown.
          // autoattackskill[1]=720*256+N → target skill/level.
          // autoattackskill[3]=12*18*256+N → cooldownTicks=216 and proc%=N.
          private const int AutoAttackSkillBearerId = 714;
          private const int AutoAttackTargetSkillId = 720;
          private const int AutoAttackCooldownTicks = 12 * 18;
          // [CaiBang-slistcache 2026-07-17] PC gaibang.lua::gaibang120.autoattackskill slot[3]
          // (client_offline + server_offline + newest slistcache đều đồng ý):
          //   {{1,12*18*256+1},{20,12*18*256+10},{21,12*18*256+10}}
          //   → low byte = proc %: L1=1%, L20=10%, L21=10% (interp L15 floor=7%).
          // Trước fix (sai): {1,1},{15,5},{20,6},{21,6} — giá trị fabricated, không có trong PC Lua.
          private static readonly List<PcCaiBangLuaLevelService.LuaPoint> AutoAttackRatePoints = new()
          {
              new PcCaiBangLuaLevelService.LuaPoint(1, 1, "Line"),
              new PcCaiBangLuaLevelService.LuaPoint(20, 10, "Line"),
              new PcCaiBangLuaLevelService.LuaPoint(21, 10, "Line"),
          };

        private void ProcessAutoAttackProcs(CombatActorState attacker, CombatActorState bearer, CombatCastReport report)
        {
            // PC: proc fires only when the bearer actually took a damaging hit.
            if (bearer == null || attacker == null) return;
            if (report.damageResults == null || report.damageResults.Count == 0) return;
            bool tookHit = false;
            foreach (var dr in report.damageResults)
            {
                if (dr.hit && dr.finalDamage > 0) { tookHit = true; break; }
            }
            if (!tookHit) return;
            // Bearer must have learned the passive 714.
            if (!bearer.skillLevels.TryGetValue(AutoAttackSkillBearerId, out int lvl) || lvl <= 0) return;
              // Canonical server gaibang.lua encodes 720*256+level in slot 1 and
              // 12*18*256+rate in slot 3. The repo-local client Lua is an older 1→10
              // revision, so this runtime slice pins the authoritative server points.
              int targetSkillId = AutoAttackTargetSkillId;
              int targetSkillLevel = Mathf.Clamp(lvl, 1, 21);
              int cooldownTicks = AutoAttackCooldownTicks;
              int procPct = Mathf.FloorToInt(PcCaiBangLuaLevelService.Link(lvl, AutoAttackRatePoints));
              // PC KNpc::AutoDoSkill allows the proc only when nextCastTime < currentTime.
              if (_nextCastTime.TryGetValue((bearer.actorId, AutoAttackSkillBearerId), out int next) && CurrentTime <= next)
                  return;
              if (_damage.RollPercent != null && !_damage.RollPercent(procPct)) return;
              // PC: cast skill 720 on the attacker — apply its debuff states to attacker.
              if (_catalog.Resolve(targetSkillId) is { } debuff && debuff.GetPcLevelData(targetSkillLevel) is { } debuffData)
              {
                  ApplyStates(bearer, attacker, CombatRelation.Enemy, debuff, targetSkillLevel, debuffData, report);
              }
              _nextCastTime[(bearer.actorId, AutoAttackSkillBearerId)] = CurrentTime + cooldownTicks;
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
        private void SpawnProjectiles(SkillDefinition skill, CombatActorState caster, Vector2 targetPoint, ObstacleGrid grid, CombatCastReport report, int forcedSkillLevel = 0, Vector2? projectileOrigin = null)
        {
            // [SECT-QUICKWIN] §2.2.2 G2: allow cả Missiles và Melee (TianWang multi-hit pattern).
            if (skill.skillStyle != PcSkillStyle.Missiles && skill.skillStyle != PcSkillStyle.Melee) return;
            int skillLevel = forcedSkillLevel > 0 ? forcedSkillLevel : ResolveLevel(caster, skill);
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
            int count = luaCountRaw > 0 ? luaCountRaw : Mathf.Max(0, skill.childSkillNum);
            if (count <= 0) return;
            SkillMissileForm form = luaFormInt > 0 ? luaForm : skill.missileForm;
            int attackRadius = luaAttackRadiusRaw > 0 ? luaAttackRadiusRaw : skill.attackRadius;
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
                var origin = child.skillId == 195 ? targetPoint : projectileOrigin ?? caster.position;
                var result = _projectiles.Cast(child, origin, targetPoint, grid);
                if (!result.success)
                {
                    report.success = false;
                    report.reason = result.reason == CastRejectReason.TargetBlocked ? CombatCastRejectReason.TargetBlocked : CombatCastRejectReason.OutOfRange;
                    report.detail = result.detail;
                    return;
                }
                if (result.projectile != null)
                {
                    report.projectiles.Add(result.projectile);
                    // Every missile carries the skill that spawned it. This is essential for
                    // event skills whose ByMissle=0 but SkillStyle=Missiles (301/352/1098).
                    report.projectileImpactSkillIds[result.projectile.instanceId] = skill.skillId;
                    report.projectileImpactSkillLevels[result.projectile.instanceId] = skillLevel;
                }
            }
        }

        // PC KSkillList::GetAddSkillDamage(nSkillID) (KSkillList.cpp:895): scan ALL learned skills;
        // for each learned skill G whose addskilldamageN[1] target == the skill being cast, add
        // G's addskilldamageN[3] percent. No proc chance, no sub-skill cast. The result is a flat
        // %-damage amplifier on the cast skill's own damage (KNpc::AppendSkillEffect, MAX_PERCENT=100).
        // Slot[1] of an addskilldamage table holds the target skillId; slot[3] holds the percent.
        private static readonly (int grantSkillId, string[] slots)[] AddSkillDamageGrants =
        {
            (119, new[] { "addskilldamage1", "addskilldamage2", "addskilldamage3" }),
            (122, new[] { "addskilldamage1", "addskilldamage2", "addskilldamage3", "addskilldamage4" }),
            (125, new[] { "addskilldamage1", "addskilldamage2" }),
            (128, new[] { "addskilldamage1", "addskilldamage2", "addskilldamage3" }),
            (359, new[] { "addskilldamage1" }),
            // [CaiBang-slistcache 2026-07-17] PC gaibang.lua::feilong_zaitian:
            //   addskilldamage1 → target 1073 +1→25% (L20=25)
            //   addskilldamage2 → target 1101 +1→25% (L20=25)
            // Trước fix (sai): thiếu 357 → 1073/1101 cast ra addSkillDmg=0 thay vì PC 25%.
            (357, new[] { "addskilldamage1", "addskilldamage2" }),
        };

        /// <summary>
        /// Sum of PC addskilldamage percents granted to <paramref name="castSkillId"/> by the
        /// caster's learned skills. Mirrors PC KSkillList::GetAddSkillDamage (no RNG, no sub-skill spawn).
        /// </summary>
        private int ComputeAddSkillDamagePercent(CombatActorState caster, int castSkillId)
        {
            if (caster?.knownSkills == null || castSkillId <= 0) return 0;
            int addP = 0;
            foreach (var (grantId, slots) in AddSkillDamageGrants)
            {
                if (!caster.knownSkills.Contains(grantId)) continue;
                int grantLevel = caster.skillLevels != null && caster.skillLevels.TryGetValue(grantId, out var lv) ? lv : 0;
                if (grantLevel <= 0) continue;
                int grantMaxLevel = _catalog.Resolve(grantId)?.maxLevel ?? grantLevel;
                if (grantMaxLevel > 0) grantLevel = Mathf.Min(grantLevel, grantMaxLevel);
                foreach (var slot in slots)
                {
                    int target = PcCaiBangLuaLevelService.GetSingleValue(grantId, grantLevel, slot, 1);
                    if (target != castSkillId) continue;
                    addP += PcCaiBangLuaLevelService.GetSingleValue(grantId, grantLevel, slot, 3);
                }
            }
            return addP;
        }

        // PC 打狗阵 (124) stance ally chain: tìm allies trong attackRadius=180, apply state 44 cho mỗi ally.
        // PC 打狗阵.lua adddefense_v(level) = 30+10*level + 25 mana cost (đã apply cho caster qua ApplyStates trước).
        // Dùng cùng levelData.state để allies nhận buff cùng magnitude với caster.
        // Sau fix: 打狗阵 stance chain buff allies — đúng PC semantic.
        private void PropagateAllyAura(CombatActorState caster, SkillDefinition skill, SkillLevelData data, CombatCastReport report)
        {
            if (data == null || AllyFinder == null) return;
            Vector2 center = caster.position;
            foreach (var ally in AllyFinder(center, skill.attackRadius))
            {
                if (ally == null || ally == caster || ally.currentLife <= 0) continue;
                if (ally.ApplySkillStateSource(ally.actorId, skill.skillId, report.skillLevel, data.state))
                {
                    foreach (var attr in data.state)
                        report.appliedState.Add(attr);
                }
                foreach (var attr in data.immediate)
                {
                    ally.ApplyCompatibilityState(attr);
                    report.appliedState.Add(attr);
                }
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
