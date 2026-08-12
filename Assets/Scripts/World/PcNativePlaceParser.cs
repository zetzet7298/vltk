// -----------------------------------------------------------------------------
// VLTK Mobile — PC nativeplacelist.ini parser (danh sách quê hương)
// Source: settings/nativeplacelist.ini
// INI format: [0], [1], ... sections with Id=, Name=, Img=, Frame=, Desc=
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcNativePlaceEntry
    {
        public int PlaceId { get; set; }
        public string PlaceName { get; set; }
        public string Img { get; set; }
        public int Frame { get; set; }
        public string Desc { get; set; }
    }

    public sealed class PcNativePlaceRegistry
    {
        private readonly Dictionary<int, PcNativePlaceEntry> _byId = new Dictionary<int, PcNativePlaceEntry>();
        public int Count => _byId.Count;
        public PcNativePlaceEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcNativePlaceEntry> All => _byId.Values;
        public void Add(PcNativePlaceEntry e) { if (e != null) _byId[e.PlaceId] = e; }
    }

    public static class PcNativePlaceParser
    {
        public static PcNativePlaceRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcNativePlaceRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "nativeplacelist.ini");
            if (!File.Exists(path)) return reg;
            var lines = PcText.ReadLinesTcvn3(path);

            // INI: sections like [0], [1], ... each containing Id=, Name=, Img=, Frame=, Desc=
            var current = new Dictionary<string, string>();

            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("#")) continue;

                // Section header — flush previous section
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    FlushSection(reg, current);
                    current = new Dictionary<string, string>();
                    continue;
                }

                var eqIdx = line.IndexOf('=');
                if (eqIdx > 0)
                {
                    var key = line.Substring(0, eqIdx).Trim();
                    var val = line.Substring(eqIdx + 1).Trim();
                    current[key] = val;
                }
            }
            // Flush last section
            FlushSection(reg, current);
            return reg;
        }

        private static void FlushSection(PcNativePlaceRegistry reg, Dictionary<string, string> kv)
        {
            if (kv.Count == 0) return;
            if (!kv.TryGetValue("Id", out var idStr)) return;
            if (!int.TryParse(idStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)) return;

            reg.Add(new PcNativePlaceEntry
            {
                PlaceId = id,
                PlaceName = kv.TryGetValue("Name", out var name) ? name : $"Place_{id}",
                Img = kv.TryGetValue("Img", out var img) ? img : "",
                Frame = kv.TryGetValue("Frame", out var frameStr) && int.TryParse(frameStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int f) ? f : 0,
                Desc = kv.TryGetValue("Desc", out var desc) ? desc : ""
            });
        }
    }
}
