using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public sealed class TangMenLuaLevelServiceTests
    {
        [SetUp] public void SetUp() => PcTangMenLuaLevelService.Reset();

        [Test]
        public void BaoyuLihua_FlyGateUsesPcDuplicateBreakpointSemantics_And301()
        {
            Assert.IsFalse(PcTangMenLuaLevelService.FlyEnabled(302, 9));
            // PC Link() chooses the left segment at the duplicate x=10 mark;
            // the 0->1 gate becomes active at level 11.
            Assert.IsFalse(PcTangMenLuaLevelService.FlyEnabled(302, 10));
            Assert.IsTrue(PcTangMenLuaLevelService.FlyEnabled(302, 11));
            Assert.AreEqual(30, PcTangMenLuaLevelService.FlyInterval(302, 11));
            Assert.AreEqual(301, PcTangMenLuaLevelService.FlySkillId(302, 20));
            Assert.AreEqual(20, PcTangMenLuaLevelService.EventSkillLevel(302, 20));
        }

        [Test]
        public void Nutang150_FlyGateAndChild1098MatchPcLua()
        {
            Assert.IsFalse(PcTangMenLuaLevelService.FlyEnabled(1070, 10));
            Assert.IsTrue(PcTangMenLuaLevelService.FlyEnabled(1070, 11));
            Assert.AreEqual(18, PcTangMenLuaLevelService.FlyInterval(1070, 20));
            Assert.AreEqual(1098, PcTangMenLuaLevelService.FlySkillId(1070, 20));
        }

        [Test]
        public void UnknownOrAbsentEventFailsClosed()
        {
            Assert.IsFalse(PcTangMenLuaLevelService.Applies(999999));
            Assert.AreEqual(0, PcTangMenLuaLevelService.FlySkillId(999999, 20));
            Assert.AreEqual(0, PcTangMenLuaLevelService.VanishSkillId(302, 20));
        }
    }
}
