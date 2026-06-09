using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcActivitySysIndexParserTests
    {
        private static string IndexDir => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcActivitySys");

        private static string SourceIndexPath => Path.Combine(IndexDir, PcActivitySysIndexParser.SourceIndexFileName);
        private static string ConfigIndexPath => Path.Combine(IndexDir, PcActivitySysIndexParser.ConfigIndexFileName);

        [Test]
        public void ParseFile_ReadsPcActivitySysSourceAndConfigCatalogs_NoRuntimeClaim()
        {
            Assert.IsTrue(File.Exists(SourceIndexPath));
            Assert.IsTrue(File.Exists(ConfigIndexPath));

            var sourceRows = PcActivitySysIndexParser.ParseFile(SourceIndexPath);
            var configRows = PcActivitySysIndexParser.ParseFile(ConfigIndexPath);

            Assert.AreEqual(496, sourceRows.Count,
                "PC script/activitysys has 496 source files; this is a source-file catalog, not an activity runtime implementation.");
            Assert.AreEqual(494, sourceRows.FindAll(r => r.isLua).Count);
            Assert.AreEqual(2, sourceRows.FindAll(r => !r.isLua).Count);
            Assert.AreEqual(87, configRows.Count,
                "PC settings/activitysys has 87 config txt files indexed as evidence only.");
            Assert.AreEqual(87, configRows.FindAll(r => r.isTextConfig).Count);
        }

        [Test]
        public void Registry_PreservesRepresentativeSourceAndConfigHashesAndSizes()
        {
            var registry = PcActivitySysIndexParser.BuildRegistry(IndexDir);
            var gActivity = registry.GetByRelativePath("source", "g_activity.lua");
            var detailLogin = registry.GetByRelativePath("source", "detailtype/login.lua");
            var activityConfig = registry.GetByRelativePath("config", "activity.txt");
            var awardOne = registry.GetByRelativePath("config", "awardtable/1.txt");

            Assert.IsNotNull(gActivity);
            Assert.AreEqual("Server 6.0/server/home_jxser/server1/script/activitysys", gActivity.sourceRoot);
            Assert.AreEqual(486, gActivity.sourceIndex);
            Assert.AreEqual("source", gActivity.indexKind);
            Assert.AreEqual("g_activity.lua", gActivity.fileName);
            Assert.AreEqual(2613, gActivity.sizeBytes);
            Assert.AreEqual("a3a7e26e3bdfb432efaca3463d57d8eaf59274793a97c886a5a58fc6a304869a", gActivity.sha256);

            Assert.IsNotNull(detailLogin);
            Assert.AreEqual("detailtype", detailLogin.directory);
            Assert.AreEqual(352, detailLogin.sizeBytes);
            Assert.AreEqual("97606ab24766195a6293b41fa1893225e7d2972a0426b70ea8145a7972164387", detailLogin.sha256);

            Assert.IsNotNull(activityConfig);
            Assert.AreEqual("Server 6.0/server/home_jxser/server1/settings/activitysys", activityConfig.sourceRoot);
            Assert.AreEqual("config", activityConfig.indexKind);
            Assert.IsTrue(activityConfig.isTextConfig);
            Assert.AreEqual(1303, activityConfig.sizeBytes);
            Assert.AreEqual("53612e27cebdda6021ec1dc3c42001dae4f1d47e595340f6d306fa046e23a2a2", activityConfig.sha256);

            Assert.IsNotNull(awardOne);
            Assert.AreEqual("awardtable", awardOne.directory);
            Assert.AreEqual(1735, awardOne.sizeBytes);
            Assert.AreEqual("36802ed12d672cf08584bdbcca05488329a79bde5095861c723e66c611a9fd71", awardOne.sha256);
        }

        [Test]
        public void Registry_GroupsByKindAndTracksTotals()
        {
            var registry = PcActivitySysIndexParser.BuildRegistry(IndexDir);

            Assert.AreEqual(583, registry.Count);
            Assert.AreEqual(496, registry.SourceFileCount);
            Assert.AreEqual(87, registry.ConfigFileCount);
            Assert.AreEqual(494, registry.LuaFileCount);
            Assert.AreEqual(87, registry.TextConfigFileCount);
            Assert.AreEqual(17, registry.GetByDirectory("source", string.Empty).Count);
            Assert.AreEqual(9, registry.GetByDirectory("source", "config/40").Count);
            Assert.AreEqual(24, registry.GetByDirectory("source", "detailtype").Count);
            Assert.AreEqual(83, registry.GetByDirectory("config", "awardtable").Count);
            Assert.Greater(registry.TotalSizeBytes, 0L);
        }

        [Test]
        public void Service_LoadsDefaultStreamingAssetsIndex()
        {
            var service = ActivitySysIndexService.LoadFromStreamingAssets();

            Assert.AreEqual(583, service.Count);
            Assert.AreEqual(496, service.SourceFileCount);
            Assert.AreEqual(87, service.ConfigFileCount);
            Assert.IsNotNull(service.GetSource("config/40/head.lua"));
            Assert.AreEqual(296, service.GetSource("config/40/head.lua").sizeBytes);
            Assert.IsNotNull(service.GetConfig("42/npcpos.txt"));
            Assert.AreEqual(1, service.GetConfigDirectory("42").Count);
        }

        [Test]
        public void MissingInputs_ReturnEmptyCatalog()
        {
            Assert.AreEqual(0, PcActivitySysIndexParser.ParseFile("/tmp/not-a-real-activitysys-index.txt").Count);
            Assert.AreEqual(0, PcActivitySysIndexParser.BuildRegistry("/tmp/not-a-real-activitysys-index-dir").Count);
            Assert.AreEqual(0, ActivitySysIndexService.LoadFromDirectory("/tmp/not-a-real-activitysys-index-dir").Count);
        }
    }
}
