// -----------------------------------------------------------------------------
// VLTK Mobile — PC item/compoundscript.txt parser (công thức ghép đồ)
// Source: PC item/compoundscript.txt. Columns: COMPOUND_TYPE, COMPOUND_SCRIPT, REMARK
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcCompoundScriptEntry
    {
        public int CompoundType { get; set; }
        public string Script { get; set; }
        public string Remark { get; set; }
    }

    public sealed class PcCompoundScriptRegistry
    {
        private readonly Dictionary<int, PcCompoundScriptEntry> _byType = new Dictionary<int, PcCompoundScriptEntry>();
        public int Count => _byType.Count;
        public PcCompoundScriptEntry Get(int type) => _byType.TryGetValue(type, out var v) ? v : null;
        public IEnumerable<PcCompoundScriptEntry> All => _byType.Values;
        public void Add(PcCompoundScriptEntry e) { if (e != null) _byType[e.CompoundType] = e; }
    }

    public static class PcCompoundScriptParser
    {
        public static PcCompoundScriptRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcCompoundScriptRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "compoundscript.txt");
            if (!File.Exists(path)) return reg;
            var lines = PcMapListParser.ReadLines(path);
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0) continue;
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                if (!int.TryParse(cols[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int type)) continue;
                reg.Add(new PcCompoundScriptEntry
                {
                    CompoundType = type,
                    Script = cols.Length > 1 ? cols[1].Trim() : "",
                    Remark = cols.Length > 2 ? cols[2].Trim() : ""
                });
            }
            return reg;
        }
    }
}
