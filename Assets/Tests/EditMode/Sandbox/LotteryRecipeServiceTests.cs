// -----------------------------------------------------------------------------
// VLTK Mobile — Lottery + Compound Recipe Service Tests
// Tests runtime service behavior using PC data in StreamingAssets/Reference/.
// -----------------------------------------------------------------------------

using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class LotteryServiceTests
    {
        private static string LotteryDir => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcLottery");

        [Test]
        public void LoadFromStreamingAssets_LoadsLotteries()
        {
            var reg = PcLotteryParser.BuildRegistry(LotteryDir);
            var svc = new LotteryService(reg);
            Assert.Greater(svc.RegisteredCount, 0, "PC has 254 lottery entries");
        }

        [Test]
        public void CanDraw_AlwaysTrueWhenNoRecurrence()
        {
            // Build a registry with one entry that has daysly=0 weekly=0
            var reg = new PcLotteryRegistry();
            reg.Register(new PcLotteryEntry
            {
                name = "TestNoRecur",
                daysly = 0,
                weekly = 0,
                itemGenre = 1,
                itemDetailType = 2,
                itemParticular = 3,
            });
            var svc = new LotteryService(reg);
            // No recurrence rule → always drawable
            Assert.IsTrue(svc.CanDraw("TestNoRecur", 1));
            Assert.IsTrue(svc.CanDraw("TestNoRecur", 7));
        }

        [Test]
        public void CanDraw_RespectsDailyLimit()
        {
            var reg = new PcLotteryRegistry();
            reg.Register(new PcLotteryEntry
            {
                name = "DailyOne",
                daysly = 1,
                weekly = 0,
                itemGenre = 1,
                itemDetailType = 2,
                itemParticular = 3,
            });
            var svc = new LotteryService(reg);
            Assert.IsTrue(svc.CanDraw("DailyOne", 3), "First draw should be allowed");
            // After one draw, daily limit hit
            svc.Draw("DailyOne");
            Assert.IsFalse(svc.CanDraw("DailyOne", 3), "Second draw should be blocked by daysly=1");
        }

        [Test]
        public void Draw_IncrementsCount()
        {
            var reg = new PcLotteryRegistry();
            reg.Register(new PcLotteryEntry
            {
                name = "CountTest",
                daysly = 99,
                weekly = 0,
                itemGenre = 4,
                itemDetailType = 5,
                itemParticular = 6,
            });
            var svc = new LotteryService(reg);
            Assert.AreEqual(0, svc.GetPullCount("CountTest"));
            var r1 = svc.Draw("CountTest");
            Assert.IsNotNull(r1);
            Assert.AreEqual(1, svc.GetPullCount("CountTest"));
            svc.Draw("CountTest");
            Assert.AreEqual(2, svc.GetPullCount("CountTest"));
        }

        [Test]
        public void ResetCounts_ZerosAllCounts()
        {
            var reg = new PcLotteryRegistry();
            reg.Register(new PcLotteryEntry { name = "A", daysly = 99, weekly = 0 });
            reg.Register(new PcLotteryEntry { name = "B", daysly = 99, weekly = 0 });
            var svc = new LotteryService(reg);
            svc.Draw("A");
            svc.Draw("B");
            svc.Draw("A");
            Assert.AreEqual(2, svc.GetPullCount("A"));
            Assert.AreEqual(1, svc.GetPullCount("B"));
            svc.ResetCounts();
            Assert.AreEqual(0, svc.GetPullCount("A"));
            Assert.AreEqual(0, svc.GetPullCount("B"));
        }

        [Test]
        public void Draw_FiresOnLotteryDrawnEvent()
        {
            var reg = new PcLotteryRegistry();
            reg.Register(new PcLotteryEntry { name = "EvtTest", daysly = 99, weekly = 0, itemGenre = 7, itemDetailType = 8, itemParticular = 9 });
            var svc = new LotteryService(reg);
            LotteryReward captured = null;
            svc.OnLotteryDrawn += r => captured = r;
            svc.Draw("EvtTest");
            Assert.IsNotNull(captured);
            Assert.AreEqual("EvtTest", captured.lotteryName);
            Assert.AreEqual(7, captured.itemGenre);
        }

        [Test]
        public void Draw_UnknownNameReturnsNull()
        {
            var svc = new LotteryService(new PcLotteryRegistry());
            Assert.IsNull(svc.Draw("DoesNotExist"));
        }
    }

    public class CompoundRecipeServiceTests
    {
        private static string RecipeDir => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcRecipe");

        [Test]
        public void LoadFromStreamingAssets_LoadsRecipes()
        {
            var reg = PcRecipeParser.BuildRegistry(RecipeDir);
            var svc = new CompoundRecipeService(reg);
            Assert.Greater(svc.RegisteredCount, 0, "PC has 1,294 platina recipes");
        }

        [Test]
        public void CanCompound_TrueForRegisteredPlatina()
        {
            var reg = new PcRecipeRegistry();
            reg.Register(new PcRecipeEntry { platinaId = 100, goldId = 200, nameRaw = "Test", taskRate = 5000, recoin = 10 });
            var svc = new CompoundRecipeService(reg);
            Assert.IsTrue(svc.CanCompound(100, 200), "Registered platina with matching goldId should be compoundable");
            Assert.IsFalse(svc.CanCompound(100, 999), "Mismatched goldId should be rejected");
            Assert.IsFalse(svc.CanCompound(0, 0), "Zero ids should be rejected");
        }

        [Test]
        public void CalculateSuccessRate_InRange0To1()
        {
            var reg = new PcRecipeRegistry();
            reg.Register(new PcRecipeEntry { platinaId = 1, goldId = 2, nameRaw = "Half", taskRate = 5000 });
            reg.Register(new PcRecipeEntry { platinaId = 3, goldId = 4, nameRaw = "Full", taskRate = 10000 });
            reg.Register(new PcRecipeEntry { platinaId = 5, goldId = 6, nameRaw = "Over", taskRate = 99999 });
            var svc = new CompoundRecipeService(reg);
            float halfRate = svc.CalculateSuccessRate(1);
            float fullRate = svc.CalculateSuccessRate(3);
            float overRate = svc.CalculateSuccessRate(5);
            Assert.GreaterOrEqual(halfRate, 0f);
            Assert.LessOrEqual(halfRate, 1f);
            Assert.AreEqual(0.5f, halfRate, 0.001f);
            Assert.AreEqual(1.0f, fullRate, 0.001f);
            Assert.AreEqual(1.0f, overRate, 0.001f, "Clamped to 1.0");
            Assert.AreEqual(0f, svc.CalculateSuccessRate(99999), "Unknown platina returns 0");
        }

        [Test]
        public void TryCompound_FailurePossible()
        {
            var reg = new PcRecipeRegistry();
            reg.Register(new PcRecipeEntry { platinaId = 50, goldId = 60, nameRaw = "Impossible", taskRate = 0, recoin = 5 });
            var svc = new CompoundRecipeService(reg);
            var result = svc.TryCompound(50, 60, playerLuck: 0);
            Assert.IsNotNull(result);
            Assert.IsFalse(result.success, "taskRate=0 + luck=0 should always fail");
            Assert.IsTrue(result.recoinConsumed);
            Assert.AreEqual(5, result.recoinCost);
            Assert.AreEqual(0, result.newItemId);
        }

        [Test]
        public void TryCompound_SuccessOnFullRate()
        {
            var reg = new PcRecipeRegistry();
            reg.Register(new PcRecipeEntry { platinaId = 70, goldId = 80, nameRaw = "Guaranteed", taskRate = 10000, recoin = 1 });
            var svc = new CompoundRecipeService(reg);
            var result = svc.TryCompound(70, 80, playerLuck: 0);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.success, "taskRate=10000 (100%) should always succeed");
            Assert.AreEqual(70, result.newItemId);
        }

        [Test]
        public void TryCompound_RejectsMismatchedGold()
        {
            var reg = new PcRecipeRegistry();
            reg.Register(new PcRecipeEntry { platinaId = 90, goldId = 100, nameRaw = "Mismatch", taskRate = 5000 });
            var svc = new CompoundRecipeService(reg);
            var result = svc.TryCompound(90, 999, playerLuck: 0);
            Assert.IsNotNull(result);
            Assert.IsFalse(result.success, "Wrong goldId should be a failure");
            Assert.AreEqual(0, result.recoinCost, "No recoin consumed when recipe is invalid");
        }

        [Test]
        public void TryCompound_FiresEvent()
        {
            var reg = new PcRecipeRegistry();
            reg.Register(new PcRecipeEntry { platinaId = 110, goldId = 120, nameRaw = "Evt", taskRate = 10000 });
            var svc = new CompoundRecipeService(reg);
            CompoundResult captured = null;
            svc.OnCompound += r => captured = r;
            svc.TryCompound(110, 120);
            Assert.IsNotNull(captured);
            Assert.AreEqual("Evt", captured.recipeNameVi);
        }
    }
}
