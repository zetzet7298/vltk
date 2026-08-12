// -----------------------------------------------------------------------------
// VLTK Mobile — TongSettingService: runtime service cho cấu hình bang hội
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class TongSettingService
    {
        private readonly PcTongSettingRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public TongSettingService() : this(null) { }

        public TongSettingService(PcTongSettingRegistry reg) { _reg = reg ?? new PcTongSettingRegistry(); }

        public static TongSettingService LoadFromStreamingAssets(string subDir = "Reference/PcTong")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new TongSettingService(PcTongSettingParser.BuildRegistry(path));
        }

        public PcTongSettingEntry GetSetting(int id) => _reg.Get(id);
        public IEnumerable<PcTongSettingEntry> GetAll() => _reg.All;
    }
}
