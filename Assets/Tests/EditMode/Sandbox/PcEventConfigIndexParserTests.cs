using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcEventConfigIndexParserTests
    {
        private static string IndexPath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcEventConfig/event_config_source_index.txt");

        private static string IndexDir => Path.GetDirectoryName(IndexPath);

        [Test]
        public void ParseFile_ReadsClientAndServerSettingsEventSourceCatalogOnly()
        {
            Assert.IsTrue(File.Exists(IndexPath));
            var rows = PcEventConfigIndexParser.ParseFile(IndexPath);

            Assert.AreEqual(54, rows.Count,
                "18 client + 18 server/home_jxser + 18 server/home_jxser_bachkim settings/event files; catalog/schema evidence only.");
            Assert.AreEqual(18, rows.FindAll(r => r.rootId == EventConfigIndexService.ClientRootId).Count);
            Assert.AreEqual(18, rows.FindAll(r => r.rootId == EventConfigIndexService.ServerJxserRootId).Count);
            Assert.AreEqual(18, rows.FindAll(r => r.rootId == EventConfigIndexService.ServerBachKimRootId).Count);
            Assert.AreEqual(18, rows.FindAll(r => r.side == "client").Count);
            Assert.AreEqual(36, rows.FindAll(r => r.side == "server").Count);
            Assert.AreEqual(54, rows.FindAll(r => r.isTextLike && r.extension == "txt").Count);
        }

        [Test]
        public void Registry_PreservesRepresentativeTxtSchemasCountsAndHashes()
        {
            var registry = PcEventConfigIndexParser.BuildRegistry(IndexDir);
            var bonus = registry.GetByRootPath(EventConfigIndexService.ClientRootId, "chinesenewyear/bonuslist.txt");
            var riddle = registry.GetByRootPath(EventConfigIndexService.ClientRootId, "riddle/huadeng.txt");
            var vnText = registry.GetByRootPath(EventConfigIndexService.ServerJxserRootId, "other/shensuanzi/vn.txt");
            var midAutumn = registry.GetByRootPath(EventConfigIndexService.ServerBachKimRootId, "zhongqiuhuodong/zhongqiudengmi.txt");

            Assert.IsNotNull(bonus);
            Assert.AreEqual("Client 6.0/settings/event", bonus.sourceRoot);
            Assert.AreEqual(15867, bonus.sizeBytes);
            Assert.AreEqual("3d3dff473b5dadb8af0102a635fe6c87e6988e503d5980ee2b4254d1137a8b2e", bonus.sha256);
            Assert.AreEqual(106, bonus.dataRowCount);
            Assert.AreEqual(19, bonus.columnCount);
            Assert.AreEqual("mID|Type|TypeName|Name|Worth|P1|P2|P3|P4|P5|P6|P7|P8|P9|P10|P11|P12|Message|Announce", bonus.headerSignature);

            Assert.IsNotNull(riddle);
            Assert.AreEqual(105809, riddle.sizeBytes);
            Assert.AreEqual("6022d9536ffcc305a12db66cc625dcb4b4e500038757e39a1e31b97581dcdc78", riddle.sha256);
            Assert.AreEqual(1081, riddle.dataRowCount);
            Assert.AreEqual(7, riddle.columnCount);
            Assert.AreEqual("STT|Tr¶ lêi|C©u Hái||B|C|D", riddle.headerSignature);

            Assert.IsNotNull(vnText);
            Assert.AreEqual(760, vnText.sizeBytes);
            Assert.AreEqual("d821dc28ed6f738545d85e073af28e266d78b1cfeec0cc96086dbe705c44c49c", vnText.sha256);
            Assert.AreEqual(19, vnText.dataRowCount);
            Assert.AreEqual(2, vnText.columnCount);
            Assert.AreEqual("RoundStartMsg|C©u hái cña ®ît %d b¾t ®Çu", vnText.headerSignature);

            Assert.IsNotNull(midAutumn);
            Assert.AreEqual(12103, midAutumn.sizeBytes);
            Assert.AreEqual("6d2a39a9d6b135f6e85bbd59b37639cb4013e85a75a71dc27c86c63a96af2890", midAutumn.sha256);
            Assert.AreEqual(126, midAutumn.dataRowCount);
            Assert.AreEqual(8, midAutumn.columnCount);
            Assert.AreEqual("STT|§¸p ¸n|C©u hái|A|B|C|D|", midAutumn.headerSignature);
        }

        [Test]
        public void Service_LoadsDefaultStreamingAssetsIndexAndGroupsBySide()
        {
            var service = EventConfigIndexService.LoadFromStreamingAssets();

            Assert.AreEqual(54, service.Count);
            Assert.AreEqual(18, service.ClientFileCount);
            Assert.AreEqual(36, service.ServerFileCount);
            Assert.AreEqual(54, service.TextFileCount);
            Assert.AreEqual(18, service.GetByRoot(EventConfigIndexService.ClientRootId).Count);
            Assert.AreEqual(36, service.GetBySide("server").Count);
            Assert.IsNotNull(service.GetByRootPath(EventConfigIndexService.ClientRootId, "wangwanglibao/gift_pack.txt"));
            Assert.Greater(service.TotalSizeBytes, 0L);
        }

        [Test]
        public void MissingInputs_ReturnEmptyCatalog()
        {
            Assert.AreEqual(0, PcEventConfigIndexParser.ParseFile("/tmp/not-a-real-event-config-index.txt").Count);
            Assert.AreEqual(0, PcEventConfigIndexParser.BuildRegistry("/tmp/not-a-real-event-config-index-dir").Count);
            Assert.AreEqual(0, EventConfigIndexService.LoadFromFile("/tmp/not-a-real-event-config-index.txt").Count);
        }
    }
}
