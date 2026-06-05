// -----------------------------------------------------------------------------
// VLTK Mobile — Ngựa runtime service
// Wraps PcHorseRegistry. Exposes horse lookup by id/level/series.
// Vietnamese: "Ngựa", "Tốc độ ngựa", "Thể lực".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service truy vấn thông tin ngựa.
    /// PC source: settings/horse.txt.
    /// </summary>
    public class HorseService
    {
        public const string LogTag = "Horse";
        private PcHorseRegistry _registry = new();

        public event Action OnLoaded;
        public int Count => _registry?.Count ?? 0;

        public void AttachRegistry(PcHorseRegistry registry)
        {
            _registry = registry ?? new PcHorseRegistry();
            OnLoaded?.Invoke();
        }

        public static HorseService LoadFromStreamingAssets(string relativeDir = "Reference/PcItemFull")
        {
            var svc = new HorseService();
            try
            {
                var path = Path.Combine(Application.streamingAssetsPath, relativeDir);
                var reg = PcHorseParser.BuildRegistry(path);
                svc.AttachRegistry(reg);
                Debug.Log($"[{LogTag}] Loaded {svc.Count} ngựa từ {path}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{LogTag}] Load thất bại: {ex.Message}");
            }
            return svc;
        }

        public PcHorseEntry GetHorse(int genre, int detail, int particular) => _registry.Get(genre, detail, particular);
        public List<PcHorseEntry> GetByLevel(int level) => _registry.GetByLevel(level);
        public List<PcHorseEntry> GetBySeries(int series) => _registry.GetBySeries(series);
        public IEnumerable<PcHorseEntry> All => _registry.All;
    }
}
