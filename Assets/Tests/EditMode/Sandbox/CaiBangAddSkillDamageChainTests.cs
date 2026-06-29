using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    // [CaiBang-AddSkillDamage 2026-06-29] PC gaibang.lua::addskilldamageN.
    // ENGINE TRUTH (KSkillList::GetAddSkillDamage + KNpc::AppendSkillEffect, MAX_PERCENT=100):
    //   addskilldamage is a PASSIVE flat %-damage amplifier, NOT a proc that casts a sub-skill.
    //   Learning skill G adds G.addskilldamageN[3]% to the damage of the skill G.addskilldamageN[1]
    //   points at, WHEN that target skill is cast. No RNG, no extra missiles/visual.
    // These tests assert the PC percent VALUES per skill/slot (slot[3] = the damage % bonus):
    //   119 (yanmen_tuobo)      → 359, +40%
    //   122 (jianren_shenshou)  → 357, +50%
    //   125 (bangda_egou)       → 359, +60% AND 1074, +50%
    //   128 (kanglong_youhui)   → 357, +55%
    //   359 (tianxia_wugou)     → 1074, +25%
    [TestFixture, Category("CaiBang")]
    public class CaiBangAddSkillDamageChainTests
    {
        private static readonly SkillCatalog _catalog = TestCatalogCache.NoviceAndCaiBang;
        private SkillCatalog Catalog() => _catalog;

        [Test]
        public void YanmenTuobo_L20Chance40_Target359()
        {
            if (!PcCaiBangLuaLevelService.Applies(119))
            {
                Assert.Ignore("gaibang.lua not loaded");
                return;
            }
            int chance = PcCaiBangLuaLevelService.GetSingleValue(119, 20, "addskilldamage1", 3);
            Assert.AreEqual(40, chance, "PC yanmen_tuobo addskilldamage1[3] L20=40");
        }

        [Test]
        public void JianrenShenshou_L20Chance50_Target357()
        {
            if (!PcCaiBangLuaLevelService.Applies(122))
            {
                Assert.Ignore("gaibang.lua not loaded");
                return;
            }
            int chance = PcCaiBangLuaLevelService.GetSingleValue(122, 20, "addskilldamage1", 3);
            Assert.AreEqual(50, chance, "PC jianren_shenshou addskilldamage1[3] L20=50");
        }

        [Test]
        public void BangDaEgou_L20Chances60And50_Target359And1074()
        {
            if (!PcCaiBangLuaLevelService.Applies(125))
            {
                Assert.Ignore("gaibang.lua not loaded");
                return;
            }
            int chance1 = PcCaiBangLuaLevelService.GetSingleValue(125, 20, "addskilldamage1", 3);
            int chance2 = PcCaiBangLuaLevelService.GetSingleValue(125, 20, "addskilldamage2", 3);
            Assert.AreEqual(60, chance1, "PC newest bangda_egou addskilldamage1[3] L20=60 → target 359");
            Assert.AreEqual(50, chance2, "PC newest bangda_egou addskilldamage2[3] L20=50 → target 1074");
        }

        [Test]
        public void TianxiaWugou_L20Chance25_Target1074()
        {
            if (!PcCaiBangLuaLevelService.Applies(359))
            {
                Assert.Ignore("gaibang.lua not loaded");
                return;
            }
            int chance = PcCaiBangLuaLevelService.GetSingleValue(359, 20, "addskilldamage1", 3);
            Assert.AreEqual(25, chance, "PC tianxia_wugou addskilldamage1[3] L20=25");
        }

        [Test]
        public void KanglongYouhui_L20Chance55_Target357()
        {
            if (!PcCaiBangLuaLevelService.Applies(128))
            {
                Assert.Ignore("gaibang.lua not loaded");
                return;
            }
            int chance = PcCaiBangLuaLevelService.GetSingleValue(128, 20, "addskilldamage1", 3);
            Assert.AreEqual(55, chance, "PC kanglong_youhui addskilldamage1[3] L20=55");
        }

        [Test]
        public void ChainTargets_AllRegisteredInCatalog()
        {
            // Chain targets 357/359/1074 phải có trong catalog.
            var cat = Catalog();
            Assert.IsNotNull(cat.Resolve(357), "chain target 357 (Phi Long) missing");
            Assert.IsNotNull(cat.Resolve(359), "chain target 359 (Bổng Đả Ác Cẩu) missing");
            Assert.IsNotNull(cat.Resolve(1074), "chain target 1074 (Phi Long Tại Thiên tier 2) missing");
        }
    }
}
