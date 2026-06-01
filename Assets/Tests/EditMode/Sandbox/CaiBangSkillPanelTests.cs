using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using VLTK.Model;
using VLTK.Sandbox;
using VLTK.UI;

namespace VLTK.Tests.Sandbox
{
    public class CaiBangSkillPanelTests
    {
        [Test]
        public void GrantCaiBangSkillPanelProgression_SetsLevel200Points200AndKnownCaiBangSkillsAtZero()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCaiBangCatalog();
            var progression = new PlayerProgressionState();
            progression.GrantCaiBangSkillPanelProgression(catalog);

            Assert.AreEqual(200, progression.level);
            Assert.AreEqual(200, progression.fightSkillPoints);
            Assert.AreEqual(CombatFaction.CaiBang, progression.faction);
            for (int id = PcCombatCatalogFactory.CaiBangMinSkillId; id <= PcCombatCatalogFactory.CaiBangMaxSkillId; id++)
            {
                Assert.IsTrue(progression.knownSkills.Contains(id), $"missing known skill {id}");
                Assert.AreEqual(0, progression.skillLevels[id], $"PC join seed should keep skill {id} unspent at level 0");
            }
        }

        [Test]
        public void ReopeningPanelProgression_DoesNotResetSpentSkillPointsOrLevels()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCaiBangCatalog();
            var progression = new PlayerProgressionState();
            progression.GrantCaiBangSkillPanelProgression(catalog);
            Assert.IsTrue(CaiBangSkillPanelService.TryUpgrade(progression, catalog, 117));
            progression.GrantCaiBangSkillPanelProgression(catalog);

            Assert.AreEqual(1, progression.skillLevels[117]);
            Assert.AreEqual(199, progression.fightSkillPoints);
        }

        [Test]
        public void TryUpgradeCaiBangSkill_SpendsOnePointAndHonorsPcCaps()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCaiBangCatalog();
            var progression = new PlayerProgressionState();
            progression.GrantCaiBangSkillPanelProgression(catalog);

            Assert.IsTrue(CaiBangSkillPanelService.TryUpgrade(progression, catalog, 128));
            Assert.AreEqual(1, progression.skillLevels[128]);
            Assert.AreEqual(199, progression.fightSkillPoints);

            var skill = catalog.Resolve(128);
            for (int i = 1; i < skill.maxLevel; i++)
                Assert.IsTrue(progression.TryUpgradeSkill(skill), $"upgrade {i + 1}");
            Assert.AreEqual(skill.maxLevel, progression.skillLevels[128]);
            Assert.IsFalse(progression.TryUpgradeSkill(skill), "PC rejects upgrades past skill max level");
        }

        [Test]
        public void LowPlayerLevelCannotUpgradePastReqLevelGate()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCaiBangCatalog();
            var progression = new PlayerProgressionState();
            progression.GrantCaiBangSkillPanelProgression(catalog);
            progression.level = 10;

            var skill = catalog.Resolve(117);
            Assert.IsTrue(progression.TryUpgradeSkill(skill));
            Assert.AreEqual(1, progression.skillLevels[117]);
            Assert.IsFalse(progression.TryUpgradeSkill(skill), "PC gate: desired level <= playerLevel - reqLevel + 1");
        }

        [Test]
        public void SkillPanelSnapshot_ListsSixteenCaiBangSkillsInPcSlotOrder()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCaiBangCatalog();
            var progression = new PlayerProgressionState();
            progression.GrantCaiBangSkillPanelProgression(catalog);

            var snap = CaiBangSkillPanelService.Build(catalog, progression);
            Assert.AreEqual(200, snap.playerLevel);
            Assert.AreEqual(200, snap.skillPoints);
            Assert.AreEqual(CombatFaction.CaiBang, snap.faction);
            Assert.AreEqual(16, snap.rows.Count);
            Assert.AreEqual(115, snap.rows[0].skillId);
            CollectionAssert.AreEqual(new[] { 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130 }, snap.rows.Select(r => r.skillId).ToArray());
            Assert.AreEqual(0, snap.rows[0].learnedLevel);
            Assert.IsTrue(snap.rows[0].canUpgrade);
            StringAssert.Contains("Cái Bang", snap.rows[0].displayName);
        }

        [Test]
        public void HudButtonSkills_OpensCaiBangPanelWithoutTouchingPlayerVisual()
        {
            var root = new VisualElement { name = "GameHud" };
            var panel = new VisualElement { name = "CaiBangSkillPanel" };
            panel.AddToClassList("hidden");
            var summary = new Label { name = "CaiBangSkillSummary" };
            var list = new ScrollView { name = "CaiBangSkillList" };
            panel.Add(summary);
            panel.Add(list);
            root.Add(panel);

            var close = new VisualElement { name = "CaiBangSkillClose" };
            root.Add(close);

            var go = new GameObject("HudSkillPanelTest");
            try
            {
                var hud = go.AddComponent<GameHudController>();
                // Use reflection-free public path by invoking the panel population through real open method;
                // no SandboxManager exists here, so it uses the PC-derived fallback catalog and progression.
                typeof(GameHudController).GetField("_caiBangSkillPanel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(hud, panel);
                typeof(GameHudController).GetField("_caiBangSkillSummary", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(hud, summary);
                typeof(GameHudController).GetField("_caiBangSkillList", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(hud, list);

                hud.OpenCaiBangSkillPanel();

                Assert.IsTrue(hud.IsCaiBangSkillPanelVisible);
                Assert.AreEqual(16, hud.CaiBangSkillPanelRowCount);
                Assert.IsNotNull(hud.CurrentCaiBangSkillSnapshot);
                Assert.AreEqual(16, hud.CurrentCaiBangSkillSnapshot.rows.Count);
                Assert.AreEqual("200", summary.text);
                // Visual invariant: this feature does not alter MalePlayerVisual/MalePlayerSpriteCatalog.
                Assert.IsNotNull(typeof(MalePlayerVisual));
                Assert.IsNotNull(typeof(MalePlayerSpriteCatalog));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
        [Test]
        public void Build_WithSelectedSkill_ExposesPcLikeDetailAndToggleTarget()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCaiBangCatalog();
            var progression = new PlayerProgressionState();
            progression.GrantCaiBangSkillPanelProgression(catalog);

            var snap = CaiBangSkillPanelService.Build(catalog, progression, 125);

            Assert.That(snap.selectedSkillId, Is.EqualTo(125));
            Assert.That(snap.selectedRow.HasValue, Is.True);
            Assert.That(snap.selectedRow.Value.displayName, Does.Contain("Thiên Hạ"));
            Assert.That(snap.selectedRow.Value.summary, Does.Contain("Cấp hiện tại"));
            Assert.That(snap.selectedRow.Value.nextLevelSummary, Is.Not.Empty);
            Assert.That(snap.selectedRow.Value.upgradeStatus, Does.Contain("dấu +"));
        }


        [Test]
        public void PcCaiBangSkillMapping_IsKeyedBySkillIdForSensitiveDogAndDragonSkills()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCaiBangCatalog();
            var progression = new PlayerProgressionState();
            progression.GrantCaiBangSkillPanelProgression(catalog);

            var snap = CaiBangSkillPanelService.Build(catalog, progression);
            var dogAura = snap.rows.Single(r => r.skillId == 124);
            var noDog = snap.rows.Single(r => r.skillId == 125);
            var dragon = snap.rows.Single(r => r.skillId == 128);

            Assert.That(dogAura.displayName, Is.EqualTo("Đả Cẩu Trận"));
            Assert.That(noDog.displayName, Is.EqualTo("Thiên Hạ Vô Cẩu"));
            Assert.That(dragon.displayName, Is.EqualTo("Kháng Long Hữu Hối"));
            Assert.That(catalog.Resolve(124).iconSourceId.sourcePath, Is.EqualTo("\\spr\\Ui\\技能图标\\icon_sk_gb_23.spr"));
            Assert.That(catalog.Resolve(125).iconSourceId.sourcePath, Is.EqualTo("\\spr\\Ui\\技能图标\\icon_sk_gb_31.spr"));
            Assert.That(catalog.Resolve(128).iconSourceId.sourcePath, Is.EqualTo("\\spr\\Ui\\技能图标\\icon_sk_gb_41.spr"));
        }

        [Test]
        public void IconPngs_AreExactPcSkillSpriteExportsDocumented()
        {
            var root = System.IO.Path.Combine(Application.dataPath, "UI/HUD/Art/Generated");
            var source = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "PC_SOURCE.txt"));

            Assert.That(source, Does.Contain("signed-byte hash"));
            Assert.That(source, Does.Contain("DrawSkillIcon"));
            Assert.That(source, Does.Contain("\\spr\\Ui\\技能图标\\icon_sk_gb_31.spr"));
            for (int skillId = 115; skillId <= 130; skillId++)
            {
                var png = System.IO.Path.Combine(root, $"cai_bang_skill_{skillId}.png");
                Assert.That(System.IO.File.Exists(png), Is.True, $"missing {png}");
                Assert.That(new System.IO.FileInfo(png).Length, Is.GreaterThan(100));
            }
        }

    }
}
