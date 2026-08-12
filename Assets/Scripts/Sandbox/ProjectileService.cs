using System;
using System.Collections.Generic;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>Why a cast was rejected (AC#3 diagnostic).</summary>
    public enum CastRejectReason
    {
        None,
        OutOfRange,
        TargetBlocked,
        NoSkill,
        OnCooldown,
    }

    /// <summary>Result of a cast attempt.</summary>
    public class CastResult
    {
        public bool success;
        public CastRejectReason reason;
        public ProjectileInstance projectile;  // null on rejection or instant skill
        public string detail;
    }

    /// <summary>A live projectile/effect placeholder spawned from a skill cast.</summary>
    public class ProjectileInstance
    {
        public int instanceId;
        public int skillId;
        public Vector2 origin;
        public Vector2 target;
        public float speed;
        public string effectClipRef;   // resolved decoded sprite clip, null if missing
        public bool effectResolved;
        public Vector2 position;        // current world position
        public bool alive = true;
        public float duration;
        public float elapsed;

        /// <summary>Advance the projectile; marks it done when it reaches the target.</summary>
        public bool Step(float deltaTime)
        {
            if (!alive) return false;

            if (duration > 0f)
            {
                elapsed += deltaTime;
                if (elapsed >= duration)
                {
                    alive = false;
                    return true; // elapsed this step
                }
                return false;
            }

            var to = target - position;
            float dist = to.magnitude;
            float step = speed * Mathf.Max(0f, deltaTime);
            if (dist <= step || dist <= 0.0001f)
            {
                position = target;
                alive = false;
                return true; // arrived this step
            }
            position += (to / dist) * step;
            return false;
        }
    }

    /// <summary>
    /// M4.2 — Missile/projectile prototype. Pure C# (no MonoBehaviour) so it is fully
    /// EditMode-testable. Spawns a projectile/effect placeholder from skill data
    /// (AC#1), resolves the decoded effect sprite when available (AC#2), and rejects
    /// a cast with a diagnostic reason when the target is out of range or blocked
    /// (AC#3). A MonoBehaviour driver maps each <see cref="ProjectileInstance"/> to a
    /// scene GameObject and advances it each frame.
    /// </summary>
    public class ProjectileService
    {
        private readonly ObstacleQueryService _obstacles;
        private readonly List<ProjectileInstance> _live = new();
        private int _nextId = 1;

        /// <summary>World units per source attack-radius unit (range scaling).</summary>
        public float RangeWorldPerUnit { get; set; } = 1f;
        public float DefaultMissileSpeed { get; set; } = 12f;

        public IReadOnlyList<ProjectileInstance> Live => _live;
        public int LiveCount => _live.Count;

        public ProjectileService(ObstacleQueryService obstacles = null)
        {
            _obstacles = obstacles;
        }

        public void Add(ProjectileInstance projectile)
        {
            if (projectile != null)
            {
                _live.Add(projectile);
            }
        }

        /// <summary>
        /// AC#1/AC#2/AC#3 — attempt to cast a skill from origin toward target.
        /// Rejects when out of range or the target cell is blocked; otherwise spawns
        /// a projectile (for missile skills) or returns an instant success.
        /// </summary>
        public CastResult Cast(SkillDefinition skill, Vector2 origin, Vector2 target, ObstacleGrid grid = null)
        {
            if (skill == null)
                return new CastResult { success = false, reason = CastRejectReason.NoSkill, detail = "No skill" };

            // AC#3 — range check (attackRadius in source units → world).
            float range = skill.attackRadius * RangeWorldPerUnit;
            float dist = (target - origin).magnitude;
            if (range > 0f && dist > range + 0.999f) // PC int-distance parity: boundary equality is in range
            {
                SubsystemLog.Info("Projectile", $"Cast {skill.skillId} rejected: out of range ({dist:F1} > {range:F1})");
                return new CastResult { success = false, reason = CastRejectReason.OutOfRange,
                    detail = $"target {dist:F1} > range {range:F1}" };
            }

            // AC#3 — blocked target check.
            if (_obstacles != null && grid != null && !_obstacles.CanWalkAt(target, grid))
            {
                SubsystemLog.Info("Projectile", $"Cast {skill.skillId} rejected: target blocked");
                return new CastResult { success = false, reason = CastRejectReason.TargetBlocked,
                    detail = "target cell blocked" };
            }

            // Instant (non-missile) skill: success without a projectile.
            if (!skill.HasMissile)
                return new CastResult { success = true, reason = CastRejectReason.None, detail = "instant skill" };

            // AC#1/AC#2 — spawn a projectile placeholder.
            bool effectResolved = skill.effectResolved && skill.effectSourceId != null;
            var proj = new ProjectileInstance
            {
                instanceId = _nextId++,
                skillId = skill.skillId,
                origin = origin,
                target = target,
                speed = skill.skillId == 195 ? 0f : DefaultMissileSpeed,
                duration = skill.skillId == 195 ? (15f / 18f) : 0f,
                effectClipRef = effectResolved ? skill.effectSourceId.ToKey() : null,
                effectResolved = effectResolved,
                position = origin,
            };
            _live.Add(proj);
            SubsystemLog.Info("Projectile", $"Spawned projectile for skill {skill.skillId} (effect={(effectResolved ? proj.effectClipRef : "<missing>")})");
            return new CastResult { success = true, reason = CastRejectReason.None, projectile = proj, detail = "missile spawned" };
        }

        /// <summary>Advance all live projectiles; removes any that arrived.</summary>
        public void Step(float deltaTime)
        {
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                if (_live[i].Step(deltaTime))
                    _live.RemoveAt(i);
            }
        }

        public void Clear() => _live.Clear();
    }
}
