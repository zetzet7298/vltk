using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

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

            // Test passive: skill 115 (Cái Bang Bổng Pháp)
            var passive = new SkillDefinition
            {
                skillId = 115,
                nameNormalized = "Cái Bang Bổng Pháp",
                skillStyle = PcSkillStyle.PassivityNpcState,
                missileForm = SkillMissileForm.None,
            };

            var fx = service.PlaySkillCast(passive, Vector2.zero, Vector2.zero, 1);
            Assert.IsNotNull(fx);
            Assert.AreEqual(SkillEffectPhase.Finished, fx.phase);
        }

        [Test]
        public void SkillEffectVisual_SurroundSkill_SpawnsMultipleMissiles()
        {
            var service = new SkillEffectVisualService(null);
            var skill = new SkillDefinition
            {
                skillId = 125,
                nameNormalized = "Thiên Hạ Vô Cẩu",
                attackRadius = 400,
                missileForm = SkillMissileForm.Surround,
                missilesGenerateData = 5,
            };

            var fx = service.PlaySkillCast(skill, Vector2.zero, new Vector2(50, 0), 1);
            Assert.IsNotNull(fx);
            Assert.AreEqual(16, fx.missileCount);
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
            // Verify each CaiBang active combat skill gets a PC visual assignment
            var service = new SkillEffectVisualService(null);
            int[] activeSkills = { 117, 119, 122, 125, 128 };

            var spriteKeys = new HashSet<string>();
            foreach (var id in activeSkills)
            {
                var skill = new SkillDefinition
                {
                    skillId = id,
                    nameNormalized = $"Skill_{id}",
                    attackRadius = 300,
                    missileForm = id == 125 ? SkillMissileForm.Surround : SkillMissileForm.Single,
                };

                var fx = service.PlaySkillCast(skill, Vector2.zero, new Vector2(100, 0), 1);
                Assert.IsNotNull(fx, $"Skill {id} should produce an effect");
                Assert.IsTrue(fx.HasPcMissileSprite, $"Skill {id} should use PC missile SPR");
                spriteKeys.Add(fx.pcMissileSpriteKey);
            }

            // Each active damage skill uses a distinct PC missile SPR.
            Assert.AreEqual(activeSkills.Length, spriteKeys.Count, "Each active skill should have a unique PC missile sprite");
        }
    }
}
