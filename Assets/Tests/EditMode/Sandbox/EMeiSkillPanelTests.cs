using System.Linq;
using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;
using VLTK.UI;

namespace VLTK.Tests.Sandbox
{
    public class EMeiSkillPanelTests
    {
        [Test]
        public void GrantEMeiSkillPanelProgression_SetsLevel200Points200AndKnownEMeiSkillsAtZero()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
            var progression = new PlayerProgressionState();
            progression.GrantEMeiSkillPanelProgression(catalog);

            Assert.AreEqual(200, progression.level);
            Assert.AreEqual(200, progression.fightSkillPoints);
            Assert.AreEqual(CombatFaction.EMei, progression.faction);
            
            // Check that all 16 EMei skills are known and start at level 0
            var expectedIds = new[] { 77, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93 };
            foreach (int id in expectedIds)
            {
                Assert.IsTrue(progression.knownSkills.Contains(id), $"missing known skill {id}");
                Assert.AreEqual(0, progression.skillLevels[id], $"PC join seed should keep skill {id} unspent at level 0");
            }
        }

        [Test]
        public void SkillPanelSnapshot_ListsAllEMeiSkillsInPcSlotOrder()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
            var progression = new PlayerProgressionState();
            progression.GrantEMeiSkillPanelProgression(catalog);

            var snap = PcSkillPanelService.Build(catalog, progression);
            Assert.AreEqual(200, snap.playerLevel);
            Assert.AreEqual(200, snap.skillPoints);
            Assert.AreEqual(CombatFaction.EMei, snap.faction);
            Assert.AreEqual(16, snap.rows.Count);
            Assert.AreEqual(77, snap.rows[0].skillId);
            
            var expectedIds = new[] { 77, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93 };
            CollectionAssert.AreEqual(expectedIds, snap.rows.Select(r => r.skillId).ToArray());
            
            Assert.AreEqual(30, PcSkillPanelService.PcFightSkillSlotsPerPage, "Mobile uses 30-slot grid for scrollable 16-skill list.");
            Assert.AreEqual(10, snap.rows.Single(r => r.skillId == 77).requiredLevel, "PC Skills.txt ReqLevel for Nga My Kiếm Pháp is 10.");
            Assert.AreEqual(0, snap.rows[0].learnedLevel);
            Assert.IsTrue(snap.rows[0].canUpgrade);
            StringAssert.Contains("Kiếm Pháp", snap.rows[0].displayName);
        }
    }
}
