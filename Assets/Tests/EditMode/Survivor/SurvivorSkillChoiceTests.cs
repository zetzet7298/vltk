// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Survivor — SurvivorSkillChoiceTests
// Ticket 29 self-check (pure logic, không scene — spec Testing Decisions):
//  - Queue FSM: trigger-ngay / enqueue-khi-waiting / FIFO dequeue sau Close
//  - Ref-count pause card scope: Acquire/Release, timescale {0,1} qua delegate
//  - Reroll 2 cmd: levelup giới hạn lượt (FrameCmdRerandomSkill) + shop giá
//    cố định trừ vàng (FrameCmdReSelectRandomSkill)
//  - Pool weight: weight walk deterministic (FixedRng) + MaxLevel loại khỏi pool
//  - Box learnNum: chọn nhiều lần, đủ lượt → close + pump (SelectBoxSkill)
//  - Pick → SkillCastRuntime.Learn (roster level tăng, cap MaxLevel)
//  - Timeout waiting window → auto-close, không auto-learn (fail-closed)
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using NUnit.Framework;
using VLTK.Survivor;

namespace VLTK.Tests.Survivor
{
    public class SurvivorSkillChoiceTests
    {
        // ------------------------------------------------------------------
        // helpers
        // ------------------------------------------------------------------

        private static SkillDef Def(int id, int maxLevel = 5)
        {
            return SkillDef.FromRow(new SkillRow { Id = id, Name = "S" + id, Form = 7, MaxLevel = maxLevel });
        }

        private static SkillChoicePool PoolOf(params int[] ids)
        {
            var pool = new SkillChoicePool();
            foreach (int id in ids) pool.Add(Def(id));
            return pool;
        }

        /// <summary>RNG kiểm soát: Next(max) trả giá trị cố định % max — deterministic tuyệt đối.</summary>
        private sealed class FixedRng : System.Random
        {
            private readonly int[] _vals;
            private int _i;
            public FixedRng(params int[] vals) { _vals = vals; }
            public override int Next(int maxValue)
            {
                return _vals[_i++ % _vals.Length] % maxValue;
            }
        }

        private sealed class GoldLog
        {
            public readonly Dictionary<ulong, int> Balance = new Dictionary<ulong, int>();
            public readonly List<GoldSpend> Spent = new List<GoldSpend>();

            public GoldLog(int startGold) { Balance[1] = startGold; }

            public bool TrySpend(ulong roleId, int amount)
            {
                Spent.Add(new GoldSpend(roleId, amount));
                if (!Balance.TryGetValue(roleId, out int b) || b < amount) return false;
                Balance[roleId] = b - amount;
                return true;
            }
        }

        private sealed class GoldSpend
        {
            public readonly ulong RoleId;
            public readonly int Amount;
            public GoldSpend(ulong roleId, int amount) { RoleId = roleId; Amount = amount; }
        }

        private static SkillChoiceService MakeService(SkillCastRuntime roster, System.Random rng,
            SkillChoicePool pool = null, GoldLog gold = null, List<bool> pauseLog = null)
        {
            var pause = new CardChoicePause(p => { if (pauseLog != null) pauseLog.Add(p); });
            System.Func<ulong, int, bool> spend = gold != null ? gold.TrySpend : (System.Func<ulong, int, bool>)null;
            return new SkillChoiceService(roster, pool ?? PoolOf(1, 2, 3, 4, 5), rng, spend, pause);
        }

        // ------------------------------------------------------------------
        // queue FSM: trigger-ngay / enqueue / FIFO pump
        // ------------------------------------------------------------------

        [Test]
        public void Request_WhenFree_TriggersNow_3Cards_Paused()
        {
            var svc = MakeService(new SkillCastRuntime(), new System.Random(42));
            svc.Tick(0);
            Assert.IsTrue(svc.Request(1, SkillChoiceMode.LevelUp), "rảnh → trigger ngay");

            var ev = svc.Current(1);
            Assert.IsNotNull(ev);
            Assert.AreEqual(SkillChoiceMode.LevelUp, ev.Mode);
            Assert.AreEqual(3, ev.Cards.Length, "levelup 3 card");
            Assert.IsTrue(svc.IsWaiting(1));
            Assert.AreEqual(1, svc.Pause.Count, "modal mở → pause acquire");
        }

        [Test]
        public void Request_WhenWaiting_Enqueues_FIFO_PumpAfterClose()
        {
            var svc = MakeService(new SkillCastRuntime(), new System.Random(42));
            svc.Tick(0);
            Assert.IsTrue(svc.Request(1, SkillChoiceMode.LevelUp), "event 1 trigger");

            Assert.IsFalse(svc.Request(1, SkillChoiceMode.Box, 2), "đang chọn → enqueue, không trigger");
            Assert.IsFalse(svc.Request(1, SkillChoiceMode.Shop), "enqueue thứ 2");
            Assert.AreEqual(1, svc.Pause.Count, "enqueue không acquire thêm pause");

            // pick event 1 → close → pump: box kế tiếp (FIFO)
            Assert.IsTrue(svc.Select(1, svc.Current(1).Cards[0]));
            Assert.AreEqual(SkillChoiceMode.Box, svc.Current(1).Mode, "FIFO: box trước shop");
            Assert.AreEqual(2, svc.Current(1).LearnCount, "box learnNum=2");
            Assert.AreEqual(1, svc.Pause.Count, "box modal vẫn pause");

            svc.Close(1); // bỏ qua box → pump shop
            Assert.AreEqual(SkillChoiceMode.Shop, svc.Current(1).Mode, "FIFO: shop sau box");

            svc.Close(1);
            Assert.IsNull(svc.Current(1), "queue cạn → hết event");
            Assert.AreEqual(0, svc.Pause.Count, "đóng hết → pause release về 0");
        }

        [Test]
        public void Tick_Timeout_AutoCloses_NoAutoLearn_PumpsNext()
        {
            var rt = new SkillCastRuntime();
            var svc = MakeService(rt, new System.Random(2));
            svc.Tick(0);
            svc.Request(1, SkillChoiceMode.LevelUp);
            var defId = svc.Current(1).Cards[0].Def.Id;
            svc.Request(1, SkillChoiceMode.Box, 1); // enqueue trong lúc waiting

            svc.Tick(31f); // quá WaitingLearnWindow 30s

            Assert.AreEqual(0, rt.GetLevel(defId), "fail-closed: KHÔNG auto-learn");
            Assert.AreEqual(SkillChoiceMode.Box, svc.Current(1).Mode, "auto-close → pump queue kế tiếp");
            Assert.AreEqual(1, svc.Pause.Count, "pause re-acquire cho event kế tiếp");
            Assert.IsTrue(svc.IsWaiting(1), "event kế tiếp mở modal mới");
        }

        // ------------------------------------------------------------------
        // pick → Learn vào roster (ticket 27)
        // ------------------------------------------------------------------

        [Test]
        public void Select_LevelUp_LearnsIntoRoster_AndCloses()
        {
            var rt = new SkillCastRuntime();
            var svc = MakeService(rt, new System.Random(7));
            svc.Tick(0);
            svc.Request(1, SkillChoiceMode.LevelUp);

            var card = svc.Current(1).Cards[0];
            Assert.IsTrue(svc.Select(1, card));
            Assert.AreEqual(1, rt.GetLevel(card.Def.Id), "pick → Learn vào roster");
            Assert.IsNull(svc.Current(1), "levelup 1 lần chọn → đóng");
            Assert.AreEqual(0, svc.Pause.Count);
        }

        [Test]
        public void Select_RepeatPick_StackLevel_CapMaxLevel_PoolEmpty()
        {
            var rt = new SkillCastRuntime();
            var pool = new SkillChoicePool();
            pool.Add(Def(1, maxLevel: 3)); // pool 1 skill, cap 3
            var svc = MakeService(rt, new System.Random(3), pool);
            svc.Tick(0);

            for (int i = 1; i <= 3; i++)
            {
                Assert.IsTrue(svc.Request(1, SkillChoiceMode.LevelUp), $"request lần {i}");
                Assert.AreEqual(1, svc.Current(1).Cards.Length, "pool 1 skill → 1 card");
                svc.Select(1, svc.Current(1).Cards[0]);
                Assert.AreEqual(i, rt.GetLevel(1), $"level {i}");
            }
            Assert.AreEqual(3, rt.GetLevel(1), "cap MaxLevel");

            // đã max → loại khỏi pool → draw 0 card (fail-closed: modal trống, không crash)
            Assert.IsTrue(svc.Request(1, SkillChoiceMode.LevelUp));
            Assert.AreEqual(0, svc.Current(1).Cards.Length, "pool cạn → 0 card");
            svc.Close(1);
        }

        // ------------------------------------------------------------------
        // reroll: levelup (giới hạn lượt) + shop (giá cố định trừ vàng)
        // ------------------------------------------------------------------

        [Test]
        public void RerollLevelUp_Limited_DrawsNewCards_NoPauseChange()
        {
            var svc = MakeService(new SkillCastRuntime(), new System.Random(42));
            svc.Tick(0);
            svc.Request(1, SkillChoiceMode.LevelUp);
            Assert.AreEqual(2, svc.Current(1).RerollsLeft, "MaxLevelUpRerolls own = 2");

            Assert.IsTrue(svc.RerollLevelUp(1));
            Assert.AreEqual(1, svc.Current(1).RerollsLeft);
            Assert.AreEqual(3, svc.Current(1).Cards.Length, "draw lại 3 card");

            Assert.IsTrue(svc.RerollLevelUp(1));
            Assert.AreEqual(0, svc.Current(1).RerollsLeft);
            Assert.IsFalse(svc.RerollLevelUp(1), "hết lượt → từ chối");
            Assert.AreEqual(1, svc.Pause.Count, "reroll không đổi pause count");
        }

        [Test]
        public void ShopReroll_FixedPrice_SpendsGold_RefusedWhenBroke()
        {
            var gold = new GoldLog(startGold: 20);
            var svc = MakeService(new SkillCastRuntime(), new System.Random(9), gold: gold);
            svc.Tick(0);
            svc.Request(1, SkillChoiceMode.Shop);

            Assert.IsTrue(svc.ShopReroll(1));
            Assert.AreEqual(15, gold.Balance[1], "reroll giá cố định 5 (own)");
            Assert.AreEqual(1, gold.Spent.Count);
            Assert.AreEqual(5, gold.Spent[0].Amount);
            Assert.AreEqual(3, svc.Current(1).Cards.Length, "draw lại 3 card");

            gold.Balance[1] = 0;
            Assert.IsFalse(svc.ShopReroll(1), "không đủ vàng → từ chối");
            Assert.IsNotNull(svc.Current(1), "reroll fail không đóng modal");
        }

        [Test]
        public void Shop_Buy_SpendsGold_Learns_Closes()
        {
            var rt = new SkillCastRuntime();
            var gold = new GoldLog(startGold: 30);
            var svc = MakeService(rt, new System.Random(9), gold: gold);
            svc.Tick(0);
            svc.Request(1, SkillChoiceMode.Shop);

            var ev = svc.Current(1);
            Assert.AreEqual(3, ev.Cards.Length, "shop 3 card");
            Assert.IsTrue(ev.Cards[0].Price > 0, "shop card có giá (own 10)");
            Assert.AreEqual(SkillChoiceService.ShopCardPrice, ev.Cards[0].Price);

            Assert.IsTrue(svc.Select(1, ev.Cards[0]));
            Assert.AreEqual(20, gold.Balance[1], "trừ ShopCardPrice");
            Assert.AreEqual(1, rt.GetLevel(ev.Cards[0].Def.Id), "mua → Learn");
            Assert.IsNull(svc.Current(1), "mua xong → đóng");
            Assert.AreEqual(0, svc.Pause.Count);
        }

        [Test]
        public void Shop_Buy_InsufficientGold_Refused_NoLearn_ModalStays()
        {
            var rt = new SkillCastRuntime();
            var gold = new GoldLog(startGold: 5);
            var svc = MakeService(rt, new System.Random(9), gold: gold);
            svc.Tick(0);
            svc.Request(1, SkillChoiceMode.Shop);

            var card = svc.Current(1).Cards[0];
            Assert.IsFalse(svc.Select(1, card), "không đủ vàng → từ chối");
            Assert.AreEqual(0, rt.GetLevel(card.Def.Id), "không học");
            Assert.AreEqual(5, gold.Balance[1], "không trừ vàng");
            Assert.IsNotNull(svc.Current(1), "modal vẫn mở");
            Assert.AreEqual(1, svc.Pause.Count, "vẫn pause");
        }

        [Test]
        public void Select_NoEvent_Or_NoGoldProvider_FailsClosed()
        {
            var rt = new SkillCastRuntime();
            var svc = MakeService(rt, new System.Random(1)); // không gold provider
            svc.Tick(0);

            Assert.IsFalse(svc.Select(1, new SkillChoiceCard(Def(1))), "chưa có event → false");
            Assert.AreEqual(0, rt.Roster.Count, "không learn gì");

            svc.Request(1, SkillChoiceMode.Shop);
            Assert.IsFalse(svc.Select(1, svc.Current(1).Cards[0]), "shop không có gold provider → fail-closed");
            Assert.AreEqual(0, rt.Roster.Count, "không learn gì");
            Assert.IsNotNull(svc.Current(1), "modal vẫn mở (fail-closed, không crash)");
            svc.Close(1);
        }

        // ------------------------------------------------------------------
        // box: learnNum nhiều card (SelectBoxSkill)
        // ------------------------------------------------------------------

        [Test]
        public void Box_LearnNum_MultiPick_TillCount_ThenClose()
        {
            var rt = new SkillCastRuntime();
            var svc = MakeService(rt, new System.Random(5));
            svc.Tick(0);
            svc.Request(1, SkillChoiceMode.Box, 3);

            var ev = svc.Current(1);
            Assert.AreEqual(3, ev.LearnCount);
            Assert.AreEqual(5, ev.Cards.Length, "box draw learnNum + BoxCardExtra(2)");

            Assert.IsTrue(svc.Select(1, ev.Cards[0]));
            Assert.AreEqual(2, svc.Current(1).LearnCount, "lượt chọn giảm");
            Assert.IsNotNull(svc.Current(1), "chưa đủ lượt → modal vẫn mở");
            Assert.AreEqual(1, svc.Pause.Count, "vẫn pause trong box multi-pick");

            Assert.IsTrue(svc.Select(1, ev.Cards[1]));
            Assert.IsTrue(svc.Select(1, ev.Cards[2]));

            Assert.IsNull(svc.Current(1), "đủ lượt → đóng");
            Assert.AreEqual(3, ev.Learned.Count, "WillLearnSkillList parity");
            Assert.AreEqual(3, rt.Roster.Count, "3 skill vào roster");
            Assert.AreEqual(0, svc.Pause.Count);
        }

        [Test]
        public void Box_SelectSkillOutsideCards_Refused()
        {
            var rt = new SkillCastRuntime();
            var svc = MakeService(rt, new System.Random(5));
            svc.Tick(0);
            svc.Request(1, SkillChoiceMode.Box, 1);

            var outside = Def(999); // không nằm trong event cards
            Assert.IsFalse(svc.Select(1, new SkillChoiceCard(outside)), "card lạ → false");
            Assert.AreEqual(0, rt.GetLevel(999), "không học");
            Assert.IsNotNull(svc.Current(1), "modal vẫn mở");
        }

        // ------------------------------------------------------------------
        // pool weight + MaxLevel filter
        // ------------------------------------------------------------------

        [Test]
        public void PoolWeight_WeightWalk_Deterministic()
        {
            var pool = new SkillChoicePool();
            var light = Def(1);
            var heavy = Def(2);
            pool.Add(light, 1);
            pool.Add(heavy, 100); // 100/101
            var rt = new SkillCastRuntime();

            var rollLow = new FixedRng(0); // roll 0 < acc light(1) → light
            Assert.AreSame(light, pool.Draw(1, rt, rollLow)[0]);

            var rollHigh = new FixedRng(100); // roll 100 ≥ acc light(1) → heavy
            Assert.AreSame(heavy, pool.Draw(1, rt, rollHigh)[0]);
        }

        [Test]
        public void PoolWeight_PoolExhausted_DrawLessThanCount()
        {
            var pool = new SkillChoicePool();
            pool.Add(Def(1), 1);
            pool.Add(Def(2), 100);
            var rng = new FixedRng(0, 100);
            var res = pool.Draw(3, new SkillCastRuntime(), rng);
            Assert.AreEqual(2, res.Count, "pool 2 phần tử → draw không trùng → 2 card");
            Assert.AreNotEqual(res[0].Id, res[1].Id, "không trùng lặp trong 1 draw");
        }

        [Test]
        public void Pool_MaxLevelSkill_Excluded()
        {
            var pool = new SkillChoicePool();
            var def = Def(1, maxLevel: 1);
            pool.Add(def);
            var rt = new SkillCastRuntime();
            rt.Learn(def); // đạt max 1

            Assert.AreEqual(0, pool.Draw(1, rt, new System.Random(1)).Count, "max → loại khỏi pool");
        }

        // ------------------------------------------------------------------
        // CardChoicePause ref-count: timescale {0,1} qua delegate
        // ------------------------------------------------------------------

        [Test]
        public void CardChoicePause_RefCount_OnlyZeroRestores()
        {
            var log = new List<bool>();
            var pause = new CardChoicePause(p => log.Add(p));

            pause.Acquire();  // → true
            pause.Acquire();  // count 2, không ghi (đã pause)
            pause.Release();  // count 1, không ghi
            Assert.AreEqual(1, log.Count, "chỉ transition 0→1 ghi true");
            Assert.IsTrue(pause.IsPaused);
            Assert.AreEqual(1, pause.Count);

            pause.Release();  // count 0 → false
            Assert.AreEqual(2, log.Count);
            CollectionAssert.AreEqual(new[] { true, false }, log);
            Assert.IsFalse(pause.IsPaused);
            Assert.AreEqual(0, pause.Count);

            pause.Release();  // dưới 0 → no-op
            Assert.AreEqual(2, log.Count, "release thừa không phá ref-count");
            Assert.IsFalse(pause.IsPaused);
        }
    }
}
