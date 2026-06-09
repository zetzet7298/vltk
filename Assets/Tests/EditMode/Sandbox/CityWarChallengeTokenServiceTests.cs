// VLTK Mobile — CityWar challenge-token turn-in proof tests.

using System.Linq;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public sealed class CityWarChallengeTokenServiceTests
    {
        [Test]
        public void Constants_MatchPcInfoCenterAndHeadLua()
        {
            Assert.AreEqual(new CityWarItemTuple(6, 1, 1499), CityWarPcConstants.ChallengeTokenItem);
            Assert.AreEqual(1839, CityWarPcConstants.TiaoZhanLingTaskDate);
            Assert.AreEqual(1840, CityWarPcConstants.TiaoZhanLingTaskCount);
            Assert.AreEqual(300, CityWarPcConstants.TiaoZhanLingDailyCap);
            Assert.AreEqual(5000, CityWarPcConstants.TiaoZhanLingExpReward);
            Assert.AreEqual(538, CityWarPcConstants.TiaoZhanLingLeagueType);
            Assert.AreEqual("tiaozhanling", CityWarPcConstants.TiaoZhanLingLeagueName);
            Assert.AreEqual(1, CityWarPcConstants.TiaoZhanLingLeagueTaskCount);
        }

        [Test]
        public void EligibleSameDate_EmitsPcCommandAndEventSurfaceWithoutInventoryMutation()
        {
            CityWarChallengeTokenInput input = BaseInput(storedDate: 2669, storedCount: 10, tongTotal: 42);
            input.GivenItems.Add(TokenUnit(101, 2));
            input.GivenItems.Add(TokenUnit(102, 3));

            CityWarChallengeTokenPlan plan = Build(input);

            Assert.That(plan.Accepted, Is.True);
            Assert.That(plan.ReasonCode, Is.EqualTo(CityWarChallengeTokenService.ReasonEligible));
            Assert.That(plan.TokenCount, Is.EqualTo(5));
            Assert.That(plan.DailyCountBefore, Is.EqualTo(10));
            Assert.That(plan.DailyCountAfter, Is.EqualTo(15));
            Assert.That(plan.ExpReward, Is.EqualTo(25000));
            Assert.That(plan.Commands.Select(c => c.Name).ToArray(), Is.EqualTo(new[]
            {
                "RemoveItemByIndex", "RemoveItemByIndex", "SetTask", "LG_ApplyAppendMemberTask", "AddOwnExp", "Ctc3tru_SetTask"
            }));
            AssertOp(plan.Commands[2], "SetTask", 1840, 15);
            AssertOp(plan.Commands[3], "LG_ApplyAppendMemberTask", 538, 1, 5);
            AssertOp(plan.Commands[4], "AddOwnExp", 25000);
            AssertOp(plan.Commands[5], "Ctc3tru_SetTask", 19, 47);
            Assert.That(plan.Events.Select(e => e.Name).ToArray(), Is.EqualTo(new[] { "Msg2Player", "Msg2Player", "WriteLog" }));
        }

        [Test]
        public void EligibleNewDate_ResetsPcDateAndCountTasksBeforeFinalCount()
        {
            CityWarChallengeTokenInput input = BaseInput(storedDate: 2668, storedCount: 300);
            input.GivenItems.Add(TokenUnit(201, 1));

            CityWarChallengeTokenPlan plan = Build(input);

            Assert.That(plan.Accepted, Is.True);
            Assert.That(plan.DailyCountBefore, Is.EqualTo(0));
            Assert.That(plan.DailyCountAfter, Is.EqualTo(1));
            AssertOp(plan.Commands[0], "SetTask", 1839, 2669);
            AssertOp(plan.Commands[1], "SetTask", 1840, 0);
            AssertOp(plan.Commands[2], "RemoveItemByIndex", 201);
            AssertOp(plan.Commands[3], "SetTask", 1840, 1);
        }

        [Test]
        public void WrongToken_RejectsWithSayEventAndNoMutationCommands()
        {
            CityWarChallengeTokenInput input = BaseInput(storedDate: 2669, storedCount: 10);
            input.GivenItems.Add(new CityWarChallengeTokenUnit(301, new CityWarItemTuple(6, 1, 1500), 1));

            CityWarChallengeTokenPlan plan = Build(input);

            AssertRejected(plan, CityWarChallengeTokenService.ReasonWrongToken, dailyBefore: 10, tokenCount: 0, remaining: 290);
        }

        [Test]
        public void OverCap_RejectsWhenStackWouldPushPastPcDailyCap()
        {
            CityWarChallengeTokenInput input = BaseInput(storedDate: 2669, storedCount: 299);
            input.GivenItems.Add(TokenUnit(401, 2));

            CityWarChallengeTokenPlan plan = Build(input);

            AssertRejected(plan, CityWarChallengeTokenService.ReasonDailyCapExceeded, dailyBefore: 299, tokenCount: 2, remaining: 1);
        }

        [Test]
        public void ExactDailyCap_IsStillEligibleBecausePcLuaRejectsOnlyGreaterThanCap()
        {
            CityWarChallengeTokenInput input = BaseInput(storedDate: 2669, storedCount: 298);
            input.GivenItems.Add(TokenUnit(501, 2));

            CityWarChallengeTokenPlan plan = Build(input);

            Assert.That(plan.Accepted, Is.True);
            Assert.That(plan.DailyCountAfter, Is.EqualTo(300));
            Assert.That(plan.DailyRemaining, Is.EqualTo(0));
        }

        private static CityWarChallengeTokenPlan Build(CityWarChallengeTokenInput input)
        {
            return new CityWarChallengeTokenService().BuildTurnInPlan(input);
        }

        private static CityWarChallengeTokenInput BaseInput(int storedDate, int storedCount, int tongTotal = 0)
        {
            return new CityWarChallengeTokenInput
            {
                TodayTaskDate = 2669,
                StoredTaskDate = storedDate,
                StoredDailyCount = storedCount,
                CurrentTongTotal = tongTotal,
            };
        }

        private static CityWarChallengeTokenUnit TokenUnit(int itemIndex, int stackCount)
        {
            return new CityWarChallengeTokenUnit(itemIndex, CityWarPcConstants.ChallengeTokenItem, stackCount);
        }

        private static void AssertRejected(CityWarChallengeTokenPlan plan, string reason, int dailyBefore, int tokenCount, int remaining)
        {
            Assert.That(plan.Accepted, Is.False);
            Assert.That(plan.ReasonCode, Is.EqualTo(reason));
            Assert.That(plan.DailyCountBefore, Is.EqualTo(dailyBefore));
            Assert.That(plan.TokenCount, Is.EqualTo(tokenCount));
            Assert.That(plan.DailyRemaining, Is.EqualTo(remaining));
            Assert.That(plan.Commands.Count, Is.EqualTo(0));
            Assert.That(plan.Events.Count, Is.EqualTo(1));
            Assert.That(plan.Events[0].Name, Is.EqualTo("Say"));
        }

        private static void AssertOp(CityWarChallengeTokenOperation op, string name, params int[] intArgs)
        {
            Assert.That(op.Name, Is.EqualTo(name));
            Assert.That(op.IntArgs, Is.EqualTo(intArgs));
        }
    }
}
