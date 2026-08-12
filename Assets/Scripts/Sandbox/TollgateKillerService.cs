// -----------------------------------------------------------------------------
// VLTK Mobile — TollgateKillerService: runtime service cho trạm kiểm tra boss
// Source: PC settings/task/tollgate/killer/killer.txt
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class TollgateKillerService
    {
        private readonly PcTollgateKillerRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public TollgateKillerService() : this(null) { }

        public TollgateKillerService(PcTollgateKillerRegistry reg) { _reg = reg ?? new PcTollgateKillerRegistry(); }

        public static TollgateKillerService LoadFromStreamingAssets(string subDir = "Reference/PcTask/tollgate/killer")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new TollgateKillerService(PcTollgateKillerParser.BuildRegistry(path));
        }

        public PcTollgateKillerEntry GetBoss(int id) => _reg?.Get(id);
        public IEnumerable<PcTollgateKillerEntry> AllBosses => _reg?.All ?? System.Array.Empty<PcTollgateKillerEntry>();
    }
}
