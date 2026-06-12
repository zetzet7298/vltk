// -----------------------------------------------------------------------------
// VLTK Mobile — ST-06.11 Arena (Võ Đài) Service
// Võ đài PvP runtime: kiểm tra điều kiện vào + danh sách đấu trường.
// PC source: settings/missions/arena/arena.txt
// Vietnamese: "Võ Đài", "Đấu Trường", "Tỷ Thí", "Hạng".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class ArenaService
    {
        public const string LogTag = "Arena";
        public const string DefaultStreamingDir = "Reference/PcMission/arena";

        private readonly PcArenaRegistry _registry;

        public int Count => _registry?.Count ?? 0;

        public ArenaService() : this(null) { }

        public ArenaService(PcArenaRegistry registry)
        {
            _registry = registry ?? new PcArenaRegistry();
        }

        public static ArenaService LoadFromStreamingAssets(string subDir = null)
        {
            string dir = string.IsNullOrEmpty(subDir)
                ? Path.Combine(Application.streamingAssetsPath, DefaultStreamingDir)
                : Path.Combine(Application.streamingAssetsPath, subDir);
            var reg = PcArenaParser.BuildRegistry(dir);
            SubsystemLog.Info(LogTag, $"Đã tải {reg.Count} đấu trường võ đài từ {dir}");
            return new ArenaService(reg);
        }

        public PcArenaEntry GetArena(int arenaId)
            => _registry.Get(arenaId);

        public IEnumerable<PcArenaEntry> GetAllArenas() => _registry.All;

        public IReadOnlyList<PcArenaEntry> GetArenasForLevel(int playerLevel)
            => _registry.GetForLevel(playerLevel);

        public IReadOnlyList<PcArenaEntry> GetArenasForMap(int mapId)
            => _registry.GetByMap(mapId);

        /// <summary>
        /// Kiểm tra nhân vật có đủ điều kiện vào arena không (cấp + hạng).
        /// </summary>
        public bool CanEnter(int arenaId, int playerLevel, int rating)
        {
            var e = _registry.Get(arenaId);
            if (e == null) return false;
            if (e.minLevel > 0 && playerLevel < e.minLevel) return false;
            if (e.maxLevel > 0 && playerLevel > e.maxLevel) return false;
            if (e.minRating > 0 && rating < e.minRating) return false;
            if (e.maxRating > 0 && rating > e.maxRating) return false;
            return true;
        }
    }
}
