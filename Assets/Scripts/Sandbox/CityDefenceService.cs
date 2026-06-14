// -----------------------------------------------------------------------------
// VLTK Mobile — ST-08.13 City Defence Service (Thủ thành runtime)
// Wraps PcCityDefenceRegistry. PC source: settings/maps/newcitydefence/*.txt.
// Vietnamese: "Thủ Thành", "Đợt Sóng", "Quái Thủ", "Phần Thưởng", "Bảo Vệ".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class CityDefenceService
    {
        public const string LogTag = "CityDefence";
        public const string DefaultStreamingDir = "Reference/PcMap";

        private PcCityDefenceRegistry _registry;
        private ICityDefenceHost _host;
        // Per-wave state: (mapId, waveIndex) -> started timestamp
        private readonly Dictionary<(int mapId, int waveIndex), long> _waveStartedAt = new();

        public event Action<int, int> OnWaveTriggered; // (mapId, waveIndex)
        public event Action<int, int> OnWaveCompleted; // (mapId, waveIndex)
        public event Action OnDefenceLoaded;

        public int Count => _registry != null ? _registry.Count : 0;

        public CityDefenceService() : this(null, null) { }
        public CityDefenceService(PcCityDefenceRegistry registry) : this(registry, null) { }
        public CityDefenceService(PcCityDefenceRegistry registry, ICityDefenceHost host)
        {
            _host = host;
            AttachRegistry(registry);
        }

        public void AttachHost(ICityDefenceHost host) { _host = host; }

        public void AttachRegistry(PcCityDefenceRegistry registry)
        {
            _registry = registry ?? new PcCityDefenceRegistry();
            SubsystemLog.Info(LogTag, $"Đã tải {_registry.Count} wave thủ thành");
            OnDefenceLoaded?.Invoke();
        }

        public IReadOnlyList<PcCityDefenceEntry> GetDefence(int mapId)
            => _registry != null
                ? _registry.Get(mapId)
                : (IReadOnlyList<PcCityDefenceEntry>)Array.Empty<PcCityDefenceEntry>();

        public IEnumerable<PcCityDefenceEntry> GetAllDefences()
            => _registry != null ? _registry.All : (IEnumerable<PcCityDefenceEntry>)Array.Empty<PcCityDefenceEntry>();

        /// <summary>Trigger wave cho map (gọi khi người chơi vào map thủ thành).</summary>
        public void TriggerWave(int mapId, int waveIndex)
        {
            SubsystemLog.Info(LogTag, $"Bắt đầu wave {waveIndex} của map {mapId}");
            _waveStartedAt[(mapId, waveIndex)] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            // Tìm entry tương ứng để lấy NPC count + interval
            var entries = GetDefence(mapId);
            PcCityDefenceEntry matching = null;
            foreach (var e in entries)
            {
                if (e.waveIndex == waveIndex) { matching = e; break; }
            }
            OnWaveTriggered?.Invoke(mapId, waveIndex);
            if (_host != null)
            {
                int npcCount = matching?.npcCount ?? 0;
                int waveInterval = matching?.waveIntervalSec ?? 0;
                int minLevel = matching?.minLevel ?? 0;
                int npcId = matching?.defenderNpcId ?? 0;
                if (npcId > 0 && npcCount > 0)
                {
                    int spawned = _host.SpawnDefenderNpc(mapId, waveIndex, npcId, npcCount);
                    if (spawned > 0) _host.SetDefenderBuff(spawned, mapId, waveIndex);
                }
                _host.OnWaveStarted(mapId, waveIndex, npcCount, waveInterval);
                _host.PlayWaveStartEffect(mapId, waveIndex);
                _host.ShowDefenceNotice(mapId, waveIndex, minLevel);
                _host.LogDefenceEvent(mapId, waveIndex, $"Bắt đầu wave {waveIndex} map {mapId} (NPC: {npcCount}, cấp tối thiểu: {minLevel})");
            }
        }

        /// <summary>Đánh dấu wave đã hoàn thành, phát thưởng cho player.</summary>
        public void CompleteWave(int mapId, int waveIndex, int playerId)
        {
            _waveStartedAt.Remove((mapId, waveIndex));
            OnWaveCompleted?.Invoke(mapId, waveIndex);
            if (_host != null)
            {
                var entries = GetDefence(mapId);
                foreach (var e in entries)
                {
                    if (e.waveIndex == waveIndex && e.rewardId > 0 && e.rewardCount > 0)
                    {
                        _host.GrantWaveReward(playerId, mapId, waveIndex, e.rewardId, e.rewardCount);
                    }
                }
            }
        }

        /// <summary>Kiểm tra wave đang active.</summary>
        public bool IsWaveActive(int mapId, int waveIndex)
            => _waveStartedAt.ContainsKey((mapId, waveIndex));

        /// <summary>Đếm số wave đang active.</summary>
        public int ActiveWaveCount => _waveStartedAt.Count;

        public static CityDefenceService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new CityDefenceService();
            if (Directory.Exists(dir))
            {
                var reg = PcCityDefenceParser.BuildRegistry(dir);
                svc.AttachRegistry(reg);
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"CityDefence: directory không tồn tại {dir}");
                svc.OnDefenceLoaded?.Invoke();
            }
            return svc;
        }
    }
}
