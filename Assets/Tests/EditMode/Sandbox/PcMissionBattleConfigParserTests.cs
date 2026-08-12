using System.IO;
using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcMissionBattleConfigParserTests
    {
        private static string MissionBattleDir => Path.Combine(
            Application.streamingAssetsPath,
            MissionBattleConfigService.DefaultStreamingDir);

        [Test]
        public void PcFilesExist_InCommittedBattlefieldReferencePath()
        {
            Assert.That(File.Exists(Path.Combine(MissionBattleDir, PcMissionBattleParser.ComboFileName)), Is.True);
            Assert.That(File.Exists(Path.Combine(MissionBattleDir, PcMissionBattleParser.ScoresFileName)), Is.True);
        }

        [Test]
        public void ParserReadsComboAndScores_MatrixCountsAndHeaders()
        {
            var registry = PcMissionBattleParser.BuildRegistry(MissionBattleDir);

            Assert.That(registry.Count, Is.EqualTo(5));
            Assert.That(registry.ComboCellCount, Is.EqualTo(25));
            Assert.That(registry.ScoreCellCount, Is.EqualTo(25));
            Assert.That(registry.ComboRowHeader, Is.EqualTo(PcMissionBattleParser.ExpectedRowHeader));
            Assert.That(registry.ScoreRowHeader, Is.EqualTo(PcMissionBattleParser.ExpectedRowHeader));
            CollectionAssert.AreEqual(
                new[] { "Soldier", "Captain", "Command", "Lieutenant", "General" },
                registry.ComboHeaders);
            CollectionAssert.AreEqual(registry.ComboHeaders, registry.ScoreHeaders);
        }

        [Test]
        public void ParserReadsPcSampleMatrixValues()
        {
            var registry = PcMissionBattleParser.BuildRegistry(MissionBattleDir);
            var soldier = registry.Get("Soldier");
            var general = registry.Get("General");

            Assert.That(soldier, Is.Not.Null);
            Assert.That(general, Is.Not.Null);
            Assert.That(soldier.ComboValues["General"], Is.EqualTo(1));
            Assert.That(general.ComboValues["Soldier"], Is.EqualTo(0));
            Assert.That(soldier.ScoreValues["General"], Is.EqualTo(150));
            Assert.That(general.ScoreValues["Soldier"], Is.EqualTo(15));
        }

        [Test]
        public void ServiceLoadsFromDefaultStreamingAssetsPath()
        {
            var service = MissionBattleConfigService.LoadFromStreamingAssets();

            Assert.That(service.Count, Is.EqualTo(5));
            Assert.That(service.ComboCellCount, Is.EqualTo(25));
            Assert.That(service.ScoreCellCount, Is.EqualTo(25));
            Assert.That(service.GetCombo("Lieutenant", "Soldier"), Is.EqualTo(0));
            Assert.That(service.GetScore("Captain", "Command"), Is.EqualTo(90));
        }
    }
}
