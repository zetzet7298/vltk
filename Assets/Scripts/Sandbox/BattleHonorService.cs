// -----------------------------------------------------------------------------
// VLTK Mobile — ST-8.x Battle Honor Service
// Quản lý vinh danh chiến trường. Reference: battlehonor.txt.
// Vietnamese: "Vinh Danh Chiến Trường", "Điểm", "Danh Hiệu Thưởng".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý vinh danh chiến trường.
    /// </summary>
    public class BattleHonorService
    {
        public const string LogTag = "BattleHonor";
        public const string DefaultStreamingDir = "Reference/PcBattlefield";

        private PcBattleHonorRegistry _registry;
        public int Count => _registry?.Count ?? 0;

        public BattleHonorService() { }
        public BattleHonorService(PcBattleHonorRegistry registry) { _registry = registry; }

        public void RegisterRegistry(PcBattleHonorRegistry reg)
        {
            _registry = reg ?? new PcBattleHonorRegistry();
            if (_registry.Count == 0) SubsystemLog.Warn(LogTag, "Vinh danh chiến trường rỗng");
        }

        public static BattleHonorService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new BattleHonorService();
            var reg = PcBattleHonorParser.BuildRegistry(dir);
            svc.RegisterRegistry(reg);
            SubsystemLog.Info(LogTag, $"Đã tải {reg.Count} mục vinh danh");
            return svc;
        }

        public PcBattleHonorEntry GetHonor(int honorId)
            => _registry != null ? _registry.Get(honorId) : null;

        public IReadOnlyList<PcBattleHonorEntry> GetByBattleType(int battleType)
            => _registry != null ? _registry.GetByBattleType(battleType) : Array.Empty<PcBattleHonorEntry>();

        /// <summary>Lấy vinh danh cao nhất đạt được với số điểm.</summary>
        public PcBattleHonorEntry GetHonorForScore(int battleType, int score)
        {
            if (_registry == null || score <= 0) return null;
            PcBattleHonorEntry best = null;
            foreach (var e in _registry.GetByBattleType(battleType))
            {
                if (e.requiredScore <= score)
                {
                    if (best == null || e.requiredScore > best.requiredScore) best = e;
                }
            }
            return best;
        }

        /// <summary>Lấy tất cả vinh danh có thể đạt được với battle type này.</summary>
        public IReadOnlyList<PcBattleHonorEntry> GetAvailableHonors(int battleType)
            => GetByBattleType(battleType);
    }
}
