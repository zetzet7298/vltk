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

        private static void ResetSandboxInstance()
        {
            typeof(SandboxManager)
                .GetProperty("Instance")
                ?.GetSetMethod(true)
                ?.Invoke(null, new object[] { null });
        }
    }
}
