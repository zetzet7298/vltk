using System.Linq;
using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;
using VLTK.UI;

namespace VLTK.Tests.Sandbox
{
    public class WuDangSkillPanelTests
    {
        [Test]
        public void GrantWuDangSkillPanelProgression_SetsLevel200Points200AndKnownWuDangSkillsAtZero()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
            var progression = new PlayerProgressionState();
            progression.GrantWuDangSkillPanelProgression(catalog);

            Assert.AreEqual(200, progression.level);
            Assert.AreEqual(200, progression.fightSkillPoints);
            Assert.AreEqual(CombatFaction.WuDang, progression.faction);
            for (int id = PcCombatCatalogFactory.WuDangMinSkillId; id <= PcCombatCatalogFactory.WuDangMaxSkillId; id++ )
            {
                Assert.IsTrue(progression.knownSkills.Contains(id), $"missing known skill {id}");
                Assert.AreEqual(0, progression.skillLevels[id], $"PC join seed should keep skill {id} unspent at level 0");
            }
        }

        [Test]
        public void SkillPanelSnapshot_ListsAllWuDangSkillsInPcSlotOrder()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
            var progression = new PlayerProgressionState();
            progression.GrantWuDangSkillPanelProgression(catalog);

            var snap = PcSkillPanelService.Build(catalog, progression);
            Assert.AreEqual(200, snap.playerLevel);
            Assert.AreEqual(200, snap.skillPoints);
            Assert.AreEqual(CombatFaction.WuDang, snap.faction);
            Assert.AreEqual(16, snap.rows.Count);
            Assert.AreEqual(151, snap.rows[0].skillId);
            CollectionAssert.AreEqual(new[] { 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 166 }, snap.rows.Select(r => r.skillId).ToArray());
            Assert.AreEqual(30, PcSkillPanelService.PcFightSkillSlotsPerPage, "Mobile uses 30-slot grid for scrollable 16-skill list.");
            Assert.AreEqual(50, snap.rows.Single(r => r.skillId == 157).requiredLevel, "PC Skills.txt ReqLevel for Tọa Vọng Vô Ngã is 50.");
            Assert.AreEqual(0, snap.rows[0].learnedLevel);
            Assert.IsTrue(snap.rows[0].canUpgrade);
            StringAssert.Contains("Kiếm Pháp", snap.rows[0].displayName);
        }
    }
}
