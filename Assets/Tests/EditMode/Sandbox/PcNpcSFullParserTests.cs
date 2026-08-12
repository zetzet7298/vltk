using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcNpcSFullParserTests
    {
        private static string NpcDir => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcNpc");

        private static string NpcsFile => Path.Combine(NpcDir, "npcs.txt");

        [Test]
        public void ParseFile_ReadsFullNpcCatalog()
        {
            var templates = PcNpcSFullParser.ParseFile(NpcsFile);
            Assert.IsNotNull(templates);
            Assert.GreaterOrEqual(templates.Count, 100, "Should parse at least 100 NPC templates from full npcs.txt");
        }

        [Test]
        public void ParseFile_TemplatesHaveNamesAndResTypes()
        {
            var templates = PcNpcSFullParser.ParseFile(NpcsFile);
            int withName = 0;
            int withResType = 0;
            foreach (var t in templates)
            {
                // templateId is a synthetic 0-based row index (npcs.txt has no numeric
                // ID column; col0 is the Vietnamese Name). 0 is a valid index, so the
                // validity check is >= 0, not > 0.
                Assert.GreaterOrEqual(t.templateId, 0, "Template ID (row index) should be non-negative");
                if (!string.IsNullOrEmpty(t.nameRaw)) withName++;
                if (!string.IsNullOrEmpty(t.spriteClipRef)) withResType++;
            }
            Assert.Greater(withName, 50, "Most NPCs should have names");
            Assert.Greater(withResType, 50, "Most NPCs should have res types");
        }

        [Test]
        public void ParseFile_TemplatesHaveStats()
        {
            var templates = PcNpcSFullParser.ParseFile(NpcsFile);
            int withAiMode = 0;
            int withWalkSpeed = 0;
            foreach (var t in templates)
            {
                if (t.aiMode > 0) withAiMode++;
                if (t.walkSpeed > 0) withWalkSpeed++;
            }
            Assert.Greater(withAiMode, 10, "Some NPCs should have AI mode");
            Assert.Greater(withWalkSpeed, 10, "Some NPCs should have walk speed");
        }

        [Test]
        public void ImportIntoRegistry_RegistersAllTemplates()
        {
            var registry = new NpcTemplateRegistry();
            int count = PcNpcSFullParser.ImportIntoRegistry(NpcsFile, registry);
            Assert.Greater(count, 100, "Should register at least 100 templates");
            Assert.AreEqual(count, registry.Count);
        }
    }

    public class PcNpcBatchLoaderTests
    {
        private static string NpcDir => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcNpc");

        [Test]
        public void LoadAll_LoadsAllNpcFiles()
        {
            var registry = new NpcTemplateRegistry();
            var result = PcNpcBatchLoader.LoadAll(NpcDir, registry);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.perFileCounts.ContainsKey("npcs"));
            Assert.GreaterOrEqual(result.perFileCounts["npcs"], 100);
            Assert.GreaterOrEqual(result.totalTemplates, 100);
        }
    }
}
