using System.IO;
using System.Linq;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcScrollParserTests
    {
        private const int ExpectedPcScrollRows = 2600;
        private const string PcSourcePath = "/var/www/vltksource_new/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/settings/scroll.txt";
        private static string ReferencePath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcMap/scroll.txt");

        [Test]
        public void ParseFile_LoadsExactPcScrollCount()
        {
            var rows = PcScrollParser.ParseFile(ReferencePath);
            Assert.AreEqual(ExpectedPcScrollRows, rows.Count,
                $"Reference scroll.txt must match exact PC source row count from {PcSourcePath}");
        }

        [Test]
        public void ParseFile_IdsAreUnique()
        {
            var rows = PcScrollParser.ParseFile(ReferencePath);
            var ids = rows.Select(r => r.scrollId).ToList();
            Assert.AreEqual(ids.Count, ids.Distinct().Count(), "Scroll ids must be unique");
        }

        [Test]
        public void ParseFile_AllIdsPositive()
        {
            var rows = PcScrollParser.ParseFile(ReferencePath);
            Assert.IsTrue(rows.All(r => r.scrollId > 0));
        }

        [Test]
        public void ParseFile_ParsesTwoColumnValueTableWithoutSkippedRows()
        {
            var rows = PcScrollParser.ParseFile(ReferencePath);
            Assert.AreEqual(1, rows.First().scrollId);
            Assert.AreEqual(2600, rows.Last().scrollId);
            Assert.IsTrue(rows.All(r => r.cost >= 0), "PC scroll.txt value column must parse as non-negative runtime data");
        }
    }
}
