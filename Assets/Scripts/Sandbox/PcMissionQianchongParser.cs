// -----------------------------------------------------------------------------
// VLTK Mobile — PC mission/qianchonglou config parser (Vạn Trọng Lâu tracks)
// Source: settings/missions/qianchonglou/playerpos.txt + track_1..6.txt (GB2312).
// Format: TRAPX, TRAPY (tab-separated). Header row skipped.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>Position coordinate.</summary>
    public class QianchongPos
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    /// <summary>One track entry: player start position + track waypoints.</summary>
    public class PcMissionQianchongEntry
    {
        public int TrackId { get; set; }
        public QianchongPos PlayerPos { get; set; }
        public List<QianchongPos> Positions { get; set; } = new List<QianchongPos>();
    }

    public sealed class PcMissionQianchongRegistry
    {
        private readonly Dictionary<int, PcMissionQianchongEntry> _byId = new Dictionary<int, PcMissionQianchongEntry>();
        public int Count => _byId.Count;
        public PcMissionQianchongEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcMissionQianchongEntry> All => _byId.Values;
        public void Add(PcMissionQianchongEntry e) { if (e != null) _byId[e.TrackId] = e; }
    }

    public static class PcMissionQianchongParser
    {
        private static QianchongPos ParseSinglePos(string path)
        {
            if (!File.Exists(path)) return null;
            var lines = PcMapListParser.ReadLines(path);
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
                if (!int.TryParse(line.Split('\t')[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                    continue;
                var cols = line.Split('\t');
                if (cols.Length < 2) cols = line.Split(',');
                if (cols.Length < 2) continue;
                if (!int.TryParse(cols[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int x)) continue;
                if (!int.TryParse(cols[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int y)) continue;
                return new QianchongPos { X = x, Y = y };
            }
            return null;
        }

        private static List<QianchongPos> ParseMultiPos(string path)
        {
            var result = new List<QianchongPos>();
            if (!File.Exists(path)) return result;
            var lines = PcMapListParser.ReadLines(path);
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
                if (!int.TryParse(line.Split('\t')[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                    continue;
                var cols = line.Split('\t');
                if (cols.Length < 2) cols = line.Split(',');
                if (cols.Length < 2) continue;
                if (!int.TryParse(cols[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int x)) continue;
                if (!int.TryParse(cols[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int y)) continue;
                result.Add(new QianchongPos { X = x, Y = y });
            }
            return result;
        }

        public static PcMissionQianchongRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcMissionQianchongRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;

            var playerPos = ParseSinglePos(Path.Combine(absoluteDir, "playerpos.txt"));

            for (int trackId = 1; trackId <= 6; trackId++)
            {
                var trackPath = Path.Combine(absoluteDir, $"track_{trackId}.txt");
                if (!File.Exists(trackPath)) continue;
                reg.Add(new PcMissionQianchongEntry
                {
                    TrackId = trackId,
                    PlayerPos = playerPos,
                    Positions = ParseMultiPos(trackPath)
                });
            }
            return reg;
        }
    }
}
