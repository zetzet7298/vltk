// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/lotterys.txt lottery (vé số / rương thần bảo) parser
// Source: lotterys.txt + lottery.txt + lotterys_.txt (254 entries, GB2312, 34 cols).
//   LOTTERY_NAME  LOTTERY_TYPE  ITEM_GENRE  ITEM_DETAILTYPE  ITEM_PARTICULAR
//   RECURRENCY_DATE_BASE  DAYSLY  WEEKLY  ...
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcLotteryParser
    {
        public const int NameCol = 0;
        public const int TypeCol = 1;
        public const int GenreCol = 2;
        public const int DetailTypeCol = 3;
        public const int ParticularCol = 4;

        public static List<PcLotteryEntry> ParseFile(string path)
        {
            var rows = new List<PcLotteryEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 5) continue;
                rows.Add(new PcLotteryEntry
                {
                    name = PcItemCommon.Str(cols, NameCol),
                    type = PcItemCommon.Str(cols, TypeCol),
                    itemGenre = PcItemCommon.Int(cols, GenreCol),
                    itemDetailType = PcItemCommon.Int(cols, DetailTypeCol),
                    itemParticular = PcItemCommon.Int(cols, ParticularCol),
                    recurrenceBase = PcItemCommon.Str(cols, 5),
                    daysly = PcItemCommon.Int(cols, 6),
                    weekly = PcItemCommon.Int(cols, 7),
                });
            }
            return rows;
        }

        public static PcLotteryRegistry BuildRegistry(string dir)
        {
            var reg = new PcLotteryRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*.txt"))
                foreach (var s in ParseFile(f)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcLotteryEntry
    {
        public string name;
        public string type;
        public int itemGenre;
        public int itemDetailType;
        public int itemParticular;
        public string recurrenceBase;
        public int daysly;
        public int weekly;
    }

    public sealed class PcLotteryRegistry
    {
        private readonly Dictionary<string, PcLotteryEntry> _byName = new();
        public int Count => _byName.Count;
        public IEnumerable<PcLotteryEntry> All => _byName.Values;
        public void Register(PcLotteryEntry e)
        {
            if (e == null || string.IsNullOrEmpty(e.name)) return;
            _byName[e.name] = e;
        }
        public PcLotteryEntry Get(string name) => _byName.TryGetValue(name ?? string.Empty, out var v) ? v : null;
    }
}
