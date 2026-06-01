using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// M1.8 — Minimap and World Map Data tests. Covers marker scale mapping,
    /// artifact registration through the asset registry, and missing-state
    /// reporting with its source id (AC#1–AC#4).
    /// </summary>
    public class MinimapTests
    {
        private SourceAssetId MakeSourceId(string path, int uid = 0)
            => new SourceAssetId
            {
                sourcePath = path,
                uid = uid,
                resourceKind = ResourceKind.Map,
            };

        private MapDefinition MakeMap(RectDef bounds, MinimapRef minimap)
            => new MapDefinition
            {
                catalogEntry = new MapCatalogEntry
                {
                    mapId = 42,
                    settingSourceId = MakeSourceId("maps/42/setting.ini", 42),
                },
                sourceBoundsRect = bounds,
                minimapRef = minimap,
            };

        private RectDef Bounds(float x, float y, float w, float h)
            => new RectDef { x = x, y = y, width = w, height = h };

        // --- AC#1: artifact registers and resolves through the registry ---

        [Test]
        public void ResolveArtifact_AvailableInRegistry_MarksRegistered()
        {
            var registry = new AssetRegistry();
            var srcId = MakeSourceId("maps/42/minimap.spr", 4200);
            registry.Register(new AssetRegistryEntry
            {
                sourceId = srcId,
                artifactType = ArtifactType.Texture2D,
                unityAssetPath = "Assets/Maps/42/minimap.png",
                status = AssetStatus.Available,
            });

            var map = MakeMap(Bounds(0, 0, 1000, 1000),
                new MinimapRef { sourceId = srcId });
            var svc = new MinimapService(registry);

            var result = svc.ResolveArtifact(map);

            Assert.AreEqual(MinimapArtifactStatus.Registered, result.status);
            Assert.IsTrue(result.IsRegistered);
            Assert.AreEqual("Assets/Maps/42/minimap.png", result.artifactPath);
            Assert.IsFalse(svc.IsMissing(map));
        }

        // --- AC#4: missing artifact surfaces missing state + source id ---

        [Test]
        public void ResolveArtifact_NotInRegistry_MarksMissingWithSourceId()
        {
            var registry = new AssetRegistry();
            var srcId = MakeSourceId("maps/99/minimap.spr", 9900);
            var map = MakeMap(Bounds(0, 0, 1000, 1000),
                new MinimapRef { sourceId = srcId });
            var svc = new MinimapService(registry);

            var result = svc.ResolveArtifact(map);

            Assert.AreEqual(MinimapArtifactStatus.Missing, result.status);
            Assert.IsTrue(svc.IsMissing(map));
            Assert.AreEqual(srcId, svc.GetMissingSourceId(map));
            Assert.AreEqual("maps/99/minimap.spr", svc.GetMissingSourceId(map).ToKey());
        }

        [Test]
        public void ResolveArtifact_NoMinimapRef_CreatesMissingRefFromCatalogSource()
        {
            var registry = new AssetRegistry();
            var map = MakeMap(Bounds(0, 0, 1000, 1000), minimap: null);
            var svc = new MinimapService(registry);

            var result = svc.ResolveArtifact(map);

            Assert.IsNotNull(result);
            Assert.AreEqual(MinimapArtifactStatus.Missing, result.status);
            Assert.AreSame(result, map.minimapRef);
            Assert.AreEqual("maps/42/setting.ini", svc.GetMissingSourceId(map).ToKey());
        }

        [Test]
        public void ResolveArtifact_RegisteredButMissingStatus_MarksMissing()
        {
            var registry = new AssetRegistry();
            var srcId = MakeSourceId("maps/42/minimap.spr", 4200);
            registry.Register(new AssetRegistryEntry
            {
                sourceId = srcId,
                unityAssetPath = "Assets/Maps/42/minimap.png",
                status = AssetStatus.Missing,
            });
            var map = MakeMap(Bounds(0, 0, 1000, 1000),
                new MinimapRef { sourceId = srcId });
            var svc = new MinimapService(registry);

            var result = svc.ResolveArtifact(map);
            Assert.AreEqual(MinimapArtifactStatus.Missing, result.status);
        }

        // --- AC#3: world position -> minimap marker scale ---

        [Test]
        public void WorldToMinimapNormalized_MapsBoundsToUnitSquare()
        {
            var svc = new MinimapService(new AssetRegistry());
            var map = MakeMap(Bounds(0, 0, 1000, 500), new MinimapRef());

            Assert.AreEqual(new Vector2(0f, 0f), svc.WorldToMinimapNormalized(map, new Vector2(0, 0)));
            Assert.AreEqual(new Vector2(1f, 1f), svc.WorldToMinimapNormalized(map, new Vector2(1000, 500)));
            Assert.AreEqual(new Vector2(0.5f, 0.5f), svc.WorldToMinimapNormalized(map, new Vector2(500, 250)));
        }

        [Test]
        public void WorldToMinimapNormalized_OffsetBounds_AccountsForOrigin()
        {
            var svc = new MinimapService(new AssetRegistry());
            var map = MakeMap(Bounds(200, 100, 800, 400), new MinimapRef());

            // World (200,100) is the bottom-left of the bounds → (0,0).
            Assert.AreEqual(new Vector2(0f, 0f), svc.WorldToMinimapNormalized(map, new Vector2(200, 100)));
            // World (600,300) is the center.
            Assert.AreEqual(new Vector2(0.5f, 0.5f), svc.WorldToMinimapNormalized(map, new Vector2(600, 300)));
        }

        [Test]
        public void WorldToMinimapNormalized_OutOfBounds_Clamps()
        {
            var svc = new MinimapService(new AssetRegistry());
            var map = MakeMap(Bounds(0, 0, 1000, 1000), new MinimapRef());

            Assert.AreEqual(new Vector2(0f, 0f), svc.WorldToMinimapNormalized(map, new Vector2(-500, -500)));
            Assert.AreEqual(new Vector2(1f, 1f), svc.WorldToMinimapNormalized(map, new Vector2(5000, 5000)));
        }

        [Test]
        public void WorldToMinimapPixel_AppliesSizeAndFlipsY()
        {
            var svc = new MinimapService(new AssetRegistry());
            var map = MakeMap(Bounds(0, 0, 1000, 1000), new MinimapRef());
            var size = new Vector2(256, 256);

            // World bottom-left (0,0) -> normalized (0,0) -> pixel top-left (0, 256).
            Assert.AreEqual(new Vector2(0f, 256f), svc.WorldToMinimapPixel(map, new Vector2(0, 0), size));
            // World top-right (1000,1000) -> normalized (1,1) -> pixel (256, 0).
            Assert.AreEqual(new Vector2(256f, 0f), svc.WorldToMinimapPixel(map, new Vector2(1000, 1000), size));
            // Center stays center.
            Assert.AreEqual(new Vector2(128f, 128f), svc.WorldToMinimapPixel(map, new Vector2(500, 500), size));
        }

        [Test]
        public void WorldToMinimapNormalized_DegenerateBounds_ReturnsCenter()
        {
            var svc = new MinimapService(new AssetRegistry());
            var map = MakeMap(Bounds(0, 0, 0, 0), new MinimapRef());
            Assert.AreEqual(new Vector2(0.5f, 0.5f), svc.WorldToMinimapNormalized(map, new Vector2(123, 456)));
        }

        [Test]
        public void MinimapPixelToWorld_InvertsTopLeftPixelMapping()
        {
            var svc = new MinimapService(new AssetRegistry());
            var map = MakeMap(Bounds(200, 100, 800, 400), new MinimapRef());
            var size = new Vector2(400, 200);

            Assert.AreEqual(new Vector2(200f, 500f), svc.MinimapPixelToWorld(map, new Vector2(0, 0), size));
            Assert.AreEqual(new Vector2(1000f, 100f), svc.MinimapPixelToWorld(map, new Vector2(400, 200), size));
            Assert.AreEqual(new Vector2(600f, 300f), svc.MinimapPixelToWorld(map, new Vector2(200, 100), size));
        }

        [Test]
        public void MinimapNormalizedToWorld_InvertsTopLeftNormalizedClick()
        {
            var svc = new MinimapService(new AssetRegistry());
            var map = MakeMap(Bounds(0, 0, 1000, 500), new MinimapRef());

            Assert.AreEqual(new Vector2(250f, 375f), svc.MinimapNormalizedToWorld(map, new Vector2(0.25f, 0.25f)));
            Assert.AreEqual(new Vector2(750f, 125f), svc.MinimapNormalizedToWorld(map, new Vector2(0.75f, 0.75f)));
        }
    }
}
