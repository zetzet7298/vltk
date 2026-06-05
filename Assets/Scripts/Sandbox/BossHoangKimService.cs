// -----------------------------------------------------------------------------
// VLTK Mobile — ST-08.13 Boss Hoàng Kim Service (Boss spawn runtime)
// Wraps PcBossHoangKimRegistry. PC source: settings/boss/bosshoangkim.txt.
// Vietnamese: "Boss Hoàng Kim", "Hồi Sinh", "Rơi Đồ", "Tọa Độ".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class BossHoangKimService
    {
        public const string LogTag = "BossHoangKim";
        public const string DefaultStreamingDir = "Reference/PcBoss";

        private PcBossHoangKimRegistry _registry;

        public event Action OnBossLoaded;

        public int Count => _registry != null ? _registry.Count : 0;

        public BossHoangKimService() { }
        public BossHoangKimService(PcBossHoangKimRegistry registry) { AttachRegistry(registry); }

        public void AttachRegistry(PcBossHoangKimRegistry registry)
        {
            _registry = registry ?? new PcBossHoangKimRegistry();
            SubsystemLog.Info(LogTag, $"Đã tải {_registry.Count} boss Hoàng Kim");
            OnBossLoaded?.Invoke();
        }

        public PcBossHoangKimEntry GetBoss(int id)
            => _registry != null ? _registry.Get(id) : null;

        public IReadOnlyList<PcBossHoangKimEntry> GetByMap(int mapId)
            => _registry != null ? _registry.GetByMap(mapId) : Array.Empty<PcBossHoangKimEntry>();

        /// <summary>Tính thời điểm hồi sinh kế tiếp cho boss. Trả về now + respawnSec.</summary>
        public DateTime ComputeRespawnTime(int bossId, DateTime? killedAt = null)
        {
            var b = GetBoss(bossId);
            if (b == null) return DateTime.MinValue;
            var start = killedAt ?? DateTime.UtcNow;
            return start.AddSeconds(Math.Max(0, b.respawnSec));
        }

        /// <summary>Lọc boss hiện đang hoạt động (chưa tới giờ hồi sinh).</summary>
        public IReadOnlyList<PcBossHoangKimEntry> GetActiveBosses(
            DateTime now,
            IReadOnlyDictionary<int, DateTime> lastDeathUtc = null)
        {
            if (_registry == null) return Array.Empty<PcBossHoangKimEntry>();
            var list = new List<PcBossHoangKimEntry>();
            foreach (var b in _registry.All)
            {
                if (lastDeathUtc != null
                    && lastDeathUtc.TryGetValue(b.bossId, out var death)
                    && death.AddSeconds(b.respawnSec) > now)
                    continue; // Chưa hồi sinh
                list.Add(b);
            }
            return list;
        }

        public static BossHoangKimService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new BossHoangKimService();
            if (Directory.Exists(dir))
            {
                var reg = PcBossHoangKimParser.BuildRegistry(dir);
                svc.AttachRegistry(reg);
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"Boss Hoàng Kim: directory không tồn tại {dir}");
                svc.OnBossLoaded?.Invoke();
            }
            return svc;
        }
    }
}
