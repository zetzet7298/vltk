// -----------------------------------------------------------------------------
// VLTK Mobile — ObjectSettingService: runtime service cho PC obj/objsetting.txt
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class ObjectSettingService
    {
        private readonly PcObjSettingRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public ObjectSettingService() { _reg = new PcObjSettingRegistry(); }
        public ObjectSettingService(PcObjSettingRegistry reg) { _reg = reg ?? new PcObjSettingRegistry(); }

        public static ObjectSettingService LoadFromStreamingAssets(string subDir = "Reference/PcObj")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new ObjectSettingService(PcObjSettingParser.BuildRegistry(path));
        }

        public PcObjSettingEntry Get(int dataId) => _reg.Get(dataId);
        public IReadOnlyList<PcObjSettingEntry> All => _reg.All;
    }
}
