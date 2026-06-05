// -----------------------------------------------------------------------------
// VLTK Mobile — DropRateConfigService: runtime service cho drop rate config
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class DropRateConfigService
    {
        private readonly PcDropRateConfigRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public DropRateConfigService(PcDropRateConfigRegistry reg) { _reg = reg ?? new PcDropRateConfigRegistry(); }

        public static DropRateConfigService LoadFromStreamingAssets(string subDir = "Reference/PcDropRate")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new DropRateConfigService(PcDropRateConfigParser.BuildRegistry(path));
        }

        public PcDropRateConfigEntry GetDrop(int id) => _reg.Get(id);
        public IEnumerable<PcDropRateConfigEntry> GetByNpcTemplate(int tpl) => _reg.GetByNpcTemplate(tpl);
    }
}
