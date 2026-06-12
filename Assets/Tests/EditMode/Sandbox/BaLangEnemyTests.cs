using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class BaLangEnemyTests
    {
        [Test]
        public void Database_HasVietnameseNamesAndPcAiFields()
        {
            var templates = BaLangEnemyDatabase.CreateTemplates().ToList();
            Assert.AreEqual(3, templates.Count, "Should have 3 supported outside-town enemy templates: Mèo vàng, Hươu sao, Heo trắng");
            Assert.IsTrue(templates.All(t => !string.IsNullOrWhiteSpace(t.nameNormalized)));
            Assert.IsTrue(templates.All(t => !ContainsChinese(t.nameNormalized)));
            // Real PC AI modes from NpcS.txt rows templateId+1: 1 (cat), 4 (deer/pig)
            Assert.IsTrue(templates.Any(t => t.aiMode == 1), "Mèo vàng should have AI mode 1");
            Assert.IsTrue(templates.Count(t => t.aiMode == 4) == 2, "Hươu sao and Heo trắng should have AI mode 4");
            Assert.IsTrue(templates.All(t => t.maxLife > 0));
        }

        [Test]
        public void Database_EnemyTemplateIds_AreCorrect()
        {
            CollectionAssert.AreEquivalent(new[] { 31, 42, 43 }, BaLangEnemyDatabase.EnemyTemplateIds);
            CollectionAssert.AreEquivalent(new[] { 31, 42, 43 }, BaLangEnemyDatabase.AllTemplateIds);
        }

        [Test]
        public void MpsToWorld_ConvertsCorrectly()
        {
            // Test with known values from Region_S data:
            // tid=31 mps=(48352,93216) region=(94,91)
            var pos1 = BaLangEnemyDatabase.MpsToWorld(48352, 93216);
            Assert.AreEqual(48352f, pos1.x, 0.1f, "worldX should equal mpsX");
            // regionRow = 93216/1024 = 91
            // worldY = -(93216 - 91*512) = -(93216 - 46592) = -46624
            Assert.AreEqual(-46624f, pos1.y, 0.1f, "worldY for mpsY=93216");

            // tid=42 mps=(48473,93264) region=(94,91)
            var pos2 = BaLangEnemyDatabase.MpsToWorld(48473, 93264);
            Assert.AreEqual(48473f, pos2.x, 0.1f);
            Assert.AreEqual(-46672f, pos2.y, 0.1f, "worldY for mpsY=93264");

            // tid=43 mps=(48406,93389) region=(94,91)
            var pos3 = BaLangEnemyDatabase.MpsToWorld(48406, 93389);
            Assert.AreEqual(48406f, pos3.x, 0.1f);
            Assert.AreEqual(-46797f, pos3.y, 0.1f, "worldY for mpsY=93389");
        }

        [Test]
        public void RegionSScanner_LoadsAllBaLangEnemySpawns()
        {
            var folder = Path.Combine(Application.streamingAssetsPath, "TestData", "Regions", "Map_79");
            if (!Directory.Exists(folder))
            {
                Assert.Inconclusive($"Region_S folder not found: {folder}");
                return;
            }

            var spawns = BaLangEnemyRegionScanner.ScanRegionS(folder);
            Assert.Greater(spawns.Count, 400, "Ba Lang Region_S should have 500+ total NPC entries");

            // Filter to enemies only (kind=0)
            var enemies = spawns.Where(s => s.kind == 0).ToList();
            Assert.Greater(enemies.Count, 400, "Should have 500+ enemies (kind=0)");

            // Verify the 3 main enemy template IDs are present
            var templateIds = enemies.Select(s => s.templateId).Distinct().ToList();
            Assert.IsTrue(templateIds.Contains(31), "Should have template 31 (Mèo vàng)");
            Assert.IsTrue(templateIds.Contains(42), "Should have template 42 (Hươu sao)");
            Assert.IsTrue(templateIds.Contains(43), "Should have template 43 (Heo trắng)");

            // All spawns should have valid MPS coordinates
            Assert.IsTrue(enemies.All(e => e.mpsX > 0 && e.mpsY > 0), "All spawns should have positive MPS coords");

            // All spawns should have a series (ngũ hành) value 0-4
            Assert.IsTrue(enemies.All(e => e.series >= 0 && e.series <= 4), "Series should be 0-4");
        }

        [Test]
        public void RegionSScanner_ParsesSingleFile()
        {
            var folder = Path.Combine(Application.streamingAssetsPath, "TestData", "Regions", "Map_79");
            if (!Directory.Exists(folder))
            {
                Assert.Inconclusive($"Region_S folder not found: {folder}");
                return;
            }

            var firstFile = Directory.GetFiles(folder, "*_Region_S.dat").FirstOrDefault();
            if (firstFile == null)
            {
                Assert.Inconclusive("No Region_S.dat files found");
                return;
            }

            var entries = BaLangEnemyRegionScanner.ParseRegionSFile(firstFile);
            // Most files have at least 1 NPC
            Assert.GreaterOrEqual(entries.Count, 0, "Should parse without errors");
        }

        [Test]
        public void RegionSScanner_FindsTrainerAndTrainingObjectsAtPcCoords()
        {
            var folder = Path.Combine(Application.streamingAssetsPath, "TestData", "Regions", "Map_79");
            if (!Directory.Exists(folder))
            {
                Assert.Inconclusive($"Region_S folder not found: {folder}");
                return;
            }

            var spawns = BaLangEnemyRegionScanner.ScanRegionS(folder);
            var trainer = spawns.Single(s => s.templateId == 311 && s.nameRaw == "武师");
            Assert.AreEqual(53493, trainer.mpsX);
            Assert.AreEqual(95313, trainer.mpsY);
            var trainerWorld = BaLangEnemyDatabase.MpsToWorld(trainer.mpsX, trainer.mpsY);
            Assert.AreEqual(new Vector2(53493f, -47697f), trainerWorld);

            Assert.AreEqual(10, spawns.Count(s => s.templateId == 413 && s.nameRaw == "木桩"));
            Assert.AreEqual(10, spawns.Count(s => s.templateId == 414 && s.nameRaw == "木人"));
            Assert.AreEqual(10, spawns.Count(s => s.templateId == 415 && s.nameRaw == "沙袋"));
        }

        [Test]
        public void TrainingNpcSpawner_UsesPcStandSpritesForTrainingObjects()
        {
            var root = new GameObject("training-spawner-test");
            try
            {
                var spawner = root.AddComponent<TrainingNpcSpawner>();
                spawner.usePlayerPosition = false;
                spawner.centerX = 0f;
                spawner.centerY = 0f;
                spawner.radius = 10f;
                spawner.Spawn();

                var visuals = root.GetComponentsInChildren<PcNpcVisual>(true);
                Assert.AreEqual(5, visuals.Length);

                var expectedPaths = new[]
                {
                    @"spr\npcres\enemy\enemy178\enemy178_st.spr",
                    @"spr\npcres\enemy\enemy178\enemy178_st.spr",
                    @"spr\npcres\enemy\enemy179\enemy179_st.spr",
                    @"spr\npcres\enemy\enemy180\enemy180_st.spr",
                    @"spr\npcres\enemy\enemy180\enemy180_st.spr",
                }.OrderBy(p => p).ToArray();
                var actualPaths = visuals.Select(v => v.standSourcePath).OrderBy(p => p).ToArray();
                CollectionAssert.AreEqual(expectedPaths, actualPaths);

                foreach (var visual in visuals)
                {
                    Assert.AreEqual(visual.standSourcePath, visual.walkSourcePath);
                    Assert.IsTrue(visual.HasAnyClip, visual.standSourcePath);
                    var spriteRenderer = visual.transform.Find("NpcSprite")?.GetComponent<SpriteRenderer>();
                    Assert.IsNotNull(spriteRenderer, visual.standSourcePath);
                    Assert.IsNotNull(spriteRenderer.sprite, visual.standSourcePath);
                    Assert.IsNull(visual.transform.Find("Body"), "Training NPCs must not use the old one-frame Body placeholder.");
                    Assert.IsNull(visual.transform.Find("Shadow"), "Training NPCs must not use the old procedural Shadow placeholder.");
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void TrainingNpcSpawner_ExposesTrainingObjectsAsHighHpCombatTargets()
        {
            var root = new GameObject("training-target-test");
            try
            {
                var spawner = root.AddComponent<TrainingNpcSpawner>();
                spawner.usePlayerPosition = false;
                spawner.centerX = 0f;
                spawner.centerY = 0f;
                spawner.radius = 10f;
                spawner.Spawn();

                var enemies = spawner.GetActiveEnemies();
                Assert.AreEqual(5, enemies.Count);
                Assert.IsTrue(enemies.All(e => e.alive));
                Assert.IsTrue(enemies.All(e => e.enemyBehaviour != null));
                Assert.IsTrue(enemies.All(e => e.currentLife == e.maxLife));
                Assert.IsTrue(enemies.All(e => e.maxLife >= 999999));
                CollectionAssert.AreEquivalent(new[] { "Bao cát", "Bao cát", "Cọc gỗ", "Cọc gỗ", "Mộc nhân" },
                    enemies.Select(e => e.displayName).ToArray());
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EnemyAi_SetLifeWithDamageFlag_SpawnsRedPcDamageNumber()
        {
            var root = new GameObject("damage-popup-root");
            var enemy = new GameObject("damage-popup-enemy");
            enemy.transform.SetParent(root.transform, false);
            try
            {
                var ai = enemy.AddComponent<BaLangEnemyAi>();
                ai.Initialize(new NpcInstance
                {
                    instanceId = 1000,
                    template = new NpcTemplate
                    {
                        templateId = 413,
                        nameNormalized = "Cọc gỗ",
                        maxLife = 100,
                    },
                    worldPosition = Vector2.zero,
                }, null);

                ai.SetLife(62, showDamage: true);

                var popup = root.GetComponentInChildren<PcDamageNumber>(true);
                Assert.IsNotNull(popup);
                Assert.AreEqual(38, popup.Damage);

                var text = popup.GetComponent<TextMesh>();
                Assert.IsNotNull(text);
                Assert.AreEqual("38", text.text);
                Assert.Greater(text.color.r, 0.9f);
                Assert.Less(text.color.g, 0.2f);
                Assert.Less(text.color.b, 0.1f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Nameplate_HasThreeLayersAndUpdatesCurrentMaxHp()
        {
            var root = new GameObject("enemy-test");
            try
            {
                var template = BaLangEnemyDatabase.CreateTemplates().First(t => t.templateId == 31);
                root.AddComponent<BaLangEnemySpawnRuntime>();
                var plate = BaLangEnemySpawnRuntime.CreateNameplate(root.transform, "Vô hệ Mèo vàng", 100);
                Assert.IsTrue(plate.HasThreeLayers);
                plate.SetLife(58);
                Assert.AreEqual("58/100", plate.hpText.text);
                Assert.AreEqual(0.58f, plate.barFill.transform.localScale.x, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void NameplateOverlay_ComponentExistsForScreenReadableThreeLayerUi()
        {
            var go = new GameObject("overlay-test");
            try
            {
                var overlay = go.AddComponent<BaLangEnemyNameplateOverlay>();
                Assert.IsTrue(overlay.visible);
                Assert.Greater(overlay.maxDrawDistance, 0f);
                Assert.AreEqual(new Vector2(0f, -2f), overlay.screenOffset);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Ai_StaticTemplateStaysStill_WanderTemplateMovesTowardTarget()
        {
            var registry = new NpcTemplateRegistry();
            BaLangEnemyDatabase.RegisterAll(registry);

            var staticGo = new GameObject("static-ai");
            var movingGo = new GameObject("moving-ai");
            try
            {
                // Null template/static placeholder stays still.
                var staticAi = staticGo.AddComponent<BaLangEnemyAi>();
                staticAi.Initialize(new NpcInstance { instanceId = 1, template = null, worldPosition = Vector2.zero }, null);
                staticAi.Tick(1f, 10f);
                Assert.AreEqual(Vector3.zero, staticGo.transform.position);

                // Template 31 (Mèo vàng) has aiMode=1, walkSpeed=6 -> wandering
                movingGo.transform.position = Vector3.zero;
                var movingAi = movingGo.AddComponent<BaLangEnemyAi>();
                movingAi.Initialize(new NpcInstance { instanceId = 2, template = registry.Resolve(31), worldPosition = Vector2.zero }, null);
                for (int i = 0; i < 30; i++)
                    movingAi.Tick(0.2f, i * 0.2f + 1f);
                Assert.Greater(((Vector2)movingGo.transform.position).magnitude, 1f, "Wandering cat should move");
            }
            finally
            {
                Object.DestroyImmediate(staticGo);
                Object.DestroyImmediate(movingGo);
            }
        }

        private static bool ContainsChinese(string text)
        {
            foreach (var c in text)
                if (c >= 0x4e00 && c <= 0x9fff) return true;
            return false;
        }
    }
}
