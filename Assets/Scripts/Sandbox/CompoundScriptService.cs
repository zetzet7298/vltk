// -----------------------------------------------------------------------------
// VLTK Mobile — CompoundScriptService: runtime service cho công thức ghép đồ
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class CompoundScriptService
    {
        private readonly PcCompoundScriptRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public CompoundScriptService(PcCompoundScriptRegistry reg) { _reg = reg ?? new PcCompoundScriptRegistry(); }

        public static CompoundScriptService LoadFromStreamingAssets(string subDir = "Reference/PcItemFull")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new CompoundScriptService(PcCompoundScriptParser.BuildRegistry(path));
        }

        public PcCompoundScriptEntry Get(int type) => _reg?.Get(type);
        public IEnumerable<PcCompoundScriptEntry> All => _reg?.All ?? System.Array.Empty<PcCompoundScriptEntry>();
    }
}
