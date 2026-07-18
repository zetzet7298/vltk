// -----------------------------------------------------------------------------
// VLTK Mobile — CaiBang Lua Level Service parity tests.
//
// Source of truth: Assets/StreamingAssets/Reference/gaibang.lua (mobile-stored
// PC-truth, 562 dòng, full SKILLS dict). Mỗi test assert giá trị interpolate
// tại các level điểm chính (L1, breakpoint, L20) để lock PC parity.
//
// [CaiBang-LuaPort 2026-06-17] Thay thế cho assert cứng trong PcCaiBangSkillTuning
// /PcCaiBangModTuning (đã xóa). Tests này verify Lua parser + interpolation
// (Line/Conic/Extrac) bằng cách đối chiếu giá trị đọc được từ file Lua thật.
// -----------------------------------------------------------------------------

using System.IO;
using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture, Category("CaiBang")]
    public class CaiBangLuaLevelServiceTests
    {
        // Mobile Lua path (server-truth, identical content with client gaibang.lua).
        private static readonly string LuaPath =
            Path.Combine(Application.dataPath, "StreamingAssets/Reference/gaibang.lua");

        [SetUp]
        public void ResetCache()
        {
            PcCaiBangLuaLevelService.Reset();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            PcCaiBangLuaLevelService.Reset();
        }

        // --- Test 1: gaibang_bangfa addphysicsdamage_p linear 10→150 ---
        [Test]
        public void Reads_GaibangBangfa_AddPhysicsDamageP_At_L1_L10_L20()
        {
            // gaibang_bangfa addphysicsdamage_p slot[1] = {{1,10},{20,150}}.
            // L1 = 10, L10 = 10 + (10-1)/(20-1)*(150-10) = 10 + 0.473*140 = 76.3 → floor 76,
            // L20 = 150.
            Assert.IsTrue(File.Exists(LuaPath), $"gaibang.lua missing at {LuaPath}");
            Assert.AreEqual(10, PcCaiBangLuaLevelService.GetSingleValue(115, 1, "addphysicsdamage_p", 1));
            Assert.AreEqual(150, PcCaiBangLuaLevelService.GetSingleValue(115, 20, "addphysicsdamage_p", 1));
            int mid = PcCaiBangLuaLevelService.GetSingleValue(115, 10, "addphysicsdamage_p", 1);
            Assert.Greater(mid, 10, "L10 must be > L1 (linear upward)");
            Assert.Less(mid, 150, "L10 must be < L20 (linear upward)");
        }

        // --- Test 2: kanglong_youhui (128) missle_speed_v 2-tier breakpoint at L10→L11 ---
        [Test]
        public void Reads_KangLongYouhui_MissileSpeed_ScalesAt_L11_Breakpoint()
        {
            // kanglong_youhui missle_speed_v = {{1,28},{20,32}}.
            // Mobile Lua (authoritative): L1=28, L10≈29 (linear), L11≈29.47→29, L20=32.
            // Not a hard breakpoint — curve is continuous linear 28→32 — but verify L11 > L10
            // and L20 caps at 32.
            int l1 = PcCaiBangLuaLevelService.GetMissileSpeed(128, 1);
            int l10 = PcCaiBangLuaLevelService.GetMissileSpeed(128, 10);
            int l11 = PcCaiBangLuaLevelService.GetMissileSpeed(128, 11);
            int l20 = PcCaiBangLuaLevelService.GetMissileSpeed(128, 20);
            Assert.AreEqual(28, l1, "L1 = 28");
            Assert.AreEqual(32, l20, "L20 = 32");
            Assert.GreaterOrEqual(l11, l10, "L11 must be >= L10 (monotonic up)");
            Assert.LessOrEqual(l11, l20, "L11 must be <= L20");
        }

        // --- Test 3: kanglong_youhui missile count jumps L10 → L11 ---
        [Test]
        public void Reads_KangLongYouhui_MissileCount_JumpsAt_L11()
        {
            // kanglong_youhui skill_misslenum_v = {{1,1},{10,1},{20,15},{25,18},{26,18}}.
            // L10=1, L11 = 1 + (11-10)/(20-10)*(15-1) = 1 + 1.4 = 2.4 → floor 2,
            // L20 = 15. Count should jump từ L10=1 lên L11=2 (breakpoint behavior).
            int l10 = PcCaiBangLuaLevelService.GetMissileCount(128, 10);
            int l11 = PcCaiBangLuaLevelService.GetMissileCount(128, 11);
            int l20 = PcCaiBangLuaLevelService.GetMissileCount(128, 20);
            Assert.AreEqual(1, l10, "L10 = 1 missile");
            Assert.GreaterOrEqual(l11, 2, "L11 must jump lên >= 2 (breakpoint sau L10)");
            Assert.AreEqual(15, l20, "L20 = 15 missiles");
        }

        // --- Test 4: kanglong_youhui firedamage_v min/max range ---
        [Test]
        public void Reads_KangLongYouhui_PhysicsDamageV_RangeMinMax()
        {
            // kanglong_youhui firedamage_v: [1]={{1,10},{20,536}}, [3]={{1,10},{20,536}}.
            // (mobile gaibang.lua uses same value cho min/max → flat damage.)
            var range = PcCaiBangLuaLevelService.GetDamageRange(128, 20, "firedamage_v");
            Assert.AreEqual(536, range.min, "firedamage_v min at L20 = 536");
            Assert.AreEqual(536, range.max, "firedamage_v max at L20 = 536");
        }

        // --- Test 5: Conic interpolation differs từ linear (gaibang_bangfa deadlystrikeenhance_p) ---
        [Test]
        public void Reads_YanMenTuobo_ConicInterpolation_DoesNotReturnLinear()
        {
            // gaibang_bangfa deadlystrikeenhance_p = {{1,2},{20,25,Conic}}.
            // L10 Conic = (25-2)*100/399 - (25-2)*1/399 + 2 = 2300/399 - 23/399 + 2 = 5.765 - 0.058 + 2 = 7.7 → floor 7.
            // L10 Linear would be = 2 + (10-1)/(20-1)*(25-2) = 2 + 0.4737*23 = 12.89 → floor 12.
            // Conic (7) < Linear (12) tại midpoint — Conic grows slower đầu, nhanh cuối.
            int l10 = PcCaiBangLuaLevelService.GetSingleValue(115, 10, "deadlystrikeenhance_p", 1);
            Assert.AreEqual(7, l10, "Conic at L10 = 7 (not linear 12)");
            Assert.AreEqual(2, PcCaiBangLuaLevelService.GetSingleValue(115, 1, "deadlystrikeenhance_p", 1), "L1 = 2");
            Assert.AreEqual(25, PcCaiBangLuaLevelService.GetSingleValue(115, 20, "deadlystrikeenhance_p", 1), "L20 = 25");
        }

        // --- Test 6: tianxia_wugou firedamage_v min != max (range damage, not flat) ---
        [Test]
        public void Reads_TianXiaWuGou_FireDamageV_MinEqualsMax()
        {
            // tianxia_wugou firedamage_v: [1]={{1,70},{15,150},{20,285}}, [3]={{1,70},{15,200},{20,432}}.
            // Mobile Lua uses DIFFERENT min vs max → real range damage.
            var rangeL1 = PcCaiBangLuaLevelService.GetDamageRange(359, 1, "firedamage_v");
            var rangeL20 = PcCaiBangLuaLevelService.GetDamageRange(359, 20, "firedamage_v");
            Assert.AreEqual(70, rangeL1.min, "L1 min = 70");
            Assert.AreEqual(70, rangeL1.max, "L1 max = 70 (same at L1)");
            Assert.AreEqual(285, rangeL20.min, "L20 min = 285");
            Assert.AreEqual(432, rangeL20.max, "L20 max = 432 (range damage)");
            Assert.Greater(rangeL20.max, rangeL20.min, "L20 max > min → range damage confirmed");
        }

        // --- Test 7: all Cái Bang skills with PC LvlData have skill_attackradius ---
        [Test]
        public void Reads_All_CaiBang_Skills_With_PcLvlData_Have_Skill_AttackRadius()
        {
            // Bước 6 prompt: skill Cái Bang. Mỗi skill có skill_attackradius trong gaibang.lua
            // hoặc fallback default (320) nếu passive. Kiểm tra service trả về radius > 0 cho
            // mọi skillId active trong catalog.
            // [CaiBang-FailClosed117 2026-07-17] 117 (Đầu Thạch Vấn Lộ) removed: PC skills.txt row 117
            //   LvlData1="skill_cost_v" only — no skill_attackradius curve. It fails closed (returns 0).
            int[] activeSkills = { 119, 122, 125, 128, 357, 359, 1073, 1074, 1539 };
            foreach (int id in activeSkills)
            {
                int r = PcCaiBangLuaLevelService.GetAttackRadius(id, 20);
                Assert.Greater(r, 0, $"skill {id} must have positive attack radius at L20");
                Assert.GreaterOrEqual(r, 320, $"skill {id} radius at L20 must be >= 320 (PC min)");
            }
            // 117 fail-closed: no PC LvlData radius curve -> sentinel 0 (caller falls through to catalog).
            Assert.AreEqual(0, PcCaiBangLuaLevelService.GetAttackRadius(117, 20),
                "117 has no PC LvlData radius; lua service fails closed (returns 0)");
        }

        // --- Test 8: unknown skillId returns 0 (no data, caller falls through) ---
        [Test]
        public void GetAttackRadius_UnknownSkill_ReturnsDefault()
        {
            int unknown = 99999;
            Assert.IsFalse(PcCaiBangLuaLevelService.Applies(unknown), "unknown skillId must not apply");
            // Service returns 0 sentinel khi skillId không có trong SKILLS dict.
            // Callers check `> 0` before using, falling back to engine/catalog value.
            int r = PcCaiBangLuaLevelService.GetAttackRadius(unknown, 20);
            Assert.AreEqual(0, r, "unknown skill must return 0 sentinel (caller falls through to engine value)");
        }

        [Test]
        public void Parser_EvaluatesLiteralMultiplicationInCanonicalLevelPoints()
        {
            const string lua = "SKILLS={probe={missle_lifetime_v={{{1,18},{20,18*2},{21,36/1}}}}}";
            var parsed = PcCaiBangLuaLevelService.ParseGaibangText(lua);
            var points = parsed["probe"]["missle_lifetime_v"][0];

            Assert.AreEqual(18f, points[0].Value);
            Assert.AreEqual(36f, points[1].Value);
            Assert.AreEqual(36f, points[2].Value);
        }
    }
}
