using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    // Acceptance contract for PC gaibang.lua::feilong_zaitian and missile 166.
    [TestFixture, Category("CaiBang")]
    public class CaiBangPhiLongCollisionAcceptanceTests
    {
        private static SkillCatalog Catalog() => TestCatalogCache.NoviceAndCaiBang;

        [Test]
        public void PhiLong_L20_CastDefersAllLongChienDamageUntilEachDragonCollides()
        {
            var damage = new DamageFormulaService { RollPercent = _ => true };
            var service = new CombatRuntimeService(Catalog(), damage: damage);
            var caster = new CombatActorState
            {
                actorId = 71,
                faction = CombatFaction.CaiBang,
                currentMana = 500,
                position = Vector2.zero,
                knownSkills = { 357 },
                skillLevels = { [357] = 20 },
            };
            var target = new CombatActorState
            {
                actorId = 72,
                faction = CombatFaction.None,
                currentLife = 10000,
                position = new Vector2(400f, 0f),
            };

            var report = service.Cast(caster, target, 357, target.position, CombatRelation.Enemy);

            Assert.IsTrue(report.success, report.detail);
            Assert.AreEqual(0, report.damageResults.Count,
                "The L20 cast has four travelling dragons, but 389 must not damage until each dragon collides.");
            Assert.AreEqual(4, report.projectiles.Count,
                "PC gaibang.lua skill_misslenum_v requires four L20 missile 166 instances.");
            Assert.IsTrue(report.projectiles.TrueForAll(projectile => projectile.skillId == 166),
                "Only Phi Long's travelling missile 166 may exist before collision.");
        }

        [Test]
        public void PhiLong_L20_UsesPcHomingLanesAndRecordsOneImpactPerDragon()
        {
            var visual = new SkillEffectVisualService(null, Catalog());
            var fx = visual.PlaySkillCast(Catalog().Resolve(357), Vector2.zero, new Vector2(400f, 0f), 20,
                () => new Vector2(400f, 0f));

            Assert.AreEqual(4, fx.missileCount, "PC Lua L20 missile count is four.");
            Assert.AreEqual(5, fx.pcMissileMoveKind, "PC missile 166 MoveKind=5 is target-tracking.");
            Assert.AreEqual(24, fx.pcMissileSpeedPerTick,
                "PC gaibang.lua L20 missle_speed_v=24 overrides missile 166's raw Speed=30.");
            Assert.AreEqual(24, fx.pcMissileLifeTicks, "PC missile 166 LifeTime=24.");
            Assert.AreEqual(32f, Vector2.Distance(fx.missileTargetOffsets[0], fx.missileTargetOffsets[1]), 0.001f,
                "PC skills.txt Param1=32 is the spacing between adjacent homing lanes.");

            // First update completes precast; the second advances all four dragons to collision.
            visual.Update(fx.preCastDuration);
            visual.Update(1f);

            Assert.That(fx.rendPositions, Has.Count.EqualTo(4),
                "Every missile 166 collision must independently produce Phi Long's impact/collide event.");
            Assert.That(fx.missileExplodeStartTime, Has.All.GreaterThanOrEqualTo(0f),
                "Each individual impact must retain its own explosion start time.");
        }

        [Test]
        public void PhiLong_L20_PerEffectCollisionCallbackFiresOnceForEachDragonInOrder()
        {
            var visual = new SkillEffectVisualService(null, Catalog());
            var collisionIndexes = new System.Collections.Generic.List<int>();
            var fx = visual.PlaySkillCast(Catalog().Resolve(357), Vector2.zero, new Vector2(400f, 0f), 20,
                () => new Vector2(400f, 0f),
                (_, missileIndex, _) => collisionIndexes.Add(missileIndex));

            visual.Update(fx.preCastDuration);
            visual.Update(1f);

            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, collisionIndexes);
        }

        [Test]
        public void PhiLong_L20_PlaysFlightAndImpactStatusSoundsForEachDragon()
        {
            var visual = new SkillEffectVisualService(null, Catalog());
            var sounds = new System.Collections.Generic.List<string>();
            visual.OnCastSound = sounds.Add;
            var fx = visual.PlaySkillCast(Catalog().Resolve(357), Vector2.zero, new Vector2(400f, 0f), 20,
                () => new Vector2(400f, 0f));

            Assert.That(sounds, Has.Count.EqualTo(1), "Skills.txt ManCastSnd plays once at the cast frame.");
            Assert.That(sounds[0], Does.EndWith("sound_k005.wav"));

            sounds.Clear();
            visual.Update(fx.preCastDuration);
            Assert.That(sounds, Has.Count.EqualTo(4),
                "PC KMissle plays SndFile2/MS_DoFly once for each of the four L20 missiles.");
            Assert.That(sounds, Has.All.EndsWith("亢龙无悔.wav"));

            sounds.Clear();
            visual.Update(1f);
            Assert.That(sounds, Has.Count.EqualTo(4),
                "Each missile collision plays SndFile4/MS_DoCollision independently.");
            Assert.That(sounds, Has.All.EndsWith("sound_k037.wav"));
        }

        [Test]
        public void PhiLong_Missile166_PreservesDistinctFlightAndImpactSpritesAndSounds()
        {
            var registry = PcMissileFullVisualRegistry.ParseFromFile(
                System.IO.Path.Combine(Application.streamingAssetsPath, "Reference", "PcMissles.txt"));

            Assert.IsTrue(registry.TryGet(166, out var missile), "PC missile 166 must be present.");
            Assert.AreEqual(5, missile.moveKind);
            Assert.AreEqual(30, missile.speed);
            Assert.AreEqual(24, missile.lifetime);
            Assert.IsNotNull(missile.PrimaryFlight);
            Assert.IsNotNull(missile.PrimaryCollision);
            Assert.IsNotEmpty(missile.PrimaryFlight.sprPath, "166 requires its travelling dragon SPR.");
            Assert.IsNotEmpty(missile.PrimaryCollision.sprPath, "166 requires its collision burst SPR.");
            Assert.IsNotEmpty(missile.PrimaryFlight.soundPath, "166 requires a flight SFX.");
            Assert.That(missile.PrimaryCollision.soundPath, Does.EndWith("sound_k037.wav"),
                "166 impact SFX must be PC SndFile4 sound_k037.wav.");
            Assert.AreNotEqual(missile.PrimaryFlight.soundPath, missile.PrimaryCollision.soundPath,
                "Flight and impact audio are separate PC animation-slot events.");
        }
    }
}
