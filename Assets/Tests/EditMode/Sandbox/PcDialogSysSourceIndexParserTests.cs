using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcDialogSysSourceIndexParserTests
    {
        private static string IndexPath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcDialogSys/dialogsys_source_index.txt");

        private static string IndexDir => Path.GetDirectoryName(IndexPath);

        [Test]
        public void ParseFile_ReadsPcDailogSysCatalog_NotLuaRuntime()
        {
            Assert.IsTrue(File.Exists(IndexPath));
            var rows = PcDialogSysSourceIndexParser.ParseFile(IndexPath);

            Assert.AreEqual(5, rows.Count, "PC script/dailogsys has exactly five Lua source files in 00.src-tinh-kiem/home_jxser.");
            Assert.AreEqual(5, rows.FindAll(r => r.extension == "lua").Count);
            Assert.AreEqual(30, Sum(rows, r => r.functionCount));
            Assert.AreEqual(27, Sum(rows, r => r.globalSymbolCount));
        }

        [Test]
        public void Registry_PreservesRepresentativeFileHashesAndFunctions()
        {
            var registry = PcDialogSysSourceIndexParser.BuildRegistry(IndexDir);
            var gDialog = registry.GetByRelativePath("g_dialog.lua");
            var dailogSay = registry.GetByRelativePath("dailogsay.lua");

            Assert.IsNotNull(gDialog);
            Assert.AreEqual(DialogSysIndexService.PcSourceRoot, gDialog.sourceRoot);
            Assert.AreEqual(5, gDialog.sourceIndex);
            Assert.AreEqual(951, gDialog.sizeBytes);
            Assert.AreEqual("cf5d3be79db728b6e71dfa5b8b45036a5b8d59f033a1b0da5b2148e57f674cb2", gDialog.sha256);
            CollectionAssert.Contains(gDialog.functions, "G_DIALOG:ShowDailog");
            CollectionAssert.Contains(gDialog.globalSymbols, "G_DIALOG");

            Assert.IsNotNull(dailogSay);
            Assert.AreEqual(3776, dailogSay.sizeBytes);
            Assert.AreEqual("2121a59ea63719fa06d6f3bd2d8075b1b99f8855f148458c4a2a3e63f77bd8cd", dailogSay.sha256);
            CollectionAssert.Contains(dailogSay.functions, "CreateNewSayEx");
            CollectionAssert.Contains(dailogSay.globalSymbols, "G_PlayerDailogData");
        }

        [Test]
        public void Registry_IndexesRepresentativeOptionAndSaySurfaces()
        {
            var registry = PcDialogSysSourceIndexParser.BuildRegistry(IndexDir);

            Assert.AreEqual(1, registry.GetByFunction("DailogOptionClass:OnSelect").Count);
            Assert.AreEqual("dialogoption.lua", registry.GetByFunction("DailogOptionClass:OnSelect")[0].relativePath);
            Assert.AreEqual(2, registry.GetBySurface("%s/#%s:%s([[%s]],%d,%d,%d)").Count);
            Assert.AreEqual(1, registry.GetBySurface("X¸c nhËn").Count);
            Assert.AreEqual("dialogoption.lua", registry.GetBySurface("Say")[0].relativePath);
            Assert.AreEqual("g_dialog.lua", registry.GetBySurface("CreateTaskSay")[0].relativePath);
        }

        [Test]
        public void Service_LoadsDefaultStreamingAssetsIndex()
        {
            var service = DialogSysIndexService.LoadFromStreamingAssets();

            Assert.AreEqual(5, service.Count);
            Assert.AreEqual(5, service.LuaFileCount);
            Assert.AreEqual(30, service.TotalFunctionCount);
            Assert.AreEqual(9, service.TotalOptionSurfaceCount);
            Assert.AreEqual(5, service.TotalSaySurfaceCount);
            Assert.AreEqual(11823, service.TotalSizeBytes);
            Assert.IsNotNull(service.GetByRelativePath("composeoption.lua"));
        }

        [Test]
        public void MissingInputs_ReturnEmptyCatalog()
        {
            Assert.AreEqual(0, PcDialogSysSourceIndexParser.ParseFile("/tmp/not-a-real-dialogsys-index.txt").Count);
            Assert.AreEqual(0, PcDialogSysSourceIndexParser.BuildRegistry("/tmp/not-a-real-dialogsys-index-dir").Count);
            Assert.AreEqual(0, DialogSysIndexService.LoadFromFile("/tmp/not-a-real-dialogsys-index.txt").Count);
        }

        private static int Sum(System.Collections.Generic.List<PcDialogSysSourceIndexEntry> rows, System.Func<PcDialogSysSourceIndexEntry, int> selector)
        {
            var total = 0;
            foreach (var row in rows) total += selector(row);
            return total;
        }
    }
}
