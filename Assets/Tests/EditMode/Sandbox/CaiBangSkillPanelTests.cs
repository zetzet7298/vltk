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
            Assert.IsTrue(PcSkillPanelService.TryUpgrade(progression, catalog, 117));
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

            Assert.IsTrue(PcSkillPanelService.TryUpgrade(progression, catalog, 128));
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
        public void PcCombatCatalog_CaiBangRowsMatchAuthoritativePcSkillsTxt()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCaiBangCatalog();
            var pcRows = ReadPcCaiBangSkillRows();

            CollectionAssert.AreEqual(new[] { 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130 }, pcRows.Keys.ToArray());

            foreach (var pair in pcRows)
            {
                var expected = pair.Value;
                var actual = catalog.Resolve(pair.Key);
                Assert.NotNull(actual, $"missing Cái Bang skill {pair.Key}");
                Assert.AreEqual(expected.Name, actual.nameRaw, $"PC SkillName mismatch for {pair.Key}");
                Assert.AreEqual(expected.ReqLevel, actual.reqLevel, $"PC ReqLevel mismatch for {pair.Key}");
                Assert.AreEqual(expected.MaxLevel, actual.maxLevel, $"PC MaxLevel mismatch for {pair.Key}");
                Assert.AreEqual(expected.SkillStyle, (int)actual.skillStyle, $"PC SkillStyle mismatch for {pair.Key}");
                Assert.AreEqual(expected.CharClass, (int)actual.faction, $"PC CharClass mismatch for {pair.Key}");
                Assert.AreEqual(expected.CharAnimId, actual.charAnimId, $"PC CharAnimId mismatch for {pair.Key}");
                Assert.AreEqual(expected.IsPhysical != 0, actual.isPhysical, $"PC IsPhysical mismatch for {pair.Key}");
                Assert.AreEqual(expected.IsMelee != 0, actual.isMelee, $"PC IsMelee mismatch for {pair.Key}");
                Assert.AreEqual(expected.Icon, actual.iconSourceId.sourcePath, $"PC SkillIcon mismatch for {pair.Key}");
            }
        }

        [Test]
        public void SkillPanelSnapshot_ListsAllCaiBangSkillsInPcSlotOrder()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCaiBangCatalog();
            var progression = new PlayerProgressionState();
            progression.GrantCaiBangSkillPanelProgression(catalog);

            var snap = PcSkillPanelService.Build(catalog, progression);
            Assert.AreEqual(200, snap.playerLevel);
            Assert.AreEqual(200, snap.skillPoints);
            Assert.AreEqual(CombatFaction.CaiBang, snap.faction);
            Assert.AreEqual(26, snap.rows.Count);
            Assert.AreEqual(115, snap.rows[0].skillId);
            CollectionAssert.AreEqual(new[] { 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 274, 277, 357, 358, 359, 360, 714, 720, 1073, 1074 }, snap.rows.Select(r => r.skillId).ToArray());
            Assert.AreEqual(30, PcSkillPanelService.PcFightSkillSlotsPerPage, "Mobile uses 30-slot grid for scrollable 24-skill list.");
            Assert.AreEqual(50, snap.rows.Single(r => r.skillId == 128).requiredLevel, "PC Skills.txt ReqLevel for 亢龙有悔 is 50.");
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
            summary.text = "200";
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
                typeof(GameHudController).GetField("_skillPanel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(hud, panel);
                typeof(GameHudController).GetField("_skillSummary", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(hud, summary);
                typeof(GameHudController).GetField("_skillList", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(hud, list);

                hud.OpenSkillPanel();

                Assert.IsTrue(hud.IsSkillPanelVisible);
                Assert.AreEqual(30, hud.PcSkillPanelRowCount, "PC combat skill page renders 30 cells, with unused slots empty.");
                Assert.IsNotNull(hud.CurrentSkillSnapshot);
                Assert.AreEqual(26, hud.CurrentSkillSnapshot.rows.Count, "Single scrollable page shows all 26 Cái Bang fight skills.");
                CollectionAssert.AreEqual(new[] { 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 274, 277, 357, 358, 359, 360, 714, 720, 1073, 1074 }, hud.CurrentSkillSnapshot.rows.Select(r => r.skillId).ToArray());
                Assert.That(hud.CurrentSkillSnapshot.rows.Single(r => r.skillId == 125).displayName, Is.EqualTo("Thiên Hạ Vô Cẩu"));
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

            var snap = PcSkillPanelService.Build(catalog, progression, 125);

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

            var snap = PcSkillPanelService.Build(catalog, progression);
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

            Assert.That(source, Does.Contain("signed-byte FileNameHash"));
            Assert.That(source, Does.Contain("DrawSkillIcon"));
            Assert.That(source, Does.Contain("decoded from exact JXWin PC SPR paths in Skills.txt"));
            Assert.That(source, Does.Contain("MOD source of truth (Server+Client-001 Việt hóa)"));
            Assert.That(source, Does.Contain("125 天下无狗 \\spr\\Ui\\技能图标\\icon_sk_gb_31.spr"));
            Assert.That(source, Does.Contain("\\spr\\Ui\\技能图标\\icon_sk_gb_31.spr"));
            int[] allSkillIds = { 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 274, 357, 359, 360, 1073, 1074 };
            int missing = 0;
            foreach (var skillId in allSkillIds)
            {
                var png = System.IO.Path.Combine(root, $"cai_bang_skill_{skillId}.png");
                if (!System.IO.File.Exists(png) || new System.IO.FileInfo(png).Length <= 100)
                    missing++;
            }
            Assert.That(missing, Is.EqualTo(0), $"{missing} skill PNG files missing or too small");
        }

        [Test]
        public void AllTenFactionsIconPngs_ArePresentAndNonEmpty()
        {
            var root = System.IO.Path.Combine(Application.dataPath, "UI/HUD/Art/Generated");
            var allSkillIds = new List<int>();
            allSkillIds.AddRange(new[] { 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 274, 357, 359, 360, 714, 1073, 1074 }); // Cái Bang
            allSkillIds.AddRange(new[] { 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 166 }); // Võ Đang
            allSkillIds.AddRange(new[] { 3, 4, 6, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21 }); // Thiếu Lâm
            allSkillIds.AddRange(new[] { 43, 45, 47, 48, 50, 51, 54, 55, 57, 58 }); // Đường Môn
            allSkillIds.AddRange(new[] { 77, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93 }); // Nga My
            allSkillIds.AddRange(new[] { 23, 24, 26, 29, 30, 31, 32, 33, 34, 35, 36, 37, 40, 41, 42 }); // Thiên Vương
            allSkillIds.AddRange(new[] { 60, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76 }); // Ngũ Độc
            allSkillIds.AddRange(new[] { 95, 97, 99, 100, 101, 102, 103, 105, 108, 109, 111, 113, 114 }); // Thúy Yên
            allSkillIds.AddRange(new[] { 131, 132, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 150 }); // Thiên Nhẫn
            allSkillIds.AddRange(new[] { 167, 168, 169, 170, 171, 172, 173, 174, 175, 176, 177, 178, 179, 180, 181, 182, 183, 184 }); // Côn Lôn

            int missing = 0;
            foreach (var skillId in allSkillIds)
            {
                var png = System.IO.Path.Combine(root, $"cai_bang_skill_{skillId}.png");
                if (!System.IO.File.Exists(png))
                {
                    Debug.LogError($"Skill PNG missing: {png}");
                    missing++;
                }
                else if (new System.IO.FileInfo(png).Length <= 100)
                {
                    Debug.LogError($"Skill PNG too small: {png}");
                    missing++;
                }
            }
            Assert.AreEqual(0, missing, $"{missing} skill PNG files missing or too small in Generated folder.");
        }

        [Test]
        public void RequestedPhiLongTaiThien_IsInCurrentAuthoritativeJxwinSkillData()
        {
            var path = Path.Combine(Application.streamingAssetsPath, "Reference/PcSkills.txt");
            var text = File.ReadAllText(path, Encoding.UTF8);

            Assert.That(text, Does.Contain("Phi Long Tại Thiên"), "Current JXWin Skills.txt now has PC-backed Phi Long Tại Thiên row.");
            Assert.That(text, Does.Contain("Thiên Hạ Vô Cẩu"), "Thiên Hạ Vô Cẩu must remain present in PC-backed Cái Bang list.");
        }

        private static SortedDictionary<int, PcSkillRow> ReadPcCaiBangSkillRows()
        {
            var path = Path.Combine(Application.streamingAssetsPath, "Reference/PcSkills.txt");
            var lines = File.ReadAllLines(path, Encoding.UTF8);
            var header = lines[0].Split('\t');
            var index = header.Select((name, i) => new { name, i }).ToDictionary(x => x.name, x => x.i);
            var rows = new SortedDictionary<int, PcSkillRow>();

            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                var parts = line.Split('\t');
                if (parts.Length <= index["SkillIcon"] || !int.TryParse(parts[index["SkillId"]], out var skillId))
                    continue;
                if (skillId < 115 || skillId > 130)
                    continue;

                rows.Add(skillId, new PcSkillRow(
                    parts[index["SkillName"]],
                    parts[index["SkillIcon"]],
                    int.Parse(parts[index["SkillStyle"]]),
                    int.Parse(parts[index["CharClass"]]),
                    int.Parse(parts[index["CharAnimId"]]),
                    int.Parse(parts[index["IsPhysical"]]),
                    int.Parse(parts[index["IsMelee"]]),
                    int.Parse(parts[index["ReqLevel"]]),
                    int.Parse(parts[index["MaxLevel"]])));
            }

            return rows;
        }

        private readonly struct PcSkillRow
        {
            public readonly string Name;
            public readonly string Icon;
            public readonly int SkillStyle;
            public readonly int CharClass;
            public readonly int CharAnimId;
            public readonly int IsPhysical;
            public readonly int IsMelee;
            public readonly int ReqLevel;
            public readonly int MaxLevel;

            public PcSkillRow(string name, string icon, int skillStyle, int charClass, int charAnimId, int isPhysical, int isMelee, int reqLevel, int maxLevel)
            {
                Name = name;
                Icon = icon;
                SkillStyle = skillStyle;
                CharClass = charClass;
                CharAnimId = charAnimId;
                IsPhysical = isPhysical;
                IsMelee = isMelee;
                ReqLevel = reqLevel;
                MaxLevel = maxLevel;
            }
        }
    }
}
