// -----------------------------------------------------------------------------
// VLTK Mobile — ST-8.9 SongJin Battle Tier Service
// Quản lý cấp bậc Tống Kim (Sơ/Trung/Cao). Reference: songjin_tier.txt.
// Vietnamese: "Tống Kim", "Sơ Cấp", "Trung Cấp", "Cao Cấp", "Yêu Cầu Cấp".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Cấp bậc Tống Kim.
    /// </summary>
    public static class SongJinTier
    {
        public const int So = 0;        // Sơ Cấp
        public const int Trung = 1;     // Trung Cấp
        public const int Cao = 2;       // Cao Cấp

        public const int Min = 0;
        public const int Max = 2;

        public static string GetName(int tier)
        {
            switch (tier)
            {
                case So: return "Sơ Cấp";
                case Trung: return "Trung Cấp";
                case Cao: return "Cao Cấp";
                default: return "Không Xác Định";
            }
        }
    }

    /// <summary>
    /// Service quản lý cấp bậc Tống Kim.
    /// </summary>
    public class SjBattleService
    {
        public const string LogTag = "SjBattle";
        public const string DefaultStreamingDir = "Reference/PcBattlefield";

        private PcSjBattleRegistry _registry;
        public int Count => _registry?.Count ?? 0;

        public SjBattleService() { }
        public SjBattleService(PcSjBattleRegistry registry) { _registry = registry; }

        public void RegisterRegistry(PcSjBattleRegistry reg)
        {
            _registry = reg ?? new PcSjBattleRegistry();
            if (_registry.Count == 0) SubsystemLog.Warn(LogTag, "Cấu hình Tống Kim rỗng");
        }

        public static SjBattleService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new SjBattleService();
            var reg = PcSjBattleParser.BuildRegistry(dir);
            svc.RegisterRegistry(reg);
            SubsystemLog.Info(LogTag, $"Đã tải {reg.Count} cấu hình Tống Kim");
            return svc;
        }

        public PcSjBattleEntry GetTier(int tierId)
            => _registry != null ? _registry.Get(tierId) : null;

        public IReadOnlyList<PcSjBattleEntry> GetByTier(int tier)
            => _registry != null ? _registry.GetByTier(tier) : Array.Empty<PcSjBattleEntry>();

        public IReadOnlyList<PcSjBattleEntry> GetByMap(int mapId)
            => _registry != null ? _registry.GetByMap(mapId) : Array.Empty<PcSjBattleEntry>();

        public string GetTierName(int tier) => SongJinTier.GetName(tier);

        /// <summary>Có thể vào tier này với cấp NV không.</summary>
        public bool CanJoinTier(int tier, int playerLevel)
        {
            if (_registry == null) return false;
            if (tier < SongJinTier.Min || tier > SongJinTier.Max) return false;
            foreach (var e in _registry.GetByTier(tier))
            {
                if (e.minLevel > 0 && playerLevel >= e.minLevel) return true;
            }
            // Fallback: cho phép nếu không có entry cụ thể
            return playerLevel >= 30 + tier * 30;
        }

        /// <summary>Trả về tier (0-2) phù hợp với cấp NV.</summary>
        public int GetTierForLevel(int playerLevel)
        {
            if (playerLevel < 30) return SongJinTier.So;
            if (playerLevel < 60) return SongJinTier.So;
            if (playerLevel < 90) return SongJinTier.Trung;
            return SongJinTier.Cao;
        }
    }
}
