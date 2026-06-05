// -----------------------------------------------------------------------------
// VLTK Mobile — PC scroll.txt parser
// Source: settings/scroll.txt (id/value table, GB2312). First non-empty row is
// the header label "品阶" (grade); subsequent rows are `id<TAB>value`. Optional
// 4-column form `id<TAB>desc<TAB>sect<TAB>fightState` is also accepted for
// parity with waypoint.txt when present.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    public static class PcScrollParser
    {
        public const int MinColumns = 2;

        public static List<ScrollEntry> ParseFile(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
                return new List<ScrollEntry>();
            return ParseLines(ReadLines(absolutePath));
        }

        public static List<ScrollEntry> ParseLines(IEnumerable<string> lines)
        {
            var rows = new List<ScrollEntry>();
            if (lines == null) return rows;
            bool headerSkipped = false;
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.TrimEnd('\r', '\n');
                if (line.Length == 0) continue;
                var cols = line.Split('\t');
                if (cols.Length < MinColumns) continue;
                if (!headerSkipped)
                {
                    headerSkipped = true;
                    if (!int.TryParse(Str(cols, 0), NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                        continue;
                }
                if (!int.TryParse(Str(cols, 0), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) || id <= 0)
                    continue;
                int value = 0;
                int.TryParse(Str(cols, 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
                string name = string.Empty;
                int mapId = 0;
                int fightState = 0;
                if (cols.Length >= 4)
                {
                    name = Str(cols, 1);
                    PcWaypointParser.ParseSect(Str(cols, 2), out mapId, out _, out _);
                    int.TryParse(Str(cols, 3), NumberStyles.Integer, CultureInfo.InvariantCulture, out fightState);
                }

                rows.Add(new ScrollEntry
                {
                    scrollId = id,
                    nameRaw = name,
                    nameNormalized = name.Trim(),
                    mapId = mapId,
                    value = value,
                    fightState = fightState,
                });
            }

            rows.Sort((a, b) => a.scrollId.CompareTo(b.scrollId));
            SubsystemLog.Info("PcScroll", $"Parsed {rows.Count} scroll rows");
            return rows;
        }

        private static string Str(string[] cols, int i)
        {
            return i >= 0 && i < cols.Length ? (cols[i] ?? string.Empty).Trim() : string.Empty;
        }

        public static string[] ReadLines(string absolutePath)
        {
            var bytes = File.ReadAllBytes(absolutePath);
            string text;
            try
            {
                text = Encoding.GetEncoding("GB2312").GetString(bytes);
            }
            catch
            {
                text = Encoding.UTF8.GetString(bytes);
            }
            if (text.Length > 0 && text[0] == '\ufeff') text = text.Substring(1);
            return text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        }
    }
}
