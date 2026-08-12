using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    // [CaiBang-PhiLongSpread 2026-06-19] Phase B.1: PC gaibang.lua::feilong_zaitian drive Phi Long spread.
    // PC source (mobile gaibang.lua): skill_misslenum_v L1=1, L11=1, L12=2, L15=2, L16=3, L20=4.
    //   skill_misslesform_v L1=1, L11=1, L11=0, L20=0 — Single form throughout.
    //   Mobile/PC gaibang.lua does NOT have skill_param1_v for 357 (only for 128 kanglong_youhui).
    //   PC skills.txt 357 Param1=32 supplies the lane gap; missile 166 MoveKind=5 supplies homing.
    // Sau fix: luaCount=1 → straight line; luaCount>1 → parallel homing lanes via SetupPcPhiLongSpread.
    [TestFixture, Category("CaiBang")]
    public class CaiBangPhiLongSpreadTests
    {
        private static readonly SkillCatalog _catalog = TestCatalogCache.NoviceAndCaiBang;
        private SkillCatalog Catalog() => _catalog;

        [Test]
        public void PhiLong_L1_StraightLine_NoSpread()
        {
            // L1: luaCount=1 → straight line, no spread positions needed.
            var cat = Catalog();
            var s = cat.Resolve(357);
            Assert.IsNotNull(s);
            Assert.AreEqual(400, s.attackRadius, "357 canonical skills.txt base AttackRadius=400");
            Assert.AreEqual(512, PcCaiBangLuaLevelService.GetAttackRadius(357, 20),
                "357 Lua L20 runtime AttackRadius=512");
            Assert.AreEqual(SkillMissileForm.Single, s.missileForm,
                "PC form 0 has child missiles; Unity keeps single-projectile render fallback");
        }

        [Test]
        public void PhiLong_L12_MissileCountFromLua()
        {
            // L12+: skill_misslenum_v = 2 → spread (mobile gaibang.lua).
            // Phi Long không có skill_param1_v trong mobile gaibang.lua — spread step được derive
            // từ SetupPcCircleOutwardMissiles (per-missile angle step).
            if (!PcCaiBangLuaLevelService.Applies(357))
            {
                Assert.Ignore("gaibang.lua not loaded — skipping Phi Long Lua parity check");
                return;
            }
            int countL11 = PcCaiBangLuaLevelService.GetMissileCount(357, 11);
            int countL12 = PcCaiBangLuaLevelService.GetMissileCount(357, 12);
            int countL20 = PcCaiBangLuaLevelService.GetMissileCount(357, 20);
            Assert.AreEqual(1, countL11, "357 skill_misslenum_v L11=1");
            Assert.AreEqual(2, countL12, "357 skill_misslenum_v L12=2 (multi-missile spread trigger)");
            Assert.AreEqual(4, countL20, "357 skill_misslenum_v L20=4");
        }

        [Test]
        public void PhiLong_NoSkillParam1V_MobileLuaParity()
        {
            // Mobile/PC gaibang.lua feilong_zaitian (357) KHÔNG có skill_param1_v (chỉ 128 kanglong_youhui mới có).
            // Verify that GetSingleValue(357, lv, "skill_param1_v", 1) trả về 0 (no attribute).
            // Runtime must therefore use PC skills.txt Param1=32 fallback for luaCount>1 parallel lanes.
            if (!PcCaiBangLuaLevelService.Applies(357))
            {
                Assert.Ignore("gaibang.lua not loaded");
                return;
            }
            int paramL1 = PcCaiBangLuaLevelService.GetSingleValue(357, 1, "skill_param1_v", 1);
            int paramL20 = PcCaiBangLuaLevelService.GetSingleValue(357, 20, "skill_param1_v", 1);
            Assert.AreEqual(0, paramL1, "357 mobile Lua no skill_param1_v → 0");
            Assert.AreEqual(0, paramL20, "357 mobile Lua no skill_param1_v → 0 (drives straight-line at runtime)");
        }

        [Test]
        public void PhiLong_MissileCount_Levels()
        {
            // PC feilong_zaitian skill_misslenum_v L1=1, L11=1, L12=2, L15=2, L16=3, L20=4.
            if (!PcCaiBangLuaLevelService.Applies(357))
            {
                Assert.Ignore("gaibang.lua not loaded");
                return;
            }
            int countL1 = PcCaiBangLuaLevelService.GetMissileCount(357, 1);
            int countL20 = PcCaiBangLuaLevelService.GetMissileCount(357, 20);
            Assert.AreEqual(1, countL1, "357 skill_misslenum_v L1=1");
            Assert.AreEqual(4, countL20, "357 skill_misslenum_v L20=4");
        }
    }
}
