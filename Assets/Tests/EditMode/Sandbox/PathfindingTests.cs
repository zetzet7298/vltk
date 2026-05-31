using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// M2.4 — Pathfinding Prototype tests. A* over obstacle cells: avoids
    /// walk-blocked cells (AC#1), reports/logs failure when no path exists (AC#2),
    /// returns a cell list for the debug overlay (AC#3), and crosses region
    /// boundaries via a regioned walkability provider (AC#4).
    /// </summary>
    public class PathfindingTests
    {
        // Build a single grid with optional blocked cells (WalkBlocked flag).
        private ObstacleGrid MakeGrid(int w, int h, params Vector2Int[] blocked)
        {
            var grid = new ObstacleGrid { width = w, height = h, cellToWorldScale = 1f, cells = new byte[w * h] };
            foreach (var b in blocked)
                grid.cells[b.y * w + b.x] = ObstacleGrid.WalkBlocked;
            return grid;
        }

        // --- AC#1: path avoids walk-blocked cells ---

        [Test]
        public void FindPath_OpenGrid_ReturnsShortestManhattanLength()
        {
            var grid = MakeGrid(10, 10);
            var world = new GridWalkability(grid);
            var pf = new PathfindingService();

            var r = pf.FindPath(new Vector2Int(0, 0), new Vector2Int(3, 2), world);
            Assert.IsTrue(r.found);
            Assert.AreEqual(new Vector2Int(0, 0), r.cells[0]);
            Assert.AreEqual(new Vector2Int(3, 2), r.cells[r.cells.Count - 1]);
            // Manhattan distance 5 → 6 cells in the path.
            Assert.AreEqual(6, r.cells.Count);
        }

        [Test]
        public void FindPath_AvoidsBlockedCells()
        {
            // Wall across x=1 for y=0..2, leaving a gap at y=3.
            var grid = MakeGrid(6, 6,
                new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(1, 2));
            var world = new GridWalkability(grid);
            var pf = new PathfindingService();

            var r = pf.FindPath(new Vector2Int(0, 0), new Vector2Int(2, 0), world);
            Assert.IsTrue(r.found);
            // Every cell in the path must be walkable.
            foreach (var c in r.cells)
                Assert.IsTrue(grid.CanWalk(c.x, c.y), $"path entered blocked cell {c}");
            // Must route around the wall, so it is longer than the direct distance (2).
            Assert.Greater(r.cells.Count, 3);
        }

        // --- AC#2: no valid path → failure logged + reported ---

        [Test]
        public void FindPath_NoPath_ReportsAndLogsFailure()
        {
            // Fully wall off the goal: block all neighbors of (5,5).
            var grid = MakeGrid(6, 6,
                new Vector2Int(4, 5), new Vector2Int(5, 4));
            // (5,5) is a corner; blocking (4,5) and (5,4) isolates it.
            var world = new GridWalkability(grid);
            var pf = new PathfindingService();

            LogAssert.Expect(LogType.Warning, "[Pathfind] No path from (0, 0) to (5, 5)");
            var r = pf.FindPath(new Vector2Int(0, 0), new Vector2Int(5, 5), world);
            Assert.IsFalse(r.found);
            Assert.IsNotEmpty(r.failureReason);
        }

        [Test]
        public void FindPath_BlockedStart_FailsGracefully()
        {
            var grid = MakeGrid(5, 5, new Vector2Int(0, 0));
            var world = new GridWalkability(grid);
            var pf = new PathfindingService();

            LogAssert.Expect(LogType.Warning, "[Pathfind] Start (0, 0) is blocked or out of bounds");
            var r = pf.FindPath(new Vector2Int(0, 0), new Vector2Int(4, 4), world);
            Assert.IsFalse(r.found);
        }

        [Test]
        public void FindPath_OutOfBoundsGoal_Fails()
        {
            var grid = MakeGrid(5, 5);
            var world = new GridWalkability(grid);
            var pf = new PathfindingService();

            LogAssert.Expect(LogType.Warning, "[Pathfind] Goal (9, 9) is blocked or out of bounds");
            var r = pf.FindPath(new Vector2Int(0, 0), new Vector2Int(9, 9), world);
            Assert.IsFalse(r.found);
        }

        // --- AC#3: returns node list for overlay ---

        [Test]
        public void FindPath_SameStartAndGoal_ReturnsSingleCell()
        {
            var grid = MakeGrid(5, 5);
            var world = new GridWalkability(grid);
            var pf = new PathfindingService();

            var r = pf.FindPath(new Vector2Int(2, 2), new Vector2Int(2, 2), world);
            Assert.IsTrue(r.found);
            Assert.AreEqual(1, r.cells.Count);
            Assert.AreEqual(new Vector2Int(2, 2), r.cells[0]);
        }

        [Test]
        public void FindPath_PathIsContiguous_StepOfOne()
        {
            var grid = MakeGrid(8, 8, new Vector2Int(3, 0), new Vector2Int(3, 1), new Vector2Int(3, 2));
            var world = new GridWalkability(grid);
            var pf = new PathfindingService();

            var r = pf.FindPath(new Vector2Int(0, 0), new Vector2Int(6, 0), world);
            Assert.IsTrue(r.found);
            for (int i = 1; i < r.cells.Count; i++)
            {
                int manhattan = Mathf.Abs(r.cells[i].x - r.cells[i - 1].x)
                              + Mathf.Abs(r.cells[i].y - r.cells[i - 1].y);
                Assert.AreEqual(1, manhattan, $"non-contiguous step at index {i}");
            }
        }

        // --- AC#4: path crosses region boundaries ---

        [Test]
        public void FindPath_AcrossRegions_UsesNeighborGrids()
        {
            // Two regions side by side; each is a 16x32 open grid. The regioned
            // provider stitches them so a path can cross from region 0 into region 1.
            var coords = new CoordinateService(new CoordinateConfig
            {
                pixelsPerUnit = 32f, cellSizePixels = 32, regionCellsX = 16, regionCellsY = 32,
                flipY = false,
            });
            var region0 = MakeGrid(16, 32);
            var region1 = MakeGrid(16, 32);
            ObstacleGrid Lookup(int rx, int ry)
                => ry == 0 ? (rx == 0 ? region0 : rx == 1 ? region1 : null) : null;

            // Search bounds span both regions on X (global cells 0..31), one region tall.
            var world = new RegionedWalkability(coords, Lookup,
                minCellX: 0, minCellY: 0, maxCellX: 31, maxCellY: 31);
            var pf = new PathfindingService();

            // Start in region 0 (cell 2,5), goal in region 1 (cell 20,5).
            var r = pf.FindPath(new Vector2Int(2, 5), new Vector2Int(20, 5), world);
            Assert.IsTrue(r.found, r.failureReason);
            Assert.AreEqual(new Vector2Int(20, 5), r.cells[r.cells.Count - 1]);
            // The path must include a cell in region 1 (global x >= 16).
            bool crossed = false;
            foreach (var c in r.cells) if (c.x >= 16) { crossed = true; break; }
            Assert.IsTrue(crossed, "path never entered the neighbor region");
        }

        [Test]
        public void RegionedWalkability_MissingNeighborRegion_IsBlocked()
        {
            var coords = new CoordinateService(new CoordinateConfig
            {
                pixelsPerUnit = 32f, cellSizePixels = 32, regionCellsX = 16, regionCellsY = 32,
                flipY = false,
            });
            var region0 = MakeGrid(16, 32);
            ObstacleGrid Lookup(int rx, int ry) => (rx == 0 && ry == 0) ? region0 : null;

            var world = new RegionedWalkability(coords, Lookup, 0, 0, 31, 31);
            // Cell in region 1 has no loaded grid → not walkable.
            Assert.IsFalse(world.CanWalk(20, 5));
            // Cell in region 0 is walkable.
            Assert.IsTrue(world.CanWalk(2, 5));
        }
    }
}
