// -----------------------------------------------------------------------------
// VLTK Mobile — UI Panel: Foundry (Rèn Đúc)
// Bảng UI cho rèn đúc, công thức, vật liệu, tỉ lệ thành công.
// Vietnamese: "Rèn Đúc", "Công thức", "Tỉ lệ thành công", "Rèn", "Vật liệu".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using VLTK.Sandbox;

namespace VLTK.UI
{
    public readonly struct FoundryPanelRow
    {
        public readonly int recipeId;
        public readonly string name;
        public readonly string requiredItems;
        public readonly float successRate;
        public readonly string resultItemName;
        public readonly int costSilver;
        public readonly bool isRepeatable;
        public readonly bool isCrafted;

        public FoundryPanelRow(int recipeId, string name, string requiredItems, float successRate, string resultItemName, int costSilver, bool isRepeatable, bool isCrafted)
        {
            this.recipeId = recipeId;
            this.name = name ?? string.Empty;
            this.requiredItems = requiredItems ?? string.Empty;
            this.successRate = successRate;
            this.resultItemName = resultItemName ?? string.Empty;
            this.costSilver = costSilver;
            this.isRepeatable = isRepeatable;
            this.isCrafted = isCrafted;
        }
    }

    public sealed class FoundryPanelSnapshot
    {
        public int playerId;
        public int learnedRecipes;
        public int totalRecipes;
        public IReadOnlyList<FoundryPanelRow> rows;
    }

    public static class FoundryPanelService
    {
        public const string LabelFoundry = "Rèn Đúc";
        public const string LabelRecipe = "Công thức";
        public const string LabelSuccessRate = "Tỉ lệ thành công";
        public const string LabelCraft = "Rèn";
        public const string LabelMaterial = "Vật liệu";

        public static FoundryPanelSnapshot BuildSnapshot(FoundryService service, int playerId)
        {
            var snapshot = new FoundryPanelSnapshot
            {
                playerId = playerId,
                learnedRecipes = 0,
                totalRecipes = 0,
                rows = Array.Empty<FoundryPanelRow>()
            };
            if (service == null) return snapshot;
            var all = service.GetAll();
            var rows = new List<FoundryPanelRow>();
            int learned = 0;
            foreach (var recipe in all)
            {
                if (recipe == null) continue;
                bool knows = service.HasLearned(playerId, recipe.recipeId);
                if (knows) learned++;
                rows.Add(new FoundryPanelRow(
                    recipe.recipeId, recipe.nameRaw, recipe.requiredItems, recipe.baseSuccessRate,
                    recipe.resultItemName, recipe.costSilver, recipe.isRepeatable, knows));
            }
            snapshot.learnedRecipes = learned;
            snapshot.totalRecipes = rows.Count;
            snapshot.rows = rows;
            return snapshot;
        }

        public static bool CanCraft(FoundryService service, int recipeId, int playerLevel, int materialCount)
        {
            if (service == null || recipeId <= 0) return false;
            var recipe = service.GetRecipe(recipeId);
            if (recipe == null) return false;
            if (playerLevel < recipe.requiredLevel) return false;
            if (materialCount < recipe.materialCount) return false;
            return true;
        }

        public static bool TryCraft(FoundryService service, int playerId, int recipeId)
        {
            if (service == null || playerId <= 0 || recipeId <= 0) return false;
            return service.TryCraft(playerId, recipeId);
        }

        public static float ComputeSuccessRate(FoundryService service, int recipeId, int playerLuck)
        {
            if (service == null || recipeId <= 0) return 0f;
            var recipe = service.GetRecipe(recipeId);
            if (recipe == null) return 0f;
            float baseRate = recipe.baseSuccessRate;
            float luckBonus = playerLuck * 0.001f; // 0.1% per luck point
            float final = baseRate + luckBonus;
            if (final > 1f) final = 1f;
            if (final < 0f) final = 0f;
            return final;
        }
    }
}
