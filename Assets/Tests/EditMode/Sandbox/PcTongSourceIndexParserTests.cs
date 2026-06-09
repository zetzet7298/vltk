using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcTongSourceIndexParserTests
    {
        private const string Server1ScriptRoot = "Server 6.0/server/home_jxser/server1/script/tong";
        private const string Server1SettingsRoot = "Server 6.0/server/home_jxser/server1/settings/tong";

        private static string SourceDir => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcTongSource");

        [Test]
        public void ImportedIndexes_PreservePcTongServerFileAndDirectoryCounts()
        {
            var catalog = PcTongSourceIndexParser.BuildCatalog(SourceDir);

            Assert.AreEqual(244, catalog.Count);
            Assert.AreEqual(172, catalog.SourceFileCount);
            Assert.AreEqual(72, catalog.ConfigFileCount);
            Assert.AreEqual(172, catalog.LuaFileCount);
            Assert.AreEqual(0, catalog.CvsMetadataCount);
            Assert.AreEqual(8, catalog.SourceRootCount);
            Assert.AreEqual(40, catalog.RootDirectoryCount);
        }

        [Test]
        public void DirectoryCounts_SeparateScriptSourceFromSettingsConfig()
        {
            var catalog = PcTongSourceIndexParser.BuildCatalog(SourceDir);

            Assert.AreEqual(40, catalog.CountDirectory("source", "."));
            Assert.AreEqual(48, catalog.CountDirectory("source", "workshop"));
            Assert.AreEqual(28, catalog.CountDirectory("source", "npc"));
            Assert.AreEqual(20, catalog.CountDirectory("config", "."));
            Assert.AreEqual(16, catalog.CountDirectory("config", "task"));
            Assert.AreEqual(36, catalog.CountDirectory("config", "workshop"));
        }

        [Test]
        public void RepresentativeScriptRows_PreservePathSizeAndShaOnly()
        {
            var catalog = PcTongSourceIndexParser.BuildCatalog(SourceDir);
            var tongMix = catalog.Get(Server1ScriptRoot, "tong_mix.lua");
            var addTongNpc = catalog.Get(Server1ScriptRoot, "addtongnpc.lua");
            var workshopTask = catalog.Get(Server1ScriptRoot, "workshop/tongcolltask.lua");

            Assert.IsNotNull(tongMix);
            Assert.AreEqual(42322, tongMix.sizeBytes);
            Assert.AreEqual("8eaf59bac363e4d6506b110ad27d7d483057952d3ad76776666086dc04064497", tongMix.sha256);
            Assert.IsTrue(addTongNpc.isLua);
            Assert.AreEqual(6859, addTongNpc.sizeBytes);
            Assert.AreEqual("a17305c8d6d92a266e793d0551367764fa9863d3868447691d0bc8aba4c83b30", addTongNpc.sha256);
            Assert.AreEqual("workshop", workshopTask.directory);
            Assert.AreEqual("d5caa8279d7593b22c62771d7f158d24b612fe8202f0b4c27afea23f5c03201a", workshopTask.sha256);
        }

        [Test]
        public void RepresentativeConfigRows_PreserveTaskAndWorkshopEvidence()
        {
            var catalog = PcTongSourceIndexParser.BuildCatalog(SourceDir);
            var task = catalog.Get(Server1SettingsRoot, "task/tong_task_def.txt");
            var workshop = catalog.Get(Server1SettingsRoot, "workshop/workshops.txt");

            Assert.IsNotNull(task);
            Assert.AreEqual("config", task.sourceKind);
            Assert.IsFalse(task.isLua);
            Assert.AreEqual(600, task.sizeBytes);
            Assert.AreEqual("93fefe057d91f8d0ded371d6ce4a737a1e9d9f8d09956621fc53f135c97a6b1a", task.sha256);
            Assert.AreEqual("workshop", workshop.directory);
            Assert.AreEqual(1945, workshop.sizeBytes);
            Assert.AreEqual("92e6a03f7f5b2bf70b7e51772829aefece26af6fde8da503c8fa0e687470169f", workshop.sha256);
        }

        [Test]
        public void Service_LoadFromStreamingAssets_IndexesCommittedEvidenceCatalog()
        {
            var service = PcTongSourceIndexService.LoadFromStreamingAssets();

            Assert.AreEqual(244, service.FileCount);
            Assert.AreEqual(172, service.SourceFileCount);
            Assert.AreEqual(72, service.ConfigFileCount);
            Assert.AreEqual(172, service.LuaFileCount);
            Assert.AreEqual(8, service.SourceRootCount);
            Assert.IsNotNull(service.Get(Server1ScriptRoot, "addtongnpc.lua"));
        }
    }
}
