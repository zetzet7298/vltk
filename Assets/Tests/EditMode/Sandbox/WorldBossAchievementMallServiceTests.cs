// -----------------------------------------------------------------------------
// VLTK Mobile — EditMode tests cho 11 ST services mới
// Cover: WorldBoss / Achievement / DailyReward / Mall / Fashion / SignIn /
//        TreasureHunt / Encounter / FriendGift / TextResource / AnimationBank
// -----------------------------------------------------------------------------

using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class WorldBossAchievementMallServiceTests
    {
        // ── WorldBoss ────────────────────────────────────────────────────────
        [Test]
        public void WorldBossService_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = WorldBossService.LoadFromStreamingAssets();
                Assert.GreaterOrEqual(svc.Count, 0);
            });
        }

        [Test]
        public void WorldBossService_GetByMap_FiltersCorrectly()
        {
            string root = Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcBoss");
            var reg = PcWorldBossParser.BuildRegistry(root);
            var svc = new WorldBossService(reg);
            for (int m = 0; m <= 200; m++)
            {
                var list = svc.GetByMap(m);
                Assert.IsNotNull(list);
                foreach (var b in list) Assert.AreEqual(m, b.mapId);
            }
        }

        [Test]
        public void WorldBossService_ComputeDpsScore_ReturnsValue()
        {
            var svc = new WorldBossService();
            int score = svc.ComputeDpsScore(0, 1000, 5000);
            Assert.AreEqual(200, score);
            // timeMs <= 0: trả về damage * 1000
            Assert.AreEqual(5000, svc.ComputeDpsScore(0, 5, 0));
            // damage = 0: trả về 0
            Assert.AreEqual(0, svc.ComputeDpsScore(0, 0, 100));
        }

        [Test]
        public void WorldBossService_GetActiveBosses_EmptyWhenNoData()
        {
            var svc = new WorldBossService();
            var list = svc.GetActiveBosses(System.DateTime.Now);
            Assert.IsNotNull(list);
            Assert.AreEqual(0, list.Count);
        }

        // ── Achievement ──────────────────────────────────────────────────────
        [Test]
        public void AchievementService_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = AchievementService.LoadFromStreamingAssets();
                Assert.GreaterOrEqual(svc.Count, 0);
            });
        }

        [Test]
        public void AchievementService_GetByCategory_FiltersCorrectly()
        {
            string root = Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcAchievement");
            var reg = PcAchievementParser.BuildRegistry(root);
            var svc = new AchievementService(reg);
            for (int c = 0; c <= 4; c++)
            {
                var list = svc.GetByCategory(c);
                Assert.IsNotNull(list);
                foreach (var a in list) Assert.AreEqual(c, a.category);
            }
        }

        [Test]
        public void AchievementService_CanEarn_RejectsLowLevel()
        {
            var reg = new PcAchievementRegistry();
            reg.Register(new PcAchievementEntry { achievementId = 1, conditionType = 0, conditionValue = 50 });
            var svc = new AchievementService(reg);
            Assert.IsFalse(svc.CanEarn(1, 40, 0));
            Assert.IsTrue(svc.CanEarn(1, 50, 0));
            Assert.IsTrue(svc.CanEarn(1, 60, 0));
            // achievement không tồn tại
            Assert.IsFalse(svc.CanEarn(999, 100, 0));
        }

        [Test]
        public void AchievementService_TryComplete_ReturnsFalse_WhenInsufficient()
        {
            var reg = new PcAchievementRegistry();
            reg.Register(new PcAchievementEntry { achievementId = 1, conditionType = 1, conditionValue = 100 });
            var svc = new AchievementService(reg);
            long p = 0;
            Assert.IsFalse(svc.TryComplete(999, ref p));
            Assert.AreEqual(0, p);
        }

        [Test]
        public void AchievementService_GetCategoryName_NonEmpty()
        {
            var svc = new AchievementService();
            Assert.AreEqual("Chiến đấu", svc.GetCategoryName(0));
            Assert.AreEqual("Nhiệm vụ", svc.GetCategoryName(1));
            Assert.AreEqual("Kỹ năng", svc.GetCategoryName(2));
            Assert.AreEqual("Tương tác", svc.GetCategoryName(3));
            Assert.AreEqual("Sưu tầm", svc.GetCategoryName(4));
            Assert.IsNotEmpty(svc.GetCategoryName(999));
        }

        // ── DailyReward ──────────────────────────────────────────────────────
        [Test]
        public void DailyRewardService_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = DailyRewardService.LoadFromStreamingAssets();
                Assert.GreaterOrEqual(svc.Count, 0);
            });
        }

        [Test]
        public void DailyRewardService_GetStreakReward_NonNull()
        {
            var reg = new PcDailyRewardRegistry();
            reg.Register(new PcDailyRewardEntry { dayIdx = 1, goldBonus = 100 });
            reg.Register(new PcDailyRewardEntry { dayIdx = 7, goldBonus = 5000, requiredVipLevel = 3 });
            reg.Register(new PcDailyRewardEntry { dayIdx = 30, goldBonus = 99999, requiredVipLevel = 5 });
            var svc = new DailyRewardService(reg);
            var streak = svc.GetStreakReward(10, 0);
            Assert.IsNotNull(streak);
            Assert.AreEqual(1, streak.dayIdx); // day 7+30 yêu cầu VIP 3+
            // vip 5
            Assert.AreEqual(30, svc.GetStreakReward(40, 5).dayIdx);
        }

        [Test]
        public void DailyRewardService_CanClaim_RejectsInvalid()
        {
            var reg = new PcDailyRewardRegistry();
            reg.Register(new PcDailyRewardEntry { dayIdx = 1, requiredVipLevel = 0 });
            reg.Register(new PcDailyRewardEntry { dayIdx = 7, requiredVipLevel = 5 });
            var svc = new DailyRewardService(reg);
            Assert.IsTrue(svc.CanClaim(1, 0, 0));
            Assert.IsFalse(svc.CanClaim(7, 0, 4));
            Assert.IsTrue(svc.CanClaim(7, 0, 5));
            Assert.IsFalse(svc.CanClaim(0, 0, 0)); // day invalid
            Assert.IsFalse(svc.CanClaim(99, 0, 0)); // không tồn tại
        }

        [Test]
        public void DailyRewardService_GetTotalDays_NonNegative()
        {
            var svc = new DailyRewardService();
            Assert.GreaterOrEqual(svc.GetTotalDays(), 0);
        }

        // ── Mall ─────────────────────────────────────────────────────────────
        [Test]
        public void MallService_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = MallService.LoadFromStreamingAssets();
                Assert.GreaterOrEqual(svc.Count, 0);
            });
        }

        [Test]
        public void MallService_GetForVip_FiltersCorrectly()
        {
            var reg = new PcMallRegistry();
            reg.Register(new PcMallEntry { mallItemId = 1, price = 100, requiredVipLevel = 0 });
            reg.Register(new PcMallEntry { mallItemId = 2, price = 200, requiredVipLevel = 3 });
            reg.Register(new PcMallEntry { mallItemId = 3, price = 300, requiredVipLevel = 5 });
            var svc = new MallService(reg);
            Assert.AreEqual(1, svc.GetForVip(0).Count);
            Assert.AreEqual(2, svc.GetForVip(3).Count);
            Assert.AreEqual(3, svc.GetForVip(5).Count);
        }

        [Test]
        public void MallService_CanBuy_RejectsInsufficientVip()
        {
            var reg = new PcMallRegistry();
            reg.Register(new PcMallEntry { mallItemId = 1, price = 100, requiredVipLevel = 5, stock = 10, maxBuyPerDay = 5 });
            var svc = new MallService(reg);
            Assert.IsFalse(svc.CanBuy(1, 4, 0));
            Assert.IsTrue(svc.CanBuy(1, 5, 0));
            Assert.IsTrue(svc.CanBuy(1, 5, 4));
            Assert.IsFalse(svc.CanBuy(1, 5, 5)); // đã mua đủ ngày
            Assert.IsFalse(svc.CanBuy(999, 10, 0)); // không tồn tại
        }

        [Test]
        public void MallService_GetEffectivePrice_RejectsInvalid()
        {
            var reg = new PcMallRegistry();
            reg.Register(new PcMallEntry { mallItemId = 1, price = 1000, requiredVipLevel = 5, discount = 20 });
            reg.Register(new PcMallEntry { mallItemId = 2, price = 1000, requiredVipLevel = 0, discount = 0 });
            reg.Register(new PcMallEntry { mallItemId = 3, price = 1000, requiredVipLevel = 0, discount = 100 });
            var svc = new MallService(reg);
            Assert.AreEqual(-1, svc.GetEffectivePrice(1, 4)); // vip thấp
            Assert.AreEqual(800, svc.GetEffectivePrice(1, 5)); // 20% off
            Assert.AreEqual(1000, svc.GetEffectivePrice(2, 0)); // không giảm
            Assert.AreEqual(0, svc.GetEffectivePrice(3, 0)); // 100% off
            Assert.AreEqual(-1, svc.GetEffectivePrice(999, 10));
        }

        [Test]
        public void MallService_IsOnSale_TrueForActiveSale()
        {
            var reg = new PcMallRegistry();
            reg.Register(new PcMallEntry { mallItemId = 1, price = 100 }); // luôn sale
            reg.Register(new PcMallEntry { mallItemId = 2, price = 100, startTimeUnix = 1000, endTimeUnix = 2000 });
            var svc = new MallService(reg);
            Assert.IsTrue(svc.IsOnSale(1, 500));
            Assert.IsFalse(svc.IsOnSale(2, 500)); // chưa tới start
            Assert.IsTrue(svc.IsOnSale(2, 1500)); // trong khoảng
            Assert.IsFalse(svc.IsOnSale(2, 3000)); // quá hạn
            Assert.IsFalse(svc.IsOnSale(999, 500));
        }

        // ── Fashion ──────────────────────────────────────────────────────────
        [Test]
        public void FashionService_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = FashionService.LoadFromStreamingAssets();
                Assert.GreaterOrEqual(svc.Count, 0);
            });
        }

        [Test]
        public void FashionService_GetBySlot_FiltersCorrectly()
        {
            var reg = new PcFashionRegistry();
            reg.Register(new PcFashionEntry { fashionId = 1, slot = 0 });
            reg.Register(new PcFashionEntry { fashionId = 2, slot = 6 });
            var svc = new FashionService(reg);
            Assert.AreEqual(1, svc.GetBySlot(0).Count);
            Assert.AreEqual(1, svc.GetBySlot(6).Count);
            Assert.AreEqual(0, svc.GetBySlot(2).Count);
        }

        [Test]
        public void FashionService_CanEquip_RejectsWrongSex()
        {
            var reg = new PcFashionRegistry();
            reg.Register(new PcFashionEntry { fashionId = 1, requiredLevel = 10, requiredSex = 0, requiredVipLevel = 0 });
            reg.Register(new PcFashionEntry { fashionId = 2, requiredLevel = 0, requiredSex = -1, requiredVipLevel = 0 });
            var svc = new FashionService(reg);
            Assert.IsFalse(svc.CanEquip(1, 10, 1, 0)); // sai sex
            Assert.IsTrue(svc.CanEquip(1, 10, 0, 0));
            Assert.IsFalse(svc.CanEquip(1, 9, 0, 0)); // sai level
            Assert.IsTrue(svc.CanEquip(2, 1, 0, 0)); // unisex
            Assert.IsTrue(svc.CanEquip(2, 1, 1, 0));
            Assert.IsFalse(svc.CanEquip(999, 50, 0, 0));
        }

        [Test]
        public void FashionService_GetSlotName_NonEmpty()
        {
            var svc = new FashionService();
            Assert.AreEqual("Tóc", svc.GetSlotName(0));
            Assert.AreEqual("Mặt", svc.GetSlotName(1));
            Assert.AreEqual("Thân", svc.GetSlotName(2));
            Assert.AreEqual("Tay", svc.GetSlotName(3));
            Assert.AreEqual("Chân", svc.GetSlotName(4));
            Assert.AreEqual("Áo choàng", svc.GetSlotName(5));
            Assert.AreEqual("Vũ khí", svc.GetSlotName(6));
            Assert.IsNotEmpty(svc.GetSlotName(99));
        }

        // ── SignIn ───────────────────────────────────────────────────────────
        [Test]
        public void SignInService_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = SignInService.LoadFromStreamingAssets();
                Assert.GreaterOrEqual(svc.Count, 0);
            });
        }

        [Test]
        public void SignInService_CanSignIn_RejectsInvalid()
        {
            var reg = new PcSignInRegistry();
            reg.Register(new PcSignInEntry { signInDay = 1, totalDaysSoFar = 1 });
            reg.Register(new PcSignInEntry { signInDay = 7, totalDaysSoFar = 7, isDouble = true });
            var svc = new SignInService(reg);
            Assert.IsTrue(svc.CanSignIn(1, 0, 0));
            Assert.IsFalse(svc.CanSignIn(1, 1, 1)); // đã điểm danh
            Assert.IsFalse(svc.CanSignIn(0, 0, 0));
            Assert.IsFalse(svc.CanSignIn(99, 0, 0));
        }

        [Test]
        public void SignInService_GetRewardForTotalDays_NonNull()
        {
            var reg = new PcSignInRegistry();
            reg.Register(new PcSignInEntry { signInDay = 1, totalDaysSoFar = 1 });
            reg.Register(new PcSignInEntry { signInDay = 7, totalDaysSoFar = 7 });
            var svc = new SignInService(reg);
            var reward = svc.GetRewardForTotalDays(5);
            Assert.IsNotNull(reward);
            Assert.AreEqual(1, reward.signInDay);
            var reward2 = svc.GetRewardForTotalDays(10);
            Assert.AreEqual(7, reward2.signInDay);
        }

        [Test]
        public void SignInService_IsDouble_RejectsInvalid()
        {
            var reg = new PcSignInRegistry();
            reg.Register(new PcSignInEntry { signInDay = 1, isDouble = false });
            reg.Register(new PcSignInEntry { signInDay = 7, isDouble = true });
            var svc = new SignInService(reg);
            Assert.IsFalse(svc.IsDouble(1));
            Assert.IsTrue(svc.IsDouble(7));
            Assert.IsFalse(svc.IsDouble(999));
        }

        // ── TreasureHunt ─────────────────────────────────────────────────────
        [Test]
        public void TreasureHuntService_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = TreasureHuntService.LoadFromStreamingAssets();
                Assert.GreaterOrEqual(svc.Count, 0);
            });
        }

        [Test]
        public void TreasureHuntService_GetByMap_FiltersCorrectly()
        {
            var reg = new PcTreasureHuntRegistry();
            reg.Register(new PcTreasureHuntEntry { treasureId = 1, mapId = 100, posX = 0, posY = 0 });
            reg.Register(new PcTreasureHuntEntry { treasureId = 2, mapId = 100, posX = 100, posY = 100 });
            reg.Register(new PcTreasureHuntEntry { treasureId = 3, mapId = 200, posX = 50, posY = 50 });
            var svc = new TreasureHuntService(reg);
            Assert.AreEqual(2, svc.GetByMap(100).Count);
            Assert.AreEqual(1, svc.GetByMap(200).Count);
            Assert.AreEqual(0, svc.GetByMap(999).Count);
        }

        [Test]
        public void TreasureHuntService_GetNearbyTreasures_EmptyWhenFar()
        {
            var reg = new PcTreasureHuntRegistry();
            reg.Register(new PcTreasureHuntEntry { treasureId = 1, mapId = 100, posX = 0, posY = 0 });
            reg.Register(new PcTreasureHuntEntry { treasureId = 2, mapId = 100, posX = 1000, posY = 1000 });
            var svc = new TreasureHuntService(reg);
            Assert.AreEqual(1, svc.GetNearbyTreasures(100, 0, 0, 50).Count);
            Assert.AreEqual(0, svc.GetNearbyTreasures(100, 500, 500, 50).Count);
            Assert.AreEqual(2, svc.GetNearbyTreasures(100, 0, 0, 2000).Count);
            Assert.AreEqual(0, svc.GetNearbyTreasures(999, 0, 0, 100).Count); // sai map
        }

        [Test]
        public void TreasureHuntService_CanDig_RejectsLowLevel()
        {
            var reg = new PcTreasureHuntRegistry();
            reg.Register(new PcTreasureHuntEntry { treasureId = 1, requiredLevel = 30 });
            var svc = new TreasureHuntService(reg);
            Assert.IsFalse(svc.CanDig(1, 29));
            Assert.IsTrue(svc.CanDig(1, 30));
            Assert.IsFalse(svc.CanDig(999, 100));
        }

        // ── Encounter ────────────────────────────────────────────────────────
        [Test]
        public void EncounterService_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = EncounterService.LoadFromStreamingAssets();
                Assert.GreaterOrEqual(svc.Count, 0);
            });
        }

        [Test]
        public void EncounterService_GetByType_FiltersCorrectly()
        {
            var reg = new PcEncounterRegistry();
            reg.Register(new PcEncounterEntry { encounterId = 1, type = 0, triggerMapId = 1, probability = 100 });
            reg.Register(new PcEncounterEntry { encounterId = 2, type = 2, triggerMapId = 1, probability = 50 });
            var svc = new EncounterService(reg);
            Assert.AreEqual(1, svc.GetByType(0).Count);
            Assert.AreEqual(1, svc.GetByType(2).Count);
            Assert.AreEqual(0, svc.GetByType(4).Count);
        }

        [Test]
        public void EncounterService_RollEncounter_Null_WhenNoData()
        {
            var svc = new EncounterService();
            Assert.IsNull(svc.RollEncounter(1, 12345));
            var reg = new PcEncounterRegistry();
            // 0% probability thì không match
            reg.Register(new PcEncounterEntry { encounterId = 1, type = 0, triggerMapId = 1, probability = 0 });
            var svc2 = new EncounterService(reg);
            Assert.IsNull(svc2.RollEncounter(1, 12345));
            // 10000 = 100% thì luôn match
            reg.Register(new PcEncounterEntry { encounterId = 2, type = 0, triggerMapId = 1, probability = 10000 });
            Assert.IsNotNull(svc2.RollEncounter(1, 12345));
        }

        [Test]
        public void EncounterService_GetEncounterTypeName_NonEmpty()
        {
            var svc = new EncounterService();
            Assert.AreEqual("Vật phẩm", svc.GetEncounterTypeName(0));
            Assert.AreEqual("NPC", svc.GetEncounterTypeName(1));
            Assert.AreEqual("Bẫy", svc.GetEncounterTypeName(2));
            Assert.AreEqual("Cổng", svc.GetEncounterTypeName(3));
            Assert.AreEqual("Sự kiện", svc.GetEncounterTypeName(4));
            Assert.IsNotEmpty(svc.GetEncounterTypeName(99));
        }

        // ── FriendGift ───────────────────────────────────────────────────────
        [Test]
        public void FriendGiftService_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = FriendGiftService.LoadFromStreamingAssets();
                Assert.GreaterOrEqual(svc.Count, 0);
            });
        }

        [Test]
        public void FriendGiftService_GetByFriendship_FiltersCorrectly()
        {
            var reg = new PcFriendGiftRegistry();
            reg.Register(new PcFriendGiftEntry { giftId = 1, friendshipRequired = 100 });
            reg.Register(new PcFriendGiftEntry { giftId = 2, friendshipRequired = 1000 });
            reg.Register(new PcFriendGiftEntry { giftId = 3, friendshipRequired = 5000 });
            var svc = new FriendGiftService(reg);
            Assert.AreEqual(1, svc.GetByFriendship(100).Count);
            Assert.AreEqual(2, svc.GetByFriendship(1000).Count);
            Assert.AreEqual(3, svc.GetByFriendship(10000).Count);
        }

        [Test]
        public void FriendGiftService_CanSendGift_RejectsLowFriendship()
        {
            var reg = new PcFriendGiftRegistry();
            reg.Register(new PcFriendGiftEntry { giftId = 1, friendshipRequired = 100, dailyLimit = 3 });
            var svc = new FriendGiftService(reg);
            Assert.IsFalse(svc.CanSendGift(1, 99, 0));
            Assert.IsTrue(svc.CanSendGift(1, 100, 0));
            Assert.IsTrue(svc.CanSendGift(1, 100, 2));
            Assert.IsFalse(svc.CanSendGift(1, 100, 3)); // đã gửi đủ
            Assert.IsFalse(svc.CanSendGift(999, 100, 0));
        }

        [Test]
        public void FriendGiftService_GetAvailableGifts_NonNull()
        {
            var svc = new FriendGiftService();
            var list = svc.GetAvailableGifts(0);
            Assert.IsNotNull(list);
        }

        // ── TextResource ─────────────────────────────────────────────────────
        [Test]
        public void TextResourceService_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = TextResourceService.LoadFromStreamingAssets();
                Assert.GreaterOrEqual(svc.Count, 0);
            });
        }

        [Test]
        public void TextResourceService_GetVietnamese_ReturnsNullForUnknown()
        {
            var svc = new TextResourceService();
            Assert.IsNull(svc.GetVietnamese("__UNKNOWN_KEY__"));
            Assert.IsNull(svc.GetVietnamese(""));
            Assert.IsNull(svc.GetVietnamese(null));
        }

        [Test]
        public void TextResourceService_GetOrVietnamese_ReturnsFallback()
        {
            var reg = new PcTextResourceRegistry();
            reg.Register(new PcTextResourceEntry { key = "OK", vietnamese = "Thành công" });
            var svc = new TextResourceService(reg);
            Assert.AreEqual("Thành công", svc.GetOrVietnamese("OK", "default"));
            Assert.AreEqual("default", svc.GetOrVietnamese("MISSING", "default"));
            Assert.AreEqual("__KEY__", svc.GetOrVietnamese("__KEY__", null));
        }

        // ── AnimationBank ────────────────────────────────────────────────────
        [Test]
        public void AnimationBankService_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = AnimationBankService.LoadFromStreamingAssets();
                Assert.GreaterOrEqual(svc.Count, 0);
            });
        }

        [Test]
        public void AnimationBankService_GetByDirection_FiltersCorrectly()
        {
            var reg = new PcAnimationBankRegistry();
            for (int d = 0; d <= 7; d++)
                reg.Register(new PcAnimationBankEntry { animId = d + 1, direction = d, frameCount = 8, frameDelayMs = 100 });
            var svc = new AnimationBankService(reg);
            for (int d = 0; d <= 7; d++)
            {
                var list = svc.GetByDirection(d);
                Assert.AreEqual(1, list.Count);
                Assert.AreEqual(d, list[0].direction);
            }
        }

        [Test]
        public void AnimationBankService_GetFrameDelayMs_ZeroForInvalid()
        {
            var reg = new PcAnimationBankRegistry();
            reg.Register(new PcAnimationBankEntry { animId = 1, frameCount = 8, frameDelayMs = 100 });
            var svc = new AnimationBankService(reg);
            Assert.AreEqual(100, svc.GetFrameDelayMs(1));
            Assert.AreEqual(0, svc.GetFrameDelayMs(999));
        }

        [Test]
        public void AnimationBankService_GetFrameCount_ZeroForInvalid()
        {
            var reg = new PcAnimationBankRegistry();
            reg.Register(new PcAnimationBankEntry { animId = 1, frameCount = 8, frameDelayMs = 100 });
            var svc = new AnimationBankService(reg);
            Assert.AreEqual(8, svc.GetFrameCount(1));
            Assert.AreEqual(0, svc.GetFrameCount(999));
        }
    }
}
