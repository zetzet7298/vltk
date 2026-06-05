// -----------------------------------------------------------------------------
// VLTK Mobile — PC rare.txt port
// Source: server settings/rare.txt (GB2312). Header is ASCII (NAME, MAGIC_ID,
// MAG_P1_MIN..MAX, SWORD..CROSSBOW weapon columns, ARMOR..PENDANT armor
// columns, METAL..EARTH element columns, then 11). Body rows describe
// rare equipment-enhancement rate tiers; the schema has no map/x/y columns
// so the spawn-table fields (mapId, positionX/Y, respawnSec) stay at default
// and the magic tier id is exposed as npcTemplateId for cross-referencing.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using VLTK.Model;

namespace VLTK.Sandbox
{
    public static class PcRareSpawnParser
    {
        public const int MinColumns = 4;

        public static List<RareSpawnEntry> ParseFile(string absolutePath, Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
                return new List<RareSpawnEntry>();
            return ParseLines(PcText.ReadLines(absolutePath, encoding));
        }

        public static List<RareSpawnEntry> ParseLines(IEnumerable<string> lines)
        {
            var result = new List<RareSpawnEntry>();
            if (lines == null) return result;
            bool headerSkipped = false;
            int rowIndex = 0;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < MinColumns) { rowIndex++; continue; }
                result.Add(BuildEntry(rowIndex, cols));
                rowIndex++;
            }
            return result;
        }

        public static RareSpawnEntry ParseRow(string[] cols)
        {
            return BuildEntry(0, cols);
        }

        private static RareSpawnEntry BuildEntry(int entryId, string[] cols)
        {
            var name = Str(cols, 0);
            var magicId = Int(cols, 1);
            var levelMin = Int(cols, 2);
            var levelMax = Int(cols, 3);
            return new RareSpawnEntry
            {
                entryId = entryId,
                nameRaw = name,
                nameNormalized = name.Trim(),
                npcTemplateId = magicId,
                levelMin = levelMin,
                levelMax = levelMax,
                respawnSec = levelMax > 0 ? levelMax : 0,
                dropRateFile = "rare.txt",
            };
        }

        private static string Str(string[] c, int i) => i >= 0 && i < c.Length ? (c[i] ?? string.Empty).Trim() : string.Empty;
        private static int Int(string[] c, int i)
        {
            var s = Str(c, i);
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
                return v;
            return 0;
        }
    }
}
