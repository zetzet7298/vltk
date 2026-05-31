using System;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>Outcome of a single movement step.</summary>
    public enum MoveStepResult
    {
        Idle,        // no target set
        Moving,      // advanced toward target
        Arrived,     // reached target this step
        Blocked,     // next step would enter a blocked cell
    }

    /// <summary>
    /// M2.1 — Player placeholder movement logic. Pure C# (no MonoBehaviour) so it is
    /// fully EditMode-testable. Moves a position toward a target at a configurable
    /// speed, rejecting steps that would enter an obstacle-blocked cell. A
    /// MonoBehaviour driver feeds it deltaTime and input; this class owns no Unity
    /// lifecycle.
    /// </summary>
    public class PlayerMovementService
    {
        private readonly ObstacleQueryService _obstacles;

        public Vector2 Position { get; private set; }
        public Vector2 Target { get; private set; }
        public bool HasTarget { get; private set; }

        /// <summary>AC#4 — movement speed in world units/second; updates take effect immediately.</summary>
        public float Speed { get; set; }

        /// <summary>Distance at which the target is considered reached.</summary>
        public float ArriveThreshold { get; set; } = 0.05f;

        public PlayerMovementService(Vector2 startPosition, float speed = 4f, ObstacleQueryService obstacles = null)
        {
            Position = startPosition;
            Speed = speed > 0 ? speed : 0f;
            _obstacles = obstacles;
        }

        /// <summary>AC#1 — place the player at a spawn/default position.</summary>
        public void SetPosition(Vector2 position)
        {
            Position = position;
            HasTarget = false;
        }

        /// <summary>
        /// AC#2/AC#3 — request a move toward a target world position. Returns false
        /// (and sets no target) if the destination cell itself is blocked, so the
        /// caller can route around or reject. A null obstacle service means open
        /// terrain (any target accepted).
        /// </summary>
        public bool RequestMoveTo(Vector2 target, ObstacleGrid grid)
        {
            if (_obstacles != null && grid != null && !_obstacles.CanWalkAt(target, grid))
            {
                HasTarget = false;
                SubsystemLog.Info("PlayerMove", $"Move rejected: target {target} is blocked");
                return false;
            }
            Target = target;
            HasTarget = true;
            return true;
        }

        /// <summary>
        /// Advance toward the current target by Speed*deltaTime. Stops short of any
        /// blocked cell (AC#3) and reports arrival (AC#2). Deterministic for a given
        /// deltaTime so it is unit-testable without a frame loop.
        /// </summary>
        public MoveStepResult Step(float deltaTime, ObstacleGrid grid)
        {
            if (!HasTarget) return MoveStepResult.Idle;

            var toTarget = Target - Position;
            float dist = toTarget.magnitude;
            if (dist <= ArriveThreshold)
            {
                Position = Target;
                HasTarget = false;
                return MoveStepResult.Arrived;
            }

            float stepLen = Mathf.Min(Speed * Mathf.Max(0f, deltaTime), dist);
            if (stepLen <= 0f) return MoveStepResult.Moving;

            var dir = toTarget / dist;
            var next = Position + dir * stepLen;

            if (_obstacles != null && grid != null && !_obstacles.CanWalkAt(next, grid))
            {
                // AC#3 — refuse to step into a blocked cell; hold position.
                HasTarget = false;
                SubsystemLog.Info("PlayerMove", $"Step blocked at {next}; holding {Position}");
                return MoveStepResult.Blocked;
            }

            Position = next;
            if ((Target - Position).magnitude <= ArriveThreshold)
            {
                Position = Target;
                HasTarget = false;
                return MoveStepResult.Arrived;
            }
            return MoveStepResult.Moving;
        }
    }
}
