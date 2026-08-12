using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;
using VLTK.SkillPort;
using VLTK.UI;

namespace VLTK.Tests.Sandbox
{
    [TestFixture, Category("RuntimeFactionSwitch")]
    public sealed class RuntimeFactionSwitchTests
    {
        private static SkillCatalog Catalog => TestCatalogCache.NoviceAndAllSect;

        [Test]
        public void ProgressionSwitch_ReplacesOldFactionAndMaxesNewFactionSkills()
        {
            var progression = new PlayerProgressionState();
            progression.GrantFactionSkillPanelProgression(Catalog, CombatFaction.TangMen);
            progression.MaxAllSkillLevels(Catalog);
            int tangMenOnly = progression.knownSkills.First(id =>
                Catalog.Resolve(id)?.faction == CombatFaction.TangMen);

            progression.GrantFactionSkillPanelProgression(Catalog, CombatFaction.WuDang);
            progression.MaxAllSkillLevels(Catalog);
            progression.knownSkills.Add(tangMenOnly);
            progression.skillLevels[tangMenOnly] = 9;
            progression.ReplaceFactionSkillPanelProgression(Catalog, CombatFaction.WuDang);
            progression.MaxAllSkillLevels(Catalog);

            Assert.AreEqual(CombatFaction.WuDang, progression.faction);
            Assert.IsFalse(progression.knownSkills.Contains(tangMenOnly));
            Assert.IsTrue(progression.knownSkills.Count > 1);
            foreach (int skillId in progression.knownSkills)
            {
                var skill = Catalog.Resolve(skillId);
                Assert.IsNotNull(skill, $"Missing learned skill {skillId}");
                Assert.IsTrue(
                    skill.faction == CombatFaction.WuDang ||
                    skillId == PcCombatCatalogFactory.UniversalLightnessSkill ||
                    skill.isLeapSkill,
                    $"Cross-faction skill leaked after switch: {skillId}/{skill.faction}");
                Assert.Greater(progression.GetSkillLevel(skillId), 0, $"Skill {skillId} must be immediately castable");
            }
        }

        [Test]
        public void PresentationReducerClear_PreservesSessionSequenceButDropsFactionTransientState()
        {
            var reducer = new CombatPresentationReducer();
            Assert.IsTrue(reducer.ApplySnapshot(new CombatPresentationSnapshot
            {
                serverSequence = 12,
                baselineTick = 34,
                casts = { new ActiveCastPresentation { castId = "old-cast", skillId = 77 } },
            }));

            reducer.ClearTransientState();

            Assert.AreEqual(12ul, reducer.lastServerSequence);
            Assert.AreEqual(34ul, reducer.baselineTick);
            Assert.IsTrue(reducer.hasBaseline);
            Assert.IsEmpty(reducer.casts);
            Assert.IsEmpty(reducer.missiles);
            Assert.IsEmpty(reducer.statuses);
        }

        [Test]
        public void CombatActorClear_RemovesSkillStateProjection()
        {
            var actor = new CombatActorState { actorId = 7 };
            actor.ApplySkillStateSource(
                actor.actorId,
                123,
                1,
                new[] { new SkillMagicAttribute(MagicAttributeKind.AttackSpeedV, 5, 30, 0) });

            Assert.IsNotEmpty(actor.stateSources);
            Assert.IsNotEmpty(actor.states);

            actor.ClearSkillStateSources();

            Assert.IsEmpty(actor.stateSources);
            Assert.IsEmpty(actor.states);
            Assert.AreEqual(0, actor.GetStateValue(MagicAttributeKind.AttackSpeedV));
        }

        [Test]
        public void RuntimeReset_ClearsOnlySelectedActorCooldownsAndAllActiveEffects()
        {
            const int skillId = 990201;
            var skill = new SkillDefinition
            {
                skillId = skillId,
                nameNormalized = "runtime-faction-switch-probe",
                maxLevel = 1,
                skillStyle = PcSkillStyle.Missiles,
                attackRadius = 100,
                targetEnemy = true,
                timePerCast = 20,
            };
            skill.pcLevelData.Add(new SkillLevelData { level = 1 });
            var catalog = new SkillCatalog();
            catalog.Register(skill);
            var runtime = new CombatRuntimeService(catalog);
            var caster = new CombatActorState
            {
                actorId = 1,
                fightMode = true,
                currentMana = 100,
                knownSkills = { skillId },
                skillLevels = { [skillId] = 1 },
                position = Vector2.zero,
            };
            var target = new CombatActorState
            {
                actorId = 2,
                currentLife = 100,
                maxLife = 100,
                position = Vector2.right,
            };

            Assert.IsTrue(runtime.Cast(caster, target, skillId, target.position, CombatRelation.Enemy).success);
            Assert.Greater(runtime.NextCastTime(caster.actorId, skillId), 0);
            Assert.AreEqual(1, runtime.ResetActorCooldowns(caster.actorId));
            Assert.AreEqual(0, runtime.NextCastTime(caster.actorId, skillId));

            var visuals = new SkillEffectVisualService(new VLTK.Sprites.SprRuntimeService(), catalog);
            visuals.PlayHitFlash(Vector2.zero, Color.white);
            Assert.AreEqual(1, visuals.ClearActiveEffects());
            Assert.AreEqual(0, visuals.ActiveEffectCount);
        }

        [Test]
        public void HotbarReset_FillsPrimaryAndFiveRegularsAndClearsDeckB()
        {
            var progression = new PlayerProgressionState();
            progression.GrantFactionSkillPanelProgression(Catalog, CombatFaction.WuDang);
            progression.MaxAllSkillLevels(Catalog);
            var expected = EligibleHotbarDefaults(Catalog, progression, CombatFaction.WuDang);
            Assert.GreaterOrEqual(expected.Length, CombatSkillSlotController.MobileSkillSlotCount + 1);

            var go = new GameObject("RuntimeFactionSwitchHotbarTest");
            try
            {
                var controller = go.AddComponent<CombatSkillSlotController>();
                controller.Initialize(Catalog, progression);
                controller.AssignPrimarySkill(123455);
                controller.AssignSkill(0, 123456);
                controller.AssignSkill(1, 123457);
                controller.ToggleDeck();
                controller.AssignPrimarySkill(123459);
                controller.AssignSkill(0, 123458);
                controller.LockTarget(99, "stale-target");

                controller.ResetForRuntimeFaction(CombatFaction.WuDang);

                Assert.AreEqual(0, controller.ActiveDeckIndex);
                Assert.AreEqual(-1, controller.LockedTargetId);
                Assert.AreEqual(expected[0], controller.GetAssignedPrimarySkill(0));
                Assert.AreEqual(0, controller.GetAssignedPrimarySkill(1), "deck B primary must clear");
                for (int slot = 0; slot < CombatSkillSlotController.MobileSkillSlotCount; slot++)
                {
                    Assert.AreEqual(expected[slot + 1], controller.GetAssignedSkill(slot, 0));
                    Assert.AreEqual(0, controller.GetAssignedSkill(slot, 1));
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void HotbarReset_RepeatedFactionChangesChangePrimarySkill()
        {
            var progression = new PlayerProgressionState();
            var go = new GameObject("RuntimeFactionSwitchHotbarPrimaryChangeTest");
            try
            {
                var controller = go.AddComponent<CombatSkillSlotController>();
                controller.Initialize(Catalog, progression);

                progression.ReplaceFactionSkillPanelProgression(Catalog, CombatFaction.WuDang);
                progression.MaxAllSkillLevels(Catalog);
                controller.ResetForRuntimeFaction(CombatFaction.WuDang);
                int wuDangPrimary = controller.GetAssignedPrimarySkill(0);

                progression.ReplaceFactionSkillPanelProgression(Catalog, CombatFaction.Shaolin);
                progression.MaxAllSkillLevels(Catalog);
                controller.ResetForRuntimeFaction(CombatFaction.Shaolin);
                int shaolinPrimary = controller.GetAssignedPrimarySkill(0);

                Assert.Greater(wuDangPrimary, 0);
                Assert.Greater(shaolinPrimary, 0);
                Assert.AreNotEqual(wuDangPrimary, shaolinPrimary);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void DefaultHotbar_AllCatalogFactionsHaveSixEligibleLearnedActiveSkills()
        {
            var shortFactions = System.Enum.GetValues(typeof(CombatFaction))
                .Cast<CombatFaction>()
                .Where(faction => faction != CombatFaction.None)
                .Where(faction =>
                {
                    var progression = new PlayerProgressionState();
                    progression.GrantFactionSkillPanelProgression(Catalog, faction);
                    progression.MaxAllSkillLevels(Catalog);
                    return EligibleHotbarDefaults(Catalog, progression, faction).Length < CombatSkillSlotController.MobileSkillSlotCount + 1;
                })
                .ToArray();

            CollectionAssert.IsEmpty(shortFactions);
        }

        private static int[] EligibleHotbarDefaults(SkillCatalog catalog, PlayerProgressionState progression, CombatFaction faction)
            => PcSkillPanelService.GetPcSkillOrder(faction)
                .Where(skillId => skillId > 0)
                .Where(skillId => !PcSkillPanelService.IsNpcVariant(skillId))
                .Where(skillId => progression.GetSkillLevel(skillId) > 0)
                .Where(skillId =>
                {
                    var skill = catalog.Resolve(skillId);
                    return skill != null
                        && skill.faction == faction
                        && skill.skillStyle != PcSkillStyle.PassivityNpcState;
                })
                .Distinct()
                .Take(CombatSkillSlotController.MobileSkillSlotCount + 1)
                .ToArray();
    }
}
