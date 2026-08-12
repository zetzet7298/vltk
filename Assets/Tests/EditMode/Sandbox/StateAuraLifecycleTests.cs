using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;
using VLTK.UI;

namespace VLTK.Tests.Sandbox
{
    [TestFixture, Category("StateAuraLifecycle")]
    public sealed class StateAuraLifecycleTests
    {
        private const int PassiveAuraSkill = 994101;
        private const int ActiveAuraSkill = 994102;

        private static SkillCatalog Catalog()
        {
            var catalog = new SkillCatalog();
            catalog.Register(StateSkill(PassiveAuraSkill, PcSkillStyle.PassivityNpcState, 43, -1));
            catalog.Register(StateSkill(ActiveAuraSkill, PcSkillStyle.InitiativeNpcState, 43, 36));
            return catalog;
        }

        private static SkillDefinition StateSkill(int id, PcSkillStyle style, int stateSpecialId, int durationTicks)
        {
            var skill = new SkillDefinition
            {
                skillId = id,
                nameNormalized = $"state-aura-{id}",
                maxLevel = 2,
                skillStyle = style,
                stateSpecialId = stateSpecialId,
                targetSelf = true,
            };

            for (int level = 1; level <= 2; level++)
            {
                var data = new SkillLevelData { level = level };
                data.state.Add(new SkillMagicAttribute(MagicAttributeKind.AllResP, 10 * level, durationTicks, 0));
                skill.pcLevelData.Add(data);
            }
            return skill;
        }

        private static CombatActorState Actor() => new CombatActorState
        {
            actorId = 1,
            faction = CombatFaction.None,
            level = 20,
            fightMode = true,
            currentLife = 1000,
            maxLife = 1000,
            currentMana = 1000,
            maxMana = 1000,
            position = Vector2.zero,
        };

        private static void Learn(CombatActorState actor, int skillId, int level = 1)
        {
            actor.knownSkills.Add(skillId);
            actor.skillLevels[skillId] = level;
        }

        [Test]
        public void InitialPassiveMaterializesAndCreatesMappedAura()
        {
            var catalog = Catalog();
            var actor = Actor();
            Learn(actor, PassiveAuraSkill);

            Assert.IsTrue(actor.MaterializeLearnedPassiveStates(catalog));
            var visual = new SkillEffectVisualService(null, catalog);

            Assert.AreEqual(1, visual.SynchronizeStateAuras(actor, Vector2.zero));
            var fx = visual.GetActiveEffects().Single();
            Assert.IsTrue(fx.hasStateSourceKey);
            Assert.AreEqual(new CombatStateSourceKey(actor.actorId, PassiveAuraSkill), fx.stateSourceKey);
            Assert.AreEqual("\\spr\\skill\\丐帮\\mag_gb_11_醉蝶狂舞.spr", fx.pcPreCastSpriteKey);
            Assert.AreEqual(float.MaxValue, fx.auraDuration);
        }

        [Test]
        public void RepeatedSynchronizationIsIdempotent_NoDuplicateEffects()
        {
            var catalog = Catalog();
            var actor = Actor();
            Learn(actor, PassiveAuraSkill);
            actor.MaterializeLearnedPassiveStates(catalog);
            var visual = new SkillEffectVisualService(null, catalog);

            visual.SynchronizeStateAuras(actor, Vector2.zero);
            var first = visual.GetActiveEffects().Single();
            Assert.IsFalse(actor.MaterializeLearnedPassiveStates(catalog), "unchanged passive materialization should not rebuild");
            visual.SynchronizeStateAuras(actor, new Vector2(5, 6));

            Assert.AreEqual(1, visual.ActiveEffectCount);
            Assert.AreSame(first, visual.GetActiveEffects().Single());
            Assert.AreEqual(new Vector2(5, 6), first.targetPos);
        }

        [Test]
        public void ActiveStateRefreshClaimsCastAuraAndDoesNotDuplicate()
        {
            var catalog = Catalog();
            var runtime = new CombatRuntimeService(catalog);
            var actor = Actor();
            Learn(actor, ActiveAuraSkill);
            var visual = new SkillEffectVisualService(null, catalog);

            Assert.IsTrue(runtime.Cast(actor, actor, ActiveAuraSkill, actor.position, CombatRelation.Self).success);
            var castAura = visual.PlaySkillCast(catalog.Resolve(ActiveAuraSkill), actor.position, actor.position, 1);
            Assert.IsTrue(castAura.isAura);
            visual.SynchronizeStateAuras(actor, actor.position);
            actor.ExpireStateSources(18);
            visual.SynchronizeStateAuras(actor, actor.position);
            Assert.AreEqual(1, visual.ActiveEffectCount);
            Assert.AreSame(castAura, visual.GetActiveEffects().Single(), "source sync should claim cast aura instead of spawning duplicate");
            Assert.AreEqual(1f, castAura.auraDuration, 0.001f);

            runtime.AdvanceTime(100);
            Assert.IsTrue(runtime.Cast(actor, actor, ActiveAuraSkill, actor.position, CombatRelation.Self).success);
            visual.SynchronizeStateAuras(actor, actor.position);
            Assert.AreEqual(1, visual.ActiveEffectCount);
            Assert.AreSame(castAura, visual.GetActiveEffects().Single());
            Assert.AreEqual(2f, castAura.auraDuration, 0.001f);
        }

        [Test]
        public void FiniteActiveExpiryRemovesMappedAura()
        {
            var catalog = Catalog();
            var runtime = new CombatRuntimeService(catalog);
            var actor = Actor();
            Learn(actor, ActiveAuraSkill);
            var visual = new SkillEffectVisualService(null, catalog);

            Assert.IsTrue(runtime.Cast(actor, actor, ActiveAuraSkill, actor.position, CombatRelation.Self).success);
            visual.SynchronizeStateAuras(actor, actor.position);
            Assert.AreEqual(1, visual.ActiveEffectCount);

            actor.ExpireStateSources(36);
            visual.SynchronizeStateAuras(actor, actor.position);
            Assert.AreEqual(0, visual.ActiveEffectCount);
        }

        [Test]
        public void LearnedPassiveRemovalAndLevelChangeUpdateAuraLifecycle()
        {
            var catalog = Catalog();
            var actor = Actor();
            Learn(actor, PassiveAuraSkill, 1);
            actor.MaterializeLearnedPassiveStates(catalog);
            var visual = new SkillEffectVisualService(null, catalog);
            visual.SynchronizeStateAuras(actor, actor.position);
            var fx = visual.GetActiveEffects().Single();
            Assert.AreEqual(10, actor.states[MagicAttributeKind.AllResP].value1);

            actor.skillLevels[PassiveAuraSkill] = 2;
            Assert.IsTrue(actor.MaterializeLearnedPassiveStates(catalog));
            visual.SynchronizeStateAuras(actor, actor.position);
            Assert.AreEqual(1, visual.ActiveEffectCount);
            Assert.AreSame(fx, visual.GetActiveEffects().Single());
            Assert.AreEqual(2, fx.skillLevel);
            Assert.AreEqual(20, actor.states[MagicAttributeKind.AllResP].value1);

            actor.knownSkills.Remove(PassiveAuraSkill);
            Assert.IsTrue(actor.MaterializeLearnedPassiveStates(catalog));
            visual.SynchronizeStateAuras(actor, actor.position);
            Assert.AreEqual(0, visual.ActiveEffectCount);
        }

        [Test]
        public void RuntimeFactionSwitchClearsTransientEffects_AndLeavesOnlySourceOwnedAuraRebuilds()
        {
            typeof(SandboxManager).GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?
                .GetSetMethod(true)?.Invoke(null, new object[] { null });

            var go = new GameObject("StateAuraFactionSwitchTest");
            try
            {
                var manager = go.AddComponent<SandboxManager>();
                manager.BootstrapCombatForTests(new AssetRegistry());
                manager.SkillEffectVisual.PlayHitFlash(Vector2.zero, Color.red, 1f);
                Assert.IsTrue(manager.SkillEffectVisual.GetActiveEffects().Any(fx => fx.skillId == -1));

                Assert.IsTrue(manager.TrySwitchRuntimeFaction(CombatFaction.CaiBang, out string detail), detail);
                Assert.IsFalse(detail.Contains("effectsCleared=0"), detail);

                var effects = manager.SkillEffectVisual.GetActiveEffects();
                Assert.IsFalse(effects.Any(fx => fx.skillId == -1), "transient hit flash must stay cleared");
                Assert.IsTrue(effects.All(fx => fx.isAura && fx.hasStateSourceKey),
                    "if catalog supplies mapped passive auras, they must be rebuilt after clear as source-owned visuals only");
            }
            finally
            {
                typeof(SandboxManager).GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?
                    .GetSetMethod(true)?.Invoke(null, new object[] { null });
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RenderersDoNotUseGenericPreCastGeometryForAuraWithoutExactPcArt()
        {
            var missingArtAura = new ActiveSkillEffect
            {
                isAura = true,
                phase = SkillEffectPhase.PreCast,
                pcPreCastSpriteKey = null,
                pcPreCastTotalFrames = 0,
                pcPreCastDirections = 0,
            };
            var normalPreCast = new ActiveSkillEffect
            {
                isAura = false,
                phase = SkillEffectPhase.PreCast,
            };

            Assert.IsFalse(SkillEffectRenderer.ShouldDrawFallbackPreCastCircle(missingArtAura));
            Assert.IsTrue(SkillEffectRenderer.ShouldDrawFallbackPreCastCircle(normalPreCast));
            Assert.IsFalse(SkillEffectWorldOverlay.ShouldDrawFallbackPreCastRing(missingArtAura));
            Assert.IsTrue(SkillEffectWorldOverlay.ShouldDrawFallbackPreCastRing(normalPreCast));
        }
    }
}
