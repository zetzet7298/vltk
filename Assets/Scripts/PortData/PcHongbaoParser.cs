// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/item/hongbao.txt Hồng Bao parser
// Source of truth: /var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/settings/item/hongbao.txt
// Backward-compatible with older hongbaosetting.ini rows when an Id header is present.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcHongbaoParser
    {
        public const int NameCol = 0;
        public const int TypeCol = 1;
        public const int GenreCol = 2;
        public const int DetailCol = 3;
        public const int ParticularCol = 4;
        public const int SeriseCol = 5;
        public const int LevelCol = 6;
        public const int Param1Col = 7;
        public const int ParamCount = 6;
        public const int ProbaCol = 13;
        public const int CostlyCol = 14;
        public const int MsgCol = 15;
        public const int LogCol = 16;

        public static List<PcHongbaoEntry> ParseFile(string path)
        {
            var rows = new List<PcHongbaoEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcText.ReadLinesTcvn3(path);
            if (lines.Length == 0) return rows;
            bool headerSkipped = false;
            bool legacySchema = false;
            int rowId = 0;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var cols = line.Split('\t');
                if (!headerSkipped)
                {
                    legacySchema = cols.Length > 0 && cols[0] == "Id";
                    headerSkipped = true;
                    continue;
                }
                var entry = legacySchema ? ParseLegacy(cols) : ParsePcItemHongbao(cols, ++rowId);
                if (entry != null) rows.Add(entry);
            }
            return rows;
        }

        public static PcHongbaoRegistry BuildRegistry(string dir)
        {
            var reg = new PcHongbaoRegistry();
            if (string.IsNullOrEmpty(dir)) return reg;

            string main = Directory.Exists(dir) ? Path.Combine(dir, "hongbao.txt") : dir;
            if (!File.Exists(main) && Directory.Exists(dir))
                main = Path.Combine(dir, "hongbaosetting.ini");
            if (!File.Exists(main)) return reg;
            foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }

        private static PcHongbaoEntry ParsePcItemHongbao(string[] cols, int rowId)
        {
            if (cols == null || cols.Length < 17) return null;
            string name = PcItemCommon.Str(cols, NameCol);
            if (string.IsNullOrEmpty(name)) return null;
            var entry = new PcHongbaoEntry
            {
                id = rowId,
                nameRaw = name,
                type = PcItemCommon.Int(cols, TypeCol),
                itemGenre = PcItemCommon.Int(cols, GenreCol),
                itemDetail = PcItemCommon.Int(cols, DetailCol),
                itemParticular = PcItemCommon.Int(cols, ParticularCol),
                serise = PcItemCommon.Int(cols, SeriseCol),
                level = PcItemCommon.Int(cols, LevelCol),
                proba = PcItemCommon.Int(cols, ProbaCol),
                costly = PcItemCommon.Int(cols, CostlyCol),
                msg = PcItemCommon.Str(cols, MsgCol),
                log = PcItemCommon.Int(cols, LogCol),
                count = 1,
            };
            for (int i = 0; i < ParamCount; i++)
                entry.param[i] = PcItemCommon.Int(cols, Param1Col + i);
            return entry;
        }

        private static PcHongbaoEntry ParseLegacy(string[] cols)
        {
            if (cols == null || cols.Length < 6) return null;
            int id = PcItemCommon.Int(cols, 0);
            if (id <= 0) return null;
            return new PcHongbaoEntry
            {
                id = id,
                type = PcItemCommon.Int(cols, 1),
                itemGenre = PcItemCommon.Int(cols, 2),
                itemDetail = PcItemCommon.Int(cols, 3),
                itemParticular = PcItemCommon.Int(cols, 4),
                count = PcItemCommon.Int(cols, 5),
                minLevel = cols.Length > 6 ? PcItemCommon.Int(cols, 6) : 0,
                maxLevel = cols.Length > 7 ? PcItemCommon.Int(cols, 7) : 0,
                silver = cols.Length > 8 ? PcItemCommon.Int(cols, 8) : 0,
                karma = cols.Length > 9 ? PcItemCommon.Int(cols, 9) : 0,
            };
        }
    }

    [System.Serializable]
    public class PcHongbaoEntry
    {
        public int id;
        public string nameRaw;
        public int type;
        public int itemGenre;
        public int itemDetail;
        public int itemParticular;
        public int serise;
        public int level;
        public readonly int[] param = new int[PcHongbaoParser.ParamCount];
        public int proba;
        public int costly;
        public string msg;
        public int log;
        public int count;
        public int minLevel;
        public int maxLevel;
        public int silver;
        public int karma;
    }

    public sealed class PcHongbaoRegistry
    {
        private readonly Dictionary<int, PcHongbaoEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcHongbaoEntry e)
        {
            if (e == null || e.id <= 0) return;
            _byId[e.id] = e;
        }
        public PcHongbaoEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcHongbaoEntry> GetAll() => _byId.Values;
    }
}
