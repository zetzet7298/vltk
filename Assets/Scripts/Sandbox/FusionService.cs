// -----------------------------------------------------------------------------
// VLTK Mobile — FusionService: runtime service cho Vân Cương (pháp bảo gia tăng)
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class FusionService
    {
        private readonly PcFusionRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public FusionService(PcFusionRegistry reg) { _reg = reg ?? new PcFusionRegistry(); }

        public static FusionService LoadFromStreamingAssets(string subDir = "Reference/PcItemFull")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new FusionService(PcFusionParser.BuildRegistry(path));
        }

        public PcFusionEntry Get(int id) => _reg?.Get(id);
        public IEnumerable<PcFusionEntry> All => _reg?.All ?? System.Array.Empty<PcFusionEntry>();
    }
}
