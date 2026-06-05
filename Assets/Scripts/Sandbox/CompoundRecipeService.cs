// -----------------------------------------------------------------------------
// VLTK Mobile — Compound Recipe Service (Công Thức / Luyện Đồ Bạch Kim)
// PC source: settings/task/equipex/platina_def.txt (1,294 công thức).
// Runtime: tra cứu recipe, tính tỷ lệ thành công, thực hiện luyện.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>Kết quả luyện đồ theo công thức.</summary>
    [Serializable]
    public class CompoundResult
    {
        /// <summary>Có thành công không.</summary>
        public bool success;
        /// <summary>Đã tiêu hao bạc (Huyền Tinh) chưa.</summary>
        public bool recoinConsumed;
        /// <summary>Số Huyền Tinh tiêu hao.</summary>
        public int recoinCost;
        /// <summary>Mã vật phẩm mới (platinaId khi thành công, 0 khi thất bại).</summary>
        public int newItemId;
        /// <summary>Giá trị rolled (platinaId).</summary>
        public int rolledValue;
        /// <summary>Tên công thức tiếng Việt.</summary>
        public string recipeNameVi;
        /// <summary>Tỷ lệ thành công áp dụng (0..1).</summary>
        public float successRateApplied;

        public static CompoundResult Failure(int recoinCost, string nameVi) => new CompoundResult
        {
            success = false,
            recoinConsumed = true,
            recoinCost = recoinCost,
            newItemId = 0,
            rolledValue = 0,
            recipeNameVi = nameVi,
        };
    }

    /// <summary>
    /// Service quản lý công thức luyện đồ Bạch Kim.
    /// Ánh xạ platina ↔ gold theo PC platina_def.
    /// </summary>
    public class CompoundRecipeService
    {
        public const string DefaultStreamingDir = "Reference/PcRecipe";

        private readonly PcRecipeRegistry _registry;

        /// <summary>Sự kiện khi thực hiện luyện đồ (kể cả thất bại).</summary>
        public event Action<CompoundResult> OnCompound;

        public int RegisteredCount => _registry?.Count ?? 0;

        public CompoundRecipeService(PcRecipeRegistry registry)
        {
            _registry = registry ?? new PcRecipeRegistry();
        }

        /// <summary>Khởi tạo từ thư mục StreamingAssets.</summary>
        public static CompoundRecipeService LoadFromStreamingAssets(string subDir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subDir) ? DefaultStreamingDir : subDir);
            var reg = PcRecipeParser.BuildRegistry(dir);
            return new CompoundRecipeService(reg);
        }

        /// <summary>Lấy công thức theo mã Bạch Kim (platina).</summary>
        public PcRecipeEntry GetRecipeByPlatina(int platinaId) =>
            _registry.GetByPlatina(platinaId);

        /// <summary>Lấy công thức theo mã Hoàng Kim (gold).</summary>
        public PcRecipeEntry GetRecipeByGold(int goldId) =>
            _registry.GetByGold(goldId);

        /// <summary>Có thể luyện platina → gold (hoặc ngược lại) không.</summary>
        public bool CanCompound(int platinaId, int goldId)
        {
            if (platinaId <= 0 || goldId <= 0) return false;
            var r = _registry.GetByPlatina(platinaId);
            return r != null && r.goldId == goldId;
        }

        /// <summary>
        /// Tính tỷ lệ thành công từ taskRate (PC dùng 0-10000 scale, 10000 = 100%).
        /// Trả về float 0..1.
        /// </summary>
        public float CalculateSuccessRate(int platinaId)
        {
            var r = _registry.GetByPlatina(platinaId);
            if (r == null) return 0f;
            return Mathf.Clamp01(r.taskRate / 10000f);
        }

        /// <summary>
        /// Thực hiện luyện đồ. Trả về kết quả (CompoundResult).
        /// - roll: UnityEngine.Random.value so với successRate + playerLuck bonus
        /// - thành công: trả về platinaId
        /// - thất bại: tiêu hao recoin, không nhận đồ
        /// </summary>
        public CompoundResult TryCompound(int platinaId, int goldId, int playerLuck = 0)
        {
            var recipe = _registry.GetByPlatina(platinaId);
            if (recipe == null || recipe.goldId != goldId)
            {
                SubsystemLog.Warn("Compound", $"Không tìm thấy công thức platina={platinaId} gold={goldId}");
                return CompoundResult.Failure(0, "Không rõ");
            }

            float baseRate = Mathf.Clamp01(recipe.taskRate / 10000f);
            // Player luck: mỗi điểm luck = +1% thành công, tối đa +50%
            float luckBonus = Mathf.Clamp(playerLuck, 0, 50) / 100f;
            float finalRate = Mathf.Clamp01(baseRate + luckBonus);

            float roll = UnityEngine.Random.value;
            bool success = roll <= finalRate;
            int recoinCost = Mathf.Max(0, recipe.recoin);

            var result = new CompoundResult
            {
                success = success,
                recoinConsumed = true,
                recoinCost = recoinCost,
                newItemId = success ? recipe.platinaId : 0,
                rolledValue = recipe.platinaId,
                recipeNameVi = recipe.nameRaw,
                successRateApplied = finalRate,
            };
            SubsystemLog.Info("Compound",
                $"{(success ? "Thành công" : "Thất bại")} luyện {recipe.nameRaw} (rate={finalRate:F2} roll={roll:F2} recoin={recoinCost})");
            OnCompound?.Invoke(result);
            return result;
        }

        /// <summary>Duyệt tất cả công thức đã đăng ký.</summary>
        public IEnumerable<PcRecipeEntry> GetAllRecipes() => _registry.All;
    }
}
