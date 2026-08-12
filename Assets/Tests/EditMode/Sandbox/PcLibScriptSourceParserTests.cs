using System.IO;
using System.Linq;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcLibScriptSourceParserTests
    {
        private static string SourceFile => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcLibScript/lib_scripts.txt");

        [Test]
        public void ImportedCatalog_PreservesExactPcServerLibScriptCounts()
        {
            Assert.IsTrue(File.Exists(SourceFile));
            var catalog = PcLibScriptSourceParser.BuildCatalog(SourceFile);

            Assert.AreEqual(52, catalog.Count,
                "PC Server script/lib under home_jxser/server1 contains 52 files; all are .lua source files.");
            Assert.AreEqual(52, catalog.LuaCount);
            Assert.AreEqual(2, catalog.DirectoryCount);
            Assert.AreEqual(173001, catalog.TotalSizeBytes);
        }

        [Test]
        public void DirectoryCounts_MatchPcScriptLibTree()
        {
            var catalog = PcLibScriptSourceParser.BuildCatalog(SourceFile);

            Assert.AreEqual(43, catalog.GetDirectoryCount("."));
            Assert.AreEqual(9, catalog.GetDirectoryCount("awardtype"));
        }

        [Test]
        public void RepresentativeRows_PreservePcPathsSizeAndSha()
        {
            var catalog = PcLibScriptSourceParser.BuildCatalog(SourceFile);

            AssertEntry(catalog.Get("droptemplet.lua"), ".", "droptemplet.lua", 5848,
                "b2e2257667e6e106b0af2e5fb5b275ff60b03f216b6d3beef877a93cdf4b2756");
            AssertEntry(catalog.Get("gb_taskfuncs.lua"), ".", "gb_taskfuncs.lua", 2274,
                "d1fa990d48e34bee8e57442516d08409c7576a7c00b7cb874f5575b989aad263");
            AssertEntry(catalog.Get("awardtype/exp.lua"), "awardtype", "exp.lua", 590,
                "59756697b20d151d300d5a153845dcc582ca43c6b4fdbfb71fb32b6533e7aff3");
            AssertEntry(catalog.Get("awardtype/item.lua"), "awardtype", "item.lua", 3693,
                "687c35664195cbf868ca0ee6dfd6eeeb213510fa9911431c04860ac7a1f1cd39");
        }

        [Test]
        public void ParserRows_AreDataOnlyAndDoNotClaimLuaRuntime()
        {
            var rows = PcLibScriptSourceParser.ParseFile(SourceFile);

            Assert.IsTrue(rows.All(r => r.isLua && r.fileKind == "lua"));
            Assert.IsTrue(PcLibScriptSourceParser.NoLuaRuntimeClaim.Contains("does not parse or execute Lua"));
            Assert.IsTrue(LibScriptSourceCatalogService.NoLuaRuntimeClaim.Contains("does not parse or execute Lua"));
        }

        [Test]
        public void Service_LoadFromStreamingAssets_IndexesCommittedCatalog()
        {
            var service = LibScriptSourceCatalogService.LoadFromStreamingAssets();

            Assert.AreEqual(52, service.Count);
            Assert.AreEqual(52, service.LuaCount);
            Assert.AreEqual(2, service.DirectoryCount);
            Assert.AreEqual(43, service.GetDirectoryCount("."));
            Assert.AreEqual("gb_taskfuncs.lua", service.GetByRelativePath("gb_taskfuncs.lua").fileName);
        }

        private static void AssertEntry(PcLibScriptSourceEntry entry, string directory, string fileName, long sizeBytes, string sha256)
        {
            Assert.IsNotNull(entry);
            Assert.AreEqual(directory, entry.directory);
            Assert.AreEqual(fileName, entry.fileName);
            Assert.IsTrue(entry.isLua);
            Assert.AreEqual(sizeBytes, entry.sizeBytes);
            Assert.AreEqual(sha256, entry.sha256);
        }
    }
}
