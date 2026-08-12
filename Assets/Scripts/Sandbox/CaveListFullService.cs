// -----------------------------------------------------------------------------
// VLTK Mobile — CaveListFullService: runtime service cho hang động đầy đủ
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class CaveListFullService
    {
        private readonly PcCaveListFullRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public CaveListFullService() : this(null) { }

        public CaveListFullService(PcCaveListFullRegistry reg) { _reg = reg ?? new PcCaveListFullRegistry(); }

        public static CaveListFullService LoadFromStreamingAssets(string subDir = "Reference/PcMap")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new CaveListFullService(PcCaveListFullParser.BuildRegistry(path));
        }

        public PcCaveListFullEntry GetCave(int id) => _reg.Get(id);
        public IEnumerable<PcCaveListFullEntry> GetByMap(int mapId) => _reg.GetByMap(mapId);
        public IEnumerable<PcCaveListFullEntry> GetByLevel(int level) => _reg.GetByLevel(level);

        public bool CanEnter(int caveId, int playerLevel, int partySize)
        {
            var c = _reg.Get(caveId);
            if (c == null) return false;
            if (playerLevel < c.MinLevel || playerLevel > c.MaxLevel) return false;
            if (partySize < c.MinParty || partySize > c.MaxParty) return false;
            return true;
        }
    }
}
