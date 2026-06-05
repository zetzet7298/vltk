using System.Linq;
using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;
using VLTK.UI;

namespace VLTK.Tests.Sandbox
{
    public class TianWangSkillPanelTests
    {
        [Test]
        public void GrantTianWangSkillPanelProgression_SetsLevel200Points200AndKnownTianWangSkillsAtZero()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
            var progression = new PlayerProgressionState();
            progression.GrantTianWangSkillPanelProgression(catalog);

            Assert.AreEqual(200, progression.level);
            Assert.AreEqual(200, progression.fightSkillPoints);
            Assert.AreEqual(CombatFaction.TianWang, progression.faction);
            
            // Check that all 15 TianWang skills are known and start at level 0
            var expectedIds = new[] { 23, 24, 26, 29, 30, 31, 32, 33, 34, 35, 36, 37, 40, 41, 42 };
            foreach (int id in expectedIds)
            {
                Assert.IsTrue(progression.knownSkills.Contains(id), $"missing known skill {id}");
                Assert.AreEqual(0, progression.skillLevels[id], $"PC join seed should keep skill {id} unspent at level 0");
            }
        }

        [Test]
        public void SkillPanelSnapshot_ListsAllTianWangSkillsInPcSlotOrder()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
            var progression = new PlayerProgressionState();
            progression.GrantTianWangSkillPanelProgression(catalog);

            var snap = PcSkillPanelService.Build(catalog, progression);
            Assert.AreEqual(200, snap.playerLevel);
            Assert.AreEqual(200, snap.skillPoints);
            Assert.AreEqual(CombatFaction.TianWang, snap.faction);
            Assert.AreEqual(15, snap.rows.Count);
            Assert.AreEqual(23, snap.rows[0].skillId);
            
            var expectedIds = new[] { 23, 24, 26, 29, 30, 31, 32, 33, 34, 35, 36, 37, 40, 41, 42 };
            CollectionAssert.AreEqual(expectedIds, snap.rows.Select(r => r.skillId).ToArray());
            
            Assert.AreEqual(30, PcSkillPanelService.PcFightSkillSlotsPerPage, "Mobile uses 30-slot grid for scrollable 15-skill list.");
            Assert.AreEqual(10, snap.rows.Single(r => r.skillId == 23).requiredLevel, "PC Skills.txt ReqLevel for Thiên Vương Thương Pháp is 10.");
            Assert.AreEqual(0, snap.rows[0].learnedLevel);
            Assert.IsTrue(snap.rows[0].canUpgrade);
            StringAssert.Contains("Thương Pháp", snap.rows[0].displayName);
        }
    }
}
