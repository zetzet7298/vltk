// VLTK Mobile — PC CityWar card/token constants tests.

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public sealed class CityWarPcConstantsTests
    {
        [Test]
        public void CardTabAndPrices_MatchPcHeadLua()
        {
            CollectionAssert.AreEqual(
                new[] { 363, 362, 355, 354, 367, 366, 359, 358, 357, 356, 365, 364, 361, 360 },
                CityWarPcConstants.CardTab);
            Assert.AreEqual(200000, CityWarPcConstants.CardPrice);
            Assert.AreEqual(10000, CityWarPcConstants.ReturnCardPrice);
        }

        [Test]
        public void ChallengeTokenAndDailyRewards_MatchPcInfoCenterLua()
        {
            Assert.AreEqual(new CityWarItemTuple(6, 1, 1499), CityWarPcConstants.ChallengeTokenItem);
            Assert.AreEqual(1839, CityWarPcConstants.TiaoZhanLingTaskDate);
            Assert.AreEqual(1840, CityWarPcConstants.TiaoZhanLingTaskCount);
            Assert.AreEqual(300, CityWarPcConstants.TiaoZhanLingDailyCap);
            Assert.AreEqual(5000, CityWarPcConstants.TiaoZhanLingExpReward);
        }

        [Test]
        public void LeagueTypesAndTasks_MatchPcHeadAndInfoCenterLua()
        {
            Assert.AreEqual(538, CityWarPcConstants.TiaoZhanLingLeagueType);
            Assert.AreEqual("tiaozhanling", CityWarPcConstants.TiaoZhanLingLeagueName);
            Assert.AreEqual(1, CityWarPcConstants.TiaoZhanLingLeagueTaskCount);
            Assert.AreEqual(508, CityWarPcConstants.CityWarSignLeagueType);
            Assert.AreEqual(509, CityWarPcConstants.CityWarFirstLeagueType);
            Assert.AreEqual(1, CityWarPcConstants.QingTongDingLeagueTaskCount);
            Assert.AreEqual(2, CityWarPcConstants.CityWarSignCountLeagueTask);
        }

        [Test]
        public void CardSideMapping_IsAttackerOddDefenderEven_ByPcCityId()
        {
            for (int cityId = 1; cityId <= 7; cityId++)
            {
                int attackerCard = CityWarPcConstants.CardTab[cityId * 2 - 2];
                int defenderCard = CityWarPcConstants.CardTab[cityId * 2 - 1];

                Assert.AreEqual(attackerCard, CityWarPcConstants.GetCardItemIdForCity(cityId, CityWarCardSide.Attacker));
                Assert.AreEqual(defenderCard, CityWarPcConstants.GetCardItemIdForCity(cityId, CityWarCardSide.Defender));
                Assert.AreEqual(CityWarCardSide.Attacker, CityWarPcConstants.GetCardSideForCity(cityId, attackerCard));
                Assert.AreEqual(CityWarCardSide.Defender, CityWarPcConstants.GetCardSideForCity(cityId, defenderCard));
            }
        }

        [Test]
        public void CardMapping_ReturnsNoneOutsidePcCityScope()
        {
            Assert.AreEqual(0, CityWarPcConstants.GetCardItemIdForCity(0, CityWarCardSide.Attacker));
            Assert.AreEqual(0, CityWarPcConstants.GetCardItemIdForCity(8, CityWarCardSide.Defender));
            Assert.AreEqual(0, CityWarPcConstants.GetCardItemIdForCity(1, CityWarCardSide.None));
            Assert.AreEqual(CityWarCardSide.None, CityWarPcConstants.GetCardSideForCity(1, 9999));
        }

        [Test]
        public void ConstantsStayAlignedWithExistingJoinRouterCardTable()
        {
            CollectionAssert.AreEqual(CityWarPcConstants.CardTab, CityWarJoinRouterRuntimeService.CardTab);
            Assert.AreEqual(363, CityWarJoinRouterRuntimeService.GetCardItemIdForCity(1, oddCard: true));
            Assert.AreEqual(362, CityWarJoinRouterRuntimeService.GetCardItemIdForCity(1, oddCard: false));
        }
    }
}
