// -----------------------------------------------------------------------------
// VLTK Mobile — SignetService: runtime service cho Tân Thú Ấn
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class SignetService
    {
        private readonly PcSignetRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public SignetService() : this(null) { }

        public SignetService(PcSignetRegistry reg) { _reg = reg ?? new PcSignetRegistry(); }

        public static SignetService LoadFromStreamingAssets(string subDir = "Reference/PcItemFull")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new SignetService(PcSignetParser.BuildRegistry(path));
        }

        public PcSignetEntry Get(int id) => _reg?.Get(id);
        public IEnumerable<PcSignetEntry> All => _reg?.All ?? System.Array.Empty<PcSignetEntry>();
    }
}
