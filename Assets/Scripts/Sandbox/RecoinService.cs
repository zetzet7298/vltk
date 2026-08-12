// -----------------------------------------------------------------------------
// VLTK Mobile — RecoinService: runtime service cho tái đúc trang bị vàng
// Source: PC settings/item/recoin_goldenequip.txt
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class RecoinService
    {
        private readonly PcRecoinRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public RecoinService() : this(null) { }

        public RecoinService(PcRecoinRegistry reg) { _reg = reg ?? new PcRecoinRegistry(); }

        public static RecoinService LoadFromStreamingAssets(string subDir = "Reference/PcItemFull")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new RecoinService(PcRecoinParser.BuildRegistry(path));
        }

        public PcRecoinEntry Get(int id) => _reg?.Get(id);
        public IEnumerable<PcRecoinEntry> All => _reg?.All ?? System.Array.Empty<PcRecoinEntry>();
    }
}
