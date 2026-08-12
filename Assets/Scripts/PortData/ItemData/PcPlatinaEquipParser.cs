// -----------------------------------------------------------------------------
// VLTK Mobile — PC platinaequip.txt data-driven port
// Source: server/settings/item/004/platinaequip.txt (GB2312, 70 tab-separated columns)
// Purpose: parse the platina equipment (Bạch Kim) catalog — 5,336 items.
// Platina items share the same base schema as gold equip (46 cols) but have
// extended columns for additional stats (up to 10 base stats), more magic
// indices, and platina-specific fields (突破/breakthrough, 品质/quality tiers).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using VLTK.Model;

namespace VLTK.Sandbox.ItemData
{
    public static class PcPlatinaEquipParser
    {
        public const int MinColumns = 46;
        public const int StatCount = 7;
        public const int ReqCount = 6;
        public const int MagicCount = 6;
        public const int ExtendedColumns = 70;
        public const string EvidenceNote = "pc_item_004_platinaequip";

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

            int element = PcItemCommon.Int(cols, 9);
            int price = PcItemCommon.Int(cols, 10);
            int level = PcItemCommon.Int(cols, 11);

            var item = new ItemDefinition
            {
                itemId = itemId,
                resId = PcItemCommon.Int(cols, 5),
                itemGenre = PcItemCommon.Int(cols, 1),
                detailType = PcItemCommon.Int(cols, 2),
                particularType = PcItemCommon.Int(cols, 3),
                nameRaw = nameRaw,
                nameNormalized = nameRaw,
                setId = ParseSetId(cols),
                refineLevel = PcItemCommon.EstimateRefineLevel(cols, 14, StatCount),
                iconSourceId = PcItemCommon.BuildIconSourceId(PcItemCommon.Str(cols, 4), EvidenceNote),
                iconResolved = false,
            };

            // Base stats + req (same layout as gold equip, cols 13-45)
            var baseDeltas = PcItemCommon.BuildStatDeltas(cols, 13, 34, StatCount, ReqCount, item.refineLevel);
            foreach (var d in baseDeltas) item.statDeltas.Add(d);

            // Magic attribute indices (cols 46-51)
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

            // Extended columns (52-69) — platina-specific
            if (cols.Length > 52)
            {
                // Additional magic indices or extended stats if cols > 61
                // col[52] = 所在套装 (set id, already parsed above)
                // col[53-56] = 扩展套装 related
                // col[57] = 可熔炼属性数量
                // col[58] = 可熔炼纹钢品质
                // col[59] = 是否可升星装备
                // col[60] = 可镶嵌星辰石数量
                // col[61] = 装备突破祝福值

                int starUpgradable = PcItemCommon.Int(cols, 59);
                if (starUpgradable > 0)
                {
                    item.statDeltas.Add(new ItemStatDelta
                    {
                        ruleId = "PLATINA_STAR_UPGRADABLE",
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
                        ruleId = "PLATINA_SOCKET_COUNT",
                        stage = ItemStatStage.Base,
                        attrCode = 9002,
                        value = socketCount,
                    });
                }
            }

            if (PcItemCommon.ContainsReplacementChar(nameRaw))
                item.warnings.Add($"PlatinaEquip row id={itemId} contains Unicode replacement char");

            return item;
        }

        private static int ParseSetId(string[] cols)
        {
            if (cols.Length <= 52) return 0;
            return PcItemCommon.Int(cols, 52);
        }
    }
}
