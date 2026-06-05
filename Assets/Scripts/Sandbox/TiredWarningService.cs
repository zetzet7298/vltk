// -----------------------------------------------------------------------------
// VLTK Mobile — TiredWarningService: runtime service cho tiredwarning.ini
// Cấu hình cảnh báo mệt mỏi của hệ thống phòng chống nghiện game (PC).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class TiredWarningService
    {
        public const string LogTag = "TiredWarning";

        private readonly PcTiredWarningRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public TiredWarningService() { _reg = new PcTiredWarningRegistry(); }
        public TiredWarningService(PcTiredWarningRegistry reg) { _reg = reg ?? new PcTiredWarningRegistry(); }

        public void RegisterRegistry(PcTiredWarningRegistry reg)
        {
            if (reg == null) return;
            foreach (var e in reg.All) _reg.Add(e);
            SubsystemLog.Info(LogTag, $"Đã tải {_reg.Count} section cảnh báo mệt");
        }

        public static TiredWarningService LoadFromStreamingAssets(string subDir = "Reference")
        {
            var svc = new TiredWarningService();
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            if (Directory.Exists(path))
            {
                svc.RegisterRegistry(PcTiredWarningParser.BuildRegistry(path));
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"Directory không tồn tại {path}");
            }
            return svc;
        }

        public PcTiredWarningEntry GetConfig() => _reg.Get("Config");
        public string Get(string key) => GetConfig()?.KeyValues.TryGetValue(key, out var v) == true ? v : null;
        public IEnumerable<PcTiredWarningEntry> GetAll() => _reg.All;
    }
}
