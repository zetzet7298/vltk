using System;
using System.Collections.Generic;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>Walkability provider for pathfinding — abstracts single grid or multi-region world.</summary>
    public interface IWalkabilityProvider
    {
        /// <summary>True if the global cell can be walked into.</summary>
        bool CanWalk(int cellX, int cellY);
        /// <summary>Optional bounds for the search (inclusive min, exclusive max).</summary>
        bool InSearchBounds(int cellX, int cellY);
    }

    /// <summary>Single ObstacleGrid adapter (AC#1).</summary>
    public class GridWalkability : IWalkabilityProvider
    {
        private readonly ObstacleGrid _grid;
        public GridWalkability(ObstacleGrid grid) { _grid = grid; }
        public bool CanWalk(int cellX, int cellY) => _grid != null && _grid.CanWalk(cellX, cellY);
        public bool InSearchBounds(int cellX, int cellY)
            => _grid != null && cellX >= 0 && cellX < _grid.width && cellY >= 0 && cellY < _grid.height;
    }

    /// <summary>
    /// AC#4 — multi-region walkability: maps a global cell into the owning region's
    /// grid via a CoordinateService, so paths cross region boundaries seamlessly.
    /// </summary>
    public class RegionedWalkability : IWalkabilityProvider
    {
        private readonly CoordinateService _coords;
        private readonly Func<int, int, ObstacleGrid> _regionGridLookup;
        private readonly int _minCellX, _minCellY, _maxCellX, _maxCellY;

        public RegionedWalkability(
            CoordinateService coords,
            Func<int, int, ObstacleGrid> regionGridLookup,
            int minCellX, int minCellY, int maxCellX, int maxCellY)
        {
            _coords = coords;
            _regionGridLookup = regionGridLookup;
            _minCellX = minCellX; _minCellY = minCellY;
            _maxCellX = maxCellX; _maxCellY = maxCellY;
        }

        public bool InSearchBounds(int cellX, int cellY)
            => cellX >= _minCellX && cellX <= _maxCellX && cellY >= _minCellY && cellY <= _maxCellY;

        public bool CanWalk(int cellX, int cellY)
        {
            var region = _coords.CellToRegion(cellX, cellY);
            var grid = _regionGridLookup?.Invoke(region.x, region.y);
            if (grid == null) return false; // unloaded/missing neighbor region → blocked
            var local = _coords.CellToLocalCell(cellX, cellY);
            return grid.CanWalk(local.x, local.y);
        }
    }

    /// <summary>Result of a pathfinding request.</summary>
    public class PathResult
    {
        public bool found;
        public List<Vector2Int> cells = new();   // global cell path, start→goal
        public int expandedNodes;
        public string failureReason;
    }

    /// <summary>
    /// M2.4 — A* pathfinding prototype over converted obstacle cells. Pure C# (no
    /// MonoBehaviour) so it is fully EditMode-testable. Works against any
    /// <see cref="IWalkabilityProvider"/>, so the same algorithm serves a single
    /// region grid (AC#1) and a multi-region world (AC#4). Logs and reports failure
    /// when no path exists (AC#2); the returned cell list feeds the debug overlay
    /// (AC#3).
    /// </summary>
    public class PathfindingService
    {
        private readonly bool _allowDiagonal;
        private readonly int _maxExpansions;

        public PathfindingService(bool allowDiagonal = false, int maxExpansions = 100000)
        {
            _allowDiagonal = allowDiagonal;
            _maxExpansions = Mathf.Max(1, maxExpansions);
        }

        public PathResult FindPath(Vector2Int start, Vector2Int goal, IWalkabilityProvider world)
        {
            var result = new PathResult();
            if (world == null)
            {
                result.failureReason = "No walkability provider";
                SubsystemLog.Warn("Pathfind", result.failureReason);
                return result;
            }
            if (!world.InSearchBounds(start.x, start.y) || !world.CanWalk(start.x, start.y))
            {
                result.failureReason = $"Start {start} is blocked or out of bounds";
                SubsystemLog.Warn("Pathfind", result.failureReason);
                return result;
            }
            if (!world.InSearchBounds(goal.x, goal.y) || !world.CanWalk(goal.x, goal.y))
            {
                result.failureReason = $"Goal {goal} is blocked or out of bounds";
                SubsystemLog.Warn("Pathfind", result.failureReason);
                return result;
            }
            if (start == goal)
            {
                result.found = true;
                result.cells.Add(start);
                return result;
            }

            // Priority queue ordered by (f, h, x, y); ties broken deterministically.
            // Lazy deletion: a node may appear with multiple f-values, but a closed
            // set ensures each is expanded at most once (with its optimal gScore).
            var open = new SortedSet<(int f, int h, int x, int y)>();
            var gScore = new Dictionary<Vector2Int, int>();
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var closed = new HashSet<Vector2Int>();

            gScore[start] = 0;
            int h0 = Heuristic(start, goal);
            open.Add((h0, h0, start.x, start.y));

            while (open.Count > 0)
            {
                var current = open.Min;
                open.Remove(current);
                var cur = new Vector2Int(current.x, current.y);
                if (closed.Contains(cur)) continue; // stale duplicate entry
                closed.Add(cur);

                if (result.expandedNodes++ > _maxExpansions)
                {
                    result.failureReason = "Search budget exceeded";
                    SubsystemLog.Warn("Pathfind", result.failureReason);
                    return result;
                }

                if (cur == goal)
                {
                    Reconstruct(cameFrom, cur, result);
                    result.found = true;
                    return result;
                }

                int curG = gScore[cur];
                foreach (var n in Neighbors(cur))
                {
                    if (closed.Contains(n)) continue;
                    if (!world.InSearchBounds(n.x, n.y) || !world.CanWalk(n.x, n.y)) continue;
                    int tentative = curG + 1; // uniform step cost
                    if (gScore.TryGetValue(n, out var known) && tentative >= known) continue;

                    cameFrom[n] = cur;
                    gScore[n] = tentative;
                    int h = Heuristic(n, goal);
                    open.Add((tentative + h, h, n.x, n.y)); // lazy: old entry ignored via closed set
                }
            }

            result.failureReason = $"No path from {start} to {goal}";
            SubsystemLog.Warn("Pathfind", result.failureReason);
            return result;
        }

        private void Reconstruct(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int goal, PathResult result)
        {
            var path = new List<Vector2Int> { goal };
            var c = goal;
            while (cameFrom.TryGetValue(c, out var prev))
            {
                c = prev;
                path.Add(c);
            }
            path.Reverse();
            result.cells = path;
        }

        private int Heuristic(Vector2Int a, Vector2Int b)
        {
            int dx = Mathf.Abs(a.x - b.x);
            int dy = Mathf.Abs(a.y - b.y);
            return _allowDiagonal ? Mathf.Max(dx, dy) : dx + dy;
        }

        private IEnumerable<Vector2Int> Neighbors(Vector2Int c)
        {
            yield return new Vector2Int(c.x + 1, c.y);
            yield return new Vector2Int(c.x - 1, c.y);
            yield return new Vector2Int(c.x, c.y + 1);
            yield return new Vector2Int(c.x, c.y - 1);
            if (_allowDiagonal)
            {
                yield return new Vector2Int(c.x + 1, c.y + 1);
                yield return new Vector2Int(c.x - 1, c.y + 1);
                yield return new Vector2Int(c.x + 1, c.y - 1);
                yield return new Vector2Int(c.x - 1, c.y - 1);
            }
        }
    }
}
