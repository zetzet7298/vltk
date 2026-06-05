// -----------------------------------------------------------------------------
// VLTK Mobile — PartnerEventService: runtime service cho sự kiện đồng hành
// Source: PC settings/partner/partner_event.ini
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class PartnerEventService
    {
        private readonly PcPartnerEventRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public PartnerEventService(PcPartnerEventRegistry reg) { _reg = reg ?? new PcPartnerEventRegistry(); }

        public static PartnerEventService LoadFromStreamingAssets(string subDir = "Reference/PcPartner")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new PartnerEventService(PcPartnerEventParser.BuildRegistry(path));
        }

        public PcPartnerEventEntry GetSection(string name) => _reg?.Get(name);
        public IEnumerable<PcPartnerEventEntry> AllSections => _reg?.All ?? System.Array.Empty<PcPartnerEventEntry>();
        public string GetValue(string section, string key)
        {
            var s = _reg?.Get(section);
            return s != null && s.Values.TryGetValue(key, out var v) ? v : null;
        }
    }
}
