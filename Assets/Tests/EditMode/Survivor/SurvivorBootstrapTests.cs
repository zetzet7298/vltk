// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Survivor — SurvivorBootstrapTests
// Phase 2 (PORT_CAIBANG §3 Gap C) run-start bootstrap self-check:
//  - TriggerBootstrap tạo event 2 card ép sẵn (128/125), RerollsLeft=0, pause
//    CardChoiceScope (timescale=0 tới click).
//  - Pick → Learn vào roster + đóng event + release pause (game chạy tiếp).
//  - Close() TỪ CHỐI khi IsBootstrap (bắt pick — không skip được card đầu).
//  - Đang waiting → TriggerBootstrap false (không chồng modal).
//  - skillId lạ → false fail-closed (không bịa card).
//  - Timeout WaitingLearnWindow → re-trigger CÙNG event (identity giữ, modal
//    không đóng nhầm), KHÔNG auto-close/auto-learn.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using NUnit.Framework;
using VLTK.Survivor;

namespace VLTK.Tests.Survivor
{
    public class SurvivorBootstrapTests
    {
        // ------------------------------------------------------------------
        // helpers
        // ------------------------------------------------------------------

        private static SkillDef Def(int id, int maxLevel = 20)
        {
            return SkillDef.FromRow(new SkillRow { Id = id, Name = "S" + id, Form = 7, MaxLevel = maxLevel });
        }

        /// <summary>Pool 4 skill Cái Bang (Phase 1 scope) — 128/125 tier 1, 1073/1074 depend.</summary>
        private static SkillChoicePool CaiBangPool()
        {
            var pool = new SkillChoicePool();
            pool.Add(Def(128));
            pool.Add(Def(125));
            pool.Add(new SurvivorSkillLibraryConfig(Def(1073),
                new List<SurvivorSkillDependEntry> { new SurvivorSkillDependEntry(128, 5) }));
            pool.Add(new SurvivorSkillLibraryConfig(Def(1074),
                new List<SurvivorSkillDependEntry> { new SurvivorSkillDependEntry(125, 5) }));
            return pool;
        }

        private static SkillChoiceService MakeService(SkillCastRuntime roster, SkillChoicePool pool,
            List<bool> pauseLog = null)
        {
            var pause = new SurvivorPause(p => { if (pauseLog != null) pauseLog.Add(p); });
            return new SkillChoiceService(roster, pool, new System.Random(42), null, pause);
        }

        // ------------------------------------------------------------------
        // trigger: event 2 card ép sẵn, không reroll, pause
        // ------------------------------------------------------------------

        [Test]
        public void TriggerBootstrap_CreatesEvent_2Cards_NoReroll_Paused()
        {
            var svc = MakeService(new SkillCastRuntime(), CaiBangPool());
            svc.Tick(0);

            Assert.IsTrue(svc.TriggerBootstrap(1u, new[] { 128, 125 }), "rảnh → trigger ngay");

            var ev = svc.Current(1u);
            Assert.IsNotNull(ev);
            Assert.IsTrue(ev.IsBootstrap, "flag bootstrap set");
            Assert.AreEqual(SkillChoiceMode.LevelUp, ev.Mode);
            Assert.AreEqual(2, ev.Cards.Length, "bootstrap 2 card, KHÔNG 3 (parity draw khác)");
            Assert.AreEqual(128, ev.Cards[0].Def.Id, "card ép sẵn: 128");
            Assert.AreEqual(125, ev.Cards[1].Def.Id, "card ép sẵn: 125");
            Assert.AreEqual(0, ev.RerollsLeft, "bootstrap KHÔNG reroll");
            Assert.IsTrue(svc.IsWaiting(1u));
            Assert.AreEqual(1, svc.Pause.Count, "modal mở → pause acquire (timescale=0)");
        }

        [Test]
        public void Bootstrap_Pick_Learns_Closes_ReleasesPause()
        {
            var rt = new SkillCastRuntime();
            var pauseLog = new List<bool>();
            var svc = MakeService(rt, CaiBangPool(), pauseLog);
            svc.Tick(0);
            svc.TriggerBootstrap(1u, new[] { 128, 125 });

            Assert.IsTrue(svc.Select(1u, svc.Current(1u).Cards[0]), "pick card 128");
            Assert.AreEqual(1, rt.GetLevel(128), "pick → Learn vào roster");
            Assert.AreEqual(0, rt.GetLevel(125), "skill kia chưa học");
            Assert.IsNull(svc.Current(1u), "pick xong → event đóng");
            Assert.AreEqual(0, svc.Pause.Count, "pause release → timescale về 1, game chạy");
            Assert.IsFalse(svc.IsWaiting(1u));
            CollectionAssert.AreEqual(new[] { true, false }, pauseLog, "acquire tới release");
        }

        [Test]
        public void Bootstrap_Pick_OtherCard_AlsoCloses()
        {
            var rt = new SkillCastRuntime();
            var svc = MakeService(rt, CaiBangPool());
            svc.Tick(0);
            svc.TriggerBootstrap(1u, new[] { 128, 125 });

            Assert.IsTrue(svc.Select(1u, svc.Current(1u).Cards[1]), "pick card 125");
            Assert.AreEqual(1, rt.GetLevel(125));
            Assert.IsNull(svc.Current(1u), "1 trong 2 → đóng");
            Assert.AreEqual(0, svc.Pause.Count);
        }

        // ------------------------------------------------------------------
        // bắt pick enforcement
        // ------------------------------------------------------------------

        [Test]
        public void Bootstrap_Close_Refused_WhileEventOpen()
        {
            var svc = MakeService(new SkillCastRuntime(), CaiBangPool());
            svc.Tick(0);
            svc.TriggerBootstrap(1u, new[] { 128, 125 });

            svc.Close(1u); // cố skip bootstrap
            Assert.IsNotNull(svc.Current(1u), "Close TỪ CHỐI khi IsBootstrap — bắt pick");
            Assert.AreEqual(1, svc.Pause.Count, "pause giữ (modal còn mở)");
            Assert.IsTrue(svc.IsWaiting(1u));
        }

        [Test]
        public void Bootstrap_Reroll_Refused_NoCardsChanged()
        {
            var svc = MakeService(new SkillCastRuntime(), CaiBangPool());
            svc.Tick(0);
            svc.TriggerBootstrap(1u, new[] { 128, 125 });
            var before = svc.Current(1u).Cards;

            Assert.IsFalse(svc.RerollLevelUp(1u), "RerollsLeft=0 → từ chối");
            Assert.AreEqual(2, svc.Current(1u).Cards.Length, "card không đổi");
            CollectionAssert.AreEqual(before, svc.Current(1u).Cards, "cùng cards");
        }

        // ------------------------------------------------------------------
        // fail-closed
        // ------------------------------------------------------------------

        [Test]
        public void TriggerBootstrap_WhileWaiting_ReturnsFalse()
        {
            var svc = MakeService(new SkillCastRuntime(), CaiBangPool());
            svc.Tick(0);
            Assert.IsTrue(svc.TriggerBootstrap(1u, new[] { 128, 125 }));

            Assert.IsFalse(svc.TriggerBootstrap(1u, new[] { 125, 128 }), "đang chọn → từ chối, không chồng modal");
            Assert.IsTrue(svc.Current(1u).IsBootstrap, "event đầu giữ nguyên");
            Assert.AreEqual(1, svc.Pause.Count, "không acquire thêm pause");
        }

        [Test]
        public void TriggerBootstrap_UnknownSkillId_FailsClosed()
        {
            var svc = MakeService(new SkillCastRuntime(), CaiBangPool());
            svc.Tick(0);

            Assert.IsFalse(svc.TriggerBootstrap(1u, new[] { 128, 999 }), "skill lạ → false");
            Assert.IsNull(svc.Current(1u), "KHÔNG tạo event dở dang");
            Assert.AreEqual(0, svc.Pause.Count, "KHÔNG acquire pause");
            Assert.AreEqual(0, new SkillCastRuntime().Roster.Count, "không learn gì");
        }

        [Test]
        public void TriggerBootstrap_NullOrEmpty_FailsClosed()
        {
            var svc = MakeService(new SkillCastRuntime(), CaiBangPool());
            svc.Tick(0);

            Assert.IsFalse(svc.TriggerBootstrap(1u, null), "null → false");
            Assert.IsFalse(svc.TriggerBootstrap(1u, new int[0]), "rỗng → false");
            Assert.IsNull(svc.Current(1u));
            Assert.AreEqual(0, svc.Pause.Count);
        }

        // ------------------------------------------------------------------
        // timeout: re-trigger cùng event, KHÔNG auto-close/auto-learn
        // ------------------------------------------------------------------

        [Test]
        public void Bootstrap_Timeout_ReTriggers_SameEvent_NoClose_NoLearn()
        {
            var rt = new SkillCastRuntime();
            var svc = MakeService(rt, CaiBangPool());
            svc.Tick(0);
            svc.TriggerBootstrap(1u, new[] { 128, 125 });
            var ev1 = svc.Current(1u);

            svc.Tick(31f); // quá WaitingLearnWindow 30s

            var ev2 = svc.Current(1u);
            Assert.AreSame(ev1, ev2, "re-trigger CÙNG event object (identity giữ — Overlay poll không đóng nhầm)");
            Assert.IsTrue(svc.IsWaiting(1u), "window reset — modal vẫn mở chờ pick");
            Assert.AreEqual(1, svc.Pause.Count, "KHÔNG auto-close → pause giữ");
            Assert.AreEqual(0, rt.GetLevel(128), "fail-closed: KHÔNG auto-learn");
            Assert.AreEqual(0, rt.GetLevel(125));

            svc.Tick(62f); // timeout lần 2 → re-trigger tiếp
            Assert.AreSame(ev1, svc.Current(1u), "re-trigger vô hạn tới khi pick");
            Assert.IsTrue(svc.IsWaiting(1u));

            // pick vẫn đóng được sau nhiều lần timeout
            Assert.IsTrue(svc.Select(1u, ev1.Cards[0]));
            Assert.IsNull(svc.Current(1u));
            Assert.AreEqual(0, svc.Pause.Count);
        }

        // ------------------------------------------------------------------
        // levelup thường SAU bootstrap vẫn parity (3 card, reroll)
        // ------------------------------------------------------------------

        [Test]
        public void NormalLevelUp_AfterBootstrap_Still3Cards_WithReroll()
        {
            var rt = new SkillCastRuntime();
            var svc = MakeService(rt, CaiBangPool());
            svc.Tick(0);
            svc.TriggerBootstrap(1u, new[] { 128, 125 });
            svc.Select(1u, svc.Current(1u).Cards[0]); // pick 128 → bootstrap xong

            Assert.IsTrue(svc.Request(1u, SkillChoiceMode.LevelUp), "levelup thường sau bootstrap");
            var ev = svc.Current(1u);
            Assert.IsFalse(ev.IsBootstrap, "KHÔNG phải bootstrap");
            Assert.AreEqual(2, ev.Cards.Length, "128 lv1 → 1073/1074 depend chưa thỏa → 2 card (depend chặn đúng)");
            Assert.AreEqual(2, ev.RerollsLeft, "reroll vẫn có (parity giữ nguyên)");
            Assert.IsTrue(svc.RerollLevelUp(1u), "reroll levelup thường hoạt động");
            svc.Select(1u, svc.Current(1u).Cards[0]);
            Assert.AreEqual(0, svc.Pause.Count);

            // depend thỏa (128 ≥ Lv5) → 1073 mở → draw 3 card parity
            int base128 = rt.GetLevel(128); // sau bootstrap pick + select đầu (có thể 128 hoặc 125)
            for (int i = 0; i < 4; i++) // pick 128 thêm 4 lần
            {
                svc.Request(1u, SkillChoiceMode.LevelUp);
                var loopEv = svc.Current(1u);
                SkillChoiceCard pick = loopEv.Cards[0];
                for (int j = 0; j < loopEv.Cards.Length; j++)
                    if (loopEv.Cards[j].Def.Id == 128) { pick = loopEv.Cards[j]; break; }
                svc.Select(1u, pick);
            }
            Assert.AreEqual(base128 + 4, rt.GetLevel(128), "pick 128 4 lần nữa");
            Assert.GreaterOrEqual(rt.GetLevel(128), 5, "128 phải đủ Lv5 mở depend 1073");
            Assert.Less(rt.GetLevel(125), 5, "125 chưa đủ Lv5 → 1074 vẫn chặn");
            svc.Request(1u, SkillChoiceMode.LevelUp);
            Assert.AreEqual(3, svc.Current(1u).Cards.Length, "128 Lv5 → 1073 mở → 3 card sẵn (1074 vẫn chặn)");
            svc.Close(1u);
        }
    }
}
