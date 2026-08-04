// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Survivor — SurvivorSupplyTests (ticket 33).
// Self-check pure logic (không scene/PlayMode — spec Testing Decisions):
//  - Heal: hồi đúng HealRatio × MaxHp qua impact 28 (BuffDot heal variant,
//    SourceBuffer, KHÔNG vào damage ledger), cap tại MaxHp, attribution skill id.
//  - Bomb: dmg vùng đúng radius (AttackRadius px ÷ 40), attribution + ledger.
//  - Magnet: scale MagnetRadius toàn màn, restore sau MagnetDuration.
//  - FullClear: dmg TẤT CẢ monster hiện tại.
//  - Cooldown FSM: cd riêng từng slot, dùng lại sau khi hết.
//  - Fail-closed: def null / tag thiếu → slot disabled (TryUse false, không crash);
//    Magnet/FullClear luôn enabled (own, không cần def).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VLTK.Survivor;

namespace VLTK.Tests.Survivor
{
    public class SurvivorSupplyTests
    {
        // ponytail: stub inline, không spin actor scene (pattern P1/Impact tests).
        private sealed class StubVisual : IActorVisual
        {
            public void SyncPosition(Vector3 p) { }
            public void SyncDepth(float y) { }
            public void SetDirection(int d) { }
            public void PlayMove(bool m) { }
            public void SetAlive(bool a) { }
        }

        private sealed class TestDamageable : ISurvivorDamageable
        {
            public int Hp { get; private set; }
            public int MaxHp { get; }
            public DamageInfo LastInfo { get; private set; }

            public TestDamageable(int hp, int maxHp) { Hp = hp; MaxHp = maxHp; }

            public void ApplyDot(DamageInfo info)
            {
                LastInfo = info;
                if (info.IsHeal) Hp = Mathf.Min(MaxHp, Hp + info.Damage);
                else Hp -= info.Damage;
            }
        }

        // --- helpers ---

        private static SkillDef MakeSupplyDef(int id, SurvivorSupplyTag tag, int attackRadius = 0)
        {
            return SkillDef.FromRow(new SkillRow { Id = id, Form = 7, SupplyTag = tag, AttackRadius = attackRadius });
        }

        private static SurvivorMonster MakeMonster(Vector3 pos, float maxHp)
        {
            var go = new GameObject("monster_test");
            var m = go.AddComponent<SurvivorMonster>();
            m.MaxHp = maxHp;
            m.Init(new StubVisual(), pos);
            return m;
        }

        private static XpGem MakeGem(Vector3 pos)
        {
            var go = new GameObject("gem_test");
            var g = go.AddComponent<XpGem>();
            g.Settings = CollectSettings.Default(); // Awake không chạy EditMode — set tay
            g.Init(pos, 1, g.Settings);
            return g;
        }

        private static SurvivorSupplyMgr MakeMgr(params SkillDef[] defs)
        {
            var mgr = new SurvivorSupplyMgr();
            mgr.Setup(defs);
            return mgr;
        }

        // ------------------------------------------------------------------
        // Heal (impact 28 heal variant)
        // ------------------------------------------------------------------

        [Test]
        public void Heal_AppliesRatio_NoLedgerDamage()
        {
            var mgr = MakeMgr(MakeSupplyDef(77, SurvivorSupplyTag.Heal));
            var target = new TestDamageable(3, 10);
            var attr = new SurvivorActorAttr { BaseMaxHp = 10 };
            attr.Recompute();
            mgr.HealTarget = target;
            mgr.HealAttr = attr;

            Assert.IsTrue(mgr.UseHeal(), "slot enabled → heal chạy");
            Assert.AreEqual(8, target.Hp, "hồi 50% × 10 = 5 → 3 + 5");
            Assert.IsTrue(target.LastInfo.IsHeal, "IsHeal flag qua impact");
            Assert.AreEqual(DamageSourceType.SourceBuffer, target.LastInfo.SourceType, "parity BuffDot SourceBuffer");
            Assert.AreEqual(77, target.LastInfo.Source.SkillId, "attribution = skill id từ SkillDef");
        }

        [Test]
        public void Heal_CapsAtMaxHp()
        {
            var mgr = MakeMgr(MakeSupplyDef(77, SurvivorSupplyTag.Heal));
            var target = new TestDamageable(9, 10);
            mgr.HealTarget = target;
            mgr.HealAttr = new SurvivorActorAttr { BaseMaxHp = 10 };

            Assert.IsTrue(mgr.UseHeal());
            Assert.AreEqual(10, target.Hp, "9 + 5 → cap tại max");
        }

        [Test]
        public void Heal_FallbackAttr_NoCrash()
        {
            // HealAttr null → dùng BaseMaxHp từ target (fail-closed, không crash)
            var mgr = MakeMgr(MakeSupplyDef(77, SurvivorSupplyTag.Heal));
            var target = new TestDamageable(2, 8); // Hp<=0 bị TickNow chặn (coi chết) → dùng 2
            mgr.HealTarget = target;

            Assert.IsTrue(mgr.UseHeal());
            Assert.AreEqual(6, target.Hp, "8 × 0.5 = 4 heal → 2+4 = 6");
        }

        [Test]
        public void Heal_NoTarget_NoOp()
        {
            var mgr = MakeMgr(MakeSupplyDef(77, SurvivorSupplyTag.Heal));
            Assert.IsFalse(mgr.UseHeal(), "HealTarget null → fail-closed no-op");
        }

        // ------------------------------------------------------------------
        // Bomb (AoE từ SkillDef Bomb tag)
        // ------------------------------------------------------------------

        [Test]
        public void Bomb_DamagesInRadius_Only()
        {
            var mgr = MakeMgr(MakeSupplyDef(88, SurvivorSupplyTag.Bomb, attackRadius: 200)); // 200px ÷ 40 = 5u
            mgr.Caster = new object();
            var inside = MakeMonster(new Vector3(1f, 0f, 0f), 100f);   // trong r=5
            var outside = MakeMonster(new Vector3(10f, 0f, 0f), 100f); // ngoài
            var monsters = new List<SurvivorMonster> { inside, outside };

            mgr.UseBomb(Vector2.zero, monsters);

            Assert.AreEqual(100f - SurvivorSupplyMgr.BombDamage, inside.Hp, "trong vùng → dmg");
            Assert.AreEqual(100f, outside.Hp, "ngoài vùng → không dmg");
            var src = new SkillImpactSource(88, 0);
            Assert.AreEqual(Mathf.RoundToInt(SurvivorSupplyMgr.BombDamage),
                inside.Ledger.GetTotal(src, mgr.Caster), "kill credit → ledger (skill id def)");
        }

        [Test]
        public void Bomb_DefaultRadius_WhenDefHasNoAttackRadius()
        {
            var mgr = MakeMgr(MakeSupplyDef(88, SurvivorSupplyTag.Bomb)); // AttackRadius = 0 → default 3.5
            var far = MakeMonster(new Vector3(4f, 0f, 0f), 100f); // ngoài 3.5
            var monsters = new List<SurvivorMonster> { far };

            mgr.UseBomb(Vector2.zero, monsters);

            Assert.AreEqual(100f, far.Hp, "default radius 3.5 — 4u ngoài vùng");
        }

        // ------------------------------------------------------------------
        // Magnet (radius scale toàn màn + restore)
        // ------------------------------------------------------------------

        [Test]
        public void Magnet_ScalesRadius_ThenRestores()
        {
            var mgr = MakeMgr();
            var g1 = MakeGem(new Vector3(2f, 0f, 0f));
            var g2 = MakeGem(new Vector3(-3f, 2f, 0f));
            var gems = new List<XpGem> { g1, g2 };
            float baseRadius = CollectSettings.Default().MagnetRadius;

            mgr.UseMagnet(gems);

            float boosted = baseRadius * SurvivorSupplyMgr.MagnetRadiusScale;
            Assert.AreEqual(SurvivorSupplyMgr.MagnetDuration, mgr.MagnetActiveTime, "timer chạy");
            Assert.AreEqual(boosted, g1.Settings.MagnetRadius, "gem trong danh sách → scale");
            Assert.AreEqual(boosted, g2.Settings.MagnetRadius, "gem trong danh sách → scale");

            mgr.Tick(SurvivorSupplyMgr.MagnetDuration);
            Assert.AreEqual(0f, mgr.MagnetActiveTime, "hết giờ");
            Assert.AreEqual(baseRadius, g1.Settings.MagnetRadius, "restore radius gốc");
            Assert.AreEqual(baseRadius, g2.Settings.MagnetRadius, "restore radius gốc");
        }

        [Test]
        public void Magnet_NoGems_NoOp_NoCrash()
        {
            var mgr = MakeMgr();
            mgr.UseMagnet(null);
            Assert.AreEqual(0f, mgr.MagnetActiveTime, "null list → không bật magnet");
        }

        // ------------------------------------------------------------------
        // FullClear (dmg tất cả monster hiện tại)
        // ------------------------------------------------------------------

        [Test]
        public void FullClear_DamagesAllMonsters()
        {
            var mgr = MakeMgr();
            var m1 = MakeMonster(new Vector3(1f, 1f, 0f), 100f);
            var m2 = MakeMonster(new Vector3(-2f, -3f, 0f), 100f);
            var monsters = new List<SurvivorMonster> { m1, m2 };

            mgr.UseFullClear(monsters);

            float expected = 100f - SurvivorSupplyMgr.FullClearDamage;
            Assert.AreEqual(expected, m1.Hp, "mọi monster đều dmg");
            Assert.AreEqual(expected, m2.Hp, "mọi monster đều dmg (không check radius)");
        }

        [Test]
        public void FullClear_NoMonsters_NoOp_NoCrash()
        {
            var mgr = MakeMgr();
            mgr.UseFullClear(null);
            mgr.UseFullClear(new List<SurvivorMonster>());
            // không exception = pass
        }

        // ------------------------------------------------------------------
        // Cooldown FSM (cd riêng từng slot)
        // ------------------------------------------------------------------

        [Test]
        public void Cooldown_PerSlot_Independent()
        {
            var mgr = MakeMgr(
                MakeSupplyDef(77, SurvivorSupplyTag.Heal),
                MakeSupplyDef(88, SurvivorSupplyTag.Bomb));

            Assert.IsTrue(mgr.TryUse(SupplyKind.Heal), "heal sẵn sàng");
            Assert.IsFalse(mgr.TryUse(SupplyKind.Heal), "heal đang cd");
            Assert.IsTrue(mgr.TryUse(SupplyKind.Bomb), "bomb cd RIÊNG — dùng được ngay");
            Assert.IsFalse(mgr.TryUse(SupplyKind.Bomb), "bomb đang cd");

            Assert.AreEqual(SurvivorSupplyMgr.HealCooldown, mgr.GetSlot(SupplyKind.Heal).Remaining, 1e-4f);
            Assert.AreEqual(SurvivorSupplyMgr.BombCooldown, mgr.GetSlot(SupplyKind.Bomb).Remaining, 1e-4f);

            // tick nhỏ hơn cd heal → cả 2 chưa sẵn sàng (bomb cd 12 > heal cd 10)
            mgr.Tick(SurvivorSupplyMgr.HealCooldown - 0.1f);
            Assert.IsFalse(mgr.TryUse(SupplyKind.Heal), "cd chưa hết");
            Assert.IsFalse(mgr.TryUse(SupplyKind.Bomb), "bomb cd 12 — chưa hết sau 9.9s");

            // đủ cho heal (10s) nhưng chưa đủ cho bomb
            mgr.Tick(0.2f);
            Assert.IsTrue(mgr.TryUse(SupplyKind.Heal), "heal dùng lại sau cd");
            Assert.IsFalse(mgr.TryUse(SupplyKind.Bomb), "bomb vẫn đang cd");

            // đủ luôn cho bomb (12s) — tick dư để khỏi float dust
            mgr.Tick(2.0f);
            Assert.IsTrue(mgr.TryUse(SupplyKind.Bomb), "bomb dùng lại sau cd riêng");
        }

        [Test]
        public void Cooldown_OwnSlots_AlwaysEnabled()
        {
            var mgr = MakeMgr(); // không def nào
            Assert.IsTrue(mgr.TryUse(SupplyKind.Magnet), "magnet own — luôn enabled");
            Assert.IsTrue(mgr.TryUse(SupplyKind.FullClear), "full-clear own — luôn enabled");
            Assert.IsFalse(mgr.TryUse(SupplyKind.Magnet), "magnet đang cd sau khi dùng");
        }

        // ------------------------------------------------------------------
        // Fail-closed (SkillDef chưa staged / tag thiếu)
        // ------------------------------------------------------------------

        [Test]
        public void FailClosed_NoDefs_HealBombDisabled()
        {
            var mgr = MakeMgr(); // defs rỗng
            Assert.IsFalse(mgr.TryUse(SupplyKind.Heal), "không def heal → disabled");
            Assert.IsFalse(mgr.TryUse(SupplyKind.Bomb), "không def bomb → disabled");
            Assert.IsFalse(mgr.UseHeal(), "UseHeal cũng no-op");
            // không exception = pass
        }

        [Test]
        public void FailClosed_NullDefs_NoCrash()
        {
            var mgr = new SurvivorSupplyMgr();
            mgr.Setup(null); // không crash
            Assert.IsFalse(mgr.TryUse(SupplyKind.Heal));
        }

        [Test]
        public void FailClosed_UntaggedOrAura_Ignored()
        {
            var mgr = MakeMgr(
                MakeSupplyDef(1, SurvivorSupplyTag.None),
                MakeSupplyDef(2, SurvivorSupplyTag.Aura));
            Assert.IsFalse(mgr.TryUse(SupplyKind.Heal), "None/Aura không enable heal");
            Assert.IsFalse(mgr.TryUse(SupplyKind.Bomb), "None/Aura không enable bomb");
            Assert.IsTrue(mgr.TryUse(SupplyKind.FullClear), "own slots vẫn dùng được");
        }

        [Test]
        public void FailClosed_NullDefInsideList_Skipped()
        {
            var mgr = new SurvivorSupplyMgr();
            mgr.Setup(new SkillDef[] { null, MakeSupplyDef(77, SurvivorSupplyTag.Heal) });
            Assert.IsTrue(mgr.TryUse(SupplyKind.Heal), "null def bị bỏ qua, def hợp lệ vẫn vào");
        }
    }
}
