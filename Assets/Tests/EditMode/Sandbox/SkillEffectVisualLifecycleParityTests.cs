// -----------------------------------------------------------------------------
// VLTK Mobile — SkillEffectVisualService lifecycle parity tests.
// Proof: active missile, stationary/zone, state aura persistence, and passive
// no-visual fail-closed timing from PC-derived catalog rows.
// -----------------------------------------------------------------------------
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture, Category("Sandbox")]
    public class SkillEffectVisualLifecycleParityTests
    {
        private static SkillCatalog Catalog() => TestCatalogCache.NoviceAndCoreSect;

        [Test]
        public void ActiveMissile_117_AdvancesFromPreCastToMissile()
        {
            var catalog = Catalog();
            var skill = catalog.Resolve(117);
            Assert.IsNotNull(skill);
            Assert.AreEqual(SkillMissileForm.Single, skill.missileForm, "117 PC row is active missile cast");

            var service = new SkillEffectVisualService(null, catalog);
            var fx = service.PlaySkillCast(skill, Vector2.zero, new Vector2(100f, 0f), 20);

            Assert.IsNotNull(fx);
            Assert.AreEqual(SkillEffectPhase.PreCast, fx.phase);
            service.Update(1f);
            Assert.AreEqual(SkillEffectPhase.Missile, fx.phase);
            Assert.Greater(service.ActiveEffectCount, 0);
        }

        [Test]
        public void StationaryZone_358_AdvancesIntoImpactLifecycle()
        {
            var catalog = Catalog();
            var skill = catalog.Resolve(358);
            Assert.IsNotNull(skill);
            Assert.AreEqual(SkillMissileForm.Stationary, skill.missileForm, "358 PC row is stationary zone");

            var service = new SkillEffectVisualService(null, catalog);
            var fx = service.PlaySkillCast(skill, Vector2.zero, new Vector2(50f, 0f), 20);

            Assert.IsNotNull(fx);
            Assert.AreEqual(0, fx.missileCount, "stationary row keeps missile count zero");
            Assert.AreEqual(SkillEffectPhase.PreCast, fx.phase);
            service.Update(1f);
            Assert.AreEqual(SkillEffectPhase.Impact, fx.phase);
        }

        [Test]
        public void StateAura_130_UsesFinitePcStateDuration()
        {
            var catalog = Catalog();
            var skill = catalog.Resolve(130);
            Assert.IsNotNull(skill);
            Assert.AreEqual(43, skill.stateSpecialId, "130 PC row maps to state aura 43");

            var service = new SkillEffectVisualService(null, catalog);
            var fx = service.PlaySkillCast(skill, Vector2.zero, Vector2.zero, 20);

            Assert.IsNotNull(fx);
            Assert.IsTrue(fx.isAura);
            float expectedDuration = skill.GetPcLevelData(20).state.Max(a => a.value2) / 18f;
            Assert.AreEqual(expectedDuration, fx.auraDuration, 0.001f);
            Assert.AreEqual(expectedDuration, fx.preCastDuration, 0.001f);
            Assert.AreEqual(SkillEffectPhase.PreCast, fx.phase);

            service.Update(10f);
            Assert.AreEqual(SkillEffectPhase.PreCast, fx.phase);
            Assert.AreEqual(1, service.ActiveEffectCount);

            service.Update(expectedDuration);
            Assert.AreEqual(0, service.ActiveEffectCount);
        }

        [Test]
        public void PassiveNoVisual_115_FailsClosedAndCleansUp()
        {
            var catalog = Catalog();
            var skill = catalog.Resolve(115);
            Assert.IsNotNull(skill);
            Assert.AreEqual(PcSkillStyle.PassivityNpcState, skill.skillStyle, "115 PC row is passive/no visual");

            var service = new SkillEffectVisualService(null, catalog);
            var fx = service.PlaySkillCast(skill, Vector2.zero, Vector2.zero, 20);

            Assert.IsNotNull(fx);
            Assert.AreEqual(SkillEffectPhase.Finished, fx.phase, "no-visual passive must fail closed");
            Assert.AreEqual(0f, fx.preCastDuration);
            Assert.AreEqual(0f, fx.impactDuration);
            Assert.AreEqual(0f, fx.missileDuration);
            Assert.AreEqual(0, fx.missileCount);

            service.Update(0f);
            Assert.AreEqual(0, service.ActiveEffectCount);
        }
    }
}
