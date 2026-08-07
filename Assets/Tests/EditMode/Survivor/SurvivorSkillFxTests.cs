// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Survivor -- SurvivorSkillFxTests
// Verifies SurvivorSkillFx.BuildSkillDefinition correctly maps Survivor SkillDef
// -> SkillDefinition for PlaySkillCast (4 active Cai Bang skills: 128/125/1073/1074).
// PC source verified (PcSkills.txt col 19/20/22/27):
//   128: Form=2(Fan)    child=48  WaitTime=5
//   125: Form=3(Surround) child=47  WaitTime=5
//   1073: Form=1(Single) child=335 WaitTime=5
//   1074: Form=1(Single) child=336 WaitTime=5
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Model;
using VLTK.Survivor;

namespace VLTK.Tests.Survivor
{
    public class SurvivorSkillFxTests
    {
        private static SkillDef MakeCbDef(int id, int form, int childId, int childNum, int waitTime)
        {
            var def = SkillDef.FromRow(new SkillRow
            {
                Id = id,
                Form = form,
                ChildMissileId = childId,
                ChildSkillNum = childNum,
                WaitTime = waitTime,
            });
            return def;
        }

        // ------------------------------------------------------------------
        // MapMissileForm parity
        // ------------------------------------------------------------------

        [Test]
        public void MapMissileForm_Form0_None()
        {
            Assert.AreEqual(SkillMissileForm.None, SurvivorSkillFx.MapMissileForm(0));
        }

        [Test]
        public void MapMissileForm_Form12_Melee_None()
        {
            // Form 12 (melee) is outside the enum -> None
            Assert.AreEqual(SkillMissileForm.None, SurvivorSkillFx.MapMissileForm(12));
        }

        [Test]
        public void MapMissileForm_Forms1to7_MatchEnum()
        {
            Assert.AreEqual(SkillMissileForm.Single, SurvivorSkillFx.MapMissileForm(1));
            Assert.AreEqual(SkillMissileForm.Fan, SurvivorSkillFx.MapMissileForm(2));
            Assert.AreEqual(SkillMissileForm.Surround, SurvivorSkillFx.MapMissileForm(3));
            Assert.AreEqual(SkillMissileForm.Chain, SurvivorSkillFx.MapMissileForm(4));
            Assert.AreEqual(SkillMissileForm.Zone, SurvivorSkillFx.MapMissileForm(5));
            Assert.AreEqual(SkillMissileForm.Stance, SurvivorSkillFx.MapMissileForm(6));
            Assert.AreEqual(SkillMissileForm.Stationary, SurvivorSkillFx.MapMissileForm(7));
        }

        // ------------------------------------------------------------------
        // SkillDefinition build parity (4 active Cai Bang skills)
        // ------------------------------------------------------------------

        [Test]
        public void BuildDef_Skill128_KangLong()
        {
            var def = MakeCbDef(128, 2, 48, 8, 5);
            var sd = SurvivorSkillFx.BuildSkillDefinition(def);
            Assert.AreEqual(128, sd.skillId);
            Assert.AreEqual(48, sd.childSkillId);
            Assert.AreEqual(SkillMissileForm.Fan, sd.missileForm);
            Assert.AreEqual(5, sd.waitTime);
            Assert.IsTrue(sd.HasMissile, "Form 2 Fan -> HasMissile");
        }

        [Test]
        public void BuildDef_Skill125_BangDa()
        {
            var def = MakeCbDef(125, 3, 47, 16, 5);
            var sd = SurvivorSkillFx.BuildSkillDefinition(def);
            Assert.AreEqual(125, sd.skillId);
            Assert.AreEqual(47, sd.childSkillId);
            Assert.AreEqual(SkillMissileForm.Surround, sd.missileForm);
            Assert.AreEqual(5, sd.waitTime);
            Assert.IsTrue(sd.HasMissile, "Form 3 Surround -> HasMissile");
        }

        [Test]
        public void BuildDef_Skill1073_ZhangGaiBang()
        {
            var def = MakeCbDef(1073, 1, 335, 1, 5);
            var sd = SurvivorSkillFx.BuildSkillDefinition(def);
            Assert.AreEqual(1073, sd.skillId);
            Assert.AreEqual(335, sd.childSkillId);
            Assert.AreEqual(SkillMissileForm.Single, sd.missileForm);
            Assert.AreEqual(5, sd.waitTime);
            Assert.IsTrue(sd.HasMissile, "Form 1 Single -> HasMissile");
        }

        [Test]
        public void BuildDef_Skill1074_GunGaiBang()
        {
            var def = MakeCbDef(1074, 1, 336, 5, 5);
            var sd = SurvivorSkillFx.BuildSkillDefinition(def);
            Assert.AreEqual(1074, sd.skillId);
            Assert.AreEqual(336, sd.childSkillId);
            Assert.AreEqual(SkillMissileForm.Single, sd.missileForm);
            Assert.AreEqual(5, sd.waitTime);
            Assert.IsTrue(sd.HasMissile, "Form 1 Single -> HasMissile");
        }

        // ------------------------------------------------------------------
        // Melee fail-closed
        // ------------------------------------------------------------------

        [Test]
        public void BuildDef_PreCastUid_MapsToEffectSourceId()
        {
            var def = SkillDef.FromRow(new SkillRow
            {
                Id = 128, Form = 2, ChildMissileId = 48, ChildSkillNum = 8, WaitTime = 5,
                PreCastSprUid = "b91ab706",
            });
            var sd = SurvivorSkillFx.BuildSkillDefinition(def);
            Assert.IsNotNull(sd.effectSourceId, "staged precast uid -> effectSourceId");
            Assert.AreEqual("b91ab706", sd.effectSourceId.sourcePath);
        }

        [Test]
        public void BuildDef_NoPreCastUid_NoEffectSource()
        {
            var def = MakeCbDef(128, 2, 48, 8, 5);
            var sd = SurvivorSkillFx.BuildSkillDefinition(def);
            Assert.IsNull(sd.effectSourceId, "unstaged precast -> fail-closed null");
        }

        [Test]
        public void BuildDef_MeleeForm12_NoMissile()
        {
            var def = MakeCbDef(999, 12, 500, 1, 5);
            var sd = SurvivorSkillFx.BuildSkillDefinition(def);
            Assert.AreEqual(SkillMissileForm.None, sd.missileForm, "Form 12 -> None");
            Assert.IsFalse(sd.HasMissile, "melee -> no missile");
        }
    }
}