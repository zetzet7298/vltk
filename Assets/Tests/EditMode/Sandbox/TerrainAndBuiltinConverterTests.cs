using System.Collections.Generic;
using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;
using UnityEngine;

namespace VLTK.Tests.Sandbox
{
    /// <summary>M1.3 — Terrain Layer Conversion tests.</summary>
    public class TerrainLayerConverterTests
    {
        private AssetRegistry MakeEmptyRegistry() => new AssetRegistry();

        private GroundLayerData MakeLayerData(int tileCount = 2, int objCount = 1)
        {
            var d = new GroundLayerData
            {
                numTiles = (uint)tileCount,
                numObjects = (uint)objCount,
                objectDataOffset = 0,
            };
            for (int i = 0; i < tileCount; i++)
                d.tiles.Add(new GroundTileRecord { h = (ushort)i, v = 0, frame = 0, spriteName = $"spr/tile{i}.spr" });
            for (int i = 0; i < objCount; i++)
                d.objects.Add(new GroundObjectRecord { positionX = i * 10, positionY = 0, imageName = $"spr/obj{i}.spr", layer = 0 });
            return d;
        }

        // AC#1: Tiles resolved through registry
        [Test]
        public void Convert_Tiles_ResolvedThroughRegistry()
        {
            var registry = MakeEmptyRegistry();
            var tile0 = new AssetRegistryEntry
            {
                sourceId = new SourceAssetId { sourcePath = "spr/tile0.spr", resourceKind = ResourceKind.Sprite },
                unityAssetPath = "Assets/Generated/tile0.png",
                status = AssetStatus.Available,
                artifactType = ArtifactType.SpriteAtlas,
            };
            registry.Register(tile0);

            var data = MakeLayerData(2, 0);
            var result = TerrainLayerConverter.Convert(data, registry, 1, 0, 0);

            Assert.AreEqual(1, result.resolvedTiles, "AC#1: registered tile should resolve");
            Assert.IsTrue(result.tiles[0].resolved);
            Assert.AreEqual("Assets/Generated/tile0.png", result.tiles[0].resolvedAssetPath);
            Assert.IsFalse(result.tiles[1].resolved, "Unregistered tile should not resolve");
        }

        // AC#2: Multiple layers appear in correct order
        [Test]
        public void Convert_Tiles_DrawLayerIncremental()
        {
            var data = MakeLayerData(5, 0);
            var result = TerrainLayerConverter.Convert(data, MakeEmptyRegistry(), 1, 0, 0);

            for (int i = 0; i < result.tiles.Count; i++)
                Assert.AreEqual(i, result.tiles[i].drawLayer, $"AC#2: tile[{i}] should have drawLayer={i}");
        }

        // AC#3: Missing tiles reported with source ID
        [Test]
        public void Convert_MissingTile_ReportedInMissingSprites()
        {
            var data = MakeLayerData(1, 0);
            var result = TerrainLayerConverter.Convert(data, MakeEmptyRegistry(), 1, 0, 0);

            Assert.Greater(result.missingSprites.Count, 0, "AC#3: missing tile should be reported");
            StringAssert.Contains("spr/tile0.spr", result.missingSprites[0]);
            Assert.Greater(result.warnings.Count, 0);
        }

        // AC#4: Large batch → draw call warning
        [Test]
        public void Convert_LargeBatch_DrawCallWarning()
        {
            var data = MakeLayerData(600, 500);
            var result = TerrainLayerConverter.Convert(data, MakeEmptyRegistry(), 1, 0, 0);

            bool hasDrawCallWarning = result.warnings.Exists(w => w.ToLower().Contains("draw-call"));
            Assert.IsTrue(hasDrawCallWarning, "AC#4: large batch should emit draw-call risk warning");
        }

        [Test]
        public void Convert_NullData_ReturnsFailed()
        {
            var result = TerrainLayerConverter.Convert(null, MakeEmptyRegistry(), 1, 0, 0);
            Assert.AreEqual(ConversionStatus.Failed, result.status);
        }

        [Test]
        public void Convert_AllResolved_StatusComplete()
        {
            var registry = MakeEmptyRegistry();
            registry.Register(new AssetRegistryEntry
            {
                sourceId = new SourceAssetId { sourcePath = "spr/tile0.spr", resourceKind = ResourceKind.Sprite },
                unityAssetPath = "Assets/Generated/tile0.png",
                status = AssetStatus.Available,
                artifactType = ArtifactType.SpriteAtlas,
            });
            var data = new GroundLayerData { numTiles = 1 };
            data.tiles.Add(new GroundTileRecord { spriteName = "spr/tile0.spr" });
            var result = TerrainLayerConverter.Convert(data, registry, 1, 0, 0);
            Assert.AreEqual(ConversionStatus.Complete, result.status);
        }
    }

    /// <summary>M1.4 — Built-in Object Layer Conversion tests.</summary>
    public class BuiltinLayerConverterTests
    {
        private AssetRegistry MakeEmptyRegistry() => new AssetRegistry();

        private BuildinObjData MakeBuildData(int count = 3, bool withForeground = false)
        {
            var d = new BuildinObjData { totalObjects = (uint)count };
            for (int i = 0; i < count; i++)
            {
                uint props = withForeground && i == 0 ? 0x04u : 0u;
                d.objects.Add(new BuildinObjRecord
                {
                    props = props,
                    imageName = $"spr/obj{i}.spr",
                    order = (ushort)i,
                    imgX1 = i * 100,
                    imgY1 = 0,
                });
            }
            return d;
        }

        // AC#1: ObjectPlacement entries include sprite, position, layer, z-order, flags
        [Test]
        public void Convert_PopulatesObjectPlacements()
        {
            var data = MakeBuildData(3);
            var result = BuiltinLayerConverter.Convert(data, MakeEmptyRegistry(), 1, 0, 0);

            Assert.AreEqual(3, result.placements.Count, "AC#1: should have one placement per object");
            Assert.AreEqual("spr/obj0.spr", result.placements[0].spritePath);
            Assert.AreEqual(0, result.placements[0].zOrder);
        }

        // AC#2: Foreground flag detected
        [Test]
        public void Convert_ForegroundFlag_Detected()
        {
            var data = MakeBuildData(3, withForeground: true);
            var result = BuiltinLayerConverter.Convert(data, MakeEmptyRegistry(), 1, 0, 0);

            Assert.Greater(result.foregroundObjects, 0, "AC#2: at least one foreground object should be detected");
            Assert.IsTrue(result.placements[0].isForeground, "AC#2: first object with 0x04 prop should be foreground");
        }

        // AC#3: Missing sprites show placeholder diagnostic
        [Test]
        public void Convert_MissingSprite_ReportedInWarnings()
        {
            var data = MakeBuildData(2);
            var result = BuiltinLayerConverter.Convert(data, MakeEmptyRegistry(), 1, 0, 0);

            Assert.Greater(result.missingSprites.Count, 0, "AC#3: missing sprites should be tracked");
            Assert.IsTrue(result.placements[0].spriteMissing, "AC#3: unresolved placement should be marked missing");
            Assert.Greater(result.warnings.Count, 0);
        }

        // AC#4: High object count → draw-call warning
        [Test]
        public void Convert_HighObjectCount_DrawCallWarning()
        {
            var data = MakeBuildData(600);
            var result = BuiltinLayerConverter.Convert(data, MakeEmptyRegistry(), 1, 0, 0);

            bool hasRisk = result.warnings.Exists(w => w.Contains("Draw-call risk") || w.Contains("draw"));
            Assert.IsTrue(hasRisk, "AC#4: high object count should generate draw-call risk warning");
        }

        [Test]
        public void Convert_NullData_ReturnsFailed()
        {
            var result = BuiltinLayerConverter.Convert(null, MakeEmptyRegistry(), 1, 0, 0);
            Assert.AreEqual(ConversionStatus.Failed, result.status);
        }
    }
}
