// -----------------------------------------------------------------------------
// VLTK Mobile — ST-06.8 Chuyển Sinh Task Service
// Nhiệm vụ chuyển sinh: cấp yêu cầu + số lần chuyển sinh, thưởng skill/title.
// PC source: settings/task/metempsychosis/*.txt
// Vietnamese: "Chuyển Sinh", "Luyện Công", "Độ Kiếp", "Khai Thiên".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class MetempsychosisTaskService
    {
        public const string LogTag = "Metempsychosis";
        public const string DefaultStreamingDir = "Reference/PcMission/metempsychosis";

        private readonly PcMetempsychosisTaskRegistry _registry;

        public int Count => _registry?.Count ?? 0;

        public MetempsychosisTaskService() : this(null) { }

        public MetempsychosisTaskService(PcMetempsychosisTaskRegistry registry)
        {
            _registry = registry ?? new PcMetempsychosisTaskRegistry();
        }

        public static MetempsychosisTaskService LoadFromStreamingAssets(string subDir = null)
        {
            string dir = string.IsNullOrEmpty(subDir)
                ? Path.Combine(Application.streamingAssetsPath, DefaultStreamingDir)
                : Path.Combine(Application.streamingAssetsPath, subDir);
            var reg = PcMetempsychosisTaskParser.BuildRegistry(dir);
            SubsystemLog.Info(LogTag, $"Đã tải {reg.Count} nhiệm vụ chuyển sinh từ {dir}");
            return new MetempsychosisTaskService(reg);
        }

        public PcMetempsychosisTaskEntry GetTask(int taskId)
            => _registry.Get(taskId);

        public IReadOnlyList<PcMetempsychosisTaskEntry> GetTasksForLevel(int playerLevel)
            => _registry.GetByLevel(playerLevel);

        public IEnumerable<PcMetempsychosisTaskEntry> GetAll() => _registry.All;
    }
}
