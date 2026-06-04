// -----------------------------------------------------------------------------
// VLTK Mobile — ST-05.2 Enhance/Refine Quest Rewards
// Item enhance (cường hóa), refine (tinh luyện), quest reward item generation.
// PC source: KNpc::EnhanceItem, RefineItem, quest reward tables.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    [Serializable]
    public class EnhanceResult
    {
        public bool success;
        public int oldLevel;
        public int newLevel;
        public int silverCost;
        public string materialUsedVi;
        public bool itemDestroyed; // PC: enhance fail có thể làm hỏng đồ
    }

    [Serializable]
    public class RefineResult
    {
        public bool success;
        public int oldRefineLevel;
        public int newRefineLevel;
        public int bonusAttrCode;
        public int bonusValue;
    }

    [Serializable]
    public class QuestReward
    {
        public long exp;
        public int silver;
        public List<int> itemIds = new();
        public List<int> itemCounts = new();
        public int skillPoints;
        public string descriptionVi;
    }

    /// <summary>
    /// Service quản lý cường hóa (enhance), tinh luyện (refine) và phần thưởng nhiệm vụ.
    /// PC source: KNpc::EnhanceItem, RefineItem, quest reward generation.
    /// </summary>
    public class EnhanceRefineService
    {
        private const float BaseSuccessRate = 0.85f; // 85% ở level 1
        private const float RateDecayPerLevel = 0.07f; // Giảm 7% mỗi level
        private const int MaxEnhanceLevel = 16;        // PC max +16
        private const int MaxRefineLevel = 10;

        /// <summary>Cường hóa vật phẩm.</summary>
        public EnhanceResult Enhance(ItemDefinition item, int currentEnhanceLevel, int playerSilver)
        {
            var result = new EnhanceResult { oldLevel = currentEnhanceLevel };

            if (item == null || currentEnhanceLevel >= MaxEnhanceLevel)
                return result;

            // Chi phí Bạc tăng theo cấp: baseCost * (level + 1)
            int cost = CalculateEnhanceCost(currentEnhanceLevel);
            if (playerSilver < cost)
            {
                SubsystemLog.Warn("Enhance", $"Không đủ Bạc ({cost} cần)");
                return result;
            }

            result.silverCost = cost;

            // Xác suất thành công giảm theo level
            float rate = Mathf.Max(0.05f, BaseSuccessRate - currentEnhanceLevel * RateDecayPerLevel);
            result.success = UnityEngine.Random.value <= rate;

            if (result.success)
            {
                result.newLevel = currentEnhanceLevel + 1;
                SubsystemLog.Info("Enhance", $"Cường hóa thành công: +{result.newLevel} ({rate * 100:F0}%)");
            }
            else
            {
                // PC JX: từ +7 trở lên, fail có thể giảm 1 level hoặc hỏng đồ
                if (currentEnhanceLevel >= 7)
                {
                    result.newLevel = currentEnhanceLevel - 1;
                    result.itemDestroyed = UnityEngine.Random.value < 0.1f; // 10% hỏng
                    SubsystemLog.Warn("Enhance", $"Cường hóa thất bại! +{result.newLevel} (ItemDestroyed={result.itemDestroyed})");
                }
                else
                {
                    result.newLevel = currentEnhanceLevel; // Giữ nguyên
                }
            }

            return result;
        }

        /// <summary>Tinh luyện vật phẩm.</summary>
        public RefineResult Refine(ItemDefinition item, int currentRefineLevel, int targetAttrCode)
        {
            var result = new RefineResult { oldRefineLevel = currentRefineLevel, bonusAttrCode = targetAttrCode };

            if (item == null || currentRefineLevel >= MaxRefineLevel)
                return result;

            float rate = Mathf.Max(0.1f, 0.9f - currentRefineLevel * 0.08f);
            result.success = UnityEngine.Random.value <= rate;

            if (result.success)
            {
                result.newRefineLevel = currentRefineLevel + 1;
                result.bonusValue = result.newRefineLevel * 2; // +2 per refine level
                SubsystemLog.Info("Refine", $"Tinh luyện thành công: Level {result.newRefineLevel}, attr {targetAttrCode} +{result.bonusValue}");
            }
            else
            {
                result.newRefineLevel = currentRefineLevel;
                SubsystemLog.Warn("Refine", "Tinh luyện thất bại.");
            }

            return result;
        }

        /// <summary>Tạo phần thưởng nhiệm vụ dựa trên difficulty và playerLevel.</summary>
        public static QuestReward GenerateQuestReward(int questDifficulty, int playerLevel)
        {
            var reward = new QuestReward
            {
                exp = questDifficulty * playerLevel * 50L,
                silver = questDifficulty * playerLevel * 5,
                skillPoints = questDifficulty >= 3 ? 1 : 0,
            };

            // Vật phẩm thưởng theo difficulty
            if (questDifficulty >= 1)
            {
                reward.itemIds.Add(1001); // Tiểu Hồi Đan
                reward.itemCounts.Add(questDifficulty);
            }
            if (questDifficulty >= 2)
            {
                reward.itemIds.Add(1002); // Đại Hồi Đan
                reward.itemCounts.Add(1);
            }
            if (questDifficulty >= 3)
            {
                reward.itemIds.Add(2001); // Huyền Tinh cấp 1
                reward.itemCounts.Add(questDifficulty);
            }
            if (questDifficulty >= 5)
            {
                reward.itemIds.Add(3001); // Tẩy Tủy Kinh
                reward.itemCounts.Add(1);
            }

            reward.descriptionVi = $"EXP {reward.exp}, Bạc {reward.silver}, {reward.itemIds.Count} loại vật phẩm";
            return reward;
        }

        private static int CalculateEnhanceCost(int currentLevel) => 100 * (currentLevel + 1) * (currentLevel + 1);
    }
}
