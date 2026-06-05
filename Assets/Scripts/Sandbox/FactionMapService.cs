// -----------------------------------------------------------------------------
// VLTK Mobile — FactionMapService (Bản Đồ Theo Môn Phái runtime)
// Wraps PcFactionMapRegistry. PC source: faction_map.txt / PcTong/tong_setting.ini.
// Mobile runtime phục vụ war portal, faction city, owner bonus lookup.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý bản đồ theo môn phái: war portal, faction capital,
    /// owner bonus (Tống Kim, Tổng Tiêu Cục, đại lý phủ).
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
            SubsystemLog.Info(LogTag, $"Bản Đồ Môn Phái loaded: {Count} map");
        }

        public PcFactionMapEntry GetMap(int mapId)
            => _registry != null ? _registry.Get(mapId) : null;

        public IReadOnlyList<PcFactionMapEntry> GetByFaction(int factionId)
            => _registry != null
                ? _registry.GetByFaction(factionId)
                : (IReadOnlyList<PcFactionMapEntry>)System.Array.Empty<PcFactionMapEntry>();

        public IEnumerable<PcFactionMapEntry> GetAllMaps()
            => _registry != null ? _registry.All : (IEnumerable<PcFactionMapEntry>)System.Array.Empty<PcFactionMapEntry>();

        /// <summary>Load từ StreamingAssets/Reference/PcTong.</summary>
        public static FactionMapService LoadFromStreamingAssets()
        {
            string dir = Path.Combine(Application.streamingAssetsPath, "Reference/PcTong");
            var reg = PcFactionMapParser.BuildRegistry(dir);
            return new FactionMapService(reg);
        }
    }
}
