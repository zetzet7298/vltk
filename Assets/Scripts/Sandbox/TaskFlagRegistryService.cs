// -----------------------------------------------------------------------------
// VLTK Mobile — ST-6.3 Task Flag Registry Service (Bảng Cờ Nhiệm Vụ)
// Wraps TaskFlagConfigRegistry. PC source: taskflagconfig.txt (29 cờ).
// Vietnamese: "Cờ Nhiệm Vụ", "Chính Tuyến", "Phụ Tuyến", "Hằng Ngày",
//             "Tuần Hoàn", "Môn Phái", "Bang Hội", "Sự Kiện", "Tu Luyện".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service runtime quản lý cấu hình cờ nhiệm vụ + validate điều kiện nhận.
    /// </summary>
    public class TaskFlagRegistryService
    {
        public const string LogTag = "TaskFlagRegistry";
        public const string DefaultStreamingDir = "Reference/PcTask";

        private static readonly Dictionary<int, string> _typeNames = new()
        {
            { 0, "Chính Tuyến" },
            { 1, "Phụ Tuyến" },
            { 2, "Hằng Ngày" },
            { 3, "Tuần Hoàn" },
            { 4, "Môn Phái" },
            { 5, "Bang Hội" },
            { 6, "Sự Kiện" },
            { 7, "Tu Luyện" },
        };

        private static readonly Dictionary<int, string> _categoryNames = new()
        {
            { 0, "Khởi Đầu" },
            { 1, "Tân Thủ" },
            { 2, "Trung Cấp" },
            { 3, "Cao Cấp" },
            { 4, "Cuối Cùng" },
            { 5, "Đặc Biệt" },
            { 6, "Hằng Ngày" },
            { 7, "Tuần Hoàn" },
        };

        private TaskFlagConfigRegistry _registry;

        public int Count => _registry != null ? _registry.Count : 0;

        public TaskFlagRegistryService() { }
        public TaskFlagRegistryService(TaskFlagConfigRegistry reg) { _registry = reg; }

        public void AttachRegistry(TaskFlagConfigRegistry reg)
        {
            _registry = reg ?? new TaskFlagConfigRegistry();
            SubsystemLog.Info(LogTag, $"TaskFlagRegistry loaded: {Count} cờ");
        }

        public TaskFlagConfigEntry GetFlag(int flagId)
            => _registry != null ? _registry.Get(flagId) : null;

        public IReadOnlyList<TaskFlagConfigEntry> GetByType(int taskType)
            => _registry != null
                ? _registry.GetByType(taskType)
                : (IReadOnlyList<TaskFlagConfigEntry>)System.Array.Empty<TaskFlagConfigEntry>();

        public IReadOnlyList<TaskFlagConfigEntry> GetByCategory(int categoryId)
            => _registry != null
                ? _registry.GetByCategory(categoryId)
                : (IReadOnlyList<TaskFlagConfigEntry>)System.Array.Empty<TaskFlagConfigEntry>();

        public IReadOnlyList<TaskFlagConfigEntry> All
            => _registry != null
                ? (IReadOnlyList<TaskFlagConfigEntry>)new List<TaskFlagConfigEntry>(_registry.All)
                : (IReadOnlyList<TaskFlagConfigEntry>)System.Array.Empty<TaskFlagConfigEntry>();

        public bool CanAccept(int flagId, int playerLevel)
        {
            var entry = GetFlag(flagId);
            if (entry == null) return false;
            if (entry.reqLevel > 0 && playerLevel < entry.reqLevel) return false;
            return true;
        }

        public string GetFlagTypeName(int taskType)
            => _typeNames.TryGetValue(taskType, out var name) ? name : "Không Rõ";

        public string GetFlagCategoryName(int categoryId)
            => _categoryNames.TryGetValue(categoryId, out var name) ? name : "Không Rõ";

        public static TaskFlagRegistryService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new TaskFlagRegistryService();
            if (Directory.Exists(dir))
            {
                var reg = PcTaskFlagConfigParser.BuildRegistry(dir);
                svc.AttachRegistry(reg);
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"TaskFlagRegistry dir không tồn tại {dir}");
            }
            return svc;
        }
    }
}
