// -----------------------------------------------------------------------------
// VLTK Mobile — PermitDialogNpcService: runtime service cho permitdialognpc_info.txt
// Danh sách NPC cho phép đối thoại khi người chơi ở trạng thái mệt mỏi (PC).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PermitDialogNpcService
    {
        public const string LogTag = "PermitDialogNpc";

        private readonly PcPermitDialogNpcRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public PermitDialogNpcService() { _reg = new PcPermitDialogNpcRegistry(); }
        public PermitDialogNpcService(PcPermitDialogNpcRegistry reg) { _reg = reg ?? new PcPermitDialogNpcRegistry(); }

        public void RegisterRegistry(PcPermitDialogNpcRegistry reg)
        {
            if (reg == null) return;
            foreach (var e in reg.All) _reg.Add(e);
            SubsystemLog.Info(LogTag, $"Đã tải {_reg.Count} NPC cho phép đối thoại");
        }

        public static PermitDialogNpcService LoadFromStreamingAssets(string subDir = "Reference")
        {
            var svc = new PermitDialogNpcService();
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            if (Directory.Exists(path))
            {
                svc.RegisterRegistry(PcPermitDialogNpcParser.BuildRegistry(path));
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"Directory không tồn tại {path}");
            }
            return svc;
        }

        public PcPermitDialogNpcEntry Get(int id) => _reg.Get(id);
        public IEnumerable<PcPermitDialogNpcEntry> GetByMap(int mapId)
        {
            foreach (var e in _reg.All)
            {
                if (e.MapId == mapId) yield return e;
            }
        }
        public IEnumerable<PcPermitDialogNpcEntry> GetAll() => _reg.All;
    }
}
