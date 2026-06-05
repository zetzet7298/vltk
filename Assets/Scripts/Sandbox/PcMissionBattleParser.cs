// -----------------------------------------------------------------------------
// VLTK Mobile — PC mission/battle config parser (Tống Kim combo + scores matrices)
// Source: settings/missions/battle/combo.txt + scores.txt (GB2312).
// Format: Killer\Dead rows: Soldier, Captain, Command, Lieutenant, General
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>One battle matrix entry (one row from combo/scores tables).</summary>
    public class PcMissionBattleEntry
    {
        public string RankName { get; set; }
        public Dictionary<string, int> ComboValues { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> ScoreValues { get; set; } = new Dictionary<string, int>();
    }

    public sealed class PcMissionBattleRegistry
    {
        private readonly Dictionary<string, PcMissionBattleEntry> _byRank = new Dictionary<string, PcMissionBattleEntry>();
        public int Count => _byRank.Count;
        public PcMissionBattleEntry Get(string rank) => _byRank.TryGetValue(rank, out var v) ? v : null;
        public IEnumerable<PcMissionBattleEntry> All => _byRank.Values;
        public void Add(PcMissionBattleEntry e)
        {
            if (e != null && !string.IsNullOrEmpty(e.RankName)) _byRank[e.RankName] = e;
        }
    }

    public static class PcMissionBattleParser
    {
        private static Dictionary<string, int> ParseMatrixFile(string path)
        {
            var result = new Dictionary<string, int>();
            if (!File.Exists(path)) return result;
            var lines = PcMapListParser.ReadLines(path);
            if (lines.Count == 0) return result;
            // Parse header: "Killer\Dead\tSoldier\tCaptain\tCommand\tLieutenant\tGeneral"
            var headerLine = lines[0].Trim();
            var headerCols = headerLine.Split('\t');
            if (headerCols.Length < 2) headerCols = headerLine.Split(',');
            // Column index 0 is "Killer\Dead" label; data starts at index 1
            var colNames = new List<string>();
            for (int i = 1; i < headerCols.Length; i++)
                colNames.Add(headerCols[i].Trim());

            for (int li = 1; li < lines.Count; li++)
            {
                var raw = lines[li];
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
                var cols = line.Split('\t');
                if (cols.Length < 2) cols = line.Split(',');
                if (cols.Length < 2) continue;
                var rank = cols[0].Trim();
                for (int ci = 1; ci < cols.Length && ci - 1 < colNames.Count; ci++)
                {
                    if (int.TryParse(cols[ci].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
                    {
                        var key = rank + "|" + colNames[ci - 1];
                        result[key] = v;
                    }
                }
            }
            return result;
        }

        public static PcMissionBattleRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcMissionBattleRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;

            var comboMap = ParseMatrixFile(Path.Combine(absoluteDir, "combo.txt"));
            var scoreMap = ParseMatrixFile(Path.Combine(absoluteDir, "scores.txt"));

            // Build entries keyed by rank name
            var ranks = new HashSet<string>();
            foreach (var k in comboMap.Keys)
            {
                var rank = k.Split('|')[0];
                ranks.Add(rank);
            }
            foreach (var k in scoreMap.Keys)
            {
                var rank = k.Split('|')[0];
                ranks.Add(rank);
            }

            foreach (var rank in ranks)
            {
                var entry = new PcMissionBattleEntry { RankName = rank };
                foreach (var kvp in comboMap)
                {
                    var parts = kvp.Key.Split('|');
                    if (parts.Length == 2 && parts[0] == rank)
                        entry.ComboValues[parts[1]] = kvp.Value;
                }
                foreach (var kvp in scoreMap)
                {
                    var parts = kvp.Key.Split('|');
                    if (parts.Length == 2 && parts[0] == rank)
                        entry.ScoreValues[parts[1]] = kvp.Value;
                }
                reg.Add(entry);
            }
            return reg;
        }
    }
}
