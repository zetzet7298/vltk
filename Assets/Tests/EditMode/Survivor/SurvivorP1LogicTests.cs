// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Survivor — SurvivorP1LogicTests
// P1 acceptance gate 1: EditMode self-check pure logic.
// Cover: XpToNext curve, ApplyCard mỗi kind, TakeDamage invuln guard.
// ponytail: không PlayMode — loop feel = manual checklist (p1-acceptance.md).
// -----------------------------------------------------------------------------

using NUnit.Framework;
using UnityEngine;
using VLTK.Survivor;

namespace VLTK.Tests.Survivor
{
    public class SurvivorP1LogicTests
    {
        // ponytail: inline stub, không spin MonoBehaviour ProxyActorVisual.
        private sealed class StubVisual : IActorVisual
        {
            public void SyncPosition(Vector3 p) { }
            public void SyncDepth(float y) { }
            public void SetDirection(int d) { }
            public void PlayMove(bool m) { }
            public void SetAlive(bool a) { }
        }

        private SurvivorPlayer MakePlayer()
        {
            var go = new GameObject("player_test");
            var p = go.AddComponent<SurvivorPlayer>();
            p.Init(new StubVisual(), Vector3.zero);
            return p;
        }

        // --- gate 1a: XpToNext curve = 5 + (L-1)*3 ---
        [Test]
        public void XpToNext_Curve_5_8_11()
        {
            var p = MakePlayer();
            Assert.AreEqual(5, p.XpToNext, "L1");
            p.AddXp(5);
            Assert.AreEqual(2, p.Level, "→L2");
            Assert.AreEqual(0, p.Xp, "xp reset");
            Assert.AreEqual(8, p.XpToNext, "L2");
            p.AddXp(8);
            Assert.AreEqual(3, p.Level, "→L3");
            Assert.AreEqual(11, p.XpToNext, "L3");
        }

        // --- gate 1b: ApplyCard mỗi kind ---
        [Test]
        public void ApplyCard_Damage_x1_25()
        {
            var p = MakePlayer();
            float d0 = p.Damage;
            p.ApplyCard(new SkillCard(SkillEffectKind.Damage, "", ""));
            Assert.AreEqual(d0 * 1.25f, p.Damage, 1e-4f);
        }

        [Test]
        public void ApplyCard_AttackSpeed_x0_8()
        {
            var p = MakePlayer();
            float i0 = p.AttackInterval;
            p.ApplyCard(new SkillCard(SkillEffectKind.AttackSpeed, "", ""));
            Assert.AreEqual(i0 * 0.8f, p.AttackInterval, 1e-4f);
        }

        [Test]
        public void ApplyCard_MoveSpeed_x1_15()
        {
            var p = MakePlayer();
            float s0 = p.MoveSpeed;
            p.ApplyCard(new SkillCard(SkillEffectKind.MoveSpeed, "", ""));
            Assert.AreEqual(s0 * 1.15f, p.MoveSpeed, 1e-4f);
        }

        [Test]
        public void ApplyCard_MultiShot_Plus1()
        {
            var p = MakePlayer();
            int n0 = p.Projectiles;
            p.ApplyCard(new SkillCard(SkillEffectKind.MultiShot, "", ""));
            Assert.AreEqual(n0 + 1, p.Projectiles);
        }

        [Test]
        public void ApplyCard_MaxHp_Plus1_And_Heal1()
        {
            var p = MakePlayer();
            // hurt first: Hp 5→4
            p.TakeDamage(1);
            Assert.AreEqual(4, p.Hp, "setup hurt");
            // invuln window — force clear by waiting via reflection-free path: skip Update,
            // instead test fresh player to avoid invuln bleed
            var p2 = MakePlayer();
            p2.TakeDamage(1);
            Assert.AreEqual(4, p2.Hp, "p2 hurt");
            int hp0 = p2.Hp;
            int max0 = p2.MaxHp;
            p2.ApplyCard(new SkillCard(SkillEffectKind.MaxHp, "", ""));
            Assert.AreEqual(max0 + 1, p2.MaxHp, "max+1");
            Assert.AreEqual(Mathf.Min(max0 + 1, hp0 + 1), p2.Hp, "heal1");
        }

        // --- gate 1c: TakeDamage invuln guard (0.6s window) ---
        [Test]
        public void TakeDamage_InvulnGuard_SecondHitIgnored()
        {
            var p = MakePlayer();
            Assert.AreEqual(5, p.Hp, "init");
            p.TakeDamage(1);
            Assert.AreEqual(4, p.Hp, "first hit");
            // EditMode: Update không tự gọi → _invuln vẫn > 0 → hit thứ 2 bị guard.
            p.TakeDamage(1);
            Assert.AreEqual(4, p.Hp, "second hit blocked by invuln");
            Assert.IsFalse(p.Dead, "still alive");
        }

        [Test]
        public void TakeDamage_Lethal_SetsDead()
        {
            var p = MakePlayer();
            p.TakeDamage(5);
            Assert.AreEqual(0, p.Hp, "hp=0");
            Assert.IsTrue(p.Dead, "dead flag");
        }
    }
}
