// -----------------------------------------------------------------------------
// VLTK Mobile — ST-03.3 Dã Tẩu Task Chain Service
// Chuỗi nhiệm vụ Dã Tẩu hàng ngày: KillNpc, FindItem, FindNpc, ReachLevel.
// PC source: DaTau task tables, reward tiers, chain counter.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public enum DaTauTaskType
    {
        KillNpc,     // Tiêu diệt quái
        FindItem,    // Tìm vật phẩm giao Dã Tẩu
        FindNpc,     // Gặp NPC cụ thể
        ReachLevel,  // Đạt cấp độ yêu cầu
    }

    [Serializable]
    public class DaTauTask
    {
        public int taskId;
        public int chainIndex;         // Vị trí trong chuỗi (0-based)
        public DaTauTaskType type;
        public int targetId;           // NpcTemplateId / ItemId / NpcId / Level
        public int targetCount;        // Số lượng cần đạt (kill count, item count)
        public int currentProgress;
        public string descriptionVi;
        public bool isComplete => currentProgress >= targetCount;
    }

    [Serializable]
    public class DaTauReward
    {
        public int exp;
        public int silver;
        public int xuanTinhCount;      // Huyền Tinh (1-8 dòng)
        public int xuanTingGrade;      // Grade 1-8
        public bool grantSkillPoint;
        public string bonusItemNameVi; // Võ Lâm Mật Tịch, Tẩy Tủy Kinh, v.v.
    }

    /// <summary>
    /// Service quản lý chuỗi nhiệm vụ Dã Tẩu hàng ngày.
    /// PC source: DaTau task chain, max 40 nhiệm vụ/ngày, phần thưởng theo chuỗi.
    /// </summary>
    public class DaTauTaskChainService
    {
        public const int MaxDailyTasks = 40;
        public const int DaTauNpcTemplateId = 500;

        private readonly TaskFlagService _taskFlags;
        private readonly PlayerLevelService _levelService;

        private int _chainCount;              // Số nhiệm vụ đã hoàn thành liên tục
        private int _dailyCompleted;          // Số nhiệm vụ đã làm hôm nay
        private DaTauTask _currentTask;
        private readonly List<DaTauTask> _history = new();

        public int ChainCount => _chainCount;
        public int DailyCompleted => _dailyCompleted;
        public DaTauTask CurrentTask => _currentTask;
        public IReadOnlyList<DaTauTask> History => _history;

        public event Action<DaTauTask> OnTaskAccepted;
        public event Action<DaTauTask, DaTauReward> OnTaskCompleted;
        public event Action<int> OnChainReset;

        public DaTauTaskChainService(TaskFlagService taskFlags, PlayerLevelService levelService)
        {
            _taskFlags = taskFlags ?? throw new ArgumentNullException(nameof(taskFlags));
            _levelService = levelService ?? throw new ArgumentNullException(nameof(levelService));
        }

        /// <summary>Nhận nhiệm vụ Dã Tẩu tiếp theo.</summary>
        public DaTauTask AcceptNextTask()
        {
            if (_dailyCompleted >= MaxDailyTasks)
            {
                SubsystemLog.Warn("DaTau", "Đã hoàn thành tối đa 40 nhiệm vụ hôm nay.");
                return null;
            }

            // Nếu đang có task chưa xong, không cho nhận mới
            if (_currentTask != null && !_currentTask.isComplete)
                return _currentTask;

            _currentTask = GenerateTask(_chainCount, _levelService.Level);
            _taskFlags.SetFlag(_currentTask.taskId, 1, 0, _currentTask.targetCount, _currentTask.descriptionVi);

            OnTaskAccepted?.Invoke(_currentTask);
            SubsystemLog.Info("DaTau", $"Nhận nhiệm vụ #{_chainCount + 1}: {_currentTask.descriptionVi}");
            return _currentTask;
        }

        /// <summary>Cập nhật tiến độ nhiệm vụ.</summary>
        public void UpdateProgress(DaTauTaskType type, int targetId, int amount = 1)
        {
            if (_currentTask == null || _currentTask.type != type) return;
            if (_currentTask.type == DaTauTaskType.KillNpc && _currentTask.targetId == targetId)
                _currentTask.currentProgress += amount;
            else if (_currentTask.type == DaTauTaskType.FindItem && _currentTask.targetId == targetId)
                _currentTask.currentProgress += amount;
            else if (_currentTask.type == DaTauTaskType.FindNpc && _currentTask.targetId == targetId)
                _currentTask.currentProgress = _currentTask.targetCount; // Gặp NPC = xong luôn
            else if (_currentTask.type == DaTauTaskType.ReachLevel && _levelService.Level >= _currentTask.targetId)
                _currentTask.currentProgress = _currentTask.targetCount;

            _currentTask.currentProgress = Mathf.Min(_currentTask.currentProgress, _currentTask.targetCount);
            _taskFlags.SetFlag(_currentTask.taskId, 1, _currentTask.currentProgress, _currentTask.targetCount);
        }

        /// <summary>Trả nhiệm vụ, nhận thưởng.</summary>
        public DaTauReward TurnInTask()
        {
            if (_currentTask == null || !_currentTask.isComplete) return null;

            var reward = CalculateReward(_chainCount);

            // Cộng thưởng
            if (_levelService != null && reward.exp > 0)
                _levelService.AddExp(reward.exp);
            if (reward.grantSkillPoint)
                _levelService.GrantSkillPoint(1);

            // Đánh dấu hoàn thành
            _taskFlags.SetFlag(_currentTask.taskId, 3);
            _history.Add(_currentTask);
            _chainCount++;
            _dailyCompleted++;

            OnTaskCompleted?.Invoke(_currentTask, reward);
            SubsystemLog.Info("DaTau", $"Hoàn thành nhiệm vụ {_chainCount}. Thưởng: {reward.exp} EXP, {reward.silver} Bạc");
            _currentTask = null;
            return reward;
        }

        /// <summary>Hủy nhiệm vụ hiện tại (reset chuỗi về 0).</summary>
        public void AbandonTask()
        {
            if (_currentTask == null) return;

            _taskFlags.SetFlag(_currentTask.taskId, 0);
            _currentTask = null;
            _chainCount = 0;

            OnChainReset?.Invoke(0);
            SubsystemLog.Info("DaTau", "Hủy nhiệm vụ. Chuỗi reset về 0.");
        }

        /// <summary>Reset daily counter (gọi mỗi ngày mới).</summary>
        public void ResetDaily()
        {
            _dailyCompleted = 0;
            SubsystemLog.Info("DaTau", "Daily task counter reset.");
        }

        // ── Reward Tables ──────────────────────────────────────────────────

        private DaTauReward CalculateReward(int chainIndex)
        {
            // PC source: Phần thưởng tăng dần theo chuỗi
            // Cứ mỗi 10 nhiệm vụ, bonus thêm EXP và Bạc
            int tier = chainIndex / 10; // 0-9 = tier 0, 10-19 = tier 1, v.v.

            var reward = new DaTauReward
            {
                exp = 500 + tier * 300 + chainIndex * 50,
                silver = 100 + tier * 50 + chainIndex * 10,
                xuanTinhCount = tier >= 3 ? 1 : 0,
                xuanTingGrade = Mathf.Min(tier, 8),
                grantSkillPoint = chainIndex > 0 && (chainIndex + 1) % 10 == 0, // Mỗi 10 chuỗi +1 skill point
            };

            // Bonus đặc biệt theo mốc chuỗi (PC milestones)
            if (chainIndex == 9)  reward.bonusItemNameVi = "Tẩy Tủy Kinh";
            if (chainIndex == 49) reward.bonusItemNameVi = "Võ Lâm Mật Tịch";
            if (chainIndex == 99) reward.bonusItemNameVi = "Nhạc Vương Kiếm";

            return reward;
        }

        // ── Task Generation ────────────────────────────────────────────────

        private DaTauTask GenerateTask(int chainIndex, int playerLevel)
        {
            // Xoay vòng loại nhiệm vụ theo PC pattern
            DaTauTaskType type = (DaTauTaskType)(chainIndex % 4);

            var task = new DaTauTask
            {
                taskId = 10000 + chainIndex, // Task IDs từ 10000 trở lên
                chainIndex = chainIndex,
                type = type,
                currentProgress = 0,
            };

            int levelFactor = Mathf.Max(1, playerLevel / 10);

            switch (type)
            {
                case DaTauTaskType.KillNpc:
                    task.targetId = PickRandomNpc(playerLevel);
                    task.targetCount = 3 + levelFactor;
                    task.descriptionVi = $"Tiêu diệt {task.targetCount} {NpcNameVi(task.targetId)}";
                    break;
                case DaTauTaskType.FindItem:
                    task.targetId = PickRandomItem(playerLevel);
                    task.targetCount = 1 + levelFactor / 2;
                    task.descriptionVi = $"Tìm {task.targetCount} {ItemNameVi(task.targetId)}";
                    break;
                case DaTauTaskType.FindNpc:
                    task.targetId = PickRandomNpc(playerLevel);
                    task.targetCount = 1;
                    task.descriptionVi = $"Gặp gỡ {NpcNameVi(task.targetId)}";
                    break;
                case DaTauTaskType.ReachLevel:
                    task.targetId = playerLevel + 1;
                    task.targetCount = 1;
                    task.descriptionVi = $"Đạt cấp độ {task.targetId}";
                    break;
            }

            return task;
        }

        private static int PickRandomNpc(int level) => level switch
        {
            <= 10 => 300,  // Mèo Vàng
            <= 20 => 301,  // Dã Cẩu
            <= 30 => 302,  // Sói Xám
            <= 40 => 303,  // Cáp Giác
            <= 50 => 304,  // Hắc Nguyệt
            _ => 305,      // Huyết Lang
        };

        private static int PickRandomItem(int level) => level switch
        {
            <= 20 => 1001, // Tiểu Hồi Đan
            <= 40 => 1002, // Đại Hồi Đan
            _ => 1003,     // Kim Sáng Dược
        };

        private static string NpcNameVi(int id) => id switch
        {
            300 => "Mèo Vàng", 301 => "Dã Cẩu", 302 => "Sói Xám",
            303 => "Cáp Giác", 304 => "Hắc Nguyệt", 305 => "Huyết Lang",
            _ => "Quái Vật",
        };

        private static string ItemNameVi(int id) => id switch
        {
            1001 => "Tiểu Hồi Đan", 1002 => "Đại Hồi Đan", 1003 => "Kim Sáng Dược",
            _ => "Vật Phẩm",
        };
    }
}
