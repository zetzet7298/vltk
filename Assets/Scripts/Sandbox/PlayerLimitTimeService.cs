// -----------------------------------------------------------------------------
// VLTK Mobile — PlayerLimitTimeService: runtime service cho player_limittime.ini
// Cấu hình giới hạn thời gian chơi game cho người chơi (PC).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PlayerLimitTimeService
    {
        public const string LogTag = "PlayerLimit";

        private readonly PcPlayerLimitRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public PlayerLimitTimeService() { _reg = new PcPlayerLimitRegistry(); }
        public PlayerLimitTimeService(PcPlayerLimitRegistry reg) { _reg = reg ?? new PcPlayerLimitRegistry(); }

        public void RegisterRegistry(PcPlayerLimitRegistry reg)
        {
            if (reg == null) return;
            foreach (var e in reg.All) _reg.Add(e);
            SubsystemLog.Info(LogTag, $"Đã tải {_reg.Count} section giới hạn thời gian");
        }

        public static PlayerLimitTimeService LoadFromStreamingAssets(string subDir = "Reference")
        {
            var svc = new PlayerLimitTimeService();
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            if (Directory.Exists(path))
            {
                svc.RegisterRegistry(PcPlayerLimitParser.BuildRegistry(path));
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"Directory không tồn tại {path}");
            }
            return svc;
        }

        public PcPlayerLimitEntry GetConfig() => _reg.Get("Config");
        public PcPlayerLimitEntry GetLimitTime() => _reg.Get("LimitTime");
        public IEnumerable<PcPlayerLimitEntry> GetAll() => _reg.All;
    }
}
