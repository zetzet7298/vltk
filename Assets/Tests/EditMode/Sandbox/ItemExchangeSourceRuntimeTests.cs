using System.Linq;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class ItemExchangeSourceRuntimeTests
    {
        [Test]
        public void ExchangeOldItemPlan_CapturesPcRemoveAwardAndLogCommands()
        {
            var plan = ItemExchangeService.BuildExchangeOldItemPlan(new ItemExchangePlanInput
            {
                GivenItemCount = 1,
                ItemIndex = 42,
                BindState = 0,
                UseTime = 0,
                ExpireTime = 0,
                ExchangeValue = 1234,
                ItemName = "PC old equip",
                ItemQuality = 4,
            });

            Assert.IsTrue(plan.Success, plan.FailureReason);
            CollectionAssert.AreEqual(new[] { "WriteLog", "RemoveItemByIndex", "AddGoldItem", "WriteLog" },
                plan.Commands.Select(c => c.ApiName).ToArray());
            Assert.AreEqual(42, plan.Commands[1].IntArgs[0]);
            CollectionAssert.AreEqual(new[] { 0, 6, 1, 2356, 1, 0, 0, 1234 }, plan.Commands[2].IntArgs);
        }

        [Test]
        public void LingpaiPlan_ConsumesSoulStoneAndReturnsOverflowBeforeAwardingToken()
        {
            var service = new ItemExchangeService();
            var plan = service.BuildLingpaiPlan(new ItemExchangePlanInput
            {
                GivenItemCount = 1,
                ItemIndex = 77,
                Genre = 6,
                Detail = 1,
                Particular = 2356,
                MagicLevel = 1500,
                BindState = -2,
                FreeBagCells = 1,
            }, "Thanh Cầu Lệnh");

            Assert.IsTrue(plan.Success, plan.FailureReason);
            Assert.AreEqual(1000, plan.RequiredMagicLevel);
            Assert.AreEqual(500, plan.OverflowMagicLevel);
            CollectionAssert.AreEqual(new[] { "WriteLog", "RemoveItemByIndex", "AddGoldItem", "AddGoldItem", "WriteLog" },
                plan.Commands.Select(c => c.ApiName).ToArray());
            CollectionAssert.AreEqual(new[] { 0, 6, 1, 2356, 1, 0, 0, 500 }, plan.Commands[2].IntArgs);
            CollectionAssert.AreEqual(new[] { 0, 6, 1, 4867, 1, 0, 0, 0 }, plan.Commands[3].IntArgs);
        }

        [Test]
        public void JinglianPutInPlan_ConsumesEnergyAndUpdatesStoneMagicLevel()
        {
            var plan = ItemExchangeService.BuildJinglianPutInPlan(new ItemExchangePlanInput
            {
                ItemIndex = 91,
                MagicLevel = 10,
                Energy = 250,
                ConsumeCount = 25,
                BindState = -2,
            });

            Assert.IsTrue(plan.Success, plan.FailureReason);
            CollectionAssert.AreEqual(new[] { "ConsumeItem", "SetItemMagicLevel", "SyncItem", "SetItemBindState", "WriteLog" },
                plan.Commands.Select(c => c.ApiName).ToArray());
            CollectionAssert.AreEqual(new[] { 91, 1, 35 }, plan.Commands[1].IntArgs);
            CollectionAssert.AreEqual(new[] { 91 }, plan.Commands[2].IntArgs);
        }
    }
}
