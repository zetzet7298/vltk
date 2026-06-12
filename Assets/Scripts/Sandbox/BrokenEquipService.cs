// -----------------------------------------------------------------------------
// VLTK Mobile — BrokenEquipService: runtime service cho trang bị tân hư
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class BrokenEquipService
    {
        private readonly PcBrokenEquipRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public BrokenEquipService() : this(null) { }

        public BrokenEquipService(PcBrokenEquipRegistry reg) { _reg = reg ?? new PcBrokenEquipRegistry(); }

        public static BrokenEquipService LoadFromStreamingAssets(string subDir = "Reference/PcItemFull")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new BrokenEquipService(PcBrokenEquipParser.BuildRegistry(path));
        }

        public PcBrokenEquipEntry Get(int id) => _reg?.Get(id);
        public IEnumerable<PcBrokenEquipEntry> All => _reg?.All ?? System.Array.Empty<PcBrokenEquipEntry>();
    }
}
