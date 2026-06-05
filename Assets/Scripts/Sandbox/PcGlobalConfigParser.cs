// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/global.ini (Cấu hình chung) parser
// Source: global.ini (key=value pairs, GB2312, INI-like format).
//   Key  Value  Description
// Sections not parsed as sections - flat key/value store.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    [System.Serializable]
    public class PcGlobalConfigEntry
    {
        public string key;
        public string value;
        public string description;
    }

    public sealed class PcGlobalConfigRegistry
    {
        private readonly Dictionary<string, PcGlobalConfigEntry> _byKey = new();
        public int Count => _byKey.Count;

        public void Register(PcGlobalConfigEntry e)
        {
            if (e == null || string.IsNullOrEmpty(e.key)) return;
            _byKey[e.key] = e;
        }

        public PcGlobalConfigEntry Get(string key)
            => _byKey.TryGetValue(key ?? string.Empty, out var v) ? v : null;

        public string GetValue(string key)
            => Get(key)?.value;

        public int GetIntValue(string key, int defaultVal = 0)
        {
            var v = Get(key);
            if (v == null) return defaultVal;
            if (int.TryParse(v.value, out int i)) return i;
            return defaultVal;
        }

        public List<PcGlobalConfigEntry> GetAll() => new List<PcGlobalConfigEntry>(_byKey.Values);
        public IEnumerable<PcGlobalConfigEntry> All => _byKey.Values;
    }

    public static class PcGlobalConfigParser
    {
        public static List<PcGlobalConfigEntry> ParseFile(string path)
        {
            var rows = new List<PcGlobalConfigEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line)) continue;
                if (line[0] == ';' || line[0] == '#') continue;
                if (line[0] == '[' && line[line.Length - 1] == ']') continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                var key = line.Substring(0, eq).Trim();
                var value = line.Substring(eq + 1).Trim();
                rows.Add(new PcGlobalConfigEntry
                {
                    key = key,
                    value = value,
                    description = "",
                });
            }
            return rows;
        }

        public static PcGlobalConfigRegistry BuildRegistry(string dir)
        {
            var reg = new PcGlobalConfigRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "global*.ini"))
                foreach (var e in ParseFile(f)) reg.Register(e);
            return reg;
        }
    }
}
