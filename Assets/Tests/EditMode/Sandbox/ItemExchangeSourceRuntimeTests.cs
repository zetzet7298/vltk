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

        // --- ExecutePlan tests (wire plan to IItemExchangeInventory) ---

        /// <summary>
        /// Fake inventory để test ExecutePlan. Tracks takes/gives/logs/gold.
        /// </summary>
        private sealed class FakeInventory : IItemExchangeInventory
        {
            public System.Collections.Generic.HashSet<int> Items = new();
            public System.Collections.Generic.List<string> Logs = new();
            public int FreeCells = 20;
            public int Gold = 0;
            public bool TakeFails = false;
            public int GiveFailAfterNth = -1;
            public int GiveCalls = 0;

            public bool HasItem(int itemIndex, int count = 1) => Items.Contains(itemIndex);
            public bool TakeItem(int itemIndex, int count = 1)
            {
                if (TakeFails) return false;
                if (!Items.Remove(itemIndex)) return false;
                return true;
            }
            public bool GiveItem(int genre, int detail, int particular, int level, int count, int magicLevel = 0)
            {
                GiveCalls++;
                if (GiveFailAfterNth >= 0 && GiveCalls > GiveFailAfterNth) return false;
                FreeCells = System.Math.Max(0, FreeCells - 1);
                return true;
            }
            public bool GiveGold(int amount) { Gold += amount; return true; }
            public int FreeBagCells() => FreeCells;
            public void WriteLog(string message) => Logs.Add(message);
            public bool ConsumeItem(int itemIndex, int count) { return true; }
            public bool SetItemMagicLevel(int itemIndex, int newMagicLevel) { return true; }
            public bool SyncItem(int itemIndex) { return true; }
            public bool SetItemBindState(int itemIndex, int bindState) { return true; }
        }

        [Test]
        public void ExecutePlan_SimpleRemoveAddGold_WritesLogsAndMutates()
        {
            var svc = new ItemExchangeService();
            var plan = ItemExchangeService.BuildExchangeOldItemPlan(new ItemExchangePlanInput
            {
                GivenItemCount = 1, ItemIndex = 42, BindState = 0,
                UseTime = 0, ExpireTime = 0, ExchangeValue = 1234,
                ItemName = "PC old equip", ItemQuality = 4,
            });
            var inv = new FakeInventory { Items = { 42 }, FreeCells = 20 };

            bool ok = svc.ExecutePlan(plan, inv, out var err);

            Assert.IsTrue(ok, err);
            Assert.IsFalse(inv.Items.Contains(42), "Item 42 should be taken.");
            Assert.AreEqual(2, inv.Logs.Count, "Two WriteLog commands expected.");
            Assert.AreEqual("PC old equip", inv.Logs[0]);
        }

        [Test]
        public void ExecutePlan_InsufficientBagCells_FailsBeforeRemoving()
        {
            var svc = new ItemExchangeService();
            var plan = ItemExchangeService.BuildExchangeOldItemPlan(new ItemExchangePlanInput
            {
                GivenItemCount = 1, ItemIndex = 42, BindState = 0,
                UseTime = 0, ExpireTime = 0, ExchangeValue = 1234,
                ItemName = "x", ItemQuality = 4,
            });
            var inv = new FakeInventory { Items = { 42 }, FreeCells = 0 };

            bool ok = svc.ExecutePlan(plan, inv, out var err);

            Assert.IsFalse(ok);
            StringAssert.Contains("InsufficientBagCells", err);
            Assert.IsTrue(inv.Items.Contains(42), "Preflight must not take item on failure.");
        }

        [Test]
        public void ExecutePlan_PartialFailure_RollsBackTakes()
        {
            var svc = new ItemExchangeService();
            var plan = svc.BuildLingpaiPlan(new ItemExchangePlanInput
            {
                GivenItemCount = 1, ItemIndex = 77,
                Genre = 6, Detail = 1, Particular = 2356,
                MagicLevel = 1500, BindState = -2, FreeBagCells = 1,
            }, "Thanh Cầu Lệnh");
            // Make the SECOND GiveItem fail.
            var inv = new FakeInventory { Items = { 77 }, FreeCells = 5, GiveFailAfterNth = 1 };

            bool ok = svc.ExecutePlan(plan, inv, out var err);

            Assert.IsFalse(ok);
            // Rollback should have restored the taken item via GiveItem.
            StringAssert.Contains("AddGoldItem", err);
        }

        [Test]
        public void ExecutePlan_UnknownCommand_SkippedGracefully()
        {
            var svc = new ItemExchangeService();
            var plan = ItemExchangePlan.Ok("test", "test");
            plan.Commands.Add(ItemExchangeHostCommand.Create("BogusApiName", null, 1, 2, 3));
            var inv = new FakeInventory { FreeCells = 20 };

            bool ok = svc.ExecutePlan(plan, inv, out var err);

            Assert.IsTrue(ok, err);
        }

        [Test]
        public void ExecutePlan_LingpaiPlan_AppliesAwardCommand()
        {
            var svc = new ItemExchangeService();
            var plan = svc.BuildLingpaiPlan(new ItemExchangePlanInput
            {
                GivenItemCount = 1, ItemIndex = 77,
                Genre = 6, Detail = 1, Particular = 2356,
                MagicLevel = 1500, BindState = -2, FreeBagCells = 5,
            }, "Thanh Cầu Lệnh");
            var inv = new FakeInventory { Items = { 77 }, FreeCells = 10 };

            bool ok = svc.ExecutePlan(plan, inv, out var err);

            Assert.IsTrue(ok, err);
            Assert.AreEqual(2, inv.GiveCalls, "Lingpai plan has 2 AddGoldItem awards.");
            Assert.AreEqual(2, inv.Logs.Count);
        }

        [Test]
        public void ExecutePlan_EmptyPlan_Succeeds()
        {
            var svc = new ItemExchangeService();
            var plan = ItemExchangePlan.Ok("empty", "test");
            var inv = new FakeInventory { FreeCells = 20 };

            bool ok = svc.ExecutePlan(plan, inv, out var err);

            Assert.IsTrue(ok, err);
        }
    }
}
