// -----------------------------------------------------------------------------
// VLTK Mobile — BossSpawnService: runtime service cho boss spawn
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class BossSpawnService
    {
        private readonly PcBossSpawnRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public BossSpawnService(PcBossSpawnRegistry reg) { _reg = reg ?? new PcBossSpawnRegistry(); }

        public static BossSpawnService LoadFromStreamingAssets(string subDir = "Reference/PcSpawn")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new BossSpawnService(PcBossSpawnParser.BuildRegistry(path));
        }

        public PcBossSpawnEntry GetBoss(int id) => _reg.Get(id);
        public IEnumerable<PcBossSpawnEntry> GetByMap(int mapId) => _reg.GetByMap(mapId);
    }
}
