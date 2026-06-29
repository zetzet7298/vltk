using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    // [CaiBang-TianXiaWuGou 2026-06-29] Newest PC version priority:
    //   skill 125 is Bổng Đả Ác Cẩu (`bangda_egou`), while 359/1539 are Thiên Hạ Vô Cẩu (`tianxia_wugou`).
    // PC gaibang.lua::tianxia_wugou skill_misslenum_v L1=1, L20=3.
    [TestFixture, Category("CaiBang")]
    public class CaiBangTianXiaWuGouTests
    {
        private static readonly SkillCatalog _catalog = TestCatalogCache.NoviceAndCaiBang;
        private SkillCatalog Catalog() => _catalog;

        [Test]
        public void TianxiaWugou_PlayerAndNpcVariantsUsePcHomingShape()
        {
            var cat = Catalog();
            var player = cat.Resolve(359);
            var npc = cat.Resolve(1539);
            Assert.IsNotNull(player, "skill 359 missing");
            Assert.IsNotNull(npc, "skill 1539 missing");
            Assert.AreEqual(SkillMissileForm.Single, player.missileForm, "359 PC MissilesForm=0 (Single, Lua count overrides to 3)");
            Assert.AreEqual(player.missileForm, npc.missileForm, "1539 missileForm = 359 missileForm");
            Assert.AreEqual(168, player.childSkillId, "359 PC child missile = 168");
            Assert.AreEqual(168, npc.childSkillId, "1539 PC child missile = 168");
            Assert.AreEqual(512, player.attackRadius, "359 PC Lua L20 AttackRadius=512");
            Assert.AreEqual(player.attackRadius, npc.attackRadius, "1539 attackRadius = 359 attackRadius");
        }

        [Test]
        public void TianxiaWugou_L20MissileCount_FromLua()
        {
            // PC gaibang.lua tianxia_wugou skill_misslenum_v L20=3 (verified).
            if (!PcCaiBangLuaLevelService.Applies(359))
            {
                Assert.Ignore("gaibang.lua not loaded");
                return;
            }
            int count = PcCaiBangLuaLevelService.GetMissileCount(359, 20);
            Assert.AreEqual(3, count, "359 tianxia_wugou skill_misslenum_v L20=3");
        }

        [Test]
        public void SkillMissileForm_ZoneEnumValue_Five()
        {
            // PC SKILL_MF_Zone form 5. Đảm bảo enum value khớp PC.
            Assert.AreEqual(5, (int)SkillMissileForm.Zone, "Zone enum value must be 5 (PC SKILL_MF_Zone)");
        }
    }
}
