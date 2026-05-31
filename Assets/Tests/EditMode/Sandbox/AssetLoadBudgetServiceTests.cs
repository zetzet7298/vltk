using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// M6.2 — Mobile Asset Loading tests. Async load progress (AC#2), memory budget
    /// warning when exceeded (AC#3), and stable runtime load mode across packaging
    /// strategies (AC#1/AC#4).
    /// </summary>
    public class AssetLoadBudgetServiceTests
    {
        private AssetLoadBudgetService MakeService(long budget)
            => new AssetLoadBudgetService { BudgetBytes = budget };

        // --- AC#2: async progress ---

        [Test]
        public void BeginLoad_TracksJobAsLoading()
        {
            var svc = MakeService(1000);
            var job = svc.BeginLoad("map1", 500);
            Assert.AreEqual(AssetLoadState.Loading, job.state);
            Assert.AreEqual(0f, job.progress);
        }

        [Test]
        public void ReportProgress_UpdatesJob()
        {
            var svc = MakeService(1000);
            svc.BeginLoad("map1", 500);
            svc.ReportProgress("map1", 0.6f);
            var job = new System.Collections.Generic.List<AssetLoadJob>(svc.Jobs)[0];
            Assert.AreEqual(0.6f, job.progress, 0.001f);
        }

        [Test]
        public void ReportProgress_ClampsToUnitRange()
        {
            var svc = MakeService(1000);
            svc.BeginLoad("map1", 500);
            svc.ReportProgress("map1", 5f);
            var job = new System.Collections.Generic.List<AssetLoadJob>(svc.Jobs)[0];
            Assert.AreEqual(1f, job.progress);
        }

        [Test]
        public void CompleteLoad_MarksLoadedAndAddsBytes()
        {
            var svc = MakeService(1000);
            svc.BeginLoad("map1", 400);
            svc.CompleteLoad("map1", 400);
            Assert.AreEqual(400, svc.LoadedBytes);
        }

        [Test]
        public void BeginLoad_Idempotent_ForSameKey()
        {
            var svc = MakeService(1000);
            var a = svc.BeginLoad("map1", 400);
            var b = svc.BeginLoad("map1", 400);
            Assert.AreSame(a, b);
            Assert.AreEqual(1, new System.Collections.Generic.List<AssetLoadJob>(svc.Jobs).Count);
        }

        // --- AC#3: memory budget warning ---

        [Test]
        public void CompleteLoad_UnderBudget_NoWarning()
        {
            var svc = MakeService(1000);
            svc.BeginLoad("a", 400);
            var status = svc.CompleteLoad("a", 400);
            Assert.IsFalse(status.overBudget);
            Assert.AreEqual(0.4f, status.utilization, 0.001f);
        }

        [Test]
        public void CompleteLoad_OverBudget_FlagsAndWarns()
        {
            var svc = MakeService(1000);
            svc.BeginLoad("a", 600);
            svc.CompleteLoad("a", 600);
            svc.BeginLoad("b", 600);

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Memory budget exceeded"));
            var status = svc.CompleteLoad("b", 600); // total 1200 > 1000
            Assert.IsTrue(status.overBudget);
            Assert.AreEqual(1200, status.loadedBytes);
        }

        [Test]
        public void Unload_FreesBytes()
        {
            var svc = MakeService(1000);
            svc.BeginLoad("a", 400);
            svc.CompleteLoad("a", 400);
            svc.Unload("a");
            Assert.AreEqual(0, svc.LoadedBytes);
        }

        [Test]
        public void FailLoad_MarksFailed()
        {
            var svc = MakeService(1000);
            svc.BeginLoad("a", 400);
            LogAssert.Expect(LogType.Warning, "[AssetLoad] Load failed for 'a': disk error");
            svc.FailLoad("a", "disk error");
            var job = new System.Collections.Generic.List<AssetLoadJob>(svc.Jobs)[0];
            Assert.AreEqual(AssetLoadState.Failed, job.state);
        }

        // --- AC#1/AC#4: runtime load mode stability ---

        [Test]
        public void RuntimeLoadMode_EditorOrTestFixture_CollapsesToStreamingAssets()
        {
            var svc = MakeService(1000);
            Assert.AreEqual(LoadMode.StreamingAssets, svc.RuntimeLoadMode(LoadMode.EditorDirect));
            Assert.AreEqual(LoadMode.StreamingAssets, svc.RuntimeLoadMode(LoadMode.TestFixture));
            Assert.AreEqual(LoadMode.StreamingAssets, svc.RuntimeLoadMode(LoadMode.Resources));
        }

        [Test]
        public void RuntimeLoadMode_BundleAndAddressables_Preserved()
        {
            var svc = MakeService(1000);
            Assert.AreEqual(LoadMode.AssetBundle, svc.RuntimeLoadMode(LoadMode.AssetBundle));
            Assert.AreEqual(LoadMode.Addressables, svc.RuntimeLoadMode(LoadMode.Addressables));
        }
    }
}
