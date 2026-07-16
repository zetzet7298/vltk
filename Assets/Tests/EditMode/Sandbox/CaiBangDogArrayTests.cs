using System.Linq;
using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    // [CaiBang-VersionPriority 2026-06-29] Newest PC Client/Server skills.txt agree:
    // skill 124 is passive dagou_zhen (SkillStyle=3, no aura), while child skill 209 is the stateSpecialId=44 aura projectile.
    [TestFixture, Category("CaiBang")]
    public class CaiBangDogArrayTests
    {
        private static readonly SkillCatalog _catalog = TestCatalogCache.NoviceAndCaiBang;
        private SkillCatalog Catalog() => _catalog;

        [Test]
        public void DogArray_124_UsesNewestPcPassiveDagouZhenConfig()
        {
            // PC newest skills.txt row 124: SkillStyle=3, IsAura=0, TargetAlly=0, TargetSelf=0,
            // AttackRadius=0, StateSpecialId=0, ChildSkillId=0, MisslesForm=7, CharAnimId=11.
            var s = Catalog().Resolve(124);
            Assert.IsNotNull(s);
            Assert.AreEqual(PcSkillStyle.PassivityNpcState, s.skillStyle);
            Assert.IsFalse(s.isAura);
            Assert.IsFalse(s.targetAlly);
            Assert.IsFalse(s.targetSelf);
            Assert.AreEqual(0, s.attackRadius);
            Assert.AreEqual(0, s.stateSpecialId);
            Assert.AreEqual(0, s.childSkillId);
            Assert.AreEqual(SkillMissileForm.None, s.missileForm);
            Assert.AreEqual(11, s.charAnimId);
        }

        [Test]
        public void DogArray_124_AddPhysicsDamageMatchesPcDagouZhenLua()
        {
            // [CaiBang-slistcache 2026-07-15] PC slistcache gaibang.lua::dagou_zhen:
            //   addphysicsdamage_p={{{1,53},{20,348},{21,369}},{{1,-1},{30,-1}},{{1,2},{2,2}}}
            var s = Catalog().Resolve(124);
            Assert.AreEqual("AddPhysicsDamageP=53,-1,2", s.GetPcLevelData(1).First(MagicAttributeKind.AddPhysicsDamageP).ToString());
            Assert.AreEqual("AddPhysicsDamageP=348,-1,2", s.GetPcLevelData(20).First(MagicAttributeKind.AddPhysicsDamageP).ToString());
        }

        [Test]
        public void DogArray_209_StateProjectileKeepsPcAuraConfig()
        {
            // PC newest skills.txt row 209 is the actual state projectile: StateSpecialId=44,
            // TargetAlly=1, TargetSelf=1, AttackRadius=180, ChildSkillId=92, ChildSkillNum=1,
            // CharAnimId=11, WaitTime=0.
            var s = Catalog().Resolve(PcCombatCatalogFactory.CaiBangDogBeatingAuraChild);
            Assert.IsNotNull(s);
            Assert.AreEqual(PcSkillStyle.Missiles, s.skillStyle);
            Assert.IsTrue(s.targetAlly);
            Assert.IsTrue(s.targetSelf);
            Assert.AreEqual(180, s.attackRadius);
            Assert.AreEqual(44, s.stateSpecialId);
            Assert.AreEqual(92, s.childSkillId);
            Assert.AreEqual(1, s.childSkillNum);
            Assert.AreEqual(11, s.charAnimId);
            Assert.AreEqual(0, s.waitTime);
            Assert.AreEqual("AddDefenseV=230,25,0", s.GetPcLevelData(20).First(MagicAttributeKind.AddDefenseV).ToString());
        }
    }
}
