using System;
using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class GoldenSnapshotCaptureDriverTests
    {
        [Test]
        public void ResolveSkillFxLayer_FailsClosedForMissingNamedLayer_AndAcceptsInjection()
        {
            Assert.Throws<InvalidOperationException>(() =>
                GoldenSnapshotCaptureDriver.ResolveSkillFxLayer("__missing_skill_fx_layer__"));
            Assert.AreEqual(8, GoldenSnapshotCaptureDriver.ResolveSkillFxLayer(injectedLayer: 8));
        }

        [Test]
        public void CaptureActive_FailsClosedForIdentityAndMissingEffect()
        {
            var effects = new SkillEffectVisualService(null);
            Assert.Throws<ArgumentException>(() =>
                GoldenSnapshotCaptureDriver.CaptureActive(effects, "", "case", "TangMen", 1, 1, injectedLayer: 8));
            Assert.Throws<InvalidOperationException>(() =>
                GoldenSnapshotCaptureDriver.CaptureActive(effects, "79", "case", "TangMen", 1, 1, injectedLayer: 8));
        }

        [Test]
        public void FocusFor_UsesActivePhaseWorldPosition()
        {
            var missile = new ActiveSkillEffect
            {
                phase = SkillEffectPhase.Missile,
                currentMissilePos = new Vector2(3f, 4f),
                missilePositions = new[] { new Vector2(9f, 10f) },
            };
            Assert.AreEqual(new Vector2(3f, 4f), GoldenSnapshotCaptureDriver.FocusFor(missile));

            missile.pcImpactSpriteKey = "impact.spr";
            missile.pcImpactTotalFrames = 1;
            Assert.AreEqual(new Vector2(9f, 10f), GoldenSnapshotCaptureDriver.FocusFor(missile));

            missile.phase = SkillEffectPhase.Impact;
            missile.targetPos = new Vector2(11f, 12f);
            Assert.AreEqual(missile.targetPos, GoldenSnapshotCaptureDriver.FocusFor(missile));
        }
    }
}
