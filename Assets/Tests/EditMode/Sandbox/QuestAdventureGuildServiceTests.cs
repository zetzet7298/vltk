// -----------------------------------------------------------------------------
// VLTK Mobile — EditMode tests cho QuestItemService, AdventureService, GuildService
// -----------------------------------------------------------------------------

using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class QuestItemServiceTests
    {
        private static QuestItemService MakeService()
        {
            return new QuestItemService(PcQuestItemParser.BuildRegistry(
                Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcItemFull")));
        }

        [Test]
        public void LoadFromStreamingAssets_LoadsItems()
        {
            var svc = QuestItemService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            int id = QuestItemService.EncodeItemId(99, 99, 99);
            svc.AddQuestItem(id, 1);
            Assert.AreEqual(1, svc.GetQuestItemCount(id));
        }

        [Test]
        public void AddAndRemoveQuestItem_TracksInventory()
        {
            var svc = MakeService();
            int fakeId = QuestItemService.EncodeItemId(1, 2, 3);
            int fired = 0;
            svc.OnQuestItemChanged += (id, oldV, newV) => fired++;

            Assert.AreEqual(0, svc.GetQuestItemCount(fakeId));
            svc.AddQuestItem(fakeId, 5);
            Assert.AreEqual(5, svc.GetQuestItemCount(fakeId));
            Assert.AreEqual(1, fired);

            Assert.IsTrue(svc.RemoveQuestItem(fakeId, 2));
            Assert.AreEqual(3, svc.GetQuestItemCount(fakeId));
            Assert.AreEqual(2, fired);

            // Trừ quá số lượng → false, count không đổi
            Assert.IsFalse(svc.RemoveQuestItem(fakeId, 99));
            Assert.AreEqual(3, svc.GetQuestItemCount(fakeId));
        }

        [Test]
        public void HasQuestItem_ReturnsCorrectly()
        {
            var svc = new QuestItemService(new PcQuestItemRegistry());
            int id = 42;
            Assert.IsTrue(svc.HasQuestItem(id, 0));
            Assert.IsFalse(svc.HasQuestItem(id, 1));

            svc.AddQuestItem(id, 5);
            Assert.IsTrue(svc.HasQuestItem(id, 1));
            Assert.IsTrue(svc.HasQuestItem(id, 5));
            Assert.IsFalse(svc.HasQuestItem(id, 6));
        }
    }

    public class AdventureServiceTests
    {
        private static AdventureService MakeService()
        {
            return new AdventureService(PcAdventureParser.BuildRegistry(
                Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcAdventure")));
        }

        [Test]
        public void LoadFromStreamingAssets_LoadsAdventures()
        {
            var svc = AdventureService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 1000, "PC adventure.txt có 1,037 mục");
        }

        [Test]
        public void MarkCompleted_AddsToSet()
        {
            var svc = MakeService();
            int fired = 0;
            int firedId = -1;
            svc.OnAdventureCompleted += id => { fired++; firedId = id; };

            Assert.IsTrue(svc.MarkCompleted(1));
            Assert.IsFalse(svc.MarkCompleted(1), "Gọi lần 2 phải trả false");
            Assert.AreEqual(1, fired);
            Assert.AreEqual(1, firedId);
            Assert.AreEqual(1, svc.CompletedCount);
        }

        [Test]
        public void IsCompleted_ReflectsSetState()
        {
            var svc = new AdventureService(new PcAdventureRegistry());
            Assert.IsFalse(svc.IsCompleted(7));
            svc.MarkCompleted(7);
            Assert.IsTrue(svc.IsCompleted(7));
            Assert.IsFalse(svc.IsCompleted(8));
        }
    }

    public class GuildServiceTests
    {
        private static GuildService MakeService()
        {
            return new GuildService(PcTongLevelParser.BuildRegistry(
                Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcTong")));
        }

        [Test]
        public void LoadFromStreamingAssets_LoadsLevels()
        {
            var svc = GuildService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            // tong_level_data.txt có ~6 cấp mặc định
            Assert.GreaterOrEqual(svc.Count, 5);
            Assert.GreaterOrEqual(svc.MaxLevel, 5);
        }

        [Test]
        public void CanUpgrade_FalseWhenInsufficientFunds()
        {
            var svc = MakeService();
            int target = Math.Min(svc.MaxLevel, 2);
            if (target <= svc.GuildLevel) return; // skip nếu max < current
            int cost = svc.GetUpgradeCost(target);
            Assert.IsFalse(svc.CanUpgrade(target, cost - 1));
            Assert.IsTrue(svc.CanUpgrade(target, cost));
        }

        [Test]
        public void TryUpgrade_SuccessWhenEnoughFunds()
        {
            var svc = MakeService();
            int target = Math.Min(svc.MaxLevel, 2);
            if (target <= svc.GuildLevel) return; // skip
            int cost = svc.GetUpgradeCost(target);

            int fired = 0;
            svc.OnGuildUpgraded += (o, n) => fired++;

            var result = svc.TryUpgrade(target, cost);
            Assert.AreEqual(GuildUpgradeResult.Success, result);
            Assert.AreEqual(target, svc.GuildLevel);
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void GetMaxAffordableLevel_HighestAffordable()
        {
            var svc = MakeService();
            // Với funds = 0 thì vẫn ở cấp 1
            Assert.AreEqual(1, svc.GetMaxAffordableLevel(0));
            int big = 1_000_000_000;
            int max = svc.GetMaxAffordableLevel(big);
            Assert.GreaterOrEqual(max, svc.GuildLevel);
            Assert.LessOrEqual(max, svc.MaxLevel);
        }
    }
}
