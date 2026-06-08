// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/item/questkey.txt quest item parser
// Source: item/questkey.txt (2,046 entries, GB2312, tab columns).
//   Name  ItemGenre  DetailType  SpritePath  ObjIndex  Width  Height  Description  ParticularType ...
// Quest items (chìa khoá, đá quý, bảo đồ, ...) used by trigger chains.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcQuestItemParser
    {
        public const int NameCol = 0;
        public const int GenreCol = 1;
        public const int DetailTypeCol = 2;
        public const int DescCol = 7;
        public const int ParticularTypeCol = 8;

        public static List<PcQuestItemEntry> ParseFile(string path)
        {
            var rows = new List<PcQuestItemEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length <= ParticularTypeCol) continue;
                rows.Add(new PcQuestItemEntry
                {
                    itemGenre = PcItemCommon.Int(cols, GenreCol),
                    detailType = PcItemCommon.Int(cols, DetailTypeCol),
                    particularType = PcItemCommon.Int(cols, ParticularTypeCol),
                    nameRaw = PcItemCommon.Str(cols, NameCol),
                    description = PcItemCommon.Str(cols, DescCol),
                });
            }
            return rows;
        }

        public static PcQuestItemRegistry BuildRegistry(string dir)
        {
            var reg = new PcQuestItemRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string questKey = Path.Combine(dir, "questkey.txt");
            if (File.Exists(questKey))
            {
                foreach (var s in ParseFile(questKey)) reg.Register(s);
                return reg;
            }
            foreach (var f in Directory.GetFiles(dir, "*.txt"))
                foreach (var s in ParseFile(f)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcQuestItemEntry
    {
        public int itemGenre;
        public int detailType;
        public int particularType;
        public string nameRaw;
        public string description;
        public int level;
        public int quality;
    }

    public sealed class PcQuestItemRegistry
    {
        private readonly Dictionary<(int, int, int), PcQuestItemEntry> _byTriple = new();
        private readonly Dictionary<int, PcQuestItemEntry> _byDetailType = new();
        private readonly List<PcQuestItemEntry> _all = new();
        public int Count => _byTriple.Count;
        public IEnumerable<PcQuestItemEntry> All => _all;
        public void Register(PcQuestItemEntry e)
        {
            if (e == null) return;
            var key = (e.itemGenre, e.detailType, e.particularType);
            if (!_byTriple.ContainsKey(key)) _all.Add(e);
            _byTriple[key] = e;
            _byDetailType[e.detailType] = e;
        }
        public PcQuestItemEntry Get(int g, int d, int p) => _byTriple.TryGetValue((g, d, p), out var v) ? v : null;
        public PcQuestItemEntry GetByDetailType(int detailType) => _byDetailType.TryGetValue(detailType, out var v) ? v : null;
    }
}
