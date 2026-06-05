// -----------------------------------------------------------------------------
// VLTK Mobile — ST Portrait runtime service
// Source: PC settings/portrait.ini.
// Quản lý chân dung/avatar nhân vật.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý Chân Dung (portrait) - lookup theo ID + filter theo môn phái.
    /// </summary>
    public class PortraitService
    {
        private PcPortraitRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public PortraitService() { }
        public PortraitService(PcPortraitRegistry reg) { _reg = reg; }

        public void RegisterRegistry(PcPortraitRegistry reg)
        {
            _reg = reg;
            if (_reg == null || _reg.Count == 0)
                SubsystemLog.Warn("Portrait", "Portrait registry rỗng");
        }

        public static PortraitService LoadFromStreamingAssets()
        {
            string root = Path.Combine(Application.streamingAssetsPath, "Reference/PcAttrib");
            var reg = PcPortraitParser.BuildRegistry(root);
            return new PortraitService(reg);
        }

        public PcPortraitEntry GetPortrait(int id) => _reg != null ? _reg.Get(id) : null;
        public IReadOnlyList<PcPortraitEntry> GetByFaction(int factionId)
            => _reg != null ? _reg.GetByFaction(factionId) : System.Array.Empty<PcPortraitEntry>();
        public IReadOnlyList<PcPortraitEntry> All
            => _reg != null ? _reg.All : System.Array.Empty<PcPortraitEntry>();
    }
}
