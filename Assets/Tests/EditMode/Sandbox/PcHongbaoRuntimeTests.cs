using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcHongbaoRuntimeTests
    {
        private static string HongbaoPath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcItemFull/hongbao.txt");

        private static string HongbaoDir => Path.GetDirectoryName(HongbaoPath);

        [Test]
        public void ParseFile_ReadsAllPcItemHongbaoRows()
        {
            var rows = PcHongbaoParser.ParseFile(HongbaoPath);
            Assert.AreEqual(69, rows.Count, "PC Client 6.0 settings/item/hongbao.txt has 69 data rows");
        }

        [Test]
        public void ParseFile_ReadsRepresentativeFieldsAndMessage()
        {
            var rows = PcHongbaoParser.ParseFile(HongbaoPath);
            var first = rows[0];

            Assert.AreEqual(1, first.id, "Runtime ids are stable 1-based hongbao.txt row numbers");
            Assert.AreEqual(1, first.type);
            Assert.AreEqual(6, first.itemGenre);
            Assert.AreEqual(1, first.itemDetail);
            Assert.AreEqual(71, first.itemParticular);
            Assert.AreEqual(0, first.serise);
            Assert.AreEqual(1, first.level);
            Assert.AreEqual(0, first.param[0]);
            Assert.AreEqual(200000, first.proba);
            Assert.AreEqual(0, first.costly);
            Assert.AreEqual(1, first.log);
            Assert.IsTrue(first.msg.Contains("<player>"));
            Assert.IsTrue(first.msg.Contains("<name>"));
        }

        [Test]
        public void BuildRegistry_LoadsHongbaoTxtFromPcItemFullDirectory()
        {
            var registry = PcHongbaoParser.BuildRegistry(HongbaoDir);
            Assert.AreEqual(69, registry.Count);
            Assert.AreEqual(159, registry.Get(2).itemGenre);
            Assert.AreEqual(1000, registry.Get(2).proba);
            Assert.AreEqual(1, registry.Get(2).costly);
        }

        [Test]
        public void LoadFromStreamingAssets_DefaultsToPcItemFullHongbao()
        {
            var service = HongbaoService.LoadFromStreamingAssets();
            Assert.AreEqual(69, service.Count);
            Assert.IsNotNull(service.GetHongbao(69));
            Assert.AreEqual(59, service.GetHongbao(69).itemParticular);
        }
    }
}
