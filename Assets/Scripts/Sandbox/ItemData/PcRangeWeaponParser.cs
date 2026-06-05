// -----------------------------------------------------------------------------
// VLTK Mobile — PC rangeweapon.txt data-driven port
// Source: server/settings/item/004/rangeweapon.txt (GB2312, 46 tab-separated columns)
// Purpose: keep the PC range weapon (cung / nỏ) catalog visible to mobile
// runtime by id/name/icon and stat deltas (base + refine).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using VLTK.Model;

namespace VLTK.Sandbox.ItemData
{
    public static class PcRangeWeaponParser
    {
        public const int MinColumns = 46;
        public const int StatCount = 7;
        public const int ReqCount = 6;
        public const string EvidenceNote = "pc_item_004_rangeweapon";

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
            int itemId = itemIdHint > 0 ? itemIdHint : PcItemCommon.Int(cols, 5);
            if (PcItemCommon.IsMissing(nameRaw) && itemId <= 0) return null;
            var item = new ItemDefinition
            {
                itemId = itemId,
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
                item.warnings.Add($"RangeWeapon row id={itemId} contains Unicode replacement char");
            return item;
        }
    }
}
