using System;
using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public sealed class CityHongbaoOpenResultServiceTests
    {
        private static string CityHongbaoPath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcItemFull/chengshidahongbao.txt");

        [Test]
        public void Catalog_ProvesPcRawWeightsAndTypeCounts()
        {
            var service = LoadService();

            Assert.AreEqual(67, service.Count, "PC settings/item/chengshidahongbao.txt has 67 data rows");
            Assert.AreEqual(1010000, service.TotalProba, "KBonus adds raw Proba values; city table is not normalized to 1,000,000");
            Assert.AreEqual(13, service.Type1Count, "Type 1 maps to KBonus.ITEM/AddItem");
            Assert.AreEqual(54, service.Type2Count, "Type 2 maps to KBonus.GOLDEN/AddGoldItem");
            Assert.AreEqual(54, service.CostlyCount, "Costly controls global-news emission when Msg exists");
            Assert.AreEqual(67, service.LogCount, "Every PC city row has Log=1");
        }

        [Test]
        public void SelectByPcRoll_UsesInclusiveCumulativeBoundaryIncludingFinalRow()
        {
            var service = LoadService();

            Assert.AreEqual(1, service.SelectByPcRoll(1).Id);
            Assert.AreEqual(1, service.SelectByPcRoll(50000).Id);
            Assert.AreEqual(2, service.SelectByPcRoll(50001).Id);
            Assert.AreEqual(4, service.SelectByPcRoll(200000).Id);
            Assert.AreEqual(13, service.SelectByPcRoll(842051).Id);
            Assert.AreEqual(66, service.SelectByPcRoll(1000000).Id);
            Assert.AreEqual(67, service.SelectByPcRoll(1000001).Id);
            Assert.AreEqual(67, service.SelectByPcRoll(1010000).Id);
            Assert.Throws<ArgumentOutOfRangeException>(() => service.SelectByPcRoll(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => service.SelectByPcRoll(1010001));
        }

        [Test]
        public void TryOpen_PreflightFailsBeforeRandomSelectionWhenFreeCellsUnderSix()
        {
            var result = LoadService().TryOpen(freeCells: 5, roll: 1010001, playerName: "Độc Cô");

            Assert.AreEqual(CityHongbaoOpenStatus.InsufficientInventorySpace, result.Status);
            Assert.AreEqual(6, result.RequiredFreeCells);
            Assert.IsNull(result.SelectedEntry);
            Assert.AreEqual(CityHongbaoRewardCommandType.None, result.RewardCommand.CommandType);
            Assert.IsFalse(result.ShouldEmitGlobalNews);
            Assert.IsFalse(result.ShouldWriteLog);
            Assert.IsTrue(result.FailureMessageVi.Contains("6 ô trống"));
        }

        [Test]
        public void TryOpen_TypeOneBuildsAddItemCommandAndLogFlag()
        {
            var result = LoadService().TryOpen(freeCells: 6, roll: 1, playerName: "Độc Cô");

            Assert.AreEqual(CityHongbaoOpenStatus.RewardSelected, result.Status);
            Assert.AreEqual(1, result.SelectedEntry.Id);
            Assert.AreEqual(1, result.SelectedEntry.Type);
            Assert.AreEqual(CityHongbaoRewardCommandType.AddItem, result.RewardCommand.CommandType);
            Assert.AreEqual("AddItem", result.RewardCommand.ApiName);
            Assert.AreEqual(4, result.RewardCommand.Genre);
            Assert.AreEqual(238, result.RewardCommand.Detail);
            Assert.AreEqual(1, result.RewardCommand.Particular);
            Assert.AreEqual(1, result.RewardCommand.Level);
            Assert.AreEqual(0, result.RewardCommand.Serise);
            Assert.AreEqual(0, result.RewardCommand.Luck);
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 0, 0, 0 }, result.RewardCommand.Params);
            Assert.IsFalse(result.ShouldEmitGlobalNews, "Costly=0 never calls AddGlobalNews");
            Assert.IsTrue(result.ShouldWriteLog, "Log=1 calls WriteLog");
            Assert.IsTrue(result.MessageTemplate.Contains("<player>"));
            Assert.IsTrue(result.MessageTemplate.Contains("<name>"));
            Assert.IsTrue(result.ResolvedMessage.Contains("Độc Cô"));
            Assert.IsTrue(result.ResolvedMessage.Contains(result.SelectedEntry.Name));
        }

        [Test]
        public void TryOpen_TypeTwoBuildsAddGoldItemCommandAndCostlyGlobalNewsFlag()
        {
            var result = LoadService().TryOpen(freeCells: 6, roll: 842051, playerName: "Độc Cô");

            Assert.AreEqual(13, result.SelectedEntry.Id);
            Assert.AreEqual(2, result.SelectedEntry.Type);
            Assert.AreEqual(CityHongbaoRewardCommandType.AddGoldItem, result.RewardCommand.CommandType);
            Assert.AreEqual("AddGoldItem", result.RewardCommand.ApiName);
            Assert.AreEqual(0, result.RewardCommand.AddGoldItemFirstArg);
            Assert.AreEqual(2, result.RewardCommand.Genre);
            Assert.IsTrue(result.ShouldEmitGlobalNews, "Costly=1 and Msg present calls AddGlobalNews");
            Assert.IsTrue(result.ShouldWriteLog);
        }

        [Test]
        public void TryOpen_FinalRollCanSelectFinalTypeOneRowPastOneMillion()
        {
            var result = LoadService().TryOpen(freeCells: 6, roll: 1010000, playerName: "Độc Cô");

            Assert.AreEqual(67, result.SelectedEntry.Id);
            Assert.AreEqual(CityHongbaoRewardCommandType.AddItem, result.RewardCommand.CommandType);
            Assert.AreEqual(6, result.RewardCommand.Genre);
            Assert.AreEqual(1, result.RewardCommand.Detail);
            Assert.AreEqual(4681, result.RewardCommand.Particular);
            Assert.IsFalse(result.ShouldEmitGlobalNews);
            Assert.IsTrue(result.ShouldWriteLog);
        }

        private static CityHongbaoOpenResultService LoadService()
        {
            var rows = PcCityHongbaoParser.ParseFile(CityHongbaoPath);
            Assert.AreEqual(67, rows.Count);
            return new CityHongbaoOpenResultService(rows);
        }
    }
}
