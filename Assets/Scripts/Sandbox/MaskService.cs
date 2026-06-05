// -----------------------------------------------------------------------------
// VLTK Mobile — MaskService: runtime service cho Mặt nạ
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class MaskService
    {
        private readonly PcMaskRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public MaskService(PcMaskRegistry reg) { _reg = reg ?? new PcMaskRegistry(); }

        public static MaskService LoadFromStreamingAssets(string subDir = "Reference/PcItemFull")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new MaskService(PcMaskParser.BuildRegistry(path));
        }

        public PcMaskEntry Get(int id) => _reg?.Get(id);
        public IEnumerable<PcMaskEntry> All => _reg?.All ?? System.Array.Empty<PcMaskEntry>();
    }
}
