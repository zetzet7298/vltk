// -----------------------------------------------------------------------------
// VLTK Mobile — ProgressConfigService: runtime service cho cấu hình tiến trình
// Source: PC settings/progressconfig.txt
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class ProgressConfigService
    {
        private readonly PcProgressConfigRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public ProgressConfigService(PcProgressConfigRegistry reg) { _reg = reg ?? new PcProgressConfigRegistry(); }

        public static ProgressConfigService LoadFromStreamingAssets(string subDir = "Reference")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new ProgressConfigService(PcProgressConfigParser.BuildRegistry(path));
        }

        public PcProgressConfigEntry Get(int id) => _reg?.Get(id);
        public IEnumerable<PcProgressConfigEntry> All => _reg?.All ?? System.Array.Empty<PcProgressConfigEntry>();
    }
}
