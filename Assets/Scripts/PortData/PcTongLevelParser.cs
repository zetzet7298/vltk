// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/tong/* Bang (GUILD) level data parser
// Source: server settings/tong/tong_level_data.txt — 33 levels (GB2312, 3 cols).
//   Level  RequiredFunds  RequiredBuild
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcTongLevelParser
    {
        public static List<PcTongLevelEntry> ParseFile(string path)
        {
            var rows = new List<PcTongLevelEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int level = PcItemCommon.Int(cols, 0);
                if (level <= 0) continue;
                rows.Add(new PcTongLevelEntry
                {
                    level = level,
                    requiredFunds = PcItemCommon.Int(cols, 1),
                    requiredBuild = cols.Length > 2 ? PcItemCommon.Int(cols, 2) : 0,
                });
            }
            return rows;
        }

        public static PcTongLevelRegistry BuildRegistry(string dir)
        {
            var reg = new PcTongLevelRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, "tong_level_data.txt");
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcTongLevelEntry
    {
        public int level;
        public int requiredFunds;
        public int requiredBuild;
    }

    public sealed class PcTongLevelRegistry
    {
        private readonly Dictionary<int, PcTongLevelEntry> _byLevel = new();
        public int Count => _byLevel.Count;
        public void Register(PcTongLevelEntry e) { if (e == null || e.level <= 0) return; _byLevel[e.level] = e; }
        public PcTongLevelEntry Get(int level) => _byLevel.TryGetValue(level, out var v) ? v : null;
        public int MaxLevel => _byLevel.Count == 0 ? 0 : MaxKey();
        private int MaxKey() { int m = 0; foreach (var k in _byLevel.Keys) if (k > m) m = k; return m; }
    }
}
