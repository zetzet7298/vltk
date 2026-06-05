// -----------------------------------------------------------------------------
// VLTK Mobile — PC waypoint.txt parser
// Source: settings/waypoint.txt (225 waypoints, tab-separated, GB2312).
// Header: ID  DESC  SECT  FightState. SECT is "mapId, x, y".
// FightState: 0=any, 1=safe, 2=combat, 3=team.
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
    public static class PcWaypointParser
    {
        public const int MinColumns = 4;

        public static List<WaypointEntry> ParseFile(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
                return new List<WaypointEntry>();
            return ParseLines(ReadLines(absolutePath));
        }

        public static List<WaypointEntry> ParseLines(IEnumerable<string> lines)
        {
            var rows = new List<WaypointEntry>();
            if (lines == null) return rows;

            bool headerSkipped = false;
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.TrimEnd('\r', '\n');
                if (line.Length == 0) continue;
                var cols = line.Split('\t');
                if (!headerSkipped)
                {
                    headerSkipped = true;
                    continue;
                }
                if (cols.Length < MinColumns) continue;
                if (!int.TryParse(Str(cols, 0), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) || id <= 0)
                    continue;
                string desc = Str(cols, 1);
                if (string.IsNullOrEmpty(desc)) continue;
                string sect = Str(cols, 2);
                int mapId = 0, x = 0, y = 0;
                ParseSect(sect, out mapId, out x, out y);
                int fightState = 0;
                int.TryParse(Str(cols, 3), NumberStyles.Integer, CultureInfo.InvariantCulture, out fightState);

                rows.Add(new WaypointEntry
                {
                    waypointId = id,
                    nameRaw = desc,
                    nameNormalized = desc.Trim(),
                    mapId = mapId,
                    posX = x,
                    posY = y,
                    fightState = fightState,
                });
            }

            rows.Sort((a, b) => a.waypointId.CompareTo(b.waypointId));
            SubsystemLog.Info("PcWaypoint", $"Parsed {rows.Count} waypoint rows");
            return rows;
        }

        public static void ParseSect(string sect, out int mapId, out int x, out int y)
        {
            mapId = 0; x = 0; y = 0;
            if (string.IsNullOrEmpty(sect)) return;
            var parts = sect.Split(',');
            if (parts.Length >= 1) int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out mapId);
            if (parts.Length >= 2) int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out x);
            if (parts.Length >= 3) int.TryParse(parts[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out y);
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
