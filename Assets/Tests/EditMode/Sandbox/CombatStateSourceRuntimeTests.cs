using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;
using VLTK.UI;

namespace VLTK.Tests.Sandbox
{
    [TestFixture, Category("CombatStateSources")]
    public sealed class CombatStateSourceRuntimeTests
    {
        private const int FirstBuff = 994001;
        private const int SecondBuff = 994002;
        private const int PassiveBuff = 994003;
        private const int RangeProbe = 994004;
        private const int ImmediateBuff = 994005;

        private static SkillCatalog Catalog()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
            catalog.Register(Buff(FirstBuff, 10, 18));
            catalog.Register(Buff(SecondBuff, 7, 36));
            catalog.Register(Passive(PassiveBuff, 30));
            catalog.Register(DamageProbe(RangeProbe));
            catalog.Register(Immediate(ImmediateBuff));
            return catalog;
        }

        private static SkillDefinition Buff(int id, int levelOneValue, int duration)
        {
            var skill = new SkillDefinition
            {
                skillId = id,
                nameNormalized = $"state-source-{id}",
                maxLevel = 2,
                skillStyle = PcSkillStyle.InitiativeNpcState,
                targetSelf = true,
            };
            for (int level = 1; level <= 2; level++)
            {
                var data = new SkillLevelData { level = level };
                data.state.Add(new SkillMagicAttribute(MagicAttributeKind.RangeDamageReturnP, levelOneValue * level, duration, 0));
                skill.pcLevelData.Add(data);
            }
            return skill;
        }

        private static SkillDefinition Passive(int id, int value)
        {
            var skill = new SkillDefinition
            {
                skillId = id,
                nameNormalized = $"passive-source-{id}",
                maxLevel = 1,
                skillStyle = PcSkillStyle.PassivityNpcState,
            };
            var data = new SkillLevelData { level = 1 };
            data.state.Add(new SkillMagicAttribute(MagicAttributeKind.RangeDamageReturnP, value, -1, 0));
            skill.pcLevelData.Add(data);
            return skill;
        }

        private static SkillDefinition Immediate(int id)
        {
            var skill = new SkillDefinition
            {
                skillId = id,
                nameNormalized = "immediate-compatibility-probe",
                maxLevel = 1,
                skillStyle = PcSkillStyle.InitiativeNpcState,
                targetSelf = true,
            };
            var data = new SkillLevelData { level = 1 };
            data.immediate.Add(new SkillMagicAttribute(MagicAttributeKind.FastWalkRunP, 66, 18, 0));
            skill.pcLevelData.Add(data);
            return skill;
        }

        private static SkillDefinition DamageProbe(int id)
        {
            var skill = new SkillDefinition
            {
                skillId = id,
                nameNormalized = "range-return-probe",
                maxLevel = 1,
                skillStyle = PcSkillStyle.Missiles,
                targetEnemy = true,
                isPhysical = true,
            };
            var data = new SkillLevelData { level = 1 };
            data.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsDamageV, 100, 0, 100));
            skill.pcLevelData.Add(data);
            return skill;
        }

        private static CombatActorState Actor(int id = 1) => new CombatActorState
        {
            actorId = id,
            faction = CombatFaction.None,
            level = 20,
            fightMode = true,
            currentLife = 1000,
            maxLife = 1000,
            currentMana = 1000,
            position = Vector2.zero,
        };

        private static void Learn(CombatActorState actor, int skillId, int level = 1)
        {
            actor.knownSkills.Add(skillId);
            actor.skillLevels[skillId] = level;
        }

        [Test]
        public void DifferentSourcesAggregate_AndGameplayExpiryRemovesOnlyExpiredSource()
        {
            var catalog = Catalog();
            var loop = new GameplayLoopService(catalog);
            var owner = loop.RegisterPlayer(1, "state-owner", 20, Vector2.zero).combat;
            Learn(owner, FirstBuff);
            Learn(owner, SecondBuff);

            Assert.IsTrue(loop.Combat.Cast(owner, owner, FirstBuff, owner.position, CombatRelation.Self).success);
            Assert.IsTrue(loop.Combat.Cast(owner, owner, SecondBuff, owner.position, CombatRelation.Self).success);
            Assert.AreEqual(17, owner.states[MagicAttributeKind.RangeDamageReturnP].value1);
            Assert.AreEqual(2, owner.stateSources.Count);

            loop.Tick(1f); // 18 PC ticks: FirstBuff expires; SecondBuff has 18 ticks left.
            Assert.AreEqual(7, owner.states[MagicAttributeKind.RangeDamageReturnP].value1);
            Assert.AreEqual(18, owner.states[MagicAttributeKind.RangeDamageReturnP].value2);
            Assert.IsFalse(owner.stateSources.ContainsKey(new CombatStateSourceKey(owner.actorId, FirstBuff)));
            Assert.IsTrue(owner.stateSources.ContainsKey(new CombatStateSourceKey(owner.actorId, SecondBuff)));
        }

        [Test]
        public void RecastRefreshReplaceAndLowerLevelIgnore_AreSourceScoped()
        {
            var catalog = Catalog();
            var runtime = new CombatRuntimeService(catalog);
            var owner = Actor();
            Learn(owner, FirstBuff, 1);

            Assert.IsTrue(runtime.Cast(owner, owner, FirstBuff, owner.position, CombatRelation.Self).success);
            owner.ExpireStateSources(5);
            Assert.AreEqual(13, owner.states[MagicAttributeKind.RangeDamageReturnP].value2);

            Assert.IsTrue(runtime.Cast(owner, owner, FirstBuff, owner.position, CombatRelation.Self).success);
            Assert.AreEqual(10, owner.states[MagicAttributeKind.RangeDamageReturnP].value1);
            Assert.AreEqual(18, owner.states[MagicAttributeKind.RangeDamageReturnP].value2, "same level refreshes instead of adding");
            Assert.AreEqual(1, owner.stateSources.Count);

            owner.skillLevels[FirstBuff] = 2;
            Assert.IsTrue(runtime.Cast(owner, owner, FirstBuff, owner.position, CombatRelation.Self).success);
            Assert.AreEqual(20, owner.states[MagicAttributeKind.RangeDamageReturnP].value1, "higher level replaces atomically");
            owner.ExpireStateSources(5);

            owner.skillLevels[FirstBuff] = 1;
            Assert.IsTrue(runtime.Cast(owner, owner, FirstBuff, owner.position, CombatRelation.Self).success);
            Assert.AreEqual(20, owner.states[MagicAttributeKind.RangeDamageReturnP].value1);
            Assert.AreEqual(13, owner.states[MagicAttributeKind.RangeDamageReturnP].value2, "lower recast does not refresh higher source");
        }

        [Test]
        public void SameSkillFromDifferentCasters_ReplacesOneReceiverOwnedAllyNode()
        {
            var catalog = Catalog();
            var allyBuff = catalog.Resolve(FirstBuff);
            allyBuff.targetSelf = false;
            allyBuff.targetAlly = true;
            var runtime = new CombatRuntimeService(catalog);
            var firstCaster = Actor(10);
            var secondCaster = Actor(11);
            var receiver = Actor(12);
            Learn(firstCaster, FirstBuff, 1);
            Learn(secondCaster, FirstBuff, 2);

            Assert.IsTrue(runtime.Cast(firstCaster, receiver, FirstBuff, receiver.position, CombatRelation.Ally).success);
            Assert.IsFalse(firstCaster.states.ContainsKey(MagicAttributeKind.RangeDamageReturnP),
                "ally-target state must not be routed back to the caster");
            Assert.AreEqual(10, receiver.states[MagicAttributeKind.RangeDamageReturnP].value1);

            Assert.IsTrue(runtime.Cast(secondCaster, receiver, FirstBuff, receiver.position, CombatRelation.Ally).success);
            Assert.AreEqual(20, receiver.states[MagicAttributeKind.RangeDamageReturnP].value1);
            Assert.AreEqual(1, receiver.stateSources.Count);
            Assert.IsTrue(receiver.stateSources.ContainsKey(new CombatStateSourceKey(receiver.actorId, FirstBuff)));

            receiver.ExpireStateSources(5);
            Assert.IsTrue(runtime.Cast(firstCaster, receiver, FirstBuff, receiver.position, CombatRelation.Ally).success);
            Assert.AreEqual(13, receiver.states[MagicAttributeKind.RangeDamageReturnP].value2,
                "lower-level recast from another caster must not refresh the receiver's higher-level node");
        }

        [Test]
        public void PassiveNodesAreNotPersisted_AndFlattenedTemporaryStateHydratesOnce()
        {
            var catalog = Catalog();
            var first = Actor();
            Learn(first, PassiveBuff);
            Learn(first, FirstBuff);
            CombatSkillSlotController.MaterializePassiveStates(first, catalog);
            CombatSkillSlotController.MaterializePassiveStates(first, catalog);
            Assert.AreEqual(30, first.states[MagicAttributeKind.RangeDamageReturnP].value1, "materialization is idempotent");
            Assert.IsTrue(first.stateSources[new CombatStateSourceKey(first.actorId, PassiveBuff)].isPermanentPassive);

            var runtime = new CombatRuntimeService(catalog);
            Assert.IsTrue(runtime.Cast(first, first, FirstBuff, first.position, CombatRelation.Self).success);
            Assert.AreEqual(40, first.states[MagicAttributeKind.RangeDamageReturnP].value1);

            var persisted = new Dictionary<MagicAttributeKind, SkillMagicAttribute>();
            CombatSkillSlotController.PersistStatesWithoutPassiveContributions(first, catalog, persisted);
            Assert.AreEqual(10, persisted[MagicAttributeKind.RangeDamageReturnP].value1, "passive node excluded from old flattened save format");

            var hydrated = Actor();
            Learn(hydrated, PassiveBuff);
            foreach (var pair in persisted)
                hydrated.states[pair.Key] = new SkillMagicAttribute(pair.Value.kind, pair.Value.value1, pair.Value.value2, pair.Value.value3);
            hydrated.ImportLegacyStates();
            CombatSkillSlotController.MaterializePassiveStates(hydrated, catalog);
            Assert.AreEqual(40, hydrated.states[MagicAttributeKind.RangeDamageReturnP].value1);
            Assert.AreEqual(2, hydrated.stateSources.Count, "temporary compatibility node plus permanent passive node");
        }

        [Test]
        public void LegacyAndImmediateStatesUseExplicitCompatibilitySource()
        {
            var catalog = Catalog();
            var runtime = new CombatRuntimeService(catalog);
            var actor = Actor();
            actor.states[MagicAttributeKind.AllResP] = new SkillMagicAttribute(MagicAttributeKind.AllResP, 12, 18, 0);
            actor.ImportLegacyStates();
            Learn(actor, ImmediateBuff);

            Assert.IsTrue(runtime.Cast(actor, actor, ImmediateBuff, actor.position, CombatRelation.Self).success);
            Assert.AreEqual(12, actor.states[MagicAttributeKind.AllResP].value1);
            Assert.AreEqual(66, actor.states[MagicAttributeKind.FastWalkRunP].value1);
            Assert.IsTrue(actor.stateSources.ContainsKey(new CombatStateSourceKey(0, CombatActorState.CompatibilityStateSourceSkillId)));
            actor.ExpireStateSources(18);
            Assert.IsFalse(actor.states.ContainsKey(MagicAttributeKind.AllResP));
            Assert.IsFalse(actor.states.ContainsKey(MagicAttributeKind.FastWalkRunP));
        }

        [Test]
        public void Skill720NegativeRangeReturn_ClampsReflectThenRestoresAfterOnly720Expires()
        {
            var catalog = Catalog();
            var loop = new GameplayLoopService(catalog);
            var defender = loop.RegisterPlayer(1, "defender", 20, Vector2.zero).combat;
            Learn(defender, FirstBuff, 2); // +20 for 18 ticks is too short; extend its node below.
            Assert.IsTrue(loop.Combat.Cast(defender, defender, FirstBuff, defender.position, CombatRelation.Self).success);
            defender.ApplySkillStateSource(defender.actorId, FirstBuff, 2,
                new[] { new SkillMagicAttribute(MagicAttributeKind.RangeDamageReturnP, 20, 300, 0) }, forceReplace: true);

            var beggar = Actor(2);
            beggar.faction = CombatFaction.CaiBang;
            Learn(beggar, 720, 20);
            Assert.IsTrue(loop.Combat.Cast(beggar, defender, 720, defender.position, CombatRelation.Enemy).success);
            Assert.AreEqual(-10, defender.states[MagicAttributeKind.RangeDamageReturnP].value1);

            var damage = new DamageFormulaService { Roll = (min, _) => min, RollPercent = _ => true };
            var runtime = new CombatRuntimeService(catalog, damage: damage);
            var attacker = Actor(3);
            Learn(attacker, RangeProbe);
            var suppressed = runtime.Cast(attacker, defender, RangeProbe, defender.position, CombatRelation.Enemy).damageResults.Single();
            Assert.Greater(suppressed.finalDamage, 0);
            Assert.AreEqual(0, suppressed.rangeReturnDamage, "existing reflect sink clamps negative aggregate at zero");

            loop.Tick(9f); // 162 PC ticks: canonical skill 720 duration.
            Assert.AreEqual(20, defender.states[MagicAttributeKind.RangeDamageReturnP].value1);
            var restored = runtime.Cast(attacker, defender, RangeProbe, defender.position, CombatRelation.Enemy).damageResults.Single();
            Assert.AreEqual(restored.finalDamage * 20 / DamageFormulaService.MaxPercent, restored.rangeReturnDamage);
        }
    }
}
