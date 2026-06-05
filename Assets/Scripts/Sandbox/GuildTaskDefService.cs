// -----------------------------------------------------------------------------
// VLTK Mobile — GuildTaskDefService: runtime service cho định nghĩa nhiệm vụ bang
// Quản lý các ID nhiệm vụ bang (tong, member, controlhelp, workshop).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class GuildTaskDefService
    {
        public const string LogTag = "GuildTaskDef";

        private readonly PcGuildTaskDefRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public GuildTaskDefService() { _reg = new PcGuildTaskDefRegistry(); }
        public GuildTaskDefService(PcGuildTaskDefRegistry reg) { _reg = reg ?? new PcGuildTaskDefRegistry(); }

        public void RegisterRegistry(PcGuildTaskDefRegistry reg)
        {
            if (reg == null) return;
            foreach (var e in reg.All) _reg.Add(e);
            SubsystemLog.Info(LogTag, $"Đã tải {_reg.Count} định nghĩa nhiệm vụ bang");
        }

        public static GuildTaskDefService LoadFromStreamingAssets(string subDir = "Reference/PcTong/task")
        {
            var svc = new GuildTaskDefService();
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            if (Directory.Exists(path))
            {
                svc.RegisterRegistry(PcGuildTaskDefParser.BuildRegistry(path));
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"Directory không tồn tại {path}");
            }
            return svc;
        }

        public IEnumerable<PcGuildTaskDefEntry> GetAll() => _reg.All;
        public IEnumerable<PcGuildTaskDefEntry> FindById(int taskId) => _reg.FindById(taskId);
    }
}
