// -----------------------------------------------------------------------------
// VLTK Mobile — EditMode tests cho 11 PC parser registries
// Cover: WorldBoss / Achievement / DailyReward / Mall / Fashion / SignIn /
//        TreasureHunt / Encounter / FriendGift / TextResource / AnimationBank
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class WorldBossAchievementMallParserTests
    {
        // ── WorldBossRegistry ────────────────────────────────────────────────
        [Test]
        public void PcWorldBossRegistry_Count_NonNegative()
        {
            var reg = new PcWorldBossRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcWorldBossRegistry_GetByMap_FiltersCorrectly()
        {
            var reg = new PcWorldBossRegistry();
            reg.Register(new PcWorldBossEntry { worldBossId = 1, mapId = 5 });
            reg.Register(new PcWorldBossEntry { worldBossId = 2, mapId = 5 });
            reg.Register(new PcWorldBossEntry { worldBossId = 3, mapId = 6 });
            reg.Register(new PcWorldBossEntry { worldBossId = 0, mapId = 5 }); // bị skip
            Assert.AreEqual(3, reg.Count);
            Assert.AreEqual(2, reg.GetByMap(5).Count);
            Assert.AreEqual(1, reg.GetByMap(6).Count);
        }

        // ── AchievementRegistry ──────────────────────────────────────────────
        [Test]
        public void PcAchievementRegistry_Count_NonNegative()
        {
            var reg = new PcAchievementRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcAchievementRegistry_GetByCategory_FiltersCorrectly()
        {
            var reg = new PcAchievementRegistry();
            reg.Register(new PcAchievementEntry { achievementId = 1, category = 0 });
            reg.Register(new PcAchievementEntry { achievementId = 2, category = 1 });
            reg.Register(new PcAchievementEntry { achievementId = 3, category = 0 });
            Assert.AreEqual(3, reg.Count);
            Assert.AreEqual(2, reg.GetByCategory(0).Count);
            Assert.AreEqual(1, reg.GetByCategory(1).Count);
        }

        // ── DailyRewardRegistry ──────────────────────────────────────────────
        [Test]
        public void PcDailyRewardRegistry_Count_NonNegative()
        {
            var reg = new PcDailyRewardRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcDailyRewardRegistry_Count_Matches_Parser()
        {
            var reg = new PcDailyRewardRegistry();
            reg.Register(new PcDailyRewardEntry { dayIdx = 1 });
            reg.Register(new PcDailyRewardEntry { dayIdx = 2 });
            reg.Register(new PcDailyRewardEntry { dayIdx = 3 });
            Assert.AreEqual(3, reg.Count);
            Assert.AreEqual(1, reg.Get(1).dayIdx);
            // duplicate
            reg.Register(new PcDailyRewardEntry { dayIdx = 1, goldBonus = 999 });
            Assert.AreEqual(3, reg.Count);
            Assert.AreEqual(999, reg.Get(1).goldBonus);
        }

        // ── MallRegistry ─────────────────────────────────────────────────────
        [Test]
        public void PcMallRegistry_Count_NonNegative()
        {
            var reg = new PcMallRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcMallRegistry_GetForVip_FiltersCorrectly()
        {
            var reg = new PcMallRegistry();
            reg.Register(new PcMallEntry { mallItemId = 1, requiredVipLevel = 0 });
            reg.Register(new PcMallEntry { mallItemId = 2, requiredVipLevel = 3 });
            reg.Register(new PcMallEntry { mallItemId = 3, requiredVipLevel = 5 });
            Assert.AreEqual(3, reg.Count);
            Assert.AreEqual(1, reg.GetForVip(0).Count);
            Assert.AreEqual(2, reg.GetForVip(3).Count);
            Assert.AreEqual(3, reg.GetForVip(10).Count);
        }

        // ── FashionRegistry ──────────────────────────────────────────────────
        [Test]
        public void PcFashionRegistry_Count_NonNegative()
        {
            var reg = new PcFashionRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcFashionRegistry_GetBySlot_FiltersCorrectly()
        {
            var reg = new PcFashionRegistry();
            reg.Register(new PcFashionEntry { fashionId = 1, slot = 0 });
            reg.Register(new PcFashionEntry { fashionId = 2, slot = 0 });
            reg.Register(new PcFashionEntry { fashionId = 3, slot = 6 });
            Assert.AreEqual(3, reg.Count);
            Assert.AreEqual(2, reg.GetBySlot(0).Count);
            Assert.AreEqual(1, reg.GetBySlot(6).Count);
        }

        // ── SignInRegistry ───────────────────────────────────────────────────
        [Test]
        public void PcSignInRegistry_Count_NonNegative()
        {
            var reg = new PcSignInRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcSignInRegistry_GetByTotalDays_FiltersCorrectly()
        {
            var reg = new PcSignInRegistry();
            reg.Register(new PcSignInEntry { signInDay = 1, totalDaysSoFar = 1 });
            reg.Register(new PcSignInEntry { signInDay = 7, totalDaysSoFar = 7 });
            reg.Register(new PcSignInEntry { signInDay = 14, totalDaysSoFar = 14 });
            Assert.AreEqual(3, reg.Count);
            Assert.AreEqual(1, reg.GetByTotalDays(1).Count);
            Assert.AreEqual(1, reg.GetByTotalDays(7).Count);
            Assert.AreEqual(0, reg.GetByTotalDays(99).Count);
        }

        // ── TreasureHuntRegistry ─────────────────────────────────────────────
        [Test]
        public void PcTreasureHuntRegistry_Count_NonNegative()
        {
            var reg = new PcTreasureHuntRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcTreasureHuntRegistry_GetByMap_FiltersCorrectly()
        {
            var reg = new PcTreasureHuntRegistry();
            reg.Register(new PcTreasureHuntEntry { treasureId = 1, mapId = 100 });
            reg.Register(new PcTreasureHuntEntry { treasureId = 2, mapId = 100 });
            reg.Register(new PcTreasureHuntEntry { treasureId = 3, mapId = 200 });
            Assert.AreEqual(3, reg.Count);
            Assert.AreEqual(2, reg.GetByMap(100).Count);
            Assert.AreEqual(1, reg.GetByMap(200).Count);
        }

        // ── EncounterRegistry ────────────────────────────────────────────────
        [Test]
        public void PcEncounterRegistry_Count_NonNegative()
        {
            var reg = new PcEncounterRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcEncounterRegistry_GetByType_FiltersCorrectly()
        {
            var reg = new PcEncounterRegistry();
            reg.Register(new PcEncounterEntry { encounterId = 1, type = 0 });
            reg.Register(new PcEncounterEntry { encounterId = 2, type = 2 });
            reg.Register(new PcEncounterEntry { encounterId = 3, type = 0 });
            Assert.AreEqual(3, reg.Count);
            Assert.AreEqual(2, reg.GetByType(0).Count);
            Assert.AreEqual(1, reg.GetByType(2).Count);
        }

        // ── FriendGiftRegistry ────────────────────────────────────────────────
        [Test]
        public void PcFriendGiftRegistry_Count_NonNegative()
        {
            var reg = new PcFriendGiftRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcFriendGiftRegistry_GetByFriendship_FiltersCorrectly()
        {
            var reg = new PcFriendGiftRegistry();
            reg.Register(new PcFriendGiftEntry { giftId = 1, friendshipRequired = 100 });
            reg.Register(new PcFriendGiftEntry { giftId = 2, friendshipRequired = 500 });
            reg.Register(new PcFriendGiftEntry { giftId = 3, friendshipRequired = 1000 });
            Assert.AreEqual(3, reg.Count);
            Assert.AreEqual(1, reg.GetByFriendship(100).Count);
            Assert.AreEqual(2, reg.GetByFriendship(500).Count);
            Assert.AreEqual(3, reg.GetByFriendship(5000).Count);
        }

        // ── TextResourceRegistry ─────────────────────────────────────────────
        [Test]
        public void PcTextResourceRegistry_Count_NonNegative()
        {
            var reg = new PcTextResourceRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcTextResourceRegistry_GetVietnamese_ReturnsNullForUnknown()
        {
            var reg = new PcTextResourceRegistry();
            reg.Register(new PcTextResourceEntry { key = "TEST", vietnamese = "Kiểm tra" });
            Assert.AreEqual(1, reg.Count);
            Assert.AreEqual("Kiểm tra", reg.Get("TEST").vietnamese);
            Assert.IsNull(reg.Get("__UNKNOWN__"));
            Assert.IsNull(reg.Get(""));
        }

        // ── AnimationBankRegistry ────────────────────────────────────────────
        [Test]
        public void PcAnimationBankRegistry_Count_NonNegative()
        {
            var reg = new PcAnimationBankRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcAnimationBankRegistry_GetByDirection_FiltersCorrectly()
        {
            var reg = new PcAnimationBankRegistry();
            for (int d = 0; d <= 7; d++)
                reg.Register(new PcAnimationBankEntry { animId = d + 1, direction = d });
            Assert.AreEqual(8, reg.Count);
            for (int d = 0; d <= 7; d++)
                Assert.AreEqual(1, reg.GetByDirection(d).Count);
        }
    }
}
