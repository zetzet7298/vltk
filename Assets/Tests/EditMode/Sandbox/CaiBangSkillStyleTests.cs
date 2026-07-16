using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    // [CaiBang-SkillStyle 2026-06-19] Phase A.1-A.2 + A.4: PC SkillStyle parity cho 6 skill IDs.
    // PC Skills.txt col 3 (SkillStyle):
    //   117 Đầu Thạch Vấn Lộ (toushi_wenlu)         → 0 Skill (damage, missile cast)
    //   119 Diên Môn Thác Bát (yanmen_tuobo)        → 0 Skill (damage, missile cast)
    //   122 Kiến Nhân Thần Thủ (jianren_shenshou)   → 0 Skill (damage, missile cast)
    //   124 Đả Cẩu Bổng Pháp (dagou_zhen)         → 3 PassivityNpcState (newest PC passive)
    //   125 Bổng Đả Ác Cẩu (bangda_egou)           → 0 Skill (damage, missile cast)
    //   127 Hoạt Bất Lưu Thủ (huabu_liushou)        → 3 PassivityNpcState
    //   128 Kháng Long Hữu Hối (kanglong_youhui)    → 0 Skill (damage, missile cast)
    [TestFixture, Category("CaiBang")]
    public class CaiBangSkillStyleTests
    {
        private static readonly SkillCatalog _catalog = TestCatalogCache.NoviceAndCaiBang;
        private SkillCatalog Catalog() => _catalog;

        [Test]
        public void DamageSkills_UseMissiles_PcSkillStyle0()
        {
            var cat = Catalog();
            int[] damageSkillIds = { 117, 119, 122, 125, 128 };
            foreach (int id in damageSkillIds)
            {
                var s = cat.Resolve(id);
                Assert.IsNotNull(s, $"skill {id} missing from catalog");
                // PC Skills.txt SkillStyle=0 (Skill) cho 117/119/122/125/128. Unity maps tới PcSkillStyle.Missiles.
                Assert.AreEqual(PcSkillStyle.Missiles, s.skillStyle, $"skill {id} PC SkillStyle=0 → Missiles");
                Assert.IsFalse(s.isMelee, $"skill {id} PC IsMelee=0");
            }
        }

        [Test]
        public void DogArray_124_IsNewestPcPassive_NotAura()
        {
            var cat = Catalog();
            var s = cat.Resolve(124);
            Assert.IsNotNull(s, "skill 124 missing");
            Assert.AreEqual(PcSkillStyle.PassivityNpcState, s.skillStyle, "124 newest PC SkillStyle=3");
            Assert.IsFalse(s.isAura, "124 newest PC IsAura=0");
            Assert.IsFalse(s.targetAlly, "124 newest PC TargetAlly=0");
            Assert.IsFalse(s.targetSelf, "124 newest PC TargetSelf=0");
            Assert.AreEqual(0, s.waitTime, "124 newest PC WaitTime=0/default");
            Assert.AreEqual(0, s.stateSpecialId, "124 newest PC StateSpecialId=0");
            Assert.AreEqual(0, s.attackRadius, "124 newest PC AttackRadius=0");
            Assert.AreEqual(0, s.childSkillId, "124 newest PC ChildSkillId=0");
        }

        [Test]
        public void HuocBatLuuThu_ActiveCastStyle_PcSkillStyle0()
        {
            // PC slistcache ec1243ff.dat skill 127 (authoritative, verified 2026-06-30):
            //   SkillStyle=0 (active cast self-buff), NOT passive. Old test claimed Style=3 — was wrong.
            var cat = Catalog();
            var s = cat.Resolve(127);
            Assert.IsNotNull(s, "skill 127 missing");
            Assert.AreEqual(PcSkillStyle.InitiativeNpcState, s.skillStyle, "127 PC SkillStyle=0 (active cast buff)");
            Assert.AreEqual(17, s.stateSpecialId, "127 PC StateSpecialId=17");
        }

        [Test]
        public void DogArray_124_Level20_AddPhysicsDamageAtPcMagnitude()
        {
            // [CaiBang-slistcache 2026-07-15] PC slistcache dagou_zhen addphysicsdamage_p L20 = 348, duration=-1, param3=2.
            var cat = Catalog();
            var s = cat.Resolve(124);
            var levelData = s.GetPcLevelData(20);
            var attr = levelData.state.FirstOrDefault(a => a.kind == MagicAttributeKind.AddPhysicsDamageP);
            Assert.IsNotNull(attr, "L20 state has AddPhysicsDamageP");
            Assert.AreEqual(348, attr.value1, "PC slistcache addphysicsdamage_p L20 = 348");
            Assert.AreEqual(-1, attr.value2, "PC duration sentinel = -1");
            Assert.AreEqual(2, attr.value3, "PC param3=2");
            // slistcache NEW: lifemax_yan_p 1→50 (L50).
            var life = levelData.state.FirstOrDefault(a => a.kind == MagicAttributeKind.LifeMaxYanP);
            Assert.IsNotNull(life, "slistcache dagou_zhen lifemax_yan_p");
        }

        [Test]
        public void TianxiaWugou_NpcVariant1539_SameShapeAsPlayer359()
        {
            // PC 1539 (NPC variant) shares Thiên Hạ Vô Cẩu form with player skill 359.
            var cat = Catalog();
            var p = cat.Resolve(359);
            var n = cat.Resolve(1539);
            Assert.IsNotNull(n, "skill 1539 missing");
            Assert.AreEqual(p.missileForm, n.missileForm, "1539 missileForm = 359 missileForm (Single + Lua count)");
            Assert.AreEqual(p.childSkillId, n.childSkillId, "1539 child missile = 359 child missile 168");
        }
    }
}
