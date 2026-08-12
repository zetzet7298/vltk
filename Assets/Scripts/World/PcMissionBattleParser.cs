using System;
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
    public sealed class PcMissionBattleMatrix
    {
        public string SourcePath { get; set; }
        public string RowHeader { get; set; }
        public List<string> Headers { get; } = new List<string>();
        public Dictionary<string, int> Values { get; } = new Dictionary<string, int>();
        public int RowCount { get; set; }
        public int CellCount => Values.Count;
    }
    public sealed class PcMissionBattleRegistry
    {
        private readonly Dictionary<string, PcMissionBattleEntry> _byRank = new Dictionary<string, PcMissionBattleEntry>(StringComparer.Ordinal);
        public int Count => _byRank.Count;
        public int ComboCellCount { get; set; }
        public int ScoreCellCount { get; set; }
        public string ComboRowHeader { get; set; }
        public string ScoreRowHeader { get; set; }
        public IReadOnlyList<string> ComboHeaders { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> ScoreHeaders { get; set; } = Array.Empty<string>();
        public PcMissionBattleEntry Get(string rank) => _byRank.TryGetValue(rank, out var v) ? v : null;
        public IEnumerable<PcMissionBattleEntry> All => _byRank.Values;
        public void Add(PcMissionBattleEntry e)
        {
            if (e != null && !string.IsNullOrEmpty(e.RankName)) _byRank[e.RankName] = e;
        }
    }

    public static class PcMissionBattleParser
    {
        public const string ComboFileName = "combo.txt";
        public const string ScoresFileName = "scores.txt";
        public const string ExpectedRowHeader = "Killer\\Dead";

        public static PcMissionBattleMatrix ParseMatrixFile(string path)
        {
            var result = new PcMissionBattleMatrix { SourcePath = path };
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return result;

            var lines = PcMapListParser.ReadLines(path);
            if (lines.Length == 0) return result;

            var headerCols = SplitColumns(lines[0]);
            if (headerCols.Length == 0) return result;

            result.RowHeader = headerCols[0].Trim();
            for (int i = 1; i < headerCols.Length; i++)
            {
                var header = headerCols[i].Trim();
                if (!string.IsNullOrEmpty(header)) result.Headers.Add(header);
            }

            for (int li = 1; li < lines.Length; li++)
            {
                var raw = lines[li];
                if (string.IsNullOrWhiteSpace(raw)) continue;

                var line = raw.Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;

                var cols = SplitColumns(line);
                if (cols.Length < 2) continue;

                var rank = cols[0].Trim();
                if (string.IsNullOrEmpty(rank)) continue;

                result.RowCount++;
                for (int ci = 1; ci < cols.Length && ci - 1 < result.Headers.Count; ci++)
                {
                    if (int.TryParse(cols[ci].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
                    {
                        result.Values[MakeKey(rank, result.Headers[ci - 1])] = v;
                    }
                }
            }

            return result;
        }

        public static PcMissionBattleRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcMissionBattleRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;

            var combo = ParseMatrixFile(Path.Combine(absoluteDir, ComboFileName));
            var scores = ParseMatrixFile(Path.Combine(absoluteDir, ScoresFileName));

            reg.ComboCellCount = combo.CellCount;
            reg.ScoreCellCount = scores.CellCount;
            reg.ComboRowHeader = combo.RowHeader;
            reg.ScoreRowHeader = scores.RowHeader;
            reg.ComboHeaders = combo.Headers.AsReadOnly();
            reg.ScoreHeaders = scores.Headers.AsReadOnly();

            var ranks = new HashSet<string>(StringComparer.Ordinal);
            AddRanks(ranks, combo.Values.Keys);
            AddRanks(ranks, scores.Values.Keys);

            foreach (var rank in ranks)
            {
                var entry = new PcMissionBattleEntry { RankName = rank };
                AddValuesForRank(entry.ComboValues, rank, combo.Values);
                AddValuesForRank(entry.ScoreValues, rank, scores.Values);
                reg.Add(entry);
            }

            return reg;
        }

        private static string[] SplitColumns(string line)
        {
            var cols = line.Split('\t');
            return cols.Length > 1 ? cols : line.Split(',');
        }
        private static string MakeKey(string killerRank, string deadRank) => killerRank + "|" + deadRank;
        private static void AddRanks(HashSet<string> ranks, IEnumerable<string> keys)
        {
            foreach (var key in keys)
            {
                var sep = key.IndexOf('|');
                if (sep > 0) ranks.Add(key.Substring(0, sep));
            }
        }
        private static void AddValuesForRank(Dictionary<string, int> target, string rank, Dictionary<string, int> source)
        {
            foreach (var kvp in source)
            {
                var sep = kvp.Key.IndexOf('|');
                if (sep <= 0) continue;
                if (string.Equals(kvp.Key.Substring(0, sep), rank, StringComparison.Ordinal))
                    target[kvp.Key.Substring(sep + 1)] = kvp.Value;
            }
        }
    }
}
