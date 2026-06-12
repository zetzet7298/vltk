// -----------------------------------------------------------------------------
// VLTK Mobile — PC recipe parsers.
// Legacy platina_def support remains for old tests; atlas_compound is the PC
// craft-plan source used by script/item/compound/atlas.lua.
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
            var lines = PcText.ReadLinesTcvn3(path);
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

        public static PcAtlasCompoundRegistry BuildAtlasCompoundRegistry(string path)
        {
            var reg = new PcAtlasCompoundRegistry();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return reg;
            var lines = PcText.ReadLinesTcvn3(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 55) continue;
                var recipe = ParseAtlasCompoundRow(cols);
                if (recipe != null) reg.Register(recipe);
            }
            return reg;
        }

        private static PcAtlasCompoundRecipe ParseAtlasCompoundRow(string[] cols)
        {
            var recipe = new PcAtlasCompoundRecipe
            {
                atlasNameRaw = PcItemCommon.Str(cols, 0),
                atlas = new PcAtlasCompoundItemSpec
                {
                    genre = PcItemCommon.Int(cols, 1, -1),
                    detailType = PcItemCommon.Int(cols, 2, -1),
                    particular = PcItemCommon.Int(cols, 3, -1),
                },
                atlasNoSign = PcItemCommon.Int(cols, 53, -1),
                materials = new List<PcAtlasCompoundMaterialSpec>(),
            };

            for (int i = 0; i < 6; i++)
            {
                int start = 4 + i * 7;
                if (string.IsNullOrWhiteSpace(PcItemCommon.Str(cols, start + 1))) continue;
                recipe.materials.Add(new PcAtlasCompoundMaterialSpec
                {
                    nameRaw = PcItemCommon.Str(cols, start),
                    genre = PcItemCommon.Int(cols, start + 1, -1),
                    detailType = PcItemCommon.Int(cols, start + 2, -1),
                    particular = PcItemCommon.Int(cols, start + 3, -1),
                    level = PcItemCommon.Int(cols, start + 4, -1),
                    series = PcItemCommon.Int(cols, start + 5, -1),
                    magicId = PcItemCommon.Int(cols, start + 6, -1),
                });
            }

            int quality = PcItemCommon.Int(cols, 47, -1);
            int detailType = PcItemCommon.Int(cols, 49, -1);
            recipe.result = new PcAtlasCompoundResultSpec
            {
                nameRaw = PcItemCommon.Str(cols, 46),
                quality = quality,
                genre = PcItemCommon.Int(cols, 48, -1),
                detailType = quality == 1 ? detailType - 1 : detailType,
                particular = PcItemCommon.Int(cols, 50, -1),
                level = PcItemCommon.Int(cols, 51, -1),
                series = PcItemCommon.Int(cols, 52, -1),
                piece = PcItemCommon.Int(cols, 53, -1),
                pieceSum = PcItemCommon.Int(cols, 54, -1),
                itemValue = PcItemCommon.Int(cols, 55, -1),
                compoundParam = "ATLAS",
            };
            return recipe;
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
