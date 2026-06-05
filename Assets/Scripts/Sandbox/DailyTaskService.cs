// -----------------------------------------------------------------------------
// VLTK Mobile — ST-06.5 Daily Task Service (Nhiệm Vụ Hàng Ngày)
// PC source: task/dailytask/dailytask.txt — nhiệm vụ hàng ngày theo cấp.
// Vietnamese: "Nhiệm Vụ Hàng Ngày", "Phần Thưởng", "Điều Kiện Nhận".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public enum DailyTaskType
    {
        Kill = 0,        // Giết quái
        Collect = 1,     // Thu thập vật phẩm
        Talk = 2,        // Nói chuyện NPC
        Visit = 3,       // Đến địa điểm
    }

    /// <summary>
    /// Service quản lý nhiệm vụ hàng ngày. Lọc theo cấp nhân vật, theo loại nhiệm vụ.
    /// </summary>
    public class DailyTaskService
    {
        public const string DefaultStreamingDir = "Reference/PcDailyTask";
        public const string LogTag = "DailyTask";

        private readonly PcDailyTaskRegistry _registry;

        public event Action<PcDailyTaskEntry> OnDailyTaskAccepted;
        public event Action<PcDailyTaskEntry> OnDailyTaskCompleted;

        public int Count => _registry?.Count ?? 0;

        public DailyTaskService(PcDailyTaskRegistry registry)
        {
            _registry = registry ?? new PcDailyTaskRegistry();
        }

        public PcDailyTaskEntry GetDailyTask(int taskId)
            => _registry?.Get(taskId);

        public IReadOnlyList<PcDailyTaskEntry> GetAllDailyTasks()
            => _registry != null
                ? (IReadOnlyList<PcDailyTaskEntry>)new List<PcDailyTaskEntry>(_registry.All)
                : Array.Empty<PcDailyTaskEntry>();

        public IReadOnlyList<PcDailyTaskEntry> GetTasksForLevel(int playerLevel)
            => _registry?.GetByLevel(playerLevel)
                ?? (IReadOnlyList<PcDailyTaskEntry>)Array.Empty<PcDailyTaskEntry>();

        public IReadOnlyList<PcDailyTaskEntry> GetTasksByType(int type)
            => _registry?.GetByType(type)
                ?? (IReadOnlyList<PcDailyTaskEntry>)Array.Empty<PcDailyTaskEntry>();

        public IReadOnlyList<PcDailyTaskEntry> GetTasksByType(DailyTaskType type)
            => GetTasksByType((int)type);

        /// <summary>Kiểm tra nhân vật có thể nhận nhiệm vụ (đúng cấp).</summary>
        public bool CanAccept(int taskId, int playerLevel)
        {
            var entry = GetDailyTask(taskId);
            if (entry == null) return false;
            if (entry.minLevel > 0 && playerLevel < entry.minLevel) return false;
            if (entry.maxLevel > 0 && playerLevel > entry.maxLevel) return false;
            return true;
        }

        public bool Accept(int taskId, int playerLevel)
        {
            if (!CanAccept(taskId, playerLevel)) return false;
            var entry = GetDailyTask(taskId);
            SubsystemLog.Info(LogTag,
                $"Nhận nhiệm vụ hàng ngày #{taskId} (loại {entry.taskType}, mục tiêu {entry.targetId} x{entry.targetCount})");
            OnDailyTaskAccepted?.Invoke(entry);
            return true;
        }

        public bool Complete(int taskId)
        {
            var entry = GetDailyTask(taskId);
            if (entry == null) return false;
            SubsystemLog.Info(LogTag,
                $"Hoàn thành nhiệm vụ hàng ngày #{taskId} → +{entry.rewardExp} EXP, +{entry.rewardSilver} bạc");
            OnDailyTaskCompleted?.Invoke(entry);
            return true;
        }

        /// <summary>Load từ StreamingAssets.</summary>
        public static DailyTaskService LoadFromStreamingAssets(string subDir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subDir) ? DefaultStreamingDir : subDir);
            var reg = PcDailyTaskParser.BuildRegistry(dir);
            SubsystemLog.Info(LogTag, $"DailyTaskService loaded {reg.Count} nhiệm vụ hàng ngày từ {dir}");
            return new DailyTaskService(reg);
        }
    }
}
