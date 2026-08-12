// -----------------------------------------------------------------------------
// VLTK Mobile — Atlas compound craft-plan model
// PC source: script/item/compound/atlas.lua + compound_header.lua
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace VLTK.Sandbox
{
    [Serializable]
    public class PcAtlasCompoundItemSpec
    {
        public int genre;
        public int detailType;
        public int particular;
    }

    [Serializable]
    public class PcAtlasCompoundMaterialSpec : PcAtlasCompoundItemSpec
    {
        public string nameRaw;
        public int level = -1;
        public int series = -1;
        public int magicId = -1;
    }

    [Serializable]
    public sealed class PcAtlasCompoundResultSpec : PcAtlasCompoundMaterialSpec
    {
        public int quality;
        public int piece;
        public int pieceSum;
        public int itemValue;
        public string compoundParam;
    }

    [Serializable]
    public sealed class PcAtlasCompoundRecipe
    {
        public string atlasNameRaw;
        public PcAtlasCompoundItemSpec atlas;
        public int atlasNoSign;
        public List<PcAtlasCompoundMaterialSpec> materials = new();
        public PcAtlasCompoundResultSpec result;
    }

    public sealed class PcAtlasCompoundRegistry
    {
        private readonly Dictionary<string, List<PcAtlasCompoundRecipe>> _byAtlasKey = new();
        public int Count { get; private set; }
        public IEnumerable<PcAtlasCompoundRecipe> All
        {
            get
            {
                foreach (var list in _byAtlasKey.Values)
                foreach (var recipe in list)
                    yield return recipe;
            }
        }

        public void Register(PcAtlasCompoundRecipe recipe)
        {
            if (recipe?.atlas == null || recipe.result == null) return;
            var key = MakeAtlasKey(recipe.atlas.genre, recipe.atlas.detailType, recipe.atlas.particular, recipe.atlasNoSign);
            if (!_byAtlasKey.TryGetValue(key, out var list))
            {
                list = new List<PcAtlasCompoundRecipe>();
                _byAtlasKey[key] = list;
            }
            list.Add(recipe);
            Count++;
        }

        public IReadOnlyList<PcAtlasCompoundRecipe> GetByAtlas(int genre, int detailType, int particular, int noSign)
        {
            return _byAtlasKey.TryGetValue(MakeAtlasKey(genre, detailType, particular, noSign), out var list)
                ? list
                : Array.Empty<PcAtlasCompoundRecipe>();
        }

        private static string MakeAtlasKey(int genre, int detailType, int particular, int noSign)
            => $"{genre},{detailType},{particular},{noSign}";
    }

    public readonly struct PcAtlasCompoundSourceItem
    {
        public readonly int genre;
        public readonly int detailType;
        public readonly int particular;
        public readonly int level;
        public readonly int series;
        public readonly int magicId;
        public readonly int noSign;
        public readonly int itemValue;

        public PcAtlasCompoundSourceItem(int genre, int detailType, int particular, int level, int series, int magicId, int noSign, int itemValue)
        {
            this.genre = genre;
            this.detailType = detailType;
            this.particular = particular;
            this.level = level;
            this.series = series;
            this.magicId = magicId;
            this.noSign = noSign;
            this.itemValue = itemValue;
        }

        public static PcAtlasCompoundSourceItem NosignAtlasPiece(int noSign, int value = 0)
            => new PcAtlasCompoundSourceItem(4, 1316 + noSign, 1, 0, 0, 0, noSign, value);
    }

    public enum CompoundPlanStatus
    {
        Ready,
        LackResource,
        RuleError,
        NoMoney,
        Succeeded,
        FailedByRng,
        AddItemFailed,
    }
}
