// -----------------------------------------------------------------------------
// VLTK Mobile — UtilitiesService: runtime service cho utilities.ini
// Cấu hình tiện ích hệ thống: ngụy trang, cấm tính năng (PC reference).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class UtilitiesService
    {
        public const string LogTag = "Utilities";

        private readonly PcUtilitiesRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public UtilitiesService() { _reg = new PcUtilitiesRegistry(); }
        public UtilitiesService(PcUtilitiesRegistry reg) { _reg = reg ?? new PcUtilitiesRegistry(); }

        public void RegisterRegistry(PcUtilitiesRegistry reg)
        {
            if (reg == null) return;
            foreach (var e in reg.All) _reg.Add(e);
            SubsystemLog.Info(LogTag, $"Đã tải {_reg.Count} section utilities");
        }

        public static UtilitiesService LoadFromStreamingAssets(string subDir = "Reference")
        {
            var svc = new UtilitiesService();
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            if (Directory.Exists(path))
            {
                svc.RegisterRegistry(PcUtilitiesParser.BuildRegistry(path));
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"Directory không tồn tại {path}");
            }
            return svc;
        }

        public PcUtilitiesEntry GetDisguiseMask() => _reg.Get("DisguiseMask");
        public IEnumerable<PcUtilitiesEntry> GetAll() => _reg.All;
    }
}
