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
            
              // Canonical PC learning evidence is the 23-ID oracle set. The legacy panel
              // may still display 51/55/57, but they are not learned/cast/upgrade state.
              var expectedLearnedIds = new[]
              {
                  43, 45, 47, 48, 50, 54, 58, 249, 302, 303, 339, 341, 342,
                  343, 345, 347, 349, 351, 710, 1069, 1070, 1071, 1110,
              };
              foreach (int id in expectedLearnedIds)
              {
                  Assert.IsTrue(progression.knownSkills.Contains(id), $"missing known skill {id}");
                  Assert.AreEqual(0, progression.skillLevels[id], $"PC join seed should keep skill {id} unspent at level 0");
              }

              var expectedKnownIds = expectedLearnedIds.Append(PcCombatCatalogFactory.UniversalLightnessSkill)
                  .OrderBy(id => id).ToArray();
              CollectionAssert.AreEqual(expectedKnownIds, progression.knownSkills.OrderBy(id => id).ToArray());
              foreach (int id in new[] { 51, 55, 57 })
              {
                  Assert.IsFalse(progression.knownSkills.Contains(id), $"display-only residual {id} must not be learned");
                  Assert.IsFalse(progression.skillLevels.ContainsKey(id), $"display-only residual {id} must not have a level entry");
                  Assert.IsFalse(progression.CanUpgradeSkill(catalog.Resolve(id)), $"display-only residual {id} must not be upgradeable");
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
              Assert.AreEqual(23, snap.rows.Count, "every canonical player-learnable TangMen root is visible");
              Assert.AreEqual(43, snap.rows[0].skillId);

              var expectedIds = new[]
              {
                  43, 45, 47, 48, 50, 54, 58,
                  249, 302, 303, 339, 341, 342, 343, 345, 347, 349, 351,
                  710, 1069, 1070, 1071, 1110,
              };
                CollectionAssert.AreEqual(expectedIds, snap.rows.Select(r => r.skillId).ToArray());
                CollectionAssert.DoesNotContain(snap.rows.Select(r => r.skillId).ToArray(), 51);
                CollectionAssert.DoesNotContain(snap.rows.Select(r => r.skillId).ToArray(), 55);
                CollectionAssert.DoesNotContain(snap.rows.Select(r => r.skillId).ToArray(), 57);
                Assert.AreEqual("Tiểu Lý Phi Đao", snap.rows.Single(r => r.skillId == 249).displayName,
                    "static-only row keeps its PC Vietnamese display name");
            
              Assert.AreEqual(25, PcSkillPanelService.GetDisplaySlotCount(CombatFaction.TangMen),
                  "UiSkillsFightSub.ini is a PC-authentic five-by-five grid, enough for all 23 canonical TangMen roots.");
            Assert.AreEqual(60, snap.rows.Single(r => r.skillId == 48).requiredLevel,
                "Frozen PC TangMen oracle pins Skills.txt ReqLevel for Tâm Nhãn to 60.");
            Assert.AreEqual(0, snap.rows[0].learnedLevel);
            Assert.IsTrue(snap.rows[0].canUpgrade);
              StringAssert.Contains("Ám Khí", snap.rows[0].displayName);
          }

          [Test]
          public void MaxAllSkillLevels_DoesNotPromoteTangMenDisplayResiduals()
          {
              var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
              var progression = new PlayerProgressionState { faction = CombatFaction.TangMen };

              progression.MaxAllSkillLevels(catalog);

              foreach (int id in new[] { 51, 55, 57 })
              {
                  Assert.IsFalse(progression.knownSkills.Contains(id), $"display-only residual {id} must not be learned by MaxAll");
                  Assert.IsFalse(progression.skillLevels.ContainsKey(id), $"display-only residual {id} must not receive a max level");
              }
          }
    }
}
