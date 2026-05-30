using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;
using UnityEngine;
using UnityEngine.TestTools;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// Integration tests for M0.6: MapManager accesses resources through registry
    /// rather than hard-coded paths.
    /// </summary>
    public class MapManagerRegistryTests
    {
        private AssetRegistry _registry;
        private MapManager _manager;

        [SetUp]
        public void Setup()
        {
            _registry = new AssetRegistry();
            _manager = new MapManager(_registry);

            // Seed catalog manually (no StreamingAssets in EditMode)
            var entry = new MapCatalogEntry
            {
                mapId = 1,
                displayNameRaw = "Test Map",
                displayNameNormalized = "Test Map",
                sourceMapPath = "maps/test_001.dat",
                isIndoor = false,
                defaultBrightness = 1f,
                defaultColor = Color.white,
                conversionStatus = ConversionStatus.NotStarted,
                rect = new RectDef { width = 10, height = 10 },
            };

            // Use internal catalog via reflection-free seeding: use LoadPlaceholderCatalog
            // then override by testing via public API
            _manager.LoadPlaceholderCatalog();
        }

        // M0.6 AC#4: MapManager accesses resources through registry not hard-coded paths
        [Test]
        public void LoadMap_RegistersEntryInAssetRegistry()
        {
            // Catalog has placeholder id=1 "Bach Duong Son"
            _manager.LoadMap(1);

            var resolved = _registry.Resolve(1);  // resolve by uid=mapId
            Assert.IsNotNull(resolved, "Registry should contain map after LoadMap");
            Assert.AreEqual(ArtifactType.MapDefinition, resolved.artifactType);
        }

        [Test]
        public void LoadMap_RegistryEntry_HasCorrectStatus_WhenNotConverted()
        {
            _manager.LoadMap(1);

            var resolved = _registry.Resolve(1);
            Assert.IsNotNull(resolved);
            // NotStarted -> Pending in registry
            Assert.AreEqual(AssetStatus.Pending, resolved.status);
        }

        [Test]
        public void LoadMap_ThenSwitch_RegistryRetainsBothMaps()
        {
            _manager.LoadMap(1);
            _manager.LoadMap(2);

            var map1 = _registry.Resolve(1);
            var map2 = _registry.Resolve(2);

            Assert.IsNotNull(map1, "Map 1 should remain in registry after switching");
            Assert.IsNotNull(map2, "Map 2 should be in registry");
        }

        [Test]
        public void LoadMap_UnknownId_DoesNotRegisterInRegistry()
        {
            LogAssert.Expect(LogType.Error, "[MapManager] Map 9999 not found in catalog");
            _manager.LoadMap(9999);  // not in catalog

            var resolved = _registry.Resolve(9999);
            Assert.IsNull(resolved, "Unknown map should not appear in registry");
        }

        [Test]
        public void NoRegistry_LoadMap_DoesNotThrow()
        {
            var noRegManager = new MapManager();  // no registry
            noRegManager.LoadPlaceholderCatalog();

            Assert.DoesNotThrow(() => noRegManager.LoadMap(1));
            Assert.AreEqual(1, noRegManager.ActiveMapId);
        }

        [Test]
        public void LoadCatalog_IsSameAs_LoadPlaceholderCatalog()
        {
            var m1 = new MapManager();
            m1.LoadCatalog();

            var m2 = new MapManager();
            m2.LoadPlaceholderCatalog();

            Assert.AreEqual(m1.Catalog.Count, m2.Catalog.Count);
        }
    }
}
