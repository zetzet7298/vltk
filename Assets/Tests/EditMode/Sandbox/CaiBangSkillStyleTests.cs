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
    //   124 Đả Cẩu Trận (dagou_zhen)               → 2 InitiativeNpcState (stance aura)
    //   125 Thiên Hạ Vô Cẩu (tianxia_wugou)         → 0 Skill (damage, missile cast)
    //   127 Hoạt Bất Lưu Thủ (huabu_liushou)        → 3 PassivityNpcState
    //   128 Kháng Long Hữu Hối (kanglong_youhui)    → 0 Skill (damage, missile cast)
    public class CaiBangSkillStyleTests
    {
        private SkillCatalog Catalog() => PcCombatCatalogFactory.CreateNoviceAndCaiBangCatalog();

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
        public void DogArray_StanceIsAura_TargetAllyTrue_WaitTimeZero()
        {
            var cat = Catalog();
            var s = cat.Resolve(124);
            Assert.IsNotNull(s, "skill 124 missing");
            Assert.AreEqual(PcSkillStyle.InitiativeNpcState, s.skillStyle, "124 PC SkillStyle=2");
            Assert.IsTrue(s.isAura, "124 PC IsAura=1");
            Assert.IsTrue(s.targetAlly, "124 PC TargetAlly=1");
            Assert.IsTrue(s.targetSelf, "124 PC TargetSelf=1");
            Assert.AreEqual(0, s.waitTime, "124 PC WaitTime=0 (immediate state apply)");
            Assert.AreEqual(44, s.stateSpecialId, "124 PC StateSpecialId=44");
            Assert.AreEqual(180, s.attackRadius, "124 PC AttackRadius=180");
            Assert.AreEqual(209, s.childSkillId, "124 PC ChildSkillId=209 (打狗阵子弹)");
        }

        [Test]
        public void HuocBatLuuThu_PassiveState_PcSkillStyle3()
        {
            var cat = Catalog();
            var s = cat.Resolve(127);
            Assert.IsNotNull(s, "skill 127 missing");
            Assert.AreEqual(PcSkillStyle.PassivityNpcState, s.skillStyle, "127 PC SkillStyle=3 (PassivityNpcState)");
            Assert.AreEqual(17, s.stateSpecialId, "127 PC StateSpecialId=17");
        }

        [Test]
        public void DogArray_Level20_DefenseBuffAtPcMagnitude()
        {
            // PC 打狗阵.lua::Getadddefense_v(level): result = 30+10*level → L20 = 230.
            var cat = Catalog();
            var s = cat.Resolve(124);
            var levelData = s.GetPcLevelData(20);
            var attr = levelData.state.FirstOrDefault(a => a.kind == MagicAttributeKind.AddDefenseV);
            Assert.IsNotNull(attr, "L20 state has AddDefenseV");
            Assert.AreEqual(230, attr.value1, "PC adddefense_v L20 = 30+10*20 = 230");
            Assert.AreEqual(25, attr.value2, "PC param2=25 (mana cost baked into state)");
        }

        [Test]
        public void TianxiaWugou_NpcVariant1539_SameShapeAsPlayer125()
        {
            // PC 1539 (NPC variant) shares skill form với 125 nhưng req level 1, max level 60.
            var cat = Catalog();
            var p = cat.Resolve(125);
            var n = cat.Resolve(1539);
            Assert.IsNotNull(n, "skill 1539 missing");
            Assert.AreEqual(p.missileForm, n.missileForm, "1539 missileForm = 125 missileForm (Surround)");
            Assert.AreEqual(0, n.childSkillNum, "1539 childSkillNum = 0 (PC runtime uses Lua skill_misslenum_v)");
        }
    }
}
