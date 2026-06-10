// -----------------------------------------------------------------------------
// VLTK Mobile — ST Item Type runtime service
// Source: PC settings/item_type.txt.
// Quản lý loại vật phẩm (map limit / consumable / tradeable).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý Item Type (loại vật phẩm).
    /// </summary>
    public class ItemTypeService
    {
        private PcItemTypeRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public ItemTypeService() { }
        public ItemTypeService(PcItemTypeRegistry reg) { _reg = reg; }

        public void RegisterRegistry(PcItemTypeRegistry reg)
        {
            _reg = reg;
            if (_reg == null || _reg.Count == 0)
                SubsystemLog.Warn("ItemType", "Item type registry rỗng");
        }

        public static ItemTypeService LoadFromStreamingAssets()
        {
            string root = Path.Combine(Application.streamingAssetsPath, "Reference/PcItemFull");
            var reg = PcItemTypeParser.BuildRegistry(root);
            return new ItemTypeService(reg);
        }

        public PcItemTypeEntry GetType(int id) => _reg != null ? _reg.Get(id) : null;
        public IReadOnlyList<PcItemTypeEntry> GetAll()
            => _reg != null ? _reg.GetAll() : System.Array.Empty<PcItemTypeEntry>();
        public IReadOnlyList<PcItemTypeEntry> All
            => _reg != null ? _reg.All : System.Array.Empty<PcItemTypeEntry>();
    }
}
