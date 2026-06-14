// -----------------------------------------------------------------------------
// VLTK Mobile — AchievementService EditMode tests.
// Kiểm tra achievement catalog lookup, condition check, per-player progress
// tracking với host dispatch (UI icon, completion broadcast, reward grants,
// SFX, save). PC source: settings/achievement/achievement.txt + lua.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class AchievementProgressTests
    {
        // ── Host fake ────────────────────────────────────────────────────────

        private sealed class FakeHost : IAchievementHost
        {
            public int ShowCalls;
            public int CompletedCalls;
            public int SfxCalls;
            public int ItemCalls;
            public int ExpCalls;
            public int MoneyCalls;
            public int PointsCalls;
            public int SaveCalls;
            public int LastPlayerId;
            public int LastAchievementId;
            public bool LastCompleted;
            public int LastRewardItem;
            public int LastRewardCount;
            public int LastRewardExp;
            public int LastPoints;

            public void ShowAchievementIcon(int playerId, int achievementId, bool isCompleted)
            {
                ShowCalls++;
                LastPlayerId = playerId;
                LastAchievementId = achievementId;
                LastCompleted = isCompleted;
            }
            public void OnAchievementCompleted(int playerId, int achievementId, string achievementName)
            {
                CompletedCalls++;
            }
            public void PlayAchievementSFX(int playerId, int achievementId) { SfxCalls++; }
            public void GrantAchievementItem(int playerId, int itemId, int count)
            {
                ItemCalls++;
                LastRewardItem = itemId;
                LastRewardCount = count;
            }
            public void GrantAchievementExp(int playerId, int exp)
            {
                ExpCalls++;
                LastRewardExp = exp;
            }
            public void GrantAchievementMoney(int playerId, int money) { MoneyCalls++; }
            public void AddAchievementPoints(int playerId, int points)
            {
                PointsCalls++;
                LastPoints = points;
            }
            public void SaveProgress(int playerId, int achievementId, long progress, bool completed)
            {
                SaveCalls++;
            }
        }

        private static PcAchievementRegistry BuildRegistry(params (int id, int cat, int ctype, int cval, int item, int cnt, int exp, int points)[] rows)
        {
            var reg = new PcAchievementRegistry();
            foreach (var r in rows)
            {
                reg.Register(new PcAchievementEntry
                {
                    achievementId = r.id,
                    nameRaw = $"Thành Tựu {r.id}",
                    category = r.cat,
                    conditionType = r.ctype,
                    conditionValue = r.cval,
                    rewardItemId = r.item,
                    rewardCount = r.cnt,
                    rewardExp = r.exp,
                    points = r.points,
                });
            }
            return reg;
        }

        // ── Registry attach + count ────────────────────────────────────────

        [Test]
        public void Count_AfterRegistry_ReturnsEntryCount()
        {
            var reg = BuildRegistry((1, 0, 0, 10, 0, 0, 0, 5));
            var svc = new AchievementService(reg);
            Assert.AreEqual(1, svc.Count);
        }

        [Test]
        public void Count_EmptyService_ReturnsZero()
        {
            var svc = new AchievementService();
            Assert.AreEqual(0, svc.Count);
        }

        [Test]
        public void RegisterRegistry_NullRegistry_EmptyState()
        {
            var svc = new AchievementService();
            svc.RegisterRegistry(null);
            Assert.AreEqual(0, svc.Count);
        }

        // ── Category constants ──────────────────────────────────────────────

        [Test]
        public void Category_Constants_AreSequential()
        {
            Assert.AreEqual(0, AchievementService.CategoryCombat);
            Assert.AreEqual(1, AchievementService.CategoryQuest);
            Assert.AreEqual(2, AchievementService.CategorySkill);
            Assert.AreEqual(3, AchievementService.CategoryInteraction);
            Assert.AreEqual(4, AchievementService.CategoryCollection);
        }

        // ── CanEarn ─────────────────────────────────────────────────────────

        [Test]
        public void CanEarn_NotFound_ReturnsFalse()
        {
            var svc = new AchievementService();
            Assert.IsFalse(svc.CanEarn(99, 50, 0));
        }

        [Test]
        public void CanEarn_LevelType_TooLow_ReturnsFalse()
        {
            var reg = BuildRegistry((1, 0, 0, 30, 0, 0, 0, 0)); // type 0 = level
            var svc = new AchievementService(reg);
            Assert.IsFalse(svc.CanEarn(1, 20, 0));
        }

        [Test]
        public void CanEarn_LevelType_EnoughLevel_ReturnsTrue()
        {
            var reg = BuildRegistry((1, 0, 0, 30, 0, 0, 0, 0));
            var svc = new AchievementService(reg);
            Assert.IsTrue(svc.CanEarn(1, 30, 0));
            Assert.IsTrue(svc.CanEarn(1, 50, 0));
        }

        [Test]
        public void CanEarn_ProgressType_Insufficient_ReturnsFalse()
        {
            var reg = BuildRegistry((1, 0, 1, 100, 0, 0, 0, 0)); // type 1 = progress
            var svc = new AchievementService(reg);
            Assert.IsFalse(svc.CanEarn(1, 0, 50));
        }

        [Test]
        public void CanEarn_ProgressType_Sufficient_ReturnsTrue()
        {
            var reg = BuildRegistry((1, 0, 1, 100, 0, 0, 0, 0));
            var svc = new AchievementService(reg);
            Assert.IsTrue(svc.CanEarn(1, 0, 100));
            Assert.IsTrue(svc.CanEarn(1, 0, 200));
        }

        // ── TryComplete ─────────────────────────────────────────────────────

        [Test]
        public void TryComplete_NotFound_ReturnsFalse()
        {
            var svc = new AchievementService();
            long p = 0;
            Assert.IsFalse(svc.TryComplete(99, ref p));
        }

        [Test]
        public void TryComplete_FirstTime_ReachesMax_ReturnsTrue()
        {
            var reg = BuildRegistry((1, 0, 1, 100, 0, 0, 0, 0));
            var svc = new AchievementService(reg);
            long p = 50;
            Assert.IsTrue(svc.TryComplete(1, ref p));
            Assert.AreEqual(100, p);
        }

        [Test]
        public void TryComplete_AlreadyMax_ReturnsFalse()
        {
            var reg = BuildRegistry((1, 0, 1, 100, 0, 0, 0, 0));
            var svc = new AchievementService(reg);
            long p = 100;
            Assert.IsFalse(svc.TryComplete(1, ref p));
        }

        [Test]
        public void TryComplete_NoCondition_ReturnsFalse()
        {
            var reg = BuildRegistry((1, 0, 1, 0, 0, 0, 0, 0)); // conditionValue = 0
            var svc = new AchievementService(reg);
            long p = 0;
            Assert.IsFalse(svc.TryComplete(1, ref p));
        }

        // ── GetProgressPercent ─────────────────────────────────────────────

        [Test]
        public void GetProgressPercent_NotFound_ReturnsZero()
        {
            var svc = new AchievementService();
            Assert.AreEqual(0f, svc.GetProgressPercent(99, 50));
        }

        [Test]
        public void GetProgressPercent_HalfProgress()
        {
            var reg = BuildRegistry((1, 0, 1, 100, 0, 0, 0, 0));
            var svc = new AchievementService(reg);
            Assert.AreEqual(50f, svc.GetProgressPercent(1, 50), 0.01f);
        }

        [Test]
        public void GetProgressPercent_Overflow_ClampsTo100()
        {
            var reg = BuildRegistry((1, 0, 1, 100, 0, 0, 0, 0));
            var svc = new AchievementService(reg);
            Assert.AreEqual(100f, svc.GetProgressPercent(1, 200), 0.01f);
        }

        [Test]
        public void GetProgressPercent_NoCondition_ReturnsZero()
        {
            var reg = BuildRegistry((1, 0, 1, 0, 0, 0, 0, 0));
            var svc = new AchievementService(reg);
            Assert.AreEqual(0f, svc.GetProgressPercent(1, 50));
        }

        // ── GetCategoryName ─────────────────────────────────────────────────

        [Test]
        public void GetCategoryName_AllFive()
        {
            var svc = new AchievementService();
            Assert.AreEqual("Chiến đấu", svc.GetCategoryName(0));
            Assert.AreEqual("Nhiệm vụ", svc.GetCategoryName(1));
            Assert.AreEqual("Kỹ năng", svc.GetCategoryName(2));
            Assert.AreEqual("Tương tác", svc.GetCategoryName(3));
            Assert.AreEqual("Sưu tầm", svc.GetCategoryName(4));
        }

        [Test]
        public void GetCategoryName_Unknown_ReturnsKhac()
        {
            var svc = new AchievementService();
            Assert.AreEqual("Khác", svc.GetCategoryName(99));
        }

        // ── TrackProgress (new) ────────────────────────────────────────────

        [Test]
        public void TrackProgress_NotFound_ReturnsFalse()
        {
            var svc = new AchievementService();
            Assert.IsFalse(svc.TrackProgress(1, 99, 10));
        }

        [Test]
        public void TrackProgress_NoCondition_ReturnsFalse()
        {
            var reg = BuildRegistry((1, 0, 1, 0, 0, 0, 0, 0));
            var svc = new AchievementService(reg);
            Assert.IsFalse(svc.TrackProgress(1, 1, 10));
        }

        [Test]
        public void TrackProgress_Accumulate_BelowMax()
        {
            var reg = BuildRegistry((1, 0, 1, 100, 0, 0, 0, 0));
            var svc = new AchievementService(reg);
            svc.TrackProgress(1, 1, 30);
            svc.TrackProgress(1, 1, 40);
            Assert.AreEqual(70, svc.GetPlayerProgress(1, 1));
        }

        [Test]
        public void TrackProgress_Overflow_ClampsToMax()
        {
            var reg = BuildRegistry((1, 0, 1, 100, 0, 0, 0, 0));
            var svc = new AchievementService(reg);
            svc.TrackProgress(1, 1, 200);
            Assert.AreEqual(100, svc.GetPlayerProgress(1, 1));
        }

        [Test]
        public void TrackProgress_HitsMax_ReturnsTrue()
        {
            var reg = BuildRegistry((1, 0, 1, 100, 0, 0, 0, 0));
            var svc = new AchievementService(reg);
            Assert.IsTrue(svc.TrackProgress(1, 1, 100));
            Assert.IsTrue(svc.IsPlayerCompleted(1, 1));
        }

        [Test]
        public void TrackProgress_AlreadyCompleted_ReturnsFalse()
        {
            var reg = BuildRegistry((1, 0, 1, 100, 0, 0, 0, 0));
            var svc = new AchievementService(reg);
            svc.TrackProgress(1, 1, 100); // complete
            Assert.IsFalse(svc.TrackProgress(1, 1, 50)); // already complete
        }

        [Test]
        public void TrackProgress_DispatchesShowOnProgress()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, 0, 1, 100, 0, 0, 0, 0));
            var svc = new AchievementService(reg, host);
            svc.TrackProgress(1, 1, 30);
            Assert.AreEqual(1, host.ShowCalls);
            Assert.IsFalse(host.LastCompleted);
        }

        [Test]
        public void TrackProgress_DispatchesCompletedOnMax()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, 0, 1, 100, 0, 0, 0, 0));
            var svc = new AchievementService(reg, host);
            svc.TrackProgress(1, 1, 100);
            Assert.AreEqual(1, host.CompletedCalls);
            Assert.AreEqual(1, host.SfxCalls);
            Assert.AreEqual(1, host.SaveCalls);
        }

        [Test]
        public void TrackProgress_DoesNotFireCompleted_BelowMax()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, 0, 1, 100, 0, 0, 0, 0));
            var svc = new AchievementService(reg, host);
            svc.TrackProgress(1, 1, 50);
            Assert.AreEqual(0, host.CompletedCalls);
        }

        [Test]
        public void TrackProgress_GrantsItemReward()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, 0, 1, 100, 500, 5, 0, 0)); // item 500 x5
            var svc = new AchievementService(reg, host);
            svc.TrackProgress(1, 1, 100);
            Assert.AreEqual(1, host.ItemCalls);
            Assert.AreEqual(500, host.LastRewardItem);
            Assert.AreEqual(5, host.LastRewardCount);
        }

        [Test]
        public void TrackProgress_GrantsExpReward()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, 0, 1, 100, 0, 0, 1000, 0));
            var svc = new AchievementService(reg, host);
            svc.TrackProgress(1, 1, 100);
            Assert.AreEqual(1, host.ExpCalls);
            Assert.AreEqual(1000, host.LastRewardExp);
        }

        [Test]
        public void TrackProgress_GrantsPointsReward()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, 0, 1, 100, 0, 0, 0, 50));
            var svc = new AchievementService(reg, host);
            svc.TrackProgress(1, 1, 100);
            Assert.AreEqual(1, host.PointsCalls);
            Assert.AreEqual(50, host.LastPoints);
        }

        [Test]
        public void TrackProgress_FiresOnProgressUpdatedEvent()
        {
            var reg = BuildRegistry((1, 0, 1, 100, 0, 0, 0, 0));
            var svc = new AchievementService(reg);
            int fired = 0;
            svc.OnProgressUpdated += (p, a) => fired++;
            svc.TrackProgress(1, 1, 30);
            svc.TrackProgress(1, 1, 40);
            Assert.AreEqual(2, fired);
        }

        [Test]
        public void TrackProgress_FiresOnCompletedEvent()
        {
            var reg = BuildRegistry((1, 0, 1, 100, 0, 0, 0, 0));
            var svc = new AchievementService(reg);
            int fired = 0;
            svc.OnCompleted += (p, a) => fired++;
            svc.TrackProgress(1, 1, 100);
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void TrackProgress_WithoutHost_DoesNotThrow()
        {
            var reg = BuildRegistry((1, 0, 1, 100, 0, 0, 0, 0));
            var svc = new AchievementService(reg);
            Assert.DoesNotThrow(() => svc.TrackProgress(1, 1, 50));
        }

        [Test]
        public void TrackProgress_MultiplePlayers_Isolated()
        {
            var reg = BuildRegistry((1, 0, 1, 100, 0, 0, 0, 0));
            var svc = new AchievementService(reg);
            svc.TrackProgress(1, 1, 30);
            svc.TrackProgress(2, 1, 80);
            Assert.AreEqual(30, svc.GetPlayerProgress(1, 1));
            Assert.AreEqual(80, svc.GetPlayerProgress(2, 1));
        }

        [Test]
        public void TrackProgress_MultipleAchievements()
        {
            var reg = BuildRegistry((1, 0, 1, 100, 0, 0, 0, 0), (2, 0, 1, 50, 0, 0, 0, 0));
            var svc = new AchievementService(reg);
            svc.TrackProgress(1, 1, 50);
            svc.TrackProgress(1, 2, 30);
            Assert.AreEqual(50, svc.GetPlayerProgress(1, 1));
            Assert.AreEqual(30, svc.GetPlayerProgress(1, 2));
        }

        [Test]
        public void GetPlayerProgress_NoEntry_ReturnsZero()
        {
            var svc = new AchievementService();
            Assert.AreEqual(0L, svc.GetPlayerProgress(99, 1));
        }

        [Test]
        public void IsPlayerCompleted_NoEntry_ReturnsFalse()
        {
            var svc = new AchievementService();
            Assert.IsFalse(svc.IsPlayerCompleted(99, 1));
        }

        [Test]
        public void GetPlayerCompletedCount_NoEntry_ReturnsZero()
        {
            var svc = new AchievementService();
            Assert.AreEqual(0, svc.GetPlayerCompletedCount(99));
        }

        [Test]
        public void GetPlayerCompletedCount_AfterCompletes()
        {
            var reg = BuildRegistry((1, 0, 1, 100, 0, 0, 0, 0), (2, 0, 1, 50, 0, 0, 0, 0));
            var svc = new AchievementService(reg);
            svc.TrackProgress(1, 1, 100);
            svc.TrackProgress(1, 2, 50);
            Assert.AreEqual(2, svc.GetPlayerCompletedCount(1));
        }

        // ── Lookup APIs ─────────────────────────────────────────────────────

        [Test]
        public void GetAchievement_NotFound_ReturnsNull()
        {
            var svc = new AchievementService();
            Assert.IsNull(svc.GetAchievement(99));
        }

        [Test]
        public void GetAchievement_Exists()
        {
            var reg = BuildRegistry((1, 0, 1, 100, 0, 0, 0, 0));
            var svc = new AchievementService(reg);
            var a = svc.GetAchievement(1);
            Assert.IsNotNull(a);
            Assert.AreEqual(1, a.achievementId);
        }

        [Test]
        public void GetByCategory_NoRegistry_ReturnsEmpty()
        {
            var svc = new AchievementService();
            Assert.AreEqual(0, svc.GetByCategory(0).Count);
        }

        [Test]
        public void All_NoRegistry_ReturnsEmpty()
        {
            var svc = new AchievementService();
            Assert.AreEqual(0, svc.All.Count);
        }

        // ── AttachHost ──────────────────────────────────────────────────────

        [Test]
        public void AttachHost_ReplacesHost()
        {
            var host1 = new FakeHost();
            var host2 = new FakeHost();
            var reg = BuildRegistry((1, 0, 1, 100, 0, 0, 0, 0));
            var svc = new AchievementService(reg, host1);
            svc.AttachHost(host2);
            svc.TrackProgress(1, 1, 30);
            Assert.AreEqual(0, host1.ShowCalls);
            Assert.AreEqual(1, host2.ShowCalls);
        }
    }
}
