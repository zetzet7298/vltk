using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;
using UnityEngine;
using UnityEngine.TestTools;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// M0.10 — Sandbox Placeholder Map Flow tests.
    /// Tests: AC1 catalog listed, AC2 map loads/clears world root,
    /// AC3 no duplicate objects on repeated switch, AC4 error shown on fail.
    /// </summary>
    public class SandboxMapFlowTests
    {
        private AssetRegistry _registry;
        private MapManager _manager;

        [SetUp]
        public void Setup()
        {
            _registry = new AssetRegistry();
            _manager = new MapManager(_registry);
            _manager.LoadPlaceholderCatalog();
        }

        // --- AC#1: Placeholder catalog exists → maps are listed ---

        [Test]
        public void LoadPlaceholderCatalog_PopulatesCatalog()
        {
            // If real MapCatalog.json exists it will load 158 maps; else placeholder gives 15.
            Assert.Greater(_manager.Catalog.Count, 0, "Catalog should have entries after load");
        }

        [Test]
        public void GetAllEntries_ReturnsSortedById()
        {
            var entries = _manager.GetAllEntries();
            Assert.Greater(entries.Count, 0);

            for (int i = 1; i < entries.Count; i++)
                Assert.LessOrEqual(entries[i - 1].mapId, entries[i].mapId,
                    "Entries should be sorted by mapId");
        }

        [Test]
        public void Search_EmptyQuery_ReturnsAllEntries()
        {
            var all = _manager.GetAllEntries();
            var searched = _manager.Search("");
            Assert.AreEqual(all.Count, searched.Count);
        }

        [Test]
        public void Search_ByMapId_ReturnsFilteredList()
        {
            var results = _manager.Search("1");
            Assert.Greater(results.Count, 0);
            foreach (var r in results)
                Assert.IsTrue(r.mapId.ToString().Contains("1") ||
                    (r.displayNameNormalized != null && r.displayNameNormalized.ToLower().Contains("1")));
        }

        // --- AC#2: Developer selects map → world root cleared and map loaded ---

        [Test]
        public void LoadMap_ValidId_SetsActiveMap()
        {
            var firstId = _manager.GetAllEntries()[0].mapId;
            _manager.LoadMap(firstId);

            Assert.AreEqual(firstId, _manager.ActiveMapId);
            Assert.IsNotNull(_manager.ActiveMap);
            Assert.IsNotNull(_manager.ActiveMap.catalogEntry);
        }

        [Test]
        public void LoadMap_ValidId_FiresOnMapLoadedEvent()
        {
            int loadedId = -1;
            _manager.OnMapLoaded += id => loadedId = id;

            var firstId = _manager.GetAllEntries()[0].mapId;
            _manager.LoadMap(firstId);

            Assert.AreEqual(firstId, loadedId);
        }

        [Test]
        public void LoadMap_AlreadyLoaded_DoesNotFireEventAgain()
        {
            var firstId = _manager.GetAllEntries()[0].mapId;
            _manager.LoadMap(firstId);

            int loadCount = 0;
            _manager.OnMapLoaded += _ => loadCount++;
            _manager.LoadMap(firstId);  // already loaded

            Assert.AreEqual(0, loadCount, "Should not fire OnMapLoaded for already-loaded map");
        }

        // --- AC#3: Switching maps → no duplicate objects ---

        [Test]
        public void SwitchMap_UnloadsOldBeforeLoadingNew()
        {
            var entries = _manager.GetAllEntries();
            Assert.GreaterOrEqual(entries.Count, 2, "Need at least 2 maps to test switching");

            int unloadedId = -1;
            _manager.OnMapUnloaded += id => unloadedId = id;

            _manager.LoadMap(entries[0].mapId);
            _manager.LoadMap(entries[1].mapId);

            // First map should have been unloaded
            Assert.AreEqual(entries[0].mapId, unloadedId);
            // Second map should now be active
            Assert.AreEqual(entries[1].mapId, _manager.ActiveMapId);
        }

        [Test]
        public void SwitchMapRepeatedly_OnlyOneActiveAtATime()
        {
            var entries = _manager.GetAllEntries();
            int count = Mathf.Min(5, entries.Count);

            for (int i = 0; i < count; i++)
            {
                _manager.LoadMap(entries[i].mapId);
                Assert.AreEqual(entries[i].mapId, _manager.ActiveMapId,
                    $"After loading map {i}, active should be {entries[i].mapId}");
            }
        }

        [Test]
        public void UnloadCurrentMap_ClearsActiveMap()
        {
            var firstId = _manager.GetAllEntries()[0].mapId;
            _manager.LoadMap(firstId);
            Assert.AreEqual(firstId, _manager.ActiveMapId);

            _manager.UnloadCurrentMap();

            Assert.AreEqual(-1, _manager.ActiveMapId);
            Assert.IsNull(_manager.ActiveMap);
        }

        [Test]
        public void UnloadCurrentMap_WhenNoneLoaded_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _manager.UnloadCurrentMap());
            Assert.AreEqual(-1, _manager.ActiveMapId);
        }

        // --- AC#4: Map fails to load → error event fires ---

        [Test]
        public void LoadMap_UnknownId_FiresOnMapErrorEvent()
        {
            string receivedError = null;
            _manager.OnMapError += msg => receivedError = msg;

            LogAssert.Expect(LogType.Error, "[MapManager] Map 99999 not found in catalog");
            _manager.LoadMap(99999);  // not in catalog

            Assert.IsNotNull(receivedError, "OnMapError should fire for unknown map id");
            Assert.IsTrue(receivedError.Length > 0);
        }

        [Test]
        public void LoadMap_UnknownId_DoesNotChangeActiveMap()
        {
            var firstId = _manager.GetAllEntries()[0].mapId;
            _manager.LoadMap(firstId);

            LogAssert.Expect(LogType.Error, "[MapManager] Map 99999 not found in catalog");
            _manager.LoadMap(99999);

            // Active map should remain unchanged
            Assert.AreEqual(firstId, _manager.ActiveMapId);
        }

        [Test]
        public void LoadMap_UnknownId_ActiveMapRemainsNull_WhenNoneWasLoaded()
        {
            LogAssert.Expect(LogType.Error, "[MapManager] Map 99999 not found in catalog");
            _manager.LoadMap(99999);
            Assert.AreEqual(-1, _manager.ActiveMapId);
            Assert.IsNull(_manager.ActiveMap);
        }

        // --- Registry integration (M0.6 carried into M0.10 flow) ---

        [Test]
        public void RegistryContains_LoadedMap_ByUid()
        {
            int firstId = 0;
            foreach (var e in _manager.GetAllEntries())
            {
                if (e.mapId != 0) { firstId = e.mapId; break; }
            }
            Assert.AreNotEqual(0, firstId, "Need a non-zero map id to test uid registration");
            _manager.LoadMap(firstId);

            var entry = _registry.Resolve(firstId);
            Assert.IsNotNull(entry, "Registry should contain loaded map by uid");
        }
    }
}
