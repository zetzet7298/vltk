// -----------------------------------------------------------------------------
// VLTK Mobile — ST-03.1 Enemy AI Service
// Implement PC AI modes from NpcS.txt AIMode column.
// Modes: Passive, Aggressive, Patrol, Guard, Flee.
// Source: PcNpcS.txt AIMode + AIParam1-9.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>
    /// PC AI mode enum từ PcNpcS.txt AIMode column.
    /// </summary>
    public enum PcAiMode
    {
        Passive     = 0,  // Đứng yên, chỉ phản kích khi bị đánh
        Aggressive  = 1,  // Đuổi theo khi thấy player trong VisionRadius
        Patrol      = 2,  // Đi tuần qua các waypoint
        Guard       = 3,  // Đứng canh, tấn công nếu player trong ActiveRadius
        Flee        = 4,  // Chạy khi HP < 50%
    }

    /// <summary>
    /// Trạng thái AI hiện tại của một enemy.
    /// </summary>
    public enum AiState
    {
        Idle,
        Wander,
        Chase,
        Attack,
        Flee,
        Return,
        Dead,
    }

    /// <summary>
    /// AI decision context cho một tick.
    /// </summary>
    public struct AiContext
    {
        public Vector2 position;
        public Vector2 playerPosition;
        public float distanceToPlayer;
        public float currentHpPercent;  // 0.0 - 1.0
        public float visionRadius;
        public float activeRadius;
        public int aiMode;
        public int[] aiParams;
        public float deltaTime;
        public NpcSkillService npcSkillService;
        public int[] npcSkillIds;
        public int currentTime;
    }

    /// <summary>
    /// Kết quả quyết định AI cho một tick.
    /// </summary>
    public struct AiDecision
    {
        public AiState state;
        public Vector2 moveTarget;
        public bool shouldAttack;
        public float moveSpeed;
        public int skillId;
        public int childSkillId;
        public int childSkillNum;
        public int attackRange;
        public int cooldownTicks;
    }

    /// <summary>
    /// Service tính toán AI decision cho enemy spawns.
    /// Pure C#, testable trong EditMode. Không phụ thuộc MonoBehaviour.
    /// Source: PcNpcS.txt AIMode + AIParam1-9.
    /// </summary>
    public static class EnemyAiService
    {
        // ── Public API ─────────────────────────────────────────────────────

        /// <summary>
        /// Tính AI decision cho một enemy trong frame hiện tại.
        /// </summary>
        public static AiDecision Tick(AiContext ctx)
        {
            var mode = (PcAiMode)ctx.aiMode;
            return mode switch
            {
                PcAiMode.Passive    => TickPassive(ctx),
                PcAiMode.Aggressive => TickAggressive(ctx),
                PcAiMode.Patrol     => TickPatrol(ctx),
                PcAiMode.Guard      => TickGuard(ctx),
                PcAiMode.Flee       => TickFlee(ctx),
                _ => TickPassive(ctx),
            };
        }

        // ── Mode implementations ───────────────────────────────────────────

        /// <summary>
        /// Mode 0 - Passive: Đứng yên, chỉ phản kích khi bị đánh.
        /// AIParam1: phản đòn thời gian (frames)
        /// </summary>
        private static AiDecision TickPassive(AiContext ctx)
        {
            return new AiDecision
            {
                state = AiState.Idle,
                moveTarget = ctx.position,
                shouldAttack = false,
                moveSpeed = 0f,
            };
        }

        /// <summary>
        /// Mode 1 - Aggressive: Đuổi theo player khi trong VisionRadius.
        /// AIParam1: chase speed multiplier
        /// AIParam2: attack range
        /// AIParam3: cooldown between attacks (frames)
        /// </summary>
        private static AiDecision TickAggressive(AiContext ctx)
        {
            float chaseSpeed = GetParam(ctx, 0, 6) * 0.1f; // AIParam1 / 10
            float attackRange = GetParam(ctx, 1, 32);       // AIParam2

            if (ctx.distanceToPlayer <= ctx.visionRadius)
            {
                if (ctx.distanceToPlayer <= attackRange)
                {
                    var skillDecision = TryNpcSkillAttack(ctx);
                    if (skillDecision.shouldAttack) return skillDecision;
                    return new AiDecision
                    {
                        state = AiState.Attack,
                        moveTarget = ctx.position,
                        shouldAttack = true,
                        moveSpeed = 0f,
                    };
                }

                // Chase player
                Vector2 dir = (ctx.playerPosition - ctx.position).normalized;
                return new AiDecision
                {
                    state = AiState.Chase,
                    moveTarget = ctx.position + dir * chaseSpeed * ctx.deltaTime * 60f,
                    shouldAttack = false,
                    moveSpeed = chaseSpeed,
                };
            }

            return new AiDecision
            {
                state = AiState.Idle,
                moveTarget = ctx.position,
                shouldAttack = false,
                moveSpeed = 0f,
            };
        }

        /// <summary>
        /// Mode 2 - Patrol: Đi tuần qua các waypoint.
        /// AIParam1: patrol speed
        /// AIParam2: waypoint count (simulated random patrol)
        /// AIParam3: idle time at waypoint (frames)
        /// </summary>
        private static AiDecision TickPatrol(AiContext ctx)
        {
            float patrolSpeed = GetParam(ctx, 0, 4) * 0.1f;

            // Random wander within active radius
            float wanderRadius = ctx.activeRadius * 0.5f;
            Vector2 wanderTarget = ctx.position + new Vector2(
                Mathf.Sin(Time.time * 0.5f) * wanderRadius,
                Mathf.Cos(Time.time * 0.3f) * wanderRadius
            );

            // If player enters vision → chase
            if (ctx.distanceToPlayer <= ctx.visionRadius)
            {
                Vector2 dir = (ctx.playerPosition - ctx.position).normalized;
                return new AiDecision
                {
                    state = AiState.Chase,
                    moveTarget = ctx.position + dir * patrolSpeed * ctx.deltaTime * 60f,
                    shouldAttack = false,
                    moveSpeed = patrolSpeed,
                };
            }

            return new AiDecision
            {
                state = AiState.Wander,
                moveTarget = wanderTarget,
                shouldAttack = false,
                moveSpeed = patrolSpeed,
            };
        }

        /// <summary>
        /// Mode 3 - Guard: Đứng canh, tấn công nếu player trong ActiveRadius.
        /// AIParam1: attack range
        /// AIParam2: reaction time (frames)
        /// </summary>
        private static AiDecision TickGuard(AiContext ctx)
        {
            float attackRange = GetParam(ctx, 0, 32);

            if (ctx.distanceToPlayer <= ctx.activeRadius)
            {
                if (ctx.distanceToPlayer <= attackRange)
                {
                    return new AiDecision
                    {
                        state = AiState.Attack,
                        moveTarget = ctx.position,
                        shouldAttack = true,
                        moveSpeed = 0f,
                    };
                }

                // Move toward player but stay in guard area
                float speed = GetParam(ctx, 1, 5) * 0.1f;
                Vector2 dir = (ctx.playerPosition - ctx.position).normalized;
                return new AiDecision
                {
                    state = AiState.Chase,
                    moveTarget = ctx.position + dir * speed * ctx.deltaTime * 60f,
                    shouldAttack = false,
                    moveSpeed = speed,
                };
            }

            return new AiDecision
            {
                state = AiState.Idle,
                moveTarget = ctx.position,
                shouldAttack = false,
                moveSpeed = 0f,
            };
        }

        /// <summary>
        /// Mode 4 - Flee: Chạy khi HP dưới 50%.
        /// AIParam1: flee speed
        /// AIParam2: flee distance
        /// </summary>
        private static AiDecision TickFlee(AiContext ctx)
        {
            float fleeSpeed = GetParam(ctx, 0, 6) * 0.1f;
            float fleeThreshold = 0.5f;

            if (ctx.currentHpPercent < fleeThreshold)
            {
                // Run away from player
                Vector2 dir = (ctx.position - ctx.playerPosition).normalized;
                return new AiDecision
                {
                    state = AiState.Flee,
                    moveTarget = ctx.position + dir * fleeSpeed * ctx.deltaTime * 60f,
                    shouldAttack = false,
                    moveSpeed = fleeSpeed,
                };
            }

            // Otherwise act aggressive
            if (ctx.distanceToPlayer <= ctx.visionRadius)
            {
                return TickAggressive(ctx);
            }

            return new AiDecision
            {
                state = AiState.Idle,
                moveTarget = ctx.position,
                shouldAttack = false,
                moveSpeed = 0f,
            };
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private static AiDecision TryNpcSkillAttack(AiContext ctx)
        {
            if (ctx.npcSkillService == null || ctx.npcSkillIds == null) return default;
            foreach (var skillId in ctx.npcSkillIds)
            {
                var plan = ctx.npcSkillService.BuildCastPlan(skillId);
                if (!plan.canCast || plan.missingScriptGuard) continue;
                if (!plan.targetEnemy || plan.targetAlly || plan.targetSelf) continue;
                if (plan.cooldownTicks > 0 && ctx.currentTime < plan.cooldownTicks) continue;
                if (plan.attackRadius > 0 && ctx.distanceToPlayer > plan.attackRadius) continue;
                return new AiDecision
                {
                    state = AiState.Attack,
                    moveTarget = ctx.position,
                    shouldAttack = true,
                    moveSpeed = 0f,
                    skillId = plan.skillId,
                    childSkillId = plan.childSkillId,
                    childSkillNum = plan.childSkillNum,
                    attackRange = plan.attackRadius,
                    cooldownTicks = plan.cooldownTicks,
                };
            }
            return default;
        }

        private static float GetParam(AiContext ctx, int index, float defaultVal)
        {
            if (ctx.aiParams == null || index >= ctx.aiParams.Length) return defaultVal;
            int val = ctx.aiParams[index];
            return val != 0 ? val : defaultVal;
        }
    }
}
