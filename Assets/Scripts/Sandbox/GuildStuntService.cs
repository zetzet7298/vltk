// -----------------------------------------------------------------------------
// VLTK Mobile — ST-09.x Guild Stunt Service (Kỹ Năng Bang runtime)
// Quản lý kỹ năng đặc biệt của bang (Phượng Hoàng Ấn, ...).
// PC source: settings/tong/tongstunt_setting.txt.
// Vietnamese: "Kỹ Năng Bang", "Bảo Trì", "Phượng Hoàng Ấn".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>Service quản lý Kỹ Năng Bang (Stunt Skill runtime).</summary>
    public class GuildStuntService
    {
        public const string LogTag = "GuildStunt";

        private PcGuildStuntRegistry _registry;

        public event Action OnStuntLoaded;

        public int Count => _registry != null ? _registry.Count : 0;

        public GuildStuntService() : this(null) { }

        public GuildStuntService(PcGuildStuntRegistry registry)
        {
            AttachRegistry(registry);
        }

        public void AttachRegistry(PcGuildStuntRegistry registry)
        {
            _registry = registry ?? new PcGuildStuntRegistry();
            SubsystemLog.Info(LogTag, $"Đã tải {_registry.Count} kỹ năng bang");
            OnStuntLoaded?.Invoke();
        }

        public PcGuildStuntEntry GetStunt(int stuntId)
            => _registry != null ? _registry.Get(stuntId) : null;

        public IReadOnlyList<PcGuildStuntEntry> GetForLevel(int guildLevel)
            => _registry != null
                ? _registry.GetForLevel(guildLevel)
                : (IReadOnlyList<PcGuildStuntEntry>)Array.Empty<PcGuildStuntEntry>();

        public IEnumerable<PcGuildStuntEntry> GetAll()
            => _registry != null ? _registry.All : (IEnumerable<PcGuildStuntEntry>)Array.Empty<PcGuildStuntEntry>();

        public static GuildStuntService LoadFromStreamingAssets(string subdir = "Reference/PcTong")
        {
            string dir = Path.Combine(Application.streamingAssetsPath, subdir);
            var svc = new GuildStuntService();
            if (Directory.Exists(dir))
            {
                var reg = PcGuildStuntParser.BuildRegistry(dir);
                svc.AttachRegistry(reg);
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"GuildStunt: directory không tồn tại {dir}");
                svc.OnStuntLoaded?.Invoke();
            }
            return svc;
        }
    }
}
