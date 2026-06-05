// -----------------------------------------------------------------------------
// VLTK Mobile — ForbitHeartService: runtime service cho forbitheart.txt
// Danh sách map cấm sử dụng "tâm pháp" (heart skill) trong trận chiến (PC).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class ForbitHeartService
    {
        public const string LogTag = "ForbitHeart";

        private readonly PcForbitHeartRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public ForbitHeartService() { _reg = new PcForbitHeartRegistry(); }
        public ForbitHeartService(PcForbitHeartRegistry reg) { _reg = reg ?? new PcForbitHeartRegistry(); }

        public void RegisterRegistry(PcForbitHeartRegistry reg)
        {
            if (reg == null) return;
            foreach (var e in reg.All) _reg.Add(e);
            SubsystemLog.Info(LogTag, $"Đã tải {_reg.Count} map cấm tâm pháp");
        }

        public static ForbitHeartService LoadFromStreamingAssets(string subDir = "Reference")
        {
            var svc = new ForbitHeartService();
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            if (Directory.Exists(path))
            {
                svc.RegisterRegistry(PcForbitHeartParser.BuildRegistry(path));
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"Directory không tồn tại {path}");
            }
            return svc;
        }

        public PcForbitHeartEntry Get(int mapId) => _reg.Get(mapId);
        public bool IsForbit(int mapId) => _reg.Get(mapId) != null;
        public IEnumerable<PcForbitHeartEntry> GetAll() => _reg.All;
    }
}
