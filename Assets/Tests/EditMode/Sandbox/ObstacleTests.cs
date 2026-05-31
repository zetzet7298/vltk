using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;
using UnityEngine;

namespace VLTK.Tests.Sandbox
{
    /// <summary>M1.5 — Obstacle Grid Conversion tests.</summary>
    public class ObstacleTests
    {
        private ObstacleGrid MakeGrid(int w, int h, byte fill = 0x00)
        {
            var g = new ObstacleGrid { width = w, height = h, cells = new byte[w * h] };
            for (int i = 0; i < g.cells.Length; i++) g.cells[i] = fill;
            return g;
        }

        // ---------- AC#1: ObstacleGrid stores compact cell flags ----------

        [Test]
        public void ObstacleGrid_CellFlags_StoredCorrectly()
        {
            var grid = MakeGrid(16, 32);
            grid.cells[0] = ObstacleGrid.WalkBlocked;
            grid.cells[1] = ObstacleGrid.FlyBlocked;
            grid.cells[2] = ObstacleGrid.JumpBlocked;
            grid.cells[3] = ObstacleGrid.WalkBlocked | ObstacleGrid.FlyBlocked | ObstacleGrid.JumpBlocked;

            Assert.AreEqual(ObstacleGrid.WalkBlocked, grid.cells[0]);
            Assert.AreEqual(ObstacleGrid.FlyBlocked, grid.cells[1]);
            Assert.AreEqual(ObstacleGrid.JumpBlocked, grid.cells[2]);
            Assert.AreEqual(0x07, grid.cells[3]);
        }

        // ---------- AC#2: visual debug overlay exists (document only in EditMode) ----------

        [Test]
        public void ObstacleOverlayRenderer_TypeExists()
        {
            // AC#2: Cannot test rendering in EditMode.
            // Verify the type exists and has Show/Hide methods.
            var type = typeof(ObstacleOverlayRenderer);
            Assert.IsNotNull(type);
            Assert.IsNotNull(type.GetMethod("Show"));
            Assert.IsNotNull(type.GetMethod("Hide"));
        }

        // ---------- AC#3: movement rejected for blocked cell ----------

        [Test]
        public void ObstacleQueryService_CanWalkAt_ReturnsFalse_ForBlockedCell()
        {
            var svc = new ObstacleQueryService(32f, 32f, Vector2.zero);
            var grid = MakeGrid(16, 32);
            // Block cell (2, 3) = index 3*16+2 = 50
            grid.cells[3 * 16 + 2] = ObstacleGrid.WalkBlocked;

            // World pos (2*32+16, 3*32+16) = (80, 112) centers on cell (2,3)
            bool canWalk = svc.CanWalkAt(new Vector2(80f, 112f), grid);
            Assert.IsFalse(canWalk, "Walk-blocked cell should reject movement");
        }

        [Test]
        public void ObstacleQueryService_CanWalkAt_ReturnsTrue_ForClearCell()
        {
            var svc = new ObstacleQueryService(32f, 32f, Vector2.zero);
            var grid = MakeGrid(16, 32, 0x00);
            Assert.IsTrue(svc.CanWalkAt(new Vector2(16f, 16f), grid));
        }

        // ---------- AC#4: world coordinate → correct cell ----------

        [Test]
        public void ObstacleQueryService_Query_ConvertsWorldPosToCellCorrectly()
        {
            var origin = new Vector2(100f, 200f);
            var svc = new ObstacleQueryService(32f, 32f, origin);
            var grid = MakeGrid(16, 32);

            // World (100 + 3*32 + 10, 200 + 5*32 + 5) → cell (3, 5)
            var result = svc.Query(new Vector2(206f, 365f), grid);

            Assert.IsTrue(result.inBounds);
            Assert.AreEqual(3, result.cellX);
            Assert.AreEqual(5, result.cellY);
        }

        [Test]
        public void ObstacleQueryService_Query_OutOfBounds_IsBlocked()
        {
            var svc = new ObstacleQueryService(32f, 32f, Vector2.zero);
            var grid = MakeGrid(16, 32);

            var result = svc.Query(new Vector2(-1f, -1f), grid);

            Assert.IsFalse(result.inBounds);
            Assert.IsFalse(result.canWalk);
            Assert.IsFalse(result.canFly);
        }

        [Test]
        public void ObstacleQueryService_Query_NullGrid_ReturnsSafe()
        {
            var svc = new ObstacleQueryService(32f, 32f, Vector2.zero);
            var result = svc.Query(Vector2.zero, null);
            Assert.IsFalse(result.inBounds);
            Assert.IsFalse(result.canWalk);
        }

        [Test]
        public void ObstacleQueryService_Query_RawFlags_Match()
        {
            var svc = new ObstacleQueryService(32f, 32f, Vector2.zero);
            var grid = MakeGrid(4, 4);
            grid.cells[1 * 4 + 1] = 0x03; // walk + fly blocked

            var result = svc.Query(new Vector2(48f, 48f), grid); // cell (1,1)
            Assert.AreEqual(0x03, result.rawFlags);
            Assert.IsFalse(result.canWalk);
            Assert.IsFalse(result.canFly);
            Assert.IsTrue(result.canJump);
        }

        // ---------- AC#5: missing obstacle data → explicit default ----------

        [Test]
        public void ObstacleGridLoader_LoadDefault_ReturnsPassableGrid()
        {
            var grid = ObstacleGridLoader.LoadDefault(16, 32);
            Assert.IsNotNull(grid);
            Assert.AreEqual(16, grid.width);
            Assert.AreEqual(32, grid.height);
            Assert.IsNotNull(grid.cells);
            Assert.AreEqual(16 * 32, grid.cells.Length);

            // All cells should be passable (0x00)
            foreach (var cell in grid.cells)
                Assert.AreEqual(0, cell, "Default grid must be fully passable");
        }

        [Test]
        public void ObstacleGridLoader_LoadFromStreamingAssets_Nonexistent_ReturnsNull()
        {
            // AC#5: missing file returns null, does NOT throw
            ObstacleGrid result = null;
            Assert.DoesNotThrow(() =>
                result = ObstacleGridLoader.LoadFromStreamingAssets("nonexistent_region_xyz.dat"));
            Assert.IsNull(result, "Missing obstacle file should return null");
        }

        [Test]
        public void ObstacleGridLoader_LoadFromStreamingAssets_NullInput_ReturnsNull()
        {
            ObstacleGrid result = null;
            Assert.DoesNotThrow(() =>
                result = ObstacleGridLoader.LoadFromStreamingAssets(null));
            Assert.IsNull(result);
        }
    }
}
