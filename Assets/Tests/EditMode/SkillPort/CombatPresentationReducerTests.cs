using System.Collections.Generic;
using NUnit.Framework;
using VLTK.SkillPort;

namespace VLTK.Tests.SkillPort
{
    [Category("SkillPort")]
    public class CombatPresentationReducerTests
    {
        private static CombatPresentationReducer NewReducer(ulong sequence = 10)
        {
            var reducer = new CombatPresentationReducer();
            Assert.IsTrue(reducer.ApplySnapshot(new CombatPresentationSnapshot
            {
                serverSequence = sequence,
                baselineTick = 100,
            }));
            return reducer;
        }

        [Test]
        public void Reducer_RequiresContiguousSequenceAndDeduplicatesEventId()
        {
            CombatPresentationReducer reducer = NewReducer();
            var start = new CombatLifecycleEvent
            {
                eventId = "evt-1",
                serverSequence = 11,
                serverTick = 101,
                kind = CombatLifecycleKind.CastStarted,
                castId = "cast-1",
                sourceEntityId = "player-1",
                skillId = 100,
            };

            Assert.AreEqual(PresentationApplyResult.Applied, reducer.Apply(start));
            Assert.AreEqual(PresentationApplyResult.Duplicate, reducer.Apply(start));

            Assert.AreEqual(PresentationApplyResult.SequenceGap, reducer.Apply(new CombatLifecycleEvent
            {
                eventId = "evt-3",
                serverSequence = 13,
                serverTick = 103,
                kind = CombatLifecycleKind.Hit,
                skillId = 100,
            }));
            Assert.AreEqual(11UL, reducer.lastServerSequence);
        }

        [Test]
        public void Reducer_TracksCastMissileAndRecoveryLifecycle()
        {
            CombatPresentationReducer reducer = NewReducer();
            Assert.AreEqual(PresentationApplyResult.Applied, reducer.Apply(new CombatLifecycleEvent
            {
                eventId = "start", serverSequence = 11, serverTick = 101,
                kind = CombatLifecycleKind.CastStarted, castId = "cast-1",
                sourceEntityId = "player-1", skillId = 100,
            }));
            Assert.AreEqual(PresentationApplyResult.Applied, reducer.Apply(new CombatLifecycleEvent
            {
                eventId = "missile", serverSequence = 12, serverTick = 102,
                kind = CombatLifecycleKind.MissileSpawned, castId = "cast-1",
                missileInstanceId = "m-1", missileContentId = 33, skillId = 100,
            }));
            Assert.AreEqual(PresentationApplyResult.Applied, reducer.Apply(new CombatLifecycleEvent
            {
                eventId = "collide", serverSequence = 13, serverTick = 103,
                kind = CombatLifecycleKind.MissileCollided, missileInstanceId = "m-1",
                skillId = 100, impactX = 10, impactY = 20,
            }));
            Assert.AreEqual(CombatLifecycleKind.MissileCollided, reducer.missiles["m-1"].phase);
            Assert.AreEqual(PresentationApplyResult.Applied, reducer.Apply(new CombatLifecycleEvent
            {
                eventId = "vanish", serverSequence = 14, serverTick = 104,
                kind = CombatLifecycleKind.MissileVanished, missileInstanceId = "m-1", skillId = 100,
            }));
            Assert.AreEqual(PresentationApplyResult.Applied, reducer.Apply(new CombatLifecycleEvent
            {
                eventId = "recovery", serverSequence = 15, serverTick = 105,
                kind = CombatLifecycleKind.CastRecoveryStarted, castId = "cast-1", skillId = 100,
            }));
            Assert.IsTrue(reducer.casts["cast-1"].recovering);
            Assert.AreEqual(PresentationApplyResult.Applied, reducer.Apply(new CombatLifecycleEvent
            {
                eventId = "end", serverSequence = 16, serverTick = 106,
                kind = CombatLifecycleKind.CastRecoveryEnded, castId = "cast-1", skillId = 100,
            }));
            Assert.AreEqual(0, reducer.casts.Count);
            Assert.AreEqual(0, reducer.missiles.Count);
        }

        [Test]
        public void Reducer_StatusRefreshRequiresIncreasingRevision()
        {
            CombatPresentationReducer reducer = NewReducer();
            Assert.AreEqual(PresentationApplyResult.Applied, reducer.Apply(new CombatLifecycleEvent
            {
                eventId = "apply", serverSequence = 11, serverTick = 101,
                kind = CombatLifecycleKind.StatusApplied, statusInstanceId = "s-1",
                statusEffectId = 7, statusRevision = 1, expiresAtTick = 120, skillId = 100,
            }));
            Assert.AreEqual(PresentationApplyResult.StateMismatch, reducer.Apply(new CombatLifecycleEvent
            {
                eventId = "refresh-stale", serverSequence = 12, serverTick = 102,
                kind = CombatLifecycleKind.StatusRefreshed, statusInstanceId = "s-1",
                statusEffectId = 7, statusRevision = 1, expiresAtTick = 130, skillId = 100,
            }));
            Assert.AreEqual(11UL, reducer.lastServerSequence);

            Assert.AreEqual(PresentationApplyResult.Applied, reducer.Apply(new CombatLifecycleEvent
            {
                eventId = "refresh", serverSequence = 12, serverTick = 102,
                kind = CombatLifecycleKind.StatusRefreshed, statusInstanceId = "s-1",
                statusEffectId = 7, statusRevision = 2, expiresAtTick = 130, skillId = 100,
            }));
            Assert.AreEqual(2UL, reducer.statuses["s-1"].revision);
        }

        [Test]
        public void PresentationGraph_SelectsExactHorseWeaponGenderTuple()
        {
            string hash = new string('a', 64);
            var graph = new SkillPresentationGraph
            {
                skillId = 100,
                canonicalFrameRate = 16,
                variants = new List<PresentationVariant>
                {
                    new PresentationVariant
                    {
                        variantId = "fallback",
                        cues = new List<PresentationCue>
                        {
                            Cue("fallback-cast", hash, animationId: 1),
                        },
                    },
                    new PresentationVariant
                    {
                        variantId = "female-horse-hidden",
                        gender = PlayerVisualGender.Female,
                        mount = MountSelector.Mounted,
                        mountVisualId = 19,
                        weaponVisibility = WeaponVisibility.Hidden,
                        weaponVisualId = 0,
                        cues = new List<PresentationCue>
                        {
                            Cue("exact-cast", hash, animationId: 14),
                        },
                    },
                },
            };
            var tuple = new PlayerVisualTuple
            {
                gender = PlayerVisualGender.Female,
                mounted = true,
                mountVisualId = 19,
                weaponVisibility = WeaponVisibility.Hidden,
                weaponVisualId = 0,
            };

            PresentationResolveResult result = PresentationGraphResolver.Resolve(
                graph, tuple, CombatLifecycleKind.CastStarted, SkillTriggerPhase.CastStart);

            Assert.IsTrue(result.success);
            Assert.AreEqual("female-horse-hidden", result.variant.variantId);
            Assert.AreEqual(14, result.cues[0].animationId);
        }

        [Test]
        public void PresentationGraph_AmbiguousTupleFailsClosed()
        {
            string hash = new string('a', 64);
            var graph = new SkillPresentationGraph
            {
                skillId = 100,
                canonicalFrameRate = 16,
                variants = new List<PresentationVariant>
                {
                    new PresentationVariant
                    {
                        variantId = "male-a", gender = PlayerVisualGender.Male,
                        cues = new List<PresentationCue> { Cue("a", hash, 1) },
                    },
                    new PresentationVariant
                    {
                        variantId = "male-b", gender = PlayerVisualGender.Male,
                        cues = new List<PresentationCue> { Cue("b", hash, 2) },
                    },
                },
            };
            var tuple = new PlayerVisualTuple
            {
                gender = PlayerVisualGender.Male,
                mounted = false,
                mountVisualId = 0,
                weaponVisibility = WeaponVisibility.Empty,
                weaponVisualId = 0,
            };

            PresentationResolveResult result = PresentationGraphResolver.Resolve(
                graph, tuple, CombatLifecycleKind.CastStarted, SkillTriggerPhase.CastStart);

            Assert.IsFalse(result.success);
            Assert.AreEqual(PresentationResolveFailure.AmbiguousVariant, result.failure);
        }

        [Test]
        public void Reconciliation_BlendsBoundedDriftAndSnapsTeleport()
        {
            Assert.AreEqual(
                ReconciliationAction.Blend,
                LocalPoseReconciliationPolicy.Evaluate(1.5, 1.0, 10, false).action);
            Assert.AreEqual(
                ReconciliationAction.Snap,
                LocalPoseReconciliationPolicy.Evaluate(0.1, 1.0, 1, true).action);
            Assert.AreEqual(
                ReconciliationAction.Snap,
                LocalPoseReconciliationPolicy.Evaluate(2.1, 1.0, 1, false).action);
        }

        [Test]
        public void InterpolationBuffer_DefaultsTwoGrowsOnJitterAndShrinksWithHysteresis()
        {
            var policy = new RemoteInterpolationBufferPolicy();
            Assert.AreEqual(2, policy.bufferTicks);
            Assert.AreEqual(3, policy.Observe(0.8, 100));
            Assert.AreEqual(2, policy.Observe(0.1, 5000));
            Assert.AreEqual(1, policy.Observe(0.1, 5000));
        }

        private static PresentationCue Cue(string id, string hash, int animationId)
        {
            return new PresentationCue
            {
                cueId = id,
                lifecycleKind = CombatLifecycleKind.CastStarted,
                triggerPhase = SkillTriggerPhase.CastStart,
                animationId = animationId,
                requiredAssetHashes = new List<string> { hash },
            };
        }
    }
}
