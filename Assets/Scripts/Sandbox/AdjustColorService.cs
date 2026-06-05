// -----------------------------------------------------------------------------
// VLTK Mobile — ST Adjust Color runtime service
// Source: PC settings/adjustcolor.txt.
// Quản lý cấu hình điều chỉnh màu sắc (R/G/B/A).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý Adjust Color (điều chỉnh màu sắc).
    /// </summary>
    public class AdjustColorService
    {
        private PcAdjustColorRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public AdjustColorService() { }
        public AdjustColorService(PcAdjustColorRegistry reg) { _reg = reg; }

        public void RegisterRegistry(PcAdjustColorRegistry reg)
        {
            _reg = reg;
            if (_reg == null || _reg.Count == 0)
                SubsystemLog.Warn("AdjustColor", "Adjust color registry rỗng");
        }

        public static AdjustColorService LoadFromStreamingAssets()
        {
            string root = Path.Combine(Application.streamingAssetsPath, "Reference");
            var reg = PcAdjustColorParser.BuildRegistry(root);
            return new AdjustColorService(reg);
        }

        public PcAdjustColorEntry GetColor(int id) => _reg != null ? _reg.Get(id) : null;
        public IReadOnlyList<PcAdjustColorEntry> All
            => _reg != null ? _reg.All : System.Array.Empty<PcAdjustColorEntry>();
    }
}
