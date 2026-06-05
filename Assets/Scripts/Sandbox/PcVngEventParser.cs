// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/vng_events.txt VNG event (Sự Kiện VNG) parser
// Source: settings/vng_events.txt (195 entries, GB2312, tab-separated).
//   Cols: EventId  Name  Type  RequiredLevel  RequiredVip  RewardSilver
//         RewardItemGenre  RewardItemDetail  RewardItemParticular  RewardItemCount
// Type: 0 = open, 1 = VIP only, 2 = level only, 3 = cả VIP + level.
// VNG là nhánh sự kiện riêng của VNG Corporation (operator Việt Nam của VLTK).
// Tolerant of missing file (trả về registry rỗng).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcVngEventParser
    {
        public const int EventIdCol = 0;
        public const int NameCol = 1;
        public const int TypeCol = 2;
        public const int RequiredLevelCol = 3;
        public const int RequiredVipCol = 4;
        public const int RewardSilverCol = 5;
        public const int RewardItemGenreCol = 6;
        public const int RewardItemDetailCol = 7;
        public const int RewardItemParticularCol = 8;
        public const int RewardItemCountCol = 9;

        public static List<PcVngEventEntry> ParseFile(string path)
        {
            var rows = new List<PcVngEventEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            string[] lines;
            try { lines = PcItemCommon.ReadServerLines(path); }
            catch { lines = File.ReadAllLines(path); }
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 5) continue;
                rows.Add(new PcVngEventEntry
                {
                    eventId = PcItemCommon.Int(cols, EventIdCol),
                    nameVi = PcItemCommon.Str(cols, NameCol),
                    type = PcItemCommon.Int(cols, TypeCol),
                    requiredLevel = PcItemCommon.Int(cols, RequiredLevelCol),
                    requiredVip = PcItemCommon.Int(cols, RequiredVipCol),
                    rewardSilver = cols.Length > RewardSilverCol ? PcItemCommon.Int(cols, RewardSilverCol) : 0,
                    rewardItemGenre = cols.Length > RewardItemGenreCol ? PcItemCommon.Int(cols, RewardItemGenreCol) : 0,
                    rewardItemDetail = cols.Length > RewardItemDetailCol ? PcItemCommon.Int(cols, RewardItemDetailCol) : 0,
                    rewardItemParticular = cols.Length > RewardItemParticularCol ? PcItemCommon.Int(cols, RewardItemParticularCol) : 0,
                    rewardItemCount = cols.Length > RewardItemCountCol ? PcItemCommon.Int(cols, RewardItemCountCol) : 0,
                });
            }
            return rows;
        }

        public static PcVngEventRegistry BuildRegistry(string dir)
        {
            var reg = new PcVngEventRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string[] candidates = { "vng_events.txt", "vngevent.txt", "vng.txt" };
            foreach (var fn in candidates)
            {
                string main = Path.Combine(dir, fn);
                if (File.Exists(main))
                {
                    foreach (var s in ParseFile(main)) reg.Register(s);
                    return reg;
                }
            }
            // Fallback: quét tất cả *.txt
            foreach (var f in Directory.GetFiles(dir, "*.txt"))
            {
                foreach (var s in ParseFile(f)) reg.Register(s);
            }
            return reg;
        }
    }

    [System.Serializable]
    public class PcVngEventEntry
    {
        public int eventId;
        public string nameVi;
        public int type;                  // 0=open, 1=VIP, 2=level, 3=VIP+level
        public int requiredLevel;
        public int requiredVip;
        public int rewardSilver;
        public int rewardItemGenre;
        public int rewardItemDetail;
        public int rewardItemParticular;
        public int rewardItemCount;
    }

    public sealed class PcVngEventRegistry
    {
        private readonly Dictionary<int, PcVngEventEntry> _byId = new();
        private readonly List<PcVngEventEntry> _all = new();
        public int Count => _byId.Count;
        public void Register(PcVngEventEntry e)
        {
            if (e == null || e.eventId <= 0) return;
            _byId[e.eventId] = e;
            _all.Add(e);
        }
        public PcVngEventEntry Get(int eventId)
            => _byId.TryGetValue(eventId, out var v) ? v : null;

        /// <summary>Lọc sự kiện theo cấp VIP yêu cầu (≤ vipLevel).</summary>
        public IEnumerable<PcVngEventEntry> GetByVip(int vipLevel)
        {
            foreach (var e in _all)
                if (e.requiredVip <= vipLevel) yield return e;
        }

        /// <summary>Lọc sự kiện theo cấp nhân vật yêu cầu (≤ playerLevel).</summary>
        public IEnumerable<PcVngEventEntry> GetByLevel(int playerLevel)
        {
            foreach (var e in _all)
                if (e.requiredLevel <= playerLevel) yield return e;
        }

        public IEnumerable<PcVngEventEntry> All => _all;
    }
}
