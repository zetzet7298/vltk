using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;
using VLTK.Core;

namespace VLTK.Tests.Sandbox
{
    [TestFixture, Category("PcBuffAura")]
    public sealed class PcBuffAuraCanonicalMappingTests
    {
        private static SkillCatalog Catalog => TestCatalogCache.NoviceAndAllSect;

        [Test]
        public void HoatBatLuuThu127_DoesNotBorrowKonLunState17()
        {
            var skill = Catalog.Resolve(127);

            Assert.IsNotNull(skill);
            Assert.AreEqual(PcSkillStyle.Missiles, skill.skillStyle);
            Assert.AreEqual(0, skill.stateSpecialId);
            Assert.IsFalse(skill.isAura);
            Assert.AreEqual(SkillMissileForm.Stance, skill.missileForm);
            Assert.AreEqual(0, skill.childSkillId);
            Assert.AreEqual(11, skill.charAnimId);
            Assert.AreEqual(5, skill.waitTime);
            Assert.IsTrue(skill.targetSelf);
            Assert.AreEqual("\\spr\\skill\\天忍\\mag_tr_16_施魔法.spr",
                skill.effectSourceId.sourcePath);

            var konLunAura = PcSkillVisualAutoMapper.GetStateAuraData(17);
            StringAssert.Contains("\\昆仑\\kl_10_滑不留手.spr", konLunAura.sprPath);
        }

        [Test]
        public void VerifiedStateIds_DoNotCrossFaction()
        {
            Assert.AreEqual(0, Catalog.Resolve(36).stateSpecialId,
                "Thiên Vương Chiến Ý has no canonical aura");
            Assert.AreEqual(29, Catalog.Resolve(146).stateSpecialId,
                "Ngũ Hành Trận uses the Thiên Nhẫn state visual, not child missile 226");

            var newerHoatBatLuuThu = Catalog.Resolve(277);
            Assert.AreEqual(57, newerHoatBatLuuThu.stateSpecialId);
            Assert.AreEqual(114, newerHoatBatLuuThu.childSkillId);
            Assert.AreEqual(-1, newerHoatBatLuuThu.childSkillLevel);
            Assert.AreEqual(1, newerHoatBatLuuThu.childSkillNum);
            Assert.IsTrue(newerHoatBatLuuThu.baseSkill);
            Assert.IsTrue(string.IsNullOrEmpty(
                PcSkillVisualAutoMapper.GetStateAuraData(57).sprPath),
                "PC state 57 is a stub without package bytes and must fail closed");
        }

        [Test]
        public void VoHinhDoc69_UsesVerifiedPcPresentationFieldsAndAura()
        {
            var skill = Catalog.Resolve(69);

            Assert.IsNotNull(skill);
            Assert.AreEqual(PcSkillStyle.InitiativeNpcState, skill.skillStyle);
            Assert.AreEqual(6, skill.stateSpecialId);
            Assert.IsTrue(skill.isAura);
            Assert.AreEqual(400, skill.attackRadius);
            Assert.AreEqual(SkillMissileForm.Stationary, skill.missileForm);
            Assert.AreEqual(203, skill.childSkillId);
            Assert.AreEqual(-1, skill.childSkillLevel);
            Assert.AreEqual(1, skill.childSkillNum);
            Assert.IsFalse(skill.baseSkill);
            Assert.AreEqual(11, skill.charAnimId);

            var aura = PcSkillVisualAutoMapper.GetStateAuraData(skill.stateSpecialId);
            Assert.AreEqual("\\spr\\skill\\五毒教\\wdu_06_无形蛊.spr", aura.sprPath);
            Assert.AreEqual(2, aura.position);
            Assert.AreEqual(30, aura.totalFrames);
            Assert.AreEqual(1, aura.directions);
            Assert.AreEqual(1, aura.intervalTicks);

            var level1 = skill.GetPcLevelData(1);
            var level20 = skill.GetPcLevelData(20);
            var slow1 = level1.state.Single(a => a.kind == MagicAttributeKind.FastWalkRunP);
            var slow20 = level20.state.Single(a => a.kind == MagicAttributeKind.FastWalkRunP);
            Assert.AreEqual(-10, slow1.value1);
            Assert.AreEqual(-42, slow20.value1);
            Assert.AreEqual(36, slow1.value2);
            Assert.AreEqual(36, slow20.value2);
            Assert.IsFalse(level1.state.Any(a => a.kind == MagicAttributeKind.AttackSpeedV),
                "Vô Hình Độc changes movement speed, not attack speed");
            Assert.IsFalse(level1.skill.Any(a => a.kind == MagicAttributeKind.SkillCostV),
                "canonical row and Lua provide no fabricated mana cost");

            var poison = level20.damage.Single(a => a.kind == MagicAttributeKind.PoisonDamageV);
            Assert.AreEqual(25, poison.value1);
            Assert.AreEqual(20, poison.value2);
            Assert.AreEqual(25, poison.value3);

            var config = new PcSkillVisualAutoMapper().GetVisualConfig(skill);
            Assert.AreEqual(203, config.missileId,
                "child missile presentation must remain available");
            Assert.IsTrue(config.hasStateAura,
                "state visual and child missile are independent PC namespaces");
        }

        [Test]
        public void NguHanhTran146_SeparatesStateVisualFromChildMissile()
        {
            var skill = Catalog.Resolve(146);

            Assert.IsNotNull(skill);
            Assert.AreEqual(PcSkillStyle.InitiativeNpcState, skill.skillStyle);
            Assert.AreEqual(29, skill.stateSpecialId);
            Assert.IsTrue(skill.isAura);
            Assert.AreEqual(SkillMissileForm.Stationary, skill.missileForm);
            Assert.AreEqual(226, skill.childSkillId);
            Assert.AreEqual(-1, skill.childSkillLevel);
            Assert.AreEqual(1, skill.childSkillNum);
            Assert.AreNotEqual(skill.childSkillId, skill.stateSpecialId,
                "state visual id and child missile id are separate PC namespaces");
        }

        [Test]
        public void AllResolvedBuffPassiveAndAuraRows_MatchCanonicalPcPresentationFields()
        {
            string path = Path.Combine(
                Application.streamingAssetsPath,
                "Reference",
                "PcAllFactionLearnedDisplaySkills.txt");
            var canonicalRows = PcConfigParser.ParseSkills(path);
            var failures = new List<string>();

            foreach (var expected in canonicalRows)
            {
                if (expected == null ||
                    (expected.skillStyle != PcSkillStyle.InitiativeNpcState &&
                     expected.skillStyle != PcSkillStyle.PassivityNpcState &&
                     expected.stateSpecialId <= 0 && !expected.isAura))
                    continue;

                var actual = Catalog.Resolve(expected.skillId);
                if (actual == null) continue;

                Compare(failures, expected.skillId, "style", (int)expected.skillStyle, (int)actual.skillStyle);
                Compare(failures, expected.skillId, "state", expected.stateSpecialId, actual.stateSpecialId);
                Compare(failures, expected.skillId, "aura", expected.isAura, actual.isAura);
                Compare(failures, expected.skillId, "radius", expected.attackRadius, actual.attackRadius);
                if (expected.missileForm != SkillMissileForm.None || expected.childSkillId <= 0)
                    Compare(failures, expected.skillId, "form", (int)expected.missileForm, (int)actual.missileForm);
                Compare(failures, expected.skillId, "child", expected.childSkillId, actual.childSkillId);
                Compare(failures, expected.skillId, "childLevel", expected.childSkillLevel, actual.childSkillLevel);
                Compare(failures, expected.skillId, "childNum", expected.childSkillNum, actual.childSkillNum);
                Compare(failures, expected.skillId, "base", expected.baseSkill, actual.baseSkill);
                Compare(failures, expected.skillId, "anim", expected.charAnimId, actual.charAnimId);
                Compare(failures, expected.skillId, "wait", expected.waitTime, actual.waitTime);
                Compare(failures, expected.skillId, "targetEnemy", expected.targetEnemy, actual.targetEnemy);
                Compare(failures, expected.skillId, "targetAlly", expected.targetAlly, actual.targetAlly);
                Compare(failures, expected.skillId, "targetSelf", expected.targetSelf, actual.targetSelf);
            }

            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        [Test]
        public void StateAuraSynchronization_IsScopedPerReceiverActor()
        {
            const int skillId = 990069;
            var catalog = new SkillCatalog();
            catalog.Register(StateAuraSkill(skillId));
            var first = Actor(101);
            var second = Actor(202);
            ApplyFiniteState(first, skillId);
            ApplyFiniteState(second, skillId);
            var visual = new SkillEffectVisualService(null, catalog);

            visual.SynchronizeStateAuras(first, Vector2.zero);
            visual.SynchronizeStateAuras(second, Vector2.one);
            Assert.AreEqual(2, visual.ActiveEffectCount,
                "syncing one receiver must not evict another receiver's aura");

            first.ExpireStateSources(18);
            visual.SynchronizeStateAuras(first, Vector2.zero);

            var remaining = visual.GetActiveEffects().Single();
            Assert.AreEqual(202, remaining.stateSourceKey.actorId);
        }

        [Test]
        public void SourceOwnedAura_DoesNotResetAnimationOrSelfExpireBeforeSourceRemoval()
        {
            const int skillId = 990070;
            var catalog = new SkillCatalog();
            catalog.Register(StateAuraSkill(skillId));
            var actor = Actor(303);
            ApplyFiniteState(actor, skillId);
            var visual = new SkillEffectVisualService(null, catalog);

            visual.SynchronizeStateAuras(actor, Vector2.zero);
            var effect = visual.GetActiveEffects().Single();
            visual.Update(0.75f);
            float elapsedBeforeSync = effect.elapsed;

            visual.SynchronizeStateAuras(actor, new Vector2(2f, 3f));
            Assert.AreEqual(elapsedBeforeSync, effect.elapsed, 0.0001f,
                "state reconciliation must not restart the PC aura frame loop");

            visual.Update(2f);
            Assert.AreEqual(1, visual.ActiveEffectCount,
                "source-owned lifetime is controlled by the combat state node");

            actor.ExpireStateSources(18);
            visual.SynchronizeStateAuras(actor, actor.position);
            Assert.AreEqual(0, visual.ActiveEffectCount);
        }

        private static SkillDefinition StateAuraSkill(int skillId)
        {
            var skill = new SkillDefinition
            {
                skillId = skillId,
                nameNormalized = "state-aura-test",
                maxLevel = 1,
                skillStyle = PcSkillStyle.InitiativeNpcState,
                stateSpecialId = 43,
                targetSelf = true,
            };
            var data = new SkillLevelData { level = 1 };
            data.state.Add(new SkillMagicAttribute(MagicAttributeKind.AllResP, 1, 18, 0));
            skill.pcLevelData.Add(data);
            return skill;
        }

        private static CombatActorState Actor(int actorId) => new CombatActorState
        {
            actorId = actorId,
            currentLife = 100,
            maxLife = 100,
            currentMana = 100,
            maxMana = 100,
            fightMode = true,
        };

        private static void ApplyFiniteState(CombatActorState actor, int skillId)
        {
            actor.ApplySkillStateSource(
                actor.actorId,
                skillId,
                1,
                new[] { new SkillMagicAttribute(MagicAttributeKind.AllResP, 1, 18, 0) });
        }

        private static void Compare<T>(
            ICollection<string> failures,
            int skillId,
            string field,
            T expected,
            T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                failures.Add($"{skillId}:{field} expected={expected} actual={actual}");
        }
    }
}
