using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcVngEventIndexParserTests
    {
        private static string IndexPath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcVngEvent/vng_event_index.txt");

        private static string IndexDir => Path.GetDirectoryName(IndexPath);

        [Test]
        public void ParseFile_ReadsPcVngEventLuaSourceCatalog_NoLuaExecution()
        {
            Assert.IsTrue(File.Exists(IndexPath));
            var rows = PcVngEventIndexParser.ParseFile(IndexPath);

            Assert.AreEqual(195, rows.Count,
                "PC script/vng_event has 195 Lua source files in the scoped home_jxser server tree; this is a file catalog only.");
            Assert.AreEqual(195, rows.FindAll(r => r.isLua).Count);
            Assert.AreEqual(0, rows.FindAll(r => !r.isLua).Count);
        }

        [Test]
        public void Registry_PreservesRepresentativePcSourceHashesAndSizes()
        {
            var registry = PcVngEventIndexParser.BuildRegistry(IndexDir);
            var freezingStar = registry.GetByRelativePath("201010/item/freezingstar.lua");
            var vngAward = registry.GetByRelativePath("vuoncaysinhnhat/vng_award.lua");

            Assert.IsNotNull(freezingStar);
            Assert.AreEqual("Server 6.0/server/home_jxser/server1/script/vng_event", freezingStar.sourceRoot);
            Assert.AreEqual(2, freezingStar.sourceIndex);
            Assert.AreEqual("201010/item", freezingStar.directory);
            Assert.AreEqual("freezingstar.lua", freezingStar.fileName);
            Assert.AreEqual("lua", freezingStar.extension);
            Assert.IsTrue(freezingStar.isLua);
            Assert.AreEqual(25677, freezingStar.sizeBytes);
            Assert.AreEqual("cab01d905ff475580a11b524ca67b72aeb6d0d4d60c7aa9280317bd0d2f2282d", freezingStar.sha256);

            Assert.IsNotNull(vngAward);
            Assert.AreEqual(195, vngAward.sourceIndex);
            Assert.AreEqual(14165, vngAward.sizeBytes);
            Assert.AreEqual("8ea4b0618ea3e166a0092ee11813afebf27074f73443cf0090dc6a94c5cc68f0", vngAward.sha256);
        }

        [Test]
        public void Registry_GroupsByPcRelativeDirectoryAndTracksTotals()
        {
            var registry = PcVngEventIndexParser.BuildRegistry(IndexDir);

            Assert.AreEqual(195, registry.Count);
            Assert.AreEqual(195, registry.LuaFileCount);
            Assert.AreEqual(0, registry.NonLuaFileCount);
            Assert.AreEqual(65, registry.LuaDirectoryCount);
            Assert.AreEqual(68, registry.SourceDirectoryCount);
            Assert.AreEqual(4, registry.GetByDirectory(string.Empty).Count);
            Assert.AreEqual(2, registry.GetByDirectory("eventpgaming/thang7").Count);
            Assert.AreEqual(5, registry.GetByDirectory("vuoncaysinhnhat").Count);
            Assert.Greater(registry.TotalSizeBytes, 0L);
        }

        [Test]
        public void Service_LoadsDefaultStreamingAssetsIndex()
        {
            var service = VngEventIndexService.LoadFromStreamingAssets();

            Assert.AreEqual(195, service.Count);
            Assert.AreEqual(195, service.LuaFileCount);
            Assert.AreEqual(65, service.LuaDirectoryCount);
            Assert.AreEqual(68, service.SourceDirectoryCount);
            Assert.IsNotNull(service.GetByRelativePath("20110215_thdnb8/acclist.lua"));
            Assert.AreEqual(1, service.GetByDirectory("traogiai/npah/awards/2").Count);
        }

        [Test]
        public void MissingInputs_ReturnEmptyCatalog()
        {
            Assert.AreEqual(0, PcVngEventIndexParser.ParseFile("/tmp/not-a-real-vng-event-index.txt").Count);
            Assert.AreEqual(0, PcVngEventIndexParser.BuildRegistry("/tmp/not-a-real-vng-event-index-dir").Count);
            Assert.AreEqual(0, VngEventIndexService.LoadFromFile("/tmp/not-a-real-vng-event-index.txt").Count);
        }
    }
}
