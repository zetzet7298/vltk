using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcDropRateEventCatalogTests
    {
        private static string EventDir => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcDropRateEvent");

        [Test]
        public void BuildCatalog_LoadsPcEventManifestAndRows()
        {
            var catalog = PcDropRateEventSourceParser.BuildCatalog(EventDir);

            Assert.AreEqual(17, catalog.FileCount, "PC source has exactly 17 settings/droprate/event INI files in vl_update_27.");
            Assert.AreEqual(1152, catalog.DropRowCount, "Catalog should include one row per numeric [N] section.");
            Assert.AreEqual(3, catalog.DirectoryCount, "Root, jianxiashengri, and jxanniversary3 directories are expected.");
            Assert.AreEqual(70391L, catalog.TotalSizeBytes);
        }

        [Test]
        public void Manifest_RecordsShaSizeAndSectionCountsForRepresentativeFile()
        {
            var catalog = PcDropRateEventSourceParser.BuildCatalog(EventDir);
            var golden = catalog.GetFile("jxanniversary3/golden_jxanni3.ini");

            Assert.IsNotNull(golden);
            Assert.AreEqual(3613L, golden.sizeBytes);
            Assert.AreEqual("c9a23761900b01068d0dcc02b27b0fe7169c69568505be7f33133977b292467a", golden.sha256);
            Assert.AreEqual(60, golden.sectionCount);
            Assert.AreEqual(59, golden.dropRowCount);
            Assert.AreEqual(59, golden.mainCount);
            Assert.AreEqual(3000000, golden.randRange);
            Assert.AreEqual(50, golden.moneyRate);
        }

        [Test]
        public void Catalog_IndexesRepresentativeNumericSections()
        {
            var catalog = PcDropRateEventSourceParser.BuildCatalog(EventDir);
            var diancangRows = catalog.GetRows("diancangshan.ini");
            var goldenRows = catalog.GetRows("jxanniversary3/golden_jxanni3.ini");

            Assert.AreEqual(64, diancangRows.Count);
            Assert.AreEqual(59, goldenRows.Count);
            Assert.AreEqual(1, diancangRows[0].sectionIndex);
            Assert.AreEqual(0, diancangRows[0].genre);
            Assert.AreEqual(0, diancangRows[0].detail);
            Assert.AreEqual(0, diancangRows[0].particular);
            Assert.AreEqual(300, diancangRows[0].randRate);
            Assert.AreEqual(35000, goldenRows[0].randRate);
        }

        [Test]
        public void RepresentativeIniFiles_ParseWithExistingPcDropRateParser()
        {
            string sample = Path.Combine(EventDir, "samples/jianxiashengri/bianjing.ini");
            var table = PcDropRateParser.ParseFile(sample, "event_jianxiashengri_bianjing");

            Assert.IsNotNull(table);
            Assert.AreEqual("event_jianxiashengri_bianjing", table.tableName);
            Assert.AreEqual(60, table.count);
            Assert.AreEqual(330000, table.randRange);
            Assert.AreEqual(60, table.entries.Count);
            Assert.AreEqual(300, table.entries[0].randRate);
        }
    }
}
