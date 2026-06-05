// -----------------------------------------------------------------------------
// VLTK Mobile — GuildWorkshopLevelService: runtime service cho công trình bang theo cấp
// Quản lý thông tin cấp độ công trình bang (Khu Binh Giáp, Thiên Công, Mặt Nạ, Luyện Tập, Thiên Ý, Lễ Vật, Hoạt Động).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class GuildWorkshopLevelService
    {
        public const string LogTag = "GuildWorkshopLevel";

        private readonly PcGuildWorkshopLevelRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public GuildWorkshopLevelService() { _reg = new PcGuildWorkshopLevelRegistry(); }
        public GuildWorkshopLevelService(PcGuildWorkshopLevelRegistry reg) { _reg = reg ?? new PcGuildWorkshopLevelRegistry(); }

        public void RegisterRegistry(PcGuildWorkshopLevelRegistry reg)
        {
            if (reg == null) return;
            foreach (var e in reg.All) _reg.Add(e);
            SubsystemLog.Info(LogTag, $"Đã tải {_reg.Count} loại công trình bang");
        }

        public static GuildWorkshopLevelService LoadFromStreamingAssets(string subDir = "Reference/PcTong/workshop")
        {
            var svc = new GuildWorkshopLevelService();
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            if (Directory.Exists(path))
            {
                svc.RegisterRegistry(PcGuildWorkshopLevelParser.BuildRegistry(path));
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"Directory không tồn tại {path}");
            }
            return svc;
        }

        public PcGuildWorkshopLevelEntry GetWorkshop(int type) => _reg.Get(type);
        public IEnumerable<PcGuildWorkshopLevelEntry> GetAll() => _reg.All;

        public PcGuildWorkshopLevelData GetLevelData(int type, int level)
        {
            var entry = _reg.Get(type);
            if (entry == null) return null;
            foreach (var lv in entry.Levels)
            {
                if (lv.Level == level) return lv;
            }
            return null;
        }
    }
}
