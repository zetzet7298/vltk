using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;
using VLTK.UI;

namespace VLTK.Tests.Sandbox
{
    public class TianRenSkillPanelTests
    {
        [Test]
        public void GrantTianRenSkillPanelProgression_SetsLevel200Points200AndKnownTianRenSkillsAtZero()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog(null, includeTianRen: true);
            var progression = new PlayerProgressionState();
            progression.GrantTianRenSkillPanelProgression(catalog);

            Assert.AreEqual(200, progression.level);
            Assert.AreEqual(200, progression.fightSkillPoints);
            Assert.AreEqual(CombatFaction.TianRen, progression.faction);
            
            int[] expectedSkills = { 131, 132, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 150 };
            foreach (int id in expectedSkills)
            {
                Assert.IsTrue(progression.knownSkills.Contains(id), $"missing known skill {id}");
                Assert.AreEqual(0, progression.skillLevels[id], $"PC join seed should keep skill {id} unspent at level 0");
            }
        }

        [Test]
        public void ReopeningPanelProgression_DoesNotResetSpentSkillPointsOrLevels()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog(null, includeTianRen: true);
            var progression = new PlayerProgressionState();
            progression.GrantTianRenSkillPanelProgression(catalog);
            Assert.IsTrue(PcSkillPanelService.TryUpgrade(progression, catalog, 135));
            progression.GrantTianRenSkillPanelProgression(catalog);

            Assert.AreEqual(1, progression.skillLevels[135]);
            Assert.AreEqual(199, progression.fightSkillPoints);
        }

        [Test]
        public void TryUpgradeTianRenSkill_SpendsOnePointAndHonorsPcCaps()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog(null, includeTianRen: true);
            var progression = new PlayerProgressionState();
            progression.GrantTianRenSkillPanelProgression(catalog);

            Assert.IsTrue(PcSkillPanelService.TryUpgrade(progression, catalog, 135));
            Assert.AreEqual(1, progression.skillLevels[135]);
            Assert.AreEqual(199, progression.fightSkillPoints);

            var skill = catalog.Resolve(135);
            for (int i = 1; i < skill.maxLevel; i++)
                Assert.IsTrue(progression.TryUpgradeSkill(skill), $"upgrade {i + 1}");
            Assert.AreEqual(skill.maxLevel, progression.skillLevels[135]);
            Assert.IsFalse(progression.TryUpgradeSkill(skill), "PC rejects upgrades past skill max level");
        }

        [Test]
        public void LowPlayerLevelCannotUpgradePastReqLevelGate()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog(null, includeTianRen: true);
            var progression = new PlayerProgressionState();
            progression.GrantTianRenSkillPanelProgression(catalog);
            progression.level = 10;

            var skill = catalog.Resolve(135); // Req level: 10
            Assert.IsTrue(progression.TryUpgradeSkill(skill));
            Assert.AreEqual(1, progression.skillLevels[135]);
            Assert.IsFalse(progression.TryUpgradeSkill(skill), "PC gate: desired level <= playerLevel - reqLevel + 1");
        }

        [Test]
        public void SkillPanelSnapshot_ListsAllTianRenSkillsInPcSlotOrder()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog(null, includeTianRen: true);
            var progression = new PlayerProgressionState();
            progression.GrantTianRenSkillPanelProgression(catalog);

            var snap = PcSkillPanelService.Build(catalog, progression);
            Assert.AreEqual(200, snap.playerLevel);
            Assert.AreEqual(200, snap.skillPoints);
            Assert.AreEqual(CombatFaction.TianRen, snap.faction);
            Assert.AreEqual(18, snap.rows.Count);
            
            int[] expectedSkills = { 131, 132, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 150 };
            CollectionAssert.AreEqual(expectedSkills, snap.rows.Select(r => r.skillId).ToArray());
            Assert.AreEqual(0, snap.rows[0].learnedLevel);
            Assert.IsTrue(snap.rows[0].canUpgrade);
            StringAssert.Contains("Thiên Nhẫn", snap.rows[0].displayName);
        }
    }
}
