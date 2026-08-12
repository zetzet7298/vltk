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
        public const string DefaultStreamingDir = "Reference/PcItemFull";
        public const int AtlasCompoundCost = 100000;

        private readonly PcRecipeRegistry _registry;
        private readonly PcAtlasCompoundRegistry _atlasRegistry;

        /// <summary>Sự kiện khi thực hiện luyện đồ (kể cả thất bại).</summary>
        public event Action<CompoundResult> OnCompound;

        public int RegisteredCount => _atlasRegistry?.Count ?? _registry?.Count ?? 0;

        public CompoundRecipeService(PcRecipeRegistry registry)
        {
            _registry = registry ?? new PcRecipeRegistry();
            _atlasRegistry = null;
        }

        public CompoundRecipeService(PcAtlasCompoundRegistry atlasRegistry)
        {
            _registry = new PcRecipeRegistry();
            _atlasRegistry = atlasRegistry ?? new PcAtlasCompoundRegistry();
        }

        /// <summary>Khởi tạo từ thư mục StreamingAssets.</summary>
        public static CompoundRecipeService LoadFromStreamingAssets(string subDir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subDir) ? DefaultStreamingDir : subDir);
            var reg = PcRecipeParser.BuildAtlasCompoundRegistry(Path.Combine(dir, "atlas_compound.txt"));
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
        public IEnumerable<PcAtlasCompoundRecipe> GetAllAtlasRecipes() => _atlasRegistry?.All ?? Array.Empty<PcAtlasCompoundRecipe>();

        public CompoundCraftPlan BuildAtlasCraftPlan(
            IReadOnlyList<PcAtlasCompoundSourceItem> necessaryItems,
            IReadOnlyList<PcAtlasCompoundSourceItem> alternativeItems)
        {
            var necessary = necessaryItems ?? Array.Empty<PcAtlasCompoundSourceItem>();
            var alternative = alternativeItems ?? Array.Empty<PcAtlasCompoundSourceItem>();
            if (_atlasRegistry == null) return CompoundCraftPlan.Reject(CompoundPlanStatus.LackResource);

            int noSignCount = 0;
            int noSign = 0;
            foreach (var item in alternative)
            {
                if (TryGetNoSign(item, out var currentNoSign))
                {
                    noSignCount++;
                    noSign = currentNoSign;
                }
            }
            if (noSignCount != 1) return CompoundCraftPlan.Reject(CompoundPlanStatus.LackResource);

            PcAtlasCompoundSourceItem atlasItem = default;
            bool foundAtlas = false;
            var materialItems = new List<PcAtlasCompoundSourceItem>();
            bool foundXuanjing = false;
            IReadOnlyList<PcAtlasCompoundRecipe> candidates = Array.Empty<PcAtlasCompoundRecipe>();

            foreach (var item in necessary)
            {
                if (!foundAtlas)
                {
                    candidates = _atlasRegistry.GetByAtlas(item.genre, item.detailType, item.particular, noSign);
                    if (candidates.Count > 0)
                    {
                        atlasItem = item;
                        foundAtlas = true;
                        continue;
                    }
                }

                if (item.genre == 6 && item.detailType == 1 && item.particular == 147)
                {
                    if (foundXuanjing) return CompoundCraftPlan.Reject(CompoundPlanStatus.RuleError);
                    foundXuanjing = true;
                    continue;
                }
                materialItems.Add(item);
            }

            if (!foundAtlas || !foundXuanjing) return CompoundCraftPlan.Reject(CompoundPlanStatus.LackResource);
            var matched = MatchRecipe(candidates, materialItems);
            if (matched == null) return CompoundCraftPlan.Reject(CompoundPlanStatus.LackResource);

            int srcValue = SumValues(necessary) + SumValues(alternative);
            int desValue = ResolveDestinationValue(matched.result);
            float prob = desValue <= 0 ? 0f : Mathf.Clamp01(srcValue / (float)desValue);
            return new CompoundCraftPlan
            {
                status = CompoundPlanStatus.Ready,
                costSilver = AtlasCompoundCost,
                sourceItemValueSum = srcValue,
                destinationItemValue = desValue,
                successProbability = prob,
                recipe = matched,
                necessaryItems = necessary,
                alternativeItems = alternative,
                operations = BuildAtlasOperations(matched, prob),
            };
        }

        private static IReadOnlyList<CompoundPlanOperation> BuildAtlasOperations(PcAtlasCompoundRecipe recipe, float probability)
        {
            return new[]
            {
                CompoundPlanOperation.Int("Pay", AtlasCompoundCost),
                CompoundPlanOperation.Text("WriteCompoundLog", $"[ATLAS]\tprob={probability:0.0000}\tresult={recipe?.result?.genre},{recipe?.result?.detailType},{recipe?.result?.particular}"),
                CompoundPlanOperation.Text("RemoveNecessaryItems", "RemoveItemByIndex necessary"),
                CompoundPlanOperation.Text("RemoveAlternativeItems", "RemoveItemByIndex alternative"),
                CompoundPlanOperation.Text("AddItemEx", "AddItemEx result"),
            };
        }

        private static PcAtlasCompoundRecipe MatchRecipe(IReadOnlyList<PcAtlasCompoundRecipe> candidates, IReadOnlyList<PcAtlasCompoundSourceItem> materialItems)
        {
            foreach (var recipe in candidates)
            {
                int reqCount = 0;
                int matchCount = 0;
                foreach (var req in recipe.materials)
                {
                    if (req.genre < 0) continue;
                    reqCount++;
                    bool exists = false;
                    foreach (var item in materialItems)
                    {
                        if (Matches(req, item))
                        {
                            exists = true;
                            matchCount++;
                            break;
                        }
                    }
                    if (!exists) break;
                }
                if (reqCount == matchCount && reqCount == materialItems.Count) return recipe;
            }
            return null;
        }

        private static bool Matches(PcAtlasCompoundMaterialSpec req, PcAtlasCompoundSourceItem item)
        {
            return item.genre == req.genre
                   && (req.detailType < 0 || item.detailType == req.detailType)
                   && (req.particular < 0 || item.particular == req.particular)
                   && (req.level < 0 || item.level >= req.level)
                   && (req.series < 0 || item.series == req.series)
                   && (req.magicId < 0 || item.magicId == req.magicId);
        }

        private static bool TryGetNoSign(PcAtlasCompoundSourceItem item, out int noSign)
        {
            noSign = 0;
            if (item.genre != 4 || item.particular != 1) return false;
            int candidate = item.noSign > 0 ? item.noSign : item.detailType - 1316;
            if (candidate < 1 || candidate > 9) return false;
            noSign = candidate;
            return true;
        }

        private static int SumValues(IReadOnlyList<PcAtlasCompoundSourceItem> items)
        {
            int sum = 0;
            foreach (var item in items) sum += Math.Max(0, item.itemValue);
            return sum;
        }

        private static int ResolveDestinationValue(PcAtlasCompoundResultSpec result)
        {
            if (result == null) return 0;
            return result.itemValue > 0 ? result.itemValue : 1;
        }

        public CompoundCraftExecutionResult ExecuteAtlasCraftPlan(CompoundCraftPlan plan, float randomRollInclusive0To1, int addedItemIndex = 1)
        {
            var executed = new List<string>();
            if (plan == null || plan.status != CompoundPlanStatus.Ready)
                return new CompoundCraftExecutionResult { status = CompoundPlanStatus.LackResource, executedOperationNames = executed };

            executed.Add("Pay");
            bool success = plan.successProbability > 0f && randomRollInclusive0To1 <= plan.successProbability;
            executed.Add("WriteCompoundLog");
            executed.Add("RemoveNecessaryItems");
            executed.Add("RemoveAlternativeItems");
            if (!success)
                return new CompoundCraftExecutionResult { status = CompoundPlanStatus.FailedByRng, resultItemIndex = 0, executedOperationNames = executed };

            executed.Add("AddItemEx");
            return new CompoundCraftExecutionResult
            {
                status = addedItemIndex > 0 ? CompoundPlanStatus.Succeeded : CompoundPlanStatus.AddItemFailed,
                resultItemIndex = addedItemIndex > 0 ? addedItemIndex : 0,
                executedOperationNames = executed,
            };
        }
    }
}
