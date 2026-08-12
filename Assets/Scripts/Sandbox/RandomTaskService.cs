// -----------------------------------------------------------------------------
// VLTK Mobile — ST-06.6 Random Task Service
// Nhiệm vụ ngẫu nhiên runtime: tra cứu task theo id / cấp / loại.
// PC source: settings/task/random/*.txt
// Vietnamese: "Nhiệm Vụ Ngẫu Nhiên", "Giết Quái", "Thu Thập", "Hội Thoại".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class RandomTaskService
    {
        public const string LogTag = "RandomTask";
        public const string DefaultStreamingDir = "Reference/PcMission/random";

        private readonly PcRandomTaskRegistry _registry;

        public int Count => _registry?.Count ?? 0;

        public RandomTaskService() : this(null) { }

        public RandomTaskService(PcRandomTaskRegistry registry)
        {
            _registry = registry ?? new PcRandomTaskRegistry();
        }

        public static RandomTaskService LoadFromStreamingAssets(string subDir = null)
        {
            string dir = string.IsNullOrEmpty(subDir)
                ? Path.Combine(Application.streamingAssetsPath, DefaultStreamingDir)
                : Path.Combine(Application.streamingAssetsPath, subDir);
            var reg = PcRandomTaskParser.BuildRegistry(dir);
            if (reg.Count == 0)
            {
                // Fallback: tìm trong PcMission
                string alt = Path.Combine(Application.streamingAssetsPath, "Reference/PcMission");
                if (Directory.Exists(alt)) reg = PcRandomTaskParser.BuildRegistry(alt);
            }
            SubsystemLog.Info(LogTag, $"Đã tải {reg.Count} nhiệm vụ ngẫu nhiên từ {dir}");
            return new RandomTaskService(reg);
        }

        public PcRandomTaskEntry GetRandomTask(int taskId)
            => _registry.Get(taskId);

        public IReadOnlyList<PcRandomTaskEntry> GetTasksForLevel(int playerLevel)
            => _registry.GetByLevel(playerLevel);

        public IReadOnlyList<PcRandomTaskEntry> GetByType(int type)
            => _registry.GetByType(type);

        public IEnumerable<PcRandomTaskEntry> GetAll() => _registry.All;
    }
}
