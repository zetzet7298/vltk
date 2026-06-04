using System.Linq;
using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;
using VLTK.UI;

namespace VLTK.Tests.Sandbox
{
    public class WuDuSkillPanelTests
    {
        [Test]
        public void GrantWuDuSkillPanelProgression_SetsLevel200Points200AndKnownWuDuSkillsAtZero()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
            var progression = new PlayerProgressionState();
            progression.GrantWuDuSkillPanelProgression(catalog);

            Assert.AreEqual(200, progression.level);
            Assert.AreEqual(200, progression.fightSkillPoints);
            Assert.AreEqual(CombatFaction.WuDu, progression.faction);
            
            // Check that all 16 WuDu skills are known and start at level 0
            var expectedIds = new[] { 60, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76 };
            foreach (int id in expectedIds)
            {
                Assert.IsTrue(progression.knownSkills.Contains(id), $"missing known skill {id}");
                Assert.AreEqual(0, progression.skillLevels[id], $"PC join seed should keep skill {id} unspent at level 0");
            }
        }

        [Test]
        public void SkillPanelSnapshot_ListsAllWuDuSkillsInPcSlotOrder()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
            var progression = new PlayerProgressionState();
            progression.GrantWuDuSkillPanelProgression(catalog);

            var snap = PcSkillPanelService.Build(catalog, progression);
            Assert.AreEqual(200, snap.playerLevel);
            Assert.AreEqual(200, snap.skillPoints);
            Assert.AreEqual(CombatFaction.WuDu, snap.faction);
            Assert.AreEqual(16, snap.rows.Count);
            Assert.AreEqual(60, snap.rows[0].skillId);
            
            var expectedIds = new[] { 60, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76 };
            CollectionAssert.AreEqual(expectedIds, snap.rows.Select(r => r.skillId).ToArray());
            
            Assert.AreEqual(30, PcSkillPanelService.PcFightSkillSlotsPerPage, "Mobile uses 30-slot grid for scrollable 16-skill list.");
            Assert.AreEqual(10, snap.rows.Single(r => r.skillId == 60).requiredLevel, "PC Skills.txt ReqLevel for Ngũ Độc Đao Pháp is 10.");
            Assert.AreEqual(0, snap.rows[0].learnedLevel);
            Assert.IsTrue(snap.rows[0].canUpgrade);
            StringAssert.Contains("Đao Pháp", snap.rows[0].displayName);
        }
    }
}
