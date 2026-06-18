using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;
using VLTK.UI;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// Tests for the combat skill slot, auto-target, and effect visual systems.
    /// Validates: skill assign, nearest enemy targeting, skill effect phases.
    /// </summary>
    public class CombatSkillSlotTests
    {
        [Test]
        public void AutoTarget_FindsNearestEnemy_InRange()
        {
            var service = new CombatAutoTargetService();
            var skill = new SkillDefinition
            {
                skillId = 117,
                attackRadius = 300,
                missileForm = SkillMissileForm.Single,
            };

            var enemies = new List<EnemyRuntimeInfo>
            {
                new() { enemyId = 1, position = new Vector2(100, 0), alive = true, currentLife = 50, maxLife = 50, displayName = "Kẻ địch 1" },
                new() { enemyId = 2, position = new Vector2(50, 0), alive = true, currentLife = 50, maxLife = 50, displayName = "Kẻ địch 2" },
                new() { enemyId = 3, position = new Vector2(200, 0), alive = true, currentLife = 50, maxLife = 50, displayName = "Kẻ địch 3" },
            };

            var result = service.FindNearestEnemy(Vector2.zero, skill, enemies);
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.enemyId); // enemy at 50 units is nearest
            Assert.AreEqual(50f, result.distance, 0.1f);
        }

        [Test]
        public void AutoTarget_ReturnsNull_WhenNoEnemyInRange()
        {
            var service = new CombatAutoTargetService();
            var skill = new SkillDefinition
            {
                skillId = 117,
                attackRadius = 30, // very short range
                missileForm = SkillMissileForm.Single,
            };

            var enemies = new List<EnemyRuntimeInfo>
            {
                new() { enemyId = 1, position = new Vector2(500, 0), alive = true, currentLife = 50, maxLife = 50 },
            };

            var result = service.FindNearestEnemy(Vector2.zero, skill, enemies);
            Assert.IsNull(result);
        }

        [Test]
        public void AutoTarget_SkipsDeadEnemies()
        {
            var service = new CombatAutoTargetService();
            var skill = new SkillDefinition { skillId = 117, attackRadius = 300, missileForm = SkillMissileForm.Single };

            var enemies = new List<EnemyRuntimeInfo>
            {
                new() { enemyId = 1, position = new Vector2(10, 0), alive = false, currentLife = 0, maxLife = 50 },
                new() { enemyId = 2, position = new Vector2(100, 0), alive = true, currentLife = 50, maxLife = 50 },
            };

            var result = service.FindNearestEnemy(Vector2.zero, skill, enemies);
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.enemyId); // skips dead enemy, finds alive one
        }

        [Test]
        public void AutoTarget_ReturnsNull_WhenNoEnemies()
        {
            var service = new CombatAutoTargetService();
            var skill = new SkillDefinition { skillId = 117, attackRadius = 300, missileForm = SkillMissileForm.Single };

            var result = service.FindNearestEnemy(Vector2.zero, skill, new List<EnemyRuntimeInfo>());
            Assert.IsNull(result);
        }

        [Test]
        public void Facing8Way_DirectionsAreCorrect()
        {
            // Test key directions
            // South (0,-1)
            Assert.AreEqual(0, CombatAutoTargetService.ComputeFacing8Way(Vector2.zero, new Vector2(0, -1)));
            // East (1,0)
            Assert.AreEqual(6, CombatAutoTargetService.ComputeFacing8Way(Vector2.zero, new Vector2(1, 0)));
            // North (0,1)
            Assert.AreEqual(4, CombatAutoTargetService.ComputeFacing8Way(Vector2.zero, new Vector2(0, 1)));
            // West (-1,0)
            Assert.AreEqual(2, CombatAutoTargetService.ComputeFacing8Way(Vector2.zero, new Vector2(-1, 0)));
        }

        [Test]
        public void SkillEffectVisual_PlayCast_StartsInPreCastPhase()
        {
            var service = new SkillEffectVisualService(null);
            var skill = new SkillDefinition
            {
                skillId = 128,
                nameNormalized = "Kháng Long Hữu Hối",
                attackRadius = 360,
                missileForm = SkillMissileForm.Single,
                timePerCast = 2,
            };

            var fx = service.PlaySkillCast(skill, Vector2.zero, new Vector2(100, 0), 1);
            Assert.IsNotNull(fx);
            Assert.AreEqual(SkillEffectPhase.PreCast, fx.phase);
            Assert.AreEqual(128, fx.skillId);
        }

        [Test]
        public void SkillEffectVisual_PassiveSkills_FinishImmediately()
        {
            var service = new SkillEffectVisualService(null);

            // Test passive: skill 115 (Cái Bang Bổng Pháp).
            // PC passive skills grant a permanent state but do not produce a visible missile cast.
            // The visual service should produce an effect object (so the call site can track it) and
            // clean it up promptly because there is no missile to animate.
            var passive = new SkillDefinition
            {
                skillId = 115,
                nameNormalized = "Cái Bang Bổng Pháp",
                skillStyle = PcSkillStyle.PassivityNpcState,
                missileForm = SkillMissileForm.None,
            };

            var fx = service.PlaySkillCast(passive, Vector2.zero, Vector2.zero, 1);
            Assert.IsNotNull(fx, "Visual service should produce a non-null effect object for passive skills.");
            // Passives do not spawn missiles → the effect must not be lingering after a few update ticks.
            for (int i = 0; i < 5; i++) service.Update(0.5f);
            Assert.AreEqual(0, service.ActiveEffectCount,
                "Passive skills (no missile) should not leave lingering effects in the active list.");
        }

        [Test]
        public void SkillEffectVisual_SurroundSkill_SpawnsMultipleMissiles()
        {
            // PC skill 125 (Thiên Hạ Vô Cẩu) — Cái Bang surround burst with 16 missiles.
            // Use the real catalog so the data-driven visual config resolves to the PC missile 47.
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
            var skill = catalog.Resolve(125);
            Assert.IsNotNull(skill, "Skill 125 should be in PC catalog.");

            var service = new SkillEffectVisualService(null, catalog);
            var fx = service.PlaySkillCast(skill, Vector2.zero, new Vector2(50, 0), 1);
            Assert.IsNotNull(fx);
            Assert.AreEqual(16, fx.missileCount, "PC skill 125 (Thiên Hạ Vô Cẩu) spawns 16 surround missiles.");
            Assert.IsNotNull(fx.missilePositions);
            Assert.AreEqual(16, fx.missilePositions.Length);
        }

        [Test]
        public void SkillEffectVisual_PreCastAdvancesToMissile()
        {
            var service = new SkillEffectVisualService(null);
            var skill = new SkillDefinition
            {
                skillId = 117,
                nameNormalized = "Ném Đá Hỏi Đường",
                attackRadius = 280,
                missileForm = SkillMissileForm.Single,
                timePerCast = 2,
            };

            var fx = service.PlaySkillCast(skill, Vector2.zero, new Vector2(100, 0), 1);
            Assert.AreEqual(SkillEffectPhase.PreCast, fx.phase);

            // Advance past PreCast duration
            service.Update(1f);
            Assert.AreEqual(SkillEffectPhase.Missile, fx.phase);
        }

        [Test]
        public void SkillEffectVisual_SingleMissileDoesNotImpactBeforeArrival()
        {
            var service = new SkillEffectVisualService(null);
            var skill = new SkillDefinition
            {
                skillId = 117,
                nameNormalized = "Ném Đá Hỏi Đường",
                attackRadius = 280,
                missileForm = SkillMissileForm.Single,
                timePerCast = 2,
            };

            var fx = service.PlaySkillCast(skill, Vector2.zero, new Vector2(100, 0), 1);

            service.Update(1f);
            Assert.AreEqual(SkillEffectPhase.Missile, fx.phase);

            service.Update(0.05f);
            Assert.AreEqual(SkillEffectPhase.Missile, fx.phase, "Single missiles should remain in-flight until their position reaches the target");
            Assert.Less(Vector2.Distance(fx.currentMissilePos, fx.targetPos), 100f);
            Assert.Greater(Vector2.Distance(fx.currentMissilePos, fx.targetPos), fx.arrivalRadius);
        }

        [Test]
        public void SkillEffectVisual_MissileAdvancesToImpact()
        {
            var service = new SkillEffectVisualService(null);
            var skill = new SkillDefinition
            {
                skillId = 117,
                nameNormalized = "Ném Đá Hỏi Đường",
                attackRadius = 280,
                missileForm = SkillMissileForm.Single,
                timePerCast = 2,
            };

            var fx = service.PlaySkillCast(skill, Vector2.zero, new Vector2(100, 0), 1);
            // Advance past PreCast + Missile
            service.Update(1f); // PreCast -> Missile
            service.Update(20f); // Missile -> Impact
            Assert.AreEqual(SkillEffectPhase.Impact, fx.phase);
        }

        [Test]
        public void SkillEffectVisual_FinishedEffectRemoved()
        {
            var service = new SkillEffectVisualService(null);
            var skill = new SkillDefinition
            {
                skillId = 117,
                nameNormalized = "Ném Đá Hỏi Đường",
                attackRadius = 280,
                missileForm = SkillMissileForm.Single,
                timePerCast = 2,
            };

            var fx = service.PlaySkillCast(skill, Vector2.zero, new Vector2(100, 0), 1);
            // Advance through all phases
            service.Update(1f);  // PreCast -> Missile
            service.Update(20f); // Missile -> Impact
            service.Update(2f);  // Impact -> Finished + cleanup (PC mag_bz_tu1_爆炸效果 is 16 frames × 2 ticks)
            Assert.AreEqual(0, service.ActiveEffectCount);
        }

        [Test]
        public void CaiBangSkill_AllActiveSkillsHaveCorrectVisuals()
        {
            // Verify each Cái Bang active combat skill gets a PC visual assignment from the catalog.
            // PC source skills (PC gaibang.lua):
            // 117 Đả Cẩu (棍击) — single missile 7
            // 119 Phi Long Hữu Hối — missile 25
            // 122 Bổng Đả — missile 46
            // 125 Thiên Hạ Vô Cẩu — surround 16 missiles 47
            // 128 Vân Khởi Tụ Phong — missile 166
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
            var service = new SkillEffectVisualService(null, catalog);
            int[] activeSkills = { 117, 119, 122, 125, 128 };

            var spriteKeys = new HashSet<string>();
            foreach (var id in activeSkills)
            {
                var skill = catalog.Resolve(id);
                Assert.IsNotNull(skill, $"Skill {id} should exist in PC catalog.");

                var fx = service.PlaySkillCast(skill, Vector2.zero, new Vector2(100, 0), 1);
                Assert.IsNotNull(fx, $"Skill {id} should produce an effect");
                Assert.IsTrue(fx.HasPcMissileSprite, $"Skill {id} should use PC missile SPR (key={fx.pcMissileSpriteKey})");
                spriteKeys.Add(fx.pcMissileSpriteKey);
            }

            // Each active damage skill uses a distinct PC missile SPR.
            Assert.AreEqual(activeSkills.Length, spriteKeys.Count, "Each active skill should have a unique PC missile sprite");
        }
        [Test]
        public void VisualService_PlayHitFlash_CreatesImpactOnlyEffectAndExpires()
        {
            var service = new SkillEffectVisualService(null);
            var fx = service.PlayHitFlash(new Vector2(10, 20), Color.red, 0.2f);

            Assert.IsNotNull(fx);
            Assert.IsTrue(fx.isHitFlash);
            Assert.AreEqual(SkillEffectPhase.Impact, fx.phase);
            Assert.AreEqual(new Vector2(10, 20), fx.targetPos);
            Assert.AreEqual(1, service.ActiveEffectCount);

            service.Update(0.25f);
            Assert.AreEqual(0, service.ActiveEffectCount);
        }

        [Test]
        public void VisualService_PlayBuffAura_CreatesAuraAndExpiresWithoutImpact()
        {
            var service = new SkillEffectVisualService(null);
            var fx = service.PlayBuffAura(new Vector2(5, 6), Color.cyan, 0.3f, 72f, "Hộ Thể");

            Assert.IsNotNull(fx);
            Assert.IsTrue(fx.isAura);
            Assert.AreEqual("Hộ Thể", fx.skillName);
            Assert.AreEqual(SkillEffectPhase.PreCast, fx.phase);
            Assert.AreEqual(72f, fx.auraRadius);
            Assert.AreEqual(1, service.ActiveEffectCount);

            service.Update(0.15f);
            Assert.AreEqual(1, service.ActiveEffectCount);
            Assert.AreEqual(SkillEffectPhase.PreCast, fx.phase);

            service.Update(0.2f);
            Assert.AreEqual(0, service.ActiveEffectCount);
        }

        [Test]
        public void GetDefaultSkillsForFaction_ReturnsCorrectSkillsForAllFactions()
        {
            // PC source-derived default deck per faction. PC gốc JX: 1 ô là skill tấn công cơ bản
            // của phái, các ô còn lại là skill cao cấp / chiêu thức đặc trưng.
            // Cái Bang uses explicit per-faction default deck (PC gaibang.lua):
            // Phi Long (357) → Thiên Hạ Vô Cẩu (359) → Túy Điệp Cuồng Vũ (130) → Kháng Long Hữu Hối (358) → Hoạt Bất Lưu Thủ (127).
            // Other factions use the first 5 entries from PcSkillPanelService.GetPcSkillOrder(faction) directly.
            // (Cái Bang's per-faction default deck is tested separately below.)
            var factions = new[]
            {
                new { faction = CombatFaction.WuDang, slot0 = 151, slot1 = 152, slot2 = 153, slot3 = 154, slot4 = 155 },
                new { faction = CombatFaction.Shaolin, slot0 = 3, slot1 = 4, slot2 = 6, slot3 = 8, slot4 = 9 },
                new { faction = CombatFaction.TangMen, slot0 = 43, slot1 = 45, slot2 = 47, slot3 = 48, slot4 = 50 },
                new { faction = CombatFaction.EMei, slot0 = 77, slot1 = 79, slot2 = 80, slot3 = 81, slot4 = 82 },
                new { faction = CombatFaction.TianWang, slot0 = 23, slot1 = 24, slot2 = 26, slot3 = 29, slot4 = 30 },
                new { faction = CombatFaction.WuDu, slot0 = 60, slot1 = 62, slot2 = 63, slot3 = 64, slot4 = 65 },
                new { faction = CombatFaction.CuiYan, slot0 = 95, slot1 = 97, slot2 = 99, slot3 = 100, slot4 = 101 },
                new { faction = CombatFaction.TianRen, slot0 = 131, slot1 = 132, slot2 = 135, slot3 = 136, slot4 = 137 },
                new { faction = CombatFaction.KunLun, slot0 = 167, slot1 = 168, slot2 = 169, slot3 = 170, slot4 = 171 },
            };

            Assert.AreEqual(5, CombatSkillSlotController.MobileSkillSlotCount,
                "Mobile uses 5-slot deck (PC JX default 5 combat skills per faction).");

            // Cái Bang: explicit per-faction default deck via CombatSkillSlotController.DefaultDeckByFaction.
            var caiBangDeckField = typeof(CombatSkillSlotController)
                .GetField("DefaultDeckByFaction", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(caiBangDeckField, "CombatSkillSlotController.DefaultDeckByFaction must exist.");
            var deckMap = (System.Collections.Generic.Dictionary<CombatFaction, int[]>)caiBangDeckField.GetValue(null);
            CollectionAssert.AreEqual(new[] { 357, 358, 1073, 130, 127 }, deckMap[CombatFaction.CaiBang],
                "Cái Bang default deck: Phi Long → Kháng Long → Thần Thủ Lệnh Long → Túy Điệp Cuồng Vũ → Hoạt Bất Lưu Thủ");
            foreach (var f in factions)
            {
                var order = PcSkillPanelService.GetPcSkillOrder(f.faction);
                CollectionAssert.AreEqual(
                    new[] { f.slot0, f.slot1, f.slot2, f.slot3, f.slot4 },
                    order.Take(5).ToArray(),
                    $"PC skill order[0..4] mismatch for {f.faction}");
            }
        }

        [Test]
        public void CreateCombatActor_UsesPlayerFactionAndComputesCorrectMana()
        {
            typeof(SandboxManager).GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?
                .GetSetMethod(true)?.Invoke(null, new object[] { null });

            var go = new GameObject("Test");
            var controller = go.AddComponent<CombatSkillSlotController>();
            
            var progression = new PlayerProgressionState();
            progression.faction = CombatFaction.WuDang;
            progression.level = 100;
            progression.knownSkills.Add(153);
            progression.skillLevels[153] = 10;

            typeof(CombatSkillSlotController).GetField("_progression", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(controller, progression);
            
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
            typeof(CombatSkillSlotController).GetField("_catalog", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(controller, catalog);

            var skill = catalog.Resolve(153);
            var playerGo = new GameObject("Player");
            var playerController = playerGo.AddComponent<SandboxPlayerController>();

            try
            {
                var method = typeof(CombatSkillSlotController).GetMethod("CreateCombatActor", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Assert.IsNotNull(method);

                var actor = method.Invoke(controller, new object[] { playerController, skill }) as CombatActorState;
                Assert.IsNotNull(actor);
                Assert.AreEqual(CombatFaction.WuDang, actor.faction);
                Assert.AreEqual(100, actor.level);
                Assert.AreEqual(PcMaxManaFormula.Compute(100, 0, CombatFaction.WuDang), actor.currentMana);
                Assert.IsTrue(actor.knownSkills.Contains(153));
                Assert.AreEqual(10, actor.skillLevels[153]);
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(playerGo);
            }
        }

        [Test]
        public void MobileDeck_AssignsFourSlotsAndSwitchesIndependentDecks()
        {
            var go = new GameObject("CombatDeckTest");
            var controller = go.AddComponent<CombatSkillSlotController>();
            try
            {
                controller.AssignSkill(0, 357);
                controller.AssignSkill(1, 359);
                controller.AssignSkill(2, 117);
                controller.AssignSkill(3, 128);

                Assert.AreEqual(357, controller.GetAssignedSkill(0));
                Assert.AreEqual(128, controller.GetAssignedSkill(3));
                Assert.AreEqual(357, controller.LeftSkillId, "legacy left slot should mirror deck A slot 0");
                Assert.AreEqual(359, controller.RightSkillId, "legacy right slot should mirror deck A slot 1");

                controller.ToggleDeck();
                Assert.AreEqual(1, controller.ActiveDeckIndex);
                Assert.AreEqual(0, controller.GetAssignedSkill(0));
                controller.AssignSkill(0, 153);
                Assert.AreEqual(153, controller.GetAssignedSkill(0));

                controller.ToggleDeck();
                Assert.AreEqual(357, controller.GetAssignedSkill(0), "deck A assignment must survive deck B edits");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void PrimaryAttack_UsesSlotZeroThenFirstAssignedSkill()
        {
            var go = new GameObject("PrimaryAttackSlotTest");
            var controller = go.AddComponent<CombatSkillSlotController>();
            try
            {
                Assert.AreEqual(-1, controller.ResolvePrimaryAttackSlot());
                controller.AssignSkill(2, 117);
                Assert.AreEqual(2, controller.ResolvePrimaryAttackSlot());
                controller.AssignSkill(0, 357);
                Assert.AreEqual(0, controller.ResolvePrimaryAttackSlot());
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TargetLock_CanLockAndClearWithoutPhysicsScan()
        {
            var go = new GameObject("TargetLockTest");
            var controller = go.AddComponent<CombatSkillSlotController>();
            try
            {
                controller.LockTarget(42, "Cọc gỗ");
                Assert.AreEqual(42, controller.LockedTargetId);
                controller.ClearTargetLock();
                Assert.AreEqual(-1, controller.LockedTargetId);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CombatRuntime_PlayerActor_BypassesManaCostAndSucceeds()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCaiBangCatalog();
            var svc = new CombatRuntimeService(catalog);
            
            // Create a player actor with actorId = 1, currentMana = 10 (very low)
            var player = new CombatActorState
            {
                actorId = 1, // Player Actor ID
                faction = CombatFaction.CaiBang,
                level = 60,
                fightMode = true,
                currentMana = 10,
                position = Vector2.zero,
                knownSkills = { 117 },
                skillLevels = { [117] = 20 }
            };
            
            var enemy = new CombatActorState
            {
                actorId = 2,
                currentLife = 1000,
                maxLife = 1000,
                position = new Vector2(10, 0)
            };
            
            var report = svc.Cast(player, enemy, 117, enemy.position, CombatRelation.Enemy);
            
            // The cast should succeed, and mana cost should be set to 0, and currentMana should remain 10
            Assert.IsTrue(report.success, $"Cast failed: {report.reason} - {report.detail}");
            Assert.AreEqual(0, report.manaCost);
            Assert.AreEqual(10, player.currentMana);
        }

        [Test]
        public void CombatRuntime_BuffStates_TickDownAndExpire()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCaiBangCatalog();
            var loop = new GameplayLoopService(catalog);
            var player = loop.RegisterPlayer(1, "TestPlayer", 60, Vector2.zero);
            
            // Manually add a temporary state with duration of 180 ticks (10 seconds)
            player.combat.states[MagicAttributeKind.FastWalkRunP] = new SkillMagicAttribute(MagicAttributeKind.FastWalkRunP, 66, 180, 0);
            
            // Manually add a permanent state with duration of -1 (should not expire)
            player.combat.states[MagicAttributeKind.MeleeDamageReturnP] = new SkillMagicAttribute(MagicAttributeKind.MeleeDamageReturnP, 20, -1, 0);
            
            // Advance time by 5 seconds (90 ticks)
            loop.Tick(5f);
            
            // FastWalkRunP should still be active, with duration reduced by 90
            Assert.IsTrue(player.combat.states.ContainsKey(MagicAttributeKind.FastWalkRunP));
            Assert.AreEqual(90, player.combat.states[MagicAttributeKind.FastWalkRunP].value2);
            
            // MeleeDamageReturnP should still be active, duration unchanged (-1)
            Assert.IsTrue(player.combat.states.ContainsKey(MagicAttributeKind.MeleeDamageReturnP));
            Assert.AreEqual(-1, player.combat.states[MagicAttributeKind.MeleeDamageReturnP].value2);
            
            // Advance time by another 6 seconds (108 ticks, total 198 ticks)
            loop.Tick(6f);
            
            // FastWalkRunP should have expired and been removed
            Assert.IsFalse(player.combat.states.ContainsKey(MagicAttributeKind.FastWalkRunP));
            
            // MeleeDamageReturnP should still remain
            Assert.IsTrue(player.combat.states.ContainsKey(MagicAttributeKind.MeleeDamageReturnP));
        }

        [Test]
        public void CombatRuntime_BuffStates_ApplyAddedDamageAndResistances()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCaiBangCatalog();
            var svc = new CombatRuntimeService(catalog);
            
            var player = new CombatActorState
            {
                actorId = 1,
                faction = CombatFaction.CaiBang,
                level = 60,
                fightMode = true,
                position = Vector2.zero,
                knownSkills = { 117 },
                skillLevels = { [117] = 20 }
            };
            
            // Add a fire damage buff to the player
            player.states[MagicAttributeKind.FireDamageV] = new SkillMagicAttribute(MagicAttributeKind.FireDamageV, 50, 180, 0);
            
            var enemy = new CombatActorState
            {
                actorId = 2,
                currentLife = 1000,
                maxLife = 1000,
                position = new Vector2(10, 0)
            };
            
            // Case 1: Enemy has no resistances
            var report1 = svc.Cast(player, enemy, 117, enemy.position, CombatRelation.Enemy);
            Assert.IsTrue(report1.success);
            int lifeAfterCast1 = enemy.currentLife;
            
            // Bypass cooldown
            svc.AdvanceTime(100);
            
            // Reset enemy HP
            enemy.currentLife = 1000;
            
            // Case 2: Enemy has AllResP active
            enemy.states[MagicAttributeKind.AllResP] = new SkillMagicAttribute(MagicAttributeKind.AllResP, 30, -1, 0); // 30% resist
            var report2 = svc.Cast(player, enemy, 117, enemy.position, CombatRelation.Enemy);
            Assert.IsTrue(report2.success);
            int lifeAfterCast2 = enemy.currentLife;
            
            // Target life after cast 2 should be higher because of the 30% resistance mitigation
            int damage1 = 1000 - lifeAfterCast1;
            int damage2 = 1000 - lifeAfterCast2;
            Assert.Less(damage2, damage1, $"Resisted damage ({damage2}) should be less than unresisted damage ({damage1})");
        }

        [Test]
        public void VisualService_PlayStateAura_ConfiguresAuraPermanently()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCaiBangCatalog();
            var service = new SkillEffectVisualService(null, catalog);
            var skill = catalog.Resolve(130); // Túy Điệp Cuồng Vũ
            
            Assert.IsNotNull(skill);
            Assert.AreEqual(43, skill.stateSpecialId);
            
            var fx = service.PlaySkillCast(skill, Vector2.zero, Vector2.zero, 1);
            Assert.IsNotNull(fx);
            Assert.IsTrue(fx.isAura);
            Assert.AreEqual(float.MaxValue, fx.auraDuration);
            Assert.AreEqual(float.MaxValue, fx.preCastDuration);
            Assert.AreEqual("\\spr\\skill\\丐帮\\mag_gb_11_醉蝶狂舞.spr", fx.pcPreCastSpriteKey);
        }
    }
}
