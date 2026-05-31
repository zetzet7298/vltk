using System;
using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>
    /// M2.2 — Coordinate hierarchy configuration for PC↔Unity parity. Mirrors the
    /// JX Online source layout: pixel space (source), obstacle cells (32px each,
    /// 16x32 per region → 512x1024 px), and regions. All values are configurable so
    /// per-map overrides stay deterministic.
    /// </summary>
    [Serializable]
    public class CoordinateConfig
    {
        public float pixelsPerUnit = 32f;     // world units = pixels / pixelsPerUnit
        public int cellSizePixels = 32;       // one obstacle cell = 32x32 px (PC cellToWorldScale)
        public int regionCellsX = 16;         // obstacle grid is 16 wide per region
        public int regionCellsY = 32;         // obstacle grid is 32 tall per region
        public bool flipY = true;             // PC pixel Y is top-down; Unity world Y is bottom-up
        public float worldHeightPixels = 0f;  // total map height in pixels (for Y flip); 0 = no flip applied

        public int RegionWidthPixels => cellSizePixels * regionCellsX;
        public int RegionHeightPixels => cellSizePixels * regionCellsY;
    }

    /// <summary>A fully decomposed coordinate readout for the GM debug inspector (AC#2).</summary>
    public struct CoordinateInspect
    {
        public Vector2 world;
        public Vector2 pixel;
        public int globalCellX;
        public int globalCellY;
        public int regionX;
        public int regionY;
        public int localCellX;
        public int localCellY;

        public override string ToString()
            => $"world({world.x:F2},{world.y:F2}) px({pixel.x:F0},{pixel.y:F0}) " +
               $"cell({globalCellX},{globalCellY}) region({regionX},{regionY}) local({localCellX},{localCellY})";
    }

    /// <summary>
    /// M2.2 — Deterministic conversion between PC source coordinates (pixel / cell /
    /// region) and Unity world coordinates. Pure C# (no MonoBehaviour) so it is
    /// fully EditMode-testable. A single linear pixel↔world transform guarantees the
    /// conversion stays continuous across region boundaries (AC#3); region/cell
    /// decomposition is integer division of the continuous global cell index.
    /// </summary>
    public class CoordinateService
    {
        private readonly CoordinateConfig _cfg;

        public CoordinateService(CoordinateConfig config = null)
        {
            _cfg = config ?? new CoordinateConfig();
            if (_cfg.pixelsPerUnit <= 0f) _cfg.pixelsPerUnit = 1f;
            if (_cfg.cellSizePixels <= 0) _cfg.cellSizePixels = 1;
            if (_cfg.regionCellsX <= 0) _cfg.regionCellsX = 1;
            if (_cfg.regionCellsY <= 0) _cfg.regionCellsY = 1;
        }

        public CoordinateConfig Config => _cfg;

        // --- AC#1: source pixel → deterministic Unity world ---

        public Vector2 PixelToWorld(Vector2 pixel)
        {
            float wx = pixel.x / _cfg.pixelsPerUnit;
            float py = _cfg.flipY && _cfg.worldHeightPixels > 0f
                ? (_cfg.worldHeightPixels - pixel.y)
                : pixel.y;
            float wy = py / _cfg.pixelsPerUnit;
            return new Vector2(wx, wy);
        }

        public Vector2 WorldToPixel(Vector2 world)
        {
            float px = world.x * _cfg.pixelsPerUnit;
            float wy = world.y * _cfg.pixelsPerUnit;
            float py = _cfg.flipY && _cfg.worldHeightPixels > 0f
                ? (_cfg.worldHeightPixels - wy)
                : wy;
            return new Vector2(px, py);
        }

        // --- Pixel ↔ global cell ---

        public Vector2Int PixelToCell(Vector2 pixel)
            => new Vector2Int(
                Mathf.FloorToInt(pixel.x / _cfg.cellSizePixels),
                Mathf.FloorToInt(pixel.y / _cfg.cellSizePixels));

        /// <summary>Pixel coordinate of a cell's center (deterministic round-trip anchor).</summary>
        public Vector2 CellCenterToPixel(int cellX, int cellY)
            => new Vector2(
                (cellX + 0.5f) * _cfg.cellSizePixels,
                (cellY + 0.5f) * _cfg.cellSizePixels);

        // --- AC#1: source cell → world (via pixel center) ---

        public Vector2 CellCenterToWorld(int cellX, int cellY)
            => PixelToWorld(CellCenterToPixel(cellX, cellY));

        /// <summary>AC#3 — world position → global obstacle cell (continuous across regions).</summary>
        public Vector2Int WorldToCell(Vector2 world)
            => PixelToCell(WorldToPixel(world));

        // --- Region decomposition (integer division of the continuous global cell) ---

        public Vector2Int CellToRegion(int globalCellX, int globalCellY)
            => new Vector2Int(
                FloorDiv(globalCellX, _cfg.regionCellsX),
                FloorDiv(globalCellY, _cfg.regionCellsY));

        public Vector2Int CellToLocalCell(int globalCellX, int globalCellY)
            => new Vector2Int(
                Mod(globalCellX, _cfg.regionCellsX),
                Mod(globalCellY, _cfg.regionCellsY));

        public Vector2Int RegionAndLocalToGlobalCell(int regionX, int regionY, int localCellX, int localCellY)
            => new Vector2Int(
                regionX * _cfg.regionCellsX + localCellX,
                regionY * _cfg.regionCellsY + localCellY);

        /// <summary>AC#2 — full debug readout: world → pixel, global cell, region, local cell.</summary>
        public CoordinateInspect Inspect(Vector2 world)
        {
            var pixel = WorldToPixel(world);
            var cell = PixelToCell(pixel);
            var region = CellToRegion(cell.x, cell.y);
            var local = CellToLocalCell(cell.x, cell.y);
            return new CoordinateInspect
            {
                world = world,
                pixel = pixel,
                globalCellX = cell.x,
                globalCellY = cell.y,
                regionX = region.x,
                regionY = region.y,
                localCellX = local.x,
                localCellY = local.y,
            };
        }

        // Floor division / positive modulo so negative coordinates decompose correctly.
        private static int FloorDiv(int a, int b)
        {
            int q = a / b;
            if ((a % b != 0) && ((a < 0) != (b < 0))) q--;
            return q;
        }

        private static int Mod(int a, int b)
        {
            int r = a % b;
            if (r < 0) r += Math.Abs(b);
            return r;
        }
    }
}
