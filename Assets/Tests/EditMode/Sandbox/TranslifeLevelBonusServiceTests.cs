using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class TranslifeLevelBonusServiceTests
    {
        private static string PcTaskDir => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcTask");
        private static string TranslifePath => Path.Combine(PcTaskDir, "translife.txt");

        private static TranslifeLevelBonusService CreateService()
            => TranslifeLevelBonusService.FromDirectory(PcTaskDir);

        [Test]
        public void SourceCounts_ExposeRealPcTranslifeLevelTableShape()
        {
            var service = CreateService();
            var header = File.ReadLines(TranslifePath).First().Split('\t');

            Assert.AreEqual("translife.txt", TranslifeLevelBonusService.SourceFileName);
            Assert.AreEqual("Client 6.0/settings/task/metempsychosis/translife.txt", TranslifeLevelBonusService.PcSourceRelativePath);
            Assert.AreEqual(41, service.SourceRowCount);
            Assert.AreEqual(29, service.SourceHeaderColumnCount);
            Assert.AreEqual(7, service.SourceBonusGroupCount);
            Assert.AreEqual(TranslifeLevelBonusService.HeaderColumnCount, header.Length);
            Assert.AreEqual("LEVEL", header[0]);
            Assert.AreEqual("SKILLLIMIT7", header[28]);
        }

        [Test]
        public void GetBonusGroups_ReturnsSevenGroupsForBoundaryLevels160And200()
        {
            var service = CreateService();
            var level160 = service.GetBonusGroups(160);
            var level200 = service.GetBonusGroups(200);

            Assert.AreEqual(7, level160.Length);
            Assert.AreEqual(7, level200.Length);
            Assert.AreEqual(5, level160[0].magicPoint);
            Assert.AreEqual(20, level160[0].prop);
            Assert.AreEqual(1, level160[0].resist);
            Assert.AreEqual(1, level160[0].skillLimit);
            Assert.IsFalse(level160[1].HasAnyValue, "PC translife.txt level 160 only fills group 1.");
            Assert.AreEqual(20, level200[6].magicPoint);
            Assert.AreEqual(100, level200[6].prop);
            Assert.AreEqual(0, level200[6].resist);
            Assert.AreEqual(1, level200[6].skillLimit);
        }

        [Test]
        public void GetBonusGroup_PreservesRepresentativeExactPcValues()
        {
            var service = CreateService();
            var level180Group2 = service.GetBonusGroup(180, 2);
            var level180Group3 = service.GetBonusGroup(180, 3);
            var level196Group2 = service.GetBonusGroup(196, 2);

            Assert.AreEqual(23, level180Group2.magicPoint);
            Assert.AreEqual(79, level180Group2.prop);
            Assert.AreEqual(4, level180Group2.resist);
            Assert.AreEqual(1, level180Group2.skillLimit);
            Assert.AreEqual(10, level180Group3.magicPoint);
            Assert.AreEqual(40, level180Group3.prop);
            Assert.AreEqual(4, level180Group3.resist);
            Assert.AreEqual(1, level180Group3.skillLimit);
            Assert.AreEqual(29, level196Group2.magicPoint);
            Assert.AreEqual(136, level196Group2.prop);
            Assert.AreEqual(5, level196Group2.resist);
            Assert.AreEqual(1, level196Group2.skillLimit);
        }

        [Test]
        public void ValidateLevel_RejectsLevelsOutsidePcRange160To200()
        {
            var service = CreateService();

            Assert.IsTrue(TranslifeLevelBonusService.IsSupportedLevel(160));
            Assert.IsTrue(TranslifeLevelBonusService.IsSupportedLevel(200));
            Assert.IsFalse(TranslifeLevelBonusService.IsSupportedLevel(159));
            Assert.IsFalse(TranslifeLevelBonusService.IsSupportedLevel(201));
            Assert.Throws<ArgumentOutOfRangeException>(() => TranslifeLevelBonusService.ValidateLevel(159));
            Assert.Throws<ArgumentOutOfRangeException>(() => service.GetBonusGroups(201));
            Assert.Throws<ArgumentOutOfRangeException>(() => service.GetBonusGroup(160, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => service.GetBonusGroup(160, 8));
        }

        [Test]
        public void GetDeltaByGroup_ComputesTargetMinusSourceForEachBonusGroup()
        {
            var service = CreateService();
            var deltas = service.GetDeltaByGroup(160, 200);

            Assert.AreEqual(7, deltas.Length);
            Assert.AreEqual(15, deltas[0].magicPoint);
            Assert.AreEqual(80, deltas[0].prop);
            Assert.AreEqual(4, deltas[0].resist);
            Assert.AreEqual(1, deltas[0].skillLimit);
            Assert.AreEqual(30, deltas[1].magicPoint);
            Assert.AreEqual(150, deltas[1].prop);
            Assert.AreEqual(5, deltas[1].resist);
            Assert.AreEqual(1, deltas[1].skillLimit);
            Assert.AreEqual(20, deltas[6].magicPoint);
            Assert.AreEqual(100, deltas[6].prop);
            Assert.AreEqual(0, deltas[6].resist);
            Assert.AreEqual(1, deltas[6].skillLimit);
        }

        [Test]
        public void GetDeltaForGroup_SupportsIntermediateAndReverseDiffs()
        {
            var service = CreateService();
            var group2 = service.GetDeltaForGroup(170, 180, 2);
            var reverseGroup1 = service.GetDeltaForGroup(200, 160, 1);

            Assert.AreEqual(8, group2.magicPoint);
            Assert.AreEqual(49, group2.prop);
            Assert.AreEqual(1, group2.resist);
            Assert.AreEqual(0, group2.skillLimit);
            Assert.IsTrue(group2.HasAnyDelta);
            Assert.AreEqual(-15, reverseGroup1.magicPoint);
            Assert.AreEqual(-80, reverseGroup1.prop);
            Assert.AreEqual(-4, reverseGroup1.resist);
            Assert.AreEqual(-1, reverseGroup1.skillLimit);
        }
    }
}
