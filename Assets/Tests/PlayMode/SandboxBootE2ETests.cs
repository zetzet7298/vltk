using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VLTK.Sandbox;

namespace VLTK.Tests.PlayMode
{
    /// <summary>
    /// E2E PlayMode tests for the sandbox runtime boot flow. Exercises the real
    /// MonoBehaviour lifecycle (SandboxManager.Awake -> InitializeSubsystems), the
    /// shared AssetRegistry / MapManager / MapRenderer wiring, and StreamingAssets
    /// catalog loading — the integrated path EditMode unit tests cannot cover.
    /// </summary>
    public class SandboxBootE2ETests
    {
        private GameObject _sandboxGo;

        [SetUp]
        public void SetUp()
        {
            // Ensure no stale singleton from a prior test.
            if (SandboxManager.Instance != null)
                Object.DestroyImmediate(SandboxManager.Instance.gameObject);
        }

        [TearDown]
        public void TearDown()
        {
            if (_sandboxGo != null) Object.DestroyImmediate(_sandboxGo);
            if (SandboxManager.Instance != null)
                Object.DestroyImmediate(SandboxManager.Instance.gameObject);
            _sandboxGo = null;
        }

        private IEnumerator BootSandbox()
        {
            _sandboxGo = new GameObject("SandboxManager_E2E");
            _sandboxGo.AddComponent<SandboxManager>(); // Awake runs immediately in play mode
            yield return null; // let one frame pass for Start/initialization settle
        }

        // --- Boot lifecycle ---

        [UnityTest]
        public IEnumerator E2E_Sandbox_Boots_AndInitializes()
        {
            yield return BootSandbox();

            Assert.IsNotNull(SandboxManager.Instance, "SandboxManager.Instance should be set after Awake");
            Assert.IsTrue(SandboxManager.Instance.IsInitialized, "Sandbox should report initialized");
            Assert.IsNotNull(SandboxManager.Instance.BootReport, "Boot report should exist");
        }

        [UnityTest]
        public IEnumerator E2E_BootReport_AllSubsystemsOk()
        {
            yield return BootSandbox();

            var report = SandboxManager.Instance.BootReport;
            Assert.IsNotEmpty(report.Entries, "Boot report should record subsystem entries");
            Assert.IsTrue(report.Entries.All(e => e.ok),
                "All subsystems should boot ok: " +
                string.Join(", ", report.Entries.Where(e => !e.ok).Select(e => e.message)));
        }

        [UnityTest]
        public IEnumerator E2E_CoreServices_Wired()
        {
            yield return BootSandbox();

            var mgr = SandboxManager.Instance;
            Assert.IsNotNull(mgr.AssetRegistry, "AssetRegistry should be created");
            Assert.IsNotNull(mgr.MapManager, "MapManager should be created");
            Assert.IsNotNull(mgr.MapRenderer, "MapRenderer MonoBehaviour should be instantiated on worldRoot");
        }

        [UnityTest]
        public IEnumerator E2E_Subsystem_Roots_Created()
        {
            yield return BootSandbox();
            var mgr = SandboxManager.Instance;
            Assert.IsNotNull(mgr.gameRoot);
            Assert.IsNotNull(mgr.cameraRoot);
            Assert.IsNotNull(mgr.uiRoot);
            Assert.IsNotNull(mgr.worldRoot);
            Assert.IsNotNull(mgr.debugRoot);
            Assert.IsNotNull(mgr.servicesRoot);
        }

        // --- Map catalog loaded from StreamingAssets ---

        [UnityTest]
        public IEnumerator E2E_MapCatalog_LoadedFromStreamingAssets()
        {
            yield return BootSandbox();

            var mgr = SandboxManager.Instance;
            Assert.Greater(mgr.MapManager.Catalog.Count, 0,
                "Map catalog should be populated from StreamingAssets/MapCatalog.json");
        }

        [UnityTest]
        public IEnumerator E2E_DefaultMap_IsTinSuVuotAiPhongKy120()
        {
            yield return BootSandbox();

            var mgr = SandboxManager.Instance;
            Assert.AreEqual(SandboxManager.TinSuVuotAiPhongKy120MapId, mgr.defaultMapId);
            Assert.AreEqual(SandboxManager.TinSuVuotAiPhongKy120MapId, mgr.MapManager.ActiveMapId);
            Assert.AreEqual("Phong Kỳ (Vượt ải 120+)", MapPortManifest.GetNameVi(mgr.MapManager.ActiveMapId));
        }

        // --- Map load / switch / unload lifecycle through the live runtime ---

        [UnityTest]
        public IEnumerator E2E_LoadMap_SetsActiveMap()
        {
            yield return BootSandbox();
            var mgr = SandboxManager.Instance.MapManager;

            int firstId = mgr.GetAllEntries().First().mapId;
            mgr.LoadMap(firstId);
            yield return null;

            Assert.AreEqual(firstId, mgr.ActiveMapId, "Active map id should match loaded map");
            Assert.IsNotNull(mgr.ActiveMap, "ActiveMap definition should be set");
        }

        [UnityTest]
        public IEnumerator E2E_LoadMap_RegistersInAssetRegistry()
        {
            yield return BootSandbox();
            var sm = SandboxManager.Instance;
            int firstId = sm.MapManager.GetAllEntries().First().mapId;

            sm.MapManager.LoadMap(firstId);
            yield return null;

            var byMap = sm.AssetRegistry.GetByMapId(firstId);
            Assert.IsNotEmpty(byMap, "Loaded map should be registered in the shared AssetRegistry");
        }

        [UnityTest]
        public IEnumerator E2E_SwitchMap_OnlyOneActive()
        {
            yield return BootSandbox();
            var mgr = SandboxManager.Instance.MapManager;
            var ids = mgr.GetAllEntries().Take(2).Select(e => e.mapId).ToList();
            Assume.That(ids.Count, Is.EqualTo(2), "Need at least 2 maps for switch test");

            mgr.LoadMap(ids[0]);
            yield return null;
            mgr.LoadMap(ids[1]);
            yield return null;

            Assert.AreEqual(ids[1], mgr.ActiveMapId, "Second map should be active after switch");
        }

        [UnityTest]
        public IEnumerator E2E_UnloadMap_ClearsActive()
        {
            yield return BootSandbox();
            var mgr = SandboxManager.Instance.MapManager;
            int firstId = mgr.GetAllEntries().First().mapId;

            mgr.LoadMap(firstId);
            yield return null;
            mgr.UnloadCurrentMap();
            yield return null;

            Assert.AreEqual(-1, mgr.ActiveMapId, "Active map id should reset after unload");
            Assert.IsNull(mgr.ActiveMap, "ActiveMap should be null after unload");
        }
    }
}
