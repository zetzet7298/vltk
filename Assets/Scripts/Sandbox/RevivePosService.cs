// -----------------------------------------------------------------------------
// VLTK Mobile — RevivePosService: runtime service cho vị trí hồi sinh
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class RevivePosService
    {
        private readonly PcRevivePosRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public RevivePosService(PcRevivePosRegistry reg) { _reg = reg ?? new PcRevivePosRegistry(); }

        public static RevivePosService LoadFromStreamingAssets(string subDir = "Reference/PcMap")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new RevivePosService(PcRevivePosParser.BuildRegistry(path));
        }

        public PcRevivePosEntry GetRevive(int id) => _reg.Get(id);
        public IEnumerable<PcRevivePosEntry> GetByMap(int mapId) => _reg.GetByMap(mapId);

        public PcRevivePosEntry GetDefaultRevivePosition(int currentMapId)
        {
            // Simplified: return the first match for the current map, or a fallback.
            return GetByMap(currentMapId).FirstOrDefault() ?? _reg.All.FirstOrDefault();
        }
    }
}
