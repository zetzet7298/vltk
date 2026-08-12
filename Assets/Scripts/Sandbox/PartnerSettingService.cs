// -----------------------------------------------------------------------------
// VLTK Mobile — PartnerSettingService: runtime service cho cấu hình đồng hành
// Source: PC settings/partner/partner_setting.ini
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class PartnerSettingService
    {
        private readonly PcPartnerSettingRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public PartnerSettingService() : this(null) { }

        public PartnerSettingService(PcPartnerSettingRegistry reg) { _reg = reg ?? new PcPartnerSettingRegistry(); }

        public static PartnerSettingService LoadFromStreamingAssets(string subDir = "Reference/PcPartner")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new PartnerSettingService(PcPartnerSettingParser.BuildRegistry(path));
        }

        public PcPartnerSettingEntry GetSection(string name) => _reg?.Get(name);
        public IEnumerable<PcPartnerSettingEntry> AllSections => _reg?.All ?? System.Array.Empty<PcPartnerSettingEntry>();
        public string GetValue(string section, string key)
        {
            var s = _reg?.Get(section);
            return s != null && s.Values.TryGetValue(key, out var v) ? v : null;
        }
    }
}
