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

        public event Action<int, int> OnWaveTriggered; // (mapId, waveIndex)
        public event Action OnDefenceLoaded;

        public int Count => _registry != null ? _registry.Count : 0;

        public CityDefenceService() { }
        public CityDefenceService(PcCityDefenceRegistry registry) { AttachRegistry(registry); }

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
            OnWaveTriggered?.Invoke(mapId, waveIndex);
        }

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
