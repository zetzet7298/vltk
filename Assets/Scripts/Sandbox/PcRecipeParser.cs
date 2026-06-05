// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/task/equipex/platina_def.txt recipe (công thức) parser
// Source: equipex/platina_def.txt (1,294 rows, GB2312, 5 cols).
//   EQUIPNAME  PLATINAID  GOLDID  TASKRATE  RECOIN
// Maps a platina (named/cyan) equipment to its gold counterpart for compound/refine.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcRecipeParser
    {
        public const int NameCol = 0;
        public const int PlatinaIdCol = 1;
        public const int GoldIdCol = 2;
        public const int TaskRateCol = 3;
        public const int RecoinCol = 4;

        public static List<PcRecipeEntry> ParseFile(string path)
        {
            var rows = new List<PcRecipeEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                rows.Add(new PcRecipeEntry
                {
                    platinaId = PcItemCommon.Int(cols, PlatinaIdCol),
                    goldId = PcItemCommon.Int(cols, GoldIdCol),
                    nameRaw = PcItemCommon.Str(cols, NameCol),
                    taskRate = PcItemCommon.Int(cols, TaskRateCol),
                    recoin = PcItemCommon.Int(cols, RecoinCol),
                });
            }
            return rows;
        }

        public static PcRecipeRegistry BuildRegistry(string dir)
        {
            var reg = new PcRecipeRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, "platina_def.txt");
            string tmp = Path.Combine(dir, "platina_def_tmp.txt");
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
            if (File.Exists(tmp))
                foreach (var s in ParseFile(tmp)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcRecipeEntry
    {
        public int platinaId;
        public int goldId;
        public string nameRaw;
        public int taskRate;
        public int recoin;
    }

    public sealed class PcRecipeRegistry
    {
        private readonly Dictionary<int, PcRecipeEntry> _byPlatina = new();
        private readonly Dictionary<int, PcRecipeEntry> _byGold = new();
        public int Count => _byPlatina.Count;
        public IEnumerable<PcRecipeEntry> All => _byPlatina.Values;
        public void Register(PcRecipeEntry e)
        {
            if (e == null || e.platinaId <= 0) return;
            _byPlatina[e.platinaId] = e;
            if (e.goldId > 0) _byGold[e.goldId] = e;
        }
        public PcRecipeEntry GetByPlatina(int id) => _byPlatina.TryGetValue(id, out var v) ? v : null;
        public PcRecipeEntry GetByGold(int id) => _byGold.TryGetValue(id, out var v) ? v : null;
    }
}
