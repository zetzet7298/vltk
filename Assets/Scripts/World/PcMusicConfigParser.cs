// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/music/musicset.txt parser
// Source: settings/music/musicset.txt (GBK). Tab-separated.
// Mỗi dòng = 1 map. Tối đa 4 bài nhạc nền + volume/start/end/cycleRandom.
// MusicService đã tồn tại (runtime). Parser này bổ sung lookup config chi tiết.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace VLTK.Sandbox
{
    [Serializable]
    public class PcMusicTrack
    {
        public string file;
        public int volume;
        public int startTime;
        public int endTime;
        public int cycleRandom;
    }

    [Serializable]
    public class PcMusicConfigEntry
    {
        public int mapId;
        public List<PcMusicTrack> tracks = new();
    }

    public sealed class PcMusicConfigRegistry
    {
        private readonly Dictionary<int, PcMusicConfigEntry> _byMap = new();
        public int Count => _byMap.Count;
        public void Register(PcMusicConfigEntry e)
        {
            if (e == null || e.mapId <= 0) return;
            _byMap[e.mapId] = e;
        }
        public PcMusicConfigEntry Get(int mapId) => _byMap.TryGetValue(mapId, out var v) ? v : null;
        public IReadOnlyList<PcMusicConfigEntry> All => new List<PcMusicConfigEntry>(_byMap.Values);
    }

    public static class PcMusicConfigParser
    {
        public static PcMusicConfigRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcMusicConfigRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "musicset.txt");
            if (!File.Exists(path)) return reg;
            var lines = PcMapListParser.ReadLines(path);
            bool headerSkipped = false;
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                if (!int.TryParse(cols[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int mapId) || mapId <= 0) continue;
                var entry = new PcMusicConfigEntry { mapId = mapId };
                for (int i = 0; i < 4; i++)
                {
                    int baseIdx = 1 + i * 5;
                    if (baseIdx >= cols.Length) break;
                    var file = cols[baseIdx].Trim();
                    if (string.IsNullOrEmpty(file)) continue;
                    entry.tracks.Add(new PcMusicTrack
                    {
                        file = file,
                        volume = baseIdx + 1 < cols.Length && int.TryParse(cols[baseIdx + 1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : 100,
                        startTime = baseIdx + 2 < cols.Length && int.TryParse(cols[baseIdx + 2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int s) ? s : 0,
                        endTime = baseIdx + 3 < cols.Length && int.TryParse(cols[baseIdx + 3].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int e) ? e : 0,
                        cycleRandom = baseIdx + 4 < cols.Length && int.TryParse(cols[baseIdx + 4].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int c) ? c : 0
                    });
                }
                if (entry.tracks.Count > 0) reg.Register(entry);
            }
            return reg;
        }
    }
}
