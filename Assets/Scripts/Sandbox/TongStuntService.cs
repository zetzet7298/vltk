// -----------------------------------------------------------------------------
// VLTK Mobile — TongStuntService: runtime service cho võ công bang hội
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class TongStuntService
    {
        private readonly PcTongStuntRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public TongStuntService() : this(null) { }

        public TongStuntService(PcTongStuntRegistry reg) { _reg = reg ?? new PcTongStuntRegistry(); }

        public static TongStuntService LoadFromStreamingAssets(string subDir = "Reference/PcTong")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new TongStuntService(PcTongStuntParser.BuildRegistry(path));
        }

        public PcTongStuntEntry GetStunt(int id) => _reg.Get(id);
        public IEnumerable<PcTongStuntEntry> GetForLevel(int level) => _reg.GetForLevel(level);
    }
}
