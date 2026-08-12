using System.IO;
using System.Linq;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcWharfParserTests
    {
        private const int ExpectedPcWharfRows = 11;
        private const int ExpectedPcWharfSectSlots = 16;
        private const string PcSourcePath = "/var/www/jx-pc/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/settings/wharf.txt";
        private static string ReferencePath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcMap/wharf.txt");

        [Test]
        public void ParseFile_LoadsExactPcWharfCount()
        {
            var rows = PcWharfParser.ParseFile(ReferencePath);
            Assert.AreEqual(ExpectedPcWharfRows, rows.Count,
                $"Reference wharf.txt must match exact PC source row count from {PcSourcePath}");
        }

        [Test]
        public void ParseFile_AllIdsPositive()
        {
            var rows = PcWharfParser.ParseFile(ReferencePath);
            Assert.IsTrue(rows.All(r => r.wharfId > 0));
        }

        [Test]
        public void ParseFile_PositionInValidRange()
        {
            var rows = PcWharfParser.ParseFile(ReferencePath);
            Assert.IsTrue(rows.All(r => r.mapId > 0), "Wharf mapId must be parsed from SECT1");
            Assert.IsTrue(rows.All(r => r.posX >= 0 && r.posX < 200000), "Wharf posX must be in valid PC range");
            Assert.IsTrue(rows.All(r => r.posY >= 0 && r.posY < 200000), "Wharf posY must be in valid PC range");
        }

        [Test]
        public void ParseFile_SectCountUsesActualNonEmptySectColumns()
        {
            var rows = PcWharfParser.ParseFile(ReferencePath);
            Assert.AreEqual(ExpectedPcWharfSectSlots, rows.Sum(r => r.sectCount),
                "PC wharf.txt contains 16 non-empty SECT slots across 11 rows");

            var bienKinh = rows.Single(r => r.wharfId == 3);
            Assert.AreEqual(2, bienKinh.sectCount,
                "Known PC mismatch: wharf row 3 declares COUNT=1 but has two SECT columns; preserve the actual columns.");
        }
    }
}
