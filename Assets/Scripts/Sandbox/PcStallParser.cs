// -----------------------------------------------------------------------------
// VLTK Mobile — Stall System (Bày Bán) parser
// Source: settings/stall/stall_setting.txt (1 row per stall type).
//   StallId  MaxItemCount  TaxPercent  MaxPrice  MinPrice  RequiredLevel
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using VLTK.Sandbox.ItemData;

namespace VLTK.Sandbox
{
    public static class PcStallParser
    {
        public const int StallIdCol = 0;
        public const int MaxItemCountCol = 1;
        public const int TaxPercentCol = 2;
        public const int MaxPriceCol = 3;
        public const int MinPriceCol = 4;
        public const int RequiredLevelCol = 5;

        public static List<PcStallEntry> ParseFile(string path)
        {
            var rows = new List<PcStallEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                rows.Add(new PcStallEntry
                {
                    stallId = PcItemCommon.Int(cols, StallIdCol),
                    maxItemCount = PcItemCommon.Int(cols, MaxItemCountCol),
                    taxPercent = PcItemCommon.Int(cols, TaxPercentCol),
                    maxPrice = cols.Length > MaxPriceCol ? PcItemCommon.Int(cols, MaxPriceCol) : 0,
                    minPrice = cols.Length > MinPriceCol ? PcItemCommon.Int(cols, MinPriceCol) : 0,
                    requiredLevel = cols.Length > RequiredLevelCol ? PcItemCommon.Int(cols, RequiredLevelCol) : 0,
                });
            }
            return rows;
        }

        public static PcStallRegistry BuildRegistry(string dir)
        {
            var reg = new PcStallRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*.txt", SearchOption.AllDirectories))
                foreach (var s in ParseFile(f)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcStallEntry
    {
        public int stallId;
        public int maxItemCount;
        public int taxPercent;
        public int maxPrice;
        public int minPrice;
        public int requiredLevel;
    }

    public sealed class PcStallRegistry
    {
        private readonly Dictionary<int, PcStallEntry> _byId = new();
        public int Count => _byId.Count;
        public IEnumerable<PcStallEntry> All => _byId.Values;
        public void Register(PcStallEntry e)
        {
            if (e == null || e.stallId <= 0) return;
            _byId[e.stallId] = e;
        }
        public PcStallEntry Get(int stallId)
            => _byId.TryGetValue(stallId, out var v) ? v : null;
    }
}
