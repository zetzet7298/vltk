using System;
using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public sealed class HongbaoOpenResultServiceTests
    {
        private static string HongbaoPath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcItemFull/hongbao.txt");

        [Test]
        public void Catalog_ProvesPcRawWeightsAndTypeCounts()
        {
            var service = LoadService();

            Assert.AreEqual(69, service.Count, "PC settings/item/hongbao.txt has 69 data rows");
            Assert.AreEqual(1000000, service.TotalProba, "KBonus adds raw Proba values without normalizing");
            Assert.AreEqual(42, service.Type1Count, "Type 1 maps to KBonus.ITEM/AddItem");
            Assert.AreEqual(27, service.Type2Count, "Type 2 maps to KBonus.GOLDEN/AddGoldItem");
            Assert.AreEqual(15, service.CostlyCount, "Costly controls global-news emission when Msg exists");
            Assert.AreEqual(69, service.LogCount, "Every PC row has Log=1");
        }

        [Test]
        public void SelectByPcRoll_UsesInclusiveCumulativeBoundary()
        {
            var service = LoadService();

            Assert.AreEqual(1, service.SelectByPcRoll(1).id);
            Assert.AreEqual(1, service.SelectByPcRoll(200000).id);
            Assert.AreEqual(2, service.SelectByPcRoll(200001).id);
            Assert.AreEqual(2, service.SelectByPcRoll(201000).id);
            Assert.AreEqual(69, service.SelectByPcRoll(1000000).id);
            Assert.Throws<ArgumentOutOfRangeException>(() => service.SelectByPcRoll(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => service.SelectByPcRoll(1000001));
        }

        [Test]
        public void TryOpen_PreflightFailsBeforeRandomSelectionWhenFreeCellsUnderSix()
        {
            var result = LoadService().TryOpen(freeCells: 5, roll: 1000001, playerName: "Độc Cô");

            Assert.AreEqual(HongbaoOpenStatus.InsufficientInventorySpace, result.Status);
            Assert.AreEqual(6, result.RequiredFreeCells);
            Assert.IsNull(result.SelectedEntry);
            Assert.AreEqual(HongbaoRewardCommandType.None, result.RewardCommand.CommandType);
            Assert.IsFalse(result.ShouldEmitGlobalNews);
            Assert.IsFalse(result.ShouldWriteLog);
            Assert.IsTrue(result.FailureMessageVi.Contains("6 ô trống"));
        }

        [Test]
        public void TryOpen_TypeOneBuildsAddItemCommandAndLogFlag()
        {
            var result = LoadService().TryOpen(freeCells: 6, roll: 1, playerName: "Độc Cô");

            Assert.AreEqual(HongbaoOpenStatus.RewardSelected, result.Status);
            Assert.AreEqual(1, result.SelectedEntry.id);
            Assert.AreEqual(HongbaoRewardCommandType.AddItem, result.RewardCommand.CommandType);
            Assert.AreEqual("AddItem", result.RewardCommand.ApiName);
            Assert.AreEqual(6, result.RewardCommand.Genre);
            Assert.AreEqual(1, result.RewardCommand.Detail);
            Assert.AreEqual(71, result.RewardCommand.Particular);
            Assert.AreEqual(1, result.RewardCommand.Level);
            Assert.AreEqual(0, result.RewardCommand.Serise);
            Assert.AreEqual(0, result.RewardCommand.Luck);
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 0, 0, 0 }, result.RewardCommand.Params);
            Assert.IsFalse(result.ShouldEmitGlobalNews, "Costly=0 never calls AddGlobalNews");
            Assert.IsTrue(result.ShouldWriteLog, "Log=1 calls WriteLog");
            Assert.IsTrue(result.MessageTemplate.Contains("<player>"));
            Assert.IsTrue(result.MessageTemplate.Contains("<name>"));
            Assert.IsTrue(result.ResolvedMessage.Contains("Độc Cô"));
            Assert.IsTrue(result.ResolvedMessage.Contains(result.SelectedEntry.nameRaw));
        }

        [Test]
        public void TryOpen_TypeTwoBuildsAddGoldItemCommandAndCostlyGlobalNewsFlag()
        {
            var result = LoadService().TryOpen(freeCells: 6, roll: 200001, playerName: "Độc Cô");

            Assert.AreEqual(2, result.SelectedEntry.id);
            Assert.AreEqual(2, result.SelectedEntry.type);
            Assert.AreEqual(HongbaoRewardCommandType.AddGoldItem, result.RewardCommand.CommandType);
            Assert.AreEqual("AddGoldItem", result.RewardCommand.ApiName);
            Assert.AreEqual(0, result.RewardCommand.AddGoldItemFirstArg);
            Assert.AreEqual(159, result.RewardCommand.Genre);
            Assert.IsTrue(result.ShouldEmitGlobalNews, "Costly=1 and Msg present calls AddGlobalNews");
            Assert.IsTrue(result.ShouldWriteLog);
        }

        private static HongbaoOpenResultService LoadService()
        {
            var rows = PcHongbaoParser.ParseFile(HongbaoPath);
            Assert.AreEqual(69, rows.Count);
            return new HongbaoOpenResultService(rows);
        }
    }
}
