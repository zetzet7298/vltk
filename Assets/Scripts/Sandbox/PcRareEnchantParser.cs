// -----------------------------------------------------------------------------
// VLTK Mobile — PC rare.txt (Bảng cường hóa thuộc tính hiếm / weapon-enchant) parser
//
// PC source (verified 2026-06-12, GB2312, TAB-separated, 1 header + 480 rows):
//   /var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/settings/rare.txt
//   /var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/Server/itemexchange_setting/rare.txt
//   (both files are byte-identical)
//
// This is a RARE MAGIC-ATTRIBUTE ROLL TABLE used by the equipment enchant /
// item-exchange system — NOT an NPC spawn table. Each row describes one tier of
// one magic attribute (e.g. 加伤害强化 = damage enhancement, 偷血 = lifesteal,
// 免疫眩晕状态 = stun immunity) and the relative weight with which it can roll
// onto each weapon type, equipment slot, and element.
//
// Real header (29 columns):
//   0  NAME         tên thuộc tính ma thuật (Trung văn, cần Việt hoá ở tầng UI)
//   1  MAGIC_ID     id magic-attribute (khớp magicattrib*.txt)
//   2  MAG_P1_MIN   giá trị tham số 1 tối thiểu của tier này
//   3  MAG_P1_MAX   giá trị tham số 1 tối đa của tier này
//   4  SWORD        trọng số roll lên Kiếm
//   5  BLADE        trọng số roll lên Đao
//   6  WAND         trọng số roll lên Bổng/Gậy
//   7  SPEAR        trọng số roll lên Thương
//   8  HAMMER       trọng số roll lên Chùy/Búa
//   9  DUALBLADES   trọng số roll lên Song đao/Song kiếm
//   10 DARTS        trọng số roll lên Tiêu/Ám khí
//   11 KNIFE        trọng số roll lên Chủy thủ/Đoản đao
//   12 CROSSBOW     trọng số roll lên Nỏ
//   13 ARMOR        trọng số roll lên Giáp
//   14 RING         trọng số roll lên Nhẫn
//   15 NECKLACE     trọng số roll lên Dây chuyền
//   16 AMULET       trọng số roll lên Bùa
//   17 BOOT         trọng số roll lên Giày
//   18 BELT         trọng số roll lên Đai lưng
//   19 HELM         trọng số roll lên Mũ
//   20 CUFF         trọng số roll lên Tay (hộ uyển)
//   21 SACHET       trọng số roll lên Túi thơm
//   22 PENDANT      trọng số roll lên Mặt dây chuyền
//   23 METAL        trọng số roll theo hệ Kim
//   24 WOOD         trọng số roll theo hệ Mộc
//   25 WATER        trọng số roll theo hệ Thủy
//   26 FIRE         trọng số roll theo hệ Hỏa
//   27 EARTH        trọng số roll theo hệ Thổ
//   28 "11"         cột cuối (toàn 0 trong dữ liệu thực) — giữ raw, không suy diễn
//
// Provenance: columns are copied verbatim from the PC header. No columns are
// invented; the trailing "11" column is preserved as-is (rawTrailing) because
// every observed value is 0 and the PC engine semantics for it are not exposed
// in the available source.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Một dòng của bảng cường hóa thuộc tính hiếm (PC rare.txt).
    /// Mỗi dòng = một tier của một thuộc tính ma thuật + trọng số roll lên từng
    /// loại vũ khí / vị trí trang bị / hệ ngũ hành.
    /// </summary>
    [System.Serializable]
    public class PcRareEnchantEntry
    {
        public string nameRaw;     // col 0  — tên thuộc tính (Trung văn)
        public int magicId;        // col 1  — MAGIC_ID
        public int magP1Min;       // col 2  — MAG_P1_MIN
        public int magP1Max;       // col 3  — MAG_P1_MAX

        // --- Trọng số roll theo loại vũ khí (col 4..12) ---
        public int wSword;         // col 4
        public int wBlade;         // col 5
        public int wWand;          // col 6
        public int wSpear;         // col 7
        public int wHammer;        // col 8
        public int wDualBlades;    // col 9
        public int wDarts;         // col 10
        public int wKnife;         // col 11
        public int wCrossbow;      // col 12

        // --- Trọng số roll theo vị trí trang bị (col 13..22) ---
        public int wArmor;         // col 13
        public int wRing;          // col 14
        public int wNecklace;      // col 15
        public int wAmulet;        // col 16
        public int wBoot;          // col 17
        public int wBelt;          // col 18
        public int wHelm;          // col 19
        public int wCuff;          // col 20
        public int wSachet;        // col 21
        public int wPendant;       // col 22

        // --- Trọng số roll theo hệ ngũ hành (col 23..27) ---
        public int wMetal;         // col 23
        public int wWood;          // col 24
        public int wWater;         // col 25
        public int wFire;          // col 26
        public int wEarth;         // col 27

        public int rawTrailing;    // col 28 ("11" header) — giữ raw, toàn 0 trong dữ liệu thực
    }

    /// <summary>
    /// Bảng cường hóa thuộc tính hiếm. Index theo magicId (nhiều tier / 1 magicId)
    /// và giữ toàn bộ dòng theo thứ tự file.
    /// </summary>
    public sealed class PcRareEnchantTable
    {
        private readonly List<PcRareEnchantEntry> _rows = new();
        private readonly Dictionary<int, List<PcRareEnchantEntry>> _byMagicId = new();

        public int Count => _rows.Count;
        public IReadOnlyList<PcRareEnchantEntry> All => _rows;

        public void Add(PcRareEnchantEntry e)
        {
            if (e == null) return;
            _rows.Add(e);
            if (!_byMagicId.TryGetValue(e.magicId, out var list))
            {
                list = new List<PcRareEnchantEntry>();
                _byMagicId[e.magicId] = list;
            }
            list.Add(e);
        }

        /// <summary>Tất cả tier của một MAGIC_ID (rỗng nếu không có).</summary>
        public List<PcRareEnchantEntry> GetByMagicId(int magicId)
            => _byMagicId.TryGetValue(magicId, out var v)
                ? new List<PcRareEnchantEntry>(v)
                : new List<PcRareEnchantEntry>();

        public int MagicIdCount => _byMagicId.Count;
    }

    /// <summary>
    /// Parser cho PC rare.txt — bảng cường hóa thuộc tính hiếm (29 cột).
    /// </summary>
    public static class PcRareEnchantParser
    {
        // Chỉ số cột theo header PC (0-indexed).
        public const int NameCol = 0;
        public const int MagicIdCol = 1;
        public const int MagP1MinCol = 2;
        public const int MagP1MaxCol = 3;
        public const int FirstWeightCol = 4;   // SWORD
        public const int LastWeightCol = 27;   // EARTH
        public const int TrailingCol = 28;     // "11"
        public const int ColumnCount = 29;

        public static List<PcRareEnchantEntry> ParseFile(string path)
        {
            var rows = new List<PcRareEnchantEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;

            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; } // bỏ dòng header
                var cols = line.Split('\t');
                if (cols.Length < ColumnCount) continue;

                rows.Add(new PcRareEnchantEntry
                {
                    nameRaw     = PcItemCommon.Str(cols, NameCol),
                    magicId     = PcItemCommon.Int(cols, MagicIdCol),
                    magP1Min    = PcItemCommon.Int(cols, MagP1MinCol),
                    magP1Max    = PcItemCommon.Int(cols, MagP1MaxCol),

                    wSword      = PcItemCommon.Int(cols, 4),
                    wBlade      = PcItemCommon.Int(cols, 5),
                    wWand       = PcItemCommon.Int(cols, 6),
                    wSpear      = PcItemCommon.Int(cols, 7),
                    wHammer     = PcItemCommon.Int(cols, 8),
                    wDualBlades = PcItemCommon.Int(cols, 9),
                    wDarts      = PcItemCommon.Int(cols, 10),
                    wKnife      = PcItemCommon.Int(cols, 11),
                    wCrossbow   = PcItemCommon.Int(cols, 12),

                    wArmor      = PcItemCommon.Int(cols, 13),
                    wRing       = PcItemCommon.Int(cols, 14),
                    wNecklace   = PcItemCommon.Int(cols, 15),
                    wAmulet     = PcItemCommon.Int(cols, 16),
                    wBoot       = PcItemCommon.Int(cols, 17),
                    wBelt       = PcItemCommon.Int(cols, 18),
                    wHelm       = PcItemCommon.Int(cols, 19),
                    wCuff       = PcItemCommon.Int(cols, 20),
                    wSachet     = PcItemCommon.Int(cols, 21),
                    wPendant    = PcItemCommon.Int(cols, 22),

                    wMetal      = PcItemCommon.Int(cols, 23),
                    wWood       = PcItemCommon.Int(cols, 24),
                    wWater      = PcItemCommon.Int(cols, 25),
                    wFire       = PcItemCommon.Int(cols, 26),
                    wEarth      = PcItemCommon.Int(cols, 27),

                    rawTrailing = PcItemCommon.Int(cols, TrailingCol),
                });
            }
            return rows;
        }

        public static PcRareEnchantTable BuildTable(string dir)
        {
            var table = new PcRareEnchantTable();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return table;
            foreach (var f in Directory.GetFiles(dir, "rare*.txt"))
                foreach (var e in ParseFile(f)) table.Add(e);
            return table;
        }
    }
}
