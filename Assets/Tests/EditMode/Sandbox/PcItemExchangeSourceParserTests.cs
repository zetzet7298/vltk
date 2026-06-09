using System.IO;
using System.Linq;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcItemExchangeSourceParserTests
    {
        private static string SourceDir => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcItemExchange");

        [Test]
        public void ImportedDirectory_ContainsOnlyPhaseOneTopLevelPcFiles()
        {
            foreach (var file in PcItemExchangeSourceParser.ExpectedTopLevelFiles)
                Assert.IsTrue(File.Exists(Path.Combine(SourceDir, file)), file);

            Assert.IsFalse(Directory.Exists(Path.Combine(SourceDir, "rolevalue_log")),
                "Runtime rolevalue_log must not be committed for phase 1 source catalog.");
            Assert.IsEmpty(Directory.GetFileSystemEntries(SourceDir, "*rolevalue_log*", SearchOption.AllDirectories));
        }

        [Test]
        public void ParseDirectory_ReportsPcFileRowCountsAndHeaders()
        {
            var catalog = PcItemExchangeSourceParser.ParseDirectory(SourceDir);

            Assert.IsTrue(catalog.normal.exists);
            Assert.AreEqual(7334, catalog.normal.dataRowCount);
            Assert.AreEqual(78, catalog.normal.headerColumns.Count);
            Assert.IsTrue(catalog.normal.headerLine.Contains("ID"));

            Assert.IsTrue(catalog.rare.exists);
            Assert.AreEqual(480, catalog.rare.dataRowCount);
            CollectionAssert.AreEqual(new[] { "NAME", "MAGIC_ID", "MAG_P1_MIN", "MAG_P1_MAX" },
                catalog.rare.headerColumns.Take(4).ToArray());
        }

        [Test]
        public void ParseDirectory_ReportsLevelExpFactsWithoutRuntimeExecution()
        {
            var catalog = PcItemExchangeSourceParser.ParseDirectory(SourceDir);

            Assert.IsTrue(catalog.levelExp.exists);
            Assert.AreEqual(200, catalog.levelExp.dataRowCount);
            Assert.AreEqual(10, catalog.levelExp.headerColumns.Count);
            Assert.AreEqual("等级", catalog.levelExp.headerColumns[0]);

            Assert.IsTrue(catalog.levelLeadExp.exists);
            Assert.AreEqual(100, catalog.levelLeadExp.dataRowCount);
            Assert.AreEqual(5, catalog.levelLeadExp.headerColumns.Count);
            Assert.AreEqual("等级", catalog.levelLeadExp.headerColumns[0]);
        }

        [Test]
        public void ParseDirectory_ReportsRoleValueIniSectionsAndKeys()
        {
            var catalog = PcItemExchangeSourceParser.ParseDirectory(SourceDir);

            Assert.IsTrue(catalog.roleValue.exists);
            CollectionAssert.AreEqual(new[] { "Value", "Jxb", "Limit", "Evaluate" }, catalog.roleValue.sections);
            Assert.AreEqual(35, catalog.roleValue.keys.Count);
            Assert.IsTrue(catalog.roleValue.keys.Any(k => k.FullKey == "Value.skill" && k.value == "5000"));
            Assert.IsTrue(catalog.roleValue.keys.Any(k => k.FullKey == "Evaluate.item" && k.value == "1"));
            Assert.IsFalse(catalog.hasRoleValueLog);
        }
    }
}
