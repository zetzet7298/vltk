// -----------------------------------------------------------------------------
// VLTK Mobile — MapListService: runtime service cho danh sách map mở rộng
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class MapListService
    {
        private readonly PcMapListRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public MapListService() : this(null) { }

        public MapListService(PcMapListRegistry reg) { _reg = reg ?? new PcMapListRegistry(); }

        public static MapListService LoadFromStreamingAssets(string subDir = "Reference/PcMap")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new MapListService(PcMapListParser2.BuildRegistry(path));
        }

        public PcMapListEntry GetMap(int id) => _reg.Get(id);
        public IEnumerable<PcMapListEntry> GetByType(int type) => _reg.GetByType(type);
    }
}
