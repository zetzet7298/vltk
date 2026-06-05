// -----------------------------------------------------------------------------
// VLTK Mobile — PC goldequip.txt data-driven port
// Source: server/settings/item/004/goldequip.txt (GB2312, 62 tab-separated columns)
// Purpose: parse the gold equipment (Hoàng Kim) catalog — 5,346 items covering
// all equipment types with 7 base stats, 6 req attributes, 6 magic attribute
// indices, set bonus, and extended fields (star upgrade, socket slots, etc.).
// Follows the same pattern as PcArmorParser but reads the full goldequip schema.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using VLTK.Model;

namespace VLTK.Sandbox.ItemData
{
    public static class PcGoldEquipParser
    {
        public const int MinColumns = 46;
        public const int StatCount = 7;
        public const int ReqCount = 6;
        public const int MagicCount = 6;
        public const int ExtendedColumns = 62;
        public const string EvidenceNote = "pc_item_004_goldequip";

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

            // col[1] = ItemGenre, col[2] = DetailType, col[3] = ParticularType
            // col[4] = spr path, col[5] = item index, col[8] = description
            // col[9] = ngũ hành element, col[10] = price, col[11] = level
            int element = PcItemCommon.Int(cols, 9);
            int price = PcItemCommon.Int(cols, 10);
            int level = PcItemCommon.Int(cols, 11);
            int detailType = PcItemCommon.Int(cols, 2);

            var item = new ItemDefinition
            {
                itemId = itemId,
                nameRaw = nameRaw,
                nameNormalized = nameRaw,
                setId = ParseSetId(cols),
                refineLevel = PcItemCommon.EstimateRefineLevel(cols, 14, StatCount),
                iconSourceId = PcItemCommon.BuildIconSourceId(PcItemCommon.Str(cols, 4), EvidenceNote),
                iconResolved = false,
            };

            // Base stats (cols 13-33: 7 × {type, min, max})
            var baseDeltas = PcItemCommon.BuildStatDeltas(cols, 13, 34, StatCount, ReqCount, item.refineLevel);
            foreach (var d in baseDeltas) item.statDeltas.Add(d);

            // Magic attribute indices (cols 46-51: 6 magic slots)
            for (int i = 0; i < MagicCount; i++)
            {
                int magicIdx = PcItemCommon.Int(cols, 46 + i);
                if (magicIdx <= 0) continue;
                item.statDeltas.Add(new ItemStatDelta
                {
                    ruleId = $"MAGIC_IDX_{i}",
                    stage = ItemStatStage.MagicIndex,
                    attrCode = magicIdx,
                    value = magicIdx,
                });
            }

            // Extended columns if available
            if (cols.Length >= ExtendedColumns)
            {
                // col[57] = 可熔炼属性数量 (forgeable count)
                // col[58] = 可熔炼纹钢品质 (forgeable quality)
                // col[59] = 是否可升星装备 (star-upgradable)
                // col[60] = 可镶嵌星辰石数量 (socket count)
                // col[61] = 装备突破祝福值 (breakthrough blessing)
                int starUpgradable = PcItemCommon.Int(cols, 59);
                if (starUpgradable > 0)
                {
                    item.statDeltas.Add(new ItemStatDelta
                    {
                        ruleId = "GOLD_STAR_UPGRADABLE",
                        stage = ItemStatStage.Base,
                        attrCode = 9001,
                        value = starUpgradable,
                    });
                }

                int socketCount = PcItemCommon.Int(cols, 60);
                if (socketCount > 0)
                {
                    item.statDeltas.Add(new ItemStatDelta
                    {
                        ruleId = "GOLD_SOCKET_COUNT",
                        stage = ItemStatStage.Base,
                        attrCode = 9002,
                        value = socketCount,
                    });
                }
            }

            if (PcItemCommon.ContainsReplacementChar(nameRaw))
                item.warnings.Add($"GoldEquip row id={itemId} contains Unicode replacement char");

            return item;
        }

        /// <summary>col[52] = 所在套装 (set id)</summary>
        private static int ParseSetId(string[] cols)
        {
            if (cols.Length <= 52) return 0;
            return PcItemCommon.Int(cols, 52);
        }
    }
}
