using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture, Category("CuiYan")]
    public sealed class CuiYanCombatRuntimeTests
    {
        private static SkillCatalog Catalog() => PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();

        private static CombatActorState CuiYan(int level) => new CombatActorState
        {
            actorId = 100,
            faction = CombatFaction.CuiYan,
            level = level,
            fightMode = true,
            currentLife = 1000,
            maxLife = 1000,
            currentMana = 1000,
            position = Vector2.zero,
            knownSkills = { 100 },
            skillLevels = { [100] = level },
        };

        private static CombatActorState Attacker() => new CombatActorState
        {
            actorId = 101,
            faction = CombatFaction.None,
            level = 1,
            fightMode = true,
            currentLife = 1000,
            maxLife = 1000,
            currentMana = 1000,
            position = new Vector2(1, 0),
        };

        private static SkillDefinition Probe(int skillId, bool melee)
        {
            var skill = new SkillDefinition
            {
                skillId = skillId,
                nameNormalized = melee ? "melee return probe" : "range return probe",
                maxLevel = 1,
                // Test fixture: Melee delivery so the probe deals direct damage (a Missile-style
                // probe with no child missile would spawn nothing and, PC KSkill::Cast, apply no
                // damage). The melee-vs-range return routing is driven by meleeType below
                // (CombatRuntimeService line: isMelee = meleeType != None), independent of SkillStyle.
                skillStyle = PcSkillStyle.Melee,
                targetEnemy = true,
                isPhysical = true,
                meleeType = melee ? PcMeleeType.AttackWithBlur : PcMeleeType.None,
            };
            var data = new SkillLevelData { level = 1 };
            data.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsDamageV, 100, 0, 100));
            skill.pcLevelData.Add(data);
            return skill;
        }

        [TestCase(1, 5)]
        [TestCase(20, 20)]
        public void HoTheHanBang_UsesCanonicalReturnStatesWithoutFabricatedDefense(int level, int expectedPercent)
        {
            var skill = Catalog().Resolve(100);
            var data = skill.GetPcLevelData(level);

            Assert.AreEqual(expectedPercent, data.First(MagicAttributeKind.MeleeDamageReturnP).value1);
            Assert.AreEqual(expectedPercent, data.First(MagicAttributeKind.RangeDamageReturnP).value1);
            Assert.AreEqual(18 * 120, data.First(MagicAttributeKind.MeleeDamageReturnP).value2);
            Assert.AreEqual(18 * 120, data.First(MagicAttributeKind.RangeDamageReturnP).value2);
            Assert.IsNull(data.First(MagicAttributeKind.ColdResP));
            Assert.IsNull(data.First(MagicAttributeKind.AddDefenseV));
        }

        [TestCase(1, 5)]
        [TestCase(20, 20)]
        public void HoTheHanBang_ReflectsDeterministicMeleeAndRangeDamage_AndRecastDoesNotDuplicateStates(int level, int expectedReturn)
        {
            var catalog = Catalog();
            const int meleeProbeId = 991000;
            const int rangeProbeId = 991001;
            catalog.Register(Probe(meleeProbeId, melee: true));
            catalog.Register(Probe(rangeProbeId, melee: false));
            var runtime = new CombatRuntimeService(catalog, damage: new DamageFormulaService
            {
                Roll = (min, _) => min,
                RollPercent = _ => true,
            });
            var defender = CuiYan(level);
            var attacker = Attacker();
            attacker.knownSkills.Add(meleeProbeId);
            attacker.knownSkills.Add(rangeProbeId);
            attacker.skillLevels[meleeProbeId] = 1;
            attacker.skillLevels[rangeProbeId] = 1;

            var buff = runtime.Cast(defender, defender, 100, defender.position, CombatRelation.Self);
            Assert.IsTrue(buff.success, buff.detail);
            Assert.AreEqual(expectedReturn, defender.states[MagicAttributeKind.MeleeDamageReturnP].value1);
            Assert.AreEqual(expectedReturn, defender.states[MagicAttributeKind.RangeDamageReturnP].value1);

            var melee = runtime.Cast(attacker, defender, meleeProbeId, defender.position, CombatRelation.Enemy).damageResults.Single();
            var range = runtime.Cast(attacker, defender, rangeProbeId, defender.position, CombatRelation.Enemy).damageResults.Single();
            Assert.AreEqual(100, melee.finalDamage);
            Assert.AreEqual(expectedReturn, melee.meleeReturnDamage);
            Assert.AreEqual(0, melee.rangeReturnDamage);
            Assert.AreEqual(100, range.finalDamage);
            Assert.AreEqual(0, range.meleeReturnDamage);
            Assert.AreEqual(expectedReturn, range.rangeReturnDamage);
            Assert.AreEqual(1000 - expectedReturn * 2, attacker.currentLife,
                "runtime sink applies return damage after each fixed 100-point hit");

            runtime.AdvanceTime(1);
            var recast = runtime.Cast(defender, defender, 100, defender.position, CombatRelation.Self);
            Assert.IsTrue(recast.success, recast.detail);
            Assert.AreEqual(2, defender.states.Count(kv =>
                kv.Key == MagicAttributeKind.MeleeDamageReturnP || kv.Key == MagicAttributeKind.RangeDamageReturnP),
                "current state dictionary replaces each return kind; this does not claim PC stacking semantics");
        }
    }
}
