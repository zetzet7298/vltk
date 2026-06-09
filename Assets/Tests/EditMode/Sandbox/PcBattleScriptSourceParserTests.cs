using System.IO;
using System.Linq;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcBattleScriptSourceParserTests
    {
        private static string SourceFile => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcBattleScript/battle_scripts.txt");

        [Test]
        public void ImportedCatalog_PreservesExactPcScriptBattleFileCounts()
        {
            Assert.IsTrue(File.Exists(SourceFile));
            var catalog = PcBattleScriptSourceParser.BuildCatalog(SourceFile);

            Assert.AreEqual(183, catalog.Count,
                "PC script/battles contains 183 files: 182 active .lua scripts plus boss/mission.lua.bak.");
            Assert.AreEqual(182, catalog.ActiveLuaCount);
            Assert.AreEqual(1, catalog.BackupFileCount);
            Assert.AreEqual(10, catalog.DirectoryCount);
        }

        [Test]
        public void DirectoryCounts_MatchPcScriptBattleTree()
        {
            var catalog = PcBattleScriptSourceParser.BuildCatalog(SourceFile);

            Assert.AreEqual(21, catalog.GetDirectoryCount("."));
            Assert.AreEqual(21, catalog.GetDirectoryCount("boss"));
            Assert.AreEqual(19, catalog.GetDirectoryCount("butcher"));
            Assert.AreEqual(18, catalog.GetDirectoryCount("guozhan"));
            Assert.AreEqual(19, catalog.GetDirectoryCount("jianta"));
            Assert.AreEqual(20, catalog.GetDirectoryCount("marshal"));
            Assert.AreEqual(20, catalog.GetDirectoryCount("seizeflag"));
            Assert.AreEqual(21, catalog.GetDirectoryCount("seizegrain"));
            Assert.AreEqual(6, catalog.GetDirectoryCount("singlefight"));
            Assert.AreEqual(18, catalog.GetDirectoryCount("tongkimxua"));
            Assert.AreEqual(20, catalog.GetActiveLuaDirectoryCount("boss"));
        }

        [Test]
        public void BackupRow_IsCatalogedButNotActiveLua()
        {
            var catalog = PcBattleScriptSourceParser.BuildCatalog(SourceFile);
            var backup = catalog.Get("boss/mission.lua.bak");

            Assert.IsNotNull(backup);
            Assert.AreEqual("boss", backup.directory);
            Assert.AreEqual("lua_backup", backup.fileKind);
            Assert.IsFalse(backup.isActiveLua);
        }

        [Test]
        public void RepresentativeRows_PreservePcRelativePathsOnly()
        {
            var rows = PcBattleScriptSourceParser.ParseFile(SourceFile);

            Assert.IsTrue(rows.Any(r => r.relativePath == ".lua" && r.directory == "." && r.isActiveLua));
            Assert.IsTrue(rows.Any(r => r.relativePath == "battlehead.lua" && r.directory == "."));
            Assert.IsTrue(rows.Any(r => r.relativePath == "seizegrain/grainobj.lua" && r.directory == "seizegrain"));
            Assert.IsTrue(rows.Any(r => r.relativePath == "singlefight/dt_mission.lua" && r.directory == "singlefight"));
        }

        [Test]
        public void Service_LoadFromStreamingAssets_IndexesCommittedCatalog()
        {
            var service = BattleScriptSourceCatalogService.LoadFromStreamingAssets();

            Assert.AreEqual(183, service.Count);
            Assert.AreEqual(182, service.ActiveLuaCount);
            Assert.AreEqual(1, service.BackupFileCount);
            Assert.AreEqual(21, service.GetDirectoryCount("boss"));
            Assert.AreEqual("mission.lua.bak", service.GetByRelativePath("boss/mission.lua.bak").fileName);
        }
    }
}
