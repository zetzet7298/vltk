// -----------------------------------------------------------------------------
// VLTK Mobile — AchievementService host dispatch tests
// PC source: settings/achievement/achievement.txt — Thành Tựu (250+).
// Verifies IAchievementServiceHost receives expected events for load / query /
// progress / completion / can-earn evaluation.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class AchievementServiceHostServiceTests
    {
        private sealed class FakeHost : IAchievementServiceHost
        {
            public int RegistryAttachedCalls;
            public int LastRegistryAchievementCount;

            public int ResolvedCalls;
            public int LastResolvedAchievementId;
            public int LastResolvedCategory;
            public string LastResolvedNameRaw;

            public int ByCategoryQueriedCalls;
            public int LastByCategoryCategory;
            public int LastByCategoryResultCount;
            public string LastByCategoryNameVi;

            public int CanEarnEvaluatedCalls;
            public int LastCanEarnAchievementId;
            public bool LastCanEarnResult;
            public int LastCanEarnPlayerLevel;
            public long LastCanEarnProgress;

            public int TryCompleteDispatchedCalls;
            public int LastTryCompleteAchievementId;
            public bool LastTryCompleteSuccess;
            public long LastTryCompleteProgress;

            public int ProgressQueriedCalls;
            public int LastProgressAchievementId;
            public float LastProgressPercent;
            public long LastProgressValue;

            public int UIShowCalls;
            public int LastUIAchievementId;
            public string LastUINameRaw;
            public int LastUICategory;

            public int LogCalls;
            public int LastLogAchievementId;
            public string LastLogEventType;
            public string LastLogDetail;

            public int SFXCalls;
            public int LastSFXAchievementId;
            public string LastSFXAction;

            public int SaveCalls;
            public int LastSaveAchievementId;
            public long LastSaveProgress;
            public int LastSaveCategory;

            public void OnAchievementRegistryAttached(int achievementCount)
            {
                RegistryAttachedCalls++;
                LastRegistryAchievementCount = achievementCount;
            }
            public void OnAchievementResolved(int achievementId, int category, string nameRaw)
            {
                ResolvedCalls++;
                LastResolvedAchievementId = achievementId;
                LastResolvedCategory = category;
                LastResolvedNameRaw = nameRaw;
            }
            public void OnAchievementsByCategoryQueried(int category, int resultCount, string categoryNameVi)
            {
                ByCategoryQueriedCalls++;
                LastByCategoryCategory = category;
                LastByCategoryResultCount = resultCount;
                LastByCategoryNameVi = categoryNameVi;
            }
            public void OnCanEarnEvaluated(int achievementId, bool canEarn, int playerLevel, long progress)
            {
                CanEarnEvaluatedCalls++;
                LastCanEarnAchievementId = achievementId;
                LastCanEarnResult = canEarn;
                LastCanEarnPlayerLevel = playerLevel;
                LastCanEarnProgress = progress;
            }
            public void OnTryCompleteDispatched(int achievementId, bool success, long progress)
            {
                TryCompleteDispatchedCalls++;
                LastTryCompleteAchievementId = achievementId;
                LastTryCompleteSuccess = success;
                LastTryCompleteProgress = progress;
            }
            public void OnProgressQueried(int achievementId, float percent, long progress)
            {
                ProgressQueriedCalls++;
                LastProgressAchievementId = achievementId;
                LastProgressPercent = percent;
                LastProgressValue = progress;
            }
            public void ShowAchievementUI(int achievementId, string nameRaw, int category)
            {
                UIShowCalls++;
                LastUIAchievementId = achievementId;
                LastUINameRaw = nameRaw;
                LastUICategory = category;
            }
            public void LogAchievementEvent(string eventType, int achievementId, string detailVi)
            {
                LogCalls++;
                LastLogEventType = eventType;
                LastLogAchievementId = achievementId;
                LastLogDetail = detailVi;
            }
            public void PlayAchievementSFX(string action, int achievementId)
            {
                SFXCalls++;
                LastSFXAction = action;
                LastSFXAchievementId = achievementId;
            }
            public void SaveAchievementState(int achievementId, long progress, int category)
            {
                SaveCalls++;
                LastSaveAchievementId = achievementId;
                LastSaveProgress = progress;
                LastSaveCategory = category;
            }
        }

        private static (PcAchievementRegistry reg, PcAchievementEntry e1, PcAchievementEntry e2) MakeRegistry()
        {
            var reg = new PcAchievementRegistry();
            var e1 = new PcAchievementEntry
            {
                achievementId = 1, nameRaw = "Sát Thần", category = 0, // Combat
                conditionType = 1, conditionValue = 100, // need 100 kills
                rewardItemId = 5001, rewardCount = 1, rewardExp = 1000, points = 10,
            };
            var e2 = new PcAchievementEntry
            {
                achievementId = 2, nameRaw = "Cấp 50", category = 1, // Quest
                conditionType = 0, conditionValue = 50, // need level 50
                rewardItemId = 5002, rewardCount = 1, rewardExp = 5000, points = 20,
            };
            reg.Register(e1);
            reg.Register(e2);
            return (reg, e1, e2);
        }

        // ── Ctor / AttachHost ───────────────────────────────────────────────
        [Test]
        public void Ctor_Default_Empty()
        {
            var svc = new AchievementService();
            Assert.AreEqual(0, svc.Count);
        }

        [Test]
        public void AttachHost_NullSafe()
        {
            var svc = new AchievementService();
            Assert.DoesNotThrow(() => svc.AttachHost(null));
        }

        // ── RegisterRegistry dispatch ──────────────────────────────────────
        [Test]
        public void RegisterRegistry_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new AchievementService();
            svc.AttachHost(host);
            var (reg, _, _) = MakeRegistry();
            int baseline = host.RegistryAttachedCalls;
            svc.RegisterRegistry(reg);
            Assert.AreEqual(baseline + 1, host.RegistryAttachedCalls);
            Assert.AreEqual(2, host.LastRegistryAchievementCount);
            Assert.AreEqual(1, host.LogCalls);
            Assert.AreEqual(1, host.SFXCalls);
            Assert.AreEqual("load", host.LastSFXAction);
        }

        // ── GetAchievement dispatch ─────────────────────────────────────────
        [Test]
        public void GetAchievement_Found_DispatchesResolved()
        {
            var host = new FakeHost();
            var (reg, e1, _) = MakeRegistry();
            var svc = new AchievementService(reg);
            svc.AttachHost(host);
            int baseline = host.ResolvedCalls;
            var a = svc.GetAchievement(1);
            Assert.IsNotNull(a);
            Assert.AreEqual(baseline + 1, host.ResolvedCalls);
            Assert.AreEqual(1, host.LastResolvedAchievementId);
            Assert.AreEqual(0, host.LastResolvedCategory);
            Assert.AreEqual("Sát Thần", host.LastResolvedNameRaw);
        }

        [Test]
        public void GetAchievement_Missing_LogsButNoResolve()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new AchievementService(reg);
            svc.AttachHost(host);
            int baseline = host.ResolvedCalls;
            int baselineLog = host.LogCalls;
            var a = svc.GetAchievement(9999);
            Assert.IsNull(a);
            Assert.AreEqual(baseline, host.ResolvedCalls);
            Assert.AreEqual(baselineLog + 1, host.LogCalls);
            Assert.AreEqual("query_missing", host.LastLogEventType);
        }

        // ── GetByCategory dispatch ─────────────────────────────────────────
        [Test]
        public void GetByCategory_DispatchesHostCount()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new AchievementService(reg);
            svc.AttachHost(host);
            var list = svc.GetByCategory(0);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(1, host.ByCategoryQueriedCalls);
            Assert.AreEqual(0, host.LastByCategoryCategory);
            Assert.AreEqual(1, host.LastByCategoryResultCount);
            Assert.AreEqual("Chiến đấu", host.LastByCategoryNameVi);
        }

        // ── CanEarn dispatch ──────────────────────────────────────────────
        [Test]
        public void CanEarn_CombatSufficient_DispatchesHostTrue()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new AchievementService(reg);
            svc.AttachHost(host);
            bool result = svc.CanEarn(1, 30, 150); // 150 kills >= 100
            Assert.IsTrue(result);
            Assert.AreEqual(1, host.CanEarnEvaluatedCalls);
            Assert.IsTrue(host.LastCanEarnResult);
            Assert.AreEqual(30, host.LastCanEarnPlayerLevel);
            Assert.AreEqual(150, host.LastCanEarnProgress);
        }

        [Test]
        public void CanEarn_LevelSufficient_DispatchesHostTrue()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new AchievementService(reg);
            svc.AttachHost(host);
            bool result = svc.CanEarn(2, 60, 0); // level 60 >= 50
            Assert.IsTrue(result);
            Assert.IsTrue(host.LastCanEarnResult);
        }

        [Test]
        public void CanEarn_Insufficient_DispatchesHostFalse()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new AchievementService(reg);
            svc.AttachHost(host);
            bool result = svc.CanEarn(1, 30, 50); // 50 kills < 100
            Assert.IsFalse(result);
            Assert.IsFalse(host.LastCanEarnResult);
        }

        [Test]
        public void CanEarn_Missing_DispatchesHostFalse()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new AchievementService(reg);
            svc.AttachHost(host);
            bool result = svc.CanEarn(9999, 30, 0);
            Assert.IsFalse(result);
            Assert.IsFalse(host.LastCanEarnResult);
        }

        // ── TryComplete dispatch ──────────────────────────────────────────
        [Test]
        public void TryComplete_Success_DispatchesHost()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new AchievementService(reg);
            svc.AttachHost(host);
            long progress = 50;
            bool result = svc.TryComplete(1, ref progress);
            Assert.IsTrue(result);
            Assert.AreEqual(100, progress);
            Assert.AreEqual(1, host.TryCompleteDispatchedCalls);
            Assert.IsTrue(host.LastTryCompleteSuccess);
            Assert.AreEqual(100, host.LastTryCompleteProgress);
            Assert.AreEqual(1, host.UIShowCalls);
            Assert.AreEqual(1, host.SaveCalls);
        }

        [Test]
        public void TryComplete_AlreadyComplete_DispatchesHostFalse()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new AchievementService(reg);
            svc.AttachHost(host);
            long progress = 200; // already over 100
            bool result = svc.TryComplete(1, ref progress);
            Assert.IsFalse(result);
            Assert.AreEqual(200, progress);
            Assert.AreEqual(1, host.TryCompleteDispatchedCalls);
            Assert.IsFalse(host.LastTryCompleteSuccess);
        }

        [Test]
        public void TryComplete_Missing_DispatchesHostFalse()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new AchievementService(reg);
            svc.AttachHost(host);
            long progress = 0;
            bool result = svc.TryComplete(9999, ref progress);
            Assert.IsFalse(result);
            Assert.AreEqual(0, progress);
            Assert.AreEqual(1, host.TryCompleteDispatchedCalls);
            Assert.IsFalse(host.LastTryCompleteSuccess);
        }

        // ── GetProgressPercent dispatch ────────────────────────────────────
        [Test]
        public void GetProgressPercent_DispatchesHost()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new AchievementService(reg);
            svc.AttachHost(host);
            float pct = svc.GetProgressPercent(1, 50);
            Assert.AreEqual(50f, pct, 0.01f);
            Assert.AreEqual(1, host.ProgressQueriedCalls);
            Assert.AreEqual(50f, host.LastProgressPercent);
        }

        [Test]
        public void GetProgressPercent_ClampedTo100()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new AchievementService(reg);
            svc.AttachHost(host);
            float pct = svc.GetProgressPercent(1, 200);
            Assert.AreEqual(100f, pct, 0.01f);
        }

        [Test]
        public void GetProgressPercent_Missing_ReturnsZero()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new AchievementService(reg);
            svc.AttachHost(host);
            float pct = svc.GetProgressPercent(9999, 0);
            Assert.AreEqual(0f, pct);
            Assert.AreEqual(1, host.ProgressQueriedCalls);
        }

        // ── GetCategoryName static helper ──────────────────────────────────
        [Test]
        public void GetCategoryName_AllKnown()
        {
            var svc = new AchievementService();
            Assert.AreEqual("Chiến đấu", svc.GetCategoryName(0));
            Assert.AreEqual("Nhiệm vụ", svc.GetCategoryName(1));
            Assert.AreEqual("Kỹ năng", svc.GetCategoryName(2));
            Assert.AreEqual("Tương tác", svc.GetCategoryName(3));
            Assert.AreEqual("Sưu tầm", svc.GetCategoryName(4));
            Assert.AreEqual("Khác", svc.GetCategoryName(99));
        }

        // ── No-host path is silent ─────────────────────────────────────────
        [Test]
        public void NoHost_OperationsDoNotThrow()
        {
            var svc = new AchievementService();
            Assert.DoesNotThrow(() => svc.RegisterRegistry(null));
            Assert.DoesNotThrow(() => svc.GetAchievement(1));
            Assert.DoesNotThrow(() => svc.GetByCategory(0));
            Assert.DoesNotThrow(() => svc.CanEarn(1, 1, 0));
            long p = 0;
            Assert.DoesNotThrow(() => svc.TryComplete(1, ref p));
            Assert.DoesNotThrow(() => svc.GetProgressPercent(1, 0));
        }
    }
}
