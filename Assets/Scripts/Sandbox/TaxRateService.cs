// -----------------------------------------------------------------------------
// VLTK Mobile — TaxRateService: runtime service cho thuế kinh tế
// Source: PC settings/taxrates.ini
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class TaxRateService
    {
        private readonly PcTaxRateRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public TaxRateService(PcTaxRateRegistry reg) { _reg = reg ?? new PcTaxRateRegistry(); }

        public static TaxRateService LoadFromStreamingAssets(string subDir = "Reference")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new TaxRateService(PcTaxRateParser.BuildRegistry(path));
        }

        public PcTaxRateEntry GetSection(string section) => _reg?.Get(section);
        public IEnumerable<PcTaxRateEntry> AllSections => _reg?.All ?? System.Array.Empty<PcTaxRateEntry>();
        public string GetValue(string section, string key)
        {
            var s = _reg?.Get(section);
            return s != null && s.Values.TryGetValue(key, out var v) ? v : null;
        }
    }
}
