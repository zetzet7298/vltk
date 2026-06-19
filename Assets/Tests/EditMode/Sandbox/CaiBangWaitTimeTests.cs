using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;
using VLTK.UI;

namespace VLTK.Tests.Sandbox
{
    // [CaiBang-WaitTime 2026-06-19] Phase C.1-C.2: PC WaitTime drives cast anim duration.
    // PC source: Skills.txt col 25 WaitTime (ticks, 16 ticks/sec).
    // PC default m_CastFrame=20 ticks (~1.25s @ 16 fps).
    // Trước fix: PcCastAnimationDurationSeconds trả 20f/18f ≈ 1.11s cứng.
    //   preCastDuration dùng timePerCast * 0.055f — sai field (timePerCast là cooldown, WaitTime mới là cast anim).
    // Sau fix: waitTime > 0 ? waitTime/16f : 20f/16f fallback.
    [TestFixture, Category("CaiBang")]
    public class CaiBangWaitTimeTests
    {
        private static readonly SkillCatalog _catalog = TestCatalogCache.NoviceAndCaiBang;
        private SkillCatalog Catalog() => _catalog;

        // Reflection helper: invoke private static PcCastAnimationDurationSeconds trên CombatSkillSlotController.
        private static float InvokeCastDuration(SkillDefinition skill)
        {
            var m = typeof(CombatSkillSlotController).GetMethod(
                "PcCastAnimationDurationSeconds",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(m, "PcCastAnimationDurationSeconds not found");
            return (float)m.Invoke(null, new object[] { skill });
        }

        [Test]
        public void CastDuration_CdoNone_ReturnsZero()
        {
            // PC: charAnimId=14 = stance/passive (không cast anim).
            var s = new SkillDefinition { skillId = 124, charAnimId = 14, waitTime = 5 };
            Assert.AreEqual(0f, InvokeCastDuration(s));
        }

        [Test]
        public void CastDuration_WaitTime16Frames_EqualsOneSecond()
        {
            // PC: WaitTime=16 ticks @ 16 fps = 1.0 sec.
            var s = new SkillDefinition { skillId = 117, charAnimId = 11, waitTime = 16 };
            Assert.AreEqual(1.0f, InvokeCastDuration(s), 0.001f);
        }

        [Test]
        public void CastDuration_WaitTimeZero_UsesFallback20Frames()
        {
            // PC: WaitTime=0 → fallback 20 ticks (PC m_CastFrame default).
            var s = new SkillDefinition { skillId = 119, charAnimId = 11, waitTime = 0 };
            Assert.AreEqual(20f / 16f, InvokeCastDuration(s), 0.001f);
        }

        [Test]
        public void CastDuration_NullSkill_ReturnsZero()
        {
            Assert.AreEqual(0f, InvokeCastDuration(null));
        }

        [Test]
        public void CaiBangDamageSkills_HavePositiveWaitTime()
        {
            // PC Skills.txt WaitTime > 0 cho active damage skills 117/119/122/125/128.
            var cat = Catalog();
            int[] damageIds = { 117, 119, 122, 125, 128 };
            foreach (int id in damageIds)
            {
                var s = cat.Resolve(id);
                Assert.IsNotNull(s, $"skill {id} missing");
                Assert.Greater(s.waitTime, 0, $"skill {id} PC WaitTime should be > 0 for active damage");
            }
        }

        [Test]
        public void DogArray_WaitTimeZero_ImmediateStateApply()
        {
            // PC 打狗阵 stance: WaitTime=0 → state apply ngay lập tức.
            var cat = Catalog();
            var s = cat.Resolve(124);
            Assert.AreEqual(0, s.waitTime, "124 PC WaitTime=0");
        }
    }
}
