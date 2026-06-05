// -----------------------------------------------------------------------------
// VLTK Mobile — ItemValueService: runtime service cho PC item/itemvalue/*.txt
// Tra cứu giá trị tính toán trang bị theo cấp, loại, magic attribute.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class ItemValueService
    {
        private readonly PcItemValueRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public ItemValueService() { _reg = new PcItemValueRegistry(); }
        public ItemValueService(PcItemValueRegistry reg) { _reg = reg ?? new PcItemValueRegistry(); }

        public static ItemValueService LoadFromStreamingAssets(string subDir = "Reference/PcItemValue")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new ItemValueService(PcItemValueParser.BuildRegistry(path));
        }

        public IReadOnlyList<PcItemValueEntry> All => _reg.All;
        public IReadOnlyList<PcItemValueEntry> GetByCategory(string category) => _reg.GetByCategory(category);
        public IReadOnlyList<PcItemValueEntry> GetByLevel(int level) => _reg.GetByLevel(level);
    }
}
