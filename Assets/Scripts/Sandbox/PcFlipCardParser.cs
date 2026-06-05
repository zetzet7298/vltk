// -----------------------------------------------------------------------------
// VLTK Mobile — Flip Card (Lật Thẻ) parser
// Source: settings/flipcard/flipcard.txt (2 versions, ~10 cards each).
//   CardId  CardName  RewardId  RewardCount  Probability  Tier
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using VLTK.Sandbox.ItemData;

namespace VLTK.Sandbox
{
    public static class PcFlipCardParser
    {
        public const int CardIdCol = 0;
        public const int CardNameCol = 1;
        public const int RewardIdCol = 2;
        public const int RewardCountCol = 3;
        public const int ProbabilityCol = 4;
        public const int TierCol = 5;

        public static List<PcFlipCardEntry> ParseFile(string path)
        {
            var rows = new List<PcFlipCardEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 5) continue;
                rows.Add(new PcFlipCardEntry
                {
                    cardId = PcItemCommon.Int(cols, CardIdCol),
                    nameRaw = PcItemCommon.Str(cols, CardNameCol),
                    rewardId = PcItemCommon.Int(cols, RewardIdCol),
                    rewardCount = PcItemCommon.Int(cols, RewardCountCol),
                    probability = PcItemCommon.Int(cols, ProbabilityCol),
                    tier = cols.Length > TierCol ? PcItemCommon.Int(cols, TierCol) : 1,
                });
            }
            return rows;
        }

        public static PcFlipCardRegistry BuildRegistry(string dir)
        {
            var reg = new PcFlipCardRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*.txt", SearchOption.AllDirectories))
                foreach (var s in ParseFile(f)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcFlipCardEntry
    {
        public int cardId;
        public string nameRaw;
        public int rewardId;
        public int rewardCount;
        public int probability;
        public int tier;
    }

    public sealed class PcFlipCardRegistry
    {
        private readonly Dictionary<int, PcFlipCardEntry> _byId = new();
        private readonly Dictionary<int, List<PcFlipCardEntry>> _byTier = new();
        public int Count => _byId.Count;
        public IEnumerable<PcFlipCardEntry> All => _byId.Values;
        public void Register(PcFlipCardEntry e)
        {
            if (e == null || e.cardId <= 0) return;
            _byId[e.cardId] = e;
            if (!_byTier.TryGetValue(e.tier, out var list))
            {
                list = new List<PcFlipCardEntry>();
                _byTier[e.tier] = list;
            }
            list.Add(e);
        }
        public PcFlipCardEntry Get(int cardId)
            => _byId.TryGetValue(cardId, out var v) ? v : null;
        public IReadOnlyList<PcFlipCardEntry> GetByTier(int tier)
            => _byTier.TryGetValue(tier, out var v)
                ? (IReadOnlyList<PcFlipCardEntry>)v
                : System.Array.Empty<PcFlipCardEntry>();
    }
}
