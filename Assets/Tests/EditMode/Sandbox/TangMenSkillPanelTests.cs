using System.Linq;
using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;
using VLTK.UI;

namespace VLTK.Tests.Sandbox
{
    public class TangMenSkillPanelTests
    {
        [Test]
        public void GrantTangMenSkillPanelProgression_SetsLevel200Points200AndKnownTangMenSkillsAtZero()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
            var progression = new PlayerProgressionState();
            progression.GrantTangMenSkillPanelProgression(catalog);

            Assert.AreEqual(200, progression.level);
            Assert.AreEqual(200, progression.fightSkillPoints);
            Assert.AreEqual(CombatFaction.TangMen, progression.faction);
            
            // Check that all TangMen skills are known and start at level 0
            var expectedIds = new[] { 43, 45, 47, 48, 50, 51, 54, 55, 57, 58 };
            foreach (int id in expectedIds)
            {
                Assert.IsTrue(progression.knownSkills.Contains(id), $"missing known skill {id}");
                Assert.AreEqual(0, progression.skillLevels[id], $"PC join seed should keep skill {id} unspent at level 0");
            }
        }

        [Test]
        public void SkillPanelSnapshot_ListsAllTangMenSkillsInPcSlotOrder()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
            var progression = new PlayerProgressionState();
            progression.GrantTangMenSkillPanelProgression(catalog);

            var snap = PcSkillPanelService.Build(catalog, progression);
            Assert.AreEqual(200, snap.playerLevel);
            Assert.AreEqual(200, snap.skillPoints);
            Assert.AreEqual(CombatFaction.TangMen, snap.faction);
            Assert.AreEqual(10, snap.rows.Count);
            Assert.AreEqual(43, snap.rows[0].skillId);
            
            var expectedIds = new[] { 43, 45, 47, 48, 50, 51, 54, 55, 57, 58 };
            CollectionAssert.AreEqual(expectedIds, snap.rows.Select(r => r.skillId).ToArray());
            
            Assert.AreEqual(30, PcSkillPanelService.PcFightSkillSlotsPerPage, "Mobile uses 30-slot grid for scrollable 10-skill list.");
            Assert.AreEqual(30, snap.rows.Single(r => r.skillId == 48).requiredLevel, "PC Skills.txt ReqLevel for Tâm Nhãn is 30.");
            Assert.AreEqual(0, snap.rows[0].learnedLevel);
            Assert.IsTrue(snap.rows[0].canUpgrade);
            StringAssert.Contains("Ám Khí", snap.rows[0].displayName);
        }
    }
}
