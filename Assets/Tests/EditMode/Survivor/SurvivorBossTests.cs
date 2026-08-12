// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Survivor — SurvivorBossTests (ticket 31 self-check)
// Pure logic, KHÔNG scene/PlayMode (spec Testing Decisions):
//  - Phase table lookup: window [BossDamageMin, BossDamageMax] inclusive,
//    Max = 0 open-ended; gap giữa window → giữ phase trước (parity
//    GetJiangHuBossPhaseConfig(lossHp) — damage-window keyed, KHÔNG timer).
//  - BossPhaseMachine: phase switch chỉ theo loss damage tích lũy; heal
//    không regress; boundary chính xác min/max.
//  - Skill pool subset: lọc theo ids, giữ thứ tự, id thiếu → fail-closed bỏ.
//  - DefaultPhases sanity: 3 phase, phase cuối open-ended.
//  - Booty roll: SurvivorCollectItemMgr.RollActorDrop theo pool BootyId,
//    deterministic seed, Xp-only (Gold/Heal = supply ticket 13/33).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VLTK.Survivor;

namespace VLTK.Tests.Survivor
{
    public class SurvivorBossTests
    {
        // ------------------------------------------------------------------
        // helpers
        // ------------------------------------------------------------------

        private static SkillDef MakeDef(int id, int form = 7)
        {
            var def = SkillDef.FromRow(new SkillRow { Id = id, Form = form });
            def.MaxLevel = 10;
            return def;
        }

        // ------------------------------------------------------------------
        // phase window lookup: boundary min/max (parity JiangHuBossPhaseConfig)
        // ------------------------------------------------------------------

        [Test]
        public void PhaseWindow_InclusiveMinMax()
        {
            var phases = SurvivorBoss.DefaultPhases(); // [0,10] [11,20] [21,∞]
            Assert.AreEqual(0, BossPhaseTable.CurrentPhaseIndex(phases, 0f), "loss 0 → phase 1");
            Assert.AreEqual(0, BossPhaseTable.CurrentPhaseIndex(phases, 10f), "max inclusive");
            Assert.AreEqual(1, BossPhaseTable.CurrentPhaseIndex(phases, 11f), "min inclusive");
            Assert.AreEqual(1, BossPhaseTable.CurrentPhaseIndex(phases, 20f), "max inclusive");
            Assert.AreEqual(2, BossPhaseTable.CurrentPhaseIndex(phases, 21f), "min inclusive");
            Assert.AreEqual(2, BossPhaseTable.CurrentPhaseIndex(phases, 1000f), "Max=0 → open-ended");
        }

        [Test]
        public void PhaseWindow_GapKeepsPrevious()
        {
            var phases = new List<BossPhaseDef>
            {
                new BossPhaseDef { Phase = 1, BossDamageMin = 0f, BossDamageMax = 5f, AiMode = BossAiMode.Chase, BootyId = 1001 },
                new BossPhaseDef { Phase = 2, BossDamageMin = 10f, BossDamageMax = 15f, AiMode = BossAiMode.Cast, BootyId = 1002 },
            };
            Assert.AreEqual(0, BossPhaseTable.CurrentPhaseIndex(phases, 7f), "khoảng trống [6,9] → giữ phase 1");
            Assert.AreEqual(0, BossPhaseTable.CurrentPhaseIndex(phases, 5f), "max inclusive phase 1");
            Assert.AreEqual(1, BossPhaseTable.CurrentPhaseIndex(phases, 10f));
        }

        [Test]
        public void PhaseWindow_EmptyTable_NoPhase()
        {
            Assert.AreEqual(-1, BossPhaseTable.CurrentPhaseIndex(new List<BossPhaseDef>(), 5f));
            Assert.AreEqual(-1, BossPhaseTable.CurrentPhaseIndex(null, 5f));
        }

        // ------------------------------------------------------------------
        // phase switch: keyed damage tích lũy, KHÔNG timer
        // ------------------------------------------------------------------

        [Test]
        public void PhaseSwitch_DamageWindow_NotTime()
        {
            var machine = new BossPhaseMachine(SurvivorBoss.DefaultPhases());
            Assert.AreEqual(-1, machine.PhaseIndex, "chưa báo HP → chưa có phase");

            // boss 30 HP: mất 5 → phase 1; mất thêm 7 (tổng 12) → phase 2; mất thêm 10 (tổng 22) → phase 3
            Assert.IsTrue(machine.ReportHp(30f, 25f), "loss 5 → phase 1");
            Assert.AreEqual(0, machine.PhaseIndex);
            Assert.IsFalse(machine.ReportHp(30f, 24f), "cùng window → không switch (không timer)");
            Assert.AreEqual(0, machine.PhaseIndex);

            Assert.IsTrue(machine.ReportHp(30f, 18f), "loss 12 → phase 2");
            Assert.AreEqual(1, machine.PhaseIndex);
            Assert.AreEqual(1002, machine.Current.BootyId, "BootyID theo phase");

            Assert.IsTrue(machine.ReportHp(30f, 8f), "loss 22 → phase 3");
            Assert.AreEqual(2, machine.PhaseIndex);
            Assert.AreEqual(BossAiMode.Chase, machine.Current.AiMode);
        }

        [Test]
        public void PhaseSwitch_ExactBoundary_Fires()
        {
            var machine = new BossPhaseMachine(SurvivorBoss.DefaultPhases());
            Assert.IsTrue(machine.ReportHp(30f, 20f), "loss 10 = max phase 1 → phase 1");
            Assert.AreEqual(0, machine.PhaseIndex);
            Assert.IsTrue(machine.ReportHp(30f, 19f), "loss 11 = min phase 2 → phase 2");
            Assert.AreEqual(1, machine.PhaseIndex);
        }

        [Test]
        public void PhaseSwitch_Monotonic_NoRegressionOnHeal()
        {
            var machine = new BossPhaseMachine(SurvivorBoss.DefaultPhases());
            machine.ReportHp(30f, 15f); // loss 15 → phase 2
            Assert.AreEqual(1, machine.PhaseIndex);
            Assert.IsFalse(machine.ReportHp(30f, 20f), "HP tăng (heal) → loss giảm → không regress phase");
            Assert.AreEqual(1, machine.PhaseIndex);
            Assert.IsFalse(machine.ReportHp(30f, 15f), "HP không đổi → không switch");
        }

        // ------------------------------------------------------------------
        // skill pool subset (BossNpc pool ticket 26 → phase SkillIds)
        // ------------------------------------------------------------------

        [Test]
        public void SkillSubset_OnlyRequestedIds_OrderPreserved()
        {
            var catalog = new List<SkillDef> { MakeDef(11), MakeDef(12), MakeDef(13) };
            var sub = BossPhaseTable.Subset(catalog, new[] { 13, 11 });
            Assert.AreEqual(2, sub.Count);
            Assert.AreEqual(13, sub[0].Id, "giữ thứ tự ids");
            Assert.AreEqual(11, sub[1].Id);
        }

        [Test]
        public void SkillSubset_MissingId_FailClosedSkip()
        {
            var catalog = new List<SkillDef> { MakeDef(11) };
            var sub = BossPhaseTable.Subset(catalog, new[] { 11, 999 });
            Assert.AreEqual(1, sub.Count, "id không có trong catalog → bỏ, không bịa");
            Assert.AreEqual(11, sub[0].Id);
        }

        [Test]
        public void SkillSubset_NullCatalog_Empty()
        {
            Assert.AreEqual(0, BossPhaseTable.Subset(null, new[] { 11 }).Count);
            Assert.AreEqual(0, BossPhaseTable.Subset(new List<SkillDef>(), null).Count);
        }

        // ------------------------------------------------------------------
        // default phase table sanity
        // ------------------------------------------------------------------

        [Test]
        public void DefaultPhases_ThreePhase_Sane()
        {
            var phases = SurvivorBoss.DefaultPhases();
            Assert.AreEqual(3, phases.Count);
            Assert.AreEqual(0f, phases[0].BossDamageMin, "phase 1 bắt đầu từ loss 0");
            Assert.AreEqual(BossAiMode.Chase, phases[0].AiMode);
            Assert.AreEqual(BossAiMode.Cast, phases[1].AiMode, "phase giữa → cast");
            Assert.AreEqual(0f, phases[2].BossDamageMax, "phase cuối open-ended");
            Assert.Greater(phases[2].BossDamageMin, phases[1].BossDamageMax, "window liên tiếp không chồng");
            Assert.Greater(phases[1].BossDamageMin, phases[0].BossDamageMax);
        }

        // ------------------------------------------------------------------
        // booty roll: DropTable pool theo BootyId (RollActorDrop, deterministic)
        // ------------------------------------------------------------------

        private static DropTableSO MakeBootyTable()
        {
            var t = ScriptableObject.CreateInstance<DropTableSO>();
            t.Entries = new List<DropEntry>
            {
                new DropEntry { PoolID = 1002, ItemID = 1, OutputType = DropOutputType.Xp, Param1 = 5, DropRate = 1f },
                new DropEntry { PoolID = 1002, ItemID = 2, OutputType = DropOutputType.Xp, Param1 = 2, DropRate = 1f, CountMin = 2, CountMax = 2 },
                new DropEntry { PoolID = 1002, ItemID = 3, OutputType = DropOutputType.Heal, Param1 = 1, DropRate = 0f },
                new DropEntry { PoolID = 1003, ItemID = 4, OutputType = DropOutputType.Xp, Param1 = 9, DropRate = 1f },
            };
            return t;
        }

        [Test]
        public void BootyRoll_ByPoolId_Deterministic()
        {
            var mgr = new SurvivorCollectItemMgr(MakeBootyTable());
            var a = mgr.RollActorDrop(1002, new System.Random(7));
            var b = mgr.RollActorDrop(1002, new System.Random(7));
            Assert.AreEqual(a.Count, b.Count, "cùng seed → cùng kết quả");

            int xp = 0;
            foreach (var r in a)
            {
                Assert.AreEqual(DropOutputType.Xp, r.OutputType, "rate 0 (Heal) không rơi");
                xp += r.Amount;
            }
            Assert.AreEqual(5 + 2 * 2, xp, "xp entry chắc chắn: Param1 5 + 2×2 (CountMin=CountMax=2)");
        }

        [Test]
        public void BootyRoll_OtherPool_Isolated()
        {
            var mgr = new SurvivorCollectItemMgr(MakeBootyTable());
            var c = mgr.RollActorDrop(1003, new System.Random(7));
            Assert.AreEqual(1, c.Count, "pool 1003 riêng — không lẫn pool 1002");
            Assert.AreEqual(DropOutputType.Xp, c[0].OutputType);
            Assert.AreEqual(9, c[0].Amount);
        }
    }
}
