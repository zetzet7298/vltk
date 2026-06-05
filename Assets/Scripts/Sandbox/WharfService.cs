// -----------------------------------------------------------------------------
// VLTK Mobile — WharfService: runtime service cho bến tàu
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class WharfService
    {
        private readonly PcWharfRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public WharfService(PcWharfRegistry reg) { _reg = reg ?? new PcWharfRegistry(); }

        public static WharfService LoadFromStreamingAssets(string subDir = "Reference/PcMap")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new WharfService(PcWharfParser.BuildRegistry(path));
        }

        public PcWharfEntry GetWharf(int id) => _reg.Get(id);
        public IEnumerable<PcWharfEntry> GetByFromMap(int mapId) => _reg.GetByFromMap(mapId);
    }
}
