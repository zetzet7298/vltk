// -----------------------------------------------------------------------------
// VLTK Mobile — PC faction.ini parser (cấu hình môn phái)
// Source: settings/faction/faction.ini (GB2312). Grouped key/value: id_name, id_Sprite, id_Map, ...
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcFactionConfigEntry
    {
        public int FactionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SpritePath { get; set; } = string.Empty;
        public int DefaultMapId { get; set; }
        public int BaseLevel { get; set; }
        public int ColorR { get; set; }
        public int ColorG { get; set; }
        public int ColorB { get; set; }
    }

    public sealed class PcFactionConfigRegistry
    {
        private readonly Dictionary<int, PcFactionConfigEntry> _byId = new Dictionary<int, PcFactionConfigEntry>();
        public int Count => _byId.Count;
        public PcFactionConfigEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcFactionConfigEntry> All => _byId.Values;
        public IReadOnlyList<PcFactionConfigEntry> GetAll() => new List<PcFactionConfigEntry>(_byId.Values);
        public void Add(PcFactionConfigEntry e) { if (e != null) _byId[e.FactionId] = e; }
    }

    public static class PcFactionConfigParser
    {
        public static PcFactionConfigRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcFactionConfigRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "faction.ini");
            if (!File.Exists(path))
            {
                path = Path.Combine(absoluteDir, "faction", "faction.ini");
                if (!File.Exists(path)) return reg;
            }
            var lines = PcMapListParser.ReadLines(path);
            var groups = new Dictionary<int, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
                if (line[0] == '[' && line[line.Length - 1] == ']') continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                var key = line.Substring(0, eq).Trim();
                var value = line.Substring(eq + 1).Trim();
                int underscore = key.IndexOf('_');
                if (underscore <= 0) continue;
                if (!int.TryParse(key.Substring(0, underscore), NumberStyles.Integer, CultureInfo.InvariantCulture, out int fid)) continue;
                var subKey = key.Substring(underscore + 1);
                if (!groups.TryGetValue(fid, out var bag))
                {
                    bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    groups[fid] = bag;
                }
                bag[subKey] = value;
            }
            foreach (var kv in groups)
            {
                var b = kv.Value;
                var e = new PcFactionConfigEntry
                {
                    FactionId = kv.Key,
                    Name = b.TryGetValue("name", out var n) ? n : (b.TryGetValue("Name", out var n2) ? n2 : string.Empty),
                    SpritePath = b.TryGetValue("Sprite", out var sp) ? sp : string.Empty,
                    DefaultMapId = b.TryGetValue("Map", out var m) && int.TryParse(m, NumberStyles.Integer, CultureInfo.InvariantCulture, out int mp) ? mp : 0,
                    BaseLevel = b.TryGetValue("BaseLevel", out var bl) && int.TryParse(bl, NumberStyles.Integer, CultureInfo.InvariantCulture, out int blv) ? blv : 1,
                    ColorR = b.TryGetValue("ColorR", out var cr) && int.TryParse(cr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int crv) ? crv : 255,
                    ColorG = b.TryGetValue("ColorG", out var cg) && int.TryParse(cg, NumberStyles.Integer, CultureInfo.InvariantCulture, out int cgv) ? cgv : 255,
                    ColorB = b.TryGetValue("ColorB", out var cb) && int.TryParse(cb, NumberStyles.Integer, CultureInfo.InvariantCulture, out int cbv) ? cbv : 255
                };
                reg.Add(e);
            }
            return reg;
        }
    }
}
