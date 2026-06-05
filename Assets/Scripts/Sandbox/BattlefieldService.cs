// -----------------------------------------------------------------------------
// VLTK Mobile — ST-1.8 Battlefield Service (Tống Kim runtime)
// Wraps PcBattlefieldRegistry. 80 chiến trường PC. Quản lý trạng thái
// vào/đầy/điều kiện tham gia.
// Vietnamese: "Tống Kim", "Chiến Trường", "Phe Tống", "Phe Kim".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>Kết quả kiểm tra điều kiện tham gia chiến trường.</summary>
    public enum BattlefieldJoinResult
    {
        Allowed = 0,          // Được vào
        LevelTooLow = 1,      // Cấp chưa đủ
        LevelTooHigh = 2,     // Cấp vượt cấp tối đa
        Full = 3,             // Chiến trường đã đầy
        NotFound = 4,         // Không tìm thấy chiến trường
    }

    /// <summary>Trạng thái runtime của một chiến trường.</summary>
    [Serializable]
    public class BattlefieldState
    {
        public int mapId;
        public string nameVi;
        public int currentPlayers;
        public long startTimestamp;
        public long endTimestamp;
        public int winningTeam;       // 0 = chưa có, 1 = Tống, 2 = Kim
        public bool isActive;
    }

    /// <summary>Service quản lý Tống Kim (chiến trường quốc chiến).</summary>
    public class BattlefieldService
    {
        public const string LogTag = "Battlefield";

        private PcBattlefieldRegistry _registry;
        private readonly Dictionary<int, BattlefieldState> _states = new();

        /// <summary>Sự kiện khi người chơi vào chiến trường. (mapId, playerCount)</summary>
        public event Action<int, int> OnPlayerJoined;
        /// <summary>Sự kiện khi chiến trường kết thúc. (mapId, winningTeam)</summary>
        public event Action<int, int> OnBattleEnded;

        public int Count => _registry != null ? _registry.Count : 0;

        public BattlefieldService() { }

        public BattlefieldService(PcBattlefieldRegistry registry)
        {
            AttachRegistry(registry);
        }

        public void AttachRegistry(PcBattlefieldRegistry registry)
        {
            _registry = registry ?? new PcBattlefieldRegistry();
            _states.Clear();
            foreach (var e in _registry.GetAll())
            {
                _states[e.mapId] = new BattlefieldState
                {
                    mapId = e.mapId,
                    nameVi = e.nameVi,
                    currentPlayers = 0,
                    startTimestamp = 0L,
                    endTimestamp = 0L,
                    winningTeam = 0,
                    isActive = false,
                };
            }
        }

        // ── Query APIs ────────────────────────────────────────────────

        public PcBattlefieldEntry GetBattlefield(int mapId)
            => _registry != null ? _registry.Get(mapId) : null;

        public IReadOnlyList<PcBattlefieldEntry> GetAllBattlefields()
            => _registry != null ? _registry.GetAll() : (IReadOnlyList<PcBattlefieldEntry>)Array.Empty<PcBattlefieldEntry>();

        public bool IsBattlefieldMap(int mapId) => GetBattlefield(mapId) != null;

        public BattlefieldState GetState(int mapId)
            => _states.TryGetValue(mapId, out var s) ? s : null;

        public IEnumerable<BattlefieldState> GetAllStates() => _states.Values;

        // ── Join Logic ────────────────────────────────────────────────

        public BattlefieldJoinResult CanJoin(int mapId, int playerLevel, int currentPlayerCount)
        {
            var entry = GetBattlefield(mapId);
            if (entry == null) return BattlefieldJoinResult.NotFound;
            if (playerLevel < entry.minLevel) return BattlefieldJoinResult.LevelTooLow;
            if (playerLevel > entry.maxLevel) return BattlefieldJoinResult.LevelTooHigh;
            if (currentPlayerCount >= entry.maxPlayers) return BattlefieldJoinResult.Full;
            return BattlefieldJoinResult.Allowed;
        }

        public bool TryJoin(int mapId, int playerLevel)
        {
            var entry = GetBattlefield(mapId);
            if (entry == null) return false;
            var state = GetState(mapId);
            if (state == null) return false;
            var result = CanJoin(mapId, playerLevel, state.currentPlayers);
            if (result != BattlefieldJoinResult.Allowed)
            {
                SubsystemLog.Info(LogTag, $"Player cấp {playerLevel} không thể vào {entry.nameVi}: {result}");
                return false;
            }
            state.currentPlayers++;
            if (state.currentPlayers == 1)
            {
                state.startTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                state.endTimestamp = state.startTimestamp + entry.duration;
                state.isActive = true;
            }
            OnPlayerJoined?.Invoke(mapId, state.currentPlayers);
            SubsystemLog.Info(LogTag, $"Player vào {entry.nameVi}: tổng {state.currentPlayers}/{entry.maxPlayers}");
            return true;
        }

        public bool EndBattle(int mapId, int winningTeam)
        {
            var state = GetState(mapId);
            if (state == null) return false;
            state.isActive = false;
            state.winningTeam = winningTeam;
            state.currentPlayers = 0;
            OnBattleEnded?.Invoke(mapId, winningTeam);
            SubsystemLog.Info(LogTag, $"Kết thúc chiến trường {state.nameVi}: phe thắng = {winningTeam}");
            return true;
        }

        // ── Loading ───────────────────────────────────────────────────

        public static BattlefieldService LoadFromStreamingAssets(string subDir = "Reference/PcMap")
        {
            var svc = new BattlefieldService();
            string dir = Path.Combine(Application.streamingAssetsPath, subDir);
            if (Directory.Exists(dir))
            {
                var reg = PcBattlefieldParser.BuildRegistry(dir);
                svc.AttachRegistry(reg);
                SubsystemLog.Info(LogTag, $"BattlefieldService loaded {reg.Count} chiến trường từ {dir}");
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"BattlefieldService: directory không tồn tại {dir}");
            }
            return svc;
        }
    }
}
