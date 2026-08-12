// -----------------------------------------------------------------------------
// VLTK Mobile — FactionConfigService: runtime service cho cấu hình môn phái
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class FactionConfigService
    {
        private readonly PcFactionConfigRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public FactionConfigService() : this(null) { }

        public FactionConfigService(PcFactionConfigRegistry reg) { _reg = reg ?? new PcFactionConfigRegistry(); }

        public static FactionConfigService LoadFromStreamingAssets(string subDir = "Reference/PcTong")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new FactionConfigService(PcFactionConfigParser.BuildRegistry(path));
        }

        public PcFactionConfigEntry GetFaction(int id) => _reg.Get(id);
        public IReadOnlyList<PcFactionConfigEntry> GetAll() => _reg.GetAll();
    }
}
