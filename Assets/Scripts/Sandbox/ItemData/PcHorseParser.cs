// -----------------------------------------------------------------------------
// VLTK Mobile — PC horse.txt data-driven port
// Source: server/settings/item/004/horse.txt (GB2312, 46 tab-separated columns)
// Source: server/settings/item/004/horseres.txt — maps horse_id -> 马匹部件编号 (SPR variant)
//
// PC verified: ItemGenre=0, DetailType=10, resId=col[5] (same for all horses = 40)
// horseres.txt maps horse row index -> horse SPR part variant (HH/HB/HT variant number)
//   e.g. variant=10 (普通黄马), variant=6 (青马), variant=9 (白马), etc.
// The variant is stored in item.resId so PlayerEquipmentService can read it.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using VLTK.Model;

namespace VLTK.Sandbox.ItemData
{
    public static class PcHorseParser
    {
        public const int MinColumns = 46;
        public const int StatCount = 7;
        public const int ReqCount = 6;
        public const string EvidenceNote = "pc_item_004_horse";

        /// <summary>
        /// Load horseres.txt: maps horse row index (1-based) → 马匹部件编号 (horse SPR variant).
        /// horseres.txt columns: 马匹编号 | 马匹部件编号 | 说明
        /// </summary>
        public static Dictionary<int, int> LoadHorseResMap(string horseresTxtPath)
        {
            var map = new Dictionary<int, int>();
            if (string.IsNullOrEmpty(horseresTxtPath) || !File.Exists(horseresTxtPath))
                return map;
            var lines = PcItemCommon.ReadServerLines(horseresTxtPath);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                if (!int.TryParse(cols[0].Trim(), out int horseId)) continue;
                if (!int.TryParse(cols[1].Trim(), out int variant)) continue;
                if (variant > 0) // variant 0 = no mount
                    map[horseId] = variant;
            }
            return map;
        }

        public static List<ItemDefinition> ParseFile(string path)
        {
            var rows = new List<ItemDefinition>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;

            // Try loading horseres.txt from same directory
            string dir = Path.GetDirectoryName(path) ?? string.Empty;
            string horseresPath = Path.Combine(dir, "horseres.txt");
            var horseResMap = LoadHorseResMap(horseresPath);

            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            int rowIndex = 0;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < MinColumns) continue;
                rowIndex++;
                var item = ParseRow(cols, rowIndex, horseResMap);
                if (item != null) rows.Add(item);
            }
            return rows;
        }

        public static ItemDefinition ParseRow(string[] cols, int itemIdHint = 0, Dictionary<int, int> horseResMap = null)
        {
            if (cols == null || cols.Length < MinColumns) return null;
            string nameRaw = PcItemCommon.Str(cols, 0);
            int itemId = itemIdHint > 0 ? itemIdHint : PcItemCommon.Int(cols, 5);
            if (PcItemCommon.IsMissing(nameRaw) && itemId <= 0) return null;

            // Look up horse SPR variant from horseres.txt map (row index = itemIdHint)
            // If not found, default to variant 16 (普通黄马 = MA_HH/HB/HT_016)
            int horseSprVariant = 16; // default: 普通黄马
            if (horseResMap != null && itemIdHint > 0 && horseResMap.TryGetValue(itemIdHint, out int mappedVariant))
                horseSprVariant = mappedVariant;

            var item = new ItemDefinition
            {
                itemId = itemId,
                resId = horseSprVariant,              // resId = horse SPR variant for player visual
                itemGenre = PcItemCommon.Int(cols, 1), // Col 1: ItemGenre (horse.txt=0)
                detailType = PcItemCommon.Int(cols, 2), // Col 2: DetailType (horse.txt=10)
                particularType = PcItemCommon.Int(cols, 3), // Col 3: ParticularType
                nameRaw = nameRaw,
                nameNormalized = nameRaw,
                setId = 0,
                refineLevel = PcItemCommon.EstimateRefineLevel(cols, 14, StatCount),
                iconSourceId = PcItemCommon.BuildIconSourceId(PcItemCommon.Str(cols, 4), EvidenceNote),
                iconResolved = false,
            };
            var deltas = PcItemCommon.BuildStatDeltas(cols, 13, 34, StatCount, ReqCount, item.refineLevel);
            foreach (var d in deltas) item.statDeltas.Add(d);
            if (PcItemCommon.ContainsReplacementChar(nameRaw))
                item.warnings.Add($"Horse row id={itemId} contains Unicode replacement char");
            return item;
        }
    }
}
