// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/item_exchange.txt Đổi Vật Phẩm parser
// Source: item_exchange.txt (GB2312, tab-separated). Each row maps a recipe
// (require item + count → get item + count) keyed by id.
//   Id  Name  RequireItemGenre  RequireItemDetail  RequireItemParticular
//   RequireCount  GetItemGenre  GetItemDetail  GetItemParticular
//   GetCount  MinLevel
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcItemExchangeParser
    {
        public const int IdCol = 0;
        public const int NameCol = 1;
        public const int ReqGenreCol = 2;
        public const int ReqDetailCol = 3;
        public const int ReqParticularCol = 4;
        public const int ReqCountCol = 5;
        public const int GetGenreCol = 6;
        public const int GetDetailCol = 7;
        public const int GetParticularCol = 8;
        public const int GetCountCol = 9;
        public const int MinLevelCol = 10;

        public static List<PcItemExchangeEntry> ParseFile(string path)
        {
            var rows = new List<PcItemExchangeEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 6) continue;
                int id = PcItemCommon.Int(cols, IdCol);
                if (id <= 0) continue;
                rows.Add(new PcItemExchangeEntry
                {
                    id = id,
                    nameRaw = PcItemCommon.Str(cols, NameCol),
                    requireGenre = PcItemCommon.Int(cols, ReqGenreCol),
                    requireDetail = PcItemCommon.Int(cols, ReqDetailCol),
                    requireParticular = PcItemCommon.Int(cols, ReqParticularCol),
                    requireCount = PcItemCommon.Int(cols, ReqCountCol),
                    getGenre = cols.Length > GetGenreCol ? PcItemCommon.Int(cols, GetGenreCol) : 0,
                    getDetail = cols.Length > GetDetailCol ? PcItemCommon.Int(cols, GetDetailCol) : 0,
                    getParticular = cols.Length > GetParticularCol ? PcItemCommon.Int(cols, GetParticularCol) : 0,
                    getCount = cols.Length > GetCountCol ? PcItemCommon.Int(cols, GetCountCol) : 0,
                    minLevel = cols.Length > MinLevelCol ? PcItemCommon.Int(cols, MinLevelCol) : 0,
                });
            }
            return rows;
        }

        public static PcItemExchangeRegistry BuildRegistry(string dir)
        {
            var reg = new PcItemExchangeRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, "item_exchange.txt");
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcItemExchangeEntry
    {
        public int id;
        public string nameRaw;
        public int requireGenre;
        public int requireDetail;
        public int requireParticular;
        public int requireCount;
        public int getGenre;
        public int getDetail;
        public int getParticular;
        public int getCount;
        public int minLevel;
    }

    public sealed class PcItemExchangeRegistry
    {
        private readonly Dictionary<int, PcItemExchangeEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcItemExchangeEntry e)
        {
            if (e == null || e.id <= 0) return;
            _byId[e.id] = e;
        }
        public PcItemExchangeEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcItemExchangeEntry> GetAll() => _byId.Values;
    }
}
