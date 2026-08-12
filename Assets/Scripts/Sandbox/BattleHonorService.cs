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
        private IBattleHonorServiceHost _host;
        public int Count => _registry?.Count ?? 0;

        public BattleHonorService() { }
        public BattleHonorService(PcBattleHonorRegistry registry) { _registry = registry; }

        public void AttachHost(IBattleHonorServiceHost host) { _host = host; }

        public void RegisterRegistry(PcBattleHonorRegistry reg)
        {
            _registry = reg ?? new PcBattleHonorRegistry();
            if (_registry.Count == 0)
            {
                SubsystemLog.Warn(LogTag, "Vinh danh chiến trường rỗng");
                if (_host != null) _host.OnBattleHonorRegistryEmpty();
            }
            else if (_host != null)
            {
                _host.OnBattleHonorRegistryAttached(_registry.Count);
                _host.LogBattleHonorEvent("load", 0, $"Loaded {_registry.Count} honors");
                _host.PlayBattleHonorSFX("load", 0);
            }
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
        {
            var e = _registry != null ? _registry.Get(honorId) : null;
            if (_host != null)
            {
                if (e != null)
                    _host.OnHonorResolved(e.honorId, e.battleType, e.name, e.requiredScore, e.bonusTitle);
                else
                    _host.LogBattleHonorEvent("query_missing", honorId, "Honor not found in registry");
            }
            return e;
        }

        public IReadOnlyList<PcBattleHonorEntry> GetByBattleType(int battleType)
        {
            var list = _registry != null ? _registry.GetByBattleType(battleType) : Array.Empty<PcBattleHonorEntry>();
            if (_host != null)
                _host.OnHonorsByBattleTypeQueried(battleType, list.Count);
            return list;
        }

        /// <summary>Lấy vinh danh cao nhất đạt được với số điểm.</summary>
        public PcBattleHonorEntry GetHonorForScore(int battleType, int score)
        {
            if (_registry == null || score <= 0)
            {
                if (_host != null) _host.OnHonorForScoreQueried(battleType, score, 0, 0, false);
                return null;
            }
            PcBattleHonorEntry best = null;
            foreach (var e in _registry.GetByBattleType(battleType))
            {
                if (e.requiredScore <= score)
                {
                    if (best == null || e.requiredScore > best.requiredScore) best = e;
                }
            }
            if (_host != null)
                _host.OnHonorForScoreQueried(battleType, score, best?.honorId ?? 0, best?.requiredScore ?? 0, best != null);
            return best;
        }

        /// <summary>Lấy tất cả vinh danh có thể đạt được với battle type này.</summary>
        public IReadOnlyList<PcBattleHonorEntry> GetAvailableHonors(int battleType) => GetByBattleType(battleType);

        /// <summary>Đánh dấu player đạt được vinh danh với điểm hiện tại.</summary>
        public void EarnHonor(int honorId, int currentScore)
        {
            var h = GetHonor(honorId);
            if (h == null) return;
            if (_host != null)
            {
                _host.OnHonorEarned(h.honorId, h.battleType, currentScore, h.bonusTitle);
                _host.ShowHonorUI(h.honorId, h.name, h.requiredScore, h.bonusTitle);
                _host.LogBattleHonorEvent("earn", h.honorId, $"Earned {h.name} at score {currentScore}");
                _host.PlayBattleHonorSFX("earn", h.honorId);
                _host.SaveBattleHonorState(h.honorId, h.battleType, currentScore);
            }
        }
    }
}
