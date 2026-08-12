// -----------------------------------------------------------------------------
// VLTK Mobile — ST-06.12 Boss Mission Service (Nhiệm Vụ Boss / Săn Boss)
// PC source: missions/boss/bossmission.txt — nhiệm vụ boss theo map / level / tổ đội.
// Vietnamese: "Nhiệm Vụ Boss", "Săn Boss", "Tổ Đội", "Phần Thưởng".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public enum BossEnterResult
    {
        CanEnter = 0,           // Đủ điều kiện vào
        NotFound = 1,           // Không tìm thấy mission
        LevelTooLow = 2,        // Cấp nhân vật chưa đủ
        LevelTooHigh = 3,       // Vượt cấp tối đa
        NotEnoughParty = 4,     // Thiếu thành viên tổ đội
    }

    /// <summary>
    /// Service quản lý nhiệm vụ Boss (sanctuary boss). Mỗi mission có:
    /// - Map riêng, NPC boss riêng.
    /// - Cấp nhân vật min/max.
    /// - Số thành viên tổ đội tối thiểu.
    /// - Phần thưởng + reset theo giờ trong ngày.
    /// </summary>
    public class BossMissionService
    {
        public const string DefaultStreamingDir = "Reference/PcBossMission";
        public const string LogTag = "BossMission";

        private readonly PcBossMissionRegistry _registry;

        public event Action<PcBossMissionEntry> OnBossMissionStarted;
        public event Action<PcBossMissionEntry> OnBossMissionCompleted;

        public int Count => _registry?.Count ?? 0;

        public BossMissionService() : this(null) { }

        public BossMissionService(PcBossMissionRegistry registry)
        {
            _registry = registry ?? new PcBossMissionRegistry();
        }

        public PcBossMissionEntry GetBossMission(int missionId)
            => _registry?.Get(missionId);

        public IReadOnlyList<PcBossMissionEntry> GetAllBossMissions()
            => _registry != null
                ? (IReadOnlyList<PcBossMissionEntry>)new List<PcBossMissionEntry>(_registry.All)
                : Array.Empty<PcBossMissionEntry>();

        public IReadOnlyList<PcBossMissionEntry> GetMissionsForMap(int mapId)
            => _registry?.GetByMap(mapId)
                ?? (IReadOnlyList<PcBossMissionEntry>)Array.Empty<PcBossMissionEntry>();

        public IReadOnlyList<PcBossMissionEntry> GetMissionsForBoss(int npcId)
            => _registry?.GetByBoss(npcId)
                ?? (IReadOnlyList<PcBossMissionEntry>)Array.Empty<PcBossMissionEntry>();

        /// <summary>Kiểm tra điều kiện vào boss mission (cấp + tổ đội).</summary>
        public BossEnterResult CanEnter(int missionId, int playerLevel, int partySize)
        {
            var entry = GetBossMission(missionId);
            if (entry == null) return BossEnterResult.NotFound;
            if (entry.minLevel > 0 && playerLevel < entry.minLevel) return BossEnterResult.LevelTooLow;
            if (entry.maxLevel > 0 && playerLevel > entry.maxLevel) return BossEnterResult.LevelTooHigh;
            if (entry.minPartySize > 0 && partySize < entry.minPartySize) return BossEnterResult.NotEnoughParty;
            return BossEnterResult.CanEnter;
        }

        public bool StartMission(int missionId, int playerLevel, int partySize)
        {
            var result = CanEnter(missionId, playerLevel, partySize);
            if (result != BossEnterResult.CanEnter)
            {
                SubsystemLog.Warn(LogTag, $"Không thể vào boss mission #{missionId}: {result}");
                return false;
            }
            var entry = GetBossMission(missionId);
            SubsystemLog.Info(LogTag,
                $"Bắt đầu boss mission #{missionId} map={entry.mapId} boss={entry.bossNpcId} " +
                $"(cấp {playerLevel}, tổ đội {partySize})");
            OnBossMissionStarted?.Invoke(entry);
            return true;
        }

        public bool CompleteMission(int missionId)
        {
            var entry = GetBossMission(missionId);
            if (entry == null) return false;
            SubsystemLog.Info(LogTag,
                $"Hoàn thành boss mission #{missionId} → +item {entry.rewardId} x{entry.rewardCount}");
            OnBossMissionCompleted?.Invoke(entry);
            return true;
        }

        /// <summary>Load từ StreamingAssets.</summary>
        public static BossMissionService LoadFromStreamingAssets(string subDir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subDir) ? DefaultStreamingDir : subDir);
            var reg = PcBossMissionParser.BuildRegistry(dir);
            SubsystemLog.Info(LogTag, $"BossMissionService loaded {reg.Count} nhiệm vụ boss từ {dir}");
            return new BossMissionService(reg);
        }
    }
}
