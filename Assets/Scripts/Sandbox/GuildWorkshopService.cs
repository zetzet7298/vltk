// -----------------------------------------------------------------------------
// VLTK Mobile — ST-09.x Guild Workshop Service (Công trình bang runtime)
// Wraps PcGuildWorkshopRegistry. PC source: settings/tong/tong_workshop.txt.
// Vietnamese: "Công Trình Bang", "Kho", "Đại Sảnh", "Luyện Đồ", "Phòng Chữa", "Chuồng Ngựa".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class GuildWorkshopService
    {
        public const string LogTag = "GuildWorkshop";

        private PcGuildWorkshopRegistry _registry;

        public event Action OnWorkshopLoaded;

        public int Count => _registry != null ? _registry.Count : 0;

public GuildWorkshopService() : this(null) { }
                public GuildWorkshopService(PcGuildWorkshopRegistry registry) { AttachRegistry(registry); }

        public void AttachRegistry(PcGuildWorkshopRegistry registry)
        {
            _registry = registry ?? new PcGuildWorkshopRegistry();
            SubsystemLog.Info(LogTag, $"Đã tải {_registry.Count} mục công trình bang");
            OnWorkshopLoaded?.Invoke();
        }

        public PcGuildWorkshopEntry GetWorkshop(int level)
            => _registry != null ? _registry.Get(level) : null;

        public PcGuildWorkshopEntry GetWorkshop(int level, int type)
            => _registry != null ? _registry.Get(level, type) : null;

        public IReadOnlyList<PcGuildWorkshopEntry> GetByType(int type)
            => _registry != null
                ? _registry.GetByType(type)
                : (IReadOnlyList<PcGuildWorkshopEntry>)Array.Empty<PcGuildWorkshopEntry>();

        public IEnumerable<PcGuildWorkshopEntry> GetAll()
            => _registry != null ? _registry.All : (IEnumerable<PcGuildWorkshopEntry>)Array.Empty<PcGuildWorkshopEntry>();

        public static GuildWorkshopService LoadFromStreamingAssets(string subdir = "Reference/PcTong")
        {
            string dir = Path.Combine(Application.streamingAssetsPath, subdir);
            var svc = new GuildWorkshopService(new PcGuildWorkshopRegistry());
            if (Directory.Exists(dir))
            {
                var reg = PcGuildWorkshopParser.BuildRegistry(dir);
                svc.AttachRegistry(reg);
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"GuildWorkshop: directory không tồn tại {dir}");
                svc.OnWorkshopLoaded?.Invoke();
            }
            return svc;
        }
    }
}
