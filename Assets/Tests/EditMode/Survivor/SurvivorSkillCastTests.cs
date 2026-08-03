// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Survivor — SurvivorSkillCastTests
// Ticket 27 self-check (pure logic, không scene/PlayMode — spec Testing Decisions):
//  - Fan spread parity PC CastSpread: dir_i = castDir + Param1×(i−half), đơn vị
//    1/64 vòng (MaxMissleDir=64), half = ChildSkillNum/2 int div, offset =
//    Param2 px dọc theo dir_i. KHÔNG chia 360° quanh caster.
//  - Scaling own-design (LvlData PC toàn 0 → công thức riêng) + cooldown.
//  - Roster learn/level/cd tick.
//  - Fail-closed: melee IsMelee sai → bỏ child visual; precast rỗng → proxy.
//  - Attribution: ledger SumSkillDamage + TopSource = kill credit.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using UnityEngine;
using VLTK.Survivor;

namespace VLTK.Tests.Survivor
{
    public class SurvivorSkillCastTests
    {
        // ------------------------------------------------------------------
        // helpers
        // ------------------------------------------------------------------

        private sealed class StubVisual : IActorVisual
        {
            public void SyncPosition(Vector3 p) { }
            public void SetDirection(int d) { }
            public void PlayMove(bool m) { }
            public void SetAlive(bool a) { }
        }

        private static SkillDef MakeDef(int id = 1)
        {
            var def = SkillDef.FromRow(new SkillRow { Id = id, Form = 7 });
            def.MaxLevel = 10;
            return def;
        }

        private static void AssertDir(Vector2 actual, float x, float y, float tol = 1e-3f)
        {
            Assert.AreEqual(x, actual.x, tol, $"dir.x — actual {actual}");
            Assert.AreEqual(y, actual.y, tol, $"dir.y — actual {actual}");
        }

        // ------------------------------------------------------------------
        // fan spread parity (1/64 vòng, KHÔNG chia 360°)
        // ------------------------------------------------------------------

        [Test]
        public void FanCount_MaxOne_MeleeZero()
        {
            var ranged = MakeDef();
            ranged.ChildSkillNum = 0;
            Assert.AreEqual(1, SkillCastRuntime.FanCount(ranged), "cnt 0 → own: 1 (PC form khác code path)");
            ranged.ChildSkillNum = 5;
            Assert.AreEqual(5, SkillCastRuntime.FanCount(ranged));

            var melee = MakeDef();
            melee.Form = 12;
            Assert.AreEqual(0, SkillCastRuntime.FanCount(melee), "melee không fan");
        }

        [Test]
        public void FanSpread_DirFormula_1of64Turns()
        {
            // P1=8, count=5: half=2 → dirs = 8×(i−2)/64 vòng = −90°, −45°, 0°, +45°, +90°
            var def = MakeDef();
            def.FanParam1 = 8;
            def.ChildSkillNum = 5;
            var plan = SkillCastRuntime.PlanCast(def, 1, new Vector2(1f, 0f));

            Assert.AreEqual(5, plan.Missiles.Length);
            AssertDir(plan.Missiles[0].Dir, 0f, -1f, 1e-3f);        // −90°
            AssertDir(plan.Missiles[1].Dir, 0.7071068f, -0.7071068f, 1e-3f); // −45°
            AssertDir(plan.Missiles[2].Dir, 1f, 0f);                // 0°
            AssertDir(plan.Missiles[3].Dir, 0.7071068f, 0.7071068f, 1e-3f);  // +45°
            AssertDir(plan.Missiles[4].Dir, 0f, 1f, 1e-3f);         // +90°
        }

        [Test]
        public void FanSpread_Not360DegreeAroundCaster()
        {
            // P1=8 count=5: bước = 45° quanh castDir — nếu sai (chia 360°/5 = 72°
            // quanh caster) đạn đầu phải là góc 72° ≠ −90°.
            var def = MakeDef();
            def.FanParam1 = 8;
            def.ChildSkillNum = 5;
            var plan = SkillCastRuntime.PlanCast(def, 1, new Vector2(1f, 0f));
            AssertDir(plan.Missiles[0].Dir, 0f, -1f, 1e-3f);
            Assert.AreEqual(-90f, Mathf.Atan2(plan.Missiles[0].Dir.y, plan.Missiles[0].Dir.x) * Mathf.Rad2Deg, 1e-3f);
        }

        [Test]
        public void FanSpread_Offset_Param2Px_AlongDir()
        {
            // Param2 = 40 px → offset 1.0 unit dọc theo dir_i (ppu 40)
            var def = MakeDef();
            def.FanParam1 = 8;
            def.ChildSkillNum = 3;
            def.FanParam2 = 40;
            var plan = SkillCastRuntime.PlanCast(def, 1, new Vector2(1f, 0f));

            Assert.AreEqual(3, plan.Missiles.Length);
            foreach (var m in plan.Missiles)
            {
                Assert.AreEqual(1f, m.Offset.magnitude, 1e-3f, "offset = Param2 px ÷ 40");
                Assert.AreEqual(0f, Vector2.Dot(m.Dir.normalized, m.Offset.normalized) - 1f, 1e-3f,
                    "offset dọc theo dir_i (nFirstStep), KHÔNG vuông góc");
            }

            var noOffset = MakeDef();
            noOffset.FanParam2 = 0;
            var p2 = SkillCastRuntime.PlanCast(noOffset, 1, new Vector2(1f, 0f));
            Assert.AreEqual(Vector2.zero, p2.Missiles[0].Offset);
        }

        [Test]
        public void FanSpread_PcParityAnchor_Count1_OffsetOnly()
        {
            // Real data: form-7 + P1=100 + P2=32400/64800 + cnt=1 (8 skill, verify thật).
            // i=0, half=0 → dir = castDir, offset = P2/40 unit dọc theo castDir.
            var def = MakeDef();
            def.FanParam1 = 100;
            def.FanParam2 = 32400;
            def.ChildSkillNum = 1;
            var plan = SkillCastRuntime.PlanCast(def, 1, new Vector2(0f, 1f));
            Assert.AreEqual(1, plan.Missiles.Length);
            AssertDir(plan.Missiles[0].Dir, 0f, 1f);
            AssertDir(plan.Missiles[0].Offset, 0f, 810f, 1e-2f); // 32400 px ÷ 40 = 810 unit
        }

        [Test]
        public void FanSpread_Param1Zero_AllSameDir()
        {
            var def = MakeDef();
            def.FanParam1 = 0;
            def.ChildSkillNum = 3;
            var plan = SkillCastRuntime.PlanCast(def, 1, new Vector2(1f, 0f));
            foreach (var m in plan.Missiles) AssertDir(m.Dir, 1f, 0f);
        }

        // ------------------------------------------------------------------
        // scaling + cooldown (own-design; LvlData PC toàn 0 đã verify)
        // ------------------------------------------------------------------

        [Test]
        public void Damage_ScalesByLevel_MeleeMul_FanMul()
        {
            var ranged = MakeDef();
            Assert.AreEqual(2f, SkillCastRuntime.DamageFor(ranged, 1), 1e-4f);
            Assert.AreEqual(8f, SkillCastRuntime.DamageFor(ranged, 5), 1e-4f); // 2 + 1.5×4

            var melee = MakeDef();
            melee.Form = 12;
            Assert.AreEqual(2.4f, SkillCastRuntime.DamageFor(melee, 1), 1e-4f, "melee ×1.2");

            // fan >1 đạn: mỗi đạn ×0.8 (tổng DPS không bùng)
            var fan = MakeDef();
            fan.FanParam1 = 8;
            fan.ChildSkillNum = 3;
            var plan = SkillCastRuntime.PlanCast(fan, 1, new Vector2(1f, 0f));
            Assert.AreEqual(1.6f, plan.Damage, 1e-4f, "2 × 0.8");
            var single = SkillCastRuntime.PlanCast(ranged, 1, new Vector2(1f, 0f));
            Assert.AreEqual(2f, single.Damage, 1e-4f, "1 đạn: không nhân fan mul");
        }

        [Test]
        public void Cooldown_ClampsToMin_MeleeFaster()
        {
            var ranged = MakeDef();
            Assert.AreEqual(1f, SkillCastRuntime.CooldownFor(ranged, 1), 1e-4f);
            Assert.AreEqual(0.4f, SkillCastRuntime.CooldownFor(ranged, 50), 1e-4f, "min 0.4");

            var melee = MakeDef();
            melee.Form = 12;
            Assert.AreEqual(0.8f, SkillCastRuntime.CooldownFor(melee, 1), 1e-4f, "melee ×0.8");
        }

        // ------------------------------------------------------------------
        // roster + cooldown tick
        // ------------------------------------------------------------------

        [Test]
        public void Roster_Learn_LevelsUp_CapsMaxLevel()
        {
            var rt = new SkillCastRuntime();
            var def = MakeDef();
            def.MaxLevel = 5;
            rt.Learn(def);
            rt.Learn(def);
            Assert.AreEqual(2, rt.GetLevel(def.Id));
            rt.Learn(def, 4);
            Assert.AreEqual(5, rt.GetLevel(def.Id), "cap MaxLevel");
            Assert.AreEqual(1, rt.Roster.Count, "trùng id không thêm entry");
            Assert.IsTrue(rt.HasAnySkill);
        }

        [Test]
        public void TryCast_Cooldown_Tick()
        {
            var rt = new SkillCastRuntime();
            var def = MakeDef();
            def.ChildSkillNum = 3;
            def.FanParam1 = 8;
            rt.Learn(def);

            Assert.IsTrue(rt.TryCast(new Vector2(1f, 0f), out var p1));
            Assert.AreEqual(def.Id, p1.SkillId);
            Assert.AreEqual(3, p1.Missiles.Length);

            Assert.IsFalse(rt.TryCast(new Vector2(1f, 0f), out _), "cd chưa hết → không cast (auto-attack tiếp)");

            rt.Tick(0.5f);
            Assert.IsFalse(rt.TryCast(new Vector2(1f, 0f), out _));
            rt.Tick(0.6f);
            Assert.IsTrue(rt.TryCast(new Vector2(1f, 0f), out _), "cd hết → cast lại");
        }

        // ------------------------------------------------------------------
        // fail-closed: precast + melee visual
        // ------------------------------------------------------------------

        [Test]
        public void PreCast_StagedCarried_UnstagedProxy()
        {
            var staged = MakeDef();
            staged.PreCastSprUid = "deadbeef";
            var p1 = SkillCastRuntime.PlanCast(staged, 1, Vector2.right);
            Assert.AreEqual("deadbeef", p1.PreCastSprUid, "staged → hiển thị SPR");

            var unstaged = MakeDef();
            unstaged.PreCastSprUid = "";
            var p2 = SkillCastRuntime.PlanCast(unstaged, 1, Vector2.right);
            Assert.IsEmpty(p2.PreCastSprUid, "rỗng → proxy màu (fail-closed, không bịa path)");
        }

        [Test]
        public void Melee_NoPreCast_ChildVisual_IsMeleeGate()
        {
            // melee hợp lệ: không precast (PC), child missile là visual
            var melee = MakeDef();
            melee.Form = 12;
            melee.IsMelee = true;
            melee.ChildMissile = new MissileVisualInfo { Id = 219, AnimFileUid = "abc12345" };
            melee.PreCastSprUid = "deadbeef"; // staged nhưng melee không dùng
            var p = SkillCastRuntime.PlanCast(melee, 1, Vector2.right);
            Assert.IsTrue(p.IsMelee);
            Assert.IsEmpty(p.PreCastSprUid, "melee không precast");
            Assert.AreEqual("abc12345", p.MissileSprUid, "visual = child missile");
            Assert.IsNull(p.Missiles, "melee không đạn");
            Assert.AreEqual(0, SkillCastRuntime.FanCount(melee));

            // IsMelee set sai → fail-closed: bỏ child visual → proxy
            var broken = MakeDef();
            broken.Form = 12;
            broken.IsMelee = false;
            broken.ChildMissile = new MissileVisualInfo { Id = 220, AnimFileUid = "abc12345" };
            var pb = SkillCastRuntime.PlanCast(broken, 1, Vector2.right);
            Assert.IsTrue(pb.IsMelee, "form 12 vẫn melee hit");
            Assert.IsEmpty(pb.MissileSprUid, "IsMelee sai → không gán child visual (proxy)");
        }

        [Test]
        public void ChildVisual_Unstaged_Proxy()
        {
            var def = MakeDef();
            def.ChildMissile = new MissileVisualInfo { Id = 1, AnimFileUid = "" };
            var p = SkillCastRuntime.PlanCast(def, 1, Vector2.right);
            Assert.IsEmpty(p.MissileSprUid, "child chưa staged → proxy");

            var noChild = MakeDef();
            noChild.ChildMissile = null;
            Assert.IsEmpty(SkillCastRuntime.PlanCast(noChild, 1, Vector2.right).MissileSprUid);
        }

        [Test]
        public void MissileSpeedLife_FromChildMissile_OrDefaults()
        {
            var def = MakeDef();
            def.ChildMissile = new MissileVisualInfo { Id = 1, Speed = 25f, LifeTime = 9f };
            var p = SkillCastRuntime.PlanCast(def, 1, Vector2.right);
            Assert.AreEqual(25f, p.MissileSpeed, 1e-4f);
            Assert.AreEqual(9f, p.MissileLife, 1e-4f);

            var bare = MakeDef();
            var p2 = SkillCastRuntime.PlanCast(bare, 1, Vector2.right);
            Assert.AreEqual(SkillCastRuntime.DefaultMissileSpeed, p2.MissileSpeed, 1e-4f);
            Assert.AreEqual(SkillCastRuntime.DefaultMissileLife, p2.MissileLife, 1e-4f);
        }

        // ------------------------------------------------------------------
        // attribution: ledger + kill credit (top source)
        // ------------------------------------------------------------------

        [Test]
        public void Attribution_HitWritesLedger_KillSourceTop()
        {
            var go = new GameObject("monster_test");
            var m = go.AddComponent<SurvivorMonster>();
            m.MaxHp = 10f;
            m.VisualRes = "enemy005"; // tránh resolver IO; JxNpcVisual.Start không chạy trong EditMode
            m.Init(new StubVisual(), Vector3.zero);
            Assert.AreEqual(10f, m.Hp, 1e-4f);

            var caster = new object();
            var srcA = new SkillImpactSource(11, 0);
            var srcB = new SkillImpactSource(22, 0);

            Assert.IsFalse(m.TakeDamage(3f, srcA, caster), "chưa chết");
            Assert.IsFalse(m.TakeDamage(2f, srcB, caster));
            Assert.AreEqual(5f, m.Hp, 1e-4f, "dmg cộng dồn");
            Assert.AreEqual(3, m.Ledger.GetTotal(srcA, caster), "hit A ghi ledger");
            Assert.AreEqual(2, m.Ledger.GetTotal(srcB, caster), "hit B ghi ledger");
            Assert.AreEqual(5, m.Ledger.TotalDamage);

            // kill credit: TopSource = skill damage nhiều nhất (cùng code path Die() dùng)
            Assert.IsTrue(m.Ledger.TryGetTopSource(out var top, out var topCaster, out var total));
            Assert.AreEqual(11, top.SkillId);
            Assert.AreSame(caster, topCaster);
            Assert.AreEqual(3, total);
        }

        [Test]
        public void Attribution_AutoAttack_NoCaster_NoLedger()
        {
            // auto-attack P1: source None + caster null → không ghi ledger (gem flow XP)
            var m = new GameObject("monster_test2").AddComponent<SurvivorMonster>();
            m.MaxHp = 5f;
            m.VisualRes = "enemy005";
            m.Init(new StubVisual(), Vector3.zero);
            Assert.IsFalse(m.TakeDamage(1f, SkillImpactSource.None, null));
            Assert.AreEqual(0, m.Ledger.TotalDamage, "auto-attack không attribution");
        }
    }
}
