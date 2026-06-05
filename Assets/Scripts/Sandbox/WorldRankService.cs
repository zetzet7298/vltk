// -----------------------------------------------------------------------------
// VLTK Mobile — ST-10.18 World Rank Service (Bảng Xếp Hạng runtime)
// Quản lý các bảng xếp hạng: Cấp Độ, Tài Phú, PK, Bang, Danh Vọng.
// PC source: settings/worldrank/toplist.txt.
// Vietnamese: "Bảng Xếp Hạng", "Top Cấp Độ", "Top Tài Phú", "Top PK", "Top Bang".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>Service quản lý Bảng Xếp Hạng (World Rank runtime).</summary>
    public class WorldRankService
    {
        public const string LogTag = "WorldRank";

        private PcWorldRankRegistry _registry;

        public event Action OnRankLoaded;

        public int Count => _registry != null ? _registry.Count : 0;

        public WorldRankService() : this(null) { }

        public WorldRankService(PcWorldRankRegistry registry)
        {
            AttachRegistry(registry);
        }

        public void AttachRegistry(PcWorldRankRegistry registry)
        {
            _registry = registry ?? new PcWorldRankRegistry();
            SubsystemLog.Info(LogTag, $"Đã tải {_registry.Count} bảng xếp hạng");
            OnRankLoaded?.Invoke();
        }

        public PcWorldRankEntry GetRank(int rankType)
            => _registry != null ? _registry.Get(rankType) : null;

        public IEnumerable<PcWorldRankEntry> GetAllRanks()
            => _registry != null ? _registry.GetAll() : (IEnumerable<PcWorldRankEntry>)Array.Empty<PcWorldRankEntry>();

        /// <summary>
        /// Kiểm tra người chơi có đủ điểm để lọt vào bảng xếp hạng này không.
        /// </summary>
        public bool CanRank(int rankType, int score)
        {
            var entry = GetRank(rankType);
            if (entry == null) return false;
            if (entry.maxScore > 0 && score > entry.maxScore) return false;
            return score >= entry.minScore;
        }

        public static WorldRankService LoadFromStreamingAssets(string subdir = "Reference/PcEvent")
        {
            string dir = Path.Combine(Application.streamingAssetsPath, subdir);
            var svc = new WorldRankService();
            if (Directory.Exists(dir))
            {
                var reg = PcWorldRankParser.BuildRegistry(dir);
                svc.AttachRegistry(reg);
            }
            if (svc.Count == 0)
            {
                // Fallback root
                string rootMain = Path.Combine(Application.streamingAssetsPath, "Reference/worldrank/toplist.txt");
                if (File.Exists(rootMain))
                {
                    var reg2 = new PcWorldRankRegistry();
                    foreach (var e in PcWorldRankParser.ParseFile(rootMain)) reg2.Register(e);
                    svc.AttachRegistry(reg2);
                    return svc;
                }
                SubsystemLog.Warn(LogTag, "WorldRank: không tìm thấy toplist.txt trong StreamingAssets");
                svc.OnRankLoaded?.Invoke();
            }
            return svc;
        }
    }
}
