using System.Linq;
using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;
using VLTK.UI;

namespace VLTK.Tests.Sandbox
{
    public class ShaolinSkillPanelTests
    {
        [Test]
        public void GrantShaolinSkillPanelProgression_SetsLevel200Points200AndKnownShaolinSkillsAtZero()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
            var progression = new PlayerProgressionState();
            progression.GrantShaolinSkillPanelProgression(catalog);

            Assert.AreEqual(200, progression.level);
            Assert.AreEqual(200, progression.fightSkillPoints);
            Assert.AreEqual(CombatFaction.Shaolin, progression.faction);
            
            // Check that all Shaolin skills are known and start at level 0
            var expectedIds = new[] { 3, 4, 6, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21 };
            foreach (int id in expectedIds)
            {
                Assert.IsTrue(progression.knownSkills.Contains(id), $"missing known skill {id}");
                Assert.AreEqual(0, progression.skillLevels[id], $"PC join seed should keep skill {id} unspent at level 0");
            }
        }

        [Test]
        public void SkillPanelSnapshot_ListsAllShaolinSkillsInPcSlotOrder()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
            var progression = new PlayerProgressionState();
            progression.GrantShaolinSkillPanelProgression(catalog);

            var snap = PcSkillPanelService.Build(catalog, progression);
            Assert.AreEqual(200, snap.playerLevel);
            Assert.AreEqual(200, snap.skillPoints);
            Assert.AreEqual(CombatFaction.Shaolin, snap.faction);
            Assert.AreEqual(17, snap.rows.Count);
            Assert.AreEqual(3, snap.rows[0].skillId);
            
            var expectedIds = new[] { 3, 4, 6, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21 };
            CollectionAssert.AreEqual(expectedIds, snap.rows.Select(r => r.skillId).ToArray());
            
            Assert.AreEqual(30, PcSkillPanelService.PcFightSkillSlotsPerPage, "Mobile uses 30-slot grid for scrollable 17-skill list.");
            Assert.AreEqual(20, snap.rows.Single(r => r.skillId == 12).requiredLevel, "PC Skills.txt ReqLevel for Kim Cang Hộ Thể is 20.");
            Assert.AreEqual(0, snap.rows[0].learnedLevel);
            Assert.IsTrue(snap.rows[0].canUpgrade);
            StringAssert.Contains("Kiếm Pháp", snap.rows[0].displayName);
        }
    }
}
