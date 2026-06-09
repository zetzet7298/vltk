using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcHuoYueDuIndexParserTests
    {
        private static string BaseDir => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcHuoYueDu");

        private static string SourceIndexPath => Path.Combine(BaseDir, "huoyuedu_source_index.txt");
        private static string ConfigIndexPath => Path.Combine(BaseDir, "huoyuedu_config_index.txt");
        private static string ActivityConfigPath => Path.Combine(BaseDir, "huoyuedu.txt");

        [Test]
        public void SourceIndex_PreservesPcHuoYueDuScriptFileCountsAndHashes()
        {
            Assert.IsTrue(File.Exists(SourceIndexPath));
            var rows = PcHuoYueDuParser.ParseSourceIndexFile(SourceIndexPath);

            Assert.AreEqual(3, rows.Count, "PC server script/huoyuedu has exactly 3 Lua files in scoped source.");
            Assert.AreEqual(3, rows.FindAll(r => r.isLua).Count);

            var main = rows.Find(r => r.relativePath == "huoyuedu.lua");
            Assert.IsNotNull(main);
            Assert.AreEqual("Server 6.0/server/home_jxser/server1/script/huoyuedu", main.sourceRoot);
            Assert.AreEqual(".", main.directory);
            Assert.AreEqual(4110, main.sizeBytes);
            Assert.AreEqual(141, main.lineCount);
            Assert.AreEqual("bbd9e699e5a0e6219b6f8782836dc0d64f2f7207805abfae53935ab94eae29c8", main.sha256);
        }

        [Test]
        public void ConfigIndex_PreservesPcActivityPointTableMetadata()
        {
            Assert.IsTrue(File.Exists(ConfigIndexPath));
            var rows = PcHuoYueDuParser.ParseConfigIndexFile(ConfigIndexPath);

            Assert.AreEqual(1, rows.Count, "PC server settings/huoyuedu has one huoyuedu.txt config table.");
            var config = rows[0];
            Assert.IsTrue(config.isConfig);
            Assert.AreEqual("Server 6.0/server/home_jxser/server1/settings/huoyuedu", config.sourceRoot);
            Assert.AreEqual("huoyuedu.txt", config.relativePath);
            Assert.AreEqual(2215, config.sizeBytes);
            Assert.AreEqual(42, config.lineCount);
            Assert.AreEqual(41, config.dataRows);
            Assert.AreEqual("f553ca042e06ca52b4f968190c39b1369f6a10c64a3dbb2519832aab6f99c244", config.sha256);
            Assert.IsTrue(config.headerColumns.StartsWith("ActivityId|ActivityName|CountTask|MaxCount"));
        }

        [Test]
        public void ActivityConfigParser_ReadsRowsWithoutRuntimeSemantics()
        {
            Assert.IsTrue(File.Exists(ActivityConfigPath));
            var rows = PcHuoYueDuParser.ParseActivityConfigFile(ActivityConfigPath);

            Assert.AreEqual(41, rows.Count);
            Assert.AreEqual(1, rows[0].activityId);
            Assert.AreEqual("BOSS", rows[0].activityName);
            Assert.AreEqual(2862, rows[0].countTask);
            Assert.AreEqual(1, rows[0].maxCount);
            Assert.AreEqual(5, rows[0].parameters[0]);
            Assert.AreEqual(0, rows[0].weekResetFlag);
            Assert.AreEqual(41, rows[40].activityId);
            Assert.AreEqual(4171, rows[40].countTask);
        }

        [Test]
        public void Service_LoadsCommittedReferenceIndexOnly()
        {
            var service = HuoYueDuIndexService.LoadFromDirectory(BaseDir);

            Assert.AreEqual(4, service.FileCount);
            Assert.AreEqual(3, service.SourceFileCount);
            Assert.AreEqual(1, service.ConfigFileCount);
            Assert.AreEqual(3, service.LuaFileCount);
            Assert.AreEqual(41, service.ActivityRowCount);
            Assert.AreEqual(9691, service.TotalSizeBytes);
            Assert.AreEqual("award.lua", service.GetSourceFile("award.lua").fileName);
            Assert.AreEqual(57, service.GetSourceFile("award.lua").lineCount);
            Assert.AreEqual(41, service.GetActivity(41).activityId);
        }

        [Test]
        public void MissingInputs_ReturnEmptyIndexAndRows()
        {
            Assert.AreEqual(0, PcHuoYueDuParser.ParseSourceIndexFile("/tmp/not-real-huoyuedu-source.txt").Count);
            Assert.AreEqual(0, PcHuoYueDuParser.ParseConfigIndexFile("/tmp/not-real-huoyuedu-config.txt").Count);
            Assert.AreEqual(0, PcHuoYueDuParser.ParseActivityConfigFile("/tmp/not-real-huoyuedu.txt").Count);
            Assert.AreEqual(0, HuoYueDuIndexService.LoadFromDirectory("/tmp/not-real-huoyuedu-dir").FileCount);
        }
    }
}
