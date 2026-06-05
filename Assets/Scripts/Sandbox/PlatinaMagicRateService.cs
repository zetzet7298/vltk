// -----------------------------------------------------------------------------
// VLTK Mobile — PlatinaMagicRateService: runtime service cho tỉ lệ thuộc tính bạch kim
// Source: PC settings/item/platina_magicrate.txt
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class PlatinaMagicRateService
    {
        private readonly PcPlatinaMagicRateRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public PlatinaMagicRateService(PcPlatinaMagicRateRegistry reg) { _reg = reg ?? new PcPlatinaMagicRateRegistry(); }

        public static PlatinaMagicRateService LoadFromStreamingAssets(string subDir = "Reference/PcItemFull")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new PlatinaMagicRateService(PcPlatinaMagicRateParser.BuildRegistry(path));
        }

        public IEnumerable<PcPlatinaMagicRateEntry> All => _reg?.All ?? System.Array.Empty<PcPlatinaMagicRateEntry>();
        public IEnumerable<PcPlatinaMagicRateEntry> GetByItem(int platinaItem)
            => _reg != null ? _reg.GetByItem(platinaItem) : System.Array.Empty<PcPlatinaMagicRateEntry>();
    }
}
