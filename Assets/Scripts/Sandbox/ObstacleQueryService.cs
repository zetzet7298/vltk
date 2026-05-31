using UnityEngine;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// M1.5 AC#4 — Converts world coordinates to ObstacleGrid cell coordinates
    /// and queries walkability. Works with any grid regardless of scale.
    /// </summary>
    public class ObstacleQueryService
    {
        private readonly float _cellWidth;
        private readonly float _cellHeight;
        private readonly Vector2 _worldOrigin;

        /// <param name="cellWidth">Width of one grid cell in world units.</param>
        /// <param name="cellHeight">Height of one grid cell in world units.</param>
        /// <param name="worldOrigin">World position of cell (0,0).</param>
        public ObstacleQueryService(float cellWidth, float cellHeight, Vector2 worldOrigin)
        {
            _cellWidth = cellWidth > 0 ? cellWidth : 1f;
            _cellHeight = cellHeight > 0 ? cellHeight : 1f;
            _worldOrigin = worldOrigin;
        }

        /// <summary>
        /// M1.5 AC#4 — Resolve world position to obstacle cell and query flags.
        /// </summary>
        public ObstacleQueryResult Query(Vector2 worldPos, ObstacleGrid grid)
        {
            var result = new ObstacleQueryResult();
            if (grid == null)
            {
                result.inBounds = false;
                return result;
            }

            result.cellX = Mathf.FloorToInt((worldPos.x - _worldOrigin.x) / _cellWidth);
            result.cellY = Mathf.FloorToInt((worldPos.y - _worldOrigin.y) / _cellHeight);
            result.inBounds = result.cellX >= 0 && result.cellX < grid.width
                           && result.cellY >= 0 && result.cellY < grid.height;

            if (!result.inBounds)
            {
                // Out of bounds → treat as blocked (safe default)
                result.rawFlags = 0xFF;
                result.canWalk = false;
                result.canFly = false;
                result.canJump = false;
                return result;
            }

            result.rawFlags = grid.GetRawFlags(result.cellX, result.cellY);
            result.canWalk = grid.CanWalk(result.cellX, result.cellY);
            result.canFly = grid.CanFly(result.cellX, result.cellY);
            result.canJump = grid.CanJump(result.cellX, result.cellY);
            return result;
        }

        /// <summary>Convenience: returns false if world position is walk-blocked or out of bounds.</summary>
        public bool CanWalkAt(Vector2 worldPos, ObstacleGrid grid)
            => Query(worldPos, grid).canWalk;

        /// <summary>Convenience: returns false if world position is fly-blocked or out of bounds.</summary>
        public bool CanFlyAt(Vector2 worldPos, ObstacleGrid grid)
            => Query(worldPos, grid).canFly;
    }

    /// <summary>Result of an obstacle query at a world position.</summary>
    public struct ObstacleQueryResult
    {
        /// <summary>Grid cell X coordinate (may be out of bounds).</summary>
        public int cellX;
        /// <summary>Grid cell Y coordinate (may be out of bounds).</summary>
        public int cellY;
        /// <summary>True if cellX/Y are within the grid bounds.</summary>
        public bool inBounds;
        /// <summary>Raw flag byte from ObstacleGrid.cells.</summary>
        public byte rawFlags;
        /// <summary>False if walk-blocked or out of bounds.</summary>
        public bool canWalk;
        /// <summary>False if fly-blocked or out of bounds.</summary>
        public bool canFly;
        /// <summary>False if jump-blocked or out of bounds.</summary>
        public bool canJump;
    }
}
