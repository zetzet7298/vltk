using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// M2.2 — Coordinate System Parity tests. Deterministic pixel↔world,
    /// debug decomposition (world → pixel/cell/region/local), and continuity
    /// across region boundaries (AC#1–AC#3).
    /// </summary>
    public class CoordinateServiceTests
    {
        private CoordinateService MakeService()
            => new CoordinateService(new CoordinateConfig
            {
                pixelsPerUnit = 32f,
                cellSizePixels = 32,
                regionCellsX = 16,
                regionCellsY = 32,
                flipY = false, // disable flip for straightforward parity math
                worldHeightPixels = 0f,
            });

        // --- AC#1: source pixel/cell → deterministic Unity world ---

        [Test]
        public void PixelToWorld_IsDeterministicLinearMapping()
        {
            var svc = MakeService();
            Assert.AreEqual(new Vector2(0f, 0f), svc.PixelToWorld(new Vector2(0, 0)));
            Assert.AreEqual(new Vector2(1f, 1f), svc.PixelToWorld(new Vector2(32, 32)));
            Assert.AreEqual(new Vector2(10f, 5f), svc.PixelToWorld(new Vector2(320, 160)));
        }

        [Test]
        public void PixelToWorld_RoundTrip_IsStable()
        {
            var svc = MakeService();
            var pixel = new Vector2(1234, 5678);
            var world = svc.PixelToWorld(pixel);
            var back = svc.WorldToPixel(world);
            Assert.That(back.x, Is.EqualTo(pixel.x).Within(0.001f));
            Assert.That(back.y, Is.EqualTo(pixel.y).Within(0.001f));
        }

        [Test]
        public void CellCenterToWorld_IsDeterministic()
        {
            var svc = MakeService();
            // Cell (0,0) center is pixel (16,16) → world (0.5, 0.5).
            Assert.AreEqual(new Vector2(0.5f, 0.5f), svc.CellCenterToWorld(0, 0));
            // Cell (2,3) center is pixel (80,112) → world (2.5, 3.5).
            Assert.AreEqual(new Vector2(2.5f, 3.5f), svc.CellCenterToWorld(2, 3));
        }

        [Test]
        public void CellCenterToWorld_ThenWorldToCell_RoundTrips()
        {
            var svc = MakeService();
            for (int cx = 0; cx < 40; cx += 7)
            for (int cy = 0; cy < 70; cy += 11)
            {
                var world = svc.CellCenterToWorld(cx, cy);
                var cell = svc.WorldToCell(world);
                Assert.AreEqual(new Vector2Int(cx, cy), cell, $"cell ({cx},{cy})");
            }
        }

        [Test]
        public void PixelToWorld_WithYFlip_InvertsAroundHeight()
        {
            var svc = new CoordinateService(new CoordinateConfig
            {
                pixelsPerUnit = 32f, cellSizePixels = 32,
                regionCellsX = 16, regionCellsY = 32,
                flipY = true, worldHeightPixels = 1024f,
            });
            // Pixel top (y=0) maps to world top (1024/32 = 32); pixel bottom (y=1024) → world 0.
            Assert.AreEqual(32f, svc.PixelToWorld(new Vector2(0, 0)).y, 0.001f);
            Assert.AreEqual(0f, svc.PixelToWorld(new Vector2(0, 1024)).y, 0.001f);
            // Round-trip still stable with flip.
            var px = new Vector2(640, 300);
            var back = svc.WorldToPixel(svc.PixelToWorld(px));
            Assert.That(back.y, Is.EqualTo(px.y).Within(0.001f));
        }

        // --- AC#2: world → full debug decomposition ---

        [Test]
        public void Inspect_DecomposesWorldIntoRegionAndLocalCell()
        {
            var svc = MakeService();
            // Region (1,0) local cell (0,0): global cell (16,0) center pixel (528,16) → world (16.5,0.5).
            var world = svc.CellCenterToWorld(16, 0);
            var info = svc.Inspect(world);

            Assert.AreEqual(16, info.globalCellX);
            Assert.AreEqual(0, info.globalCellY);
            Assert.AreEqual(1, info.regionX);   // 16 / 16
            Assert.AreEqual(0, info.regionY);
            Assert.AreEqual(0, info.localCellX); // 16 % 16
            Assert.AreEqual(0, info.localCellY);
        }

        [Test]
        public void Inspect_LocalCellWrapsWithinRegion()
        {
            var svc = MakeService();
            // Global cell (17, 33): region (1,1), local (1,1).
            var world = svc.CellCenterToWorld(17, 33);
            var info = svc.Inspect(world);
            Assert.AreEqual(new Vector2Int(1, 1), new Vector2Int(info.regionX, info.regionY));
            Assert.AreEqual(new Vector2Int(1, 1), new Vector2Int(info.localCellX, info.localCellY));
        }

        [Test]
        public void RegionAndLocalToGlobalCell_InvertsDecomposition()
        {
            var svc = MakeService();
            var global = svc.RegionAndLocalToGlobalCell(2, 3, 5, 7);
            Assert.AreEqual(new Vector2Int(2 * 16 + 5, 3 * 32 + 7), global);
            Assert.AreEqual(new Vector2Int(2, 3), svc.CellToRegion(global.x, global.y));
            Assert.AreEqual(new Vector2Int(5, 7), svc.CellToLocalCell(global.x, global.y));
        }

        // --- AC#3: continuity across region boundaries ---

        [Test]
        public void WorldToCell_IsContinuous_AcrossRegionBoundary()
        {
            var svc = MakeService();
            // Last cell of region 0 on X is global cell 15; first of region 1 is 16.
            var lastOfR0 = svc.CellCenterToWorld(15, 0);
            var firstOfR1 = svc.CellCenterToWorld(16, 0);

            // The world gap between adjacent cells is exactly one cell width (1 unit), no jump.
            Assert.AreEqual(1f, firstOfR1.x - lastOfR0.x, 0.001f);
            Assert.AreEqual(new Vector2Int(15, 0), svc.WorldToCell(lastOfR0));
            Assert.AreEqual(new Vector2Int(16, 0), svc.WorldToCell(firstOfR1));
            // Region flips exactly at the boundary.
            Assert.AreEqual(0, svc.CellToRegion(15, 0).x);
            Assert.AreEqual(1, svc.CellToRegion(16, 0).x);
        }

        [Test]
        public void NegativeCoordinates_DecomposeWithFloorDivision()
        {
            var svc = MakeService();
            // Global cell -1 should be region -1, local 15 (not region 0, local -1).
            Assert.AreEqual(-1, svc.CellToRegion(-1, 0).x);
            Assert.AreEqual(15, svc.CellToLocalCell(-1, 0).x);
            // -17 → region -2, local 15.
            Assert.AreEqual(-2, svc.CellToRegion(-17, 0).x);
            Assert.AreEqual(15, svc.CellToLocalCell(-17, 0).x);
        }
    }
}
