using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture, Category("CombatRuntimeHorseRecast")]
    public sealed class CombatRuntimeHorseRecastTests
    {
        private const int FootSkill = 990101;
        private const int HorseSkill = 990102;
        private const int HorseZeroSkill = 990103;
        private const int AuraSkill = 990104;

        [Test]
        public void FootCast_UsesTimePerCast()
        {
            var runtime = Runtime(Skill(FootSkill, timePerCast: 17, timePerCastOnHorse: 99));
            var caster = Actor(FootSkill, rideHorse: false);
            var target = Target();

            var first = runtime.Cast(caster, target, FootSkill, target.position, CombatRelation.Enemy);
            var blocked = runtime.Cast(caster, target, FootSkill, target.position, CombatRelation.Enemy);

            Assert.IsTrue(first.success, first.detail);
            Assert.AreEqual(17, runtime.NextCastTime(caster.actorId, FootSkill));
            Assert.AreEqual(CombatCastRejectReason.OnCooldown, blocked.reason);
        }

        [Test]
        public void MountedCast_UsesTimePerCastOnHorse()
        {
            var runtime = Runtime(Skill(HorseSkill, timePerCast: 17, timePerCastOnHorse: 5));
            var caster = Actor(HorseSkill, rideHorse: true);
            var target = Target();

            var report = runtime.Cast(caster, target, HorseSkill, target.position, CombatRelation.Enemy);

            Assert.IsTrue(report.success, report.detail);
            Assert.AreEqual(5, runtime.NextCastTime(caster.actorId, HorseSkill));
        }

        [Test]
        public void MountedZeroCast_OverridesNonzeroFootCadence()
        {
            var runtime = Runtime(Skill(HorseZeroSkill, timePerCast: 9, timePerCastOnHorse: 0));
            var caster = Actor(HorseZeroSkill, rideHorse: true);
            var target = Target();

            var first = runtime.Cast(caster, target, HorseZeroSkill, target.position, CombatRelation.Enemy);
            var second = runtime.Cast(caster, target, HorseZeroSkill, target.position, CombatRelation.Enemy);

            Assert.IsTrue(first.success, first.detail);
            Assert.AreEqual(0, runtime.NextCastTime(caster.actorId, HorseZeroSkill));
            Assert.IsTrue(second.success, second.detail);
        }

        [Test]
        public void AuraCast_DoesNotArmNextCastTime()
        {
            var runtime = Runtime(Skill(AuraSkill, timePerCast: 30, timePerCastOnHorse: 40, isAura: true));
            runtime.AdvanceTime(10);
            var caster = Actor(AuraSkill, rideHorse: true);
            var target = Target();

            var first = runtime.Cast(caster, target, AuraSkill, target.position, CombatRelation.Enemy);
            var second = runtime.Cast(caster, target, AuraSkill, target.position, CombatRelation.Enemy);

            Assert.IsTrue(first.success, first.detail);
            Assert.AreEqual(0, runtime.NextCastTime(caster.actorId, AuraSkill));
            Assert.IsTrue(second.success, second.detail);
        }

        [Test]
        public void PcModSkillParser_PreservesTimePerCastOnHorse()
        {
            var row = PcModSkillParser.ParseLines(new[] { "header", ModSkillRow(1216, timePerCast: 12, timePerCastOnHorse: 34) }, minSkillId: 1).Single();
            var skill = PcModSkillParser.ToSkillDefinition(row);

            Assert.AreEqual(12, row.timePerCast);
            Assert.AreEqual(34, row.timePerCastOnHorse);
            Assert.AreEqual(34, skill.timePerCastOnHorse);
        }

        [Test]
        public void FactoryStaticPath_PreservesCanonicalMountedCadence()
        {
            var expected = new Dictionary<int, int>
            {
                [19] = 5,
                [20] = 54,
                [40] = 27,
                [138] = 40,
                [164] = 25,
                [181] = 54,
                [392] = 27,
            };
            var actual = PcCombatCatalogFactory.CreateShaolinSkills()
                .Concat(PcCombatCatalogFactory.CreateTianWangSkills())
                .Concat(PcCombatCatalogFactory.CreateTianRenSkills())
                .Concat(PcCombatCatalogFactory.CreateWuDangSkills())
                .Concat(PcCombatCatalogFactory.CreateKunLunSkills())
                .Where(skill => expected.ContainsKey(skill.skillId))
                .ToDictionary(skill => skill.skillId, skill => skill.timePerCastOnHorse);

            CollectionAssert.AreEquivalent(expected.Keys, actual.Keys);
            foreach (var pair in expected)
                Assert.AreEqual(pair.Value, actual[pair.Key], $"SkillId {pair.Key}");
        }

        private static CombatRuntimeService Runtime(SkillDefinition skill)
        {
            var catalog = new SkillCatalog();
            catalog.Register(skill);
            return new CombatRuntimeService(catalog);
        }

        private static SkillDefinition Skill(int id, int timePerCast, int timePerCastOnHorse, bool isAura = false)
        {
            var skill = new SkillDefinition
            {
                skillId = id,
                nameNormalized = "recast-probe-" + id,
                maxLevel = 1,
                skillStyle = PcSkillStyle.Missiles,
                attackRadius = 100,
                targetEnemy = true,
                timePerCast = timePerCast,
                timePerCastOnHorse = timePerCastOnHorse,
                isAura = isAura,
            };
            skill.pcLevelData.Add(new SkillLevelData { level = 1 });
            return skill;
        }

        private static CombatActorState Actor(int skillId, bool rideHorse) => new()
        {
            actorId = 1,
            fightMode = true,
            rideHorse = rideHorse,
            currentMana = 100,
            knownSkills = { skillId },
            skillLevels = { [skillId] = 1 },
            position = Vector2.zero,
        };

        private static CombatActorState Target() => new()
        {
            actorId = 2,
            currentLife = 100,
            maxLife = 100,
            position = new Vector2(1, 0),
        };

        private static string ModSkillRow(int skillId, int timePerCast, int timePerCastOnHorse)
        {
            var columns = Enumerable.Repeat("0", 114).ToArray();
            columns[0] = "mod-skill";
            columns[2] = skillId.ToString();
            columns[4] = "0";
            columns[32] = timePerCast.ToString();
            columns[33] = timePerCastOnHorse.ToString();
            columns[34] = "1";
            columns[36] = "1";
            columns[54] = "1";
            return string.Join("\t", columns);
        }
    }
}
