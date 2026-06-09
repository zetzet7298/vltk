using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcSkillGocScriptIndexParserTests
    {
        private static string IndexPath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcSkillGocScript/skill_goc_source_index.txt");

        private static string IndexDir => Path.GetDirectoryName(IndexPath);

        [Test]
        public void ParseFile_ReadsPcSkillGocCatalog_NoLuaExecution()
        {
            Assert.IsTrue(File.Exists(IndexPath));
            var rows = PcSkillGocScriptIndexParser.ParseFile(IndexPath);

            Assert.AreEqual(737, rows.Count,
                "PC script/skill-goc source index is a file catalog only, not executable Lua runtime parity.");
            Assert.AreEqual(737, rows.FindAll(r => r.isLua).Count);
            Assert.AreEqual(0, rows.FindAll(r => !r.isLua).Count);
        }

        [Test]
        public void Registry_TracksFileDirLuaCountsAndTotalBytes()
        {
            var registry = PcSkillGocScriptIndexParser.BuildRegistry(IndexDir);

            Assert.AreEqual(737, registry.Count);
            Assert.AreEqual(737, registry.LuaFileCount);
            Assert.AreEqual(0, registry.NonLuaFileCount);
            Assert.AreEqual(32, registry.DirectoryCount);
            Assert.AreEqual(1709442L, registry.TotalSizeBytes);
            Assert.AreEqual(3, registry.GetByDirectory("newskill").Count);
        }

        [Test]
        public void Registry_PreservesRepresentativeAdvancedNewskillAndSkilllvlupHashes()
        {
            var registry = PcSkillGocScriptIndexParser.BuildRegistry(IndexDir);

            AssertEntry(registry.GetByRelativePath("advancedskill.lua"), 1, "", "advancedskill.lua", 11540,
                "a3c44169af30fc99d59d8038d80d3e9f631af69c412b2f060be6e07f2c67884c");
            AssertEntry(registry.GetByRelativePath("newskill/manabienkinh.lua"), 172, "newskill", "manabienkinh.lua", 685,
                "df1648ecfed09ce0d6ba0d3ba2bd16537fbbfebd0522f0a628a26f9fa299ab6b");
            AssertEntry(registry.GetByRelativePath("newskill/maubienkinh.lua"), 173, "newskill", "maubienkinh.lua", 686,
                "34df7a2870a028136d10d273f62396959b9f380721c74e0ac8f112b921487c7b");
            AssertEntry(registry.GetByRelativePath("newskill/maumanabienkinh.lua"), 174, "newskill", "maumanabienkinh.lua", 792,
                "3600722d10283be302f1dfd4ba75df6430c18d7d228c5a706e119df608be93b1");
            AssertEntry(registry.GetByRelativePath("skilllvlup.lua"), 451, "", "skilllvlup.lua", 5234,
                "f23dd027ebd013f76ffcc7509f5bad7d42aa4c0fb4aa75eb8ae858f98a4101aa");
            AssertEntry(registry.GetByRelativePath("translife_4/skilllvlup.lua"), 650, "translife_4", "skilllvlup.lua", 1240,
                "231082fddd3333703f0a10a5a65ad71e2d0ea6e5146873915878c16dbc892615");
        }

        [Test]
        public void Service_LoadsDefaultStreamingAssetsIndex()
        {
            var service = SkillGocScriptIndexService.LoadFromStreamingAssets();

            Assert.AreEqual(737, service.Count);
            Assert.AreEqual(737, service.LuaFileCount);
            Assert.AreEqual(32, service.DirectoryCount);
            Assert.IsNotNull(service.GetByRelativePath("newskill/maumanabienkinh.lua"));
            Assert.AreEqual(3, service.GetByDirectory("newskill").Count);
        }

        [Test]
        public void MissingInputs_ReturnEmptyCatalog()
        {
            Assert.AreEqual(0, PcSkillGocScriptIndexParser.ParseFile("/tmp/not-a-real-skill-goc-index.txt").Count);
            Assert.AreEqual(0, PcSkillGocScriptIndexParser.BuildRegistry("/tmp/not-a-real-skill-goc-index-dir").Count);
            Assert.AreEqual(0, SkillGocScriptIndexService.LoadFromFile("/tmp/not-a-real-skill-goc-index.txt").Count);
        }

        private static void AssertEntry(PcSkillGocScriptIndexEntry entry, int index, string directory,
            string fileName, long sizeBytes, string sha256)
        {
            Assert.IsNotNull(entry);
            Assert.AreEqual(SkillGocScriptIndexService.PcSourceRoot, entry.sourceRoot);
            Assert.AreEqual(index, entry.sourceIndex);
            Assert.AreEqual(directory, entry.directory);
            Assert.AreEqual(fileName, entry.fileName);
            Assert.AreEqual("lua", entry.extension);
            Assert.IsTrue(entry.isLua);
            Assert.AreEqual(sizeBytes, entry.sizeBytes);
            Assert.AreEqual(sha256, entry.sha256);
        }
    }
}
