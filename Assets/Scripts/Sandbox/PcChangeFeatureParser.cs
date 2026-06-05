// -----------------------------------------------------------------------------
// VLTK Mobile — Change Feature (Đổi Ngoại Hình) parser
// Source: settings/changefeature/changefeature.txt (15 entries).
//   FeatureId  RequiredItemGenre  RequiredItemDetail  RequiredItemCount
//   ResultSpriteId  CostSilver
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using VLTK.Sandbox.ItemData;

namespace VLTK.Sandbox
{
    public static class PcChangeFeatureParser
    {
        public const int FeatureIdCol = 0;
        public const int RequiredItemGenreCol = 1;
        public const int RequiredItemDetailCol = 2;
        public const int RequiredItemCountCol = 3;
        public const int ResultSpriteIdCol = 4;
        public const int CostSilverCol = 5;

        public static List<PcChangeFeatureEntry> ParseFile(string path)
        {
            var rows = new List<PcChangeFeatureEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 4) continue;
                rows.Add(new PcChangeFeatureEntry
                {
                    featureId = PcItemCommon.Int(cols, FeatureIdCol),
                    requiredItemGenre = PcItemCommon.Int(cols, RequiredItemGenreCol),
                    requiredItemDetail = PcItemCommon.Int(cols, RequiredItemDetailCol),
                    requiredItemCount = PcItemCommon.Int(cols, RequiredItemCountCol),
                    resultSpriteId = cols.Length > ResultSpriteIdCol ? PcItemCommon.Int(cols, ResultSpriteIdCol) : 0,
                    costSilver = cols.Length > CostSilverCol ? PcItemCommon.Int(cols, CostSilverCol) : 0,
                });
            }
            return rows;
        }

        public static PcChangeFeatureRegistry BuildRegistry(string dir)
        {
            var reg = new PcChangeFeatureRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*.txt", SearchOption.AllDirectories))
                foreach (var s in ParseFile(f)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcChangeFeatureEntry
    {
        public int featureId;
        public int requiredItemGenre;
        public int requiredItemDetail;
        public int requiredItemCount;
        public int resultSpriteId;
        public int costSilver;
    }

    public sealed class PcChangeFeatureRegistry
    {
        private readonly Dictionary<int, PcChangeFeatureEntry> _byId = new();
        public int Count => _byId.Count;
        public IEnumerable<PcChangeFeatureEntry> All => _byId.Values;
        public void Register(PcChangeFeatureEntry e)
        {
            if (e == null || e.featureId <= 0) return;
            _byId[e.featureId] = e;
        }
        public PcChangeFeatureEntry Get(int featureId)
            => _byId.TryGetValue(featureId, out var v) ? v : null;
    }
}
