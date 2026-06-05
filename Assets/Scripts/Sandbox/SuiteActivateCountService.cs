// -----------------------------------------------------------------------------
// VLTK Mobile — SuiteActivateCountService: runtime service cho số lần kích hoạt bộ
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class SuiteActivateCountService
    {
        private readonly PcSuiteCountRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public SuiteActivateCountService(PcSuiteCountRegistry reg) { _reg = reg ?? new PcSuiteCountRegistry(); }

        public static SuiteActivateCountService LoadFromStreamingAssets(string subDir = "Reference/PcItemFull")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new SuiteActivateCountService(PcSuiteCountParser.BuildRegistry(path));
        }

        public PcSuiteCountEntry Get(int suiteId) => _reg?.Get(suiteId);
        public IEnumerable<PcSuiteCountEntry> All => _reg?.All ?? System.Array.Empty<PcSuiteCountEntry>();
    }
}
