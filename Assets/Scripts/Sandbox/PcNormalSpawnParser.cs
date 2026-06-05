// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/normal.txt port
// Source: server settings/normal.txt (GB2312, tab-separated, 5,385 lines).
// Header columns (1-indexed):
//   1  道具名称      (item name, Chinese)
//   2  道具品质      (item quality, 0/1/4)
//   3  道具ID        (template id, 1..3879; some rows duplicate the same id)
//   4  道具种类      (item genre, -1 if absent)
//   5  具体类别      (item sub-type)
//   6  详细类别      (item detail type)
//   7  白金等级      (gold/platinum level, 0..10)
//   8  等级          (item level, 1..10)
//   9  魔法属性ID1   (magic attribute id 1)
//  10  参数1         (parameter 1)
//  11..25  magic 1 ranges/values (5 × start/end/value)
//  26  魔法属性ID2
//  27  参数2
//  28..42  magic 2 ranges/values
//  43  魔法属性ID3
//  44  参数3
//  45..59  magic 3 ranges/values
//  60  魔法属性ID4
//  61  参数4
//  62..76  magic 4 ranges/values
//  77  保底          (floor / base value)
//  78  (trailing)    (mirrors column 77 in source)
//
// This source is an item-equipment template catalog, not a monster spawn table.
// The parser extracts templateId, level, and item metadata, and stamps the
// monster-spawn fields (mapId/x/y/direction/count/respawnSec/aiMode/groupId)
// at 0 with a warning explaining the source-shape mismatch. The shape mirrors
// RareSpawnEntry / GoldBossEntry so a registry can index by template id.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using VLTK.Model;

namespace VLTK.Sandbox
{
    public static class PcNormalSpawnParser
    {
        public const int MinColumns = 8;
        public const int TotalColumns = 78;

        public const int NameCol = 0;
        public const int QualityCol = 1;
        public const int TemplateIdCol = 2;
        public const int ItemGenreCol = 3;
        public const int ItemSubTypeCol = 4;
        public const int ItemDetailCol = 5;
        public const int GoldLevelCol = 6;
        public const int LevelCol = 7;
        public const int FloorCol = 76;
        public const int TrailingCol = 77;

        public static List<SpawnPoint> ParseFile(string absolutePath, Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
                return new List<SpawnPoint>();
            return ParseLines(PcText.ReadLines(absolutePath, encoding));
        }

        public static List<SpawnPoint> ParseLines(IEnumerable<string> lines)
        {
            var result = new List<SpawnPoint>();
            if (lines == null) return result;
            bool headerSkipped = false;
            int rowIndex = 0;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < MinColumns) { rowIndex++; continue; }
                result.Add(BuildPoint(rowIndex, cols));
                rowIndex++;
            }
            return result;
        }

        public static SpawnPoint ParseRow(string[] cols)
        {
            return BuildPoint(0, cols);
        }

        public static bool IsReplacementCharPresent(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            for (int i = 0; i < text.Length; i++)
                if (text[i] == '\ufffd') return true;
            return false;
        }

        public static int ReadQuality(string[] cols) => Int(cols, QualityCol);
        public static int ReadItemGenre(string[] cols) => Int(cols, ItemGenreCol);
        public static int ReadItemSubType(string[] cols) => Int(cols, ItemSubTypeCol);
        public static int ReadItemDetail(string[] cols) => Int(cols, ItemDetailCol);
        public static int ReadGoldLevel(string[] cols) => Int(cols, GoldLevelCol);
        public static int ReadFloor(string[] cols) => Int(cols, FloorCol);
        public static int ReadTrailing(string[] cols) => Int(cols, TrailingCol);

        private static SpawnPoint BuildPoint(int rowIndex, string[] cols)
        {
            var name = Str(cols, NameCol);
            var templateId = Int(cols, TemplateIdCol);
            var point = new SpawnPoint
            {
                mapId = 0,
                npcTemplateId = templateId,
                x = 0,
                y = 0,
                direction = 0,
                count = 0,
                level = Int(cols, LevelCol),
                respawnSec = 0,
                aiMode = 0,
                groupId = 0,
                nameRaw = name,
                sourceFile = "normal.txt",
                rowIndex = rowIndex,
            };

            if (string.IsNullOrEmpty(name))
                point.warnings.Add("nameRaw is empty");
            if (IsReplacementCharPresent(name))
                point.warnings.Add("nameRaw contains U+FFFD replacement characters; re-check encoding");
            if (templateId <= 0)
                point.warnings.Add("templateId <= 0 (column " + (TemplateIdCol + 1) + " is '" + Str(cols, TemplateIdCol) + "')");

            if (cols.Length < TotalColumns)
                point.warnings.Add("row has " + cols.Length + " columns, expected " + TotalColumns);

            point.warnings.Add("normal.txt is item equipment data; mapId/x/y/direction/count/respawnSec/aiMode/groupId are not present in source and stay at 0");

            return point;
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
