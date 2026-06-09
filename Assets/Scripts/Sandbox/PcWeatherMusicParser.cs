// -----------------------------------------------------------------------------
// VLTK Mobile — PC weather/music source index parser.
// Source: vl_update_27 settings/weather and settings/music. Index only.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcWeatherMusicParser
    {
        public const string SourceFileName = "weather_music_index.txt";
        public const string DefaultRelativeDir = "Reference/PcWeatherMusic";

        public static List<PcWeatherMusicEntry> ParseFile(string path)
        {
            var rows = new List<PcWeatherMusicEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            bool header = false;
            foreach (var raw in File.ReadAllText(path).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var cols = raw.Split('\t');
                if (!header) { header = true; continue; }
                if (cols.Length < 11) continue;
                rows.Add(new PcWeatherMusicEntry
                {
                    key = Str(cols, 0),
                    fileName = Str(cols, 1),
                    serverPath = Str(cols, 2),
                    clientPath = Str(cols, 3),
                    bytes = Int(cols, 4),
                    sha256 = Str(cols, 5),
                    lineCount = Int(cols, 6),
                    sectionCount = Int(cols, 7),
                    dataRowCount = Int(cols, 8),
                    clientServerByteIdentical = Bool(cols, 9),
                    notes = Str(cols, 10),
                });
            }
            return rows;
        }

        public static PcWeatherMusicRegistry BuildRegistry(string dirOrFile)
        {
            string path = Directory.Exists(dirOrFile) ? Path.Combine(dirOrFile, SourceFileName) : dirOrFile;
            var reg = new PcWeatherMusicRegistry();
            foreach (var row in ParseFile(path)) reg.Register(row);
            return reg;
        }

        private static string Str(string[] cols, int i) => i >= 0 && i < cols.Length ? (cols[i] ?? string.Empty).Trim() : string.Empty;
        private static int Int(string[] cols, int i) => int.TryParse(Str(cols, i), out var v) ? v : 0;
        private static bool Bool(string[] cols, int i) => string.Equals(Str(cols, i), "true", StringComparison.OrdinalIgnoreCase);
    }

    public sealed class PcWeatherMusicRegistry
    {
        private readonly List<PcWeatherMusicEntry> _all = new List<PcWeatherMusicEntry>();
        private readonly Dictionary<string, PcWeatherMusicEntry> _byKey = new Dictionary<string, PcWeatherMusicEntry>(StringComparer.OrdinalIgnoreCase);

        public int Count => _all.Count;
        public IReadOnlyList<PcWeatherMusicEntry> All => _all;

        public void Register(PcWeatherMusicEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.key)) return;
            _all.Add(entry);
            _byKey[entry.key] = entry;
        }

        public PcWeatherMusicEntry GetByKey(string key)
            => key != null && _byKey.TryGetValue(key, out var row) ? row : null;
    }

    [Serializable]
    public sealed class PcWeatherMusicEntry
    {
        public string key;
        public string fileName;
        public string serverPath;
        public string clientPath;
        public int bytes;
        public string sha256;
        public int lineCount;
        public int sectionCount;
        public int dataRowCount;
        public bool clientServerByteIdentical;
        public string notes;
    }
}
