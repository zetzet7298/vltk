// -----------------------------------------------------------------------------
// VLTK Mobile — ST-04.2 / ST-04.3 Combat System Tests
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class CombatSystemTests
    {
        [Test]
        public void ElementRelation_KimKhacMoc_IncreasesDamage()
        {
            // Faction 1 = Shaolin (Kim), Faction 3 = TangMen (Mộc)
            float mult = PcSkillDamageService.GetElementRelationMultiplier(
                CombatFactionExt.ShaolinId, CombatFactionExt.TangMenId);

            Assert.AreEqual(1.3f, mult, 0.01f); // 30% bonus
        }

        [Test]
        public void ElementRelation_MocBiKhacKim_DecreasesDamage()
        {
            // Faction 3 = TangMen (Mộc), Faction 1 = Shaolin (Kim)
            float mult = PcSkillDamageService.GetElementRelationMultiplier(
                CombatFactionExt.TangMenId, CombatFactionExt.ShaolinId);

            Assert.AreEqual(0.7f, mult, 0.01f); // 30% reduction
        }

        [Test]
        public void ElementRelation_Neutral_NoMultiplier()
        {
            float mult = PcSkillDamageService.GetElementRelationMultiplier(
                CombatFactionExt.ShaolinId, CombatFactionExt.WuDangId); // Kim and Thổ -> no direct counter in JX1

            Assert.AreEqual(1.0f, mult, 0.01f);
        }

        [Test]
        public void CombatReflection_CalculatesCorrectMeleeReturn()
        {
            // Melee damage = 100, reflect = 20%
            int reflected = CombatReflectionService.ApplyReflection(
                finalDamage: 100,
                reflectPercent: 20,
                attackerCurrentHp: 50,
                isMelee: true
            );

            Assert.AreEqual(20, reflected);
        }

        [Test]
        public void CombatReflection_CappedAtAttackerHpMinusOne()
        {
            // Melee damage = 200, reflect = 50% -> reflected = 100.
            // Attacker has 50 HP -> reflected capped at 49 to prevent instant suicide.
            int reflected = CombatReflectionService.ApplyReflection(
                finalDamage: 200,
                reflectPercent: 50,
                attackerCurrentHp: 50,
                isMelee: true
            );

            Assert.AreEqual(49, reflected);
        }

        [Test]
        public void BuffStateService_AppliesAndTicksBuff()
        {
            var service = new BuffStateService();
            var skill = new SkillDefinition
            {
                skillId = 20,
                nameRaw = "Sư Tử Hống",
                stateSpecialId = 22
            };

            Assert.IsFalse(service.HasBuff(1, 20));

            // Apply 3 seconds buff
            service.ApplyBuff(1, skill, 1, 3.0f);
            Assert.IsTrue(service.HasBuff(1, 20));

            // Tick 2 seconds
            service.Tick(2.0f);
            Assert.IsTrue(service.HasBuff(1, 20));

            // Tick 1.1 seconds (expired)
            service.Tick(1.1f);
            Assert.IsFalse(service.HasBuff(1, 20));
        }

        [Test]
        public void AutoTarget_FindsNearestValidEnemy()
        {
            var playerPos = Vector2.zero;
            var actors = new List<CombatActorState>
            {
                new() { actorId = 2, position = new Vector2(100, 0), currentLife = 100 }, // Far
                new() { actorId = 3, position = new Vector2(20, 0), currentLife = 100 },  // Near
                new() { actorId = 4, position = new Vector2(5, 0), currentLife = 0 }      // Dead (nearest but invalid)
            };

            var target = AutoTargetService.FindBestTarget(playerPos, 50f, actors);

            Assert.IsNotNull(target);
            Assert.AreEqual(3, target.actorId);
        }

        [Test]
        public void AutoTarget_CycleTarget_LoopsCorrectly()
        {
            var playerPos = Vector2.zero;
            var actors = new List<CombatActorState>
            {
                new() { actorId = 2, position = new Vector2(10, 0), currentLife = 100 },
                new() { actorId = 3, position = new Vector2(15, 0), currentLife = 100 }
            };

            // First target is 2. Cycle next should return 3.
            var next = AutoTargetService.CycleTarget(playerPos, 50f, actors, currentTargetId: 2);
            Assert.AreEqual(3, next.actorId);

            // Cycle from 3 should wrap back to 2.
            var wrap = AutoTargetService.CycleTarget(playerPos, 50f, actors, currentTargetId: 3);
            Assert.AreEqual(2, wrap.actorId);
        }
    }
}
