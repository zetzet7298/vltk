// -----------------------------------------------------------------------------
// VLTK Mobile — PcMapListFullParser EditMode tests.
// Kiểm tra parse maplist.ini: type mapping, name/path/maptype/mappos keys,
// comments, BuildRegistry + host dispatch chain.
// PC source: Client 6.0/settings/maplist.ini (1,005 maps, INI format).
// -----------------------------------------------------------------------------

using System.IO;
using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class PcMapListFullParserTests
    {
        // ── Host fake ────────────────────────────────────────────────────────

        private sealed class FakeHost : IPcMapListFullHost
        {
            public int ParseStartCalls;
            public int ParseCompleteCalls;
            public int ParseFailedCalls;
            public int RegistryBuiltCalls;
            public int ShowCalls;
            public int LogCalls;
            public int SfxCalls;
            public int SaveCalls;
            public string LastFilePath;
            public int LastEntryCount;
            public int LastDurationMs;
            public string LastReason;
            public int LastTotalMaps;
            public int LastWithMapType;
            public int LastWithoutMapType;
            public long LastRegistryDurationMs;

            public void OnParseStart(string filePath) { ParseStartCalls++; LastFilePath = filePath; }
            public void OnParseComplete(string filePath, int entryCount, int durationMs)
            {
                ParseCompleteCalls++;
                LastEntryCount = entryCount;
                LastDurationMs = durationMs;
            }
            public void OnParseFailed(string filePath, string reason) { ParseFailedCalls++; LastReason = reason; }
            public void OnRegistryBuilt(int totalMaps, int withMapType, int withoutMapType, long durationMs)
            {
                RegistryBuiltCalls++;
                LastTotalMaps = totalMaps;
                LastWithMapType = withMapType;
                LastWithoutMapType = withoutMapType;
                LastRegistryDurationMs = durationMs;
            }
            public void ShowMapList(int totalMaps, int filtered) { ShowCalls++; }
            public void LogMapListEvent(string message) { LogCalls++; }
            public void PlayMapLoadSFX(string action) { SfxCalls++; }
            public void SaveMapLog(int totalMaps, int withMapType, int withoutMapType) { SaveCalls++; }
        }

        private string _tmpDir;
        private FakeHost _host;

        [SetUp]
        public void SetUp()
        {
            _tmpDir = Path.Combine(Application.temporaryCachePath, "test_maplist_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tmpDir);
            _host = new FakeHost();
            PcMapListFullParser.AttachHost(_host);
        }

        [TearDown]
        public void TearDown()
        {
            PcMapListFullParser.AttachHost(null);
            try { if (Directory.Exists(_tmpDir)) Directory.Delete(_tmpDir, true); } catch { }
        }

        private string WriteMapList(params string[] lines)
        {
            string path = Path.Combine(_tmpDir, "maplist.ini");
            File.WriteAllLines(path, lines);
            return path;
        }

        // ── MapTypeFromString ────────────────────────────────────────────────

        [Test]
        public void MapTypeFromString_City() => Assert.AreEqual(PcMapListFullParser.TypeCity, PcMapListFullParser.MapTypeFromString("City"));
        [Test]
        public void MapTypeFromString_Capital() => Assert.AreEqual(PcMapListFullParser.TypeCapital, PcMapListFullParser.MapTypeFromString("Capital"));
        [Test]
        public void MapTypeFromString_Country() => Assert.AreEqual(PcMapListFullParser.TypeCountry, PcMapListFullParser.MapTypeFromString("Country"));
        [Test]
        public void MapTypeFromString_Field() => Assert.AreEqual(PcMapListFullParser.TypeField, PcMapListFullParser.MapTypeFromString("Field"));
        [Test]
        public void MapTypeFromString_Cave() => Assert.AreEqual(PcMapListFullParser.TypeCave, PcMapListFullParser.MapTypeFromString("Cave"));
        [Test]
        public void MapTypeFromString_Tong() => Assert.AreEqual(PcMapListFullParser.TypeTong, PcMapListFullParser.MapTypeFromString("Tong"));
        [Test]
        public void MapTypeFromString_Battlefield() => Assert.AreEqual(PcMapListFullParser.TypeBattlefield, PcMapListFullParser.MapTypeFromString("Battlefield"));
        [Test]
        public void MapTypeFromString_Instance() => Assert.AreEqual(PcMapListFullParser.TypeInstance, PcMapListFullParser.MapTypeFromString("Instance"));
        [Test]
        public void MapTypeFromString_Other() => Assert.AreEqual(PcMapListFullParser.TypeOther, PcMapListFullParser.MapTypeFromString("Others"));
        [Test]
        public void MapTypeFromString_CaseInsensitive() => Assert.AreEqual(PcMapListFullParser.TypeCity, PcMapListFullParser.MapTypeFromString("CITY"));
        [Test]
        public void MapTypeFromString_Empty_Other() => Assert.AreEqual(PcMapListFullParser.TypeOther, PcMapListFullParser.MapTypeFromString(""));
        [Test]
        public void MapTypeFromString_Unknown_Other() => Assert.AreEqual(PcMapListFullParser.TypeOther, PcMapListFullParser.MapTypeFromString("Atlantis"));
        [Test]
        public void MapTypeFromString_Null_Other() => Assert.AreEqual(PcMapListFullParser.TypeOther, PcMapListFullParser.MapTypeFromString(null));

        // ── ParseFile ────────────────────────────────────────────────────────

        [Test]
        public void ParseFile_EmptyPath_DispatchesFailed()
        {
            var rows = PcMapListFullParser.ParseFile("");
            Assert.AreEqual(0, rows.Count);
            Assert.AreEqual(1, _host.ParseFailedCalls);
            Assert.AreEqual("empty path", _host.LastReason);
        }

        [Test]
        public void ParseFile_NullPath_DispatchesFailed()
        {
            var rows = PcMapListFullParser.ParseFile(null);
            Assert.AreEqual(0, rows.Count);
            Assert.AreEqual(1, _host.ParseFailedCalls);
        }

        [Test]
        public void ParseFile_NoFile_DispatchesFailed()
        {
            var rows = PcMapListFullParser.ParseFile("/tmp/nonexistent_" + System.Guid.NewGuid() + ".ini");
            Assert.AreEqual(0, rows.Count);
            Assert.AreEqual(1, _host.ParseFailedCalls);
            Assert.AreEqual("file not found", _host.LastReason);
        }

        [Test]
        public void ParseFile_Basic()
        {
            var path = WriteMapList(
                "[List]",
                "1=World\\PhuongTuong",
                "1_name=Phượng Tường",
                "1_MapType=Capital",
                "1_MapPos=100,200",
                "2=World\\BaLang",
                "2_name=Ba Lăng",
                "2_MapType=City",
                "2_MapPos=150,250"
            );
            var rows = PcMapListFullParser.ParseFile(path);
            Assert.AreEqual(2, rows.Count);
        }

        [Test]
        public void ParseFile_DispatchesStart()
        {
            var path = WriteMapList("1=x", "1_name=Y", "1_MapType=City");
            PcMapListFullParser.ParseFile(path);
            Assert.AreEqual(1, _host.ParseStartCalls);
            Assert.AreEqual(path, _host.LastFilePath);
        }

        [Test]
        public void ParseFile_DispatchesComplete()
        {
            var path = WriteMapList("1=x", "1_name=Y", "1_MapType=City");
            PcMapListFullParser.ParseFile(path);
            Assert.AreEqual(1, _host.ParseCompleteCalls);
            Assert.AreEqual(1, _host.LastEntryCount);
            Assert.AreEqual(1, _host.SfxCalls);
        }

        [Test]
        public void ParseFile_Comments_Skipped()
        {
            var path = WriteMapList(
                "; comment 1",
                "# comment 2",
                "[List]",
                "; comment 3",
                "1=World\\PhuongTuong",
                "1_name=Phượng Tường"
            );
            var rows = PcMapListFullParser.ParseFile(path);
            Assert.AreEqual(1, rows.Count);
        }

        [Test]
        public void ParseFile_EmptyFile()
        {
            var path = WriteMapList();
            var rows = PcMapListFullParser.ParseFile(path);
            Assert.AreEqual(0, rows.Count);
        }

        [Test]
        public void ParseFile_NoLogIfNoHost()
        {
            PcMapListFullParser.AttachHost(null);
            var path = WriteMapList("1=x", "1_name=Y");
            Assert.DoesNotThrow(() => PcMapListFullParser.ParseFile(path));
        }

        // ── BuildRegistry ───────────────────────────────────────────────────

        [Test]
        public void BuildRegistry_NullDir_DispatchesEmpty()
        {
            var reg = PcMapListFullParser.BuildRegistry(null);
            Assert.AreEqual(0, reg.Count);
            Assert.AreEqual(1, _host.RegistryBuiltCalls);
            Assert.AreEqual(0, _host.LastTotalMaps);
        }

        [Test]
        public void BuildRegistry_EmptyDir()
        {
            var reg = PcMapListFullParser.BuildRegistry(_tmpDir);
            Assert.AreEqual(0, reg.Count);
        }

        [Test]
        public void BuildRegistry_NonexistentDir()
        {
            var reg = PcMapListFullParser.BuildRegistry("/tmp/nonexistent_dir_" + System.Guid.NewGuid());
            Assert.AreEqual(0, reg.Count);
        }

        [Test]
        public void BuildRegistry_WithMaps()
        {
            WriteMapList("1=World\\A", "1_name=A", "1_MapType=City", "2=World\\B", "2_name=B", "2_MapType=Field");
            var reg = PcMapListFullParser.BuildRegistry(_tmpDir);
            Assert.IsTrue(reg.Count >= 2);
        }

        [Test]
        public void BuildRegistry_DispatchesHost()
        {
            WriteMapList("1=World\\A", "1_name=A", "1_MapType=City");
            PcMapListFullParser.BuildRegistry(_tmpDir);
            Assert.AreEqual(1, _host.RegistryBuiltCalls);
            Assert.AreEqual(1, _host.LastTotalMaps);
            Assert.AreEqual(1, _host.LastWithMapType);
            Assert.AreEqual(0, _host.LastWithoutMapType);
            Assert.AreEqual(1, _host.ShowCalls);
            Assert.AreEqual(1, _host.SaveCalls);
        }

        [Test]
        public void BuildRegistry_CountsWithoutMapType()
        {
            // Map without MapType key → TypeOther
            WriteMapList("1=World\\A", "1_name=A", "2=World\\B", "2_name=B", "2_MapType=City");
            PcMapListFullParser.BuildRegistry(_tmpDir);
            Assert.AreEqual(1, _host.LastWithMapType);
            Assert.AreEqual(1, _host.LastWithoutMapType);
        }

        [Test]
        public void BuildRegistry_SkipsSampleFiles()
        {
            WriteMapList("1=World\\A", "1_name=A", "1_MapType=City");
            var samplePath = Path.Combine(_tmpDir, "maplist_sample.ini");
            File.WriteAllLines(samplePath, new[] { "1=World\\X", "1_name=X", "1_MapType=City" });
            var reg = PcMapListFullParser.BuildRegistry(_tmpDir);
            // Should NOT include the X from sample
            Assert.AreEqual(1, reg.Count);
        }

        [Test]
        public void BuildRegistry_MultipleFiles()
        {
            WriteMapList("1=World\\A", "1_name=A", "1_MapType=City");
            var path2 = Path.Combine(_tmpDir, "maplist2.ini");
            File.WriteAllLines(path2, new[] { "2=World\\B", "2_name=B", "2_MapType=Field" });
            var reg = PcMapListFullParser.BuildRegistry(_tmpDir);
            Assert.AreEqual(2, reg.Count);
        }
    }
}
