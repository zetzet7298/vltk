// -----------------------------------------------------------------------------
// VLTK Mobile — ST-09.x Guild Rank Service (Cấp Bậc Bang runtime)
// Quản lý cấp bậc thành viên bang (Bang Chủ, Trưởng Lão, Thành Viên).
// PC source: settings/tong/tong_rank.txt (5 ranks).
// Vietnamese: "Cấp Bậc", "Bang Chủ", "Trưởng Lão", "Thành Viên", "Lương Tuần".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>Service quản lý cấp bậc bang hội (Bang Chủ, Trưởng Lão, Thành Viên).</summary>
    public class GuildRankService
    {
        public const string LogTag = "GuildRank";

        private PcGuildRankRegistry _registry;

        /// <summary>Sự kiện khi registry sẵn sàng.</summary>
        public event Action OnRankLoaded;

        public int Count => _registry != null ? _registry.Count : 0;

        public GuildRankService() : this(null) { }

        public GuildRankService(PcGuildRankRegistry registry)
        {
            AttachRegistry(registry);
        }

        public void AttachRegistry(PcGuildRankRegistry registry)
        {
            _registry = registry ?? new PcGuildRankRegistry();
            SubsystemLog.Info(LogTag, $"Đã tải {_registry.Count} cấp bậc bang");
            OnRankLoaded?.Invoke();
        }

        public PcGuildRankEntry GetRank(int rank)
            => _registry != null ? _registry.Get(rank) : null;

        public IEnumerable<PcGuildRankEntry> GetAllRanks()
            => _registry != null ? _registry.GetAll() : (IEnumerable<PcGuildRankEntry>)Array.Empty<PcGuildRankEntry>();

        public IReadOnlyList<PcGuildRankEntry> GetByAuthority(int auth)
            => _registry != null
                ? _registry.GetByAuthority(auth)
                : (IReadOnlyList<PcGuildRankEntry>)Array.Empty<PcGuildRankEntry>();

        public static GuildRankService LoadFromStreamingAssets(string subdir = "Reference/PcTong")
        {
            string dir = Path.Combine(Application.streamingAssetsPath, subdir);
            var svc = new GuildRankService();
            if (Directory.Exists(dir))
            {
                var reg = PcGuildRankParser.BuildRegistry(dir);
                svc.AttachRegistry(reg);
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"GuildRank: directory không tồn tại {dir}");
                svc.OnRankLoaded?.Invoke();
            }
            return svc;
        }
    }
}
