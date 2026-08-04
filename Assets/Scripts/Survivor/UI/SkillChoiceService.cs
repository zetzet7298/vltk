// -----------------------------------------------------------------------------
// VLTK.Survivor — SkillChoiceService (ticket 29)
// SurvivorRandomSkillCtrl parity (r-dhcd-002 + research 06 S1-S9), pure logic:
//  - 3 mode: SkillChoiceMode {1 LevelUp, 2 Box, 3 Shop} + box learnNum
//    (RandomSkillParam.Type + RandomSkillBoxParam.learnNum parity).
//  - Per-role queue: Dictionary<ulong,PlayerData> + Queue<SkillChoiceParam> +
//    BeginWaitingLearnTime (PlayerRandomSkillData parity; waiting = time
//    predicate IsPlayerWaitingLearn). RequestRandomSkill parity: đang chọn →
//    enqueue FIFO; rảnh → trigger ngay. Selection thành công → CheckWaitingList
//    pump (NormalLevelLogic.SelectClientRandomSkill parity).
//  - Reroll 2 cmd riêng: RerollLevelUp (FrameCmdRerandomSkill, giới hạn lượt
//    own) + ShopReroll (FrameCmdReSelectRandomSkill, giá cố định trừ vàng —
//    XianDaoShopConfig.RefreshPrice parity-shape, số own).
//  - Card pool theo weight own-design (RandomSkillConfig.LevelUpRandomWeight
//    parity-shape; số liệu own); skill đã đạt MaxLevel bị loại khỏi pool
//    (RandomSkillLibraryConfig.IsMaxLevel parity-shape) → card trùng chỉ nâng
//    cấp tới cap, không tràn.
//  - Pick → SkillCastRuntime.Learn (roster ticket 27).
//  - Pause card scope: SurvivorPause ref-count scope "CardChoice" (r-dhcd-003
//    m_pauseCount parity-shape; own: timeScale ∈ {0,1}, KHÔNG claim input lock).
//    ticket 43: dùng CHUNG SurvivorPause toàn game (director inject) — card +
//    settings + app-lifecycle + gameover + levelup chung 1 counter, không bao
//    giờ resume nhầm khi scope khác còn giữ.
// Core thuần (không scene, RNG + gold + pause inject qua delegate) = EditMode
// test được (spec Testing Decisions).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace VLTK.Survivor
{
    /// <summary>RandomSkillEventType parity: 1 levelup / 2 box / 3 shop.</summary>
    public enum SkillChoiceMode { LevelUp = 1, Box = 2, Shop = 3 }

    /// <summary>RandomSkillParam + RandomSkillBoxParam parity.</summary>
    public readonly struct SkillChoiceParam
    {
        public readonly SkillChoiceMode Mode;
        public readonly int LearnNum; // box only
        public SkillChoiceParam(SkillChoiceMode mode, int learnNum = 0)
        {
            Mode = mode;
            LearnNum = learnNum;
        }
    }

    /// <summary>1 card trên modal (LevelSkillShopParam parity-shape: Price &gt; 0 = shop mua).</summary>
    public readonly struct SkillChoiceCard
    {
        public readonly SkillDef Def;
        public readonly int Price;

        public SkillChoiceCard(SkillDef def, int price = 0)
        {
            Def = def;
            Price = price;
        }

        public string Title =>
            Def != null && Def.Name.Length > 0 ? Def.Name : Def != null ? "Skill #" + Def.Id : "?";
        public string Desc => Def != null && Def.Desc.Length > 0 ? Def.Desc : "Chưa có mô tả";
        /// <summary>Staged precast uid; "" → UI proxy màu (fail-closed, không bịa path).</summary>
        public string IconUid => Def != null ? Def.PreCastSprUid : "";
    }

    /// <summary>
    /// Event param đang hiển thị (LevelEventParam / LevelBoxEventParam /
    /// LevelSkillShopEventParam parity-shape).
    /// </summary>
    public sealed class SkillChoiceEvent
    {
        public ulong RoleId;
        public SkillChoiceMode Mode;
        public SkillChoiceCard[] Cards;
        public int RerollsLeft;   // levelup only (FrameCmdRerandomSkill)
        public int LearnCount;    // box: lượt chọn còn lại
        /// <summary>Box đã chọn (LevelBoxEventParam.WillLearnSkillList parity).</summary>
        public readonly List<SkillDef> Learned = new List<SkillDef>();
    }

    /// <summary>
    /// Card pool theo weight own-design (RandomSkillConfig.LevelUpRandomWeight
    /// parity-shape; số liệu own — weight mặc định 1, gán cao hơn cho card
    /// hiếm). Draw không trùng, theo weight walk (total → roll → walk).
    /// </summary>
    public sealed class SkillChoicePool
    {
        public const int DefaultWeight = 1;

        public readonly List<SkillDef> Entries = new List<SkillDef>();
        private readonly Dictionary<int, int> _weights = new Dictionary<int, int>();

        public void Add(SkillDef def, int weight = DefaultWeight)
        {
            if (def == null) return;
            Entries.Add(def);
            _weights[def.Id] = weight;
        }

        public int WeightOf(int skillId)
        {
            return _weights.TryGetValue(skillId, out int w) && w > 0 ? w : DefaultWeight;
        }

        /// <summary>Draw ≤ count card không trùng theo weight; loại skill đã MaxLevel.</summary>
        public List<SkillDef> Draw(int count, SkillCastRuntime roster, System.Random rng)
        {
            var res = new List<SkillDef>();
            var cand = new List<SkillDef>(Entries.Count);
            for (int i = 0; i < Entries.Count; i++)
            {
                var def = Entries[i];
                if (AtMaxLevel(def, roster)) continue;
                cand.Add(def);
            }
            while (res.Count < count && cand.Count > 0)
            {
                int total = 0;
                for (int i = 0; i < cand.Count; i++) total += WeightOf(cand[i].Id);
                int roll = rng.Next(total);
                int idx = 0;
                for (int acc = 0; idx < cand.Count; idx++)
                {
                    acc += WeightOf(cand[idx].Id);
                    if (roll < acc) break;
                }
                res.Add(cand[idx]);
                cand.RemoveAt(idx);
            }
            return res;
        }

        private static bool AtMaxLevel(SkillDef def, SkillCastRuntime roster)
        {
            int max = def.MaxLevel > 0 ? def.MaxLevel : 99;
            return roster != null && roster.GetLevel(def.Id) >= max;
        }
    }

    /// <summary>
    /// 3-mode skill choice + per-role queue + reroll (ticket 29). Pure logic —
    /// UI (OverlayPanel) render Current(roleId).Cards, gọi Select / RerollLevelUp
    /// / ShopReroll / Close. Test EditMode inject RNG + gold + pause delegate,
    /// không scene, không đụng Time.timeScale.
    /// </summary>
    public sealed class SkillChoiceService
    {
        // own-design constants (spec D5: số balance = own, ghi rationale)
        public const int LevelUpCardCount = 3;    // user story 7: levelup 3 card
        public const int ShopCardCount = 3;       // shop cũng 3 card
        public const int BoxCardExtra = 2;        // box draw learnNum+2, chọn learnNum (dư lựa chọn)
        public const int MaxLevelUpRerolls = 2;   // reroll levelup giới hạn own — không kẹt build, không vô hạn
        public const int ShopCardPrice = 10;      // giá mua card shop (XianDaoShopConfig.BuyPriceWeight parity-shape, own số)
        public const int ShopRerollPrice = 5;     // giá reroll shop cố định (RefreshPrice parity-shape, own số)
        /// <summary>beginWaitingLearnTime window (O6 own): hết → auto-close. Instance field (không const)
        /// — runtime mặc định 30s, test rút timeout (ticket 44 PlayMode) không đợi thật.</summary>
        public float WaitingLearnWindow = 30f;

        private sealed class PlayerData
        {
            public ulong RoleId;
            public readonly Queue<SkillChoiceParam> WaitingQueue = new Queue<SkillChoiceParam>();
            public float BeginWaitingLearnTime = float.NegativeInfinity; // parity: mặc định "không waiting"
            public SkillChoiceEvent Current;
        }

        private readonly Dictionary<ulong, PlayerData> _players = new Dictionary<ulong, PlayerData>();
        private readonly SkillCastRuntime _roster;
        private readonly SkillChoicePool _pool;
        private readonly System.Random _rng;
        private readonly System.Func<ulong, int, bool> _trySpendGold; // null = shop không mua/reroll được (fail-closed)
        private float _now;

        public readonly SurvivorPause Pause;

        public SkillChoiceService(SkillCastRuntime roster, SkillChoicePool pool, System.Random rng,
            System.Func<ulong, int, bool> trySpendGold = null, SurvivorPause pause = null)
        {
            _roster = roster;
            _pool = pool;
            _rng = rng;
            _trySpendGold = trySpendGold;
            // ticket 43: pause dùng CHUNG SurvivorPause toàn game (director inject) —
            // nếu null tự tạo standalone (test / chưa wire) với apply timescale.
            Pause = pause ?? new SurvivorPause(paused => Time.timeScale = paused ? 0f : 1f);
        }

        /// <summary>
        /// RequestRandomSkill parity: đang chọn → enqueue FIFO (return false);
        /// rảnh → trigger ngay (return true).
        /// </summary>
        public bool Request(ulong roleId, SkillChoiceMode mode, int learnNum = 0)
        {
            var d = GetOrCreate(roleId);
            var param = new SkillChoiceParam(mode, learnNum);
            if (IsWaiting(d)) { d.WaitingQueue.Enqueue(param); return false; }
            Trigger(d, param);
            return true;
        }

        public SkillChoiceEvent Current(ulong roleId)
        {
            return _players.TryGetValue(roleId, out var d) ? d.Current : null;
        }

        public bool IsWaiting(ulong roleId)
        {
            return _players.TryGetValue(roleId, out var d) && IsWaiting(d);
        }

        /// <summary>IsPlayerWaitingLearn parity: waiting window đang mở (now &lt; begin).</summary>
        private bool IsWaiting(PlayerData d) => _now < d.BeginWaitingLearnTime;

        /// <summary>
        /// Check*SkillEvent + selection→pump parity (SelectClientRandomSkill):
        /// levelup = chọn 1 → learn + đóng + pump; box = chọn tới đủ learnNum;
        /// shop = trừ vàng (giá card) rồi learn + đóng. Fail-closed: card lạ /
        /// không đủ vàng / chưa có event → false, không đổi state.
        /// </summary>
        public bool Select(ulong roleId, SkillChoiceCard card)
        {
            var d = GetOrCreate(roleId);
            var ev = d.Current;
            if (ev == null || card.Def == null) return false;
            switch (ev.Mode)
            {
                case SkillChoiceMode.LevelUp:
                    if (!Contains(ev, card.Def)) return false; // ticket 43: card lạ → từ chối (Box đã có)
                    _roster.Learn(card.Def);
                    Close(roleId);
                    return true;

                case SkillChoiceMode.Box:
                    if (!Contains(ev, card.Def)) return false;
                    _roster.Learn(card.Def);
                    ev.Learned.Add(card.Def);
                    ev.LearnCount--;
                    if (ev.LearnCount <= 0) Close(roleId);
                    return true;

                case SkillChoiceMode.Shop:
                    if (!Contains(ev, card.Def)) return false; // ticket 43: card lạ → từ chối trước khi trừ vàng
                    if (_trySpendGold == null || !_trySpendGold(roleId, card.Price)) return false;
                    _roster.Learn(card.Def);
                    Close(roleId);
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>FrameCmdRerandomSkill parity: levelup reroll, giới hạn lượt own.</summary>
        public bool RerollLevelUp(ulong roleId)
        {
            var ev = Current(roleId);
            if (ev == null || ev.Mode != SkillChoiceMode.LevelUp || ev.RerollsLeft <= 0) return false;
            ev.Cards = DrawCards(ev.Mode, 0);
            ev.RerollsLeft--;
            return true;
        }

        /// <summary>FrameCmdReSelectRandomSkill parity: shop reroll giá cố định trừ vàng.</summary>
        public bool ShopReroll(ulong roleId)
        {
            var ev = Current(roleId);
            if (ev == null || ev.Mode != SkillChoiceMode.Shop) return false;
            if (_trySpendGold == null || !_trySpendGold(roleId, ShopRerollPrice)) return false;
            ev.Cards = DrawCards(ev.Mode, 0);
            return true;
        }

        /// <summary>
        /// Đóng modal không chọn (OnMiJiResultUIClose parity) → SetPlayerNotWaiting
        /// + release pause + CheckWaitingList pump.
        /// </summary>
        public void Close(ulong roleId)
        {
            if (!_players.TryGetValue(roleId, out var d)) return;
            d.Current = null;
            d.BeginWaitingLearnTime = float.NegativeInfinity;
            Pause.Release(SurvivorPause.CardChoiceScope);
            Pump(d);
        }

        /// <summary>
        /// Game loop tick (now giây): timeout waiting window → auto-close (O6
        /// own). Fail-closed: KHÔNG auto-learn — player chỉ bỏ lỡ event, state
        /// queue giữ nguyên, event kế tiếp pump bình thường.
        /// </summary>
        public void Tick(float now)
        {
            _now = now;
            foreach (var kv in _players)
            {
                var d = kv.Value;
                if (d.Current != null && !IsWaiting(d)) Close(kv.Key);
            }
        }

        // ------------------------------------------------------------------
        // internal
        // ------------------------------------------------------------------

        private void Trigger(PlayerData d, SkillChoiceParam param)
        {
            d.Current = new SkillChoiceEvent
            {
                RoleId = d.RoleId,
                Mode = param.Mode,
                Cards = DrawCards(param.Mode, param.LearnNum),
                RerollsLeft = MaxLevelUpRerolls,
                LearnCount = param.Mode == SkillChoiceMode.Box ? Mathf.Max(1, param.LearnNum) : 0,
            };
            d.BeginWaitingLearnTime = _now + WaitingLearnWindow; // SetPlayerWaiting parity
            Pause.Acquire(SurvivorPause.CardChoiceScope);
        }

        private SkillChoiceCard[] DrawCards(SkillChoiceMode mode, int learnNum)
        {
            int count = mode == SkillChoiceMode.LevelUp ? LevelUpCardCount
                : mode == SkillChoiceMode.Box ? Mathf.Max(1, learnNum) + BoxCardExtra
                : ShopCardCount;
            int price = mode == SkillChoiceMode.Shop ? ShopCardPrice : 0;
            var defs = _pool.Draw(count, _roster, _rng);
            var cards = new SkillChoiceCard[defs.Count];
            for (int i = 0; i < defs.Count; i++) cards[i] = new SkillChoiceCard(defs[i], price);
            return cards;
        }

        /// <summary>CheckWaitingList parity: rảnh → dequeue FIFO → trigger tiếp.</summary>
        private void Pump(PlayerData d)
        {
            while (!IsWaiting(d) && d.WaitingQueue.Count > 0)
                Trigger(d, d.WaitingQueue.Dequeue());
        }

        private PlayerData GetOrCreate(ulong roleId)
        {
            if (!_players.TryGetValue(roleId, out var d))
            {
                d = new PlayerData { RoleId = roleId };
                _players[roleId] = d;
            }
            return d;
        }

        private static bool Contains(SkillChoiceEvent ev, SkillDef def)
        {
            for (int i = 0; i < ev.Cards.Length; i++)
                if (ev.Cards[i].Def.Id == def.Id) return true;
            return false;
        }
    }
}
