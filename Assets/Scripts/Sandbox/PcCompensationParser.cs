// -----------------------------------------------------------------------------
// VLTK Mobile — Compensation (Bồi Thường) parser
// Source: settings/compensation/compensation.txt.
//   CompId  AffectedPlayerCount  ItemGenre  ItemDetail  ItemParticular
//   ItemCount  Silver  ExpireDate
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using VLTK.Sandbox.ItemData;

namespace VLTK.Sandbox
{
    public static class PcCompensationParser
    {
        public const int CompIdCol = 0;
        public const int AffectedPlayerCountCol = 1;
        public const int ItemGenreCol = 2;
        public const int ItemDetailCol = 3;
        public const int ItemParticularCol = 4;
        public const int ItemCountCol = 5;
        public const int SilverCol = 6;
        public const int ExpireDateCol = 7;

        public static List<PcCompensationEntry> ParseFile(string path)
        {
            var rows = new List<PcCompensationEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 4) continue;
                rows.Add(new PcCompensationEntry
                {
                    compId = PcItemCommon.Int(cols, CompIdCol),
                    affectedPlayerCount = PcItemCommon.Int(cols, AffectedPlayerCountCol),
                    itemGenre = PcItemCommon.Int(cols, ItemGenreCol),
                    itemDetail = PcItemCommon.Int(cols, ItemDetailCol),
                    itemParticular = PcItemCommon.Int(cols, ItemParticularCol),
                    itemCount = cols.Length > ItemCountCol ? PcItemCommon.Int(cols, ItemCountCol) : 0,
                    silver = cols.Length > SilverCol ? PcItemCommon.Int(cols, SilverCol) : 0,
                    expireDate = cols.Length > ExpireDateCol ? PcItemCommon.Int(cols, ExpireDateCol) : 0,
                });
            }
            return rows;
        }

        public static PcCompensationRegistry BuildRegistry(string dir)
        {
            var reg = new PcCompensationRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*.txt", SearchOption.AllDirectories))
                foreach (var s in ParseFile(f)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcCompensationEntry
    {
        public int compId;
        public int affectedPlayerCount;
        public int itemGenre;
        public int itemDetail;
        public int itemParticular;
        public int itemCount;
        public int silver;
        public int expireDate;

        public bool IsActive(int currentDate)
            => expireDate == 0 || currentDate <= expireDate;
    }

    public sealed class PcCompensationRegistry
    {
        private readonly Dictionary<int, PcCompensationEntry> _byId = new();
        public int Count => _byId.Count;
        public IEnumerable<PcCompensationEntry> All => _byId.Values;
        public void Register(PcCompensationEntry e)
        {
            if (e == null || e.compId <= 0) return;
            _byId[e.compId] = e;
        }
        public PcCompensationEntry Get(int compId)
            => _byId.TryGetValue(compId, out var v) ? v : null;
        public IReadOnlyList<PcCompensationEntry> GetActive(int currentDate)
        {
            var result = new List<PcCompensationEntry>();
            foreach (var e in _byId.Values)
                if (e != null && e.IsActive(currentDate)) result.Add(e);
            return result;
        }
    }
}
