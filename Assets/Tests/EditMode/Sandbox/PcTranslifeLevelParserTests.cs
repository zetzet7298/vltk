using System.IO;
using System.Linq;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcTranslifeLevelParserTests
    {
        private static string PcTaskDir => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcTask");
        private static string TranslifePath => Path.Combine(PcTaskDir, "translife.txt");

        [Test]
        public void ParseFile_LoadsFortyOneLevelRowsFromRealPcTranslifeTable()
        {
            var rows = PcTranslifeLevelParser.ParseFile(TranslifePath);

            Assert.AreEqual(41, rows.Count, "PC translife.txt has one header plus level rows 160..200.");
            Assert.AreEqual(160, rows[0].level);
            Assert.AreEqual(200, rows[rows.Count - 1].level);
            Assert.AreEqual(PcTranslifeLevelParser.BonusGroupCount, rows[0].bonuses.Length);
            Assert.AreEqual(PcTranslifeLevelParser.BonusGroupCount, rows[rows.Count - 1].bonuses.Length);
        }

        [Test]
        public void ParseFile_PreservesFirstLevel160SampleValues()
        {
            var first = PcTranslifeLevelParser.ParseFile(TranslifePath).First(r => r.level == 160);
            var group1 = first.GetBonusGroup(1);
            var group2 = first.GetBonusGroup(2);

            Assert.AreEqual(5, group1.magicPoint);
            Assert.AreEqual(20, group1.prop);
            Assert.AreEqual(1, group1.resist);
            Assert.AreEqual(1, group1.skillLimit);
            Assert.IsFalse(group2.HasAnyValue, "Level 160 only has MAGICPOINT1/PROP1/RESIST1/SKILLLIMIT1 in PC data.");
        }

        [Test]
        public void ParseFile_PreservesLastLevel200AllSevenGroups()
        {
            var last = PcTranslifeLevelParser.ParseFile(TranslifePath).First(r => r.level == 200);

            Assert.AreEqual(20, last.GetBonusGroup(1).magicPoint);
            Assert.AreEqual(100, last.GetBonusGroup(1).prop);
            Assert.AreEqual(5, last.GetBonusGroup(1).resist);
            Assert.AreEqual(2, last.GetBonusGroup(1).skillLimit);
            Assert.AreEqual(30, last.GetBonusGroup(2).magicPoint);
            Assert.AreEqual(150, last.GetBonusGroup(2).prop);
            Assert.AreEqual(20, last.GetBonusGroup(7).magicPoint);
            Assert.AreEqual(100, last.GetBonusGroup(7).prop);
            Assert.AreEqual(0, last.GetBonusGroup(7).resist);
            Assert.AreEqual(1, last.GetBonusGroup(7).skillLimit);
        }

        [Test]
        public void RegistryAndService_LookUpRowsByLevel()
        {
            var registry = PcTranslifeLevelParser.BuildRegistry(PcTaskDir);
            var service = new TranslifeLevelService(registry);

            Assert.AreEqual(41, service.Count);
            Assert.AreEqual(160, service.GetLevel(160).level);
            Assert.AreEqual(200, service.GetLevel(200).level);
            Assert.IsNull(service.GetLevel(159));
            Assert.IsNull(service.GetLevel(201));
        }

        [Test]
        public void Service_LoadsCommittedStreamingAssetsReferencePcTaskTranslifeTxt()
        {
            var service = TranslifeLevelService.LoadFromStreamingAssets();

            Assert.AreEqual(41, service.Count);
            Assert.AreEqual(18, service.GetLevel(196).GetBonusGroup(1).magicPoint);
            Assert.AreEqual(82, service.GetLevel(196).GetBonusGroup(1).prop);
            Assert.AreEqual(29, service.GetLevel(196).GetBonusGroup(2).magicPoint);
            Assert.AreEqual(136, service.GetLevel(196).GetBonusGroup(2).prop);
        }

        [Test]
        public void ParserTargetsLevelTable_NotMissingTranslifeSkillTxtSchema()
        {
            var header = File.ReadLines(TranslifePath).First().Split('\t');
            var rows = PcTranslifeLevelParser.ParseFile(TranslifePath);

            Assert.AreEqual("translife.txt", PcTranslifeLevelParser.SourceFileName);
            Assert.AreEqual(PcTranslifeLevelParser.ExpectedColumnCount, header.Length);
            Assert.AreEqual("LEVEL", header[0]);
            Assert.AreEqual("MAGICPOINT1", header[1]);
            Assert.AreEqual("SKILLLIMIT7", header[28]);
            Assert.IsTrue(rows.All(r => r.level >= 160 && r.level <= 200));
            Assert.IsTrue(rows.All(r => r.level > TranslifeSkillService.MaxTranslifeLevel));
        }
    }
}
