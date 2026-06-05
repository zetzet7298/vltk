// -----------------------------------------------------------------------------
// VLTK Mobile — ST Map Traffic runtime service
// Source: PC settings/maptraffic.ini.
// Quản lý lưu lượng map (max players / recommended level / min-max level / pk mode).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý Map Traffic (lưu lượng map) - max players / level range / pk mode.
    /// </summary>
    public class MapTrafficService
    {
        private PcMapTrafficRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public MapTrafficService() { }
        public MapTrafficService(PcMapTrafficRegistry reg) { _reg = reg; }

        public void RegisterRegistry(PcMapTrafficRegistry reg)
        {
            _reg = reg;
            if (_reg == null || _reg.Count == 0)
                SubsystemLog.Warn("MapTraffic", "Map traffic registry rỗng");
        }

        public static MapTrafficService LoadFromStreamingAssets()
        {
            string root = Path.Combine(Application.streamingAssetsPath, "Reference/PcMap");
            var reg = PcMapTrafficParser.BuildRegistry(root);
            return new MapTrafficService(reg);
        }

        public PcMapTrafficEntry GetTraffic(int mapId) => _reg != null ? _reg.Get(mapId) : null;
        public IReadOnlyList<PcMapTrafficEntry> All
            => _reg != null ? _reg.All : System.Array.Empty<PcMapTrafficEntry>();

        public bool IsLevelAllowed(int mapId, int level)
        {
            var t = GetTraffic(mapId);
            if (t == null) return true; // không có rule → cho phép
            if (t.minLevel > 0 && level < t.minLevel) return false;
            if (t.maxLevel > 0 && level > t.maxLevel) return false;
            return true;
        }
    }
}
