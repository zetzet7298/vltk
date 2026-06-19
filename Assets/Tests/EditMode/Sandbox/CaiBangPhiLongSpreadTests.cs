using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    // [CaiBang-PhiLongSpread 2026-06-19] Phase B.1: PC gaibang.lua::skill_param1_v drives Phi Long spread.
    // PC source: gaibang.lua feilong_zaitian skill_misslenum_v L1=1, L11=1, L12=2, L15=2, L16=3, L20=4.
    //   skill_param1_v L1=0, L11=0, L11=32, L20=32 — straight-line at low levels, spread 32 at L11+.
    // Trước fix: SetupPcPhiLongSpread luôn applied với stepWu=32 — sai straight-line L1-L10.
    // Sau fix: rawParam == 0 → single straight missile, rawParam > 0 → spread.
    public class CaiBangPhiLongSpreadTests
    {
        private SkillCatalog Catalog() => PcCombatCatalogFactory.CreateNoviceAndCaiBangCatalog();

        [Test]
        public void PhiLong_L1_StraightLine_NoSpread()
        {
            // L1: luaCount=1 → straight line, no spread positions needed.
            var cat = Catalog();
            var s = cat.Resolve(357);
            Assert.IsNotNull(s);
            // Phi Long catalog: attackRadius = 512, missileForm = Single (bundled PcSkills.txt 357 MissilesForm=0),
            //   childSkillNum = 1 (runtime uses Lua skill_misslenum_v L20=4).
            Assert.AreEqual(512, s.attackRadius, "357 PC AttackRadius L20=512");
            Assert.AreEqual(SkillMissileForm.Single, s.missileForm, "357 PC MissilesForm=0 (Single)");
        }

        [Test]
        public void PhiLong_L12Plus_HasSpreadStep32()
        {
            // L12+: skill_param1_v = 32 → spread with 32 world units per step.
            // (Verified via runtime: PcCaiBangLuaLevelService.GetSingleValue(357, 12, "skill_param1_v", 1) == 32)
            // L11 ambiguous (skill_param1_v has both (11,0) and (11,32) — PC Link returns first match = 0).
            if (!PcCaiBangLuaLevelService.Applies(357))
            {
                Assert.Ignore("gaibang.lua not loaded — skipping Phi Long Lua parity check");
                return;
            }
            int paramL12 = PcCaiBangLuaLevelService.GetSingleValue(357, 12, "skill_param1_v", 1);
            int paramL20 = PcCaiBangLuaLevelService.GetSingleValue(357, 20, "skill_param1_v", 1);
            Assert.AreEqual(32, paramL12, "PC feilong_zaitian skill_param1_v L12=32");
            Assert.AreEqual(32, paramL20, "PC feilong_zaitian skill_param1_v L20=32");
        }

        [Test]
        public void PhiLong_LowLevel_StraightLine_ParamZero()
        {
            // L1-L11: skill_param1_v = 0 → straight-line (no spread).
            if (!PcCaiBangLuaLevelService.Applies(357))
            {
                Assert.Ignore("gaibang.lua not loaded");
                return;
            }
            int paramL1 = PcCaiBangLuaLevelService.GetSingleValue(357, 1, "skill_param1_v", 1);
            int paramL11 = PcCaiBangLuaLevelService.GetSingleValue(357, 11, "skill_param1_v", 1);
            Assert.AreEqual(0, paramL1, "PC feilong_zaitian skill_param1_v L1=0 (straight)");
            Assert.AreEqual(0, paramL11, "PC feilong_zaitian skill_param1_v L11=0 (straight, ambiguous (11,0)/(11,32) pair)");
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
