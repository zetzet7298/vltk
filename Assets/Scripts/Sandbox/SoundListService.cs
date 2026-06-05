// -----------------------------------------------------------------------------
// VLTK Mobile — ST Sound List runtime service
// Source: PC settings/soundlist.txt.
// Quản lý danh sách âm thanh (skill / ui / ambient / combat / npc).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý Sound List (danh sách âm thanh).
    /// Category: 0=skill, 1=ui, 2=ambient, 3=combat, 4=npc.
    /// </summary>
    public class SoundListService
    {
        private PcSoundListRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public SoundListService() { }
        public SoundListService(PcSoundListRegistry reg) { _reg = reg; }

        public void RegisterRegistry(PcSoundListRegistry reg)
        {
            _reg = reg;
            if (_reg == null || _reg.Count == 0)
                SubsystemLog.Warn("SoundList", "Sound list registry rỗng");
        }

        public static SoundListService LoadFromStreamingAssets()
        {
            string root = Path.Combine(Application.streamingAssetsPath, "Reference");
            var reg = PcSoundListParser.BuildRegistry(root);
            return new SoundListService(reg);
        }

        public PcSoundListEntry GetSound(int id) => _reg != null ? _reg.Get(id) : null;
        public IReadOnlyList<PcSoundListEntry> GetByCategory(int category)
            => _reg != null ? _reg.GetByCategory(category) : System.Array.Empty<PcSoundListEntry>();
        public IReadOnlyList<PcSoundListEntry> All
            => _reg != null ? _reg.All : System.Array.Empty<PcSoundListEntry>();
    }
}
