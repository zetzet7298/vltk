// -----------------------------------------------------------------------------
// VLTK Mobile — PC mission/arena config parser (arena battle/ready positions)
// Source: settings/missions/arena/battlepos.txt + readypos.txt (GB2312).
// Mỗi dòng: TRAPX, TRAPY (tab-separated). Header row skipped.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>Position on arena map (X, Y).</summary>
    public class ArenaPos
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    /// <summary>One arena config entry: battle and ready position lists.</summary>
    public class PcMissionArenaEntry
    {
        public int ArenaId { get; set; }
        public List<ArenaPos> BattlePositions { get; set; } = new List<ArenaPos>();
        public List<ArenaPos> ReadyPositions { get; set; } = new List<ArenaPos>();
    }

    public sealed class PcMissionArenaRegistry
    {
        private readonly Dictionary<int, PcMissionArenaEntry> _byId = new Dictionary<int, PcMissionArenaEntry>();
        public int Count => _byId.Count;
        public PcMissionArenaEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcMissionArenaEntry> All => _byId.Values;
        public void Add(PcMissionArenaEntry e) { if (e != null) _byId[e.ArenaId] = e; }
    }

    public static class PcMissionArenaParser
    {
        private static List<ArenaPos> ParsePosFile(string path)
        {
            var result = new List<ArenaPos>();
            if (!File.Exists(path)) return result;
            var lines = PcMapListParser.ReadLines(path);
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
                // Skip header row
                if (!int.TryParse(line.Split('\t')[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                    continue;
                var cols = line.Split('\t');
                if (cols.Length < 2) cols = line.Split(',');
                if (cols.Length < 2) continue;
                if (!int.TryParse(cols[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int x)) continue;
                if (!int.TryParse(cols[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int y)) continue;
                result.Add(new ArenaPos { X = x, Y = y });
            }
            return result;
        }

        public static PcMissionArenaRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcMissionArenaRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;

            var entry = new PcMissionArenaEntry { ArenaId = 1 };
            entry.BattlePositions = ParsePosFile(Path.Combine(absoluteDir, "battlepos.txt"));
            entry.ReadyPositions = ParsePosFile(Path.Combine(absoluteDir, "readypos.txt"));
            reg.Add(entry);
            return reg;
        }
    }
}
