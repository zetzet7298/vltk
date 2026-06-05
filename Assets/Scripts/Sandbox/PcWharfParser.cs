// -----------------------------------------------------------------------------
// VLTK Mobile — PC wharf.txt parser
// Source: settings/wharf.txt (boat/wharf stations, tab-separated, GB2312).
// Header: ID  DESC  COUNT  SECT1  SECT2  SECT3  SECT4. SECTn is "mapId, x, y".
// We use the first SECT as the primary position; additional SECTs are kept as
// the sectCount for multi-stop routes.
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
    public static class PcWharfParser
    {
        public const int MinColumns = 4;

        public static List<WharfEntry> ParseFile(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
                return new List<WharfEntry>();
            return ParseLines(ReadLines(absolutePath));
        }

        public static List<WharfEntry> ParseLines(IEnumerable<string> lines)
        {
            var rows = new List<WharfEntry>();
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
                    continue;
                }
                if (!int.TryParse(Str(cols, 0), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) || id <= 0)
                    continue;
                string desc = Str(cols, 1);
                if (string.IsNullOrEmpty(desc)) continue;
                int count = 0;
                int.TryParse(Str(cols, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out count);
                int mapId = 0, posX = 0, posY = 0;
                int sectCols = cols.Length - 3;
                int sectCount = sectCols < count ? sectCols : count;
                if (sectCount < 0) sectCount = 0;
                if (sectCount > 0)
                {
                    PcWaypointParser.ParseSect(Str(cols, 3), out mapId, out posX, out posY);
                }

                rows.Add(new WharfEntry
                {
                    wharfId = id,
                    nameRaw = desc,
                    nameNormalized = desc.Trim(),
                    mapId = mapId,
                    posX = posX,
                    posY = posY,
                    price = 0,
                    sectCount = sectCount,
                });
            }

            rows.Sort((a, b) => a.wharfId.CompareTo(b.wharfId));
            SubsystemLog.Info("PcWharf", $"Parsed {rows.Count} wharf rows");
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
