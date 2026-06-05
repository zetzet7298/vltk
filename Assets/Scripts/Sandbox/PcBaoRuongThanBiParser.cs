// -----------------------------------------------------------------------------
// VLTK Mobile — Bảo Rương Thần Bí (Mystery Chest) parser
// Source: settings/event/shenmibaoxiang/shenmibaoxiangaward.txt (8 entries).
//   BoxId  BoxName  Tier  RequiredLevel  RewardId  RewardCount  Probability
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using VLTK.Sandbox.ItemData;

namespace VLTK.Sandbox
{
    public static class PcBaoRuongThanBiParser
    {
        public const int BoxIdCol = 0;
        public const int BoxNameCol = 1;
        public const int TierCol = 2;
        public const int RequiredLevelCol = 3;
        public const int RewardIdCol = 4;
        public const int RewardCountCol = 5;
        public const int ProbabilityCol = 6;

        public static List<PcBaoRuongThanBiEntry> ParseFile(string path)
        {
            var rows = new List<PcBaoRuongThanBiEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 5) continue;
                rows.Add(new PcBaoRuongThanBiEntry
                {
                    boxId = PcItemCommon.Int(cols, BoxIdCol),
                    nameRaw = PcItemCommon.Str(cols, BoxNameCol),
                    tier = PcItemCommon.Int(cols, TierCol),
                    requiredLevel = PcItemCommon.Int(cols, RequiredLevelCol),
                    rewardId = PcItemCommon.Int(cols, RewardIdCol),
                    rewardCount = PcItemCommon.Int(cols, RewardCountCol),
                    probability = cols.Length > ProbabilityCol ? PcItemCommon.Int(cols, ProbabilityCol) : 10000,
                });
            }
            return rows;
        }

        public static PcBaoRuongThanBiRegistry BuildRegistry(string dir)
        {
            var reg = new PcBaoRuongThanBiRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*.txt", SearchOption.AllDirectories))
                foreach (var s in ParseFile(f)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcBaoRuongThanBiEntry
    {
        public int boxId;
        public string nameRaw;
        public int tier;
        public int requiredLevel;
        public int rewardId;
        public int rewardCount;
        public int probability;
    }

    public sealed class PcBaoRuongThanBiRegistry
    {
        private readonly Dictionary<int, PcBaoRuongThanBiEntry> _byId = new();
        private readonly Dictionary<int, List<PcBaoRuongThanBiEntry>> _byTier = new();
        public int Count => _byId.Count;
        public IEnumerable<PcBaoRuongThanBiEntry> All => _byId.Values;
        public void Register(PcBaoRuongThanBiEntry e)
        {
            if (e == null || e.boxId <= 0) return;
            _byId[e.boxId] = e;
            if (!_byTier.TryGetValue(e.tier, out var list))
            {
                list = new List<PcBaoRuongThanBiEntry>();
                _byTier[e.tier] = list;
            }
            list.Add(e);
        }
        public PcBaoRuongThanBiEntry Get(int boxId)
            => _byId.TryGetValue(boxId, out var v) ? v : null;
        public IReadOnlyList<PcBaoRuongThanBiEntry> GetByTier(int tier)
            => _byTier.TryGetValue(tier, out var v)
                ? (IReadOnlyList<PcBaoRuongThanBiEntry>)v
                : System.Array.Empty<PcBaoRuongThanBiEntry>();
    }
}
