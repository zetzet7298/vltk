// -----------------------------------------------------------------------------
// VLTK Mobile — PC potion.txt data-driven port
// Source: server/settings/item/004/potion.txt (GB2312, 28 tab-separated columns)
// Purpose: keep the PC potion (thuốc) catalog visible to mobile runtime by
// id/name/icon and per-effect deltas. Potion uses a 5-effect × 3-field (type,
// value, time) layout instead of the 7-stat + 6-requirement layout used by the
// 11 equip files.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using VLTK.Model;

namespace VLTK.Sandbox.ItemData
{
    public static class PcPotionParser
    {
        public const int MinColumns = 28;
        public const int DrugCount = 5;
        public const string EvidenceNote = "pc_item_004_potion";

        public static List<ItemDefinition> ParseFile(string path)
        {
            var rows = new List<ItemDefinition>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
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
                var item = ParseRow(cols, rowIndex);
                if (item != null) rows.Add(item);
            }
            return rows;
        }

        public static ItemDefinition ParseRow(string[] cols, int itemIdHint = 0)
        {
            if (cols == null || cols.Length < MinColumns) return null;
            string nameRaw = PcItemCommon.Str(cols, 0);
            if (PcItemCommon.IsMissing(nameRaw) && itemIdHint <= 0) return null;
            int itemId = itemIdHint > 0 ? itemIdHint : PcItemCommon.Int(cols, 5);

            var item = new ItemDefinition
            {
                itemId = itemId,
                nameRaw = nameRaw,
                nameNormalized = nameRaw,
                setId = 0,
                refineLevel = 0,
                iconSourceId = PcItemCommon.BuildIconSourceId(PcItemCommon.Str(cols, 4), EvidenceNote),
                iconResolved = false,
            };
            for (int i = 0; i < DrugCount; i++)
            {
                int typeCol = 13 + i * 3;
                int valCol = typeCol + 1;
                int timeCol = typeCol + 2;
                var type = PcItemCommon.Str(cols, typeCol);
                if (PcItemCommon.IsMissing(type)) continue;
                int value = PcItemCommon.Int(cols, valCol);
                int time = PcItemCommon.Int(cols, timeCol);
                if (value == 0 && time == 0) continue;
                int attrCode = PcItemCommon.Int(cols, typeCol);
                item.statDeltas.Add(new ItemStatDelta
                {
                    ruleId = $"STAT_BASE_{type}",
                    stage = ItemStatStage.Base,
                    attrCode = attrCode,
                    value = value,
                });
                if (time > 0)
                {
                    item.warnings.Add($"Potion row id={itemId} drug{i + 1} has duration {time} (cycles)");
                }
            }
            if (PcItemCommon.ContainsReplacementChar(nameRaw))
                item.warnings.Add($"Potion row id={itemId} contains Unicode replacement char");
            return item;
        }
    }
}
