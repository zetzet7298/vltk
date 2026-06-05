// -----------------------------------------------------------------------------
// VLTK Mobile — ShipinService: runtime service cho Trang sức tân thủ
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class ShipinService
    {
        private readonly PcShipinRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public ShipinService(PcShipinRegistry reg) { _reg = reg ?? new PcShipinRegistry(); }

        public static ShipinService LoadFromStreamingAssets(string subDir = "Reference/PcItemFull")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new ShipinService(PcShipinParser.BuildRegistry(path));
        }

        public PcShipinEntry Get(int id) => _reg?.Get(id);
        public IEnumerable<PcShipinEntry> All => _reg?.All ?? System.Array.Empty<PcShipinEntry>();
    }
}
