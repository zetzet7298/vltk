using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    // [CaiBang-AddSkillDamage 2026-06-19] Phase D: PC gaibang.lua::addskilldamageN chain.
    // Chain map (skillId → chain targetId, L20 chance %):
    //   119 (yanmen_tuobo, Diên Môn Thác Bát)         → 359 (Bổng Đả Ác Cẩu), 40%
    //   122 (jianren_shenshou, Kiến Nhân Thần Thủ)    → 357 (Phi Long), 50%
    //   125 (tianxia_wugou, Thiên Hạ Vô Cẩu)         → 1074 (Phi Long Tại Thiên tier 2), 25%
    //   128 (kanglong_youhui, Kháng Long Hữu Hối)     → 357 (Phi Long), 55%
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
        public void TianxiaWugou_L20Chance25_Target1074()
        {
            if (!PcCaiBangLuaLevelService.Applies(125))
            {
                Assert.Ignore("gaibang.lua not loaded");
                return;
            }
            int chance = PcCaiBangLuaLevelService.GetSingleValue(125, 20, "addskilldamage1", 3);
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
