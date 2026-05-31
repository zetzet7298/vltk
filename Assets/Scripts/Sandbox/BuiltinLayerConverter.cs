using System;
using System.Collections.Generic;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// M1.4 — Built-in object layer conversion result.
    /// Contains all placements for a region's builtin section with
    /// resolved sprites, foreground flags, and draw-call diagnostics.
    /// </summary>
    public class BuiltinLayerResult
    {
        public int mapId;
        public int regionX;
        public int regionY;
        public string sourceRegionPath;

        // AC#1: Object placements
        public List<ObjectPlacement> placements = new();
        // AC#3: Missing sprites for placeholder diagnostic
        public List<string> missingSprites = new();
        public List<string> warnings = new();

        public int totalObjects;
        public int resolvedObjects;
        public int foregroundObjects;  // AC#2
        public ConversionStatus status;
    }

    /// <summary>
    /// M1.4 — Converts parsed BuildinObjData into BuiltinLayerResult with
    /// registry resolution. Satisfies all 4 AC.
    /// </summary>
    public static class BuiltinLayerConverter
    {
        // AC#4: Warn when object count risks too many draw calls
        private const int DRAW_CALL_WARN_THRESHOLD = 500;

        // Foreground: objects with numAbove > 0 order (above-head objects in PC source)
        // In PC: "above" objects draw over the player character
        private const uint ABOVE_HEAD_FLAG = 0x04;

        public static BuiltinLayerResult Convert(
            BuildinObjData builtinData,
            IAssetRegistry registry,
            int mapId, int regionX, int regionY,
            string sourceRegionPath = null)
        {
            var result = new BuiltinLayerResult
            {
                mapId = mapId,
                regionX = regionX,
                regionY = regionY,
                sourceRegionPath = sourceRegionPath,
                totalObjects = (int)(builtinData?.totalObjects ?? 0),
            };

            if (builtinData == null || builtinData.objects == null)
            {
                result.status = ConversionStatus.Failed;
                result.warnings.Add("No builtin object data to convert");
                return result;
            }

            int index = 0;
            foreach (var obj in builtinData.objects)
            {
                // AC#2: foreground = "above head" objects that draw over the player
                bool isForeground = (obj.props & ABOVE_HEAD_FLAG) != 0
                    || obj.order > builtinData.maxAboveHeadOrder;

                if (isForeground) result.foregroundObjects++;

                var placement = new ObjectPlacement
                {
                    placementIndex = index++,
                    spriteId = 0,  // uid not available from section directly
                    spritePath = obj.imageName,
                    posX = obj.imgX1,
                    posY = obj.imgY1,
                    layer = 0,
                    zOrder = obj.order,
                    flags = (int)obj.props,
                    isForeground = isForeground,
                };

                // AC#1: resolve through registry
                var registryEntry = registry?.Resolve(obj.imageName);
                if (registryEntry != null && registryEntry.status == AssetStatus.Available)
                {
                    placement.spriteMissing = false;
                    result.resolvedObjects++;
                }
                else
                {
                    // AC#3: mark missing, add diagnostic
                    placement.spriteMissing = true;
                    if (!string.IsNullOrEmpty(obj.imageName) &&
                        !result.missingSprites.Contains(obj.imageName))
                    {
                        result.missingSprites.Add(obj.imageName);
                        result.warnings.Add($"Missing builtin sprite: {obj.imageName}");
                    }
                }

                result.placements.Add(placement);
            }

            // AC#4: draw-call risk diagnostic
            if (result.totalObjects > DRAW_CALL_WARN_THRESHOLD)
                result.warnings.Add(
                    $"Draw-call risk: {result.totalObjects} builtin objects — " +
                    $"consider sprite atlasing ({result.missingSprites.Count} unique missing sprites)");

            // Status
            if (result.missingSprites.Count == 0 && result.totalObjects > 0)
                result.status = ConversionStatus.Complete;
            else if (result.resolvedObjects > 0)
                result.status = ConversionStatus.Partial;
            else if (result.totalObjects == 0)
                result.status = ConversionStatus.NotStarted;
            else
                result.status = ConversionStatus.Failed;

            SubsystemLog.Info("Builtin",
                $"Region [{regionX},{regionY}]: {result.resolvedObjects}/{result.totalObjects} objects. " +
                $"{result.foregroundObjects} foreground. {result.missingSprites.Count} missing sprites.");

            return result;
        }
    }
}
