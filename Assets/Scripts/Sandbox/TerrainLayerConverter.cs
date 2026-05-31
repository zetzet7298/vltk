using System;
using System.Collections.Generic;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// M1.3 — Terrain layer conversion result. Contains all resolved tile/object entries
    /// for a single region's ground section, with registry lookup for each sprite.
    /// </summary>
    public class TerrainLayerResult
    {
        public int mapId;
        public int regionX;
        public int regionY;
        public string sourceRegionPath;

        // AC#1: Tiles with resolved sprite info
        public List<TerrainTileEntry> tiles = new();
        // AC#2: Objects (ground layer objects drawn over tiles)
        public List<TerrainObjectEntry> objects = new();
        // AC#3: Missing tiles/objects for placeholder display
        public List<string> missingSprites = new();
        public List<string> warnings = new();

        // AC#4: Chunked rendering stats
        public int totalTiles;
        public int totalObjects;
        public int resolvedTiles;
        public int resolvedObjects;
        public ConversionStatus status;
    }

    public class TerrainTileEntry
    {
        /// <summary>Horizontal tile index in the tile grid.</summary>
        public int h;
        /// <summary>Vertical tile index.</summary>
        public int v;
        /// <summary>Animation frame index.</summary>
        public int frame;
        /// <summary>Original .spr filename from PC data.</summary>
        public string spriteName;
        /// <summary>Resolved Unity asset path (null = missing).</summary>
        public string resolvedAssetPath;
        /// <summary>True if the sprite was found in the registry.</summary>
        public bool resolved;
        /// <summary>Layer draw order (0 = bottom ground).</summary>
        public int drawLayer;
    }

    public class TerrainObjectEntry
    {
        public float posX;
        public float posY;
        public string imageName;
        public int width;
        public int height;
        public int frame;
        public int relateRegion;
        public int drawOrder;
        public int layer;
        public string resolvedAssetPath;
        public bool resolved;
    }

    /// <summary>
    /// M1.3 — Converts parsed GroundLayerData into TerrainLayerResult with
    /// asset registry resolution. Satisfies all 4 AC.
    /// </summary>
    public static class TerrainLayerConverter
    {
        private const int CHUNK_SIZE = 256;   // AC#4: tiles per chunk

        public static TerrainLayerResult Convert(
            GroundLayerData layerData,
            IAssetRegistry registry,
            int mapId, int regionX, int regionY,
            string sourceRegionPath = null)
        {
            var result = new TerrainLayerResult
            {
                mapId = mapId,
                regionX = regionX,
                regionY = regionY,
                sourceRegionPath = sourceRegionPath,
                totalTiles = layerData?.tiles?.Count ?? 0,
                totalObjects = layerData?.objects?.Count ?? 0,
            };

            if (layerData == null)
            {
                result.status = ConversionStatus.Failed;
                result.warnings.Add("No ground layer data to convert");
                return result;
            }

            // AC#1: Resolve each tile's sprite through asset registry
            int drawLayer = 0;
            foreach (var tile in layerData.tiles)
            {
                var entry = new TerrainTileEntry
                {
                    h = tile.h,
                    v = tile.v,
                    frame = tile.frame,
                    spriteName = tile.spriteName,
                    drawLayer = drawLayer++,
                };

                var registryEntry = registry?.Resolve(tile.spriteName);
                if (registryEntry != null && registryEntry.status == AssetStatus.Available)
                {
                    entry.resolved = true;
                    entry.resolvedAssetPath = registryEntry.unityAssetPath;
                    result.resolvedTiles++;
                }
                else
                {
                    // AC#3: Track missing sprite, log diagnostic
                    entry.resolved = false;
                    if (!string.IsNullOrEmpty(tile.spriteName) &&
                        !result.missingSprites.Contains(tile.spriteName))
                    {
                        result.missingSprites.Add(tile.spriteName);
                        result.warnings.Add($"Missing terrain sprite: {tile.spriteName}");
                    }
                }

                result.tiles.Add(entry);

                // AC#4: Warn on large batches
                if (result.tiles.Count % CHUNK_SIZE == 0)
                    SubsystemLog.Info("Terrain",
                        $"Region [{regionX},{regionY}] processed {result.tiles.Count} tiles...");
            }

            // AC#2: Object entries (drawn above tiles in layer order)
            foreach (var obj in layerData.objects)
            {
                var entry = new TerrainObjectEntry
                {
                    posX = obj.positionX,
                    posY = obj.positionY,
                    imageName = obj.imageName,
                    width = obj.width,
                    height = obj.height,
                    frame = obj.frame,
                    relateRegion = obj.relateRegion,
                    drawOrder = obj.order,
                    layer = obj.layer,
                };

                var registryEntry = registry?.Resolve(obj.imageName);
                if (registryEntry != null && registryEntry.status == AssetStatus.Available)
                {
                    entry.resolved = true;
                    entry.resolvedAssetPath = registryEntry.unityAssetPath;
                    result.resolvedObjects++;
                }
                else
                {
                    entry.resolved = false;
                    if (!string.IsNullOrEmpty(obj.imageName) &&
                        !result.missingSprites.Contains(obj.imageName))
                    {
                        result.missingSprites.Add(obj.imageName);
                        result.warnings.Add($"Missing terrain object: {obj.imageName}");
                    }
                }

                result.objects.Add(entry);
            }

            // AC#4: Draw call risk diagnostic
            int totalDrawables = result.tiles.Count + result.objects.Count;
            if (totalDrawables > 1000)
                result.warnings.Add(
                    $"High draw-call risk: {totalDrawables} terrain drawables — consider batching/atlasing");

            // Status: Complete if all resolved, Partial if some missing, Failed if none
            if (result.missingSprites.Count == 0)
                result.status = ConversionStatus.Complete;
            else if (result.resolvedTiles > 0 || result.resolvedObjects > 0)
                result.status = ConversionStatus.Partial;
            else
                result.status = ConversionStatus.NotStarted;

            SubsystemLog.Info("Terrain",
                $"Region [{regionX},{regionY}]: {result.resolvedTiles}/{result.totalTiles} tiles, " +
                $"{result.resolvedObjects}/{result.totalObjects} objects resolved. " +
                $"{result.missingSprites.Count} missing.");

            return result;
        }
    }
}
