using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;
using VLTK.SkillPort;
using VLTK.Sprites;

namespace VLTK.Tests.SkillPort
{
    [Category("SkillPort")]
    public class AuthoritativeCombatPresentationBridgeTests
    {
        [Test]
        public void Bridge_AcceptsResyncThenPresentsMappedServerLifecycle()
        {
            var context = new ContextStub();
            var preload = new PreloadStub { ready = true };
            var sink = new SinkStub();
            var bridge = NewBridge(context, preload, sink, SkillPresentationMode.GraphV2, SkillAuthorityMode.GoOnly);

            Assert.AreEqual(
                AuthoritativePresentationDispatchResult.ResyncApplied,
                bridge.ApplyServerEnvelope(ResyncEnvelope(1)));

            global::Game.V1.ServerEnvelope cast = CombatEnvelope(2, global::Game.V1.CombatEventKind.CastStarted);
            cast.Combat.AnimationId = 14;
            cast.Combat.VisualEffectId = 33;
            cast.Combat.AudioCueId = "skill/cast.wav";
            cast.Combat.SkillLevel = 20;

            Assert.AreEqual(AuthoritativePresentationDispatchResult.Presented, bridge.ApplyServerEnvelope(cast));
            Assert.AreEqual(1, sink.count);
            Assert.AreEqual(14, sink.last.animationId);
            Assert.AreEqual(33, sink.last.visualEffectId);
            Assert.AreEqual("skill/cast.wav", sink.last.audioCueId);
            Assert.AreEqual(20, sink.last.skillLevel);
            Assert.IsTrue(sink.lastTuple.mounted);
            Assert.AreEqual(19, sink.lastTuple.mountVisualId);
        }

        [Test]
        public void Bridge_ObservesNonCombatEnvelopeWithoutFalseReducerGap()
        {
            var bridge = NewBridge(new ContextStub(), new PreloadStub { ready = true }, new SinkStub(),
                SkillPresentationMode.GraphV2, SkillAuthorityMode.GoOnly);
            Assert.AreEqual(AuthoritativePresentationDispatchResult.ResyncApplied,
                bridge.ApplyServerEnvelope(ResyncEnvelope(1)));

            var pong = new global::Game.V1.ServerEnvelope
            {
                SessionEpoch = 7,
                ServerSeq = 2,
                ServerTick = 101,
                Pong = new global::Game.V1.Pong(),
            };
            Assert.AreEqual(AuthoritativePresentationDispatchResult.SequenceObserved,
                bridge.ApplyServerEnvelope(pong));
            Assert.AreEqual(AuthoritativePresentationDispatchResult.Presented,
                bridge.ApplyServerEnvelope(CombatEnvelope(3, global::Game.V1.CombatEventKind.CastStarted)));
        }

        [Test]
        public void Bridge_ShadowPolicyNeverCallsPresentationSink()
        {
            var sink = new SinkStub();
            var bridge = NewBridge(new ContextStub(), new PreloadStub { ready = true }, sink,
                SkillPresentationMode.GraphV2Shadow, SkillAuthorityMode.LegacyActiveGoShadow);
            bridge.ApplyServerEnvelope(ResyncEnvelope(1));

            Assert.AreEqual(AuthoritativePresentationDispatchResult.ShadowOnly,
                bridge.ApplyServerEnvelope(CombatEnvelope(2, global::Game.V1.CombatEventKind.CastStarted)));
            Assert.AreEqual(0, sink.count);
        }

        [Test]
        public void Bridge_BlocksPresentationUntilPreloadReady()
        {
            var sink = new SinkStub();
            var bridge = NewBridge(new ContextStub(), new PreloadStub { ready = false }, sink,
                SkillPresentationMode.GraphV2, SkillAuthorityMode.GoOnly);
            bridge.ApplyServerEnvelope(ResyncEnvelope(1));

            Assert.AreEqual(AuthoritativePresentationDispatchResult.PreloadBlocked,
                bridge.ApplyServerEnvelope(CombatEnvelope(2, global::Game.V1.CombatEventKind.CastStarted)));
            Assert.AreEqual(0, sink.count);
        }

        [Test]
        public void Bridge_RequiresAuthoritativeInputOnlyForGoActivePolicy()
        {
            var bridge = NewBridge(new ContextStub(), new PreloadStub(), new SinkStub(),
                SkillPresentationMode.GraphV2, SkillAuthorityMode.GoOnly);

            Assert.IsTrue(bridge.RequiresAuthoritativeInput(117, "CaiBang"));
            Assert.IsFalse(bridge.RequiresAuthoritativeInput(118, "CaiBang"));
            Assert.IsFalse(bridge.RequiresAuthoritativeInput(117, "Shaolin"));
        }

        [Test]
        public void VisualService_AuthoritativeMissileNeverRunsLocalLifecycle()
        {
            SkillCatalog catalog = PcCombatCatalogFactory.CreateNoviceAndCaiBangCatalog();
            var service = new SkillEffectVisualService(new SprRuntimeService(), catalog);
            SkillDefinition skill = catalog.Resolve(117);

            ActiveSkillEffect effect = service.SpawnAuthoritativeMissile(
                "missile-1",
                skill,
                new Vector2(1f, 2f),
                new Vector2(100f, 200f),
                20);

            Assert.NotNull(effect);
            Assert.IsTrue(effect.authoritativeLifecycle);
            Vector2 initial = effect.currentMissilePos;
            service.Update(10f);
            Assert.AreEqual(initial, effect.currentMissilePos);
            Assert.AreEqual(SkillEffectPhase.Missile, effect.phase);

            Assert.IsTrue(service.UpdateAuthoritativeMissile("missile-1", new Vector2(4f, 5f), false));
            Assert.AreEqual(new Vector2(4f, 5f), effect.currentMissilePos);
            Assert.IsTrue(service.CollideAuthoritativeMissile("missile-1", new Vector2(6f, 7f), false));
            Assert.AreEqual(SkillEffectPhase.Impact, effect.phase);
            service.Update(10f);
            Assert.AreEqual(SkillEffectPhase.Impact, effect.phase);
            Assert.IsTrue(service.VanishAuthoritativeMissile("missile-1"));
            Assert.AreEqual(0, service.ActiveEffectCount);
        }

        private static AuthoritativeCombatPresentationBridge NewBridge(
            IAuthoritativeCombatPresentationContext context,
            IAuthoritativeCombatPresentationPreloadGate preload,
            IAuthoritativeCombatPresentationSink sink,
            SkillPresentationMode presentation,
            SkillAuthorityMode authority)
        {
            var bridge = new AuthoritativeCombatPresentationBridge(context, preload, sink);
            var policy = new RuntimePolicySnapshot(1);
            policy.SetSkill(new SkillRuntimeMode
            {
                skillId = 117,
                factionKey = "CaiBang",
                exposed = true,
                authorityMode = authority,
                presentationMode = presentation,
            });
            bridge.SetRuntimePolicy(policy);
            bridge.BeginSession(7, 0, 99);
            return bridge;
        }

        private static global::Game.V1.ServerEnvelope ResyncEnvelope(ulong sequence)
        {
            return new global::Game.V1.ServerEnvelope
            {
                SessionEpoch = 7,
                ServerSeq = sequence,
                ServerTick = 100,
                ActiveCombatResync = new global::Game.V1.ActiveCombatResyncState
                {
                    BaselineTick = 100,
                    Full = true,
                },
            };
        }

        private static global::Game.V1.ServerEnvelope CombatEnvelope(
            ulong sequence,
            global::Game.V1.CombatEventKind kind)
        {
            return new global::Game.V1.ServerEnvelope
            {
                SessionEpoch = 7,
                ServerSeq = sequence,
                ServerTick = 100 + sequence,
                Combat = new global::Game.V1.CombatEvent
                {
                    EventId = "event-" + sequence,
                    ServerTick = 100 + sequence,
                    Kind = kind,
                    CastId = "cast-1",
                    SourceEntityId = "player-1",
                    TargetEntityId = "enemy-1",
                    SkillId = 117,
                },
            };
        }

        private sealed class ContextStub : IAuthoritativeCombatPresentationContext
        {
            public bool TryResolve(string sourceEntityId, out string factionKey, out PlayerVisualTuple visualTuple)
            {
                factionKey = "CaiBang";
                visualTuple = new PlayerVisualTuple
                {
                    gender = PlayerVisualGender.Female,
                    mounted = true,
                    mountVisualId = 19,
                    weaponVisibility = WeaponVisibility.Hidden,
                    weaponVisualId = 0,
                };
                return sourceEntityId == "player-1";
            }
        }

        private sealed class PreloadStub : IAuthoritativeCombatPresentationPreloadGate
        {
            public bool ready;
            public bool CanReveal(int skillId, CombatLifecycleEvent evt) => ready;
        }

        private sealed class SinkStub : IAuthoritativeCombatPresentationSink
        {
            public int count;
            public CombatLifecycleEvent last;
            public PlayerVisualTuple lastTuple;

            public bool TryPresent(CombatLifecycleEvent evt, PlayerVisualTuple visualTuple)
            {
                count++;
                last = evt;
                lastTuple = visualTuple;
                return true;
            }
        }
    }
}
