using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcHonorWorldRankSourceIndexParserTests
    {
        private static string IndexPath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcHonorWorldRank/honor_worldrank_source_index.txt");

        private static string IndexDir => Path.GetDirectoryName(IndexPath);

        [Test]
        public void ParseFile_ReadsPcHonorWorldRankSourceCatalogOnly()
        {
            Assert.IsTrue(File.Exists(IndexPath));
            var rows = PcHonorWorldRankSourceIndexParser.ParseFile(IndexPath);

            Assert.AreEqual(10, rows.Count,
                "Catalog covers PC source files only: 6 honor Lua, 2 worldrank Lua, and 2 ranksetting evidence files; no runtime rank/honor behavior is claimed.");
            Assert.AreEqual(8, rows.FindAll(r => r.isLua).Count);
            Assert.AreEqual(6, rows.FindAll(r => r.category == "honor" && r.isLua).Count);
            Assert.AreEqual(2, rows.FindAll(r => r.category == "worldrank" && r.isLua).Count);
            Assert.AreEqual(2, rows.FindAll(r => r.isSettings).Count);
        }

        [Test]
        public void Registry_PreservesRepresentativeSourceFileHashesAndSizes()
        {
            var registry = PcHonorWorldRankSourceIndexParser.BuildRegistry(IndexDir);
            var honorHead = registry.GetBySourcePath(
                HonorWorldRankSourceIndexService.HonorPcSourceRoot,
                "honor_head.lua");
            var worldRankLib = registry.GetBySourcePath(
                HonorWorldRankSourceIndexService.WorldRankPcSourceRoot,
                "lib.lua");
            var serverRankSetting = registry.GetBySourcePath(
                HonorWorldRankSourceIndexService.ServerRankSettingPcSourceRoot,
                "ranksetting.txt");
            var clientRankSetting = registry.GetBySourcePath(
                HonorWorldRankSourceIndexService.ClientRankSettingPcSourceRoot,
                "ranksetting.txt");

            Assert.IsNotNull(honorHead);
            Assert.AreEqual(2, honorHead.sourceIndex);
            Assert.AreEqual("honor", honorHead.category);
            Assert.AreEqual(14691, honorHead.sizeBytes);
            Assert.AreEqual("de9a3121cea3423c03721a0602b0e73ffdbf9c474b711ed4dff50f99e9afda64", honorHead.sha256);

            Assert.IsNotNull(worldRankLib);
            Assert.AreEqual(8, worldRankLib.sourceIndex);
            Assert.AreEqual("worldrank", worldRankLib.category);
            Assert.AreEqual(5316, worldRankLib.sizeBytes);
            Assert.AreEqual("f8bf03f40111a3f215eab149b3fab0b8d2797f5bf35487b3e49776c8edc6f41a", worldRankLib.sha256);

            Assert.IsNotNull(serverRankSetting);
            Assert.IsFalse(serverRankSetting.isLua);
            Assert.IsTrue(serverRankSetting.isSettings);
            Assert.AreEqual(1718, serverRankSetting.sizeBytes);
            Assert.AreEqual("57eeb0d4d615184a885bb7dea8ba4063159dec28fbfce16010c4d174e2cfa35c", serverRankSetting.sha256);

            Assert.IsNotNull(clientRankSetting);
            Assert.AreEqual(2007, clientRankSetting.sizeBytes);
            Assert.AreEqual("be2ddd5a7c8d60238057d92904eac0d105cbe0dc7e4b1f71be9c26627b0a93ff", clientRankSetting.sha256);
        }

        [Test]
        public void Registry_GroupsByCategoryAndSourceRootAndTracksTotals()
        {
            var registry = PcHonorWorldRankSourceIndexParser.BuildRegistry(IndexDir);

            Assert.AreEqual(10, registry.Count);
            Assert.AreEqual(8, registry.LuaFileCount);
            Assert.AreEqual(6, registry.HonorLuaFileCount);
            Assert.AreEqual(2, registry.WorldRankLuaFileCount);
            Assert.AreEqual(2, registry.SettingsFileCount);
            Assert.AreEqual(40487, registry.TotalSizeBytes);
            Assert.AreEqual(6, registry.GetByCategory("honor").Count);
            Assert.AreEqual(2, registry.GetByCategory("worldrank").Count);
            Assert.AreEqual(2, registry.GetByCategory("settings").Count);
            Assert.AreEqual(6, registry.GetBySourceRoot(HonorWorldRankSourceIndexService.HonorPcSourceRoot).Count);
            Assert.AreEqual(2, registry.GetBySourceRoot(HonorWorldRankSourceIndexService.WorldRankPcSourceRoot).Count);
        }

        [Test]
        public void Service_LoadsDefaultStreamingAssetsIndex()
        {
            var service = HonorWorldRankSourceIndexService.LoadFromStreamingAssets();

            Assert.AreEqual(10, service.Count);
            Assert.AreEqual(8, service.LuaFileCount);
            Assert.AreEqual(6, service.HonorLuaFileCount);
            Assert.AreEqual(2, service.WorldRankLuaFileCount);
            Assert.AreEqual(2, service.SettingsFileCount);
            Assert.IsNotNull(service.GetBySourcePath(HonorWorldRankSourceIndexService.HonorPcSourceRoot, "honor_master.lua"));
            Assert.IsNotNull(service.GetBySourcePath(HonorWorldRankSourceIndexService.WorldRankPcSourceRoot, "head.lua"));
            Assert.AreEqual(2, service.GetByCategory("settings").Count);
        }

        [Test]
        public void MissingInputs_ReturnEmptyCatalog()
        {
            Assert.AreEqual(0, PcHonorWorldRankSourceIndexParser.ParseFile("/tmp/not-a-real-honor-worldrank-index.txt").Count);
            Assert.AreEqual(0, PcHonorWorldRankSourceIndexParser.BuildRegistry("/tmp/not-a-real-honor-worldrank-index-dir").Count);
            Assert.AreEqual(0, HonorWorldRankSourceIndexService.LoadFromFile("/tmp/not-a-real-honor-worldrank-index.txt").Count);
        }
    }
}
