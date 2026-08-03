// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Survivor — SurvivorImpactTests
// Ticket 28 self-check: 4-bucket math, DOT tick (loop/TickWhenAdd/RemoveAfterDot/
// heal/SourceBuffer), stun lifecycle + gates, stack/replace/refresh, attribution
// → kill credit. Pure logic, không scene, không MonoBehaviour (spec Testing Decisions).
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Survivor;

namespace VLTK.Tests.Survivor
{
    public class SurvivorImpactTests
    {
        // ponytail: stub inline, không spin actor scene.
        private sealed class TestDamageable : ISurvivorDamageable
        {
            public int Hp { get; private set; }
            public int MaxHp { get; }
            public bool Dead => Hp <= 0;
            public DamageInfo LastInfo { get; private set; }
            public readonly SurvivorDamageLedger Ledger = new SurvivorDamageLedger();

            public TestDamageable(int hp) { Hp = hp; MaxHp = hp; }
            public TestDamageable(int hp, int maxHp) { Hp = hp; MaxHp = maxHp; }

            public void ApplyDot(DamageInfo info)
            {
                LastInfo = info;
                if (info.IsHeal) Hp = System.Math.Min(MaxHp, Hp + info.Damage);
                else Hp -= info.Damage;
            }
        }

        private static SurvivorActorAttr MakeAttr(float damage = 1f)
        {
            var a = new SurvivorActorAttr { BaseDamage = damage };
            a.Recompute();
            return a;
        }

        private static BuffDef MakeDef(int buffId, BuffAttrConfig level)
        {
            var def = new BuffDef { BuffId = buffId };
            def.Levels.Add(level);
            return def;
        }

        private static BuffAttrConfig Level(BuffStateID states = BuffStateID.None, float dur = 10f,
            ActorAttrImpact[] attr = null, SkillAttrDamageData dot = null, BuffDotTickConfig tick = null,
            int stackNum = 1)
        {
            return new BuffAttrConfig
            {
                StackNum = stackNum,
                DurTime = dur,
                States = states,
                AttrData = attr ?? System.Array.Empty<ActorAttrImpact>(),
                DotDamageData = dot,
                DotTick = tick,
            };
        }

        private static SkillAttrDamageData DotDmg(float param1 = 1f, float param2 = 0f, bool heal = false)
        {
            return new SkillAttrDamageData { AttrType = ActorAttrDataType.Damage, Param1 = param1, Param2 = param2, IsHeal = heal };
        }

        // ================= 4-bucket math =================

        [Test]
        public void BucketMath_Order_AbThenRelThenMulThenEffect()
        {
            var a = MakeAttr(damage: 1f);
            var m = a.ImpactMgr;
            m.Add(new ActorAttrImpact(ActorAttrDataType.Damage, ActorAttrAddType.Absolute, 0.5f));
            a.Recompute();
            Assert.AreEqual(1.5f, a.FinalDamage, 1e-4f, "abs +0.5");

            m.Add(new ActorAttrImpact(ActorAttrDataType.Damage, ActorAttrAddType.SumPercent, 0.5f));
            a.Recompute();
            Assert.AreEqual(2.25f, a.FinalDamage, 1e-4f, "rel +50%");

            m.Add(new ActorAttrImpact(ActorAttrDataType.Damage, ActorAttrAddType.MulPercent, 0.5f));
            a.Recompute();
            Assert.AreEqual(3.375f, a.FinalDamage, 1e-4f, "mul +50%");

            m.Add(new ActorAttrImpact(ActorAttrDataType.Damage, ActorAttrAddType.Effect, 0.25f));
            a.Recompute();
            Assert.AreEqual(3.625f, a.FinalDamage, 1e-4f, "effect +0.25 cuối");
        }

        [Test]
        public void BucketMath_RelPercent_AreAdditive_MulPercent_AreChain()
        {
            var a = MakeAttr(damage: 10f);
            var m = a.ImpactMgr;
            m.Add(new ActorAttrImpact(ActorAttrDataType.Damage, ActorAttrAddType.SumPercent, 0.2f));
            m.Add(new ActorAttrImpact(ActorAttrDataType.Damage, ActorAttrAddType.SumPercent, 0.3f));
            a.Recompute();
            Assert.AreEqual(15f, a.FinalDamage, 1e-4f, "rel cộng dồn 10*1.5");

            var b = MakeAttr(damage: 10f);
            b.ImpactMgr.Add(new ActorAttrImpact(ActorAttrDataType.Damage, ActorAttrAddType.MulPercent, 0.2f));
            b.ImpactMgr.Add(new ActorAttrImpact(ActorAttrDataType.Damage, ActorAttrAddType.MulPercent, 0.3f));
            b.Recompute();
            Assert.AreEqual(15.6f, b.FinalDamage, 1e-4f, "mul chain 10*1.2*1.3");
        }

        [Test]
        public void ImpactMgr_Clear_RevertsToBase()
        {
            var a = MakeAttr(damage: 1f);
            var m = a.ImpactMgr;
            m.Add(new ActorAttrImpact(ActorAttrDataType.Damage, ActorAttrAddType.Absolute, 5f));
            m.Add(new ActorAttrImpact(ActorAttrDataType.MoveSpeed, ActorAttrAddType.SumPercent, -0.5f));
            a.Recompute();
            Assert.AreEqual(6f, a.FinalDamage, 1e-4f);
            Assert.AreEqual(2.5f, a.FinalMoveSpeed, 1e-4f);

            m.Clear();
            a.Recompute();
            Assert.AreEqual(1f, a.FinalDamage, 1e-4f, "revert");
            Assert.AreEqual(5f, a.FinalMoveSpeed, 1e-4f, "revert");
        }

        // ================= buff apply/remove → attr =================

        [Test]
        public void Buff_ApplyAddsAttr_RemoveReverts()
        {
            var attr = MakeAttr(damage: 1f);
            var sm = new SurvivorActorSM();
            var t = new TestDamageable(100);
            var mgr = new SurvivorBuffMgr(attr, sm, t, t.Ledger);

            var def = MakeDef(101, Level(dur: 30f, attr: new[] {
                new ActorAttrImpact(ActorAttrDataType.Damage, ActorAttrAddType.Absolute, 2f),
                new ActorAttrImpact(ActorAttrDataType.MoveSpeed, ActorAttrAddType.SumPercent, -0.3f),
            }));

            mgr.AddBuff(def, caster: "casterA", source: new SkillImpactSource(5, 101));
            Assert.AreEqual(3f, attr.FinalDamage, 1e-4f, "abs +2");
            Assert.AreEqual(3.5f, attr.FinalMoveSpeed, 1e-4f, "-30%");

            mgr.RmvBuff(101);
            Assert.AreEqual(1f, attr.FinalDamage, 1e-4f, "revert");
            Assert.AreEqual(5f, attr.FinalMoveSpeed, 1e-4f, "revert");
            Assert.AreEqual(0, mgr.BuffCount);
        }

        // ================= DOT =================

        [Test]
        public void Dot_LoopTick_IntervalAndSourceBufferAndLedger()
        {
            var attr = MakeAttr(damage: 10f); // caster attr → dot val 10
            var sm = new SurvivorActorSM();
            var t = new TestDamageable(100);
            var mgr = new SurvivorBuffMgr(attr, sm, t, t.Ledger);

            var def = MakeDef(201, Level(dur: 10f, dot: DotDmg(1f, 0f), tick: new BuffDotTickConfig(1f)));
            mgr.AddBuff(def, caster: "casterX", source: new SkillImpactSource(7, 201), casterAttr: attr);

            mgr.Tick(0.4f);
            Assert.AreEqual(100, t.Hp, "chưa tới interval");
            Assert.AreEqual(0, t.Ledger.TotalDamage, "chưa có tick");

            mgr.Tick(0.6f); // cd 1.0 → tick đầu
            Assert.AreEqual(90, t.Hp, "tick 1");
            Assert.AreEqual(10, t.Ledger.TotalDamage, "ledger ghi 10");
            Assert.AreEqual(10, t.Ledger.GetTotal(new SkillImpactSource(7, 201), "casterX"), "đúng source");
            Assert.AreEqual(DamageSourceType.SourceBuffer, t.LastInfo.SourceType, "sourceType=SourceBuffer");
            Assert.AreEqual(7, t.LastInfo.Source.SkillId, "skillId");
            Assert.AreEqual(201, t.LastInfo.Source.BuffId, "buffId");

            mgr.Tick(0.9f);
            Assert.AreEqual(90, t.Hp, "chưa đủ 1s nữa");
            mgr.Tick(0.1f);
            Assert.AreEqual(80, t.Hp, "tick 2 đúng interval");
        }

        [Test]
        public void Dot_TickWhenAdd_InstantTick()
        {
            var attr = MakeAttr(damage: 10f);
            var sm = new SurvivorActorSM();
            var t = new TestDamageable(100);
            var mgr = new SurvivorBuffMgr(attr, sm, t, t.Ledger);

            var def = MakeDef(202, Level(dur: 10f, dot: DotDmg(1f), tick: new BuffDotTickConfig(5f, tickWhenAdd: true)));
            mgr.AddBuff(def, caster: "c", source: new SkillImpactSource(8, 202), casterAttr: attr);

            Assert.AreEqual(90, t.Hp, "tick ngay khi apply");
            Assert.AreEqual(1, t.Ledger.SourceCount);
        }

        [Test]
        public void Dot_RemoveAfterDot_BuffRemovedAfterTick()
        {
            var attr = MakeAttr(damage: 10f);
            var sm = new SurvivorActorSM();
            var t = new TestDamageable(100);
            var mgr = new SurvivorBuffMgr(attr, sm, t, t.Ledger);

            // single-tick DOT: TickWhenAdd + RemoveAfterDot
            var def = MakeDef(203, Level(dur: 10f, dot: DotDmg(1f),
                tick: new BuffDotTickConfig(5f, tickWhenAdd: true, removeAfterDot: true)));
            mgr.AddBuff(def, caster: "c", source: new SkillImpactSource(9, 203), casterAttr: attr);

            Assert.AreEqual(90, t.Hp, "1 tick");
            mgr.Tick(0f); // xử lý deferred remove (parity MarkToFree → frame sau)
            Assert.IsFalse(mgr.HasBuff(203), "buff bị gỡ sau dot");
        }

        [Test]
        public void Dot_HealVariant_HealsTarget_NoLedgerDamage()
        {
            var attr = MakeAttr(damage: 10f);
            var sm = new SurvivorActorSM();
            var t = new TestDamageable(50, 100);
            var mgr = new SurvivorBuffMgr(attr, sm, t, t.Ledger);

            var def = MakeDef(204, Level(dur: 10f, dot: DotDmg(1f, 0f, heal: true), tick: new BuffDotTickConfig(2f)));
            mgr.AddBuff(def, caster: "c", source: new SkillImpactSource(10, 204), casterAttr: attr);

            mgr.Tick(2f);
            Assert.AreEqual(60, t.Hp, "heal +10 (100 max)");
            Assert.AreEqual(0, t.Ledger.TotalDamage, "heal không vào damage ledger");
            Assert.IsTrue(t.LastInfo.IsHeal, "IsHeal flag");
        }

        [Test]
        public void Dot_ScaledByCasterAttr_Formula()
        {
            var attr = MakeAttr(damage: 10f);
            var sm = new SurvivorActorSM();
            var t = new TestDamageable(100);
            var mgr = new SurvivorBuffMgr(attr, sm, t, t.Ledger);

            // val = caster Damage(10) * 0.5 + 2 = 7
            var def = MakeDef(205, Level(dur: 10f, dot: DotDmg(0.5f, 2f), tick: new BuffDotTickConfig(1f)));
            var b = mgr.AddBuff(def, caster: "c", source: new SkillImpactSource(11, 205), casterAttr: attr);

            Assert.AreEqual(7, b.Dot.DotVal, "formula attr*Param1+Param2");
            mgr.Tick(1f);
            Assert.AreEqual(93, t.Hp);
        }

        [Test]
        public void Dot_StopsWhenBuffExpires()
        {
            var attr = MakeAttr(damage: 10f);
            var sm = new SurvivorActorSM();
            var t = new TestDamageable(100);
            var mgr = new SurvivorBuffMgr(attr, sm, t, t.Ledger);

            // dur 2.5s, tick 1s → tick tại t=1, t=2; buff hết t=2.5 → hết tick
            var def = MakeDef(207, Level(dur: 2.5f, dot: DotDmg(1f), tick: new BuffDotTickConfig(1f)));
            mgr.AddBuff(def, caster: "c", source: new SkillImpactSource(12, 207), casterAttr: attr);

            mgr.Tick(1f); Assert.AreEqual(90, t.Hp, "tick 1");
            mgr.Tick(1f); Assert.AreEqual(80, t.Hp, "tick 2");
            mgr.Tick(1f); Assert.AreEqual(70, t.Hp, "tick cuối cùng frame expire (dot trước remove)");
            Assert.IsFalse(mgr.HasBuff(207), "buff hết hạn");

            mgr.Tick(5f);
            Assert.AreEqual(70, t.Hp, "hết buff → DOT dừng hẳn");
        }

        [Test]
        public void Dot_StackLevelChange_ReinitWithNewConfig()
        {
            var attr = MakeAttr(damage: 10f);
            var sm = new SurvivorActorSM();
            var t = new TestDamageable(100);
            var mgr = new SurvivorBuffMgr(attr, sm, t, t.Ledger);

            // level 1: dot 10/tick; level 2: dot 20/tick
            var def = new BuffDef { BuffId = 208, ReplaceType = BuffReplaceType.Stack };
            def.Levels.Add(Level(dur: 10f, dot: DotDmg(1f, 0f), tick: new BuffDotTickConfig(1f), stackNum: 1));
            def.Levels.Add(Level(dur: 10f, dot: DotDmg(1f, 10f), tick: new BuffDotTickConfig(1f), stackNum: 2));

            var b1 = mgr.AddBuff(def, "c", new SkillImpactSource(13, 208), attr);
            Assert.AreEqual(10, b1.Dot.DotVal, "level 1 dot");

            var b2 = mgr.AddBuff(def, "c", new SkillImpactSource(13, 208), attr);
            Assert.AreEqual(2, b2.Stack);
            Assert.AreEqual(20, b2.Dot.DotVal, "level 2 → dot re-init theo config mới");

            mgr.Tick(1f);
            Assert.AreEqual(80, t.Hp, "tick theo dot level 2");
        }

        [Test]
        public void Dot_StackLevelWithoutDot_StopsOldDot()
        {
            var attr = MakeAttr(damage: 10f);
            var sm = new SurvivorActorSM();
            var t = new TestDamageable(100);
            var mgr = new SurvivorBuffMgr(attr, sm, t, t.Ledger);

            // level 1: có dot; level 2: không dot → dot phải tắt
            var def = new BuffDef { BuffId = 209, ReplaceType = BuffReplaceType.Stack };
            def.Levels.Add(Level(dur: 10f, dot: DotDmg(1f), tick: new BuffDotTickConfig(1f), stackNum: 1));
            def.Levels.Add(Level(dur: 10f, attr: new[] { new ActorAttrImpact(ActorAttrDataType.Damage, ActorAttrAddType.Absolute, 1f) }, stackNum: 2));

            var b1 = mgr.AddBuff(def, "c", new SkillImpactSource(14, 209), attr);
            Assert.IsNotNull(b1.Dot, "level 1 có dot");

            var b2 = mgr.AddBuff(def, "c", new SkillImpactSource(14, 209), attr);
            Assert.AreEqual(2, b2.Stack);
            Assert.IsNull(b2.Dot, "level 2 không dot → dot cũ tắt");
            Assert.AreEqual(11f, attr.FinalDamage, 1e-4f, "attr level 2 vẫn apply (base 10 + 1)");

            mgr.Tick(3f);
            Assert.AreEqual(100, t.Hp, "không còn tick nào");
        }

        // ================= control states + stun lifecycle =================

        [Test]
        public void Stun_Lifecycle_BlocksMoveSkill_ExpireReturnsIdle()
        {
            var attr = MakeAttr();
            var sm = new SurvivorActorSM();
            var t = new TestDamageable(100);
            var mgr = new SurvivorBuffMgr(attr, sm, t, t.Ledger);

            var def = MakeDef(301, Level(states: BuffStateID.Stun, dur: 2f));
            mgr.AddBuff(def, caster: "c", source: SkillImpactSource.None);

            Assert.AreEqual(ActorStateID.Stun, sm.State, "SM vào Stun");
            Assert.AreEqual(2f, sm.StunDuration, 1e-4f, "duration param");
            Assert.IsFalse(mgr.CanMove, "stun chặn move");
            Assert.IsFalse(mgr.CanSkill, "stun chặn skill");

            mgr.Tick(2f); // hết duration → expire

            Assert.AreEqual(ActorStateID.Idle, sm.State, "Finish_Stun → Idle");
            Assert.AreEqual(0f, sm.StunDuration, 1e-4f);
            Assert.IsTrue(mgr.CanMove, "mở lại");
            Assert.IsTrue(mgr.CanSkill, "mở lại");
            Assert.IsFalse(mgr.HasBuff(301), "buff hết hạn");
        }

        [Test]
        public void NoMove_BlocksMoveOnly()
        {
            var attr = MakeAttr();
            var sm = new SurvivorActorSM();
            var t = new TestDamageable(100);
            var mgr = new SurvivorBuffMgr(attr, sm, t, t.Ledger);

            mgr.AddBuff(MakeDef(302, Level(states: BuffStateID.NoMove, dur: 5f)), "c", SkillImpactSource.None);

            Assert.IsFalse(mgr.CanMove, "chặn move");
            Assert.IsTrue(mgr.CanSkill, "skill vẫn dùng được");
            Assert.IsFalse(mgr.HasState(BuffStateID.Stun), "không phải stun");
        }

        [Test]
        public void NoSkill_BlocksSkillOnly()
        {
            var attr = MakeAttr();
            var sm = new SurvivorActorSM();
            var t = new TestDamageable(100);
            var mgr = new SurvivorBuffMgr(attr, sm, t, t.Ledger);

            mgr.AddBuff(MakeDef(303, Level(states: BuffStateID.NoSkill, dur: 5f)), "c", SkillImpactSource.None);

            Assert.IsTrue(mgr.CanMove, "move vẫn chạy");
            Assert.IsFalse(mgr.CanSkill, "chặn skill");
        }

        [Test]
        public void Sleep_RemovedOnDamage()
        {
            var attr = MakeAttr();
            var sm = new SurvivorActorSM();
            var t = new TestDamageable(100);
            var mgr = new SurvivorBuffMgr(attr, sm, t, t.Ledger);

            mgr.AddBuff(MakeDef(304, Level(states: BuffStateID.Sleep, dur: 30f)), "c", SkillImpactSource.None);
            Assert.IsTrue(mgr.HasState(BuffStateID.Sleep), "sleep active");

            mgr.NotifyDamaged(); // parity RemoveSleepTypeBuff

            Assert.IsFalse(mgr.HasBuff(304), "sleep bị gỡ khi nhận damage");
            Assert.IsFalse(mgr.HasState(BuffStateID.Sleep), "state cleared");
        }

        // ================= stack / replace / refresh =================

        [Test]
        public void Stacking_StackType_LevelConfigByStack()
        {
            var attr = MakeAttr(damage: 1f);
            var sm = new SurvivorActorSM();
            var t = new TestDamageable(100);
            var mgr = new SurvivorBuffMgr(attr, sm, t, t.Ledger);

            var def = new BuffDef { BuffId = 401, ReplaceType = BuffReplaceType.Stack };
            def.Levels.Add(Level(dur: 10f, attr: new[] { new ActorAttrImpact(ActorAttrDataType.Damage, ActorAttrAddType.Absolute, 1f) }, stackNum: 1));
            def.Levels.Add(Level(dur: 10f, attr: new[] { new ActorAttrImpact(ActorAttrDataType.Damage, ActorAttrAddType.Absolute, 2f) }, stackNum: 2));

            var b1 = mgr.AddBuff(def, "c", SkillImpactSource.None);
            Assert.AreEqual(1, b1.Stack);
            Assert.AreEqual(2f, attr.FinalDamage, 1e-4f, "level 1: +1");

            mgr.Tick(4f); // hao duration
            var b2 = mgr.AddBuff(def, "c", SkillImpactSource.None);
            Assert.AreEqual(2, b2.Stack, "stack lên 2");
            Assert.AreEqual(3f, attr.FinalDamage, 1e-4f, "level 2: +2");
            Assert.AreEqual(10f, b2.Remaining, 1e-4f, "duration refresh full");

            var b3 = mgr.AddBuff(def, "c", SkillImpactSource.None);
            Assert.AreEqual(2, b3.Stack, "cap 2 — không stack nữa");
            Assert.AreEqual(3f, attr.FinalDamage, 1e-4f, "giữ level 2");
        }

        [Test]
        public void Stacking_RefreshType_DurationOnly_NoStack()
        {
            var attr = MakeAttr(damage: 1f);
            var sm = new SurvivorActorSM();
            var t = new TestDamageable(100);
            var mgr = new SurvivorBuffMgr(attr, sm, t, t.Ledger);

            var def = MakeDef(402, Level(dur: 10f, attr: new[] { new ActorAttrImpact(ActorAttrDataType.Damage, ActorAttrAddType.Absolute, 1f) }));
            mgr.AddBuff(def, "c", SkillImpactSource.None);
            mgr.Tick(6f);
            Assert.AreEqual(4f, mgr.GetBuff(402).Remaining, 1e-4f, "còn 4s");

            mgr.AddBuff(def, "c", SkillImpactSource.None); // re-apply

            Assert.AreEqual(1, mgr.GetBuff(402).Stack, "stack giữ nguyên (Refresh)");
            Assert.AreEqual(10f, mgr.GetBuff(402).Remaining, 1e-4f, "duration refresh");
            Assert.AreEqual(2f, attr.FinalDamage, 1e-4f, "attr giữ +1");
        }

        [Test]
        public void BuffDef_FindAttr_LevelByStack()
        {
            var def = new BuffDef { BuffId = 403 };
            def.Levels.Add(Level(stackNum: 1, attr: new[] { new ActorAttrImpact(ActorAttrDataType.Damage, ActorAttrAddType.Absolute, 1f) }));
            def.Levels.Add(Level(stackNum: 3, attr: new[] { new ActorAttrImpact(ActorAttrDataType.Damage, ActorAttrAddType.Absolute, 3f) }));

            Assert.AreEqual(1, def.FindAttr(1).StackNum);
            Assert.AreEqual(1, def.FindAttr(2).StackNum, "stack 2 chưa đủ threshold level 3 → level 1");
            Assert.AreEqual(3, def.FindAttr(3).StackNum);
            Assert.AreEqual(3, def.FindAttr(9).StackNum, "vượt cap → level cuối");
        }

        // ================= attribution → kill credit =================

        [Test]
        public void Attribution_KillCredit_DotSourceGetsXp()
        {
            var attr = MakeAttr(damage: 10f);
            var sm = new SurvivorActorSM();
            var t = new TestDamageable(10);
            var mgr = new SurvivorBuffMgr(attr, sm, t, t.Ledger);

            // bullet góp 4 từ skill 1
            var bullet = new SkillImpactSource(1, 0);
            t.Ledger.SumSkillDamage(bullet, "casterA", 4);

            // DOT skill 2/buff 205 gây 10 → kill (hp 10)
            var def = MakeDef(205, Level(dur: 10f, dot: DotDmg(1f), tick: new BuffDotTickConfig(1f)));
            mgr.AddBuff(def, caster: "casterB", source: new SkillImpactSource(2, 205), casterAttr: attr);

            mgr.Tick(1f);
            Assert.AreEqual(0, t.Hp, "dot hạ target"); // dead state do orchestrator quyết
            Assert.IsTrue(t.LastInfo.IsDead, "DamageInfo.IsDead");

            Assert.IsTrue(t.Ledger.TryGetTopSource(out var top, out var topCaster, out var total), "có top source");
            Assert.AreEqual(2, top.SkillId, "skill gây kill credit");
            Assert.AreEqual(205, top.BuffId, "buff id");
            Assert.AreEqual("casterB", topCaster, "caster đúng");
            Assert.AreEqual(10, total, "tổng damage của source thắng");
            Assert.AreEqual(14, t.Ledger.TotalDamage, "cả 2 source gộp");
        }

        [Test]
        public void Attribution_MuteDamage_GatesDotTick()
        {
            var attr = MakeAttr(damage: 10f);
            var sm = new SurvivorActorSM();
            var t = new TestDamageable(100);
            var mgr = new SurvivorBuffMgr(attr, sm, t, t.Ledger);

            var def = MakeDef(206, Level(dur: 10f, dot: DotDmg(1f), tick: new BuffDotTickConfig(1f)));
            mgr.AddBuff(def, "c", new SkillImpactSource(3, 206), attr);

            mgr.MuteDamage = true; // parity m_muteDamage — gate cả DOT (S13)
            mgr.Tick(2f);
            Assert.AreEqual(100, t.Hp, "mute chặn dot tick");
            Assert.AreEqual(0, t.Ledger.TotalDamage);

            mgr.MuteDamage = false;
            mgr.Tick(1f);
            Assert.AreEqual(90, t.Hp, "mở mute → tick lại");
        }

        [Test]
        public void Buff_ClearAll_RemovesEverything()
        {
            var attr = MakeAttr(damage: 1f);
            var sm = new SurvivorActorSM();
            var t = new TestDamageable(100);
            var mgr = new SurvivorBuffMgr(attr, sm, t, t.Ledger);

            mgr.AddBuff(MakeDef(501, Level(dur: 10f, attr: new[] { new ActorAttrImpact(ActorAttrDataType.Damage, ActorAttrAddType.Absolute, 3f) })), "c", SkillImpactSource.None);
            mgr.AddBuff(MakeDef(502, Level(states: BuffStateID.NoMove, dur: 10f)), "c", SkillImpactSource.None);

            mgr.ClearAllBuff();

            Assert.AreEqual(0, mgr.BuffCount);
            Assert.AreEqual(1f, attr.FinalDamage, 1e-4f, "attr revert");
            Assert.IsTrue(mgr.CanMove, "state cleared");
        }
    }
}
