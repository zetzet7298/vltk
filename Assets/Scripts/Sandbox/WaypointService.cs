// -----------------------------------------------------------------------------
// VLTK Mobile — WaypointService: runtime service cho waypoint dịch chuyển
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class WaypointService
    {
        private readonly PcWaypointRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public WaypointService() : this(null) { }

        public WaypointService(PcWaypointRegistry reg) { _reg = reg ?? new PcWaypointRegistry(); }

        public static WaypointService LoadFromStreamingAssets(string subDir = "Reference/PcMap")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new WaypointService(PcWaypointParser.BuildRegistry(path));
        }

        public PcWaypointEntry GetWaypoint(int id) => _reg.Get(id);
        public IEnumerable<PcWaypointEntry> GetByMap(int mapId) => _reg.GetByMap(mapId);
    }
}
