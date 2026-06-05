// -----------------------------------------------------------------------------
// VLTK Mobile — ST Item Detail runtime service
// Source: PC settings/item_detail.txt (202 entries).
// Quản lý chi tiết vật phẩm (category / equip slot / required level / max stack).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý Item Detail (chi tiết vật phẩm) - category / equip slot.
    /// </summary>
    public class ItemDetailService
    {
        private PcItemDetailRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public ItemDetailService() { }
        public ItemDetailService(PcItemDetailRegistry reg) { _reg = reg; }

        public void RegisterRegistry(PcItemDetailRegistry reg)
        {
            _reg = reg;
            if (_reg == null || _reg.Count == 0)
                SubsystemLog.Warn("ItemDetail", "Item detail registry rỗng");
        }

        public static ItemDetailService LoadFromStreamingAssets()
        {
            string root = Path.Combine(Application.streamingAssetsPath, "Reference/PcItem");
            var reg = PcItemDetailParser.BuildRegistry(root);
            return new ItemDetailService(reg);
        }

        public PcItemDetailEntry GetDetail(int id) => _reg != null ? _reg.Get(id) : null;
        public IReadOnlyList<PcItemDetailEntry> GetByCategory(int category)
            => _reg != null ? _reg.GetByCategory(category) : System.Array.Empty<PcItemDetailEntry>();
        public IReadOnlyList<PcItemDetailEntry> All
            => _reg != null ? _reg.All : System.Array.Empty<PcItemDetailEntry>();
    }
}
