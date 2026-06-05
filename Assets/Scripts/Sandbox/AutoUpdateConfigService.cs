// -----------------------------------------------------------------------------
// VLTK Mobile — AutoUpdateConfigService: runtime service cho autoupdate.ini
// Quản lý danh sách FTP site cho auto-update client (PC reference).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class AutoUpdateConfigService
    {
        public const string LogTag = "AutoUpdateCfg";

        private readonly PcAutoUpdateRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public AutoUpdateConfigService() { _reg = new PcAutoUpdateRegistry(); }
        public AutoUpdateConfigService(PcAutoUpdateRegistry reg) { _reg = reg ?? new PcAutoUpdateRegistry(); }

        public void RegisterRegistry(PcAutoUpdateRegistry reg)
        {
            if (reg == null) return;
            foreach (var e in reg.All) _reg.Add(e);
            SubsystemLog.Info(LogTag, $"Đã tải {_reg.Count} section auto-update");
        }

        public static AutoUpdateConfigService LoadFromStreamingAssets(string subDir = "Reference")
        {
            var svc = new AutoUpdateConfigService();
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            if (Directory.Exists(path))
            {
                svc.RegisterRegistry(PcAutoUpdateParser.BuildRegistry(path));
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"Directory không tồn tại {path}");
            }
            return svc;
        }

        public PcAutoUpdateEntry GetSection(string section) => _reg.Get(section);
        public IEnumerable<PcAutoUpdateEntry> GetAll() => _reg.All;
    }
}
