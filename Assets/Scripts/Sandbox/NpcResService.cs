// -----------------------------------------------------------------------------
// VLTK Mobile — NpcResService: runtime service cho tài nguyên NPC
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class NpcResService
    {
        private readonly PcNpcResRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public NpcResService(PcNpcResRegistry reg) { _reg = reg ?? new PcNpcResRegistry(); }

        public static NpcResService LoadFromStreamingAssets(string subDir = "Reference/PcNpc")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new NpcResService(PcNpcResParser.BuildRegistry(path));
        }

        public PcNpcResEntry GetNpc(int id) => _reg.Get(id);
        public IEnumerable<PcNpcResEntry> GetByFaction(int factionId) => _reg.GetByFaction(factionId);
    }
}
