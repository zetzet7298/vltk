// -----------------------------------------------------------------------------
// VLTK Mobile — ST-06.7 Partner Task Service
// Nhiệm vụ pet (đồng hành) runtime: tra cứu theo pet / cấp nhân vật.
// PC source: settings/task/partner/partner_task_def.txt
// Vietnamese: "Nhiệm Vụ Đồng Hành", "Thú Cưng", "Phần Thưởng Pet".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PartnerTaskService
    {
        public const string LogTag = "PartnerTask";
        public const string DefaultStreamingDir = "Reference/PcPartner";

        private readonly PcPartnerTaskRegistry _registry;

        public int Count => _registry?.Count ?? 0;

        public PartnerTaskService(PcPartnerTaskRegistry registry)
        {
            _registry = registry ?? new PcPartnerTaskRegistry();
        }

        public static PartnerTaskService LoadFromStreamingAssets(string subDir = null)
        {
            string dir = string.IsNullOrEmpty(subDir)
                ? Path.Combine(Application.streamingAssetsPath, DefaultStreamingDir)
                : Path.Combine(Application.streamingAssetsPath, subDir);
            var reg = PcPartnerTaskParser.BuildRegistry(dir);
            SubsystemLog.Info(LogTag, $"Đã tải {reg.Count} nhiệm vụ pet từ {dir}");
            return new PartnerTaskService(reg);
        }

        public PcPartnerTaskEntry GetPartnerTask(int taskId)
            => _registry.Get(taskId);

        public IReadOnlyList<PcPartnerTaskEntry> GetTasksForPartner(int partnerId)
            => _registry.GetByPartner(partnerId);

        public IReadOnlyList<PcPartnerTaskEntry> GetTasksForLevel(int playerLevel)
            => _registry.GetByLevel(playerLevel);

        public IEnumerable<PcPartnerTaskEntry> GetAll() => _registry.All;
    }
}
