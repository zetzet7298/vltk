// -----------------------------------------------------------------------------
// VLTK Mobile — ST-09.x Guild Task Service (Nhiệm Vụ Bang runtime)
// Quản lý nhiệm vụ bang: Tiêu Boss, Thu Thập, Cống Hiến, Phòng Thành, Đấu Trường.
// PC source: settings/tong/tong_task.txt.
// Vietnamese: "Nhiệm Vụ Bang", "Tiêu Boss", "Thu Thập", "Cống Hiến".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>Service quản lý Nhiệm Vụ Bang.</summary>
    public class GuildTaskService
    {
        public const string LogTag = "GuildTask";

        private PcGuildTaskRegistry _registry;

        public event Action OnTaskLoaded;

        public int Count => _registry != null ? _registry.Count : 0;

        public GuildTaskService() : this(null) { }

        public GuildTaskService(PcGuildTaskRegistry registry)
        {
            AttachRegistry(registry);
        }

        public void AttachRegistry(PcGuildTaskRegistry registry)
        {
            _registry = registry ?? new PcGuildTaskRegistry();
            SubsystemLog.Info(LogTag, $"Đã tải {_registry.Count} nhiệm vụ bang");
            OnTaskLoaded?.Invoke();
        }

        public PcGuildTaskEntry GetTask(int taskId)
            => _registry != null ? _registry.Get(taskId) : null;

        public IReadOnlyList<PcGuildTaskEntry> GetByLevel(int guildLevel)
            => _registry != null
                ? _registry.GetByLevel(guildLevel)
                : (IReadOnlyList<PcGuildTaskEntry>)Array.Empty<PcGuildTaskEntry>();

        public IEnumerable<PcGuildTaskEntry> GetAll()
            => _registry != null ? _registry.All : (IEnumerable<PcGuildTaskEntry>)Array.Empty<PcGuildTaskEntry>();

        public static GuildTaskService LoadFromStreamingAssets(string subdir = "Reference/PcTong")
        {
            string dir = Path.Combine(Application.streamingAssetsPath, subdir);
            var svc = new GuildTaskService();
            if (Directory.Exists(dir))
            {
                var reg = PcGuildTaskParser.BuildRegistry(dir);
                svc.AttachRegistry(reg);
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"GuildTask: directory không tồn tại {dir}");
                svc.OnTaskLoaded?.Invoke();
            }
            return svc;
        }
    }
}
