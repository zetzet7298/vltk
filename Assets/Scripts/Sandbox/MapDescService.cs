// -----------------------------------------------------------------------------
// VLTK Mobile — MapDescService: runtime service cho mô tả map
// -----------------------------------------------------------------------------

using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class MapDescService
    {
        private readonly PcMapDescRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public MapDescService() : this(null) { }

        public MapDescService(PcMapDescRegistry reg) { _reg = reg ?? new PcMapDescRegistry(); }

        public static MapDescService LoadFromStreamingAssets(string subDir = "Reference/PcMap")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new MapDescService(PcMapDescParser.BuildRegistry(path));
        }

        public PcMapDescEntry GetDesc(int id) => _reg.Get(id);
    }
}
