// -----------------------------------------------------------------------------
// VLTK Mobile — MantleService: runtime service cho Phi Phong (áo choàng)
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class MantleService
    {
        private readonly PcMantleRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public MantleService() : this(null) { }

        public MantleService(PcMantleRegistry reg) { _reg = reg ?? new PcMantleRegistry(); }

        public static MantleService LoadFromStreamingAssets(string subDir = "Reference/PcItemFull")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new MantleService(PcMantleParser.BuildRegistry(path));
        }

        public PcMantleEntry Get(int id) => _reg?.Get(id);
        public IEnumerable<PcMantleEntry> All => _reg?.All ?? System.Array.Empty<PcMantleEntry>();
    }
}
