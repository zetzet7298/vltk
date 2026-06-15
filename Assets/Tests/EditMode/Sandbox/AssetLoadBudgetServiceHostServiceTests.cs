// -----------------------------------------------------------------------------
// VLTK Mobile — AssetLoadBudgetService host dispatch tests
// M6.2 AC#1/AC#2/AC#3/AC#4. Verifies IAssetLoadBudgetHost receives expected
// events for begin / progress / complete / fail / unload / budget / mode query.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class AssetLoadBudgetServiceHostServiceTests
    {
        private sealed class FakeHost : IAssetLoadBudgetHost
        {
            public int BegunCalls;
            public int LastBegunKeyCalls;
            public long LastBegunEstimated;
            public bool LastBegunIsExisting;

            public int ProgressCalls;
            public int LastProgressKeyCalls;
            public float LastProgress01;

            public int CompletedCalls;
            public int LastCompletedKeyCalls;
            public long LastCompletedActual;
            public long LastCompletedTotalLoaded;

            public int FailedCalls;
            public int LastFailedKeyCalls;
            public string LastFailedError;

            public int UnloadedCalls;
            public int LastUnloadedKeyCalls;
            public long LastUnloadedBytesFreed;
            public long LastUnloadedTotalLoaded;

            public int BudgetCheckedCalls;
            public long LastBudgetCheckedLoaded;
            public long LastBudgetCheckedBudget;
            public bool LastBudgetCheckedOverBudget;
            public float LastBudgetCheckedUtilization;

            public int OverrunWarningCalls;
            public long LastOverrunLoaded;
            public long LastOverrunBudget;
            public float LastOverrunUtilization;

            public int RuntimeModeResolvedCalls;
            public int LastRuntimeRegisteredMode;
            public int LastRuntimeResolvedMode;
            public string LastRuntimeModeName;

            public int UIShowCalls;
            public int LastUIShowKeyCalls;
            public float LastUIShowProgress;
            public long LastUIShowLoaded;
            public long LastUIShowTotal;

            public int LogCalls;
            public int LastLogKeyCalls;
            public string LastLogEventType;
            public string LastLogDetail;

            public int SFXCalls;
            public int LastSFXKeyCalls;
            public string LastSFXAction;

            public int SaveCalls;
            public int LastSaveModeId;
            public string LastSaveModeName;

            public void OnLoadBegun(string assetKey, long estimatedBytes, bool isExisting)
            {
                BegunCalls++;
                LastBegunKeyCalls++;
                LastBegunEstimated = estimatedBytes;
                LastBegunIsExisting = isExisting;
            }
            public void OnLoadProgress(string assetKey, float progress01)
            {
                ProgressCalls++;
                LastProgressKeyCalls++;
                LastProgress01 = progress01;
            }
            public void OnLoadCompleted(string assetKey, long actualBytes, long totalLoadedBytes)
            {
                CompletedCalls++;
                LastCompletedKeyCalls++;
                LastCompletedActual = actualBytes;
                LastCompletedTotalLoaded = totalLoadedBytes;
            }
            public void OnLoadFailed(string assetKey, string errorVi)
            {
                FailedCalls++;
                LastFailedKeyCalls++;
                LastFailedError = errorVi;
            }
            public void OnLoadUnloaded(string assetKey, long bytesFreed, long totalLoadedBytes)
            {
                UnloadedCalls++;
                LastUnloadedKeyCalls++;
                LastUnloadedBytesFreed = bytesFreed;
                LastUnloadedTotalLoaded = totalLoadedBytes;
            }
            public void OnBudgetChecked(long loadedBytes, long budgetBytes, bool overBudget, float utilization01)
            {
                BudgetCheckedCalls++;
                LastBudgetCheckedLoaded = loadedBytes;
                LastBudgetCheckedBudget = budgetBytes;
                LastBudgetCheckedOverBudget = overBudget;
                LastBudgetCheckedUtilization = utilization01;
            }
            public void OnBudgetOverrunWarning(long loadedBytes, long budgetBytes, float utilization01)
            {
                OverrunWarningCalls++;
                LastOverrunLoaded = loadedBytes;
                LastOverrunBudget = budgetBytes;
                LastOverrunUtilization = utilization01;
            }
            public void OnRuntimeLoadModeResolved(int registeredModeId, int runtimeModeId, string modeName)
            {
                RuntimeModeResolvedCalls++;
                LastRuntimeRegisteredMode = registeredModeId;
                LastRuntimeResolvedMode = runtimeModeId;
                LastRuntimeModeName = modeName;
            }
            public void ShowLoadProgressUI(string assetKey, float progress01, long loadedBytes, long totalBytes)
            {
                UIShowCalls++;
                LastUIShowKeyCalls++;
                LastUIShowProgress = progress01;
                LastUIShowLoaded = loadedBytes;
                LastUIShowTotal = totalBytes;
            }
            public void LogLoadEvent(string eventType, string assetKey, string detailVi)
            {
                LogCalls++;
                LastLogEventType = eventType;
                LastLogKeyCalls++;
                LastLogDetail = detailVi;
            }
            public void PlayLoadSFX(string action, string assetKey)
            {
                SFXCalls++;
                LastSFXAction = action;
                LastSFXKeyCalls++;
            }
            public void SaveLoadModeCache(int runtimeModeId, string modeName)
            {
                SaveCalls++;
                LastSaveModeId = runtimeModeId;
                LastSaveModeName = modeName;
            }
        }

        // ── Ctor / AttachHost ───────────────────────────────────────────────
        [Test]
        public void Ctor_Default_256MBBudget()
        {
            var svc = new AssetLoadBudgetService();
            Assert.AreEqual(256L * 1024 * 1024, svc.BudgetBytes);
            Assert.AreEqual(0, svc.LoadedBytes);
        }

        [Test]
        public void AttachHost_NullSafe()
        {
            var svc = new AssetLoadBudgetService();
            Assert.DoesNotThrow(() => svc.AttachHost(null));
        }

        // ── BeginLoad dispatch ─────────────────────────────────────────────
        [Test]
        public void BeginLoad_DispatchesHost_New()
        {
            var host = new FakeHost();
            var svc = new AssetLoadBudgetService(host);
            svc.BeginLoad("map/baolang", 1024 * 1024);
            Assert.AreEqual(1, host.BegunCalls);
            Assert.IsFalse(host.LastBegunIsExisting);
            Assert.AreEqual(1024 * 1024, host.LastBegunEstimated);
            Assert.AreEqual(1, host.UIShowCalls);
            Assert.AreEqual(0f, host.LastUIShowProgress);
            Assert.AreEqual(1, host.LogCalls);
            Assert.AreEqual("begin", host.LastLogEventType);
            Assert.AreEqual(1, host.SFXCalls);
            Assert.AreEqual("begin", host.LastSFXAction);
        }

        [Test]
        public void BeginLoad_Duplicate_DispatchesExisting()
        {
            var host = new FakeHost();
            var svc = new AssetLoadBudgetService(host);
            svc.BeginLoad("map/x", 100);
            svc.BeginLoad("map/x", 200);
            Assert.AreEqual(2, host.BegunCalls);
            Assert.IsTrue(host.LastBegunIsExisting);
        }

        // ── ReportProgress dispatch ─────────────────────────────────────────
        [Test]
        public void ReportProgress_DispatchesHost_Clamps()
        {
            var host = new FakeHost();
            var svc = new AssetLoadBudgetService(host);
            svc.BeginLoad("map/x", 100);
            int baseline = host.ProgressCalls;
            svc.ReportProgress("map/x", 1.5f);
            Assert.AreEqual(baseline + 1, host.ProgressCalls);
            Assert.AreEqual(1f, host.LastProgress01);
            svc.ReportProgress("map/x", -0.5f);
            Assert.AreEqual(0f, host.LastProgress01);
        }

        [Test]
        public void ReportProgress_UnknownKey_NoDispatch()
        {
            var host = new FakeHost();
            var svc = new AssetLoadBudgetService(host);
            int baseline = host.ProgressCalls;
            svc.ReportProgress("nope", 0.5f);
            Assert.AreEqual(baseline, host.ProgressCalls);
        }

        // ── CompleteLoad dispatch ──────────────────────────────────────────
        [Test]
        public void CompleteLoad_DispatchesHost_AddsBytes()
        {
            var host = new FakeHost();
            var svc = new AssetLoadBudgetService(host);
            svc.BeginLoad("map/baolang", 100);
            svc.CompleteLoad("map/baolang", 500);
            Assert.AreEqual(500, svc.LoadedBytes);
            Assert.AreEqual(1, host.CompletedCalls);
            Assert.AreEqual(500, host.LastCompletedActual);
            Assert.AreEqual(500, host.LastCompletedTotalLoaded);
            Assert.AreEqual(1, host.BudgetCheckedCalls);
        }

        [Test]
        public void CompleteLoad_BudgetOverrun_DispatchesWarning()
        {
            var host = new FakeHost();
            var svc = new AssetLoadBudgetService(host) { BudgetBytes = 100 };
            svc.BeginLoad("big", 50);
            svc.CompleteLoad("big", 200); // over budget
            Assert.AreEqual(1, host.OverrunWarningCalls);
            Assert.IsTrue(host.LastOverrunUtilization > 1f);
        }

        // ── FailLoad dispatch ──────────────────────────────────────────────
        [Test]
        public void FailLoad_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new AssetLoadBudgetService(host);
            svc.BeginLoad("map/x", 100);
            int baseLog = host.LogCalls;
            int baseSfx = host.SFXCalls;
            svc.FailLoad("map/x", "Network timeout");
            Assert.AreEqual(1, host.FailedCalls);
            Assert.AreEqual(baseLog + 1, host.LogCalls);
            Assert.AreEqual("fail", host.LastLogEventType);
            Assert.AreEqual(baseSfx + 1, host.SFXCalls);
            Assert.AreEqual("fail", host.LastSFXAction);
        }

        // ── Unload dispatch ────────────────────────────────────────────────
        [Test]
        public void Unload_DispatchesHost_FreesBytes()
        {
            var host = new FakeHost();
            var svc = new AssetLoadBudgetService(host);
            svc.BeginLoad("map/x", 100);
            svc.CompleteLoad("map/x", 500);
            long baselineLoaded = svc.LoadedBytes;
            svc.Unload("map/x");
            Assert.AreEqual(baselineLoaded - 500, svc.LoadedBytes);
            Assert.AreEqual(1, host.UnloadedCalls);
            Assert.AreEqual(500, host.LastUnloadedBytesFreed);
        }

        [Test]
        public void Unload_NotLoaded_NoDispatch()
        {
            var host = new FakeHost();
            var svc = new AssetLoadBudgetService(host);
            svc.BeginLoad("map/x", 100); // Loading, not Loaded
            int baseline = host.UnloadedCalls;
            svc.Unload("map/x");
            Assert.AreEqual(baseline, host.UnloadedCalls);
        }

        // ── CheckBudget dispatch ───────────────────────────────────────────
        [Test]
        public void CheckBudget_Empty_DispatchesZero()
        {
            var host = new FakeHost();
            var svc = new AssetLoadBudgetService(host);
            int baseline = host.BudgetCheckedCalls;
            var status = svc.CheckBudget();
            Assert.IsFalse(status.overBudget);
            Assert.AreEqual(0f, status.utilization);
            Assert.AreEqual(baseline + 1, host.BudgetCheckedCalls);
        }

        [Test]
        public void CheckBudget_ZeroBudget_DispatchesZeroUtilization()
        {
            var host = new FakeHost();
            var svc = new AssetLoadBudgetService(host) { BudgetBytes = 0 };
            int baseline = host.BudgetCheckedCalls;
            var status = svc.CheckBudget();
            Assert.AreEqual(0f, status.utilization);
            Assert.AreEqual(baseline + 1, host.BudgetCheckedCalls);
        }

        // ── RuntimeLoadMode dispatch ───────────────────────────────────────
        [Test]
        public void RuntimeLoadMode_StreamingAssets_DispatchesResolved()
        {
            var host = new FakeHost();
            var svc = new AssetLoadBudgetService(host);
            var mode = svc.RuntimeLoadMode(LoadMode.EditorDirect);
            Assert.AreEqual(LoadMode.StreamingAssets, mode);
            Assert.AreEqual(1, host.RuntimeModeResolvedCalls);
            Assert.AreEqual((int)LoadMode.EditorDirect, host.LastRuntimeRegisteredMode);
            Assert.AreEqual((int)LoadMode.StreamingAssets, host.LastRuntimeResolvedMode);
            Assert.AreEqual(1, host.SaveCalls);
        }

        [Test]
        public void RuntimeLoadMode_Addressables_Preserved()
        {
            var host = new FakeHost();
            var svc = new AssetLoadBudgetService(host);
            var mode = svc.RuntimeLoadMode(LoadMode.Addressables);
            Assert.AreEqual(LoadMode.Addressables, mode);
            Assert.AreEqual((int)LoadMode.Addressables, host.LastRuntimeResolvedMode);
        }

        [Test]
        public void RuntimeLoadMode_AssetBundle_Preserved()
        {
            var host = new FakeHost();
            var svc = new AssetLoadBudgetService(host);
            var mode = svc.RuntimeLoadMode(LoadMode.AssetBundle);
            Assert.AreEqual(LoadMode.AssetBundle, mode);
        }

        // ── No-host path is silent ─────────────────────────────────────────
        [Test]
        public void NoHost_OperationsDoNotThrow()
        {
            var svc = new AssetLoadBudgetService();
            Assert.DoesNotThrow(() => svc.BeginLoad("x", 100));
            Assert.DoesNotThrow(() => svc.ReportProgress("x", 0.5f));
            Assert.DoesNotThrow(() => svc.CompleteLoad("x", 200));
            Assert.DoesNotThrow(() => svc.FailLoad("x", "err"));
            Assert.DoesNotThrow(() => svc.Unload("x"));
            Assert.DoesNotThrow(() => svc.CheckBudget());
            Assert.DoesNotThrow(() => svc.RuntimeLoadMode(LoadMode.Addressables));
        }
    }
}
