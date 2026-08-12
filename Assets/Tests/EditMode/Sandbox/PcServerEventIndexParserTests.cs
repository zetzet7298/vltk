using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcServerEventIndexParserTests
    {
        private static string IndexPath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcServerEvent/server_event_index.txt");

        private static string IndexDir => Path.GetDirectoryName(IndexPath);

        [Test]
        public void ParseFile_ReadsPcScriptEventFileCatalog_NotFabricatedEventsTxt()
        {
            Assert.IsTrue(File.Exists(IndexPath));
            var rows = PcServerEventIndexParser.ParseFile(IndexPath);

            Assert.AreEqual(455, rows.Count,
                "PC script/event has 455 source files total; this is a file catalog, not a settings/events.txt semantic event table.");
            Assert.AreEqual(427, rows.FindAll(r => r.isLua).Count);
            Assert.AreEqual(28, rows.FindAll(r => !r.isLua).Count);
            Assert.AreEqual(28, rows.FindAll(r => r.isCvsMetadata).Count);
        }

        [Test]
        public void Registry_PreservesRepresentativeSourceFileHashesAndSizes()
        {
            var registry = PcServerEventIndexParser.BuildRegistry(IndexDir);
            var nationalDay = registry.GetByRelativePath("2006vm_nationalday/event.lua");
            var cvsEntries = registry.GetByRelativePath("mid_autumn06/item/cvs/entries");

            Assert.IsNotNull(nationalDay);
            Assert.AreEqual("Server 6.0/server/home_jxser/server1/script/event", nationalDay.sourceRoot);
            Assert.AreEqual(1, nationalDay.sourceIndex);
            Assert.AreEqual("2006vm_nationalday", nationalDay.directory);
            Assert.AreEqual("event.lua", nationalDay.fileName);
            Assert.AreEqual("lua", nationalDay.extension);
            Assert.IsTrue(nationalDay.isLua);
            Assert.IsFalse(nationalDay.isCvsMetadata);
            Assert.AreEqual(3759, nationalDay.sizeBytes);
            Assert.AreEqual("69b609cc961e1cf6d5c37d0e131b0eefda5c3154cb90f88a046ad26b9b9eab67", nationalDay.sha256);

            Assert.IsNotNull(cvsEntries);
            Assert.IsFalse(cvsEntries.isLua);
            Assert.IsTrue(cvsEntries.isCvsMetadata);
            Assert.AreEqual(119, cvsEntries.sizeBytes);
            Assert.AreEqual("8f503c06ebd803857a19b1900533b046381be7f84b507d2d2510fb8b5ff438c7", cvsEntries.sha256);
        }

        [Test]
        public void Registry_GroupsByPcRelativeDirectoryAndTracksTotals()
        {
            var registry = PcServerEventIndexParser.BuildRegistry(IndexDir);

            Assert.AreEqual(455, registry.Count);
            Assert.AreEqual(427, registry.LuaFileCount);
            Assert.AreEqual(28, registry.NonLuaFileCount);
            Assert.AreEqual(28, registry.CvsMetadataFileCount);
            Assert.AreEqual(7, registry.GetByDirectory("bingo_machine").Count);
            Assert.AreEqual(7, registry.GetByDirectory("mid_autumn06/item/cvs").Count);
            Assert.Greater(registry.TotalSizeBytes, 0L);
        }

        [Test]
        public void Service_LoadsDefaultStreamingAssetsIndex()
        {
            var service = ServerEventIndexService.LoadFromStreamingAssets();

            Assert.AreEqual(455, service.Count);
            Assert.AreEqual(427, service.LuaFileCount);
            Assert.AreEqual(28, service.CvsMetadataFileCount);
            Assert.IsNotNull(service.GetByRelativePath("zhongqiu_jieri/help.lua"));
            Assert.AreEqual(7, service.GetByDirectory("bingo_machine").Count);
        }

        [Test]
        public void MissingInputs_ReturnEmptyCatalog()
        {
            Assert.AreEqual(0, PcServerEventIndexParser.ParseFile("/tmp/not-a-real-server-event-index.txt").Count);
            Assert.AreEqual(0, PcServerEventIndexParser.BuildRegistry("/tmp/not-a-real-server-event-index-dir").Count);
            Assert.AreEqual(0, ServerEventIndexService.LoadFromFile("/tmp/not-a-real-server-event-index.txt").Count);
        }
    }
}
