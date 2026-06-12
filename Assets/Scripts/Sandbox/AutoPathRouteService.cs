// -----------------------------------------------------------------------------
// VLTK Mobile — AutoPathRouteService: runtime service cho auto path finding
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class AutoPathRouteService
    {
        private readonly PcAutoPathRouteRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public AutoPathRouteService() : this(null) { }

        public AutoPathRouteService(PcAutoPathRouteRegistry reg) { _reg = reg ?? new PcAutoPathRouteRegistry(); }

        public static AutoPathRouteService LoadFromStreamingAssets(string subDir = "Reference/PcMap")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new AutoPathRouteService(PcAutoPathRouteParser.BuildRegistry(path));
        }

        public PcAutoPathRouteEntry GetRoute(int id) => _reg.Get(id);
        public IEnumerable<PcAutoPathRouteEntry> GetByFromTo(int fromMap, int toMap) => _reg.GetByFromTo(fromMap, toMap);
    }
}
