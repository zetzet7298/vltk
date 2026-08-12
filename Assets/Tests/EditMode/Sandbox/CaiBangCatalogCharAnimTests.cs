using System.Linq;
using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    // [CaiBang-Catalog 2026-06-19] Phase A.4: PC Skills.txt col 22 CharAnimId parity cho 16 skill IDs.
    // PC source (gaibang.lua + Skills.txt):
    //   115, 116 (mastery passive)    → 0 (default, no cast anim)
    //   117, 119, 122, 125, 128       → 11 (Cái Bang cast anim, mag_tr_16_施魔法.spr)
    //   118, 120, 121, 123, 126, 129  → 0 or default (buff utility, no cast anim)
    //   124                           → 11 (newest PC passive dagou_zhen row)
    //   127                           → 14 (passive cdo_none)
    //   130                           → 43 (Túy Điệp Cuồng Vũ — state 43 aura)
    [TestFixture, Category("CaiBang")]
    public class CaiBangCatalogCharAnimTests
    {
        private static readonly SkillCatalog _catalog = TestCatalogCache.NoviceAndCaiBang;
        private SkillCatalog Catalog() => _catalog;

        [Test]
        public void DamageSkills_CharAnim11_PcAccurate()
        {
            // PC: 117/119/122/125/128 CharAnimId=11 (Cái Bang cast anim).
            var cat = Catalog();
            int[] ids = { 117, 119, 122, 125, 128 };
            foreach (int id in ids)
            {
                var s = cat.Resolve(id);
                Assert.IsNotNull(s, $"skill {id} missing");
                Assert.AreEqual(11, s.charAnimId, $"skill {id} PC CharAnimId=11");
                Assert.IsFalse(s.isMelee, $"skill {id} PC IsMelee=0");
            }
        }

        [Test]
        public void PassiveUtilitySkills_CharAnimMatchesNewestPc()
        {
            var cat = Catalog();
            Assert.AreEqual(11, cat.Resolve(121).charAnimId, "121 canonical slistcache CharAnimId=11");
            Assert.AreEqual(11, cat.Resolve(124).charAnimId, "124 newest PC CharAnimId=11 (dagou_zhen passive row)");
            Assert.AreEqual(11, cat.Resolve(127).charAnimId, "127 PC CharAnimId=11 (slistcache authoritative; old test wrongly claimed 14)");
        }

        [Test]
        public void TuyDiepCuongVu_UsesCanonicalCastAnimation()
        {
            var cat = Catalog();
            var s = cat.Resolve(130);
            Assert.IsNotNull(s);
            Assert.AreEqual(11, s.charAnimId, "130 canonical slistcache CharAnimId=11; StateSpecialId remains 43");
            Assert.AreEqual(43, s.stateSpecialId);
        }

        [Test]
        public void SkillSectCatalog_BuildCaiBang_CharAnimIdAligned()
        {
            // SkillSectCatalog.GetSkills(CaiBangId) entries must match PC CharAnimId.
            var skills = SkillSectCatalog.GetSkills(CombatFactionExt.CaiBangId);
            Assert.IsNotEmpty(skills, "Cái Bang sect missing");
            var byId = skills.ToDictionary(sk => sk.skillId);
            Assert.AreEqual("Bổng Đả Ác Cẩu", byId[125].nameVi);
            Assert.AreEqual(11, byId[117].charAnimId, "117 sect charAnim=11");
            Assert.AreEqual(11, byId[119].charAnimId, "119 sect charAnim=11");
            Assert.AreEqual(11, byId[122].charAnimId, "122 sect charAnim=11");
            Assert.AreEqual(11, byId[121].charAnimId, "121 canonical sect charAnim=11");
            Assert.AreEqual(11, byId[124].charAnimId, "124 sect charAnim=11");
            Assert.AreEqual(11, byId[127].charAnimId, "127 canonical sect charAnim=11");
            Assert.AreEqual(11, byId[125].charAnimId, "125 sect charAnim=11");
            Assert.AreEqual(11, byId[128].charAnimId, "128 sect charAnim=11");
            Assert.AreEqual(11, byId[130].charAnimId, "130 canonical sect charAnim=11");
            Assert.IsFalse(byId[117].isMelee, "117 sect melee=false (PC IsMelee=0)");
            Assert.IsFalse(byId[119].isMelee, "119 sect melee=false (PC IsMelee=0)");
            Assert.IsFalse(byId[122].isMelee, "122 sect melee=false (PC IsMelee=0)");
            Assert.IsFalse(byId[125].isMelee, "125 sect melee=false (PC IsMelee=0)");
            Assert.IsFalse(byId[128].isMelee, "128 sect melee=false (PC IsMelee=0)");
        }

        [Test]
        public void SkillSectCatalog_BuildCaiBang_AllSkillIdsPresent()
        {
            var skills = SkillSectCatalog.GetSkills(CombatFactionExt.CaiBangId);
            var byId = skills.ToDictionary(sk => sk.skillId);
            int[] requiredIds = { 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130 };
            foreach (int id in requiredIds)
                Assert.IsTrue(byId.ContainsKey(id), $"Cái Bang sect missing skill {id}");
        }
    }
}
