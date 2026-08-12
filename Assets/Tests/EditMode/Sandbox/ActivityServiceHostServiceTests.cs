// -----------------------------------------------------------------------------
// VLTK Mobile — ActivityService host dispatch tests
// PC source: settings/activitysys/activity.txt (21 entries).
// Hệ thống hoạt động runtime. Verifies IActivityServiceHost receives expected
// events for load / query / start activity.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class ActivityServiceHostServiceTests
    {
        private sealed class FakeHost : IActivityServiceHost
        {
            public int RegistryAttachedCalls;
            public int LastRegistryActivityCount;

            public int ResolvedCalls;
            public int LastResolvedActivityId;
            public string LastResolvedNameRaw;
            public int LastResolvedType;
            public int LastResolvedOpenHour;
            public int LastResolvedCloseHour;

            public int ByTypeQueriedCalls;
            public int LastByTypeType;
            public int LastByTypeResultCount;
            public string LastByTypeNameVi;

            public int AtHourQueriedCalls;
            public int LastAtHour;
            public int LastAtHourResultCount;

            public int AllQueriedCalls;
            public int LastAllResultCount;

            public int StartDispatchedCalls;
            public int LastStartActivityId;
            public bool LastStartSuccess;
            public string LastStartDetailVi;

            public int UIShowCalls;
            public int LastUIActivityId;
            public string LastUINameRaw;
            public int LastUIType;

            public int LogCalls;
            public int LastLogActivityId;
            public string LastLogEventType;
            public string LastLogDetail;

            public int SFXCalls;
            public int LastSFXActivityId;
            public string LastSFXAction;

            public int SaveCalls;
            public int LastSaveActivityId;
            public int LastSaveType;
            public int LastSaveCurrentHour;

            public void OnActivityRegistryAttached(int activityCount)
            {
                RegistryAttachedCalls++;
                LastRegistryActivityCount = activityCount;
            }
            public void OnActivityResolved(int activityId, string nameRaw, int type, int openHour, int closeHour)
            {
                ResolvedCalls++;
                LastResolvedActivityId = activityId;
                LastResolvedNameRaw = nameRaw;
                LastResolvedType = type;
                LastResolvedOpenHour = openHour;
                LastResolvedCloseHour = closeHour;
            }
            public void OnActivitiesByTypeQueried(int type, int resultCount, string typeNameVi)
            {
                ByTypeQueriedCalls++;
                LastByTypeType = type;
                LastByTypeResultCount = resultCount;
                LastByTypeNameVi = typeNameVi;
            }
            public void OnActivitiesAtHourQueried(int hour, int resultCount)
            {
                AtHourQueriedCalls++;
                LastAtHour = hour;
                LastAtHourResultCount = resultCount;
            }
            public void OnAllActivitiesQueried(int resultCount)
            {
                AllQueriedCalls++;
                LastAllResultCount = resultCount;
            }
            public void OnActivityStartDispatched(int activityId, bool success, string detailVi)
            {
                StartDispatchedCalls++;
                LastStartActivityId = activityId;
                LastStartSuccess = success;
                LastStartDetailVi = detailVi;
            }
            public void ShowActivityUI(int activityId, string nameRaw, int type)
            {
                UIShowCalls++;
                LastUIActivityId = activityId;
                LastUINameRaw = nameRaw;
                LastUIType = type;
            }
            public void LogActivityEvent(string eventType, int activityId, string detailVi)
            {
                LogCalls++;
                LastLogEventType = eventType;
                LastLogActivityId = activityId;
                LastLogDetail = detailVi;
            }
            public void PlayActivitySFX(string action, int activityId)
            {
                SFXCalls++;
                LastSFXAction = action;
                LastSFXActivityId = activityId;
            }
            public void SaveActivityState(int activityId, int type, int currentHour)
            {
                SaveCalls++;
                LastSaveActivityId = activityId;
                LastSaveType = type;
                LastSaveCurrentHour = currentHour;
            }
        }

        private static (PcActivityRegistry reg, PcActivityEntry e1, PcActivityEntry e2) MakeRegistry()
        {
            var reg = new PcActivityRegistry();
            var e1 = new PcActivityEntry
            {
                activityId = 1, nameRaw = "Phong Hoa Lien Thanh", type = 0, // daily
                openHour = 12, closeHour = 14, mapId = 200, minLevel = 30, maxLevel = 100,
            };
            var e2 = new PcActivityEntry
            {
                activityId = 2, nameRaw = "Vo Lam Lien Dau", type = 1, // weekly
                openHour = 20, closeHour = 22, mapId = 201, minLevel = 50, maxLevel = 150,
            };
            reg.Register(e1);
            reg.Register(e2);
            return (reg, e1, e2);
        }

        // ── Ctor / AttachHost ───────────────────────────────────────────────
        [Test]
        public void Ctor_Default_Empty()
        {
            var svc = new ActivityService();
            Assert.AreEqual(0, svc.Count);
        }

        [Test]
        public void AttachHost_NullSafe()
        {
            var svc = new ActivityService();
            Assert.DoesNotThrow(() => svc.AttachHost(null));
        }

        // ── AttachRegistry dispatch ────────────────────────────────────────
        [Test]
        public void AttachRegistry_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new ActivityService();
            svc.AttachHost(host);
            var (reg, _, _) = MakeRegistry();
            int baseline = host.RegistryAttachedCalls;
            svc.AttachRegistry(reg);
            Assert.AreEqual(baseline + 1, host.RegistryAttachedCalls);
            Assert.AreEqual(2, host.LastRegistryActivityCount);
            Assert.AreEqual(1, host.LogCalls);
            Assert.AreEqual(1, host.SFXCalls);
            Assert.AreEqual("load", host.LastSFXAction);
        }

        // ── GetActivity dispatch ───────────────────────────────────────────
        [Test]
        public void GetActivity_Found_DispatchesResolved()
        {
            var host = new FakeHost();
            var (reg, e1, _) = MakeRegistry();
            var svc = new ActivityService();
            svc.AttachHost(host);
            svc.AttachRegistry(reg);
            int baseline = host.ResolvedCalls;
            var a = svc.GetActivity(1);
            Assert.IsNotNull(a);
            Assert.AreEqual(baseline + 1, host.ResolvedCalls);
            Assert.AreEqual(1, host.LastResolvedActivityId);
            Assert.AreEqual("Phong Hoa Lien Thanh", host.LastResolvedNameRaw);
            Assert.AreEqual(0, host.LastResolvedType);
            Assert.AreEqual(12, host.LastResolvedOpenHour);
            Assert.AreEqual(14, host.LastResolvedCloseHour);
        }

        [Test]
        public void GetActivity_Missing_LogsButNoResolve()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new ActivityService();
            svc.AttachHost(host);
            svc.AttachRegistry(reg);
            int baseline = host.ResolvedCalls;
            int baselineLog = host.LogCalls;
            var a = svc.GetActivity(9999);
            Assert.IsNull(a);
            Assert.AreEqual(baseline, host.ResolvedCalls);
            Assert.AreEqual(baselineLog + 1, host.LogCalls);
            Assert.AreEqual("query_missing", host.LastLogEventType);
        }

        // ── GetByType dispatch ─────────────────────────────────────────────
        [Test]
        public void GetByType_DispatchesHostCount()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new ActivityService();
            svc.AttachHost(host);
            svc.AttachRegistry(reg);
            var list = svc.GetByType(0);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(1, host.ByTypeQueriedCalls);
            Assert.AreEqual(0, host.LastByTypeType);
            Assert.AreEqual(1, host.LastByTypeResultCount);
            Assert.AreEqual("Hằng Ngày", host.LastByTypeNameVi);
        }

        [Test]
        public void GetByType_Weekly_DispatchesVietnamese()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new ActivityService();
            svc.AttachHost(host);
            svc.AttachRegistry(reg);
            var list = svc.GetByType(1);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("Hằng Tuần", host.LastByTypeNameVi);
        }

        // ── GetActiveAtHour dispatch ──────────────────────────────────────
        [Test]
        public void GetActiveAtHour_DispatchesHostCount()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new ActivityService();
            svc.AttachHost(host);
            svc.AttachRegistry(reg);
            var list = svc.GetActiveAtHour(13);
            Assert.GreaterOrEqual(list.Count, 1);
            Assert.AreEqual(1, host.AtHourQueriedCalls);
            Assert.AreEqual(13, host.LastAtHour);
        }

        // ── GetAllActivities dispatch ──────────────────────────────────────
        [Test]
        public void GetAllActivities_DispatchesHostCount()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new ActivityService();
            svc.AttachHost(host);
            svc.AttachRegistry(reg);
            int n = 0;
            foreach (var _ in svc.GetAllActivities()) n++;
            Assert.AreEqual(2, n);
            Assert.AreEqual(1, host.AllQueriedCalls);
            Assert.AreEqual(2, host.LastAllResultCount);
        }

        // ── StartActivity dispatch ─────────────────────────────────────────
        [Test]
        public void StartActivity_Success_DispatchesHost()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new ActivityService();
            svc.AttachHost(host);
            svc.AttachRegistry(reg);
            int baselineUI = host.UIShowCalls;
            int baselineLog = host.LogCalls;
            int baselineSFX = host.SFXCalls;
            int baselineSave = host.SaveCalls;
            svc.StartActivity(1);
            Assert.AreEqual(1, host.StartDispatchedCalls);
            Assert.IsTrue(host.LastStartSuccess);
            Assert.AreEqual(1, host.LastStartActivityId);
            Assert.AreEqual(baselineUI + 1, host.UIShowCalls);
            Assert.AreEqual("Phong Hoa Lien Thanh", host.LastUINameRaw);
            Assert.AreEqual(baselineLog + 1, host.LogCalls);
            Assert.AreEqual("start", host.LastLogEventType);
            Assert.AreEqual(baselineSFX + 1, host.SFXCalls);
            Assert.AreEqual("start", host.LastSFXAction);
            Assert.AreEqual(baselineSave + 1, host.SaveCalls);
        }

        [Test]
        public void StartActivity_Missing_DispatchesHostFalse()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new ActivityService();
            svc.AttachHost(host);
            svc.AttachRegistry(reg);
            svc.StartActivity(9999);
            Assert.AreEqual(1, host.StartDispatchedCalls);
            Assert.IsFalse(host.LastStartSuccess);
        }

        // ── TypeNameVi static helper ───────────────────────────────────────
        [Test]
        public void TypeNameVi_AllKnown()
        {
            Assert.AreEqual("Hằng Ngày", ActivityService.TypeNameVi(0));
            Assert.AreEqual("Hằng Tuần", ActivityService.TypeNameVi(1));
            Assert.AreEqual("Hằng Tháng", ActivityService.TypeNameVi(2));
            Assert.AreEqual("Khác (99)", ActivityService.TypeNameVi(99));
        }

        // ── No-host path is silent ─────────────────────────────────────────
        [Test]
        public void NoHost_OperationsDoNotThrow()
        {
            var svc = new ActivityService();
            Assert.DoesNotThrow(() => svc.AttachRegistry(null));
            Assert.DoesNotThrow(() => svc.GetActivity(1));
            Assert.DoesNotThrow(() => svc.GetByType(0));
            Assert.DoesNotThrow(() => svc.GetActiveAtHour(12));
            foreach (var _ in svc.GetAllActivities()) { }
            Assert.DoesNotThrow(() => svc.StartActivity(1));
        }
    }
}
