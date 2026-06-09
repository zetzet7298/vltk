using System.IO;
using System.Linq;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcMissionScriptSourceParserTests
    {
        private static string SourceFile => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcMissionScript/mission_scripts.txt");

        [Test]
        public void ImportedCatalog_PreservesExactPcScriptMissionInventoryCounts()
        {
            Assert.IsTrue(File.Exists(SourceFile));
            var catalog = PcMissionScriptSourceParser.BuildCatalog(SourceFile);

            Assert.AreEqual(985, catalog.Count,
                "PC Server 6.0/server/home_jxser/server1/script/missions contains 985 files.");
            Assert.AreEqual(942, catalog.ActiveLuaCount);
            Assert.AreEqual(43, catalog.NonLuaFileCount);
            Assert.AreEqual(151, catalog.DirectoryCount);
            Assert.AreEqual(145, catalog.LuaDirectoryCount);
        }

        [Test]
        public void DirectoryCounts_MatchRepresentativePcMissionScriptTrees()
        {
            var catalog = PcMissionScriptSourceParser.BuildCatalog(SourceFile);

            Assert.AreEqual(1, catalog.GetDirectoryCount("."));
            Assert.AreEqual(1, catalog.GetActiveLuaDirectoryCount("."));
            Assert.AreEqual(7, catalog.GetDirectoryCount("clearskill"));
            Assert.AreEqual(7, catalog.GetActiveLuaDirectoryCount("clearskill"));
            Assert.AreEqual(7, catalog.GetDirectoryCount("basemission"));
            Assert.AreEqual(7, catalog.GetActiveLuaDirectoryCount("basemission"));
        }

        [Test]
        public void RepresentativeRows_PreservePcPathSizeAndShaFacts()
        {
            var catalog = PcMissionScriptSourceParser.BuildCatalog(SourceFile);

            AssertRow(catalog.Get("clearskill/head.lua"), "clearskill", "lua", 4729,
                "f57ce5fefd1af7ae730ab5f6346cedf8fcee333527fe324ee273c38bbac755c7");
            AssertRow(catalog.Get("clearskill/mission.lua"), "clearskill", "lua", 1166,
                "54e4d032c3b3dfad3ddb97bc79694c1c634b8fe8a11867725d109407c548dd94");
            AssertRow(catalog.Get("basemission/mission.lua"), "basemission", "lua", 295,
                "4d849e5708cdc9f30ee2a30f040975db7798a0909beb17c4b39f0ca5ae324993");
            AssertRow(catalog.Get("mission1.lua"), ".", "lua", 205,
                "e980ed82917a745ecea4cf4bb10184595ca2aaa48b640d41fab7d7b172302bac");
        }

        [Test]
        public void NonLuaRows_AreCatalogedButNotActiveLua()
        {
            var catalog = PcMissionScriptSourceParser.BuildCatalog(SourceFile);
            var backup = catalog.Get("leaguematch/schedule/newworld.lua.bak");

            Assert.IsNotNull(backup);
            Assert.AreEqual("leaguematch/schedule", backup.directory);
            Assert.AreEqual("lua_backup", backup.fileKind);
            Assert.AreEqual(1927, backup.sizeBytes);
            Assert.IsFalse(backup.isActiveLua);
        }

        [Test]
        public void Service_LoadFromStreamingAssets_IndexesCommittedCatalogOnly()
        {
            var service = MissionScriptSourceCatalogService.LoadFromStreamingAssets();

            Assert.AreEqual(985, service.Count);
            Assert.AreEqual(942, service.ActiveLuaCount);
            Assert.AreEqual(145, service.LuaDirectoryCount);
            Assert.AreEqual(7, service.GetDirectoryCount("clearskill"));
            Assert.AreEqual("mission1.lua", service.GetByRelativePath("mission1.lua").fileName);
            Assert.IsTrue(service.All.Any(r => r.relativePath == "basemission/class.lua"));
        }

        private static void AssertRow(
            PcMissionScriptSourceEntry entry,
            string directory,
            string fileKind,
            long sizeBytes,
            string sha256)
        {
            Assert.IsNotNull(entry);
            Assert.AreEqual(directory, entry.directory);
            Assert.AreEqual(fileKind, entry.fileKind);
            Assert.IsTrue(entry.isActiveLua);
            Assert.AreEqual(sizeBytes, entry.sizeBytes);
            Assert.AreEqual(sha256, entry.sha256);
        }
    }
}
