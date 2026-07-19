// -----------------------------------------------------------------------------
// VLTK Mobile — state visual residual parity guard.
//
// Purpose: residual PC StateSpecialId rows beyond the loaded 1-49 state aura
// table must stay visually empty as state auras. style3.spr stub rows are not
// fallback art.
// Evidence: harness/docs/stories/SKL-ALL-PARITY-001/state-visual-residuals.md
// -----------------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture, Category("SkillVisual")]
    public sealed class StateVisualAbsentParityTests
    {
        private static readonly (int skillId, int stateId)[] ResidualPairs =
        {
            (15, 52),
            (90, 64),
            (174, 66),
            (175, 54),
            (177, 65),
            (273, 53),
            (277, 57),
            (282, 55),
            (332, 56),
            (356, 54),
            (364, 58),
            (391, 59),
            (392, 63),
            (393, 65),
            (394, 60),
            (716, 122),
            (720, 120),
        };

        private static string PcSkillsPath =>
            Path.Combine(Application.streamingAssetsPath, "Reference", "PcSkills.txt");

        [Test]
        public void ResidualRootStates_DoNotInventStateAuraSprites_WhileMappedControlStillAttaches()
        {
            Assert.AreEqual(17, ResidualPairs.Length, "guard covers every known residual root/state pair");
            CollectionAssert.AreEquivalent(
                new[] { 52, 53, 54, 55, 56, 57, 58, 59, 60, 63, 64, 65, 66, 120, 122 },
                ResidualPairs.Select(pair => pair.stateId).Distinct().OrderBy(id => id).ToArray(),
                "guard covers every unique residual StateSpecialId");

            var parsedSkills = PcConfigParser.ParseSkills(PcSkillsPath);
            var mapper = new PcSkillVisualAutoMapper();
            mapper.Initialize(Application.streamingAssetsPath);
            var service = new SkillEffectVisualService(null);

            foreach (var (skillId, stateId) in ResidualPairs)
            {
                var skill = parsedSkills.FirstOrDefault(candidate =>
                    candidate.skillId == skillId && candidate.stateSpecialId == stateId);
                Assert.IsNotNull(skill, $"PcSkills.txt must contain skill {skillId} with StateSpecialId {stateId}");

                var aura = PcSkillVisualAutoMapper.GetStateAuraData(stateId);
                Assert.IsTrue(string.IsNullOrEmpty(aura.sprPath), $"state {stateId} must stay unmapped; no style3.spr fallback");

                var config = mapper.GetVisualConfig(skill);
                Assert.IsNotNull(config, $"skill {skillId} visual config");
                Assert.IsFalse(config.hasStateAura, $"skill {skillId}/state {stateId} must not attach a state aura");
                Assert.IsTrue(string.IsNullOrEmpty(config.stateAuraSprPath), $"skill {skillId}/state {stateId} must not invent stateAuraSprPath");
                AssertNoStyle3(config.preCastSprPath, $"skill {skillId} precast");
                AssertNoStyle3(config.flightSprPath, $"skill {skillId} flight");
                AssertNoStyle3(config.explodeSprPath, $"skill {skillId} impact");

                var fx = service.PlaySkillCast(skill, Vector2.zero, Vector2.right * 64f, 20);
                Assert.IsNotNull(fx, $"skill {skillId} effect");
                Assert.IsFalse(fx.isAura, $"skill {skillId}/state {stateId} must not become a Unity state aura");
                Assert.AreEqual(0, fx.pcAuraFrameStart, $"skill {skillId}/state {stateId} aura frame start");
                Assert.AreEqual(0, fx.pcAuraFrameEnd, $"skill {skillId}/state {stateId} aura frame end");
                Assert.AreEqual(0, fx.stateAuraPos, $"skill {skillId}/state {stateId} aura position");
                AssertNoStyle3(fx.pcPreCastSpriteKey, $"skill {skillId} Unity precast key");
                AssertNoStyle3(fx.pcMissileSpriteKey, $"skill {skillId} Unity missile key");
                AssertNoStyle3(fx.pcImpactSpriteKey, $"skill {skillId} Unity impact key");
            }

            var mapped = PcSkillVisualAutoMapper.GetStateAuraData(44);
            Assert.AreEqual(@"\spr\skill\丐帮\mag_gb_12_打狗阵.spr", mapped.sprPath, "positive control state 44 remains mapped");

            var mappedConfig = mapper.GetVisualConfig(new SkillDefinition { skillId = 900044, stateSpecialId = 44 });
            Assert.IsTrue(mappedConfig.hasStateAura, "mapped control config must still attach state aura");
            Assert.AreEqual(mapped.sprPath, mappedConfig.stateAuraSprPath, "mapped control config path");

            var mappedFx = service.PlaySkillCast(new SkillDefinition { skillId = 900044, stateSpecialId = 44 }, Vector2.zero, Vector2.zero, 20);
            Assert.IsTrue(mappedFx.isAura, "mapped control effect must still become a Unity aura");
            Assert.AreEqual(mapped.sprPath, mappedFx.pcPreCastSpriteKey, "mapped control Unity key");
            Assert.AreEqual(8, mappedFx.pcPreCastTotalFrames, "mapped control frame count");
        }

        private static void AssertNoStyle3(string value, string label)
        {
            Assert.IsFalse((value ?? string.Empty).IndexOf("style3.spr", StringComparison.OrdinalIgnoreCase) >= 0,
                $"{label} must not reference non-loader style3.spr stub");
        }
    }
}
