// -----------------------------------------------------------------------------
// VLTK Mobile — TaskFlagService EditMode tests.
// Kiểm tra task flag runtime: set/get/has, can accept, serialize, host.
// PC source: Task flags + lua TaskState.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class TaskFlagServiceHostServiceTests
    {
        // ── Host fake ────────────────────────────────────────────────────────

        private sealed class FakeHost : ITaskFlagHost
        {
            public int SetCalls;
            public int CompleteCalls;
            public int RewardedCalls;
            public int DeniedCalls;
            public int CatalogAttachedCalls;
            public int SerializedCalls;
            public int DeserializedCalls;
            public int ShowCalls;
            public int LogCalls;
            public int SfxCalls;
            public int SaveCalls;
            public int LastTaskId;
            public int LastOldStatus;
            public int LastNewStatus;
            public int LastProgress;
            public int LastTargetCount;
            public int LastPlayerLevel;
            public int LastReqLevel;
            public int LastFlagCount;
            public int LastTaskCount;
            public string LastJson;
            public string LastSfxAction;

            public void OnTaskFlagSet(int taskId, int oldStatus, int newStatus, int progress, int targetCount)
            {
                SetCalls++;
                LastTaskId = taskId;
                LastOldStatus = oldStatus;
                LastNewStatus = newStatus;
                LastProgress = progress;
                LastTargetCount = targetCount;
            }
            public void OnTaskComplete(int taskId, int progress, int targetCount) { CompleteCalls++; }
            public void OnTaskRewarded(int taskId) { RewardedCalls++; }
            public void OnTaskAcceptDenied(int taskId, int playerLevel, int reqLevel)
            {
                DeniedCalls++;
                LastPlayerLevel = playerLevel;
                LastReqLevel = reqLevel;
            }
            public void OnCatalogAttached(int flagCount)
            {
                CatalogAttachedCalls++;
                LastFlagCount = flagCount;
            }
            public void OnSerialized(string json, int taskCount)
            {
                SerializedCalls++;
                LastJson = json;
                LastTaskCount = taskCount;
            }
            public void OnDeserialized(int taskCount)
            {
                DeserializedCalls++;
                LastTaskCount = taskCount;
            }
            public void ShowTaskUI(int taskId, int status, int progress, int targetCount) { ShowCalls++; }
            public void LogTaskFlagEvent(int taskId, int status, string message) { LogCalls++; }
            public void PlayTaskSFX(int taskId, int status, string action) { SfxCalls++; LastSfxAction = action; }
            public void SaveTaskFlagState(int taskId, int status, int progress, int targetCount) { SaveCalls++; }
        }

        // ── Ctor / AttachHost ───────────────────────────────────────────────

        [Test]
        public void Constructor_Default()
        {
            var svc = new TaskFlagService();
            Assert.IsNotNull(svc);
        }

        [Test]
        public void Constructor_WithHost()
        {
            var host = new FakeHost();
            var svc = new TaskFlagService(host);
            Assert.IsNotNull(svc);
        }

        [Test]
        public void AttachHost_Stores()
        {
            var host = new FakeHost();
            var svc = new TaskFlagService();
            svc.AttachHost(host);
            svc.SetFlag(1, 1);
            Assert.AreEqual(1, host.SetCalls);
        }

        // ── SetFlag / GetFlag / HasFlag ─────────────────────────────────────

        [Test]
        public void SetFlag_NewTask()
        {
            var svc = new TaskFlagService();
            svc.SetFlag(1, 1, 0, 10, "Gather 10 herbs");
            Assert.IsTrue(svc.HasFlag(1));
            Assert.AreEqual(1, svc.GetFlag(1));
        }

        [Test]
        public void SetFlag_UpdatesExisting()
        {
            var svc = new TaskFlagService();
            svc.SetFlag(1, 1);
            svc.SetFlag(1, 2);
            Assert.AreEqual(2, svc.GetFlag(1));
        }

        [Test]
        public void SetFlag_KeepTargetCount_WhenZero()
        {
            var svc = new TaskFlagService();
            svc.SetFlag(1, 1, 0, 10);
            svc.SetFlag(1, 2, 5, 0);  // 0 means keep
            var data = svc.GetTaskData(1);
            Assert.AreEqual(10, data.targetCount);
        }

        [Test]
        public void SetFlag_KeepDescription_WhenEmpty()
        {
            var svc = new TaskFlagService();
            svc.SetFlag(1, 1, 0, 0, "Initial");
            svc.SetFlag(1, 2, 5, 0, "");
            var data = svc.GetTaskData(1);
            Assert.AreEqual("Initial", data.descriptionVi);
        }

        [Test]
        public void GetFlag_NotFound_Zero()
        {
            var svc = new TaskFlagService();
            Assert.AreEqual(0, svc.GetFlag(999));
        }

        [Test]
        public void HasFlag_False()
        {
            var svc = new TaskFlagService();
            Assert.IsFalse(svc.HasFlag(1));
        }

        [Test]
        public void HasFlag_True()
        {
            var svc = new TaskFlagService();
            svc.SetFlag(1, 1);
            Assert.IsTrue(svc.HasFlag(1));
        }

        [Test]
        public void GetTaskData_NotFound_Null()
        {
            var svc = new TaskFlagService();
            Assert.IsNull(svc.GetTaskData(999));
        }

        [Test]
        public void GetTaskData_Exists()
        {
            var svc = new TaskFlagService();
            svc.SetFlag(1, 2, 5, 10, "Test");
            var data = svc.GetTaskData(1);
            Assert.IsNotNull(data);
            Assert.AreEqual(2, data.status);
            Assert.AreEqual(5, data.progress);
        }

        [Test]
        public void IsTaskComplete_Status2_True()
        {
            var svc = new TaskFlagService();
            svc.SetFlag(1, 2);
            Assert.IsTrue(svc.IsTaskComplete(1));
        }

        [Test]
        public void IsTaskComplete_Status1_False()
        {
            var svc = new TaskFlagService();
            svc.SetFlag(1, 1);
            Assert.IsFalse(svc.IsTaskComplete(1));
        }

        [Test]
        public void IsTaskFinished_Status3_True()
        {
            var svc = new TaskFlagService();
            svc.SetFlag(1, 3);
            Assert.IsTrue(svc.IsTaskFinished(1));
        }

        [Test]
        public void IsTaskFinished_Status2_False()
        {
            var svc = new TaskFlagService();
            svc.SetFlag(1, 2);
            Assert.IsFalse(svc.IsTaskFinished(1));
        }

        // ── CanAcceptTask ───────────────────────────────────────────────────

        [Test]
        public void CanAcceptTask_FirstTime_True()
        {
            var svc = new TaskFlagService();
            Assert.IsTrue(svc.CanAcceptTask(1, 10, 5));
        }

        [Test]
        public void CanAcceptTask_LowLevel_False()
        {
            var svc = new TaskFlagService();
            Assert.IsFalse(svc.CanAcceptTask(1, 3, 5));
        }

        [Test]
        public void CanAcceptTask_AlreadyAccepted_False()
        {
            var svc = new TaskFlagService();
            svc.SetFlag(1, 1);
            Assert.IsFalse(svc.CanAcceptTask(1, 10, 5));
        }

        [Test]
        public void CanAcceptTask_WithPrereq_NotFinished_False()
        {
            var svc = new TaskFlagService();
            Assert.IsFalse(svc.CanAcceptTask(2, 10, 5, prerequisiteTaskId: 1));
        }

        [Test]
        public void CanAcceptTask_WithPrereq_Finished_True()
        {
            var svc = new TaskFlagService();
            svc.SetFlag(1, 3);
            Assert.IsTrue(svc.CanAcceptTask(2, 10, 5, prerequisiteTaskId: 1));
        }

        [Test]
        public void CanAcceptTask_LowLevel_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new TaskFlagService(host);
            svc.CanAcceptTask(1, 3, 5);
            Assert.AreEqual(1, host.DeniedCalls);
            Assert.AreEqual(3, host.LastPlayerLevel);
            Assert.AreEqual(5, host.LastReqLevel);
        }

        // ── Serialize / Deserialize ─────────────────────────────────────────

        [Test]
        public void SerializeToSave_Empty()
        {
            var svc = new TaskFlagService();
            string json = svc.SerializeToSave();
            Assert.IsNotNull(json);
            Assert.IsTrue(json.Length > 0);
        }

        [Test]
        public void SerializeToSave_WithData()
        {
            var svc = new TaskFlagService();
            svc.SetFlag(1, 2, 5, 10, "Test");
            string json = svc.SerializeToSave();
            Assert.IsTrue(json.Contains("\"taskId\":1"));
        }

        [Test]
        public void SerializeToSave_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new TaskFlagService(host);
            svc.SetFlag(1, 1);
            svc.SerializeToSave();
            Assert.AreEqual(1, host.SerializedCalls);
            Assert.AreEqual(1, host.LastTaskCount);
        }

        [Test]
        public void DeserializeFromSave_Null_NoOp()
        {
            var svc = new TaskFlagService();
            svc.SetFlag(1, 1);
            svc.DeserializeFromSave(null);
            Assert.IsTrue(svc.HasFlag(1)); // unchanged
        }

        [Test]
        public void DeserializeFromSave_Empty_NoOp()
        {
            var svc = new TaskFlagService();
            svc.SetFlag(1, 1);
            svc.DeserializeFromSave("");
            Assert.IsTrue(svc.HasFlag(1));
        }

        [Test]
        public void DeserializeFromSave_RoundTrip()
        {
            var svc = new TaskFlagService();
            svc.SetFlag(1, 2, 5, 10, "Task1");
            svc.SetFlag(2, 1, 0, 5, "Task2");
            string json = svc.SerializeToSave();

            var svc2 = new TaskFlagService();
            svc2.DeserializeFromSave(json);
            Assert.AreEqual(2, svc2.GetFlag(1));
            Assert.AreEqual(1, svc2.GetFlag(2));
        }

        [Test]
        public void DeserializeFromSave_ClearsExisting()
        {
            var svc = new TaskFlagService();
            svc.SetFlag(1, 1);
            svc.SetFlag(2, 1);
            svc.SetFlag(3, 1);
            svc.DeserializeFromSave("{\"tasks\":[{\"taskId\":10,\"status\":2,\"progress\":0,\"targetCount\":0,\"descriptionVi\":\"X\"}]}");
            Assert.IsFalse(svc.HasFlag(1));
            Assert.IsTrue(svc.HasFlag(10));
        }

        [Test]
        public void DeserializeFromSave_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new TaskFlagService(host);
            svc.DeserializeFromSave("{\"tasks\":[{\"taskId\":1,\"status\":2,\"progress\":0,\"targetCount\":0,\"descriptionVi\":\"X\"}]}");
            Assert.AreEqual(1, host.DeserializedCalls);
            Assert.AreEqual(1, host.LastTaskCount);
        }

        [Test]
        public void DeserializeFromSave_InvalidJson_NoThrow()
        {
            var svc = new TaskFlagService();
            Assert.DoesNotThrow(() => svc.DeserializeFromSave("not a json"));
        }

        // ── OnTaskStatusChanged event ───────────────────────────────────────

        [Test]
        public void SetFlag_FiresOnTaskStatusChanged()
        {
            var svc = new TaskFlagService();
            int fired = 0;
            svc.OnTaskStatusChanged += (id, st) => fired++;
            svc.SetFlag(1, 1);
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void SetFlag_SameStatus_NoEvent()
        {
            var svc = new TaskFlagService();
            svc.SetFlag(1, 2);
            int fired = 0;
            svc.OnTaskStatusChanged += (id, st) => fired++;
            svc.SetFlag(1, 2);  // same status
            Assert.AreEqual(0, fired);
        }

        // ── AttachCatalog ───────────────────────────────────────────────────

        [Test]
        public void AttachCatalog_Null_FallsBackToEmpty()
        {
            var svc = new TaskFlagService();
            svc.AttachCatalog(null);
            Assert.AreEqual(0, svc.CatalogCount);
        }

        [Test]
        public void AttachCatalog_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new TaskFlagService(host);
            var reg = new PcTaskFlagRegistry();
            reg.Add(new PcTaskFlagEntry { flagId = 1, taskName = "T1" });
            reg.Add(new PcTaskFlagEntry { flagId = 2, taskName = "T2" });
            svc.AttachCatalog(reg);
            Assert.AreEqual(1, host.CatalogAttachedCalls);
            Assert.AreEqual(2, host.LastFlagCount);
        }

        [Test]
        public void GetFlagMeta_NoCatalog_Null()
        {
            var svc = new TaskFlagService();
            Assert.IsNull(svc.GetFlagMeta(1));
        }

        // ── No-host ─────────────────────────────────────────────────────────

        [Test]
        public void TaskFlagService_WithoutHost_DoesNotThrow()
        {
            var svc = new TaskFlagService();
            Assert.DoesNotThrow(() => svc.SetFlag(1, 1));
            Assert.DoesNotThrow(() => svc.CanAcceptTask(1, 3, 5));  // denied
            Assert.DoesNotThrow(() => svc.SerializeToSave());
            Assert.DoesNotThrow(() => svc.DeserializeFromSave("{\"tasks\":[]}"));
        }
    }
}
