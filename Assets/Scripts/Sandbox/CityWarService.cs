// -----------------------------------------------------------------------------
// VLTK Mobile — ST-XX CityWar Service (Thành chiến runtime)
// Wraps PcCityWarRegistry. Owner state per city, capture mechanics.
// Vietnamese: "Thành Chiến", "Chiếm Thành", "Phe Phái", "Phòng Thủ".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>Trạng thái runtime của một thành trong hệ thống Thành Chiến.</summary>
    [Serializable]
    public class CityWarState
    {
        public int cityId;
        public string nameVi;
        public int ownerFaction;          // 0 = phe Trung Lập / chưa ai chiếm
        public long captureTimestamp;     // epoch seconds
        public int defenderCount;
        public List<int> mapIds = new();
    }

    /// <summary>
    /// Service quản lý trạng thái các thành trong Thành Chiến. PC source:
    /// settings/event/citywar.ini — AreaName01..AreaName07, AreaIncludes..
    /// </summary>
    public class CityWarService
    {
        public const string LogTag = "CityWar";

        public const int NeutralFaction = 0;

        private PcCityWarRegistry _registry;
        private ICityWarHost _host;
        // Per-city capture reward map: cityId -> (itemId, count). Defaults to 0.
        private readonly Dictionary<int, (int itemId, int count)> _captureRewards = new();
        private readonly Dictionary<int, CityWarState> _cityStates = new();
        private bool _indexed;

        /// <summary>Sự kiện khi một thành bị chiếm. (cityId, oldOwner, newOwner)</summary>
        public event Action<int, int, int> OnCityCaptured;
        /// <summary>Sự kiện khi defender count thay đổi. (cityId, count)</summary>
        public event Action<int, int> OnDefenderChanged;

        public int Count => _cityStates.Count;

        public CityWarService() : this(null, null) { }
        public CityWarService(PcCityWarRegistry registry) : this(registry, null) { }
        public CityWarService(PcCityWarRegistry registry, ICityWarHost host)
        {
            _host = host;
            AttachRegistry(registry);
        }

        public void AttachHost(ICityWarHost host) { _host = host; }

        /// <summary>Thiết lập phần thưởng cho từng thành khi chiếm thành công.</summary>
        public void SetCaptureReward(int cityId, int rewardItem, int rewardCount)
            => _captureRewards[cityId] = (rewardItem, rewardCount);

        public void AttachRegistry(PcCityWarRegistry registry)
        {
            _registry = registry ?? new PcCityWarRegistry();
            _indexed = false;
            BuildIndex();
        }

        private void BuildIndex()
        {
            _cityStates.Clear();
            if (_registry == null) { _indexed = true; return; }
            foreach (var area in _registry.All)
            {
                int cityId = ExtractCityId(area);
                if (cityId <= 0) continue;
                if (_cityStates.ContainsKey(cityId)) continue;
                _cityStates[cityId] = new CityWarState
                {
                    cityId = cityId,
                    nameVi = area.name,
                    ownerFaction = NeutralFaction,
                    captureTimestamp = 0L,
                    defenderCount = 0,
                    mapIds = new List<int>(area.mapIds),
                };
            }
            _indexed = true;
        }

        private static int ExtractCityId(PcCityWarArea area)
        {
            if (area == null || string.IsNullOrEmpty(area.key)) return 0;
            string key = area.key;
            const string prefix = "AreaName";
            if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                string tail = key.Substring(prefix.Length);
                return int.TryParse(tail, out int n) ? n : 0;
            }
            return 0;
        }

        // ── Query APIs ────────────────────────────────────────────────

        public PcCityWarArea GetCity(int cityId)
        {
            if (_registry == null) return null;
            foreach (var a in _registry.All)
            {
                if (ExtractCityId(a) == cityId) return a;
            }
            return null;
        }

        public IEnumerable<PcCityWarArea> GetAllCities()
        {
            if (_registry == null) yield break;
            foreach (var a in _registry.All)
                if (ExtractCityId(a) > 0) yield return a;
        }

        public CityWarState GetCityState(int cityId)
        {
            if (!_indexed) BuildIndex();
            return _cityStates.TryGetValue(cityId, out var s) ? s : null;
        }

        public IEnumerable<CityWarState> GetAllCityStates()
        {
            if (!_indexed) BuildIndex();
            return _cityStates.Values;
        }

        public bool IsOwnedBy(int cityId, int factionId)
        {
            var s = GetCityState(cityId);
            return s != null && s.ownerFaction == factionId;
        }

        public IEnumerable<CityWarState> GetCitiesOwnedBy(int factionId)
        {
            if (!_indexed) BuildIndex();
            foreach (var s in _cityStates.Values)
                if (s.ownerFaction == factionId) yield return s;
        }

        // ── Mutators ──────────────────────────────────────────────────

        public bool CaptureCity(int cityId, int newOwnerFaction)
        {
            if (!_indexed) BuildIndex();
            if (!_cityStates.TryGetValue(cityId, out var s))
            {
                SubsystemLog.Warn(LogTag, $"CaptureCity: thành {cityId} không tồn tại");
                return false;
            }
            int oldOwner = s.ownerFaction;
            if (oldOwner == newOwnerFaction) return false;
            s.ownerFaction = newOwnerFaction;
            s.captureTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            s.defenderCount = 0;
            string factionName = FactionName(newOwnerFaction);
            SubsystemLog.Info(LogTag, $"Thành {s.nameVi} (id={cityId}) bị chiếm bởi {factionName}");
            OnCityCaptured?.Invoke(cityId, oldOwner, newOwnerFaction);
            if (_host != null)
            {
                _host.OnCityOwnerChanged(cityId, oldOwner, newOwnerFaction, s.nameVi);
                _host.ShowCityMarker(cityId, newOwnerFaction, s.nameVi);
                _host.PlayCaptureSFX(cityId, newOwnerFaction);
                _host.LogCityWarEvent(cityId, oldOwner, newOwnerFaction,
                    $"Thành {s.nameVi} (id={cityId}) bị chiếm bởi {factionName}");
                _host.UpdateLeaderboard(cityId, newOwnerFaction, s.defenderCount, s.captureTimestamp);
                if (_captureRewards.TryGetValue(cityId, out var reward) && reward.itemId > 0 && reward.count > 0)
                {
                    _host.GrantCaptureReward(cityId, newOwnerFaction, reward.itemId, reward.count);
                }
            }
            return true;
        }

        public bool AddDefender(int cityId, int count = 1)
        {
            var s = GetCityState(cityId);
            if (s == null) return false;
            s.defenderCount += count;
            if (s.defenderCount < 0) s.defenderCount = 0;
            OnDefenderChanged?.Invoke(cityId, s.defenderCount);
            if (_host != null)
            {
                _host.UpdateDefenderNpcs(cityId, s.ownerFaction, s.defenderCount);
                _host.UpdateLeaderboard(cityId, s.ownerFaction, s.defenderCount, s.captureTimestamp);
            }
            return true;
        }

        public void ResetAll()
        {
            int total = _cityStates.Count;
            int neutral = 0;
            foreach (var s in _cityStates.Values)
            {
                s.ownerFaction = NeutralFaction;
                s.captureTimestamp = 0L;
                s.defenderCount = 0;
                neutral++;
            }
            SubsystemLog.Info(LogTag, "Reset toàn bộ trạng thái Thành Chiến");
            _host?.OnCityWarReset(total, neutral);
        }

        private static string FactionName(int factionId)
        {
            return factionId switch
            {
                NeutralFaction => "Trung Lập",
                1 => "Thiếu Lâm",
                2 => "Thiên Vương",
                3 => "Đường Môn",
                4 => "Ngũ Độc",
                5 => "Nga My",
                6 => "Thúy Yên",
                7 => "Cái Bang",
                8 => "Thiên Nhẫn",
                9 => "Võ Đang",
                10 => "Côn Luân",
                _ => $"Phe {factionId}",
            };
        }

        // ── Loading ───────────────────────────────────────────────────

        public static CityWarService LoadFromStreamingAssets()
        {
            var svc = new CityWarService();
            string dir = Path.Combine(Application.streamingAssetsPath, "Reference/PcEvent");
            if (Directory.Exists(dir))
            {
                var reg = PcCityWarParser.BuildRegistry(dir);
                svc.AttachRegistry(reg);
                SubsystemLog.Info(LogTag, $"CityWarService loaded {reg.Count} khu vực thành chiến từ {dir}");
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"CityWarService: directory không tồn tại {dir}");
            }
            return svc;
        }
    }
}
