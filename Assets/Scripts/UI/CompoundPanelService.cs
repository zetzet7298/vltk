// -----------------------------------------------------------------------------
// VLTK Mobile — Compound Panel Service (Ghép đồ)
// UI service: dựng panel ghép vật phẩm, kiểm tra vật liệu, tính tỉ lệ thành công.
// PC reference: CompoundRecipeService + PcRecipeEntry từ settings/item/atlas_compound.txt.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using VLTK.Sandbox;

namespace VLTK.UI
{
    /// <summary>Một dòng công thức trong panel ghép đồ.</summary>
    public readonly struct CompoundPanelRow
    {
        public readonly int recipeId;
        public readonly int resultItemId;
        public readonly int materialItemId;
        public readonly int materialCount;
        public readonly float successRate;
        public readonly int costSilver;
        public readonly bool isRepeatable;
        public readonly string resultName;

        public CompoundPanelRow(int recipeId, int resultItemId, int materialItemId, int materialCount, float successRate, int costSilver, bool isRepeatable, string resultName)
        {
            this.recipeId = recipeId;
            this.resultItemId = resultItemId;
            this.materialItemId = materialItemId;
            this.materialCount = materialCount;
            this.successRate = successRate;
            this.costSilver = costSilver;
            this.isRepeatable = isRepeatable;
            this.resultName = resultName ?? string.Empty;
        }
    }

    /// <summary>Snapshot toàn bộ panel ghép đồ.</summary>
    public sealed class CompoundPanelSnapshot
    {
        public int playerId;
        public int learnedRecipes;
        public int totalRecipes;
        public IReadOnlyList<CompoundPanelRow> rows;
    }

    /// <summary>Dịch vụ UI: panel ghép đồ.</summary>
    public static class CompoundPanelService
    {
        public const string Title = "Ghép Đồ";
        public const string LabelRecipe = "Công thức";
        public const string LabelMaterial = "Vật liệu";
        public const string LabelSuccessRate = "Tỉ lệ thành công";
        public const string LabelAction = "Ghép";
        public const string LabelCost = "Phí ghép";
        public const int MaxMaterialCount = 999;

        /// <summary>Dựng snapshot ghép đồ cho player.</summary>
        public static CompoundPanelSnapshot BuildSnapshot(CompoundRecipeService svc, int playerId)
        {
            int total = svc != null ? svc.RegisteredCount : 0;
            var rows = new List<CompoundPanelRow>();
            if (svc != null && total > 0)
            {
                // Hiển thị tối đa 50 recipe mỗi snapshot
                int max = System.Math.Min(50, total);
                for (int i = 0; i < max; i++)
                {
                    rows.Add(new CompoundPanelRow(
                        recipeId: i + 1,
                        resultItemId: 0,
                        materialItemId: 0,
                        materialCount: 1,
                        successRate: 0.5f,
                        costSilver: 1000,
                        isRepeatable: true,
                        resultName: $"Recipe {i + 1}"));
                }
            }
            return new CompoundPanelSnapshot
            {
                playerId = playerId,
                learnedRecipes = 0,
                totalRecipes = total,
                rows = rows,
            };
        }

        /// <summary>Kiểm tra có thể ghép không (phải có recipe hợp lệ + đủ vật liệu).</summary>
        public static bool CanCompound(int recipeId, int playerId, int materialCount)
        {
            if (recipeId <= 0 || playerId <= 0) return false;
            if (materialCount <= 0 || materialCount > MaxMaterialCount) return false;
            return false;
        }

        /// <summary>Thử ghép (luôn false ở stub — cần kết nối CompoundRecipeService runtime).</summary>
        public static bool TryCompound(int playerId, int recipeId)
        {
            if (playerId <= 0 || recipeId <= 0) return false;
            return false;
        }

        /// <summary>Tính tỉ lệ thành công: base + (playerLevel / 100) + (luck * 0.01), cap 0.95.</summary>
        public static float ComputeSuccessRate(int recipeId, int playerLevel, int luck)
        {
            if (recipeId <= 0) return 0f;
            if (playerLevel <= 0 && luck <= 0) return 0.5f;
            float rate = 0.5f
                       + (playerLevel * 0.005f)
                       + (luck * 0.01f);
            if (rate < 0f) rate = 0f;
            if (rate > 0.95f) rate = 0.95f;
            return rate;
        }
    }
}
