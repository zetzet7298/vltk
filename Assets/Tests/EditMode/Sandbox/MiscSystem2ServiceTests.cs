// -----------------------------------------------------------------------------
// VLTK Mobile — EditMode tests cho MiscSystem2 (batch cuối: NewPlayerGuide,
// ChangeFeature, Stall, FlipCard, BaoRuongThanBi, SeasonalEvent, Compensation).
// Vietnamese: Kiểm thử các service hệ thống phụ / sự kiện cuối cùng.
// -----------------------------------------------------------------------------

using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class NewPlayerGuideServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = NewPlayerGuideService.LoadFromStreamingAssets();
                Assert.IsNotNull(svc);
            }, "NewPlayerGuideService phải khởi tạo được ngay cả khi thư mục rỗng");
        }

        [Test]
        public void GetForLevel_FiltersCorrectly()
        {
            var reg = new PcNewPlayerGuideRegistry();
            reg.Register(new PcNewPlayerGuideEntry { guideId = 1, requiredLevel = 1, step = 1 });
            reg.Register(new PcNewPlayerGuideEntry { guideId = 2, requiredLevel = 10, step = 2 });
            reg.Register(new PcNewPlayerGuideEntry { guideId = 3, requiredLevel = 30, step = 3 });
            var svc = new NewPlayerGuideService(reg);

            var lv5 = svc.GetForLevel(5);
            int countLv5 = 0; foreach (var _ in lv5) countLv5++;
            Assert.AreEqual(1, countLv5, "Level 5 chỉ thấy guideId=1");

            var lv15 = svc.GetForLevel(15);
            int countLv15 = 0; foreach (var _ in lv15) countLv15++;
            Assert.AreEqual(2, countLv15, "Level 15 thấy guideId 1+2");

            var lv50 = svc.GetForLevel(50);
            int countLv50 = 0; foreach (var _ in lv50) countLv50++;
            Assert.AreEqual(3, countLv50, "Level 50 thấy cả 3 guide");
        }
    }

    public class ChangeFeatureServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = ChangeFeatureService.LoadFromStreamingAssets();
                Assert.IsNotNull(svc);
            });
        }

        [Test]
        public void Count_NonNegative()
        {
            var svc = new ChangeFeatureService(new PcChangeFeatureRegistry());
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class StallServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = StallService.LoadFromStreamingAssets();
                Assert.IsNotNull(svc);
            });
        }

        [Test]
        public void GetStall_ReturnsNullForInvalid()
        {
            var svc = new StallService(new PcStallRegistry());
            Assert.IsNull(svc.GetStall(999_999));
        }
    }

    public class FlipCardServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = FlipCardService.LoadFromStreamingAssets();
                Assert.IsNotNull(svc);
            });
        }

        [Test]
        public void GetByTier_FiltersCorrectly()
        {
            var reg = new PcFlipCardRegistry();
            reg.Register(new PcFlipCardEntry { cardId = 1, tier = 1, rewardId = 100, probability = 5000 });
            reg.Register(new PcFlipCardEntry { cardId = 2, tier = 2, rewardId = 200, probability = 3000 });
            reg.Register(new PcFlipCardEntry { cardId = 3, tier = 1, rewardId = 300, probability = 2000 });
            var svc = new FlipCardService(reg);

            var tier1 = svc.GetByTier(1);
            Assert.AreEqual(2, tier1.Count);
            foreach (var c in tier1) Assert.AreEqual(1, c.tier);

            var tier2 = svc.GetByTier(2);
            Assert.AreEqual(1, tier2.Count);

            var tier3 = svc.GetByTier(3);
            Assert.AreEqual(0, tier3.Count);
        }
    }

    public class BaoRuongThanBiServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = BaoRuongThanBiService.LoadFromStreamingAssets();
                Assert.IsNotNull(svc);
            });
        }

        [Test]
        public void GetByTier_FiltersCorrectly()
        {
            var reg = new PcBaoRuongThanBiRegistry();
            reg.Register(new PcBaoRuongThanBiEntry { boxId = 1, tier = 1, rewardId = 100 });
            reg.Register(new PcBaoRuongThanBiEntry { boxId = 2, tier = 2, rewardId = 200 });
            reg.Register(new PcBaoRuongThanBiEntry { boxId = 3, tier = 3, rewardId = 300 });
            var svc = new BaoRuongThanBiService(reg);

            var tier2 = svc.GetByTier(2);
            Assert.AreEqual(1, tier2.Count);
            Assert.AreEqual(2, tier2[0].boxId);

            var tier1 = svc.GetByTier(1);
            Assert.AreEqual(1, tier1.Count);
        }
    }

    public class SeasonalEventServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = SeasonalEventService.LoadFromStreamingAssets();
                Assert.IsNotNull(svc);
            });
        }

        [Test]
        public void GetActiveByMonth_FiltersCorrectly()
        {
            var reg = new PcSeasonalEventRegistry();
            reg.Register(new PcSeasonalEventEntry { eventId = 1, startMonth = 1, endMonth = 3, nameRaw = "Tết" });
            reg.Register(new PcSeasonalEventEntry { eventId = 2, startMonth = 5, endMonth = 8, nameRaw = "Hè" });
            reg.Register(new PcSeasonalEventEntry { eventId = 3, startMonth = 11, endMonth = 2, nameRaw = "Đông/Tết" });
            var svc = new SeasonalEventService(reg);

            var feb = svc.GetActiveByMonth(2);
            Assert.AreEqual(2, feb.Count, "Tháng 2: Tết + Đông/Tết");

            var june = svc.GetActiveByMonth(6);
            Assert.AreEqual(1, june.Count);
            Assert.AreEqual(2, june[0].eventId);

            var oct = svc.GetActiveByMonth(10);
            Assert.AreEqual(0, oct.Count);
        }
    }

    public class CompensationServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = CompensationService.LoadFromStreamingAssets();
                Assert.IsNotNull(svc);
            });
        }

        [Test]
        public void GetActive_FiltersCorrectly()
        {
            var reg = new PcCompensationRegistry();
            reg.Register(new PcCompensationEntry { compId = 1, affectedPlayerCount = 100, itemGenre = 1, itemDetail = 1, itemParticular = 1, itemCount = 5, silver = 1000, expireDate = 100 });
            reg.Register(new PcCompensationEntry { compId = 2, affectedPlayerCount = 50, itemGenre = 2, itemDetail = 2, itemParticular = 2, itemCount = 1, silver = 500, expireDate = 200 });
            reg.Register(new PcCompensationEntry { compId = 3, affectedPlayerCount = 10, itemGenre = 3, itemDetail = 3, itemParticular = 3, itemCount = 3, silver = 2000, expireDate = 0 }); // 0 = vô hạn
            var svc = new CompensationService(reg);

            var active50 = svc.GetActive(50);
            Assert.AreEqual(3, active50.Count);

            var active150 = svc.GetActive(150);
            Assert.AreEqual(2, active150.Count, "compId=1 expired (50<100), compId=2 và 3 còn");

            var active300 = svc.GetActive(300);
            Assert.AreEqual(1, active300.Count, "Chỉ compId=3 vô hạn còn");
        }
    }
}
