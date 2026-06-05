// -----------------------------------------------------------------------------
// VLTK Mobile — NativePlaceService: runtime service cho quê hương (birthplace)
// Source: PC settings/nativeplacelist.ini
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class NativePlaceService
    {
        private readonly PcNativePlaceRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public NativePlaceService(PcNativePlaceRegistry reg) { _reg = reg ?? new PcNativePlaceRegistry(); }

        public static NativePlaceService LoadFromStreamingAssets(string subDir = "Reference/PcMap")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new NativePlaceService(PcNativePlaceParser.BuildRegistry(path));
        }

        public PcNativePlaceEntry GetPlace(int id) => _reg?.Get(id);
        public IEnumerable<PcNativePlaceEntry> AllPlaces => _reg?.All ?? System.Array.Empty<PcNativePlaceEntry>();
    }
}
