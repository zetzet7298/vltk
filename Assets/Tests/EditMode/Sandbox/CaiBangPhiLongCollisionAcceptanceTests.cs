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
        public void PhiLong_L20_UsesPcWallOriginsAndOneLiveHomingTarget()
        {
            var visual = new SkillEffectVisualService(null, Catalog());
            var liveTarget = new Vector2(400f, 0f);
            var fx = visual.PlaySkillCast(Catalog().Resolve(357), Vector2.zero, liveTarget, 20,
                () => liveTarget);

            Assert.AreEqual(4, fx.missileCount, "PC Lua L20 missile count is four.");
            Assert.AreEqual(5, fx.pcMissileMoveKind, "PC missile 166 MoveKind=5 is target-tracking.");
            Assert.AreEqual(24, fx.pcMissileSpeedPerTick,
                "PC gaibang.lua L20 missle_speed_v=24 overrides missile 166's raw Speed=30.");
            Assert.AreEqual(24, fx.pcMissileLifeTicks, "PC missile 166 LifeTime=24.");
            float[] expectedOffsets = { -64f, -32f, 0f, 32f };
            for (int i = 0; i < fx.missileCount; i++)
            {
                Assert.AreEqual(0f, fx.missileOrigins[i].x, 0.001f);
                Assert.AreEqual(expectedOffsets[i], fx.missileOrigins[i].y, 0.001f,
                    $"PC CastWall origin {i} must use -Param1*count/2 + Param1*i.");
                Assert.AreEqual(liveTarget, fx.ResolveMissileTarget(i),
                    "All four follow missiles must chase the same NPC center, not four offset targets.");
                Assert.Greater(Vector2.Dot(Vector2.right, fx.ResolveMissileDirection(i)), 0.999f,
                    "Param2 != 0 makes all four missiles initially face the cast target.");
            }
        }

        [Test]
        public void PhiLong_L20_RetargetsOnNinthPcTickAndThenConverges()
        {
            var visual = new SkillEffectVisualService(null, Catalog());
            var liveTarget = new Vector2(400f, 0f);
            var fx = visual.PlaySkillCast(Catalog().Resolve(357), Vector2.zero, liveTarget, 20,
                () => liveTarget);

            visual.Update(fx.preCastDuration);
            liveTarget = new Vector2(0f, 400f);
            for (int tick = 0; tick < 8; tick++)
                visual.Update(1f / 18f);

            float spreadBeforeRetarget = fx.missilePositions[3].y - fx.missilePositions[0].y;
            for (int i = 0; i < fx.missileCount; i++)
            {
                Assert.Greater(Vector2.Dot(Vector2.right, fx.ResolveMissileDirection(i)), 0.999f,
                    $"Missile {i} must preserve its initial direction for PC ticks 1-8.");
            }

            visual.Update(1f / 18f);

            float spreadAfterRetarget = fx.missilePositions[3].y - fx.missilePositions[0].y;
            for (int i = 0; i < fx.missileCount; i++)
            {
                Vector2 direction = fx.ResolveMissileDirection(i);
                Assert.Less(direction.x, 0f, $"Missile {i} must turn west toward the moved target on tick 9.");
                Assert.Greater(direction.y, 0f, $"Missile {i} must turn north toward the moved target on tick 9.");
            }
            Assert.Less(spreadAfterRetarget, spreadBeforeRetarget,
                "Once all four missiles retarget the same center, their wall formation must converge.");
        }

        [Test]
        public void PhiLong_L20_RecordsOneImpactPerDragonAndStopsArrivedMissiles()
        {
            var visual = new SkillEffectVisualService(null, Catalog());
            var fx = visual.PlaySkillCast(Catalog().Resolve(357), Vector2.zero, new Vector2(400f, 0f), 20,
                () => new Vector2(400f, 0f));

            // First update completes precast; the second advances all four dragons to collision.
            visual.Update(fx.preCastDuration);
            visual.Update(1f);

            Assert.That(fx.rendPositions, Has.Count.EqualTo(4),
                "Every missile 166 collision must independently produce Phi Long's impact/collide event.");
            Assert.That(fx.missileExplodeStartTime, Has.All.GreaterThanOrEqualTo(0f),
                "Each individual impact must retain its own explosion start time.");

            var collidedPositions = (Vector2[])fx.missilePositions.Clone();
            visual.Update(0.25f);
            for (int i = 0; i < fx.missileCount; i++)
                Assert.AreEqual(collidedPositions[i], fx.missilePositions[i],
                    $"Missile {i} must stop moving as soon as its collision event fires.");
        }

        [Test]
        public void PhiLong_L20_PerEffectCollisionCallbackFiresOnceForEachDragonInOrder()
        {
            var visual = new SkillEffectVisualService(null, Catalog());
            var collisionIndexes = new System.Collections.Generic.List<int>();
            var popupRoot = new GameObject("phi-long-damage-popups");
            try
            {
                var fx = visual.PlaySkillCast(Catalog().Resolve(357), Vector2.zero, new Vector2(400f, 0f), 20,
                    () => new Vector2(400f, 0f),
                    (_, missileIndex, position) =>
                    {
                        collisionIndexes.Add(missileIndex);
                        PcDamageNumber.Spawn(position, 100 + missileIndex, popupRoot.transform);
                    });

                visual.Update(fx.preCastDuration);
                visual.Update(1f);

                CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, collisionIndexes);
                var popups = popupRoot.GetComponentsInChildren<PcDamageNumber>(true);
                Assert.AreEqual(4, popups.Length, "All four collision callbacks must finish without a damage-popup exception.");
                foreach (var popup in popups)
                {
                    Assert.AreEqual(5, popup.GetComponentsInChildren<TextMesh>(true).Length,
                        "Each damage popup requires one main TextMesh and four outline shadows.");
                }
            }
            finally
            {
                Object.DestroyImmediate(popupRoot);
            }
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
