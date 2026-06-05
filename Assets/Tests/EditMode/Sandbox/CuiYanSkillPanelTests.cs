using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using VLTK.Model;
using VLTK.Sandbox;
using VLTK.UI;

namespace VLTK.Tests.Sandbox
{
    public class CuiYanSkillPanelTests
    {
        [Test]
        public void GrantCuiYanSkillPanelProgression_SetsLevel200Points200AndKnownCuiYanSkillsAtZero()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog(null, includeWuDang: false, includeShaolin: false, includeTangMen: false, includeEMei: false, includeTianWang: false, includeWuDu: false, includeCuiYan: true);
            var progression = new PlayerProgressionState();
            progression.GrantCuiYanSkillPanelProgression(catalog);

            Assert.AreEqual(200, progression.level);
            Assert.AreEqual(200, progression.fightSkillPoints);
            Assert.AreEqual(CombatFaction.CuiYan, progression.faction);
            
            int[] expectedSkills = { 95, 97, 99, 100, 101, 102, 103, 105, 108, 109, 111, 113, 114 };
            foreach (int id in expectedSkills)
            {
                Assert.IsTrue(progression.knownSkills.Contains(id), $"missing known skill {id}");
                Assert.AreEqual(0, progression.skillLevels[id], $"PC join seed should keep skill {id} unspent at level 0");
            }
        }

        [Test]
        public void ReopeningPanelProgression_DoesNotResetSpentSkillPointsOrLevels()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog(null, includeWuDang: false, includeShaolin: false, includeTangMen: false, includeEMei: false, includeTianWang: false, includeWuDu: false, includeCuiYan: true);
            var progression = new PlayerProgressionState();
            progression.GrantCuiYanSkillPanelProgression(catalog);
            Assert.IsTrue(PcSkillPanelService.TryUpgrade(progression, catalog, 99));
            progression.GrantCuiYanSkillPanelProgression(catalog);

            Assert.AreEqual(1, progression.skillLevels[99]);
            Assert.AreEqual(199, progression.fightSkillPoints);
        }

        [Test]
        public void TryUpgradeCuiYanSkill_SpendsOnePointAndHonorsPcCaps()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog(null, includeWuDang: false, includeShaolin: false, includeTangMen: false, includeEMei: false, includeTianWang: false, includeWuDu: false, includeCuiYan: true);
            var progression = new PlayerProgressionState();
            progression.GrantCuiYanSkillPanelProgression(catalog);

            Assert.IsTrue(PcSkillPanelService.TryUpgrade(progression, catalog, 105));
            Assert.AreEqual(1, progression.skillLevels[105]);
            Assert.AreEqual(199, progression.fightSkillPoints);

            var skill = catalog.Resolve(105);
            for (int i = 1; i < skill.maxLevel; i++)
                Assert.IsTrue(progression.TryUpgradeSkill(skill), $"upgrade {i + 1}");
            Assert.AreEqual(skill.maxLevel, progression.skillLevels[105]);
            Assert.IsFalse(progression.TryUpgradeSkill(skill), "PC rejects upgrades past skill max level");
        }

        [Test]
        public void LowPlayerLevelCannotUpgradePastReqLevelGate()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog(null, includeWuDang: false, includeShaolin: false, includeTangMen: false, includeEMei: false, includeTianWang: false, includeWuDu: false, includeCuiYan: true);
            var progression = new PlayerProgressionState();
            progression.GrantCuiYanSkillPanelProgression(catalog);
            progression.level = 10;

            var skill = catalog.Resolve(99);
            Assert.IsTrue(progression.TryUpgradeSkill(skill));
            Assert.AreEqual(1, progression.skillLevels[99]);
            Assert.IsFalse(progression.TryUpgradeSkill(skill), "PC gate: desired level <= playerLevel - reqLevel + 1");
        }

        [Test]
        public void SkillPanelSnapshot_ListsAllCuiYanSkillsInPcSlotOrder()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog(null, includeWuDang: false, includeShaolin: false, includeTangMen: false, includeEMei: false, includeTianWang: false, includeWuDu: false, includeCuiYan: true);
            var progression = new PlayerProgressionState();
            progression.GrantCuiYanSkillPanelProgression(catalog);

            var snap = PcSkillPanelService.Build(catalog, progression);
            Assert.AreEqual(200, snap.playerLevel);
            Assert.AreEqual(200, snap.skillPoints);
            Assert.AreEqual(CombatFaction.CuiYan, snap.faction);
            Assert.AreEqual(13, snap.rows.Count);
            
            int[] expectedSkills = { 95, 97, 99, 100, 101, 102, 103, 105, 108, 109, 111, 113, 114 };
            CollectionAssert.AreEqual(expectedSkills, snap.rows.Select(r => r.skillId).ToArray());
            Assert.AreEqual(0, snap.rows[0].learnedLevel);
            Assert.IsTrue(snap.rows[0].canUpgrade);
            StringAssert.Contains("Thúy Yên", snap.rows[0].displayName);
        }
    }
}
