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

#if UNITY_EDITOR
            Assert.AreEqual(SandboxBootProfile.FastEditor, manager.ActiveBootProfile);
            Assert.IsTrue(manager.IsFastEditorBootActive);
            Assert.IsTrue(manager.cacheReferenceDataInFastEditorBoot);
            Assert.IsTrue(manager.ServiceLoadStatuses.TryGetValue("OptionalStreamingServices", out var status));
            Assert.AreEqual(SandboxServiceDataStatus.SkippedForFastBoot, status.status);
            Assert.IsTrue(status.IsSkipped);
            Assert.IsNotNull(manager.TaskFlagService);
            Assert.IsNotNull(manager.FactionMapRuntimeService);
            Assert.IsNotNull(manager.BattleScriptRuntimeService);
            Assert.IsNotNull(manager.MapManager);
            Assert.AreEqual(-1, manager.MapManager.ActiveMapId);
            Assert.IsNotNull(manager.BootReport);
            Assert.AreEqual(SandboxBootProfile.FastEditor, manager.BootReport.BootProfile);
            Assert.GreaterOrEqual(manager.BootReport.TotalMilliseconds, 0);
            Assert.IsNotEmpty(manager.BootReport.Timings);
#else
            Assert.AreEqual(SandboxBootProfile.Full, manager.ActiveBootProfile);
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
