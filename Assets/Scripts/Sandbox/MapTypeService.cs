// -----------------------------------------------------------------------------
// VLTK Mobile — ST Map Type runtime service
// Source: PC settings/map_type.txt.
// Quản lý loại bản đồ (instance / pvp / battlefield).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý Map Type (loại bản đồ) - instance / pvp / battlefield.
    /// </summary>
    public class MapTypeService
    {
        private PcMapTypeRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public MapTypeService() { }
        public MapTypeService(PcMapTypeRegistry reg) { _reg = reg; }

        public void RegisterRegistry(PcMapTypeRegistry reg)
        {
            _reg = reg;
            if (_reg == null || _reg.Count == 0)
                SubsystemLog.Warn("MapType", "Map type registry rỗng");
        }

        public static MapTypeService LoadFromStreamingAssets()
        {
            string root = Path.Combine(Application.streamingAssetsPath, "Reference/PcMap");
            var reg = PcMapTypeParser.BuildRegistry(root);
            return new MapTypeService(reg);
        }

        public PcMapTypeEntry GetType(int id) => _reg != null ? _reg.Get(id) : null;
        public IReadOnlyList<PcMapTypeEntry> GetAll()
            => _reg != null ? _reg.GetAll() : System.Array.Empty<PcMapTypeEntry>();
        public IReadOnlyList<PcMapTypeEntry> All
            => _reg != null ? _reg.All : System.Array.Empty<PcMapTypeEntry>();
    }
}
