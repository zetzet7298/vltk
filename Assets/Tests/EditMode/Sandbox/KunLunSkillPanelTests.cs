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
    public class KunLunSkillPanelTests
    {
        [Test]
        public void GrantKunLunSkillPanelProgression_SetsLevel200Points200AndKnownKunLunSkillsAtZero()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog(null, includeKunLun: true);
            var progression = new PlayerProgressionState();
            progression.GrantKunLunSkillPanelProgression(catalog);

            Assert.AreEqual(200, progression.level);
            Assert.AreEqual(200, progression.fightSkillPoints);
            Assert.AreEqual(CombatFaction.KunLun, progression.faction);
            
            int[] expectedSkills = { 167, 168, 169, 170, 171, 172, 173, 174, 175, 176, 177, 178, 179, 180, 181, 182, 183, 184 };
            foreach (int id in expectedSkills)
            {
                Assert.IsTrue(progression.knownSkills.Contains(id), $"missing known skill {id}");
                Assert.AreEqual(0, progression.skillLevels[id], $"PC join seed should keep skill {id} unspent at level 0");
            }
        }

        [Test]
        public void ReopeningPanelProgression_DoesNotResetSpentSkillPointsOrLevels()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog(null, includeKunLun: true);
            var progression = new PlayerProgressionState();
            progression.GrantKunLunSkillPanelProgression(catalog);
            Assert.IsTrue(PcSkillPanelService.TryUpgrade(progression, catalog, 169));
            progression.GrantKunLunSkillPanelProgression(catalog);

            Assert.AreEqual(1, progression.skillLevels[169]);
            Assert.AreEqual(199, progression.fightSkillPoints);
        }

        [Test]
        public void TryUpgradeKunLunSkill_SpendsOnePointAndHonorsPcCaps()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog(null, includeKunLun: true);
            var progression = new PlayerProgressionState();
            progression.GrantKunLunSkillPanelProgression(catalog);

            Assert.IsTrue(PcSkillPanelService.TryUpgrade(progression, catalog, 169));
            Assert.AreEqual(1, progression.skillLevels[169]);
            Assert.AreEqual(199, progression.fightSkillPoints);

            var skill = catalog.Resolve(169);
            for (int i = 1; i < skill.maxLevel; i++)
                Assert.IsTrue(progression.TryUpgradeSkill(skill), $"upgrade {i + 1}");
            Assert.AreEqual(skill.maxLevel, progression.skillLevels[169]);
            Assert.IsFalse(progression.TryUpgradeSkill(skill), "PC rejects upgrades past skill max level");
        }

        [Test]
        public void LowPlayerLevelCannotUpgradePastReqLevelGate()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog(null, includeKunLun: true);
            var progression = new PlayerProgressionState();
            progression.GrantKunLunSkillPanelProgression(catalog);
            progression.level = 10;

            var skill = catalog.Resolve(169); // Req level: 10
            Assert.IsTrue(progression.TryUpgradeSkill(skill));
            Assert.AreEqual(1, progression.skillLevels[169]);
            Assert.IsFalse(progression.TryUpgradeSkill(skill), "PC gate: desired level <= playerLevel - reqLevel + 1");
        }

        [Test]
        public void SkillPanelSnapshot_ListsAllKunLunSkillsInPcSlotOrder()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog(null, includeKunLun: true);
            var progression = new PlayerProgressionState();
            progression.GrantKunLunSkillPanelProgression(catalog);

            var snap = PcSkillPanelService.Build(catalog, progression);
            Assert.AreEqual(200, snap.playerLevel);
            Assert.AreEqual(200, snap.skillPoints);
            Assert.AreEqual(CombatFaction.KunLun, snap.faction);
            Assert.AreEqual(18, snap.rows.Count);
            
            int[] expectedSkills = { 167, 168, 169, 170, 171, 172, 173, 174, 175, 176, 177, 178, 179, 180, 181, 182, 183, 184 };
            CollectionAssert.AreEqual(expectedSkills, snap.rows.Select(r => r.skillId).ToArray());
            Assert.AreEqual(0, snap.rows[0].learnedLevel);
            Assert.IsTrue(snap.rows[0].canUpgrade);
            StringAssert.Contains("Côn Lôn", snap.rows[0].displayName);
        }
    }
}
