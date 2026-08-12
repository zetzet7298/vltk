using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class MissionBattleScoringServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_UsesPcImportedMatrixShape()
        {
            var service = MissionBattleScoringService.LoadFromStreamingAssets();

            Assert.That(service.RankCount, Is.EqualTo(5));
            Assert.That(service.ComboCellCount, Is.EqualTo(25));
            Assert.That(service.ScoreCellCount, Is.EqualTo(25));
            Assert.That(service.ComboRowHeader, Is.EqualTo(PcMissionBattleParser.ExpectedRowHeader));
            Assert.That(service.ScoreRowHeader, Is.EqualTo(PcMissionBattleParser.ExpectedRowHeader));
            CollectionAssert.AreEqual(
                new[] { "Soldier", "Captain", "Command", "Lieutenant", "General" },
                service.Ranks);
        }

        [Test]
        public void Lookup_ReturnsPcKillDeathScoringFacts()
        {
            var service = MissionBattleScoringService.LoadFromStreamingAssets();
            var soldierKillsGeneral = service.Lookup("Soldier", "General");
            var generalKillsSoldier = service.Lookup("General", "Soldier");

            Assert.That(soldierKillsGeneral.RanksExist, Is.True);
            Assert.That(soldierKillsGeneral.ComboValue, Is.EqualTo(1));
            Assert.That(soldierKillsGeneral.ScoreValue, Is.EqualTo(150));
            Assert.That(soldierKillsGeneral.IsValidCombo, Is.True);
            Assert.That(soldierKillsGeneral.ComboRowHeader, Is.EqualTo(PcMissionBattleParser.ExpectedRowHeader));
            Assert.That(soldierKillsGeneral.ScoreRowHeader, Is.EqualTo(PcMissionBattleParser.ExpectedRowHeader));
            Assert.That(soldierKillsGeneral.KillerSourceRowName, Is.EqualTo("Soldier"));
            Assert.That(soldierKillsGeneral.DeadSourceColumnName, Is.EqualTo("General"));
            Assert.That(soldierKillsGeneral.PcKillerTitleIndex, Is.EqualTo(1));
            Assert.That(soldierKillsGeneral.PcDeadTitleIndex, Is.EqualTo(5));

            Assert.That(generalKillsSoldier.RanksExist, Is.True);
            Assert.That(generalKillsSoldier.ComboValue, Is.EqualTo(0));
            Assert.That(generalKillsSoldier.ScoreValue, Is.EqualTo(15));
            Assert.That(generalKillsSoldier.IsValidCombo, Is.False);
            Assert.That(generalKillsSoldier.PcKillerTitleIndex, Is.EqualTo(5));
            Assert.That(generalKillsSoldier.PcDeadTitleIndex, Is.EqualTo(1));
        }

        [Test]
        public void LookupByPcTitleIndex_MatchesScoringLuaOneBasedTables()
        {
            var service = MissionBattleScoringService.LoadFromStreamingAssets();
            var captainKillsCommand = service.LookupByPcTitleIndex(2, 3);
            var lieutenantKillsSoldier = service.LookupByPcTitleIndex(4, 1);

            Assert.That(captainKillsCommand.KillerRank, Is.EqualTo("Captain"));
            Assert.That(captainKillsCommand.DeadRank, Is.EqualTo("Command"));
            Assert.That(captainKillsCommand.ComboValue, Is.EqualTo(1));
            Assert.That(captainKillsCommand.ScoreValue, Is.EqualTo(90));

            Assert.That(lieutenantKillsSoldier.KillerRank, Is.EqualTo("Lieutenant"));
            Assert.That(lieutenantKillsSoldier.DeadRank, Is.EqualTo("Soldier"));
            Assert.That(lieutenantKillsSoldier.ComboValue, Is.EqualTo(0));
            Assert.That(lieutenantKillsSoldier.ScoreValue, Is.EqualTo(30));
            Assert.That(lieutenantKillsSoldier.IsValidCombo, Is.False);
        }

        [Test]
        public void InvalidRanks_ReturnSafeZerosAndDoNotFabricateRanks()
        {
            var service = MissionBattleScoringService.LoadFromStreamingAssets();
            var missingKiller = service.Lookup("Hero", "Soldier");
            var missingDead = service.Lookup("Soldier", "Hero");
            var missingTitle = service.LookupByPcTitleIndex(6, 1);

            Assert.That(missingKiller.KillerRankExists, Is.False);
            Assert.That(missingKiller.DeadRankExists, Is.True);
            Assert.That(missingKiller.ComboValue, Is.EqualTo(0));
            Assert.That(missingKiller.ScoreValue, Is.EqualTo(0));
            Assert.That(missingKiller.KillerSourceRowName, Is.Null);

            Assert.That(missingDead.KillerRankExists, Is.True);
            Assert.That(missingDead.DeadRankExists, Is.False);
            Assert.That(missingDead.ComboValue, Is.EqualTo(0));
            Assert.That(missingDead.ScoreValue, Is.EqualTo(0));
            Assert.That(missingDead.DeadSourceColumnName, Is.Null);

            Assert.That(missingTitle.KillerRankExists, Is.False);
            Assert.That(missingTitle.DeadRankExists, Is.True);
            Assert.That(missingTitle.PcKillerTitleIndex, Is.EqualTo(6));
            Assert.That(missingTitle.PcDeadTitleIndex, Is.EqualTo(1));
            Assert.That(missingTitle.KillerRank, Is.Null);
            Assert.That(service.GetRankNameByPcTitleIndex(6), Is.Null);
            Assert.That(service.GetPcTitleIndex("Hero"), Is.EqualTo(0));
            CollectionAssert.DoesNotContain(service.Ranks, "Hero");
        }
    }
}
