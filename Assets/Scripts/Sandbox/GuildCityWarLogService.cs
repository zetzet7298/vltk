// -----------------------------------------------------------------------------
// VLTK Mobile — ST-9.10 Guild City War Log runtime service
// Source: PC settings/guildcitywarlog.txt.
// Quản lý nhật ký công thành giữa các bang hội.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class GuildCityWarLogService
    {
        public const string LogTag = "GuildCityWarLog";
        public const string DefaultStreamingDir = "Reference/PcTong";

        private PcGuildCityWarLogRegistry _registry;

        public int Count => _registry?.Count ?? 0;

        public GuildCityWarLogService() { }
        public GuildCityWarLogService(PcGuildCityWarLogRegistry registry) { _registry = registry; }

        public void RegisterRegistry(PcGuildCityWarLogRegistry reg)
        {
            _registry = reg;
            if (_registry == null) SubsystemLog.Warn(LogTag, "Guild city war log registry rỗng");
        }

        public static string GetEventTypeName(int eventType)
        {
            return eventType switch
            {
                0 => "Bắt đầu",
                1 => "Giết người",
                2 => "Phá cổng",
                3 => "Chiến thắng",
                4 => "Thất bại",
                _ => $"Không rõ ({eventType})",
            };
        }

        public PcGuildCityWarLogEntry GetLog(int logId) => _registry != null ? _registry.Get(logId) : null;
        public IReadOnlyList<PcGuildCityWarLogEntry> GetByWar(int warId)
            => _registry != null ? _registry.GetByWar(warId) : System.Array.Empty<PcGuildCityWarLogEntry>();
        public IReadOnlyList<PcGuildCityWarLogEntry> GetByCity(int cityId)
            => _registry != null ? _registry.GetByCity(cityId) : System.Array.Empty<PcGuildCityWarLogEntry>();
        public IReadOnlyList<PcGuildCityWarLogEntry> GetByTong(int tongId)
            => _registry != null ? _registry.GetByTong(tongId) : System.Array.Empty<PcGuildCityWarLogEntry>();

        public IReadOnlyList<PcGuildCityWarLogEntry> GetRecentLogs(int warId, int count)
        {
            var list = new List<PcGuildCityWarLogEntry>(GetByWar(warId));
            list.Sort((a, b) => b.eventTimeUnix.CompareTo(a.eventTimeUnix));
            if (list.Count > count) list = list.GetRange(0, count);
            return list;
        }

        public static GuildCityWarLogService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new GuildCityWarLogService();
            if (Directory.Exists(dir))
            {
                svc.RegisterRegistry(PcGuildCityWarLogParser.BuildRegistry(dir));
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"Không tìm thấy {dir}");
            }
            return svc;
        }
    }
}
