// -----------------------------------------------------------------------------
// VLTK Mobile — ST-8.x Battle Map Config Service
// Quản lý cấu hình chiến trường. Reference: battlemapconfig.txt.
// Vietnamese: "Chiến Trường", "Tống Kim", "Quốc Chiến", "Công Thành".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Loại chiến trường.
    /// </summary>
    public static class BattleType
    {
        public const int SongJin = 0;       // Tống Kim
        public const int NationalWar = 1;   // Quốc Chiến
        public const int CityWar = 2;       // Công Thành
        public const int Boss = 3;          // Boss
        public const int PvP = 4;           // PvP

        public static string GetName(int type)
        {
            switch (type)
            {
                case SongJin: return "Tống Kim";
                case NationalWar: return "Quốc Chiến";
                case CityWar: return "Công Thành";
                case Boss: return "Boss";
                case PvP: return "PvP";
                default: return "Khác";
            }
        }
    }

    /// <summary>
    /// Service quản lý cấu hình chiến trường.
    /// </summary>
    public class BattleMapConfigService
    {
        public const string LogTag = "BattleMapConfig";
        public const string DefaultStreamingDir = "Reference/PcBattlefield";

        private PcBattleMapConfigRegistry _registry;
        public int Count => _registry?.Count ?? 0;

        public BattleMapConfigService() { }
        public BattleMapConfigService(PcBattleMapConfigRegistry registry) { _registry = registry; }

        public void RegisterRegistry(PcBattleMapConfigRegistry reg)
        {
            _registry = reg ?? new PcBattleMapConfigRegistry();
            if (_registry.Count == 0) SubsystemLog.Warn(LogTag, "Cấu hình chiến trường rỗng");
        }

        public static BattleMapConfigService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new BattleMapConfigService();
            var reg = PcBattleMapConfigParser.BuildRegistry(dir);
            svc.RegisterRegistry(reg);
            SubsystemLog.Info(LogTag, $"Đã tải {reg.Count} cấu hình chiến trường");
            return svc;
        }

        public PcBattleMapConfigEntry GetBattleMap(int battleMapId)
            => _registry != null ? _registry.Get(battleMapId) : null;

        public IReadOnlyList<PcBattleMapConfigEntry> GetByBattleType(int battleType)
            => _registry != null ? _registry.GetByBattleType(battleType) : Array.Empty<PcBattleMapConfigEntry>();

        public IReadOnlyList<PcBattleMapConfigEntry> GetByMap(int mapId)
            => _registry != null ? _registry.GetByMap(mapId) : Array.Empty<PcBattleMapConfigEntry>();

        public int GetDurationSec(int battleMapId)
        {
            var e = GetBattleMap(battleMapId);
            return e?.durationSec ?? 0;
        }

        public int GetScoreWin(int battleMapId)
        {
            var e = GetBattleMap(battleMapId);
            return e?.scoreWin ?? 0;
        }

        /// <summary>Có thể vào chiến trường với cấp NV này không.</summary>
        public bool CanJoin(int battleMapId, int playerLevel)
        {
            var e = GetBattleMap(battleMapId);
            if (e == null) return false;
            if (e.minLevel > 0 && playerLevel < e.minLevel) return false;
            if (e.maxLevel > 0 && playerLevel > e.maxLevel) return false;
            return true;
        }

        public string GetBattleTypeName(int type) => BattleType.GetName(type);
    }
}
