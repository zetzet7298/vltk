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
            return new FoundryPanelSnapshot { rows = System.Array.Empty<FoundryPanelRow>() };
        }

        public static bool CanCraft(FoundryService service, int recipeId, int playerLevel, int materialCount)
        {
            return false;
        }

        public static bool TryCraft(FoundryService service, int playerId, int recipeId)
        {
            return false;
        }

        public static float ComputeSuccessRate(FoundryService service, int recipeId, int playerLuck)
        {
            return 0f;
        }

    }
}
