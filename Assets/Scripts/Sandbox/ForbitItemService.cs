// -----------------------------------------------------------------------------
// VLTK Mobile — ForbitItemService: runtime service cho cấm vật phẩm
// Source: PC settings/forbititem.ini
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class ForbitItemService
    {
        private readonly PcForbitItemRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public ForbitItemService() : this(null) { }

        public ForbitItemService(PcForbitItemRegistry reg) { _reg = reg ?? new PcForbitItemRegistry(); }

        public static ForbitItemService LoadFromStreamingAssets(string subDir = "Reference")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new ForbitItemService(PcForbitItemParser.BuildRegistry(path));
        }

        public PcForbitItemEntry GetItem(string section) => _reg?.Get(section);
        public IEnumerable<PcForbitItemEntry> AllItems => _reg?.All ?? System.Array.Empty<PcForbitItemEntry>();
        public string GetValue(string section, string key)
        {
            var s = _reg?.Get(section);
            return s != null && s.Values.TryGetValue(key, out var v) ? v : null;
        }
    }
}
