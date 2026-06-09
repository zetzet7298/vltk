// -----------------------------------------------------------------------------
// VLTK Mobile — FactionMapService legacy facade for PC Tong map catalog
// PC source: script/tong/addtongnpc.lua map arrays + tong_mix.lua enter gate under vl_update_27.
// The service exposes imported rows only; Tong ownership/capture/runtime rules are
// intentionally not inferred from this data.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Legacy-named service for the PC bang hội/Tong map catalog.
    /// </summary>
    public class FactionMapService
    {
        public const string LogTag = "FactionMap";

        private PcFactionMapRegistry _registry;

        public int Count => _registry != null ? _registry.Count : 0;

        public FactionMapService() : this(null) { }

        public FactionMapService(PcFactionMapRegistry registry)
        {
            _registry = registry;
        }

        public void RegisterRegistry(PcFactionMapRegistry registry)
        {
            _registry = registry;
            SubsystemLog.Info(LogTag, $"PC Tong map catalog loaded: {Count} row");
        }

        public PcFactionMapEntry GetMap(int mapId)
            => _registry != null ? _registry.Get(mapId) : null;

        public IReadOnlyList<PcFactionMapEntry> GetByFaction(int factionId)
            => _registry != null
                ? _registry.GetByFaction(factionId)
                : (IReadOnlyList<PcFactionMapEntry>)System.Array.Empty<PcFactionMapEntry>();

        public IReadOnlyList<PcFactionMapEntry> GetBySourceTable(string sourceTable)
            => _registry != null
                ? _registry.GetBySourceTable(sourceTable)
                : (IReadOnlyList<PcFactionMapEntry>)System.Array.Empty<PcFactionMapEntry>();

        public IEnumerable<PcFactionMapEntry> GetAllMaps()
            => _registry != null ? _registry.All : (IEnumerable<PcFactionMapEntry>)System.Array.Empty<PcFactionMapEntry>();

        /// <summary>Load từ StreamingAssets/Reference/PcTong/faction_map.txt.</summary>
        public static FactionMapService LoadFromStreamingAssets()
        {
            string dir = Path.Combine(Application.streamingAssetsPath, "Reference/PcTong");
            var reg = PcFactionMapParser.BuildRegistry(dir);
            return new FactionMapService(reg);
        }
    }
}
