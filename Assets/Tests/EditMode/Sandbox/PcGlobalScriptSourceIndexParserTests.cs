using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcGlobalScriptSourceIndexParserTests
    {
        private static string IndexPath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcGlobalScript/global_script_index.txt");

        private static string IndexDir => Path.GetDirectoryName(IndexPath);

        [Test]
        public void ParseFile_ReadsPcScriptGlobalSourceIndex_NoRuntimeClaim()
        {
            Assert.IsTrue(File.Exists(IndexPath));
            var registry = PcGlobalScriptSourceIndexParser.BuildRegistry(IndexDir);

            Assert.AreEqual(672, registry.Count);
            Assert.AreEqual(579, registry.FileCount);
            Assert.AreEqual(93, registry.DirectoryCount,
                "PC script/global has 93 relative subdirectories under the scoped root; root itself is not indexed as a relative row.");
            Assert.AreEqual(526, registry.LuaFileCount);
            Assert.AreEqual(53, registry.NonLuaFileCount,
                "Non-Lua rows are source evidence such as PC log/ini files, not runnable Lua scripts.");
        }

        [Test]
        public void RepresentativeRows_PreservePcRelativePathsSizesAndSha256()
        {
            var registry = PcGlobalScriptSourceIndexParser.BuildRegistry(IndexDir);
            AssertEntry(registry, "cn/extpointfunc_proc.lua", "cn", "extpointfunc_proc.lua", 1643,
                "851310532f43bbd03f51abe174f273e28f670c58483434ace585b058f5096d5f");
            AssertEntry(registry, "vn/gamebank_proc.lua", "vn", "gamebank_proc.lua", 5154,
                "13da69b5ceb066434b54ba7b3a7fe655af061d218683bcca7bb9de2312127c76");
            AssertEntry(registry, "worldrank/head.lua", "worldrank", "head.lua", 861,
                "cf24ff4cfba4930656e88219639ffe984d2bb141804c0cd1a23d4a77459800ad");
            AssertEntry(registry, "newworld/citydefence_newworld.lua", "newworld", "citydefence_newworld.lua", 363,
                "824d6dc61b0928d90ec2e04aadea410b3a193755a5cddda8a7381afae36d6b20");
            AssertEntry(registry, "npc/event.lua", "npc", "event.lua", 1072,
                "c906bdbc3322a3d1e1307ca4a16118130cee95ea4dfca4e8210c4c69370333c4");
        }

        [Test]
        public void Registry_GroupsRepresentativePcDirectoriesAndNonLuaEvidence()
        {
            var registry = PcGlobalScriptSourceIndexParser.BuildRegistry(IndexDir);
            var log = registry.GetByRelativePath("pgaming/cobac/baucua/logs/baucua_01_03_2026.log");

            Assert.AreEqual(3, registry.GetByDirectory("cn").Count);
            Assert.AreEqual(3, registry.GetByDirectory("vn").Count);
            Assert.AreEqual(2, registry.GetByDirectory("worldrank").Count);
            Assert.AreEqual(3, registry.GetByDirectory("newworld").Count);
            Assert.AreEqual(6, registry.GetByDirectory("npc").Count);
            Assert.AreEqual(52, registry.GetByDirectory("pgaming/cobac/baucua/logs").Count);
            Assert.IsNotNull(log);
            Assert.IsTrue(log.IsFile);
            Assert.IsFalse(log.isLua);
            Assert.AreEqual("log", log.extension);
            Assert.AreEqual(1944, log.sizeBytes);
            Assert.AreEqual("f5f92142efcee413d4a9e489f900ab79755351bcab621daab6e409dc7b1fc1e2", log.sha256);
        }

        [Test]
        public void Service_LoadsCommittedStreamingAssetsIndex()
        {
            var service = GlobalScriptSourceIndexService.LoadFromStreamingAssets();

            Assert.AreEqual(579, service.FileCount);
            Assert.AreEqual(93, service.DirectoryCount);
            Assert.AreEqual(526, service.LuaFileCount);
            Assert.AreEqual(3428529L, service.TotalSizeBytes);
            Assert.IsNotNull(service.GetByRelativePath("npc/event.lua"));
        }

        [Test]
        public void MissingInputs_ReturnEmptyCatalog()
        {
            Assert.AreEqual(0, PcGlobalScriptSourceIndexParser.ParseFile("/tmp/not-a-real-global-script-index.txt").Count);
            Assert.AreEqual(0, PcGlobalScriptSourceIndexParser.BuildRegistry("/tmp/not-a-real-global-script-index-dir").Count);
            Assert.AreEqual(0, GlobalScriptSourceIndexService.LoadFromFile("/tmp/not-a-real-global-script-index.txt").Count);
        }

        private static void AssertEntry(
            PcGlobalScriptSourceIndexRegistry registry,
            string relativePath,
            string directory,
            string fileName,
            long sizeBytes,
            string sha256)
        {
            var row = registry.GetByRelativePath(relativePath);
            Assert.IsNotNull(row, relativePath);
            Assert.AreEqual(GlobalScriptSourceIndexService.PcSourceRoot, row.sourceRoot);
            Assert.IsTrue(row.IsFile);
            Assert.IsTrue(row.isLua);
            Assert.AreEqual(directory, row.directory);
            Assert.AreEqual(fileName, row.fileName);
            Assert.AreEqual("lua", row.extension);
            Assert.AreEqual(sizeBytes, row.sizeBytes);
            Assert.AreEqual(sha256, row.sha256);
        }
    }
}
