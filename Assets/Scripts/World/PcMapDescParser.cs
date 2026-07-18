// -----------------------------------------------------------------------------
// VLTK Mobile — PC mapdesc.txt parser (mô tả map - nhạc nền, thời tiết)
// Source: settings/maps/desc.txt or mapdesc.txt (GB2312). Grouped key/value.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcMapDescEntry
    {
        public int MapId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string MusicTrack { get; set; } = string.Empty;
        public int WeatherType { get; set; }
    }

    public sealed class PcMapDescRegistry
    {
        private readonly Dictionary<int, PcMapDescEntry> _byId = new Dictionary<int, PcMapDescEntry>();
        public int Count => _byId.Count;
        public PcMapDescEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcMapDescEntry> All => _byId.Values;
        public void Add(PcMapDescEntry e) { if (e != null) _byId[e.MapId] = e; }
    }

    public static class PcMapDescParser
    {
        public static PcMapDescRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcMapDescRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            string[] candidates = { "mapdesc.txt", Path.Combine("maps", "desc.txt"), "maps/mapdesc.txt" };
            string path = null;
            foreach (var c in candidates)
            {
                var p = Path.Combine(absoluteDir, c);
                if (File.Exists(p)) { path = p; break; }
            }
            if (path == null) return reg;
            var lines = PcMapListParser.ReadLines(path);
            var groups = new Dictionary<int, Dictionary<string, string>>();
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
                if (!int.TryParse(key.Substring(0, underscore), NumberStyles.Integer, CultureInfo.InvariantCulture, out int mid)) continue;
                var subKey = key.Substring(underscore + 1);
                if (!groups.TryGetValue(mid, out var bag))
                {
                    bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    groups[mid] = bag;
                }
                bag[subKey] = value;
            }
            foreach (var kv in groups)
            {
                var b = kv.Value;
                var e = new PcMapDescEntry
                {
                    MapId = kv.Key,
                    Description = b.TryGetValue("Description", out var d) ? d : (b.TryGetValue("Desc", out var d2) ? d2 : string.Empty),
                    MusicTrack = b.TryGetValue("Music", out var m) ? m : (b.TryGetValue("MusicTrack", out var m2) ? m2 : string.Empty),
                    WeatherType = b.TryGetValue("Weather", out var w) && int.TryParse(w, NumberStyles.Integer, CultureInfo.InvariantCulture, out int wv) ? wv : 0
                };
                reg.Add(e);
            }
            return reg;
        }
    }
}
