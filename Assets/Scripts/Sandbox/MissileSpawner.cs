// -----------------------------------------------------------------------------
// VLTK Mobile — ST-04.3 Missile Spawner
// Spawns projectile templates based on SkillMissileForm settings from PC source.
// Source: PC PcMissles.txt Speed, LifeTime columns.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Spawner tạo đạn (missile/projectile) dựa trên SkillMissileForm từ PcSkills.txt.
    /// PC source: KMissle::Activate / PcMissles.txt Speed, LifeTime.
    /// </summary>
    public class MissileSpawner
    {
        private readonly ProjectileService _projectileService;
        // Monotonic counter guarantees unique instance IDs even when many
        // projectiles spawn in the same frame (Fan / Surround forms). Random
        // 1000..9999 would collide; ProjectileService also uses its own
        // _nextId, but MissileSpawner writes the field directly so we own it.
        private int _nextMissileId = 1;

        /// <summary>Event kích hoạt khi đạn bắn trúng đích.</summary>
        public event Action<ProjectileInstance, CombatActorState> OnMissileHit;

        public MissileSpawner(ProjectileService projectileService)
        {
            _projectileService = projectileService ?? throw new ArgumentNullException(nameof(projectileService));
        }

        /// <summary>
        /// Tạo và bay đạn theo cấu hình của skill.
        /// Trả về danh sách đạn đã được đăng ký vào ProjectileService.
        /// </summary>
        public List<ProjectileInstance> SpawnMissiles(SkillDefinition skill, Vector2 origin, Vector2 target, int childCount = 1, float speedOverride = 0)
        {
            var spawned = new List<ProjectileInstance>();
            if (skill == null) return spawned;

            // Lấy thông số đạn từ catalog hoặc default
            float speed = speedOverride > 0 ? speedOverride : 300f; // Tốc độ đạn mặc định (PC pixel/sec)
            float duration = 1.5f; // Thời gian bay mặc định (s)

            if (skill.childSkillId > 0 && PcMissileRegistry.TryGet(skill.childSkillId, out var mDef))
            {
                if (speedOverride <= 0 && mDef.speed > 0)
                {
                    speed = mDef.speed * 18f; // PC speed is pixels per tick (18 ticks/sec)
                }
                if (mDef.lifetime > 0)
                {
                    duration = mDef.lifetime / 18f; // PC lifetime is in ticks
                }
            }

            switch (skill.missileForm)
            {
                case SkillMissileForm.Single:
                    // 1) Đạn đơn: Bay thẳng từ origin tới target
                    var p = CreateInstance(skill.skillId, origin, target, speed, duration);
                    _projectileService.Add(p);
                    spawned.Add(p);
                    break;

                case SkillMissileForm.Fan:
                    // 2) Đạn hình quạt (Fan/Spread): Bắn nhiều luồng đạn hướng về phía mục tiêu
                    int count = childCount > 0 ? childCount : 3;
                    float baseAngle = Mathf.Atan2(target.y - origin.y, target.x - origin.x);
                    float spread = 30f * Mathf.Deg2Rad; // Góc quạt 30 độ

                    for (int i = 0; i < count; i++)
                    {
                        float angle = baseAngle + (i - (count - 1) / 2f) * (spread / (count > 1 ? count - 1 : 1));
                        Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                        Vector2 fanTarget = origin + dir * skill.attackRadius;

                        var pf = CreateInstance(skill.skillId, origin, fanTarget, speed, duration);
                        _projectileService.Add(pf);
                        spawned.Add(pf);
                    }
                    break;

                case SkillMissileForm.Surround:
                    // 3) Đạn vòng tròn (Surround): Tỏa ra xung quanh caster
                    int circleCount = childCount > 0 ? childCount : 8;
                    for (int i = 0; i < circleCount; i++)
                    {
                        float angle = (i * 360f / circleCount) * Mathf.Deg2Rad;
                        Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                        Vector2 circleTarget = origin + dir * skill.attackRadius;

                        var pc = CreateInstance(skill.skillId, origin, circleTarget, speed, duration);
                        _projectileService.Add(pc);
                        spawned.Add(pc);
                    }
                    break;

                case SkillMissileForm.Chain:
                    // 4) Đạn xích (Chain): Nhảy truyền giữa các mục tiêu (giả lập bay thẳng tới đích đầu tiên)
                    var pch = CreateInstance(skill.skillId, origin, target, speed * 1.2f, duration);
                    _projectileService.Add(pch);
                    spawned.Add(pch);
                    break;

                case SkillMissileForm.None:
                default:
                    // Melee hoặc chiêu thức tức thời, không tạo đạn bay
                    break;
            }

            return spawned;
        }

        /// <summary>
        /// Update mỗi frame để kiểm tra va chạm của đạn.
        /// </summary>
        public void UpdateMissiles(float deltaTime, IEnumerable<CombatActorState> targets)
        {
            // Tiến hành bay đạn thông qua ProjectileService
            _projectileService.Step(deltaTime);

            // Kiểm tra va chạm của các đạn đang bay
            foreach (var p in _projectileService.Live)
            {
                if (!p.alive) continue;

                // Kiểm tra va chạm với danh sách targets
                foreach (var target in targets)
                {
                    if (target.actorId == SandboxManager.PlayerActorId || target.currentLife <= 0)
                        continue;

                    // Kiểm tra khoảng cách va chạm nhỏ (16 pixels)
                    float distSq = (p.position - target.position).sqrMagnitude;
                    if (distSq <= 16f * 16f)
                    {
                        p.alive = false; // Tiêu diệt đạn
                        OnMissileHit?.Invoke(p, target);
                        break;
                    }
                }
            }
        }

        // ── Helper ─────────────────────────────────────────────────────────

        private ProjectileInstance CreateInstance(int skillId, Vector2 origin, Vector2 target, float speed, float duration)
        {
            return new ProjectileInstance
            {
                instanceId = _nextMissileId++,
                skillId = skillId,
                origin = origin,
                target = target,
                position = origin,
                speed = speed,
                duration = duration,
                elapsed = 0f,
                alive = true
            };
        }
    }
}
