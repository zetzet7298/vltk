// -----------------------------------------------------------------------------
// VLTK Mobile — ST-1.9 Instance/Phó Bản Map Service
// Wraps PcInstanceMapRegistry. 802 phó bản PC. Hỗ trợ lọc theo loại (mê
// cung, võ đài, boss) và kiểm tra điều kiện vào (cấp, số người).
// Vietnamese: "Phó Bản", "Mê Cung", "Võ Đài", "Săn Boss", "Tổ Đội".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>Kết quả kiểm tra điều kiện vào phó bản.</summary>
    public enum InstanceEnterResult
    {
        Allowed = 0,          // Được vào
        LevelTooLow = 1,      // Cấp chưa đủ
        LevelTooHigh = 2,     // Cấp vượt cấp tối đa
        PartyTooSmall = 3,    // Tổ đội chưa đủ người
        PartyTooBig = 4,      // Tổ đội vượt quá
        NotFound = 5,         // Không tìm thấy phó bản
    }

    /// <summary>Trạng thái runtime của một phó bản.</summary>
    [Serializable]
    public class InstanceState
    {
        public int mapId;
        public string nameVi;
        public int mapType;
        public long startTimestamp;
        public long endTimestamp;
        public int currentPartySize;
        public bool isActive;
        public bool isCleared;
    }

    /// <summary>Service quản lý phó bản / mê cung / võ đài.</summary>
    public class InstanceMapService
    {
        public const string LogTag = "InstanceMap";

        private PcInstanceMapRegistry _registry;
        private readonly Dictionary<int, InstanceState> _states = new();

        public event Action<int, int> OnInstanceStarted;   // (mapId, partySize)
        public event Action<int, bool> OnInstanceFinished;  // (mapId, cleared)

        public int Count => _registry != null ? _registry.Count : 0;

        public InstanceMapService() { }

        public InstanceMapService(PcInstanceMapRegistry registry)
        {
            AttachRegistry(registry);
        }

        public void AttachRegistry(PcInstanceMapRegistry registry)
        {
            _registry = registry ?? new PcInstanceMapRegistry();
            _states.Clear();
            foreach (var e in _registry.GetAll())
            {
                _states[e.mapId] = new InstanceState
                {
                    mapId = e.mapId,
                    nameVi = e.nameVi,
                    mapType = e.mapType,
                    startTimestamp = 0L,
                    endTimestamp = 0L,
                    currentPartySize = 0,
                    isActive = false,
                    isCleared = false,
                };
            }
        }

        // ── Query APIs ────────────────────────────────────────────────

        public PcInstanceMapEntry GetInstance(int mapId)
            => _registry != null ? _registry.Get(mapId) : null;

        public IReadOnlyList<PcInstanceMapEntry> GetAllInstances()
            => _registry != null ? _registry.GetAll() : (IReadOnlyList<PcInstanceMapEntry>)Array.Empty<PcInstanceMapEntry>();

        public IReadOnlyList<PcInstanceMapEntry> GetInstancesByType(int type)
            => _registry != null ? _registry.GetByType(type) : (IReadOnlyList<PcInstanceMapEntry>)Array.Empty<PcInstanceMapEntry>();

        public InstanceState GetState(int mapId)
            => _states.TryGetValue(mapId, out var s) ? s : null;

        public IEnumerable<InstanceState> GetAllStates() => _states.Values;

        // ── Enter Logic ───────────────────────────────────────────────

        public InstanceEnterResult CanEnter(int mapId, int playerLevel, int partySize)
        {
            var entry = GetInstance(mapId);
            if (entry == null) return InstanceEnterResult.NotFound;
            if (playerLevel < entry.minLevel) return InstanceEnterResult.LevelTooLow;
            if (playerLevel > entry.maxLevel) return InstanceEnterResult.LevelTooHigh;
            if (partySize < entry.minPartySize) return InstanceEnterResult.PartyTooSmall;
            if (partySize > entry.maxPartySize) return InstanceEnterResult.PartyTooBig;
            return InstanceEnterResult.Allowed;
        }

        public bool TryStartInstance(int mapId, int playerLevel, int partySize)
        {
            var entry = GetInstance(mapId);
            if (entry == null) return false;
            var state = GetState(mapId);
            if (state == null) return false;
            var result = CanEnter(mapId, playerLevel, partySize);
            if (result != InstanceEnterResult.Allowed)
            {
                SubsystemLog.Info(LogTag, $"Không thể vào {entry.nameVi}: {result}");
                return false;
            }
            state.currentPartySize = partySize;
            state.startTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            state.endTimestamp = state.startTimestamp + entry.durationMinutes * 60;
            state.isActive = true;
            state.isCleared = false;
            OnInstanceStarted?.Invoke(mapId, partySize);
            SubsystemLog.Info(LogTag, $"Bắt đầu {entry.nameVi}: party {partySize} người, thời lượng {entry.durationMinutes} phút");
            return true;
        }

        public bool FinishInstance(int mapId, bool cleared)
        {
            var state = GetState(mapId);
            if (state == null) return false;
            state.isActive = false;
            state.isCleared = cleared;
            state.currentPartySize = 0;
            OnInstanceFinished?.Invoke(mapId, cleared);
            SubsystemLog.Info(LogTag, $"Kết thúc {state.nameVi}: {(cleared ? "Phá Đảo" : "Thất Bại")}");
            return true;
        }

        // ── Loading ───────────────────────────────────────────────────

        public static InstanceMapService LoadFromStreamingAssets(string subDir = "Reference/PcMap")
        {
            var svc = new InstanceMapService();
            string dir = Path.Combine(Application.streamingAssetsPath, subDir);
            if (Directory.Exists(dir))
            {
                var reg = PcInstanceMapParser.BuildRegistry(dir);
                svc.AttachRegistry(reg);
                SubsystemLog.Info(LogTag, $"InstanceMapService loaded {reg.Count} phó bản từ {dir}");
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"InstanceMapService: directory không tồn tại {dir}");
            }
            return svc;
        }
    }
}
