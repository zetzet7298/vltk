// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/attribconstdata.ini magic attribute constants parser
// Source: attribconstdata.ini (Windows-style .ini, sections in [brackets],
//   Count=N lines, Data0=..DataN..).
//   Each section is a magic attribute catalog. We index by section name + index.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcAttribConstParser
    {
        public static List<PcAttribConstSection> ParseFile(string path)
        {
            var rows = new List<PcAttribConstSection>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            PcAttribConstSection current = null;
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith(";")) continue;
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    if (current != null) rows.Add(current);
                    current = new PcAttribConstSection { name = line.Substring(1, line.Length - 2) };
                    continue;
                }
                if (current == null) continue;
                int eq = line.IndexOf('=');
                if (eq < 0) continue;
                var key = line.Substring(0, eq).Trim();
                var val = line.Substring(eq + 1).Trim();
                if (key.Equals("Count", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(val, out int c)) current.count = c;
                }
                else if (key.StartsWith("Data", System.StringComparison.OrdinalIgnoreCase))
                {
                    current.data[key] = val;
                }
                else
                {
                    current.extras[key] = val;
                }
            }
            if (current != null) rows.Add(current);
            return rows;
        }

        public static PcAttribConstRegistry BuildRegistry(string dir)
        {
            var reg = new PcAttribConstRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*.ini"))
                foreach (var s in ParseFile(f)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcAttribConstSection
    {
        public string name;
        public int count;
        public Dictionary<string, string> data = new();
        public Dictionary<string, string> extras = new();
    }

    public sealed class PcAttribConstRegistry
    {
        private readonly Dictionary<string, PcAttribConstSection> _byName = new();
        public int Count => _byName.Count;
        public void Register(PcAttribConstSection s) { if (s == null || string.IsNullOrEmpty(s.name)) return; _byName[s.name] = s; }
        public PcAttribConstSection Get(string name) => _byName.TryGetValue(name ?? string.Empty, out var v) ? v : null;
        public IEnumerable<PcAttribConstSection> GetAll() => _byName.Values;
    }
}
