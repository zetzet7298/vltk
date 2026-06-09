using System.IO;
using System.Linq;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcCityHongbaoParserTests
    {
        private static string CityHongbaoPath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcItemFull/chengshidahongbao.txt");

        private static string CityHongbaoDir => Path.GetDirectoryName(CityHongbaoPath);

        [Test]
        public void ParseFile_ReadsAllPcCityHongbaoRowsAndColumns()
        {
            var rows = PcCityHongbaoParser.ParseFile(CityHongbaoPath);

            Assert.AreEqual(67, rows.Count, "PC chengshidahongbao.txt has 67 data rows");
            Assert.AreEqual(17, PcCityHongbaoParser.ExpectedColumnCount);
            Assert.AreEqual(6, PcCityHongbaoParser.ParamCount);
            Assert.AreEqual(1010000, rows.Sum(r => r.Proba), "PC city hongbao total weight is not normalized to 1,000,000");
        }

        [Test]
        public void ParseFile_PreservesFullKBonusTupleForTypeOneRows()
        {
            var first = PcCityHongbaoParser.ParseFile(CityHongbaoPath)[0];

            Assert.AreEqual(1, first.Id);
            Assert.AreEqual(1, first.Type);
            Assert.AreEqual(4, first.Genre);
            Assert.AreEqual(238, first.Detail);
            Assert.AreEqual(1, first.Particular);
            Assert.AreEqual(0, first.Serise);
            Assert.AreEqual(1, first.Level);
            Assert.AreEqual(0, first.Param[0]);
            Assert.AreEqual(50000, first.Proba);
            Assert.AreEqual(0, first.Costly);
            Assert.AreEqual(1, first.Log);
            Assert.IsTrue(first.Msg.Contains("<player>"));
            Assert.IsTrue(first.Msg.Contains("<name>"));
        }

        [Test]
        public void ParseFile_PreservesGoldenItemRowsAndLogFlags()
        {
            var rows = PcCityHongbaoParser.ParseFile(CityHongbaoPath);
            var firstGolden = rows.First(r => r.Type == 2);

            Assert.AreEqual(13, firstGolden.Id);
            Assert.AreEqual(2, firstGolden.Genre, "Type=2 uses Genre as AddGoldItem id in PC KBonus");
            Assert.AreEqual(0, firstGolden.Detail);
            Assert.AreEqual(0, firstGolden.Particular);
            Assert.AreEqual(60, firstGolden.Proba);
            Assert.AreEqual(1, firstGolden.Costly);
            Assert.AreEqual(1, firstGolden.Log);
            Assert.AreEqual(54, rows.Count(r => r.Type == 2));
            Assert.AreEqual(13, rows.Count(r => r.Type == 1));
        }

        [Test]
        public void BuildRegistry_LoadsPcItemFullCityHongbaoByDefaultShape()
        {
            var registry = PcCityHongbaoParser.BuildRegistry(CityHongbaoDir);

            Assert.AreEqual(67, registry.Count);
            Assert.AreEqual(1010000, registry.TotalProba);
            Assert.AreEqual(4681, registry.Get(67).Particular);
            Assert.AreEqual(10000, registry.Get(67).Proba);
            Assert.AreEqual(67, CityHongbaoService.LoadFromStreamingAssets().Count);
        }
    }
}
