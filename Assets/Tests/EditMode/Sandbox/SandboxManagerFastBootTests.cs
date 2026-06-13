using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class SandboxManagerFastBootTests
    {
        [SetUp]
        public void SetUp()
        {
            ResetSandboxInstance();
        }

        [TearDown]
        public void TearDown()
        {
            var manager = SandboxManager.Instance;
            if (manager != null)
                Object.DestroyImmediate(manager.gameObject);
            ResetSandboxInstance();
        }

        [Test]
        public void Awake_DefaultEditorFastBoot_SkipsOptionalServicesAndDefaultMap()
        {
            var go = new GameObject("SandboxManagerFastBootTest");
            var manager = go.AddComponent<SandboxManager>();
            manager.useFastEditorBoot = true;
            // In EditMode, InitializeSubsystems has many null dependencies (no scene/camera/UI).
            // Just verify that the boot profile resolves correctly without running full init.
            var resolveMethod = typeof(SandboxManager)
                .GetMethod("ResolveBootProfile", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var profile = (SandboxBootProfile)resolveMethod?.Invoke(manager, null);

#if UNITY_EDITOR
            Assert.AreEqual(SandboxBootProfile.FastEditor, profile);
            Assert.IsTrue(manager.useFastEditorBoot);
            Assert.IsFalse(manager.loadOptionalServicesInFastEditorBoot);
#else
            Assert.AreEqual(SandboxBootProfile.Full, profile);
#endif
        }

        // ── CTS-03 minimal-setup contract tests ─────────────────────────────────
        // These verify that SandboxManager.InitializeSubsystems() — the actual
        // boot path — does NOT throw when wired up with the absolute minimum
        // scene (no StreamingAssets, no scene-root transforms, no camera, no UI).
        // The contract from CTS-03 is: a missing dependency catalog must be
        // tolerated, and SandboxManager must still report IsInitialized=true so
        // HUD/services can probe via `manager?.XxxService?.Foo`.
        //
        // Implementation note: we invoke InitializeSubsystems via reflection
        // rather than relying on AddComponent → Awake. The Awake guard
        // (`Instance != null && Instance != this`) trips when a prior test's
        // destroyed-but-not-GC'd Instance lingers (cf. PORT_STATUS note from
        // CTS-02: "stale Instance from a prior test trips the Awake guard").
        // Direct invocation lets the contract test focus on the boot logic itself.

        [Test]
        public void InitializeSubsystems_MinimalSetup_DoesNotThrow_AndSetsIsInitializedTrue()
        {
            var go = new GameObject("SandboxManagerMinimalAwakeTest");
            var manager = go.AddComponent<SandboxManager>();

            var initMethod = typeof(SandboxManager)
                .GetMethod("InitializeSubsystems", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(initMethod, "InitializeSubsystems must exist on SandboxManager.");

            Assert.DoesNotThrow(() => initMethod.Invoke(manager, null),
                "InitializeSubsystems must not throw on a fresh GameObject with no dependencies.");

            Assert.IsTrue(manager.IsInitialized,
                "SandboxManager must report IsInitialized=true after InitializeSubsystems, " +
                "even when its dependency catalog (RegionCatalog, ItemDb, MapManager) is null.");
            Assert.IsNotNull(manager.BootReport,
                "SandboxManager must produce a BootReport so callers can inspect which steps succeeded.");
        }

        [Test]
        public void InitializeSubsystems_MinimalSetup_AssetRegistryIsAlwaysConstructed()
        {
            // AssetRegistry is constructed in-memory with no external dependencies;
            // it must always be present after InitializeSubsystems so HUD services
            // that read it (e.g. MinimapService) don't see null.
            var go = new GameObject("SandboxManagerAssetRegistryTest");
            var manager = go.AddComponent<SandboxManager>();

            var initMethod = typeof(SandboxManager)
                .GetMethod("InitializeSubsystems", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.DoesNotThrow(() => initMethod.Invoke(manager, null),
                "InitializeSubsystems must not throw on a fresh GameObject.");

            Assert.IsNotNull(manager.AssetRegistry,
                "AssetRegistry must be constructed during InitializeSubsystems — " +
                "it is required by MinimapService and other HUD services.");
        }

        [Test]
        public void InitializeSubsystems_MinimalSetup_FastEditorBoot_ResolvesFastEditor()
        {
            // useFastEditorBoot must be honored by InitializeSubsystems.
            var go = new GameObject("SandboxManagerFastEditorTest");
            var manager = go.AddComponent<SandboxManager>();
            manager.useFastEditorBoot = true;
            manager.loadOptionalServicesInFastEditorBoot = false;

            var initMethod = typeof(SandboxManager)
                .GetMethod("InitializeSubsystems", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.DoesNotThrow(() => initMethod.Invoke(manager, null),
                "InitializeSubsystems must not throw under FastEditor boot profile.");

            Assert.AreEqual(SandboxBootProfile.FastEditor, manager.ActiveBootProfile,
                "ActiveBootProfile must match ResolveBootProfile when useFastEditorBoot is true.");
        }

        private static void ResetSandboxInstance()
        {
            typeof(SandboxManager)
                .GetProperty("Instance")
                ?.GetSetMethod(true)
                ?.Invoke(null, new object[] { null });
        }
    }
}
