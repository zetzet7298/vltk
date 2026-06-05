// -----------------------------------------------------------------------------
// VLTK Mobile — MapRespawn runtime service
// Wraps PcMapRespawnRegistry. PC source: settings/respawn.txt.
// Vietnamese: "Điểm Hồi Sinh", "Thành Thị", "Chết", "Vật Phẩm", "Kỹ Năng".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class MapRespawnService
    {
        private readonly PcMapRespawnRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public MapRespawnService() { _reg = new PcMapRespawnRegistry(); }
        public MapRespawnService(PcMapRespawnRegistry reg) { _reg = reg ?? new PcMapRespawnRegistry(); }

        public static MapRespawnService LoadFromStreamingAssets(string subDir = "Reference/PcMap")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new MapRespawnService(PcMapRespawnParser.BuildRegistry(path));
        }

        public IReadOnlyList<PcMapRespawnEntry> GetRespawnPoints(int mapId) => _reg.GetByMap(mapId);
        public IReadOnlyList<PcMapRespawnEntry> GetByType(int type) => _reg.GetByType(type);
        public IReadOnlyList<PcMapRespawnEntry> GetAll() => _reg.All;

        /// <summary>Trả về điểm hồi sinh mặc định của map (ưu tiên normal, fallback town).</summary>
        public PcMapRespawnEntry GetDefaultRespawn(int mapId)
        {
            var list = _reg.GetByMap(mapId);
            foreach (var e in list) if (e.respawnType == PcMapRespawnParser.RespawnNormal) return e;
            foreach (var e in list) if (e.respawnType == PcMapRespawnParser.RespawnTown) return e;
            return list.Count > 0 ? list[0] : null;
        }

        public PcMapRespawnEntry GetTownRespawn(int mapId)
        {
            var list = _reg.GetByMap(mapId);
            foreach (var e in list) if (e.respawnType == PcMapRespawnParser.RespawnTown) return e;
            return null;
        }

        public PcMapRespawnEntry GetDeathRespawn(int mapId)
        {
            var list = _reg.GetByMap(mapId);
            foreach (var e in list) if (e.respawnType == PcMapRespawnParser.RespawnDeath) return e;
            return null;
        }
    }
}
