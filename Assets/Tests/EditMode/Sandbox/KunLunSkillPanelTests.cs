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
            
            // Canonical PC learning evidence is the 24-ID oracle set (PcKunLunOracle.json). The
            // legacy panel may still display 170/177/180/183/184, but they are not learned/cast/
            // upgrade state.
            int[] expectedLearnedIds =
            {
                90, 167, 168, 169, 171, 172, 173, 174, 175, 176, 178, 179, 181, 182,
                275, 372, 375, 392, 393, 394, 630, 717, 1080, 1081,
            };
            foreach (int id in expectedLearnedIds)
            {
                Assert.IsTrue(progression.knownSkills.Contains(id), $"missing known skill {id}");
                Assert.AreEqual(0, progression.skillLevels[id], $"PC join seed should keep skill {id} unspent at level 0");
            }

            var expectedKnownIds = expectedLearnedIds.Append(PcCombatCatalogFactory.UniversalLightnessSkill)
                .OrderBy(id => id).ToArray();
            CollectionAssert.AreEqual(expectedKnownIds, progression.knownSkills.OrderBy(id => id).ToArray());
            foreach (int id in new[] { 170, 177, 180, 183, 184 })
            {
                Assert.IsFalse(progression.knownSkills.Contains(id), $"display-only residual {id} must not be learned");
                Assert.IsFalse(progression.skillLevels.ContainsKey(id), $"display-only residual {id} must not have a level entry");
                Assert.IsFalse(progression.CanUpgradeSkill(catalog.Resolve(id)), $"display-only residual {id} must not be upgradeable");
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
        public void SkillPanelSnapshot_ListsObservedKunLunDisplayRows_ResidualsNonUpgradeable()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog(null, includeKunLun: true);
            var progression = new PlayerProgressionState();
            progression.GrantKunLunSkillPanelProgression(catalog);

            var snap = PcSkillPanelService.Build(catalog, progression);
            Assert.AreEqual(200, snap.playerLevel);
            Assert.AreEqual(200, snap.skillPoints);
            Assert.AreEqual(CombatFaction.KunLun, snap.faction);
            // uiOrder stays unproven against the frozen oracle: assert only the observed 18-row
            // display contract as a set, never an inferred PC slot order.
            Assert.AreEqual(18, snap.rows.Count);
            int[] observedDisplayIds = { 167, 168, 169, 170, 171, 172, 173, 174, 175, 176, 177, 178, 179, 180, 181, 182, 183, 184 };
            CollectionAssert.AreEquivalent(observedDisplayIds, snap.rows.Select(r => r.skillId).ToArray());
            foreach (int id in new[] { 170, 177, 180, 183, 184 })
                Assert.IsFalse(snap.rows.Single(r => r.skillId == id).canUpgrade,
                    $"legacy display residual {id} must render as non-upgradeable");
            var first = snap.rows.First(r => r.skillId == 167);
            Assert.AreEqual(0, first.learnedLevel);
            Assert.IsTrue(first.canUpgrade);
            StringAssert.Contains("Côn Lôn", first.displayName);
        }

        [Test]
        public void MaxAllSkillLevels_DoesNotPromoteKunLunDisplayResiduals()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog(null, includeKunLun: true);
            var progression = new PlayerProgressionState { faction = CombatFaction.KunLun };

            progression.MaxAllSkillLevels(catalog);

            foreach (int id in new[] { 170, 177, 180, 183, 184 })
            {
                Assert.IsFalse(progression.knownSkills.Contains(id), $"display-only residual {id} must not be learned by MaxAll");
                Assert.IsFalse(progression.skillLevels.ContainsKey(id), $"display-only residual {id} must not receive a max level");
            }
        }
    }
}
