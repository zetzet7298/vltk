// -----------------------------------------------------------------------------
// VLTK Mobile — Seasonal Event runtime service (Sự Kiện Mùa)
// Quản lý sự kiện theo mùa: id, tên, startMonth, endMonth, phần thưởng.
// Hỗ trợ wrap-around (Tết: 11..2).
// Wraps PcSeasonalEventRegistry.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class SeasonalEventService
    {
        public const string LogTag = "SeasonalEvent";
        public const string DefaultStreamingDir = "Reference/PcSeasonalEvent";

        private PcSeasonalEventRegistry _registry;

        public int Count => _registry != null ? _registry.Count : 0;
        public IEnumerable<PcSeasonalEventEntry> All => _registry?.All;

        public SeasonalEventService() : this(null) { }

        public SeasonalEventService(PcSeasonalEventRegistry registry)
        {
            _registry = registry;
            SubsystemLog.Info(LogTag, $"SeasonalEventService loaded {_registry?.Count ?? 0} sự kiện mùa");
        }

        public void RegisterRegistry(PcSeasonalEventRegistry registry)
        {
            _registry = registry;
        }

        public PcSeasonalEventEntry GetEvent(int eventId)
            => _registry != null ? _registry.Get(eventId) : null;

        public IReadOnlyList<PcSeasonalEventEntry> GetActiveByMonth(int month)
            => _registry != null
                ? _registry.GetActiveByMonth(month)
                : System.Array.Empty<PcSeasonalEventEntry>();

        public static SeasonalEventService LoadFromStreamingAssets(string subDir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subDir) ? DefaultStreamingDir : subDir);
            var reg = PcSeasonalEventParser.BuildRegistry(dir);
            return new SeasonalEventService(reg);
        }
    }
}
