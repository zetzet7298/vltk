// -----------------------------------------------------------------------------
// VLTK Mobile — NpcSFullService: runtime service cho toàn bộ NPC client-side
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class NpcSFullService
    {
        private readonly PcNpcSFullRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public NpcSFullService() : this(null) { }

        public NpcSFullService(PcNpcSFullRegistry reg) { _reg = reg ?? new PcNpcSFullRegistry(); }

        public static NpcSFullService LoadFromStreamingAssets(string subDir = "Reference/PcNpc")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new NpcSFullService(PcNpcSFullParser.BuildRegistry(path));
        }

        public PcNpcSFullEntry GetNpc(int id) => _reg.Get(id);
        public IEnumerable<PcNpcSFullEntry> GetByTemplate(int tpl) => _reg.GetByTemplate(tpl);
        public IEnumerable<PcNpcSFullEntry> GetByFaction(int f) => _reg.GetByFaction(f);
    }
}
