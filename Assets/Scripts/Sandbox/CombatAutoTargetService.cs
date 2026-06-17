using System;
using System.Collections.Generic;
using UnityEngine;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Finds the nearest enemy in range for auto-targeting when a skill slot is tapped.
    /// Mirrors PC KNpc::FindNearestEnemy logic used when left/right-clicking a skill.
    /// </summary>
    public class CombatAutoTargetService
    {
        /// <summary>World range multiplier for auto-target search.</summary>
        public float RangeWorldPerPcUnit { get; set; } = 1f;

        /// <summary>
        /// Find the nearest enemy within skill range from caster position.
        /// Returns null if no enemy in range.
        /// </summary>
        public CombatTargetInfo FindNearestEnemy(
            Vector2 casterPos,
            SkillDefinition skill,
            IReadOnlyList<EnemyRuntimeInfo> enemies,
            int skillLevel = 0)
        {
            if (skill == null || enemies == null || enemies.Count == 0)
                return null;

            int attackRadius;
            if (PcKangLongYouHuiTuning.Applies(skill.skillId) && skillLevel > 0)
                attackRadius = PcKangLongYouHuiTuning.AtLevel(skillLevel).attackRadius;
            else if (PcCaiBangLuaLevelService.Applies(skill.skillId) && skillLevel > 0)
            {
                int luaRadius = PcCaiBangLuaLevelService.GetAttackRadius(skill.skillId, skillLevel);
                attackRadius = luaRadius > 0 ? luaRadius : skill.attackRadius;
            }
            else
                attackRadius = skill.attackRadius;
            float maxRange = attackRadius * RangeWorldPerPcUnit;
            if (maxRange <= 0) maxRange = 500f; // PC default melee range fallback

            CombatTargetInfo best = null;
            float bestDist = float.MaxValue;

            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (!enemy.alive) continue;

                float dist = Vector2.Distance(casterPos, enemy.position);
                if (dist > maxRange) continue;

                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = new CombatTargetInfo
                    {
                        enemyIndex = i,
                        position = enemy.position,
                        distance = dist,
                        enemyId = enemy.enemyId,
                        name = enemy.displayName,
                        currentLife = enemy.currentLife,
                        maxLife = enemy.maxLife,
                        enemyBehaviour = enemy.enemyBehaviour,
                    };
                }
            }

            return best;
        }

        /// <summary>
        /// Compute the direction the caster should face to attack the target.
        /// Returns 8-way direction index (0-7) matching PC KDir values.
        /// </summary>
        public static int ComputeFacing8Way(Vector2 from, Vector2 to)
        {
            var dir = to - from;
            if (dir.sqrMagnitude < 0.01f) return 0; // default: down

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            // PC uses: 0=right, 1=up-right, 2=up, etc. (clockwise from right)
            // Convert angle to 8-way: angle 0=right, 90=up, -180/-180=left
            int sector = Mathf.RoundToInt(angle / 45f) & 7;
            // Remap to PC convention: 0=down, 1=down-right, 2=right, etc.
            // Our angle: 0=right (east), positive=up (north)
            // PC 8-way: 0=S, 1=SW, 2=W, 3=NW, 4=N, 5=NE, 6=E, 7=SE
            return RemapAngleToPC8Way(sector);
        }

        private static int RemapAngleToPC8Way(int angleSector)
        {
            // angleSector: 0=E(→), 1=NE(↗), 2=N(↑), 3=NW(↖), 4=W(←), 5=SW(↙), 6=S(↓), 7=SE(↘)
            // PC direction: 0=S, 1=SW, 2=W, 3=NW, 4=N, 5=NE, 6=E, 7=SE
            return angleSector switch
            {
                0 => 6, // E
                1 => 5, // NE
                2 => 4, // N
                3 => 3, // NW
                4 => 2, // W
                5 => 1, // SW
                6 => 0, // S
                7 => 7, // SE
                _ => 0,
            };
        }
    }

    /// <summary>Runtime info about an enemy in the scene.</summary>
    public class EnemyRuntimeInfo
    {
        public int enemyId;
        public string displayName;
        public Vector2 position;
        public bool alive = true;
        public int currentLife = 100;
        public int maxLife = 100;
        public BaLangEnemyAi enemyBehaviour;
    }

    /// <summary>Result of auto-target search.</summary>
    public class CombatTargetInfo
    {
        public int enemyIndex;
        public Vector2 position;
        public float distance;
        public int enemyId;
        public string name;
        public int currentLife;
        public int maxLife;
        public BaLangEnemyAi enemyBehaviour;
    }
}
