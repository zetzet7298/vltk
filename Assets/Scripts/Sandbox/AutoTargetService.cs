// -----------------------------------------------------------------------------
// VLTK Mobile — ST-04.3 Auto-Target Service
// Target selection algorithms optimized for mobile touches.
// Source: PC auto-target filters, range checks, and obstacle queries.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service hỗ trợ tự động tìm mục tiêu (Auto-Target) cho mobile UI.
    /// Giúp người chơi tự động chọn kẻ địch tối ưu trong tầm đánh của kỹ năng.
    /// </summary>
    public static class AutoTargetService
    {
        /// <summary>
        /// Tìm mục tiêu tốt nhất cho người chơi dựa trên khoảng cách và máu.
        /// </summary>
        public static CombatActorState FindBestTarget(Vector2 playerPos, float maxRange, IEnumerable<CombatActorState> actors, ObstacleGrid grid = null)
        {
            if (actors == null) return null;

            CombatActorState bestTarget = null;
            // Range gate: squared max range là hằng số, KHÔNG dùng lại cho tiebreak.
            float maxRangeSq = maxRange * maxRange;
            // Tiebreak cùng máu: khoảng cách tới target tốt nhất hiện tại.
            float bestDistanceSq = maxRangeSq;
            int lowestHp = int.MaxValue;

            foreach (var actor in actors)
            {
                // Bỏ qua bản thân, đồng đội hoặc quái đã chết
                if (actor.actorId == SandboxManager.PlayerActorId || actor.currentLife <= 0)
                    continue;

                // Kiểm tra khoảng cách — dùng maxRangeSq cố định, KHÔNG dùng
                // bestDistanceSq (đang co lại theo từng target tốt hơn) để tránh
                // bỏ sót target ít máu hơn nhưng xa hơn vẫn nằm trong tầm.
                float distSq = (actor.position - playerPos).sqrMagnitude;
                if (distSq > maxRangeSq)
                    continue;

                // Kiểm tra vật cản nếu có ObstacleGrid
                if (grid != null && IsBlocked(playerPos, actor.position, grid))
                    continue;

                // Ưu tiên quái ít máu nhất
                if (actor.currentLife < lowestHp)
                {
                    lowestHp = actor.currentLife;
                    bestTarget = actor;
                    bestDistanceSq = distSq; // Cập nhật khoảng cách của mục tiêu tốt nhất
                }
                // Nếu máu bằng nhau, ưu tiên quái gần hơn
                else if (actor.currentLife == lowestHp && distSq < bestDistanceSq)
                {
                    bestTarget = actor;
                    bestDistanceSq = distSq;
                }
            }

            return bestTarget;
        }

        /// <summary>
        /// Xoay vòng đổi mục tiêu khi người chơi bấm nút Tab/Đổi mục tiêu.
        /// </summary>
        public static CombatActorState CycleTarget(Vector2 playerPos, float maxRange, IEnumerable<CombatActorState> actors, int currentTargetId, ObstacleGrid grid = null)
        {
            if (actors == null) return null;

            var candidates = new List<CombatActorState>();
            float maxRangeSq = maxRange * maxRange;

            foreach (var actor in actors)
            {
                if (actor.actorId == SandboxManager.PlayerActorId || actor.currentLife <= 0)
                    continue;

                float distSq = (actor.position - playerPos).sqrMagnitude;
                if (distSq <= maxRangeSq)
                {
                    if (grid == null || !IsBlocked(playerPos, actor.position, grid))
                    {
                        candidates.Add(actor);
                    }
                }
            }

            if (candidates.Count == 0) return null;

            // Sắp xếp các ứng viên theo ID tăng dần
            candidates.Sort((a, b) => a.actorId.CompareTo(b.actorId));

            // Tìm vị trí của mục tiêu hiện tại trong danh sách
            int currentIdx = candidates.FindIndex(c => c.actorId == currentTargetId);

            // Chọn mục tiêu tiếp theo trong vòng lặp
            int nextIdx = (currentIdx + 1) % candidates.Count;
            return candidates[nextIdx];
        }

        /// <summary>
        /// Kiểm tra mục tiêu hiện tại có còn hợp lệ không (còn sống, trong tầm đánh, không vật cản).
        /// </summary>
        public static bool IsTargetValid(CombatActorState target, Vector2 playerPos, float maxRange, ObstacleGrid grid = null)
        {
            if (target == null || target.currentLife <= 0)
                return false;

            float distSq = (target.position - playerPos).sqrMagnitude;
            if (distSq > maxRange * maxRange)
                return false;

            if (grid != null && IsBlocked(playerPos, target.position, grid))
                return false;

            return true;
        }

        // ── Helper ─────────────────────────────────────────────────────────

        private static bool IsBlocked(Vector2 start, Vector2 end, ObstacleGrid grid)
        {
            if (grid == null) return false;
            for (float t = 0.2f; t <= 0.8f; t += 0.3f)
            {
                Vector2 checkPoint = Vector2.Lerp(start, end, t);
                int cx = Mathf.RoundToInt(checkPoint.x);
                int cy = Mathf.RoundToInt(checkPoint.y);
                if (!grid.CanWalk(cx, cy))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
