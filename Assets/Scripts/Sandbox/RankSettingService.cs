// -----------------------------------------------------------------------------
// VLTK Mobile — RankSettingService: runtime service cho cài đặt xếp hạng
// Source: PC settings/ranksetting.txt
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class RankSettingService
    {
        private readonly PcRankSettingRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public RankSettingService() : this(null) { }

        public RankSettingService(PcRankSettingRegistry reg) { _reg = reg ?? new PcRankSettingRegistry(); }

        public static RankSettingService LoadFromStreamingAssets(string subDir = "Reference")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new RankSettingService(PcRankSettingParser.BuildRegistry(path));
        }

        public PcRankSettingEntry Get(int id) => _reg?.Get(id);
        public IEnumerable<PcRankSettingEntry> All => _reg?.All ?? System.Array.Empty<PcRankSettingEntry>();
    }
}
