// -----------------------------------------------------------------------------
// VLTK Mobile — PartnerBagService: runtime service cho túi đồ đồng hành
// Source: PC settings/partner/partner_bag.ini
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class PartnerBagService
    {
        private readonly PcPartnerBagRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public PartnerBagService(PcPartnerBagRegistry reg) { _reg = reg ?? new PcPartnerBagRegistry(); }

        public static PartnerBagService LoadFromStreamingAssets(string subDir = "Reference/PcPartner")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new PartnerBagService(PcPartnerBagParser.BuildRegistry(path));
        }

        public PcPartnerBagEntry GetSection(string name) => _reg?.Get(name);
        public IEnumerable<PcPartnerBagEntry> AllSections => _reg?.All ?? System.Array.Empty<PcPartnerBagEntry>();
    }
}
