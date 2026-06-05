using System.IO;
using System.Linq;
using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcFullNpcRegistryTests
    {
        private static string SamplePath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcNpc/npcs_sample.txt");

        [Test]
        public void BuildRegistry_AddsAllTenTemplates()
        {
            if (!File.Exists(SamplePath))
            {
                Assert.Inconclusive($"Sample file not found at {SamplePath}");
                return;
            }

            var reg = new NpcTemplateRegistry();
            int added = PcFullNpcParser.BuildRegistry(reg, SamplePath);

            Assert.AreEqual(10, added, "BuildRegistry should add 10 templates from the sample");
            Assert.AreEqual(10, reg.Count);
        }

        [Test]
        public void BuildRegistry_ResolvesKnownTemplate()
        {
            if (!File.Exists(SamplePath))
            {
                Assert.Inconclusive($"Sample file not found at {SamplePath}");
                return;
            }

            var reg = new NpcTemplateRegistry();
            PcFullNpcParser.BuildRegistry(reg, SamplePath);

            for (int i = 0; i < 10; i++)
            {
                var t = reg.Resolve(i);
                Assert.IsNotNull(t, $"Template {i} should be resolvable");
                Assert.AreEqual(i, t.templateId);
                Assert.IsFalse(string.IsNullOrEmpty(t.nameRaw), $"Template {i} nameRaw empty");
            }
        }

        [Test]
        public void BuildRegistry_ResolvesUnknownReturnsNull()
        {
            if (!File.Exists(SamplePath))
            {
                Assert.Inconclusive($"Sample file not found at {SamplePath}");
                return;
            }

            var reg = new NpcTemplateRegistry();
            PcFullNpcParser.BuildRegistry(reg, SamplePath);

            Assert.IsNull(reg.Resolve(999));
            Assert.IsFalse(reg.Contains(999));
        }

        [Test]
        public void BuildRegistry_PopulatesAiParams()
        {
            if (!File.Exists(SamplePath))
            {
                Assert.Inconclusive($"Sample file not found at {SamplePath}");
                return;
            }

            var reg = new NpcTemplateRegistry();
            PcFullNpcParser.BuildRegistry(reg, SamplePath);

            for (int i = 0; i < 10; i++)
            {
                var t = reg.Resolve(i);
                Assert.IsNotNull(t);
                Assert.IsNotNull(t.aiParams, $"Template {i} aiParams null");
                Assert.AreEqual(9, t.aiParams.Length, $"Template {i} aiParams must be length 9");
            }
        }
    }
}
