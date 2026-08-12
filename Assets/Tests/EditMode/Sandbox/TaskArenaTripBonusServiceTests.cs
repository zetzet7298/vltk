// -----------------------------------------------------------------------------
// VLTK Mobile — EditMode tests for Random/Partner/Metempsychosis/Arena/Trip/Bonus services
// Vietnamese: Kiểm thử 6 service mới (task + arena + trip + bonus online).
// -----------------------------------------------------------------------------

using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class RandomTaskServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => RandomTaskService.LoadFromStreamingAssets());
            var svc = RandomTaskService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0, "Service khởi tạo OK ngay cả khi data rỗng");
        }

        [Test]
        public void GetTasksForLevel_FiltersCorrectly()
        {
            // Build registry giả để test filter
            var reg = new PcRandomTaskRegistry();
            reg.Register(new PcRandomTaskEntry { taskId = 1, taskType = 0, minLevel = 10, maxLevel = 20, targetId = 100, targetCount = 5 });
            reg.Register(new PcRandomTaskEntry { taskId = 2, taskType = 1, minLevel = 30, maxLevel = 50, targetId = 200, targetCount = 3 });
            reg.Register(new PcRandomTaskEntry { taskId = 3, taskType = 0, minLevel = 1, maxLevel = 0, targetId = 300, targetCount = 1 });
            var svc = new RandomTaskService(reg);

            // task3 has maxLevel=0 (open-range) so it matches EVERY level by design
            // (see at5 assertion below). Level filter must therefore return the
            // in-range task plus the open-range task.
            var at15 = svc.GetTasksForLevel(15);
            Assert.AreEqual(2, at15.Count, "Cấp 15: task 10-20 + task open-range");
            Assert.IsTrue(at15.Any(t => t.taskId == 1), "task1 (10-20) phải match cấp 15");
            Assert.IsTrue(at15.Any(t => t.taskId == 3), "task3 (open-range) phải match mọi cấp");

            var at40 = svc.GetTasksForLevel(40);
            Assert.AreEqual(2, at40.Count, "Cấp 40: task 30-50 + task open-range");
            Assert.IsTrue(at40.Any(t => t.taskId == 2), "task2 (30-50) phải match cấp 40");
            Assert.IsTrue(at40.Any(t => t.taskId == 3), "task3 (open-range) phải match mọi cấp");

            var at5 = svc.GetTasksForLevel(5);
            // Task id=3 có minLevel=1 maxLevel=0 (không giới hạn trên) → match
            Assert.GreaterOrEqual(at5.Count, 1, "Task open-range (maxLevel=0) phải match mọi cấp");
        }
    }

    public class PartnerTaskServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => PartnerTaskService.LoadFromStreamingAssets());
            var svc = PartnerTaskService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }

        [Test]
        public void GetTasksForPartner_FiltersCorrectly()
        {
            var reg = new PcPartnerTaskRegistry();
            reg.Register(new PcPartnerTaskEntry { taskId = 1, partnerId = 10, taskType = 0, targetId = 100, targetCount = 5 });
            reg.Register(new PcPartnerTaskEntry { taskId = 2, partnerId = 10, taskType = 1, targetId = 200, targetCount = 3 });
            reg.Register(new PcPartnerTaskEntry { taskId = 3, partnerId = 20, taskType = 0, targetId = 300, targetCount = 1 });
            var svc = new PartnerTaskService(reg);

            var list = svc.GetTasksForPartner(10);
            Assert.AreEqual(2, list.Count, "Pet 10 có 2 tasks");
            foreach (var t in list) Assert.AreEqual(10, t.partnerId);

            var none = svc.GetTasksForPartner(999);
            Assert.AreEqual(0, none.Count, "Pet 999 không có task");
        }
    }

    public class MetempsychosisTaskServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => MetempsychosisTaskService.LoadFromStreamingAssets());
            var svc = MetempsychosisTaskService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }

        [Test]
        public void GetTasksForLevel_FiltersCorrectly()
        {
            var reg = new PcMetempsychosisTaskRegistry();
            reg.Register(new PcMetempsychosisTaskEntry { taskId = 1, requiredLevel = 90, requiredTranslifeCount = 0, taskType = 0, targetId = 100, targetCount = 1 });
            reg.Register(new PcMetempsychosisTaskEntry { taskId = 2, requiredLevel = 120, requiredTranslifeCount = 1, taskType = 0, targetId = 200, targetCount = 1 });
            reg.Register(new PcMetempsychosisTaskEntry { taskId = 3, requiredLevel = 150, requiredTranslifeCount = 4, taskType = 0, targetId = 300, targetCount = 1 });
            var svc = new MetempsychosisTaskService(reg);

            var at100 = svc.GetTasksForLevel(100);
            Assert.AreEqual(1, at100.Count, "Cấp 100 chỉ match task requiredLevel=90");
            Assert.AreEqual(1, at100[0].taskId);

            var at125 = svc.GetTasksForLevel(125);
            Assert.AreEqual(2, at125.Count, "Cấp 125 match task 90 + 120");

            var at50 = svc.GetTasksForLevel(50);
            Assert.AreEqual(0, at50.Count, "Cấp 50 chưa đủ cho bất kỳ task nào");
        }
    }

    public class ArenaServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => ArenaService.LoadFromStreamingAssets());
            var svc = ArenaService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }

        [Test]
        public void CanEnter_RejectsLevelMismatch()
        {
            var reg = new PcArenaRegistry();
            reg.Register(new PcArenaEntry { arenaId = 1, mapId = 100, minLevel = 30, maxLevel = 50, minRating = 1000, maxRating = 2000, rewardId = 50, rewardCount = 1, resetHour = 0 });
            reg.Register(new PcArenaEntry { arenaId = 2, mapId = 100, minLevel = 50, maxLevel = 70, minRating = 2000, maxRating = 3000, rewardId = 60, rewardCount = 1, resetHour = 0 });
            var svc = new ArenaService(reg);

            // Cấp 25 → fail (dưới minLevel=30)
            Assert.IsFalse(svc.CanEnter(1, playerLevel: 25, rating: 1500), "Cấp 25 dưới minLevel 30 → false");
            // Cấp 60 → fail (trên maxLevel=50)
            Assert.IsFalse(svc.CanEnter(1, playerLevel: 60, rating: 1500), "Cấp 60 trên maxLevel 50 → false");
            // Cấp 40, rating 1500 → pass
            Assert.IsTrue(svc.CanEnter(1, playerLevel: 40, rating: 1500), "Cấp 40 + rating 1500 → pass");
            // Cấp 40, rating 500 → fail (dưới minRating 1000)
            Assert.IsFalse(svc.CanEnter(1, playerLevel: 40, rating: 500), "Rating 500 dưới minRating 1000 → false");
            // Arena không tồn tại
            Assert.IsFalse(svc.CanEnter(999, 50, 1500), "Arena 999 không tồn tại");
        }
    }

    public class TripServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => TripService.LoadFromStreamingAssets());
            var svc = TripService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }

        [Test]
        public void GetTripsFromMap_FiltersCorrectly()
        {
            var reg = new PcTripRegistry();
            reg.Register(new PcTripEntry { tripId = 1, startMapId = 100, endMapId = 200, durationSec = 60, rewardExp = 1000, rewardSilver = 500, requiredItem = 0 });
            reg.Register(new PcTripEntry { tripId = 2, startMapId = 100, endMapId = 300, durationSec = 120, rewardExp = 2000, rewardSilver = 1000, requiredItem = 0 });
            reg.Register(new PcTripEntry { tripId = 3, startMapId = 400, endMapId = 500, durationSec = 90, rewardExp = 1500, rewardSilver = 750, requiredItem = 0 });
            var svc = new TripService(reg);

            var from100 = svc.GetTripsFromMap(100);
            Assert.AreEqual(2, from100.Count, "Map 100 có 2 trips");
            foreach (var t in from100) Assert.AreEqual(100, t.startMapId);

            var from999 = svc.GetTripsFromMap(999);
            Assert.AreEqual(0, from999.Count, "Map 999 không có trip");
        }
    }

    public class BonusOnlineServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => BonusOnlineService.LoadFromStreamingAssets());
            var svc = BonusOnlineService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }

        [Test]
        public void CanClaim_RejectsInsufficientTime()
        {
            var reg = new PcBonusOnlineRegistry();
            reg.Register(new PcBonusOnlineEntry { bonusId = 1, requiredMinutes = 30, rewardType = 0, rewardId = 0, rewardCount = 1000, vipRequired = 0 });
            reg.Register(new PcBonusOnlineEntry { bonusId = 2, requiredMinutes = 60, rewardType = 1, rewardId = 0, rewardCount = 500, vipRequired = 0 });
            reg.Register(new PcBonusOnlineEntry { bonusId = 3, requiredMinutes = 60, rewardType = 2, rewardId = 999, rewardCount = 1, vipRequired = 3 });
            var svc = new BonusOnlineService(reg);

            // 20 phút → không đủ cho bonus nào
            Assert.IsFalse(svc.CanClaim(1, minutes: 20, vipLevel: 0));
            Assert.IsFalse(svc.CanClaim(2, minutes: 20, vipLevel: 0));

            // 30 phút → bonus 1 OK
            Assert.IsTrue(svc.CanClaim(1, minutes: 30, vipLevel: 0));
            Assert.IsFalse(svc.CanClaim(2, minutes: 30, vipLevel: 0), "Bonus 2 cần 60 phút");

            // 60 phút + VIP 0 → bonus 2 OK, bonus 3 chưa (cần VIP 3)
            Assert.IsTrue(svc.CanClaim(2, minutes: 60, vipLevel: 0));
            Assert.IsFalse(svc.CanClaim(3, minutes: 60, vipLevel: 0), "Bonus 3 cần VIP 3");
            Assert.IsTrue(svc.CanClaim(3, minutes: 60, vipLevel: 5), "VIP 5 đủ điều kiện");

            // Claim → không claim lại được
            Assert.IsTrue(svc.MarkClaimed(1));
            Assert.IsFalse(svc.CanClaim(1, minutes: 999, vipLevel: 0), "Đã claim rồi");

            // Bonus không tồn tại
            Assert.IsFalse(svc.CanClaim(999, 9999, 99));
        }
    }
}
